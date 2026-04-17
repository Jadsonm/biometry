(function() {
  'use strict';

  angular
    .module('readerConfigApp.dashboard', [])
    .controller('DashboardCtrl', DashboardCtrl);

  DashboardCtrl.$inject = ['$scope', 'RestSvc', 'configurations'];

  /* @ngInject */
  function DashboardCtrl($scope, RestSvc, configurations) {
    $scope.config = [];
    $scope.successSAM = false;
    $scope.errorSAM = false;
    $scope.success = false;
    $scope.error = false;
    $scope.searchSAM = false;
    $scope.successControlId = false;
    $scope.errorControlId = false;
    $scope.searchControlId = false;
    $scope.deviceModelList = [{ id: 1, name: 'Dispositivo REP' }, { id: 2, name: 'Dispositivo Acesso' }];

    var connectionType = {
      SAM: 0,
      CONTROLID: 1,
      DIGICON_FACIAL: 2
    };

    $scope.hasErrorForm = function(component) {
      return component.$touched && component.$error.required || component.$touched && component.$invalid;
    };

    function getConfig() {
      $scope.config = configurations;
    }

    getConfig();

    $scope.testSAMConnection = function() {
      var params = {
        "connectionType": connectionType.SAM,
        "samUser": $scope.config.samUser,
        "samPassword": $scope.config.samPassword,
        "applicationLogin": $scope.config.applicationLogin,
        "accessKey": $scope.config.accessKey,
        "accessSecret": $scope.config.accessSecret,
        "tenant": $scope.config.tenant
      };
      $scope.successSAM = false;
      $scope.errorSAM = false;
      $scope.searchSAM = true;
      RestSvc.postServiceEndpoint('/connection', params).then(function() {
        $scope.errorSAM = false;
        $scope.successSAM = true;
        $scope.searchSAM = false;
      }).catch(function() {
        $scope.successSAM = false;
        $scope.errorSAM = true;
        $scope.searchSAM = false;
      });
    };

    $scope.isConnectionTestSAMDisabled = (form) => form.passwordSAM.$invalid || form.userSAM.$invalid || $scope.searchSAM || ($scope.config.applicationLogin && (form.accessKey.$invalid || form.accessSecret.$invalid || form.tenant.$invalid));

    $scope.testControlIdConnection = function() {
      var params = {
        "connectionType": connectionType.CONTROLID,
        "controlIdModel": $scope.config.controlIdModel,
        "controlIdIP": $scope.config.controlIdIP,
        "controlIdUser": $scope.config.controlIdUser,
        "controlIdPassword": $scope.config.controlIdPassword,
        "controlIdPort": $scope.config.controlIdPort
      };
      $scope.successControlId = false;
      $scope.errorControlId = false;
      $scope.searchControlId = true;

      RestSvc.postServiceEndpoint('/connection', params).then(function() {
        $scope.successControlId = true;
        $scope.errorControlId = false;
        $scope.searchControlId = false;
      }).catch(function() {
        $scope.successControlId = false;
        $scope.errorControlId = true;
        $scope.searchControlId = false;
      });
    };

    $scope.isConnectionTestControlIdDisabled = function(form) {
      return form.passwordControlID.$invalid || form.userControliD.$invalid || form.ipDriver.$invalid || form.deviceModelName.$invalid || $scope.searchControlId || form.driverPort.$invalid;
    };

    $scope.testDigiconFacialConnection = function() {
      var params = {
        "connectionType": connectionType.DIGICON_FACIAL,
        "digiconFacialIp": $scope.config.digiconFacialIp,
        "digiconFacialPort": $scope.config.digiconFacialPort
      };
      $scope.successDigiconFacial = false;
      $scope.errorDigiconFacial = false;
      $scope.searchDigiconFacial = true;

      RestSvc.postServiceEndpoint('/connection', params).then(function() {
        $scope.successDigiconFacial = true;
        $scope.errorDigiconFacial = false;
        $scope.searchDigiconFacial = false;
      }).catch(function() {
        $scope.successDigiconFacial = false;
        $scope.errorDigiconFacial = true;
        $scope.searchDigiconFacial = false;
      });
    };

    $scope.isConnectionTestDigiconFacialDisabled = function(form) {
      return form.digiconFacialIp.$invalid || form.digiconFacialPort.$invalid || $scope.searchDigiconFacial;
    };

    $scope.save = function() {
      RestSvc.postServiceEndpoint('/config', $scope.config).then(function() {
        $scope.error = false;
        $scope.success = true;
      }).catch(function() {
        $scope.success = false;
        $scope.error = true;
      });
    };

    $scope.hideMessageSAM = function() {
      $scope.successSAM = false;
      $scope.errorSAM = false;
      $scope.searchSAM = false;
    }

    $scope.isUserLoginRequired = () => !$scope.config.applicationLogin;

  }
})();
