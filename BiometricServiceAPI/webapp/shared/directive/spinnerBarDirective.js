(function () {
    'use strict';

    angular
        .module('readerConfigApp.spinnerbar', [])
        .directive('ngSpinnerBar', spinnerBarDirective);

    spinnerBarDirective.$inject = ['$rootScope', '$http'];

    /* @ngInject */
    function spinnerBarDirective($rootScope, $http) {
        return {
            link: function(scope, element, attrs) {
                var stateProgress = false;
                element.addClass('hide');
                $rootScope.$on('$stateChangeStart', function() {
                    element.removeClass('hide'); // show spinner bar
                });
                $rootScope.$on('$stateChangeSuccess', function() {
                    element.addClass('hide');
                    $('body').removeClass('page-on-load');
                });
                $rootScope.$on('$stateNotFound', function() {
                    element.addClass('hide');
                });
                $rootScope.$on('stateProgressIntegration', function() {
                    stateProgress = true;
                });
                $rootScope.$on('$stateChangeError', function() {
                    element.addClass('hide'); // hide spinner bar
                });
                scope.isLoading = function() {
                    return $http.pendingRequests.length > 0;
                };
                scope.$watch(scope.isLoading, function(v) {
                    if (v) {
                        if (stateProgress){
                            element.addClass('hide');
                        }else{
                            element.removeClass('hide');
                        }
                    } else {
                        stateProgress = false;
                        element.addClass('hide');
                    }
                });

            }
        }    
    }
})();
