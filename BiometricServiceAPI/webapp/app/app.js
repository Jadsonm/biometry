(function () {
    'use strict';
    angular.module('readerConfigApp', [
        'ui.router',
        'ui.bootstrap',
        'pascalprecht.translate',
        'readerConfigApp.dashboard',
        'readerConfigApp.biometricReader',
        'readerConfigApp.service',
        'readerConfigApp.spinnerbar',
        'readerConfigApp.toastr',
        'readerConfigApp.enum'
    ])
        .config(configFunction);

    configFunction.$inject = ['$urlRouterProvider', '$stateProvider', '$translateProvider', '$httpProvider'];

    function configFunction($urlRouterProvider, $stateProvider, $translateProvider, $httpProvider) {
        configTranslateProvider($translateProvider);
        configStateProvider($stateProvider);
        configHttpProvider($httpProvider);

        $urlRouterProvider.otherwise('/dashboard/home');
    }

    function configStateProvider(provider) {
        provider.state('dashboard', {
            abstract: true,
            url: '/dashboard',
            templateUrl: 'app/index.html',
            resolve: {
                preferencesUser: function(RestSvc, $translate){
                    return RestSvc.getService("/preferences").then(function(response){
                        var pt_br = 0;
                        var en = 1;
                        var es = 2;
                        switch (response.locale.language){
                            case pt_br:{
                                $translate.use('pt-br');    
                                break;
                            }
                            case en:{
                                $translate.use('en');
                                break;
                            }
                            case es:{
                                $translate.use('es');
                                break;
                            }
                            default:{
                                $translate.use('pt-br');
                            }
                        }
                        return response;
                    }).catch(function () {
                        $translate.use('pt-br');
                        return [];
                    });
                }
            }
        })
            .state('dashboard.home', {
                url: "/home"
            })
            .state('dashboard.configuration', {
                url: "/configuration",
                templateUrl: "app/modules/dashboard/views/dashboard.html",
                controller: 'DashboardCtrl',
                resolve: {
                    configurations: function (RestSvc, preferencesUser) {
                        return RestSvc.getService('/config');
                    }
                }
            })
            .state('dashboard.readerBiometric', {
                url: "/biometric/:personId",
                templateUrl: "app/modules/biometricReader/views/biometricReader.html",
                controller: 'BiometricReaderController',
                controllerAs: 'reader',
                bindToController: true,
                resolve: {
                    person: function(RestSvc, $stateParams, preferencesUser){
                        if ($stateParams.personId !== "0"){
                            return RestSvc.getService('/person/'+$stateParams.personId).then(function (response) {
                                return response;
                            }).catch(function () {
                                return false;
                            });    
                        }else{
                            return false;
                        }
                    },
                    personPhoto: function(RestSvc, $stateParams){
                        if ($stateParams.personId !== "0"){
                            return RestSvc.getService('/person/'+$stateParams.personId+"/photo");    
                        }else{
                            return false;
                        }
                    }
                }
            });
    }

    function configTranslateProvider(provider) {
        $.getJSON("../assets/translate/pt.json", function (data) {
            provider.translations('pt-br', data);
        });
        $.getJSON("../assets/translate/en.json", function (data) {
            provider.translations('en', data);
        });
        $.getJSON("../assets/translate/es.json", function (data) {
            provider.translations('es', data);
        });
        provider.preferredLanguage('pt-br');
        provider.useSanitizeValueStrategy('escape');
    }

    function configHttpProvider(provider) {
        if (!provider.defaults.headers.get) {
            provider.defaults.headers.get = {};
        }
        if (!provider.defaults.headers.post) {
            provider.defaults.headers.post = {};
        }
        provider.defaults.headers.get['If-Modified-Since'] = 'Mon, 26 Jul 1997 05:00:00 GMT';
        provider.defaults.cache = false;
    }

    function redirectHome($state, toastrSvc, msg) {
        toastrSvc.toastrMsg("error", msg);
        $state.go('dashboard.home');
    }
})();
