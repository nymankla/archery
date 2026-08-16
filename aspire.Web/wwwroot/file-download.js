window.fileDownload = {
    saveFromBase64(fileName, contentType, base64Content) {
        const byteChars = atob(base64Content);
        const byteNumbers = new Array(byteChars.length);
        for (let i = 0; i < byteChars.length; i++) {
            byteNumbers[i] = byteChars.charCodeAt(i);
        }

        const blob = new Blob([new Uint8Array(byteNumbers)], { type: contentType });
        const url = URL.createObjectURL(blob);

        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
    }
};
