$(function () {
    var $guardianSelect = $(".js-guardian-select");
    if ($guardianSelect.length === 0 || !$.fn.select2) {
        return;
    }

    $guardianSelect.select2({
        width: "100%",
        placeholder: $guardianSelect.data("placeholder") || "Seleccioná tutores",
        allowClear: true,
        closeOnSelect: false
    });
});
