(function () {
    'use strict';

    angular
        .module('readerConfigApp.biometricReader', [])
        .controller('BiometricReaderController', BiometricReaderController);

    BiometricReaderController.$inject = ['RestSvc', 'person', 'personPhoto', 'toastrSvc', 'ManufacturerBiometricsEnum', '$translate'];

    /* @ngInject */
    function BiometricReaderController(RestSvc, person, personPhoto, toastrSvc, ManufacturerBiometricsEnum, $translate) {
        var vm = this;
        vm.person = person;
        vm.personId = "";
        vm.personPhoto = personPhoto;
        vm.biometric = [];
        vm.manufacturer = null;
        vm.validBiometricsManufacturers = ManufacturerBiometricsEnum.get.filter(obj => obj.manufactureValid);
        vm.captureDisabled = captureDisabled;
        vm.changeManufacturer = changeManufacturer;
        vm.clearPersonAndSearch = clearPersonAndSearch;
        vm.getPhoto = getPhoto;

        vm.onSelect = onSelect;
        vm.searchPerson = searchPerson;
        vm.showPersonInformation = showPersonInformation;

        vm.modelOptions = {
            debounce: {
                default: 500,
                blur: 250
            },
            getterSetter: true
        };

        vm.biometricCapture = () => {
            const supremaBiometrics = {
                isSupremaBiometrics: vm.manufacturer === vm.validBiometricsManufacturers[1].id,
                supremaBiometricsCapturing: document.getElementById('biometric-suprema-capturing'),
                supremaBiometricsCapturingMessage: document.getElementById('biometric-suprema-capturing-message')
            }
            const params = {
                personId: vm.person.id,
                manufacturer: vm.manufacturer
            };
            RestSvc.postServiceEndpoint("/biometric/capture", params, supremaBiometrics).then(() => {
                toastrSvc.toastrMsg("success", "BIOMETRIA_CADASTRADA_SUCESSO");
                this.hideSupremaMessage(supremaBiometrics);
                window.sendSamReaderEvent('Captura de Biometria');
            }).catch(data => {
                const msg = data.data.data.message;
                
                if (msg) {
                	toastrSvc.toastrMsg("error", msg);
                    window.sendSamReaderEvent('Captura de Biometria', msg);
                } else {
                	toastrSvc.toastrMsg("error", "NAO_FOI_POSSIVEL_CONECTAR_NA_LEITORA_BIOMETRICA");
                    window.sendSamReaderEvent('Captura de Biometria', $translate.instant("NAO_FOI_POSSIVEL_CONECTAR_NA_LEITORA_BIOMETRICA"));
                }
                this.hideSupremaMessage(supremaBiometrics);
            });
        }

        this.hideSupremaMessage = (supremaBiometrics) => {
            RestSvc.hideHtmlElement(supremaBiometrics.supremaBiometricsCapturing);
            RestSvc.hideHtmlElement(supremaBiometrics.supremaBiometricsCapturingMessage);
        }

        function captureDisabled() {
            return vm.manufacturer ? false : true;
        }

        function changeManufacturer(item) {
            vm.manufacturer = item;
        }

        function clearPersonAndSearch() {
            vm.person = false;
            vm.personId = "";
            vm.personSelected = null;
            vm.personPhoto = "";
            window.location.assign("/#/dashboard/biometric/0");
        }

        function getPhoto() {
            return "url(" + vm.personPhoto.photoURL + ")";
        }

        function getPerson() {
            var endPointPerson = "/person/{personId}";
            endPointPerson = endPointPerson.replace('{personId}', vm.personId);
            RestSvc.getService(endPointPerson).then(function (response) {
                vm.person = response;
                getPersonPhoto();
            }).catch(function () {
                toastrSvc.toastrMsg("error", "NAO_POSSIVEL_BUSCAR_INFORMACOES_PESSOA");
            });
        }

        function getPersonPhoto() {
            var endPointPerson = "/person/{personId}/photo";
            endPointPerson = endPointPerson.replace('{personId}', vm.personId);
            RestSvc.getService(endPointPerson).then(function (response) {
                vm.personPhoto = response;
                vm.showPersonInformation = true;
            }).catch(function () {
                toastrSvc.toastrMsg("error", "NAO_POSSIVEL_BUSCAR_A_FOTO_PESSOA");
            });
        }

        function onSelect(item) {
            vm.personId = item.id;
            vm.manufacturer = null;
            window.location.assign("/#/dashboard/biometric/" + item.id);
            getPerson();
        }

        function searchPerson(term) {
            return RestSvc.getService("/serch/person/" + term);
        }

        function showPersonInformation(person){
            if (person){
                return true;
            }
            return false;
        }
    }
})();
