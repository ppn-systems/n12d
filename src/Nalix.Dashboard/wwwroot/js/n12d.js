// The only JavaScript in the dashboard: browser storage, theme switching and file download.
window.n12d = {
  storage: {
    area(kind) { return kind === "session" ? window.sessionStorage : window.localStorage; },
    get(kind, key) { try { return this.area(kind).getItem(key); } catch { return null; } },
    set(kind, key, value) { try { this.area(kind).setItem(key, value); } catch { } },
    remove(kind, key) { try { this.area(kind).removeItem(key); } catch { } }
  },
  setTheme(theme) {
    if (theme === "light" || theme === "dark") document.documentElement.dataset.theme = theme;
    else delete document.documentElement.dataset.theme;
  },
  async download(fileName, contentType, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
};
