# Firebase · Primal Echo

El juego, la PWA y la app de Android usan **el mismo proyecto de Firebase**: una sola cuenta de jugador (Authentication) y un documento por jugador en Cloud Firestore.

## Configurar el proyecto (una sola vez)

1. En [console.firebase.google.com](https://console.firebase.google.com) crea el proyecto (por ejemplo `primal-echo`).
2. **Authentication → Método de acceso:** activa **Correo electrónico/contraseña**.
3. **Firestore Database:** créala en modo producción y pega en la pestaña **Reglas** el contenido de `firestore.rules`.
4. **Configuración del proyecto → General → Tus apps:** agrega una app **Web**. De ahí salen la `apiKey` y el `projectId`.
5. En el juego, pega esos dos valores en `Assets/Resources/Firebase/firebase_config.json`.
6. Para la app de Android, agrega en el mismo proyecto una app **Android** y descarga su `google-services.json`.

Mientras `firebase_config.json` tenga los valores de ejemplo, el menú no pide iniciar sesión y el juego funciona como antes.

## Documento del jugador: `jugadores/{uid}`

`uid` es el identificador del usuario en Firebase Authentication.

| Campo | Tipo | Qué guarda |
| --- | --- | --- |
| `nombre` | string | Nombre de jugador (se pide al crear la cuenta) |
| `correo` | string | Correo de la cuenta |
| `creado` | timestamp | Fecha de registro |
| `nivelActual` | string | Escena donde va el jugador (`Nivel_00_Tutorial`, `Nivel_01_...`, `Nivel_02_...`) |
| `checkpoint` | string | Último checkpoint encendido |
| `jefesDerrotados` | array de string | `Atlas`, `Aeris`, `CentinelaRex` |
| `llaveBase` | boolean | Ya tiene la llave de la base (Nivel 1) |
| `fragmentos` | integer | Fragmentos de eco disponibles |
| `frutas` | integer | Frutas de la selva (máximo 3) |
| `nucleos` | integer | Núcleos de centinela |
| `corazonesMax` | integer | Corazones máximos |
| `mejoras` | array de string | Mejoras compradas en el brazalete |
| `chips` | array de string | Chips de memoria encontrados |
| `actualizado` | timestamp | Última vez que el juego guardó |

El juego **escribe** estos campos; la PWA y la app **los leen**. La colección `novedades` es de solo lectura para todos.

## Archivos del juego

| Archivo | Para qué |
| --- | --- |
| `Assets/Scripts/Firebase/CuentaJugador.cs` | Iniciar sesión, crear cuenta, recuperar contraseña y recordar la sesión |
| `Assets/Scripts/Firebase/Firestore.cs` | Leer y escribir documentos de Firestore (API REST) |
| `Assets/Scripts/Firebase/GuardadoNube.cs` | `GuardadoNube.Subir(datos)` y `GuardadoNube.Descargar(...)` con el progreso del jugador |
| `Assets/Scripts/Menu/MenuInicio.Cuenta.cs` | Pantallas de login y registro en el menú de inicio |
