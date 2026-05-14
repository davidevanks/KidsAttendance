$(function () {
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkout-signature-pad", "CheckOutSignatureBase64", "btn-clear-checkout-signature")
        : null;

    var recordSelect = $("#RecordIds");
    var guardianSelect = $("#CheckOutGuardianId");
    var form = $("#checkout-form");

    recordSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná uno o varios niños"
    });

    guardianSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná tutor que retira"
    });

    function loadGuardiansByChildren() {
        var childIds = [];
        (recordSelect.find(":selected") || []).each(function () {
            var childId = $(this).data("child-id");
            if (childId) {
                childIds.push(childId);
            }
        });

        guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná tutor" })).trigger("change");
        if (childIds.length === 0) {
            return;
        }

        $.get("/Attendance/GetGuardiansForChildren", { childIds: childIds.join(",") })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                data.forEach(function (item) {
                    guardianSelect.append($("<option>", { value: item.id, text: item.fullName + " - " + item.phoneNumber }));
                });

                if (data.length === 1) {
                    guardianSelect.val(String(data[0].id)).trigger("change");
                } else {
                    guardianSelect.trigger("change");
                }
            });
    }

    recordSelect.on("change", loadGuardiansByChildren);
    loadGuardiansByChildren();

    if (!form.length) {
        return;
    }

    form.on("submit", function () {
        if (signatureHandler) {
            signatureHandler.updateHidden();
        }
    });
});
