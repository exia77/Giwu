// Bridge between Blazor and Google Identity Services (GIS).
// Usage from C#:
//   await JS.InvokeAsync<bool>("giwuGoogle.init", clientId, dotNetRef);
//   await JS.InvokeVoidAsync("giwuGoogle.prompt");
//
// The .NET ref must expose a [JSInvokable] method named OnGoogleCredential(string idToken).
//
// In MAUI's WebView the page origin may be a custom scheme that Google's GIS
// rejects. If init throws or the popup never opens, register the WebView's
// origin in the Google Cloud Console under "Authorized JavaScript origins".

window.giwuGoogle = window.giwuGoogle || (function () {
    let dotNetRef = null;
    let initialized = false;

    function ready() {
        return typeof google !== 'undefined' && google.accounts && google.accounts.id;
    }

    function waitForGis(timeoutMs) {
        return new Promise((resolve, reject) => {
            if (ready()) return resolve();
            const start = Date.now();
            const timer = setInterval(() => {
                if (ready()) { clearInterval(timer); resolve(); return; }
                if (Date.now() - start > timeoutMs) {
                    clearInterval(timer);
                    reject(new Error('Google Identity Services script did not load.'));
                }
            }, 100);
        });
    }

    async function init(clientId, ref) {
        if (!clientId) throw new Error('Google Client ID is not configured on the server.');
        await waitForGis(8000);
        dotNetRef = ref;
        google.accounts.id.initialize({
            client_id: clientId,
            callback: (resp) => {
                if (!resp || !resp.credential) return;
                if (dotNetRef) dotNetRef.invokeMethodAsync('OnGoogleCredential', resp.credential);
            },
            ux_mode: 'popup',
            auto_select: false,
        });
        initialized = true;
        return true;
    }

    function prompt() {
        if (!initialized) throw new Error('Call giwuGoogle.init first.');
        google.accounts.id.prompt();
    }

    function renderButton(containerId) {
        if (!initialized) throw new Error('Call giwuGoogle.init first.');
        const el = document.getElementById(containerId);
        if (!el) return;
        google.accounts.id.renderButton(el, {
            theme: 'outline',
            size: 'large',
            type: 'standard',
            text: 'continue_with',
            shape: 'rectangular',
        });
    }

    return { init, prompt, renderButton };
})();
