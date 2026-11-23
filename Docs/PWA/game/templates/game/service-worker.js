
const CACHE_NAME = 'primal-echo-v4';
const OFFLINE_URL = '/offline/';
const OFFLINE_IMAGE = '/static/game/img/offlinesinfondo.png';

// Instalar y guardar la página offline
self.addEventListener('install', event => {

    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll([OFFLINE_URL, OFFLINE_IMAGE]))
            .then(() => self.skipWaiting())
    );

});

// Activar el Service Worker
self.addEventListener('activate', event => {

    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys
                    .filter(key => key.startsWith('primal-echo-') && key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            );
        }).then(() => self.clients.claim())
    );

});

// Detectar cuando no hay conexión
self.addEventListener('fetch', event => {

    const request = event.request;
    const url = new URL(request.url);

    // No interceptar formularios POST
    if (request.method !== 'GET') {
        return;
    }

    // Navegación entre páginas
    if (request.mode === 'navigate') {

        event.respondWith(
            fetch(request).catch(async () => {

                const offlinePage = await caches.match(OFFLINE_URL);

                return offlinePage || new Response(
                    'No tienes conexión a internet.',
                    {
                        headers: {
                            'Content-Type': 'text/plain'
                        }
                    }
                );

            })
        );

        return;
    }

    // Guardar recursos estáticos de la aplicación
    if (url.origin === self.location.origin &&
        url.pathname.startsWith('/static/')) {

        event.respondWith(
            caches.match(request).then(async cached => {

                if (cached) {
                    return cached;
                }

                try {
                    const response = await fetch(request);

                    if (response.ok) {
                        const cache = await caches.open(CACHE_NAME);
                        cache.put(request, response.clone());
                    }

                    return response;

                } catch (error) {
                    return new Response('', { status: 503 });
                }

            })
        );

    }

});
