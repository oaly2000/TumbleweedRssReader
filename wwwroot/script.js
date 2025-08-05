/* -------------------------------------------------------------------------- */
/*                               .NET Interrupt                               */
/* -------------------------------------------------------------------------- */

function __call(obj, method, ...args) {
    return obj[method](...args);
}

function __set(obj, prop, value) {
    obj[prop] = value;
}

function __download(filename, content) {
    const a = document.createElement("a");
    a.href = URL.createObjectURL(new Blob([content]));
    a.download = filename;
    a.click();
}

/* -------------------------------------------------------------------------- */
/*                                  set theme                                 */
/* -------------------------------------------------------------------------- */

(function () {
    if (!window.matchMedia) return;
    
    const DARK = '(prefers-color-scheme: dark)'
    if (window.matchMedia(DARK).matches) document.documentElement.classList.add('wa-dark');
})()

/* -------------------------------------------------------------------------- */
/*                              wa custom events                              */
/* -------------------------------------------------------------------------- */

function registerWebAwesomeEvents() {
    Blazor.registerCustomEventType('WaHide', {
        browserEventName: 'wa-hide',
        createEventArgs: (e) => ({ })
    });
}
