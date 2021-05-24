

function onStartDraw() {
    const canvas = document.getElementById("page-draw-area");
    const content = document.getElementById("page-contents");
    const menu = document.getElementById("page-draw-menu");

    if (canvas == null || content == null || menu == null) {
        return;
    }

    const StackSize = 50;
    let undoStack = [];
    let redoStack = [];

    const parent = canvas.parentElement;
    parent.style.position = "relative";
    parent.style.width = "100%";
    parent.style.height = "100%";

    canvas.style.position = "absolute";
    canvas.style.padding = "0";

    var box = parent.getBoundingClientRect();
    canvas.setAttribute("width", box.width);
    canvas.setAttribute("height", box.height);

    function fitSize() {
        var box = parent.getBoundingClientRect();

        var temp_cnvs = document.createElement('canvas');
        var temp_cntx = temp_cnvs.getContext('2d');
        temp_cnvs.width = box.width;
        temp_cnvs.height = box.height;
        temp_cntx.fillStyle = "rgba(0, 0, 0, .0)";
        temp_cntx.fillRect(0, 0, box.width, box.height);
        temp_cntx.drawImage(canvas, 0, 0);
        canvas.width = box.width;
        canvas.height = box.height;
        context.drawImage(temp_cnvs, 0, 0);
    }

    canvas.style.display = "block";

    const baseIndexZ = content.style.zIndex;

    const context = canvas.getContext('2d');
    const lastPosition = { x: null, y: null };
    let isDrag = false;
    let isPainting = false;

    let IsErase = false;
    let currentEraseWidth = 40;
    let currentStrokeWidth = 1;
    let currentColor = '#000000';

    function draw(x, y) {
        if (!isDrag) {
            return;
        }

        if (IsErase) {
            context.globalCompositeOperation = 'destination-out';
            context.lineWidth = currentEraseWidth;
        }
        else {
            context.globalCompositeOperation = 'source-over';
            context.lineWidth = currentStrokeWidth;
        }

        context.lineCap = 'round';
        context.lineJoin = 'round';
        context.strokeStyle = currentColor;
        if (lastPosition.x === null || lastPosition.y === null) {
            context.moveTo(x, y);
        } else {
            context.moveTo(lastPosition.x, lastPosition.y);
        }
        context.lineTo(x, y);
        context.stroke();

        lastPosition.x = x;
        lastPosition.y = y;
    }

    function paintOn() {
        document.querySelector(".paint-on").style.display = "none";
        document.querySelector(".paint-off").style.display = "inline";
        document.querySelectorAll(".paint-button").forEach(x => {
            x.style.display = "inline";
        });
        fitSize();
        canvas.style.pointerEvents = "auto";
        canvas.style.zIndex = baseIndexZ + 1;
        menu.style.zIndex = baseIndexZ + 2;
        isPainting = true;
    }
    function paintOff() {
        document.querySelector(".paint-on").style.display = "inline";
        document.querySelector(".paint-off").style.display = "none";
        document.querySelectorAll(".paint-button").forEach(x => {
            x.style.display = "none";
        });
        canvas.style.pointerEvents = "none";
        isPainting = false;
    }

    function paintEraseAll() {
        redoStack = [];
        if (undoStack.length >= StackSize) {
            undoStack.pop();
        }
        undoStack.unshift(context.getImageData(0, 0, canvas.width, canvas.height));

        context.clearRect(0, 0, canvas.width, canvas.height);
    }

    function dragStart(_) {
        if (!isPainting) return;
        context.beginPath();
        isDrag = true;

        redoStack = [];
        if (undoStack.length >= StackSize) {
            undoStack.pop();
        }
        undoStack.unshift(context.getImageData(0, 0, canvas.width, canvas.height));
    }

    function dragEnd(_) {
        context.closePath();
        isDrag = false;
        lastPosition.x = null;
        lastPosition.y = null;
    }


    function undo(ev) {
        if (undoStack.length <= 0) return;
        redoStack.unshift(context.getImageData(0, 0, canvas.width, canvas.height));
        context.putImageData(undoStack.shift(), 0, 0);
    }

    function redo(ev) {
        if (redoStack.length <= 0) return;
        undoStack.unshift(context.getImageData(0, 0, canvas.width, canvas.height));
        context.putImageData(redoStack.shift(), 0, 0);
    }


    function initEventHandler() {

        menu.querySelectorAll(".paint-drawer").forEach(button => {
            button.addEventListener('click', x => {
                IsErase = false;
                if (button.hasAttribute("data-size")) {
                    currentStrokeWidth = button.getAttribute("data-size");
                }
                if (button.hasAttribute("data-color")) {
                    currentColor = button.getAttribute("data-color");
                    menu.querySelectorAll(".paint-pen").forEach(x => {
                        x.style.color = currentColor;
                    });
                }
            });
        });

        menu.querySelectorAll(".paint-eraser").forEach(button => {
            button.addEventListener('click', x => {
                IsErase = true;
                if (button.hasAttribute("data-size")) {
                    currentEraseWidth = button.getAttribute("data-size");
                }
            });
        });

        menu.querySelectorAll(".paint-undo").forEach(button => {
            button.addEventListener('click', undo);
        });
        menu.querySelectorAll(".paint-redo").forEach(button => {
            button.addEventListener('click', redo);
        });


        const paintOnButton = document.querySelector('.paint-on');
        paintOnButton.addEventListener('click', paintOn);

        const paintOffButton = document.querySelector('.paint-off');
        paintOffButton.addEventListener('click', paintOff);

        const paintEraseAllButton = document.querySelector('.paint-erase-all');
        paintEraseAllButton.addEventListener('click', paintEraseAll);

        canvas.addEventListener('mousedown', dragStart);
        canvas.addEventListener('mouseup', dragEnd);
        canvas.addEventListener('mouseout', dragEnd);
        canvas.addEventListener('mousemove', (event) => {
            draw(event.layerX, event.layerY);
        });

        canvas.addEventListener('touchstart', (event) => {
            if (event.changedTouches.length == 1 && event.touches[0].touchType == "stylus") {
                dragStart(event);
            }
        });
        canvas.addEventListener('touchend', (event) => {
            if (event.changedTouches.length == 1) {
                dragEnd(event);
            }
        });
        canvas.addEventListener('touchcancel', (event) => {
            if (event.changedTouches.length == 1) {
                dragEnd(event);
            }
        });
        canvas.addEventListener('touchmove', (event) => {
            if (event.changedTouches.length == 1 && event.touches[0].touchType == "stylus") {
                event.preventDefault();
                draw(event.layerX, event.layerY);
            }
        });




        window.addEventListener('resize', fitSize);
    }
    initEventHandler();
}


window.addEventListener('load', onStartDraw);