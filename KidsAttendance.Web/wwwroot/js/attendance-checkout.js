$(function () {
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkout-signature-pad", "CheckOutSignatureBase64", "btn-clear-checkout-signature")
        : null;

    var recordSelect = $("#RecordIds");
    var guardianSelect = $("#CheckOutGuardianId");
    var form = $("#checkout-form");
    var hidePhone = form.data("hide-phone") === true || form.data("hide-phone") === "true";
    var isGlobal = form.data("is-global") === true || form.data("is-global") === "true";

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
        var checkInGuardianIds = [];
        var selectedGuardianId = guardianSelect.val();
        (recordSelect.find(":selected") || []).each(function () {
            var childId = $(this).data("child-id");
            if (childId) {
                childIds.push(childId);
            }

            var checkInGuardianId = $(this).data("check-in-guardian-id");
            if (checkInGuardianId && checkInGuardianIds.indexOf(String(checkInGuardianId)) === -1) {
                checkInGuardianIds.push(String(checkInGuardianId));
            }
        });

        guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná padre" })).trigger("change");
        if (childIds.length === 0) {
            return;
        }

        $.get("/Attendance/GetGuardiansForChildren", {
            childIds: childIds.join(","),
            authorizedPickupOnly: true,
            isGlobal: isGlobal
        })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                data.forEach(function (item) {
                    var text = hidePhone ? item.fullName : item.fullName + " - " + item.phoneNumber;
                    guardianSelect.append($("<option>", { value: item.id, text: text }));
                });

                var availableGuardianIds = data.map(function (item) { return String(item.id); });
                var checkInGuardianId = checkInGuardianIds.length === 1 ? checkInGuardianIds[0] : null;
                var guardianIdToSelect = availableGuardianIds.indexOf(String(selectedGuardianId)) !== -1
                    ? String(selectedGuardianId)
                    : checkInGuardianId && availableGuardianIds.indexOf(checkInGuardianId) !== -1
                        ? checkInGuardianId
                        : "";

                guardianSelect.val(guardianIdToSelect).trigger("change");
            });
    }

    recordSelect.on("select2:selecting", function (event) {
        var selectedOption = recordSelect.find('option[value="' + event.params.args.data.id + '"]');
        var incomingGuardianId = String(selectedOption.data("check-in-guardian-id"));
        var hasDifferentGuardian = recordSelect.find(":selected").toArray().some(function (option) {
            return String($(option).data("check-in-guardian-id")) !== incomingGuardianId;
        });

        if (hasDifferentGuardian) {
            event.preventDefault();
            $("#checkout-mixed-guardians-error").removeClass("d-none");
            return;
        }

        $("#checkout-mixed-guardians-error").addClass("d-none");
    });

    recordSelect.on("select2:unselect", function () {
        $("#checkout-mixed-guardians-error").addClass("d-none");
    });

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
