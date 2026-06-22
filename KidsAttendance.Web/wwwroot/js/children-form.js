$(function () {
    var RELATIONSHIPS = ["Padre", "Madre", "Abuel@", "Herman@", "Tutor"];

    // ── Guardian search (Select2 AJAX) ──────────────────────────────────────
    var $search = $("#guardian-search");
    if ($search.length && $.fn.select2) {
        $search.select2({
            width: "100%",
            placeholder: $search.data("placeholder") || "Buscar tutor por nombre o celular...",
            allowClear: true,
            minimumInputLength: 2,
            language: { inputTooShort: function () { return "Escribí al menos 2 caracteres"; } },
            ajax: {
                url: "/Children/SearchGuardians",
                dataType: "json",
                delay: 300,
                data: function (params) { return { term: params.term }; },
                processResults: function (data) {
                    return {
                        results: data.map(function (g) {
                            return { id: g.id, text: g.fullName + " · " + g.phoneNumber, phone: g.phoneNumber, fullName: g.fullName };
                        })
                    };
                }
            }
        });

        $search.on("select2:select", function (e) {
            var d = e.params.data;
            addGuardianCard({ id: d.id, name: d.fullName, phone: d.phone, type: "existing", relationship: "Tutor" });
            $search.val(null).trigger("change");
        });
    }

    // ── Inline new guardian form ─────────────────────────────────────────────
    var $panel = $("#new-guardian-panel");
    var $btnAdd = $("#btn-add-guardian");
    var $btnConfirm = $("#btn-ng-confirm");
    var $btnCancel = $("#btn-ng-cancel");
    var $ngError = $("#ng-error");

    $btnAdd.on("click", function () {
        $panel.slideDown(150);
        $("#ng-name").focus();
    });

    $btnCancel.on("click", function () {
        clearNewGuardianPanel();
        $panel.slideUp(150);
    });

    $btnConfirm.on("click", function () {
        var name = $("#ng-name").val().trim();
        var phone = $("#ng-phone").val().trim();
        var phone2 = $("#ng-phone2").val().trim();
        var rel = $("#ng-rel").val();
        var phonePattern = /^\d{8,15}$/;

        if (!name) { showNgError("El nombre es requerido."); return; }
        if (!phone || !phonePattern.test(phone)) { showNgError("El celular debe tener entre 8 y 15 dígitos."); return; }
        if (phone2 && !phonePattern.test(phone2)) { showNgError("El celular secundario debe tener entre 8 y 15 dígitos."); return; }
        if (guardianAlreadyAdded(null, phone)) { showNgError("Ya existe un tutor con ese celular en la lista."); return; }

        addGuardianCard({ id: null, name: name, phone: phone, phone2: phone2 || null, relationship: rel, type: "new" });
        clearNewGuardianPanel();
        $panel.slideUp(150);
    });

    function showNgError(msg) {
        $ngError.text(msg).show();
    }

    function clearNewGuardianPanel() {
        $("#ng-name").val("");
        $("#ng-phone").val("");
        $("#ng-phone2").val("");
        $("#ng-rel").val("Padre");
        $ngError.hide().text("");
    }

    // ── Guardian cards ───────────────────────────────────────────────────────
    function buildRelSelect(selected) {
        var opts = RELATIONSHIPS.map(function (r) {
            return '<option value="' + r + '"' + (r === selected ? ' selected' : '') + '>' + r + '</option>';
        }).join("");
        return '<select class="form-select form-select-sm w-auto guardian-rel-select">' + opts + '</select>';
    }

    function addGuardianCard(g) {
        if (guardianAlreadyAdded(g.id, g.phone)) return;
        var dataAttrs = 'data-type="' + g.type + '" data-phone="' + (g.phone || '') + '" data-name="' + g.name + '"';
        if (g.type === "existing") dataAttrs += ' data-id="' + g.id + '"';
        if (g.phone2) dataAttrs += ' data-phone2="' + g.phone2 + '"';

        var card = $('<div class="guardian-card card border-0 bg-light px-3 py-2 d-flex flex-row align-items-center gap-2" ' + dataAttrs + '>' +
            '<div class="flex-grow-1"><span class="fw-semibold">' + g.name + '</span>' +
            '<span class="text-muted small ms-1">· ' + (g.phone || '') + '</span></div>' +
            buildRelSelect(g.relationship || "Tutor") +
            '<button type="button" class="btn btn-sm btn-outline-danger guardian-remove" title="Quitar">&times;</button>' +
            '</div>');
        $("#guardian-list").append(card);
    }

    function guardianAlreadyAdded(id, phone) {
        var found = false;
        $(".guardian-card").each(function () {
            var $c = $(this);
            if (id && $c.data("type") === "existing" && String($c.data("id")) === String(id)) { found = true; return false; }
            if (phone && $c.data("phone") === phone) { found = true; return false; }
        });
        return found;
    }

    $(document).on("click", ".guardian-remove", function () {
        $(this).closest(".guardian-card").remove();
    });

    // ── Serialize guardian data into hidden fields before submit ─────────────
    $("#child-form").on("submit", function () {
        var $container = $("#guardian-hidden-fields").empty();
        var existingIdx = 0;
        var newIdx = 0;

        $(".guardian-card").each(function () {
            var $card = $(this);
            var rel = $card.find(".guardian-rel-select").val() || "Tutor";
            var type = $card.data("type");

            if (type === "existing") {
                $container.append('<input type="hidden" name="SelectedGuardianIds[' + existingIdx + ']" value="' + $card.data("id") + '">');
                $container.append('<input type="hidden" name="SelectedGuardianRelationships[' + existingIdx + ']" value="' + rel + '">');
                existingIdx++;
            } else {
                $container.append('<input type="hidden" name="NewGuardians[' + newIdx + '].FullName" value="' + $card.data("name") + '">');
                $container.append('<input type="hidden" name="NewGuardians[' + newIdx + '].PhoneNumber" value="' + $card.data("phone") + '">');
                var p2 = $card.data("phone2") || "";
                if (p2) $container.append('<input type="hidden" name="NewGuardians[' + newIdx + '].SecondaryPhoneNumber" value="' + p2 + '">');
                $container.append('<input type="hidden" name="NewGuardians[' + newIdx + '].Relationship" value="' + rel + '">');
                newIdx++;
            }
        });
    });
});
