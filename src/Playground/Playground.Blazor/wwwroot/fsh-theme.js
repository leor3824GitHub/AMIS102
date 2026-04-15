window.FshTheme = {
    setFavicon: function (faviconUrl) {
        if (!faviconUrl) {
            // Reset to default
            const link = document.querySelector("link[rel~='icon']");
            if (link) {
                link.href = 'favicon.ico?t=' + Date.now();
            }
            return;
        }

        let link = document.querySelector("link[rel~='icon']");
        if (!link) {
            link = document.createElement('link');
            link.rel = 'icon';
            document.head.appendChild(link);
        }
        link.href = faviconUrl + '?t=' + Date.now();
    }
};

window.PhysicalCount = {
    triggerFileInput: function (inputId, dotnetHelper) {
        const el = document.getElementById(inputId);
        if (!el) return;
        // The 'cancel' event fires when the user dismisses the file picker without selecting.
        const onCancel = () => {
            el.removeEventListener('cancel', onCancel);
            dotnetHelper.invokeMethodAsync('NotifyCameraCancel');
        };
        el.addEventListener('cancel', onCancel);
        el.click();
    }
};