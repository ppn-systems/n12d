window.downloadHelper = {
    downloadFile: function (fileName, contentType, contentBytes) {
        const blob = new Blob([contentBytes], { type: contentType });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        window.URL.revokeObjectURL(link.href);
    }
};
