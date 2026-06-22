$(function () {
    var digitsOnlyPhonePattern = /^\d{8,15}$/;
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkin-signature-pad", "CheckInSignatureBase64", "btn-clear-checkin-signature")
        : null;

    var form = $("#checkin-form");
    var hidePhone = form.data("hide-phone") === true || form.data("hide-phone") === "true";
    var childSelect = $("#ChildIds");
    var guardianSelect = $("#GuardianId");
    var classGroupInput = $("#ClassGroupId");
    var noGuardianFound = $("#no-guardian-found");
    var quickGuardianPanel = $("#quick-guardian-panel");
    var antiForgeryToken = $("input[name='__RequestVerificationToken']").val();

    function getClassGroupId() {
        return parseInt(classGroupInput.val(), 10) || 0;
    }

    function getSelectedChildIds() {
        return (childSelect.val() || []).map(function (id) { return parseInt(id, 10); }).filter(Boolean);
    }

    function guardianText(g) {
        return hidePhone ? g.fullName : g.fullName + " - " + g.phoneNumber;
    }

    function populateGuardianSelect(guardians) {
        guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná quién entrega..." }));
        guardians.forEach(function (g) {
            guardianSelect.append($("<option>", { value: g.id, text: guardianText(g) }));
        });
        guardianSelect.trigger("change");

        if (guardians.length === 1) {
            guardianSelect.val(String(guardians[0].id)).trigger("change");
        }
        noGuardianFound.removeClass("d-none");
    }

    function loadGuardiansByChildren() {
        var ids = getSelectedChildIds();
        if (ids.length === 0) {
            guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná quién entrega..." }));
            guardianSelect.trigger("change");
            noGuardianFound.addClass("d-none");
            quickGuardianPanel.addClass("d-none");
            return;
        }

        $.get("/Attendance/GetGuardiansForChildren", { childIds: ids.join(",") })
            .done(function (data) {
                populateGuardianSelect(Array.isArray(data) ? data : []);
            });
    }

    childSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Buscar niño por nombre...",
        minimumInputLength: 2,
        language: {
            noResults: function () {
                return "No se encontraron resultados.";
            },
            inputTooShort: function () {
                return "Escribí al menos 2 letras.";
            }
        },
        ajax: {
            url: "/Attendance/SearchChildren",
            dataType: "json",
            delay: 250,
            data: function (params) {
                return {
                    term: params.term,
                    classGroupId: getClassGroupId()
                };
            },
            processResults: function (data) {
                return {
                    results: Array.isArray(data) ? data.map(function (item) {
                        return { id: item.id, text: item.fullName };
                    }) : []
                };
            }
        }
    });

    guardianSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná quién entrega..."
    });

    childSelect.on("change", loadGuardiansByChildren);

    classGroupInput.on("change", function () {
        childSelect.val(null).trigger("change");
        guardianSelect.empty().append($("<option>", { value: "", text: "Seleccioná quién entrega..." })).trigger("change");
        noGuardianFound.addClass("d-none");
        quickGuardianPanel.addClass("d-none");
    });

    $("#btn-show-quick-guardian").on("click", function () {
        quickGuardianPanel.toggleClass("d-none");
        if (!quickGuardianPanel.hasClass("d-none")) {
            $("#quick-guardian-name").trigger("focus");
        }
    });

    $("#btn-quick-save-guardian").on("click", function () {
        var name = ($("#quick-guardian-name").val() || "").trim();
        var phone = ($("#quick-guardian-phone").val() || "").trim();
        var ids = getSelectedChildIds();

        if (!name || !phone) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("Nombre y celular son requeridos.");
            return;
        }
        if (!digitsOnlyPhonePattern.test(phone)) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("El celular debe contener solo números (8 a 15 dígitos).");
            return;
        }
        if (ids.length === 0) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("Seleccioná al menos un niño primero.");
            return;
        }

        $.ajax({
            url: "/Attendance/RegisterDropoffGuardian",
            type: "POST",
            data: {
                __RequestVerificationToken: antiForgeryToken,
                fullName: name,
                phoneNumber: phone,
                childIds: ids.join(",")
            }
        }).done(function (result) {
            if (!result || !result.success) {
                var msg = result && result.message ? result.message : "No se pudo guardar.";
                $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text(msg);
                return;
            }

            var optionText = guardianText(result);
            var exists = guardianSelect.find("option[value='" + result.guardianId + "']").length > 0;
            if (!exists) {
                guardianSelect.append($("<option>", { value: result.guardianId, text: optionText }));
            }
            guardianSelect.val(String(result.guardianId)).trigger("change");

            $("#quick-guardian-msg").removeClass("text-danger").addClass("text-success").text("Guardado correctamente.");
            quickGuardianPanel.addClass("d-none");
            noGuardianFound.addClass("d-none");
            $("#quick-guardian-name").val("");
            $("#quick-guardian-phone").val("");
        }).fail(function (xhr) {
            var response = xhr.responseJSON || {};
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text(response.message || "No se pudo guardar.");
        });
    });

    if (!form.length) {
        return;
    }

    form.on("submit", function (e) {
        if (signatureHandler) {
            signatureHandler.updateHidden();
        }

        var signatureValue = $("#CheckInSignatureBase64").val();
        if (!signatureValue) {
            e.preventDefault();
            $("#checkin-signature-error").removeClass("d-none");
            document.getElementById("checkin-signature-pad").scrollIntoView({ behavior: "smooth", block: "center" });
            return false;
        }

        $("#checkin-signature-error").addClass("d-none");
    });

    $("#btn-clear-checkin-signature").on("click", function () {
        $("#checkin-signature-error").addClass("d-none");
    });
});
