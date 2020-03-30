using System;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;
using StackExchange.Redis;

namespace Worker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var pgsql = OpenDbConnection("Server=db;Username=postgres;Password=postgres;");
                var redisConn = OpenRedisConnection("redis");
                var redis = redisConn.GetDatabase();

                // Keep alive is not implemented in Npgsql yet. This workaround was recommended:
                // https://github.com/npgsql/npgsql/issues/1214#issuecomment-235828359
                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                var definition = new { answer1="", answer2="" , voter_id = "", questionid = "" };
                while (true)
                {
                    // Slow down to prevent CPU spike, only query each 100ms
                    Thread.Sleep(100);

                    // Reconnect redis if down
                    if (redisConn == null || !redisConn.IsConnected) {
                        Console.WriteLine("Reconnecting Redis");
                        redisConn = OpenRedisConnection("redis");
                        redis = redisConn.GetDatabase();
                    }
                    string json = redis.ListLeftPopAsync("votes").Result;
                    if (json != null)
                    {
                        var vote = JsonConvert.DeserializeAnonymousType(json, definition);
                        // Console.WriteLine($"Processing vote for '{vote.vote}' by '{vote.voter_id}' '{vote.questionid}'");
                         Console.WriteLine("Json extraction complete ");
                        // Reconnect DB if down
                        if (!pgsql.State.Equals(System.Data.ConnectionState.Open))
                        {
                            Console.WriteLine("Reconnecting DB");
                            pgsql = OpenDbConnection("Server=db;Username=postgres;Password=postgres;");
                        }
                        else
                        { // Normal +1 vote requested
                            string[] answers = new string[2]{vote.answer1, vote.answer2};
                            UpdateVote(pgsql, vote.voter_id, answers, vote.questionid);
                        }
                    }
                    else
                    {
                        keepAliveCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static NpgsqlConnection OpenDbConnection(string connectionString)
        {
            NpgsqlConnection connection;

            while (true)
            {
                try
                {
                    connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    break;
                }
                catch (SocketException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
                catch (DbException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
            }

            Console.Error.WriteLine("Connected to db PgSql");
            Console.Error.WriteLine("Will implement create table command");

            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE votes (
                                        id VARCHAR(255) NOT NULL UNIQUE,
                                        questionid1 VARCHAR(5) NOT NULL,
                                        answer1 VARCHAR(10) NOT NULL,
                                        questionid2 VARCHAR(5) NOT NULL,
                                        answer2 VARCHAR(10) NOT NULL


                                    )";
            command.ExecuteNonQuery();
            
            Console.Error.WriteLine("--------Creation of table successful!!!----------");
            
            return connection;
        }

        private static ConnectionMultiplexer OpenRedisConnection(string hostname)
        {
            // Use IP address to workaround https://github.com/StackExchange/StackExchange.Redis/issues/410
            var ipAddress = GetIp(hostname);
            Console.WriteLine($"Found redis at {ipAddress}");

            while (true)
            {
                try
                {
                    Console.Error.WriteLine("Hello World To  redis");
                    Console.Error.WriteLine("Connecting to redis");
                    return ConnectionMultiplexer.Connect(ipAddress);
                }
                catch (RedisConnectionException)
                {
                    Console.Error.WriteLine("Waiting for redis");
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetIp(string hostname)
            => Dns.GetHostEntryAsync(hostname)
                .Result
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

        private static void UpdateVote(NpgsqlConnection connection, string voterId, string[] answers, string questionid)
        {
            var command = connection.CreateCommand();
            try
            {
                command.CommandText = "INSERT INTO votes (id, questionid1, answer1, questionid2, answer2) VALUES (@id, @questionid1, @answer1, @questionid2, @answer2)";
                command.Parameters.AddWithValue("@id", voterId);
                command.Parameters.AddWithValue("@questionid1", 1);
                command.Parameters.AddWithValue("@answer1", answers[0]);
                command.Parameters.AddWithValue("@questionid2", 2);
                command.Parameters.AddWithValue("@answer2", answers[1]);

                command.ExecuteNonQuery();
            }
            catch (DbException)
            {
                Console.Error.WriteLine("You cannot change your vote");   
                command.ExecuteNonQuery();
            }
            finally
            {
                command.Dispose();
            }
            
            
        }
    }
}
