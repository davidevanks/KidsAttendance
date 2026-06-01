$(function () {
    var digitsOnlyPhonePattern = /^\d{8,15}$/;
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkin-signature-pad", "CheckInSignatureBase64", "btn-clear-checkin-signature")
        : null;

    var form = $("#checkin-form");
    var guardianSelect = $("#GuardianId");
    var childSelect = $("#ChildIds");
    var classGroupInput = $("#ClassGroupId");
    var antiForgeryToken = $("input[name='__RequestVerificationToken']").val();

    function getClassGroupId() {
        return parseInt(classGroupInput.val(), 10) || 0;
    }

    function renderChildrenOptions(children, selectedIds) {
        childSelect.empty();
        children.forEach(function (item) {
            childSelect.append($("<option>", { value: item.id, text: item.fullName }));
        });
        if (Array.isArray(selectedIds) && selectedIds.length > 0) {
            childSelect.val(selectedIds);
        }
        childSelect.trigger("change");
    }

    function loadChildrenByGuardian(newChildIdToSelect) {
        var guardianId = guardianSelect.val();
        var classGroupId = getClassGroupId();
        if (!guardianId || !classGroupId) {
            childSelect.empty();
            return;
        }

        var previouslySelectedIds = childSelect.val() || [];
        $.get("/Attendance/GetChildrenByGuardianAndGroup", { guardianId: guardianId, classGroupId: classGroupId })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                var availableIds = new Set(data.map(function (item) { return String(item.id); }));
                var selectedIds = previouslySelectedIds.filter(function (id) { return availableIds.has(String(id)); });
                if (newChildIdToSelect !== undefined && newChildIdToSelect !== null) {
                    var newChildIdAsString = String(newChildIdToSelect);
                    if (availableIds.has(newChildIdAsString) && selectedIds.indexOf(newChildIdAsString) === -1) {
                        selectedIds.push(newChildIdAsString);
                    }
                }

                if (selectedIds.length === 0 && data.length === 1) {
                    selectedIds = [String(data[0].id)];
                }

                renderChildrenOptions(data, selectedIds);
            });
    }

    guardianSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Buscar padre...",
        minimumInputLength: 2,
        ajax: {
            url: "/Attendance/SearchGuardianByPhone",
            dataType: "json",
            delay: 250,
            data: function (params) {
                return {
                    term: params.term,
                    classGroupId: getClassGroupId()
                };
            },
            processResults: function (data) {
                var results = Array.isArray(data) ? data.map(function (item) {
                    return {
                        id: item.id,
                        text: item.fullName + " - " + item.phoneNumber
                    };
                }) : [];
                return { results: results };
            }
        }
    });

    childSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná uno o varios niños"
    });

    guardianSelect.on("change", loadChildrenByGuardian);

    $("#btn-show-quick-guardian").on("click", function () {
        $("#quick-guardian-panel").toggleClass("d-none");
    });

    $("#btn-show-quick-child").on("click", function () {
        $("#quick-child-panel").toggleClass("d-none");
    });

    $("#btn-quick-save-guardian").on("click", function () {
        var name = ($("#quick-guardian-name").val() || "").toString().trim();
        var phone = ($("#quick-guardian-phone").val() || "").toString().trim();
        if (!name || !phone) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("Nombre y celular son requeridos.");
            return;
        }
        if (!digitsOnlyPhonePattern.test(phone)) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("El celular debe contener solo números (8 a 15 dígitos).");
            return;
        }

        $.ajax({
            url: "/Attendance/QuickAddGuardian",
            type: "POST",
            headers: { "RequestVerificationToken": antiForgeryToken },
            data: {
                __RequestVerificationToken: antiForgeryToken,
                fullName: name,
                phoneNumber: phone
            }
        }).done(function (result) {
            if (!result || !result.success) {
                var errorMessage = result && result.message ? result.message : "No se pudo guardar padre.";
                $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text(errorMessage);
                return;
            }

            var optionText = result.fullName + " - " + result.phoneNumber;
            var newOption = new Option(optionText, result.guardianId, true, true);
            guardianSelect.append(newOption).trigger("change");
            $("#quick-guardian-msg").removeClass("text-danger").addClass("text-success").text("Padre guardado.");
            $("#quick-guardian-panel").addClass("d-none");
        }).fail(function (xhr) {
            var response = xhr.responseJSON || {};
            var errorMessage = response.message || "No se pudo guardar padre.";
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text(errorMessage);
        });
    });

    $("#btn-quick-save-child").on("click", function () {
        var fullName = ($("#quick-child-name").val() || "").toString().trim();
        var ageRawValue = ($("#quick-child-age").val() || "").toString().trim();
        var guardianId = guardianSelect.val();
        var classGroupId = getClassGroupId();
        var age = null;

        if (!guardianId) {
            $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("Seleccioná primero un padre.");
            return;
        }

        if (!fullName || !classGroupId) {
            $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("Nombre de niño requerido.");
            return;
        }
        if (ageRawValue) {
            age = parseInt(ageRawValue, 10);
            if (Number.isNaN(age) || age < 0 || age > 20) {
                $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("La edad debe ser un número entre 0 y 20.");
                return;
            }
        }

        $.ajax({
            url: "/Attendance/QuickAddChild",
            type: "POST",
            headers: { "RequestVerificationToken": antiForgeryToken },
            data: {
                __RequestVerificationToken: antiForgeryToken,
                fullName: fullName,
                classGroupId: classGroupId,
                guardianId: guardianId,
                age: age
            }
        }).done(function (result) {
            if (!result || !result.success) {
                var errorMessage = result && result.message ? result.message : "No se pudo guardar niño.";
                $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text(errorMessage);
                return;
            }

            loadChildrenByGuardian(result.childId);
            $("#quick-child-msg").removeClass("text-danger").addClass("text-success").text("Niño guardado.");
            $("#quick-child-panel").addClass("d-none");
        }).fail(function (xhr) {
            var response = xhr.responseJSON || {};
            var errorMessage = response.message || "No se pudo guardar niño.";
            $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text(errorMessage);
        });
    });

    if (!form.length) {
        return;
    }

    form.on("submit", function () {
        if (signatureHandler) {
            signatureHandler.updateHidden();
        }
    });
});
