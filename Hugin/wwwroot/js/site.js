

// Utilities -------------------------------------------------------------

function modalOn(targetSelector) {
    var modal = document.querySelector(targetSelector);
    if (modal != null) {
        modal.classList.add("is-active")
    }
}
function modalOff(targetSelector) {
    var modal = document.querySelector(targetSelector);
    if (modal != null) {
        modal.classList.remove("is-active")
    }
}
function scrollToBottom(targetSelector) {
    var obj = document.querySelector(targetSelector);
    obj.scrollTop = obj.scrollHeight;
}

