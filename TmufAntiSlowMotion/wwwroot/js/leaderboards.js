var loadedRecords = [];

function setLeaderboard(sender, response, target, mapUid) {

    var html = '<table class="table table-responsive table-sm" style="font-size:0.75rem;border-spacing: 0;">';

    var closestMapLeaderboard = sender.closest(".tr-map-leaderboard");
    var closestMap = sender.closest(".tr-map");

    for (var i = 0; i < response.length; i++) {
        var record = response[i];

        if (closestMapLeaderboard != null && closestMapLeaderboard.getAttribute("data-login") == record.login)
            html += '<tr bgcolor="yellow"><td class="py-0" style="min-width:1.5rem">';
        else if (closestMap != null && closestMap.getAttribute("data-logins").split(",,,").includes(record.login))
            html += '<tr bgcolor="yellow"><td class="py-0" style="min-width:1.5rem">';
        else
            html += '<tr><td class="py-0" style="min-width:1.5rem">';
        html += record.rank;
        html += '</td><td class="py-0">';
        html += record.time;
        html += '</td><td class="py-0 text-nowrap" style="width: 7rem;" title="' + record.login + '">';
        html += record.nickname;
        html += '</td><tr>';
    }

    html += '</table>';

    if (closestMapLeaderboard != null)
        closestMapLeaderboard.getElementsByClassName(target)[0].innerHTML = html;
    else if (target == "td-map-leaderboard-before")
        closestMap.getElementsByClassName(target)[0].innerHTML = html;
    else if (target == "td-map-leaderboard-after")
        closestMap.getElementsByClassName(target)[0].innerHTML = html;

    loadedRecords[mapUid + target] = response;
}

$(".button-map-leaderboard").on("click", function () {
    (function (self) {
        var mapUid = self.getAttribute("data-mapuid");

        if ((mapUid + "td-map-leaderboard-before") in loadedRecords
         && (mapUid + "td-map-leaderboard-after") in loadedRecords) {
            setLeaderboard(self, loadedRecords[mapUid + "td-map-leaderboard-before"], "td-map-leaderboard-before")
            setLeaderboard(self, loadedRecords[mapUid + "td-map-leaderboard-after"], "td-map-leaderboard-after")
            return;
        }

        $.ajax({
            headers: {
                "Accept": "application/json",
            }, url: "/api/v1/leaderboards/map/before/" + mapUid,
            success: function (response) {
                setLeaderboard(self, response, "td-map-leaderboard-before", mapUid)
            }
        })

        $.ajax({
            headers: {
                "Accept": "application/json",
            }, url: "/api/v1/leaderboards/map/after/" + mapUid,
            success: function (response) {
                setLeaderboard(self, response, "td-map-leaderboard-after", mapUid)
            }
        })

    })(this);
})

$(".button-map-leaderboard-before").on("click", function () {
    (function (self) {
        var mapUid = self.closest(".tr-map").getAttribute("data-mapuid");

        if ((mapUid + "td-map-leaderboard-before") in loadedRecords) {
            setLeaderboard(self, loadedRecords[mapUid + "td-map-leaderboard-before"], "td-map-leaderboard-before")
            return;
        }

        $.ajax({
            headers: {
                "Accept": "application/json",
            }, url: "/api/v1/leaderboards/map/before/" + mapUid,
            success: function (response) {
                setLeaderboard(self, response, "td-map-leaderboard-before", mapUid)
            }
        })
    })(this);
})

$(".button-map-leaderboard-after").on("click", function () {
    (function (self) {
        var mapUid = self.closest(".tr-map").getAttribute("data-mapuid");

        if ((mapUid + "td-map-leaderboard-after") in loadedRecords) {
            setLeaderboard(self, loadedRecords[mapUid + "td-map-leaderboard-after"], "td-map-leaderboard-after")
            return;
        }

        $.ajax({
            headers: {
                "Accept": "application/json",
            }, url: "/api/v1/leaderboards/map/after/" + mapUid,
            success: function (response) {
                setLeaderboard(self, response, "td-map-leaderboard-after", mapUid)
            }
        })
    })(this);
})