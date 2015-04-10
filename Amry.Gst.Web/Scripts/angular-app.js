'use strict';

angular
    .module('gstServices', ['ngResource'])
    .factory('GstApi', ['$resource',
        function($resource) {
             return $resource('/api/:searchType/:searchText');
        }]);

angular
    .module('gstApp', ['gstServices', 'ngSanitize'])
    .controller('GstLookupCtrl', ['$scope', 'GstApi',
        function ($scope, GstApi) {
            $scope.searchType = 'GstNo';

            $scope.executeSearch = function () {
                $scope.results = GstApi.query({
                    searchType: $scope.searchType,
                    searchText: $scope.searchText.replace(' ', '_')
                }, function() {
                    // Do nothing if successful
                }, function (response) {
                    alert(response.data.Message + ' ' + response.data.ExceptionMessage);
                });
            };
        }]);