const prefix = "count-to-zero-pwa-";
const cacheName = prefix + {{{ JSON.stringify(PRODUCT_VERSION + "-" + CODE_FILENAME + "-" + DATA_FILENAME) }}};
const shell = ["./", "index.html", "manifest.webmanifest", "TemplateData/style.css",
  "Build/{{{ LOADER_FILENAME }}}", "Build/{{{ FRAMEWORK_FILENAME }}}",
  "Build/{{{ DATA_FILENAME }}}", "Build/{{{ CODE_FILENAME }}}"]; 
self.addEventListener("install", event => {
  event.waitUntil(caches.open(cacheName).then(cache => cache.addAll(shell)).then(() => self.skipWaiting()));
});
self.addEventListener("activate", event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith(prefix) && key !== cacheName).map(key => caches.delete(key)))).then(() => self.clients.claim()));
});
self.addEventListener("fetch", event => {
  if (event.request.method !== "GET" || new URL(event.request.url).origin !== self.location.origin) return;
  event.respondWith(caches.open(cacheName).then(async cache => {
    const cached = await cache.match(event.request);
    if (cached) return cached;
    const response = await fetch(event.request);
    if (response.ok) await cache.put(event.request, response.clone());
    return response;
  }));
});
