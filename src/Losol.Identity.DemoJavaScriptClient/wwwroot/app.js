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
    //response_type: "id_token token",
    scope: "openid profile test.api",
    post_logout_redirect_uri: "http://localhost:5003/index.html",
});

mgr.getUser().then(function (user) {
    if (user) {
        log("User logged in", user.profile);
    }
    else {
        log("User not logged in");
    }
});

function sendSmsCode(phoneNumber) {
    // TODO: add captcha
    var url = "http://localhost:5000/api/phone/verification";
    var xhr = new XMLHttpRequest();
    xhr.open("POST", url, true);
    xhr.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
    xhr.onload = function () {
        log(xhr.status, JSON.parse(xhr.responseText));
    };
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            log("Verification code sent to " + phoneNumber);
            var response = JSON.parse(xhr.responseText);
            alert(xhr.responseText);
        }
    };
    xhr.send("phone=" + encodeURIComponent(phoneNumber));
}

function reSendSmsCode(phoneNumber) {
    // TODO: add captcha
    // TODO: check delay
    var url = "http://localhost:5000/api/phone/verification";
    var xhr = new XMLHttpRequest();
    xhr.open("POST", url, true);
    xhr.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
    xhr.onload = function () {
        log(xhr.status, JSON.parse(xhr.responseText));
    };
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            alert(xhr.responseText);
        }
    };
    xhr.send("phone=" + encodeURIComponent(phoneNumber));
}

function login() {
    mgr.signinRedirect();
}

function api() {
    mgr.getUser().then(function (user) {
        log("Demo API call goes here. Auth token: " + user.access_token);
    });
}

function logout() {
    mgr.signoutRedirect();
}