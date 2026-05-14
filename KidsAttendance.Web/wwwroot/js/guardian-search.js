$(function () {
    var phoneInput = $("#guardian-phone-search");
    var searchButton = $("#btn-guardian-search");
    var guardianSelect = $("#GuardianId");

    if (!phoneInput.length || !searchButton.length || !guardianSelect.length) {
        return;
    }

    searchButton.on("click", function () {
        var phone = phoneInput.val();
        if (!phone) {
            return;
        }

        $.get("/Attendance/SearchGuardianByPhone", { phone: phone })
            .done(function (data) {
                if (!Array.isArray(data)) {
                    return;
                }

                guardianSelect.empty();
                guardianSelect.append($("<option>", { value: "", text: "Tutor" }));
                data.forEach(function (item) {
                    guardianSelect.append($("<option>", {
                        value: item.id,
                        text: item.fullName + " - " + item.phoneNumber
                    }));
                });
            });
    });
});
