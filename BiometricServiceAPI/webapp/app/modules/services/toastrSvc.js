(function() {
    'use strict';

    angular
        .module('readerConfigApp.toastr', [])
        .service('toastrSvc', ToastrSvc);

    ToastrSvc.$inject = ['$translate'];

    /* @ngInject */
    function ToastrSvc($translate) {
        this.toastrMsg = function(type, msg, titleMsg) {
            toastr.clear();
            var toastrOptions = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": false,
                "positionClass": "toast-bottom-full-width",
                "preventDuplicates": false,
                "preventOpenDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "5000",
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut"
            };
            switch (type) {
                case 'warning':
                    return toastr.warning($translate.instant(msg), $translate.instant(titleMsg), toastrOptions);
                case 'info':
                    return toastr.info($translate.instant(msg), $translate.instant(titleMsg), toastrOptions);
                case 'error':
                    return toastr.error($translate.instant(msg), $translate.instant(titleMsg), toastrOptions);
                case 'success':
                    return toastr.success($translate.instant(msg), $translate.instant(titleMsg), toastrOptions);
                default:
                    return toastr.info($translate.instant(msg), $translate.instant(titleMsg), toastrOptions);
            }
        };
    }
})();
