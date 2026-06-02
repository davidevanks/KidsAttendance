$(function () {
    var digitsOnlyPhonePattern = /^\d{8,15}$/;
    var noResultsFocusDelayMs = 1000;
    var focusHighlightClass = "border border-2 border-warning shadow-sm";
    var signatureHandler = window.kidsAttendanceSignatures
        ? window.kidsAttendanceSignatures.initSignaturePad("checkin-signature-pad", "CheckInSignatureBase64", "btn-clear-checkin-signature")
        : null;

    var form = $("#checkin-form");
    var guardianSelect = $("#GuardianId");
    var childSelect = $("#ChildIds");
    var showQuickGuardianButton = $("#btn-show-quick-guardian");
    var showQuickChildButton = $("#btn-show-quick-child");
    var classGroupInput = $("#ClassGroupId");
    var antiForgeryToken = $("input[name='__RequestVerificationToken']").val();
    var guardianSearchState = {
        hasNoResults: false,
        term: "",
        focusTimerId: 0,
        isAutoClosing: false
    };
    var childSearchState = {
        hasNoResults: false,
        term: "",
        focusTimerId: 0,
        isAutoClosing: false
    };

    function clearFocusTimer(searchState) {
        if (searchState.focusTimerId) {
            window.clearTimeout(searchState.focusTimerId);
            searchState.focusTimerId = 0;
        }
    }

    function resetSearchState(searchState) {
        clearFocusTimer(searchState);
        searchState.hasNoResults = false;
        searchState.term = "";
        searchState.isAutoClosing = false;
    }

    function focusQuickAddButton(button) {
        if (!button.length) {
            return;
        }

        button.trigger("focus");
        button.addClass(focusHighlightClass);
        window.setTimeout(function () {
            button.removeClass(focusHighlightClass);
        }, 1800);
    }

    function isDropdownOpen(selectElement) {
        return selectElement.data("select2") && selectElement.data("select2").isOpen();
    }

    function scheduleNoResultsFocus(searchState, selectElement, quickAddButton, shouldFocusCallback) {
        clearFocusTimer(searchState);
        if (!searchState.hasNoResults || searchState.term.length === 0) {
            return;
        }

        searchState.focusTimerId = window.setTimeout(function () {
            searchState.focusTimerId = 0;
            if (!searchState.hasNoResults || !isDropdownOpen(selectElement)) {
                return;
            }

            if (typeof shouldFocusCallback === "function" && !shouldFocusCallback()) {
                return;
            }

            searchState.isAutoClosing = true;
            selectElement.select2("close");
            focusQuickAddButton(quickAddButton);
            resetSearchState(searchState);
        }, noResultsFocusDelayMs);
    }

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
            resetSearchState(childSearchState);
            return;
        }

        var previouslySelectedIds = childSelect.val() || [];
        $.get("/Attendance/GetChildrenByGuardianAndGroup", { guardianId: guardianId, classGroupId: classGroupId })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                childSearchState.hasNoResults = data.length === 0;
                childSearchState.term = "";
                clearFocusTimer(childSearchState);

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
        language: {
            noResults: function () {
                return "No se encontraron resultados.";
            }
        },
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
            processResults: function (data, params) {
                var results = Array.isArray(data) ? data.map(function (item) {
                    return {
                        id: item.id,
                        text: item.fullName + " - " + item.phoneNumber
                    };
                }) : [];

                guardianSearchState.hasNoResults = results.length === 0;
                guardianSearchState.term = (params.term || "").trim();
                if (guardianSearchState.hasNoResults) {
                    scheduleNoResultsFocus(guardianSearchState, guardianSelect, showQuickGuardianButton, function () {
                        return !guardianSelect.val();
                    });
                }
                else {
                    clearFocusTimer(guardianSearchState);
                }

                return { results: results };
            }
        }
    });

    childSelect.select2({
        theme: "bootstrap-5",
        width: "100%",
        placeholder: "Seleccioná uno o varios niños",
        language: {
            noResults: function () {
                return "No se encontraron resultados.";
            }
        },
        matcher: function (params, data) {
            var term = $.trim(params.term || "");
            if (term.length === 0) {
                childSearchState.term = "";
                childSearchState.hasNoResults = false;
                clearFocusTimer(childSearchState);
                return data;
            }

            if (typeof data.text === "undefined") {
                return null;
            }

            if (data.text.toUpperCase().indexOf(term.toUpperCase()) > -1) {
                childSearchState.term = term;
                return data;
            }

            return null;
        }
    });

    guardianSelect.on("change", loadChildrenByGuardian);
    guardianSelect.on("select2:select", function () {
        resetSearchState(guardianSearchState);
    });
    guardianSelect.on("select2:closing", function () {
        if (guardianSearchState.isAutoClosing) {
            guardianSearchState.isAutoClosing = false;
            return;
        }

        clearFocusTimer(guardianSearchState);
        guardianSearchState.hasNoResults = false;
    });

    childSelect.on("change", function () {
        if ((childSelect.val() || []).length > 0) {
            resetSearchState(childSearchState);
        }
    });
    childSelect.on("select2:open", function () {
        var searchField = $(".select2-container--open .select2-search__field");
        searchField.off("input.quickChildFocus").on("input.quickChildFocus", function () {
            window.setTimeout(function () {
                var hasNoResultsMessage = $(".select2-container--open .select2-results__message").length > 0;
                childSearchState.term = (searchField.val() || "").toString().trim();
                childSearchState.hasNoResults = hasNoResultsMessage && childSearchState.term.length > 0;
                if (childSearchState.hasNoResults) {
                    scheduleNoResultsFocus(childSearchState, childSelect, showQuickChildButton);
                }
                else {
                    clearFocusTimer(childSearchState);
                }
            }, 0);
        });
    });
    childSelect.on("select2:closing", function () {
        if (childSearchState.isAutoClosing) {
            childSearchState.isAutoClosing = false;
            return;
        }

        clearFocusTimer(childSearchState);
        childSearchState.hasNoResults = false;
    });

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
