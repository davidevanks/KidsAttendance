(function (window) {
    function initSignaturePad(canvasId, hiddenInputId, clearButtonId) {
        var canvas = document.getElementById(canvasId);
        var hiddenInput = document.getElementById(hiddenInputId);
        var clearButton = document.getElementById(clearButtonId);
        if (!canvas || !hiddenInput || typeof SignaturePad === "undefined") {
            return null;
        }

        function resizeCanvas() {
            var ratio = Math.max(window.devicePixelRatio || 1, 1);
            canvas.width = canvas.offsetWidth * ratio;
            canvas.height = 140 * ratio;
            canvas.getContext("2d").scale(ratio, ratio);
        }

        resizeCanvas();
        var signaturePad = new SignaturePad(canvas, { backgroundColor: "rgb(255,255,255)" });
        window.addEventListener("resize", resizeCanvas);

        if (clearButton) {
            clearButton.addEventListener("click", function () {
                signaturePad.clear();
                hiddenInput.value = "";
            });
        }

        return {
            updateHidden: function () {
                hiddenInput.value = signaturePad.isEmpty() ? "" : signaturePad.toDataURL("image/png");
            },
            clear: function () {
                signaturePad.clear();
                hiddenInput.value = "";
            }
        };
    }

    window.kidsAttendanceSignatures = {
        initSignaturePad: initSignaturePad
    };
})(window);
