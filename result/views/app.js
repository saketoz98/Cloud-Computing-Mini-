var app = angular.module('catsvsdogs', []);
var socket = io.connect({ transports: ['polling'] });

var bg1 = document.getElementById('background-stats-1');
var bg2 = document.getElementById('background-stats-2');
var bg3 = document.getElementById('background-stats-3');
var bg4 = document.getElementById('background-stats-4');
app.controller('statsCtrl', function($scope) {
  $scope.aPercent1 = 25;
  $scope.bPercent1 = 25;
  $scope.cPercent1 = 25;
  $scope.dPercent1 = 25;
  $scope.aPercent2 = 25;
  $scope.bPercent2 = 25;
  $scope.cPercent2 = 25;
  $scope.dPercent2 = 25;

  var updateScores = function() {
    socket.on('scores', function(json) {
      data = JSON.parse(json);
      var a1 = parseInt(data.questionid1.a || 0);
      var b1 = parseInt(data.questionid1.b || 0);
      var c1 = parseInt(data.questionid1.c || 0);
      var d1 = parseInt(data.questionid1.d || 0);
      var percentages1 = getPercentages(a1, b1, c1, d1);

      var a2 = parseInt(data.questionid2.a || 0);
      var b2 = parseInt(data.questionid2.b || 0);
      var c2 = parseInt(data.questionid2.c || 0);
      var d2 = parseInt(data.questionid2.d || 0);
      var percentages2 = getPercentages(a2, b2, c2, d2);

      bg1.style.width = percentages1.a + '%';
      bg2.style.width = percentages1.b + '%';
      bg3.style.width = percentages1.c + '%';
      bg4.style.width = percentages1.d + '%';

      $scope.$apply(function() {
        $scope.aPercent1 = percentages1.a;
        $scope.bPercent1 = percentages1.b;
        $scope.cPercent1 = percentages1.c;
        $scope.dPercent1 = percentages1.d;

        $scope.aPercent2 = percentages2.a;
        $scope.bPercent2 = percentages2.b;
        $scope.cPercent2 = percentages2.c;
        $scope.dPercent2 = percentages2.d;

        $scope.a1 = a1;
        $scope.b1 = b1;
        $scope.c1 = c1;
        $scope.d1 = d1;

        $scope.a2 = a2;
        $scope.b2 = b2;
        $scope.c2 = c2;
        $scope.d2 = d2;

        $scope.total = a1 + b1 + c1 + d1;
      });
    });
  };

  var init = function() {
    document.body.style.opacity = 1;
    updateScores();
  };
  socket.on('message', function(data) {
    init();
  });
});

function getPercentages(a, b, c, d) {
  var result = {};

  if (a + b + c + d > 0) {
    result.a = Math.round((a / (a + b + c + d)) * 100);
    result.b = Math.round((b / (a + b + c + d)) * 100);
    result.c = Math.round((c / (a + b + c + d)) * 100);
    result.d = Math.round((d / (a + b + c + d)) * 100);
    //result.d = 100-(result.a+result.b+result.c);
  } else {
    result.a = result.b = result.c = result.d = 25;
  }

  return result;
}
