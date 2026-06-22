$(function () {
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkout-signature-pad", "CheckOutSignatureBase64", "btn-clear-checkout-signature")
        : null;

    var recordSelect = $("#RecordIds");
    var guardianSelect = $("#CheckOutGuardianId");
    var form = $("#checkout-form");
    var hidePhone = form.data("hide-phone") === true || form.data("hide-phone") === "true";

    recordSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná uno o varios niños"
    });

    guardianSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná padre que retira"
    });

    function loadGuardiansByChildren() {
        var childIds = [];
        (recordSelect.find(":selected") || []).each(function () {
            var childId = $(this).data("child-id");
            if (childId) {
                childIds.push(childId);
            }
        });

        guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná padre" })).trigger("change");
        if (childIds.length === 0) {
            return;
        }

        $.get("/Attendance/GetGuardiansForChildren", { childIds: childIds.join(",") })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                data.forEach(function (item) {
                    var text = hidePhone ? item.fullName : item.fullName + " - " + item.phoneNumber;
                    guardianSelect.append($("<option>", { value: item.id, text: text }));
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

    form.on("submit", function (e) {
        if (signatureHandler) {
            signatureHandler.updateHidden();
        }

        var signatureValue = $("#CheckOutSignatureBase64").val();
        if (!signatureValue) {
            e.preventDefault();
            $("#checkout-signature-error").removeClass("d-none");
            document.getElementById("checkout-signature-pad").scrollIntoView({ behavior: "smooth", block: "center" });
            return false;
        }

        $("#checkout-signature-error").addClass("d-none");
    });

    $("#btn-clear-checkout-signature").on("click", function () {
        $("#checkout-signature-error").addClass("d-none");
    });
});
