window.authSession = (() => {
    let idleTimer = null;
    let dotNetRef = null;
    const events = ["mousemove", "mousedown", "keydown", "scroll", "touchstart", "click"];
    let handler = null;

    function resetIdleTimeout(minutes) {
        if (idleTimer) {
            clearTimeout(idleTimer);
        }

        idleTimer = setTimeout(() => {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnIdleTimeout");
            }
        }, minutes * 60 * 1000);
    }

    return {
        initialize(ref, idleTimeoutMinutes) {
            dotNetRef = ref;
            handler = () => {
                fetch("/auth/activity", { method: "POST", credentials: "include" });
                resetIdleTimeout(idleTimeoutMinutes);
            };

            events.forEach(e => window.addEventListener(e, handler, { passive: true }));
            handler();
        },
        async refresh() {
            await fetch("/auth/refresh", { method: "POST", credentials: "include" });
        },
        dispose() {
            if (handler) {
                events.forEach(e => window.removeEventListener(e, handler));
            }
            if (idleTimer) {
                clearTimeout(idleTimer);
            }
            dotNetRef = null;
            handler = null;
        }
    };
})();
