from flask import Flask, render_template, request, make_response, g
from redis import Redis
import os
import socket
import random
import json

option_a = os.getenv('OPTION_A', "Cats")
option_b = os.getenv('OPTION_B', "Dogs")
hostname = socket.gethostname()

app = Flask(__name__)

questions = [{
    'questionid': '1',
    'question': "Who will win Lok Sabha Elections 2020?",
    'option_a': "BJP",
    'option_b': "CONGRESS",
    'option_c': "AAP",
    'option_d': "OTHERS",
    'answer': ""
},
    {
    'questionid': '2',
    'question': "Who will be the next prime minister of India?",
    'option_a': "Narendra Modi",
    'option_b': "Rahul Gandhi",
    'option_c': "Arvind Kejrival",
    'option_d': "Rinku Sahu",
    'answer': ""
}
]


def get_redis():
    if not hasattr(g, 'redis'):
        g.redis = Redis(host="redis", db=0, socket_timeout=5)
    return g.redis


@app.route("/", methods=['POST', 'GET'])
def hello():
    voter_id = request.cookies.get('voter_id')
    if not voter_id:
        voter_id = hex(random.getrandbits(64))[2:-1]

    vote = []

    if request.method == 'POST':
        redis = get_redis()

        # for i in range(len(questions)):
        #     vote.append(request.form['vote'+str(i)])
        # vote = request.form['vote']
        answer1 = request.form.get('vote0')
        answer2 = request.form.get('vote1')
        data = json.dumps(
            {'voter_id': voter_id, 'answer1': answer1, 'answer2': answer2, 'questionid': '1'})
        redis.rpush('votes', data)
    resp = make_response(render_template(
        'index.html',
        questions=questions,
        hostname=hostname,
        vote=vote,
    ))
    resp.set_cookie('voter_id', voter_id)
    return resp


if __name__ == "__main__":
    app.run(host='0.0.0.0', port=80, debug=True, threaded=True)
