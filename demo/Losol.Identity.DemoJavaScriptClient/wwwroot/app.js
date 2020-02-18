function log() {
    document.getElementById("results").innerText = "";

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        }
        else if (typeof msg !== "string") {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById("results").innerHTML += msg + "\r\n";
    });
}

document.getElementById("login").addEventListener("click", login, false);
document.getElementById("api").addEventListener("click", api, false);
document.getElementById("logout").addEventListener("click", logout, false);

var mgr = new Oidc.UserManager({
    authority: "http://localhost:5000",
    client_id: "demo-js-client",
    redirect_uri: "http://localhost:5003/callback.html",
    response_type: "code",
    scope: "openid profile demo.api",
    post_logout_redirect_uri: "http://localhost:5003/index.html"
});

function updateUIComponents() {
    mgr.getUser().then(function (user) {
        if (user) {
            log("User logged in", user.profile);
        }
        else {
            log("User not logged in");
        }
        var loggedIn = !!user;
        document.getElementById("phone").disabled = loggedIn;
        document.getElementById("login").disabled = loggedIn;
        document.getElementById("logout").disabled = !loggedIn;
        document.getElementById("api").disabled = !loggedIn;
    });
}

function login() {
    mgr.signinRedirect({
        login_hint: document.getElementById("phone").value
    }).then(function () {
        updateUIComponents();
    });
}

function api() {
    mgr.getUser().then(function (user) {
        var url = "http://localhost:5001/identity";
        var xhr = new XMLHttpRequest();
        xhr.open("GET", url);
        xhr.onload = function () {
            log(xhr.status, JSON.parse(xhr.responseText));
        };
        xhr.setRequestHeader("Authorization", "Bearer " + user.access_token);
        xhr.send();
    });
}

function logout() {
    mgr.signoutRedirect().then(function () {
        updateUIComponents();
    });
}

updateUIComponents();