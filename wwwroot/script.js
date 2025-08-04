function __call(obj, method, ...args) {
    return obj[method](...args);
}
function __download(filename, content) {
    const a = document.createElement("a");
    a.href = URL.createObjectURL(new Blob([content]));
    a.download = filename;
    a.click();
}