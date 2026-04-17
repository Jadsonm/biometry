(function() {
    'use strict';

    angular
        .module('readerConfigApp.enum', [])
        .service('ManufacturerBiometricsEnum', ManufacturerBiometricsEnum);

    ManufacturerBiometricsEnum.$inject = [];

    /* @ngInject */
    function ManufacturerBiometricsEnum() {

        this.get = [{
            id: 1,
            key: "FINGERPRINT_SAGEM",
            name: "Fingerprint Sagem",
            manufactureValid: true
        }, {
            id: 2,
            key: "FINGERPRINT_SUPREMA",
            name: "Fingerprint Suprema",
            manufactureValid: true
        }, {
            id: 3,
            key: "FINGERPRINT_VIRDI",
            name: "Fingerprint Virdi",
            manufactureValid: false
        }, {
            id: 4,
            key: "FINGERPRINT_NITGEN",
            name: "Fingerprint Nitgen",
            manufactureValid: true
        }, {
            id: 5,
            key: "FINGERPRINT_CAMA",
            name: "Fingerprint CAMA",
            manufactureValid: true
        }, {
            id: 6,
            key: "FINGERPRINT_INNOVATRICS",
            name: "Fingerprint Innovatrics",
            manufactureValid: true
        }, {
            id: 7,
            key: "HANDKEY_IR",
            name: "HandKey IR",
            manufactureValid: false
        }, {
            id: 8,
            key: "FACIAL",
            name: "Facial",
            manufactureValid: false
        }, {
            id: 9,
            key: "FINGERPRINT_ZKTECO",
            name: "Fingerprint ZKTeco",
            manufactureValid: true
        }, {
            id: 15,
            key: "FINGERPRINT_INTELBRAS_BIOT",
            name: "Fingerprint Intelbras Bio-T",
            manufactureValid: true	
        }, {
            id: 16,
            key: "FACIAL_DIGICON",
            name: "Facial Digicon",
            manufactureValid: true	
        }];
    }
})();