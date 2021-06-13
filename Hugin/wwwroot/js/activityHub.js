"use strict";

var enbaleAction = false;

function selectTab(activityId, tab) {
    var name = tab.getAttribute("data-tab-name");
    var activity = document.getElementById(activityId);

    activity.querySelectorAll(".tab").forEach(c => {
        if (c.getAttribute("data-tab-name") == name) {
            c.classList.add("is-active");
        }
        else {
            c.classList.remove("is-active");
        }
    });

    activity.querySelectorAll(".tab-content").forEach(c => {
        if (c.getAttribute("data-content-name") == name) {
            c.classList.add("is-active");
        }
        else {
            c.classList.remove("is-active");
        }
    });
}

var blocklies = {};

function buildBlockly(dom) {
    var prefix = dom.getAttribute("data-prefix");
    var configure = JSON.parse( dom.querySelector(".blockly-configure").value );
    var definition = dom.querySelector(".block-definition").value;
    var base = { toolbox: document.getElementById(prefix + "toolbox") };
    blocklies[prefix] = Blockly.inject(prefix + "blocklyDiv", { ...base, ...configure });
    Blockly.defineBlocksWithJsonArray(JSON.parse(definition));
}

function disableServerActions() {
    document.querySelectorAll(".activity-action-button").forEach(button => {
        button.setAttribute("disabled", "true");
    });
    document.querySelectorAll(".activity-action-text").forEach(button => {
        button.setAttribute("disabled", "true");
    });
    enbaleAction = false;
}
function enableServerActions() {
    document.querySelectorAll(".activity-action-button").forEach(button => {
        if (!button.hasAttribute("data-allow") || button.getAttribute("data-allow") == "true") {
            button.removeAttribute("disabled");
        }
    });
    document.querySelectorAll(".activity-action-text").forEach(button => {
        button.removeAttribute("disabled");
    });
    enbaleAction = true;
}
function hideMessages(activityId) {
    var activity = document.getElementById(activityId);
    
    activity.querySelector(".messages").style.display = "none";

    activity.querySelectorAll(".message")
        .forEach(x => {
            x.style.display = "none";
        });
    activity.querySelectorAll(".message-body")
        .forEach(x => {
            x.innerText = "";
        });

    activity.querySelector(".activity-figures").innerText = "";
    activity.querySelector(".activity-figures").style.display = "none";
}

function setFileNameToFileUpload(file) {
    var inp = file.parentElement.parentElement;
    var name = inp.querySelector(".file-name");
    name.innerText = file.value;
    name.style.display = "inline";
}

async function getFileValues(activity) {
    var textfiles = {};
    var binaryfiles = {};
    var blocklyfiles = {};

    activity.querySelectorAll(".activity-file").forEach(file => {
        textfiles[file.getAttribute("data-name")] = file.value;
    });

    var files = activity.querySelectorAll(".activity-file-upload");
    for (var i = 0; i < files.length; i++) {
        var inp = files[i].querySelector(".file-input");
        if (inp.files.length > 0) {
            binaryfiles[inp.getAttribute("data-name")] = await readFile(inp);
        }
    }

    activity.querySelectorAll(".activity-blockly").forEach(file => {
        var prefix = file.getAttribute("data-prefix");
        var data = {};
        var xml = Blockly.Xml.workspaceToDom(blocklies[prefix]);
        data["xml"] = Blockly.Xml.domToText(xml);
        data["code-body"] = Blockly.Python.workspaceToCode(blocklies[prefix]);
        data["code-filename"] = file.getAttribute("data-code-filename");
        blocklyfiles[file.getAttribute("data-name")] = JSON.stringify(data);
    });

    activity.querySelectorAll(".activity-file-form").forEach(form => {
        var data = {};
        form.querySelectorAll(".activity-form-input").forEach(x => {
            if (x.tagName == "INPUT" && x.type == "checkbox") {
                data[x.name] = x.checked;
            } else if (x.tagName == "INPUT" && x.type == "radio") {
                if (x.checked) {
                    data[x.name] = x.value;
                }
            } else {
                data[x.name] = x.value;
            }
        });
        textfiles[form.getAttribute("data-name")] = JSON.stringify(data);
    });
    return [textfiles, binaryfiles, blocklyfiles];
}


async function readFile(file) {
    const p = new Promise((resolve, reject) => {
        var reader = new FileReader();
        reader.readAsDataURL(file.files[0]);
        reader.addEventListener("loadend", function (event) {
            resolve(reader.result);
        });
    });
    return await p;
}



var connection = new signalR.HubConnectionBuilder().withUrl("/activityHub").build();

disableServerActions();

connection.on("ReceiveActivityStatus", function (activityId, json) {
    if (json != null) {
        var activity = document.getElementById(activityId);
        var data = JSON.parse(json);

        activity.querySelector(".button-for-show-feedback").style.display = "none";
        activity.querySelector(".label-for-submitted").style.display = "none";
        activity.querySelector(".label-for-require-resubmit").style.display = "none";
        activity.querySelector(".label-for-accepting-resubmit").style.display = "none";
        activity.querySelector(".label-for-confirmed").style.display = "none";
        activity.querySelector(".label-for-disqualified").style.display = "none";
        activity.querySelector(".field-grade").style.display = "none";
        activity.querySelector(".field-feedback-comment").style.display = "none";
        activity.querySelector(".deadline-indicator").style.display = "none";
        activity.querySelector(".no-deadline-indicator").style.display = "none";
        
        if (data["deadline"] != null) {
            activity.querySelector(".deadline-indicator").style.display = "inline";
            activity.querySelector(".deadline-value").innerText = data.deadline;
        } else {
            activity.querySelector(".no-deadline-indicator").style.display = "inline";
        }
        
        if (data["submissionState"] != null) {
            if (data["grade"] != null) {
                activity.querySelector(".button-for-show-feedback").style.display = "block";
                activity.querySelector(".field-grade").style.display = "inline";
                activity.querySelector(".grade").innerText = data.grade;
            }
            if (data["feedbackComment"] != null) {
                activity.querySelector(".button-for-show-feedback").style.display = "block";
                activity.querySelector(".field-feedback-comment").style.display = "inline";
                activity.querySelector(".feedback-comment").value = data.feedbackComment;
            }
            if (data["markedAt"] != null) {
                activity.querySelector(".button-for-show-feedback").style.display = "block";
                activity.querySelector(".marked-at").innerText = data.markedAt;
            }

            if (data.submissionState == "RequiringResubmit") {
                activity.querySelector(".label-for-submitted").style.display = "none";
                activity.querySelector(".label-for-require-resubmit").style.display = "inline-flex";
                activity.querySelector(".label-for-accepting-resubmit").style.display = "none";
                activity.querySelector(".label-for-confirmed").style.display = "none";
                activity.querySelector(".label-for-disqualified").style.display = "none";
            }
            else if (data.submissionState == "AcceptingResubmit") {
                activity.querySelector(".label-for-submitted").style.display = "none";
                activity.querySelector(".label-for-require-resubmit").style.display = "none";
                activity.querySelector(".label-for-accepting-resubmit").style.display = "inline-flex";
                activity.querySelector(".label-for-confirmed").style.display = "none";
                activity.querySelector(".label-for-disqualified").style.display = "none";
            }
            else if (data.submissionState == "Confirmed") {
                activity.querySelector(".label-for-submitted").style.display = "none";
                activity.querySelector(".label-for-require-resubmit").style.display = "none";
                activity.querySelector(".label-for-accepting-resubmit").style.display = "none";
                activity.querySelector(".label-for-confirmed").style.display = "inline-flex";
                activity.querySelector(".label-for-disqualified").style.display = "none";
            }
            else if (data.submissionState == "Disqualified") {
                activity.querySelector(".label-for-submitted").style.display = "none";
                activity.querySelector(".label-for-require-resubmit").style.display = "none";
                activity.querySelector(".label-for-accepting-resubmit").style.display = "none";
                activity.querySelector(".label-for-confirmed").style.display = "none";
                activity.querySelector(".label-for-disqualified").style.display = "inline-flex";
            }
            else {
                activity.querySelector(".label-for-submitted").style.display = "inline-flex";
                activity.querySelector(".label-for-require-resubmit").style.display = "none";
                activity.querySelector(".label-for-accepting-resubmit").style.display = "none";
                activity.querySelector(".label-for-confirmed").style.display = "none";
                activity.querySelector(".label-for-disqualified").style.display = "none";
            }
        }
    }
});

connection.on("ReceiveActionPermissions", function (activityId, canValidate, canSubmit) {
    var activity = document.getElementById(activityId);
    if (canValidate != null) {
        var x = activity.querySelector(".activity-validate");
        if (x != null) {
            x.setAttribute("data-allow", canValidate ? "true" : "false");
            if (enbaleAction) {
                if (canValidate) {
                    x.classList.remove("disabled")
                } else {
                    x.classList.add("disabled")
                }
            }
        }
    }
    if (canSubmit != null) {
        var x = activity.querySelector(".activity-submit-button");
        if (x != null) {
            x.setAttribute("data-allow", canSubmit ? "true" : "false");
            if (enbaleAction) {
                if (canSubmit) {
                    x.classList.remove("disabled")
                } else {
                    x.classList.add("disabled")
                }
            }
        }
    }
});

connection.on("ReceiveActivityXmlResult", function (activityId, xml) {
    if (xml != null) {
        document.getElementById(activityId)
            .querySelector(".messages").style.display = "block";
        var x = document.getElementById(activityId).querySelector(".meesage-activity-stdout");
        x.style.display = "block";
        x.querySelector(".activity-stdout").innerText = xml;
    }
});

connection.on("ReceiveStdout", function (activityId, data) {
    if (data != null) {
        document.getElementById(activityId)
            .querySelector(".messages").style.display = "block";
        var x = document.getElementById(activityId).querySelector(".meesage-activity-stdout");
        x.style.display = "block";
        x.querySelector(".activity-stdout").innerHTML += data?.replace(/&/g, "&amp;")?.replace(/</g, "&lt;")?.replace(/>/g, "&gt;") + "\n";
    }
});
connection.on("ReceiveStderr", function (activityId, data) {
    if (data != null) {
        document.getElementById(activityId)
            .querySelector(".messages").style.display = "block";
        var x = document.getElementById(activityId).querySelector(".meesage-activity-stderr");
        x.style.display = "block";
        x.querySelector(".activity-stderr").innerHTML += data?.replace(/&/g, "&amp;")?.replace(/</g, "&lt;")?.replace(/>/g, "&gt;") + "\n";
    }
});
connection.on("ReceiveCommand", function (activityId, data) {
    if (data != null) {

        var activity = document.getElementById(activityId);
        var page = document.getElementById("content-page");
        var commands = data.split(/[ \n]+/);

        if (commands[0] == "image" && commands.length > 2) {

            var lectureOwnerAccount = page.getAttribute("data-lecture-owner");
            var lectureName = page.getAttribute("data-lecture-name");
            var lectureSubject = page.getAttribute("data-lecture-subject");
            var pagePath = page.getAttribute("data-page");
            var userAccount = page.getAttribute("data-user-account");
            var rivision = page.getAttribute("data-rivision");
            var dir = activity.getAttribute("data-directory");
            var base_url = "" + dir;
            var hstr = Math.random().toString(36).slice(-10);
            var base = "/RawFile/LectureUserData/" + lectureOwnerAccount + "/" + lectureName + "/" + userAccount + "/" + rivision + "/home/";
            var output = "<div class=\"activity-figures\">";
            var output = "  <ul class=\"figures\">";
            for (var i = 2; i < commands.length; i++) {
                var href = "";
                if (dir == "" || dir == null) {
                    href = base + commands[i] + "?" + hstr;
                }
                else {
                    href = base + dir + "/" + commands[i] + "?hstr=" + hstr;
                }
                output += "    <li class=\"figure\">"
                output += "      <a target=\"_blank\" href=\"" + href + "\"><img loading=\"lazy\" src=\"" + href + "\" /></a>";
                output += "      <br/>";
                output += "    " + commands[i];
                output += "    </li>";
            }
            output += "  </ul>";
            output += "  <div class=\"figures-caption\">" + commands[1] + "</div>";
            output += "</div>";

            activity.querySelector(".messages").style.display = "block";
            activity.querySelector(".activity-figures").insertAdjacentHTML('beforeend', output);
            activity.querySelector(".activity-figures").style.display = "block";
        }
    }
});
connection.on("ReceiveActionResult", function (activityId, message, errorMessage, json) {
    enableServerActions();
    var activity = document.getElementById(activityId);
    if (message != null) {
        activity.querySelector(".messages").style.display = "block";
        var x = activity.querySelector(".meesage-activity-message");
        x.style.display = "block";
        x.querySelector(".activity-message").innerHTML += message?.replace(/&/g, "&amp;")?.replace(/</g, "&lt;")?.replace(/>/g, "&gt;") + "\n";
    }
    if (errorMessage != null) {
        activity.querySelector(".messages").style.display = "block";
        var x = activity.querySelector(".meesage-activity-error-message");
        x.style.display = "block";
        x.querySelector(".activity-error-message").innerHTML += errorMessage?.replace(/&/g, "&amp;")?.replace(/</g, "&lt;")?.replace(/>/g, "&gt;") + "\n";
    }
    if (json != null) {
        var data = JSON.parse(json);
        activity.querySelectorAll(".activity-file").forEach(file => {
            var x = file.getAttribute("data-name");
            if (data[x] != null) {
                file.value = data[x];
            } else {
                file.value = null;
            }
        });
        activity.querySelectorAll(".activity-blockly").forEach(block => {
            var x = block.getAttribute("data-name");
            if (data[x] != null) {
                var prefix = block.getAttribute("data-prefix");
                var ws = blocklies[prefix];
                var xml = Blockly.Xml.textToDom(data[x]);
                ws.clear();
                Blockly.Xml.domToWorkspace(xml, ws);
            }
        });
        activity.querySelectorAll(".activity-file-form").forEach(form => {
            var name = form.getAttribute("data-name");
            var obj = JSON.parse(data[name]);
            form.querySelectorAll(".activity-form-input").forEach(x => {
                if (x.tagName == "INPUT" && x.type == "checkbox") {
                    if (obj[x.name]) {
                        x.checked = true;
                    }
                } else if (x.tagName == "INPUT" && x.type == "radio") {
                    x.checked = x.value == obj[x.name];
                } else {
                    x.value = obj[x.name];
                }
            });
        });
    }
});

function simpleAction(connection, action, activityId, profile, args, data) {
    connection
        .invoke(action, activityId, profile, data[0], data[1], data[2])
        .catch(function (err) {
            return console.error(err.toString());
        });
}
function runAction(connection, action, activityId, profile, args, data) {
    connection
        .invoke(action, activityId, profile, args["runner-name"], data[0], data[1], data[2])
        .catch(function (err) {
            return console.error(err.toString());
        });
}

function submitAction(connection, action, activityId, profile, args, data) {
    var msg = document.getElementById(activityId).querySelector(".submit-comment");
    var message = msg.value;
    connection
        .invoke(action, activityId, profile, data[0], data[1], data[2], message)
        .catch(function (err) {
            return console.error(err.toString());
        });
    msg.value = "";
    modalOff("submit-modal-" + activityId);
}
function requestAction(connection, action, activityId, profile, args) {
    connection
        .invoke(action, activityId, profile)
        .catch(function (err) {
            return console.error(err.toString());
        });
}

connection.onclose(function () {
    console.log("Disconnected");
    disableServerActions();
    document.querySelectorAll(".connected-indicator").forEach(x => {
        x.style.display = "none";
    });
    document.querySelectorAll(".disconnected-indicator").forEach(x => {
        x.style.display = "inline";
    });
});

connection.start().then(function () {
    var actions = {
        ".activity-save": { name: "SendSaveRequest", event: simpleAction, send_data: true },
        ".activity-run": { name: "SendRunRequest", event: runAction, send_data: true },
        ".activity-load-test": { name: "SendLoadTestRequest", event: simpleAction, send_data: true },
        ".activity-submit": { name: "SendSubmitRequest", event: submitAction, send_data: true },
        ".activity-validate": { name: "SendValidateRequest", event: simpleAction, send_data: true },
        ".activity-discard": { name: "SendDiscardRequest", event: requestAction, send_data: false },
        ".activity-reset": { name: "SendResetRequest", event: requestAction, send_data: false },
        ".activity-answer": { name: "SendAnswerRequest", event: requestAction, send_data: false },
        ".activity-activity-xml": { name: "SendActivityXmlRequest", event: requestAction, send_data: false },
    };

    console.log("Connected");
    document.querySelectorAll(".activity").forEach(activity => {
        var id = activity.id;
        var profile = activity.getAttribute("data-profile");
        Object.keys(actions).forEach(key => {
            activity.querySelectorAll(key).forEach(x => {
                var args = {};
                args["runner-name"] = x.getAttribute("data-runner-name");
                x.addEventListener("click", function (event) {
                    if (enbaleAction) {
                        disableServerActions();
                        hideMessages(id);
                        var action = actions[key];
                        if (action.send_data) {
                            getFileValues(activity).then((data) => {
                                action.event(connection, action.name, id, profile, args, data)
                            });
                        }
                        else {
                            action.event(connection, action.name, id, profile, args)
                        }
                        event.preventDefault();
                    }
                });
            });
        });

        requestAction(connection, "SendDiscardRequest", id, profile);


        activity.querySelectorAll(".activity-blockly").forEach(block => {
            buildBlockly(block);
        });


        connection
            .invoke("SendActivityStatus", id, profile)
            .catch(function (err) {
                return console.error(err.toString());
            });

    });


    var page = document.getElementById("content-page");
    connection
        .invoke("SendAccess",
            page.getAttribute("data-lecture-owner"), page.getAttribute("data-lecture-name"), page.getAttribute("data-lecture-subject"), page.getAttribute("data-page"), page.getAttribute("data-user-account"))
        .catch(function (err) {
            return console.error(err.toString());
        });


    document.querySelectorAll(".connected-indicator").forEach(x => {
        x.style.display = "inline";
    });
    document.querySelectorAll(".disconnected-indicator").forEach(x => {
        x.style.display = "none";
    });
    enableServerActions();
}).catch(function (err) {
    console.log("Disconnected");
    disableServerActions();
    document.querySelectorAll(".connected-indicator").forEach(x => {
        x.style.display = "none";
    });
    document.querySelectorAll(".disconnected-indicator").forEach(x => {
        x.style.display = "inline";
    });
    return console.error(err.toString());
});

