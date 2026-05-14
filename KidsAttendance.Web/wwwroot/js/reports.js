$(function () {
    var snackContainer = $("#snackteam-count-container");
    if (snackContainer.length) {
        var refresh = function () {
            $.get("/SnackTeam/CurrentCountByGroup")
                .done(function (response) {
                    if (!response || !Array.isArray(response.groups)) {
                        return;
                    }

                    snackContainer.empty();
                    response.groups.forEach(function (item) {
                        snackContainer.append(
                            $("<p>", { "class": "mb-1" }).append($("<strong>").text(item.groupName + ": "), document.createTextNode(item.count))
                        );
                    });
                    snackContainer.append($("<hr>"));
                    snackContainer.append(
                        $("<p>", { "class": "mb-0" }).append($("<strong>").text("Total general: "), document.createTextNode(response.total))
                    );
                });
        };

        refresh();
        setInterval(refresh, 20000);
    }
});
