// CloudFront Function: Unity SDK version resolver
function handler(event) {
    var request = event.request;
    var uri = request.uri;

    // VERSION_LIST_PLACEHOLDER
    var versions = [
        /* versions will be injected here by CI */
    ];

    if (uri.startsWith("/unity/")) {
        var parts = uri.split("/");
        var prefix = parts[2];
        var rest = parts.slice(3).join("/");

        var match = null;
        for (var i = 0; i < versions.length; i++) {
            if (versions[i].startsWith(prefix)) {
                match = versions[i];
                break;
            }
        }

        if (match) {
            request.uri = "/version/unity-sdk-" + match + "/" + rest;
            console.log("Resolved version:", match);
        } else {
            console.log("No matching version for prefix:", prefix);
        }
    }

    return request;
}