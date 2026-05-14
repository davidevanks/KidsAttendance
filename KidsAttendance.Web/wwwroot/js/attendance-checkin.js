$(function () {
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
        placeholder: "Buscar tutor...",
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
        var name = $("#quick-guardian-name").val();
        var phone = $("#quick-guardian-phone").val();
        if (!name || !phone) {
            $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("Nombre y celular son requeridos.");
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
                $("#quick-guardian-msg").removeClass("text-success").addClass("text-danger").text("No se pudo guardar tutor.");
                return;
            }

            var optionText = result.fullName + " - " + result.phoneNumber;
            var newOption = new Option(optionText, result.guardianId, true, true);
            guardianSelect.append(newOption).trigger("change");
            $("#quick-guardian-msg").removeClass("text-danger").addClass("text-success").text("Tutor guardado.");
            $("#quick-guardian-panel").addClass("d-none");
        });
    });

    $("#btn-quick-save-child").on("click", function () {
        var fullName = $("#quick-child-name").val();
        var age = $("#quick-child-age").val();
        var guardianId = guardianSelect.val();
        var classGroupId = getClassGroupId();

        if (!guardianId) {
            $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("Seleccioná primero un tutor.");
            return;
        }

        if (!fullName || !classGroupId) {
            $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("Nombre de niño requerido.");
            return;
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
                age: age || null
            }
        }).done(function (result) {
            if (!result || !result.success) {
                $("#quick-child-msg").removeClass("text-success").addClass("text-danger").text("No se pudo guardar niño.");
                return;
            }

            loadChildrenByGuardian(result.childId);
            $("#quick-child-msg").removeClass("text-danger").addClass("text-success").text("Niño guardado.");
            $("#quick-child-panel").addClass("d-none");
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
