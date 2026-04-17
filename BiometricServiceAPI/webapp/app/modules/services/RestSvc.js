(function() {
    'use strict';

    angular
        .module('readerConfigApp.service', [])
        .service('RestSvc', RestSvc);

    RestSvc.$inject = ['$http', '$q'];

    /* @ngInject */
    function RestSvc($http, $q) {
        this.getService = getService;
        this.getServiceParams = getServiceParams;

        var url = '/rest/readers';

        function getServiceParams(endpoint, queryParams) {
            var future = $q.defer();
            $http.get(url + endpoint, {
                params: queryParams
            })
                .then(getCompleted)
                .catch(getFailed);

            function getCompleted(response) {
                future.resolve(response.data);
            }

            function getFailed(data, status) {
                future.reject({
                    data: data,
                    status: status
                });
            }

            return future.promise;
        }
        
        this.postServiceEndpoint = (endpoint, data, supremaBiometrics) => {
            const future = $q.defer();
            $http.post(`${url}${endpoint}`, data)
                .then(getCompleted)
                .catch(getFailed);

            function getCompleted(response) {
                future.resolve(response.data);
            }

            function getFailed(data, status) {
                future.reject({
                    data: data,
                    status: status
                });
            }

            if (supremaBiometrics && supremaBiometrics.isSupremaBiometrics) {
                this.showHtmlElement(supremaBiometrics.supremaBiometricsCapturing);
                this.showHtmlElement(supremaBiometrics.supremaBiometricsCapturingMessage);
            }

            return future.promise;
        }

        this.showHtmlElement = (element) => element.classList.remove('hide');

        this.hideHtmlElement = (element) => element.classList.add('hide');

        function getService(endpoint) {
            var future = $q.defer();
            $http.get(url + endpoint)
                .then(getCompleted)
                .catch(getFailed);

            function getCompleted(response) {
                future.resolve(response.data);
            }

            function getFailed(data, status) {
                future.reject({
                    data: data,
                    status: status
                });
            }

            return future.promise;
        }

    }
})();