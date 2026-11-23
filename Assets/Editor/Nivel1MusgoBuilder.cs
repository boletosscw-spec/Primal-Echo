using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Construye el Nivel 1 (Bosque del Musgo) desde Assets/Niveles/Nivel_01_Musgo.json con el arte de
    /// Assets/Arte/Nivel1. Conserva de la escena anterior solo a Kael, la cámara, la luz global y el fondo
    /// parallax del bosque; todo lo demás se reemplaza. Se ejecuta solo cuando existe el archivo
    /// Logs/construir_nivel1_musgo.flag (lo borra al terminar). v2 El mapa se edita en Tools/niveles/nivel1_musgo.py.
    ///
    /// También construye el NIVEL 2 (bosque de musgo + base tecnológica) desde Assets/Niveles/Nivel_02_Musgo.json
    /// cuando existe Logs/construir_nivel2_musgo.flag. El mapa del Nivel 2 se edita en Tools/niveles/nivel2_musgo.py
    /// y el arte de la base está en Assets/Arte/Nivel2/Estaticos. La escena del Nivel 2 se rehace desde
    /// SampleScene en cada construcción (la versión anterior se respalda una vez en Assets/Scenes/Respaldo).
    /// </summary>
    [InitializeOnLoad]
    public static class Nivel1MusgoBuilder
    {
        private const string Flag = "Logs/construir_nivel1_musgo.flag";
        private const string Flag2 = "Logs/construir_nivel2_musgo.flag";
        private const string Flag2b = "Logs/construir_nivel2_base_v2.flag";   // aviso nuevo: solo lo entiende esta versión
        private const string BaseScene = "Assets/Scenes/SampleScene.unity";
        private const string Est = "Assets/Arte/Nivel1/Estaticos/";
        private const string Est2 = "Assets/Arte/Nivel2/Estaticos/";
        private const string Ani = "Assets/Arte/Nivel1/Animaciones/";
        private const string Ani2 = "Assets/Arte/Nivel2/Animaciones/";
        // Lo que cambia entre niveles (por defecto: Nivel 1)
        private static string ScenePath = "Assets/Scenes/Nivel_01_Valle_del_Eco.unity";
        private static string DataPath = "Assets/Niveles/Nivel_01_Musgo.json";
        private static string ArtPath = "Assets/Niveles/Nivel_01_Musgo_Arte.json";
        private static string RootName = "Nivel1_Musgo";
        private static string Version = "_Nivel1_Musgo_v5";
        private static string Nombre = "Nivel 1";
        private static bool EsNivel2;

        private static void ConfigNivel1()
        {
            ScenePath = "Assets/Scenes/Nivel_01_Valle_del_Eco.unity";
            DataPath = "Assets/Niveles/Nivel_01_Musgo.json";
            ArtPath = "Assets/Niveles/Nivel_01_Musgo_Arte.json";
            RootName = "Nivel1_Musgo"; Version = "_Nivel1_Musgo_v5"; Nombre = "Nivel 1"; EsNivel2 = false;
        }

        private static void ConfigNivel2()
        {
            ScenePath = "Assets/Scenes/Nivel_02_Templo_Nemesis.unity";
            DataPath = "Assets/Niveles/Nivel_02_Musgo.json";
            ArtPath = "Assets/Niveles/Nivel_02_Musgo_Arte.json";
            RootName = "Nivel2_Musgo_Base"; Version = "_Nivel2_Musgo_v1"; Nombre = "Nivel 2"; EsNivel2 = true;
        }

        /// <summary>Construye el Nivel 2 (bosque de musgo + base tecnológica).</summary>
        [MenuItem("Primal Echo/Niveles/Construir Nivel 1 (Bosque del Musgo)", false, 1)]
        public static void ConstruirNivel1()
        {
            ConfigNivel1();
            Construir();
        }

        [MenuItem("Primal Echo/Niveles/Construir Nivel 2 (Base de NÉMESIS)", false, 2)]
        public static void ConstruirNivel2()
        {
            ConfigNivel2();
            try { Construir(); } finally { ConfigNivel1(); }
        }
        private const int Terrain = 6;
        private const float HalfWidth = 684f / 2f / 32f, HalfHeight = 384f / 2f / 32f;

        // Revisa cada segundo si hay un archivo de aviso en Logs/ (así también funciona si los cambios llegaron
        // mientras estabas en modo Play: se construye en cuanto sales).
        private static double siguienteRevision;

        static Nivel1MusgoBuilder()
        {
            EditorApplication.update -= RevisarAvisos;
            EditorApplication.update += RevisarAvisos;
        }

        private static void RevisarAvisos()
        {
            if (EditorApplication.timeSinceStartup < siguienteRevision) return;
            siguienteRevision = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(Flag))
            {
                try { File.Delete(Flag); } catch (Exception e) { Debug.LogException(e); return; }
                ConstruirNivel1();
            }
            if (File.Exists(Flag2) || File.Exists(Flag2b))
            {
                try { if (File.Exists(Flag2)) File.Delete(Flag2); if (File.Exists(Flag2b)) File.Delete(Flag2b); }
                catch (Exception e) { Debug.LogException(e); return; }
                ConstruirNivel2();
            }
        }

        // ───────────── datos ─────────────
#pragma warning disable 0649
        [Serializable] private class Bloque { public float x0, x1, y0, y1; public string estilo; }
        [Serializable] private class Plat { public float x, y, esc; public string s; public bool unSentido, flip; }
        [Serializable] private class Deco { public string s, ancla; public float x, y, esc, par, rot, r, g, b, a; public int orden; public bool flipX, flipY; }
        [Serializable] private class Anim { public string s; public float x, y, esc, fps, r, g, b, a, par; public int orden; public bool flipX, flipY; }
        [Serializable] private class Luz { public float x, y, r, g, b, i, rad; public bool pulso; }
        [Serializable] private class Peligro { public float x, y, w, h; public bool regresa; }
        [Serializable] private class Rebote { public float x, y, esc, fuerza; }
        [Serializable] private class Enemigo { public string tipo; public float x, y, min, max; }
        [Serializable] private class Npc { public float x, y, esc, radio, kiroX; public bool flipX, desaparece; public string[] lineas; }
        [Serializable] private class Narr { public float x, y, ancho, alto, dur; public string texto; }
        [Serializable] private class Cp { public float x, y; public string nombre; }
        [Serializable] private class Chip { public float x, y; public string titulo, texto; }
        [Serializable] private class Punto { public float x, y; }
        [Serializable] private class Rango { public float x0, x1, y0, y1; }
        [Serializable] private class Jefe { public string n; public float x, y; }
        [Serializable]
        private class Datos
        {
            public string titulo, subtitulo, completado, siguiente;
            public float ancho, alto;
            public Punto inicio, llave, meta;
            public Bloque[] bloques, techos;
            public Plat[] plataformas;
            public Deco[] sprites;
            public Anim[] anims;
            public Luz[] luces;
            public Peligro[] peligros;
            public Rebote[] rebotes;
            public Enemigo[] slimes, centinelas;
            public Npc[] npcs;
            public Narr[] narraciones;
            public Cp[] checkpoints;
            public Punto[] frutas, fragmentos;
            public Chip[] chips;
            public Rango[] cuevas, fondoCueva;
            // Nivel 2
            public Rango[] fondoBase;          // pared interior de la base (detrás de todo)
            public Punto[] puertas;            // barrera de la base que se abre con la llave
            public Jefe[] jefes;               // marcadores "Vista_<n>" para que cada jefe sepa dónde aparecer
            public string subMeta;             // subtítulo al terminar el nivel
            public bool metaSinLlave;          // la salida no pide la llave
        }
        [Serializable] private class ArteEst { public string n; public float w, h, borde, surf; }
        [Serializable] private class ArteAni { public string n; public int cols, filas, cuadros, fw, fh, pad; public float fps, w, h; }
        [Serializable] private class Arte { public ArteEst[] estaticos; public ArteAni[] animaciones; }
#pragma warning restore 0649

        private static Dictionary<string, ArteEst> est;
        private static Dictionary<string, ArteAni> ani;
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static Transform lucesRoot;
        private static int faltantes;

        // ───────────── construcción ─────────────
        public static void Construir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[" + Nombre + "] Sal del modo Play primero."); return; }
            try
            {
                EditorUtility.DisplayProgressBar("Primal Echo", "Construyendo el " + Nombre + "…", 0.1f);
                var d = JsonUtility.FromJson<Datos>(File.ReadAllText(DataPath));
                var arte = JsonUtility.FromJson<Arte>(File.ReadAllText(ArtPath));
                est = arte.estaticos.ToDictionary(e => e.n);
                ani = arte.animaciones.ToDictionary(a => a.n);
                sprites.Clear();
                faltantes = 0;

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (EsNivel2) RehacerEscenaDesdeBase();
                if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(BaseScene, ScenePath))
                    throw new IOException("No se pudo crear " + ScenePath);
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (EsNivel2) JungleLevelBuilder.PrepararEscenaNivel2(scene);

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) throw new InvalidOperationException("La escena no tiene a Kael (tag Player).");
                Camera cam = Camera.main;
                if (cam == null) throw new InvalidOperationException("La escena no tiene cámara principal.");

                Limpiar();
                EditorUtility.DisplayProgressBar("Primal Echo", "Terreno…", 0.3f);

                var root = new GameObject(RootName).transform;
                new GameObject(Version).transform.SetParent(root, false);
                Transform terreno = Hijo(root, "Terreno");
                Transform deco = Hijo(root, "Decoracion");
                Transform juego = Hijo(root, "Jugabilidad");
                Transform historia = Hijo(root, "Historia");
                lucesRoot = (GameObject.Find("Iluminacion") ?? new GameObject("Iluminacion")).transform;

                // fondo de la cueva (tapa el bosque detrás)
                foreach (var f in d.fondoCueva ?? Array.Empty<Rango>())
                {
                    var sr = Poner(deco, "negro", (f.x0 + f.x1) / 2f, (f.y0 + f.y1) / 2f, 1f, -50, "centro");
                    if (sr == null) continue;
                    sr.name = "Fondo_Cueva";
                    sr.transform.localScale = new Vector3((f.x1 - f.x0) / sr.sprite.bounds.size.x, (f.y1 - f.y0) / sr.sprite.bounds.size.y, 1f);
                    sr.color = new Color(0.025f, 0.022f, 0.035f, 1f); // negro azulado opaco: el bosque ya no se transparenta
                    // orillas con degradado para que la entrada y la salida de la cueva no se vean como un corte
                    sr.gameObject.AddComponent<DegradadoCueva>().Configure(f.x0, f.x1, f.y0, f.y1, sr.color, 10f, sr.sortingOrder);
                }
                // pared interior de la base tecnológica (se repite en mosaico)
                foreach (var f in d.fondoBase ?? Array.Empty<Rango>()) CrearFondoBase(deco, f);
                // oscuridad al fondo de los fosos
                var abismo = Poner(deco, "abismo", d.ancho / 2f, 1f, 1f, 7, "centro");
                if (abismo != null)
                {
                    abismo.name = "Oscuridad_Fosos";
                    abismo.transform.localScale = new Vector3((d.ancho + 60f) / abismo.sprite.bounds.size.x, 4.2f / abismo.sprite.bounds.size.y, 1f);
                }

                var todos = (d.bloques ?? Array.Empty<Bloque>()).Select(b => (b, techo: false))
                    .Concat((d.techos ?? Array.Empty<Bloque>()).Select(b => (b, techo: true)))
                    .OrderBy(t => t.b.y1).ToList();
                for (int i = 0; i < todos.Count; i++) CrearBloque(terreno, todos[i].b, todos[i].techo, i % 2);
                foreach (var p in d.plataformas ?? Array.Empty<Plat>()) CrearPlataforma(terreno, p);

                EditorUtility.DisplayProgressBar("Primal Echo", "Decoración…", 0.5f);
                foreach (var s in d.sprites ?? Array.Empty<Deco>())
                {
                    var sr = Poner(deco, s.s, s.x, s.y, s.esc, s.orden, s.ancla, s.flipX, s.flipY, s.rot);
                    if (sr == null) continue;
                    sr.color = new Color(s.r, s.g, s.b, s.a);
                    if (Mathf.Abs(s.par) > 0.001f) sr.gameObject.AddComponent<ParallaxSimple>().Configure(s.par);
                }
                foreach (var a in d.anims ?? Array.Empty<Anim>())
                {
                    var sr = Animado(deco, a.s, a.x, a.y, a.esc, a.orden, a.flipX, a.fps, a.flipY);
                    if (sr == null) continue;
                    sr.color = new Color(a.r, a.g, a.b, a.a);
                    if (Mathf.Abs(a.par) > 0.001f) sr.gameObject.AddComponent<ParallaxSimple>().Configure(a.par);
                }
                foreach (var l in d.luces ?? Array.Empty<Luz>())
                    CrearLuz("Brillo", new Vector3(l.x, l.y, 0f), new Color(l.r, l.g, l.b), l.i, l.rad, l.pulso);

                EditorUtility.DisplayProgressBar("Primal Echo", "Jugabilidad…", 0.7f);
                foreach (var z in d.peligros ?? Array.Empty<Peligro>()) CrearPeligro(juego, z);
                foreach (var r in d.rebotes ?? Array.Empty<Rebote>()) CrearRebote(juego, r);
                foreach (var e in d.slimes ?? Array.Empty<Enemigo>()) CrearSlime(juego, e);
                foreach (var e in d.centinelas ?? Array.Empty<Enemigo>()) CrearCentinela(juego, e);
                foreach (var c in d.checkpoints ?? Array.Empty<Cp>()) CrearCheckpoint(juego, c);
                foreach (var f in d.frutas ?? Array.Empty<Punto>()) CrearObjeto(juego, f.x, f.y, ItemKind.Fruit, "", "", "");
                var chips = d.chips ?? Array.Empty<Chip>();
                for (int i = 0; i < chips.Length; i++)
                    CrearObjeto(juego, chips[i].x, chips[i].y, ItemKind.Chip, "Nivel_01_Musgo_chip_" + i, chips[i].titulo, chips[i].texto);
                int orbes = 0;
                foreach (var o in d.fragmentos ?? Array.Empty<Punto>()) { CrearOrbe(juego, o); orbes++; }
                // JsonUtility nunca deja un campo en null: sin "llave" en el JSON llega en (0,0) y no se pone
                if (d.llave != null && (d.llave.x != 0f || d.llave.y != 0f)) CrearLlave(juego, d.llave);
                foreach (var pu in d.puertas ?? Array.Empty<Punto>()) CrearPuerta(juego, pu);
                foreach (var j in d.jefes ?? Array.Empty<Jefe>())
                {
                    var marca = new GameObject("Vista_" + j.n);
                    marca.transform.SetParent(juego, false);
                    marca.transform.position = new Vector3(j.x, j.y, 0f);
                }
                if (d.meta != null) CrearMeta(juego, d);
                foreach (var n in d.npcs ?? Array.Empty<Npc>()) CrearNpc(historia, n);
                foreach (var n in d.narraciones ?? Array.Empty<Narr>()) CrearNarracion(historia, n);

                // Kael, cámara y cueva
                player.transform.position = new Vector3(d.inicio.x, d.inicio.y + 0.05f, 0f);
                if (player.TryGetComponent(out KaelRespawn respawn)) SetField(respawn, "startName", "Inicio");
                EditorUtility.SetDirty(player);
                ConfigurarCamara(cam, d, player.transform.position);
                ConfigurarCueva(cam, d);

                var hud = new GameObject("HUD_Nivel").AddComponent<LevelHUD>();
                hud.transform.SetParent(juego);
                hud.Configure(d.titulo, d.subtitulo, orbes);
                if (!EsNivel2) CrearCinematica(historia);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("No se pudo guardar " + ScenePath);
                Debug.Log($"<color=#4CAF50><b>[Primal Echo]</b> {Nombre} construido: {todos.Count} bloques, " +
                          $"{d.plataformas?.Length ?? 0} plataformas, {d.centinelas?.Length ?? 0} centinelas, {orbes} orbes." +
                          (faltantes > 0 ? $" Faltaron {faltantes} sprites (ver avisos)." : "") + "</color>");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Primal Echo", "No se pudo construir el " + Nombre + ":\n" + e.Message, "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void Limpiar()
        {
            foreach (var name in new[] { "Ambientacion_Valle", "Carteles", "Jugabilidad", RootName, "Sala del mirador" })
            {
                var go = GameObject.Find(name);
                while (go != null && go.transform.parent == null) { Object.DestroyImmediate(go); go = GameObject.Find(name); }
            }
            foreach (var room in Object.FindObjectsByType<CameraRoom2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(room.gameObject);
            var entorno = GameObject.Find("Entorno");
            if (entorno != null)
                foreach (var child in new[] { "Grid", "Meta_Nivel", "Meta_Mirador_Nivel1" })
                {
                    var t = entorno.transform.Find(child);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
            var luces = GameObject.Find("Iluminacion");
            if (luces != null)
                for (int i = luces.transform.childCount - 1; i >= 0; i--)
                    if (luces.transform.GetChild(i).name != "Luz_Global_2D") Object.DestroyImmediate(luces.transform.GetChild(i).gameObject);
            var fondo = GameObject.Find("Fondo_Parallax");
            if (fondo != null)
            {
                for (int i = fondo.transform.childCount - 1; i >= 0; i--)
                    if (fondo.transform.GetChild(i).name.StartsWith("Cueva_")) Object.DestroyImmediate(fondo.transform.GetChild(i).gameObject);
                if (!Enumerable.Range(0, fondo.transform.childCount).Any(i => fondo.transform.GetChild(i).name.StartsWith("Capa_")))
                    Debug.LogWarning("[Nivel 1] Fondo_Parallax no tiene las capas del bosque.");
            }
        }

        // ───────────── terreno ─────────────
        private static void CrearBloque(Transform parent, Bloque b, bool techo, int orden)
        {
            bool cueva = b.estilo == "cueva", baseTec = b.estilo == "base";
            string pieza = baseTec ? "base_bloque" : cueva ? "cueva_bloque" : "musgo_bloque";
            float extra = baseTec ? 0.1f : cueva ? 0.18f : 0.3f;
            var go = new GameObject(techo ? "Techo" : "Suelo") { layer = Terrain };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((b.x0 + b.x1) / 2f, (b.y0 + b.y1) / 2f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(b.x1 - b.x0, b.y1 - b.y0);
            var sprite = Cargar(pieza);
            if (sprite == null) return;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive;
            sr.size = new Vector2(b.x1 - b.x0 + extra * 2f, b.y1 - b.y0 + extra * 2f);
            sr.sortingOrder = orden;
        }

        private static void CrearPlataforma(Transform parent, Plat p)
        {
            if (!est.TryGetValue(p.s, out var info)) { Falta(p.s); return; }
            float esc = p.esc > 0f ? p.esc : 1f;
            var go = new GameObject("Plataforma_" + p.s) { layer = Terrain };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(p.x, p.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            float w = info.w * esc;
            if (p.unSentido)
            {
                col.size = new Vector2(w * 0.85f, 0.35f);
                col.offset = new Vector2(0f, -0.175f);
                col.usedByEffector = true;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.surfaceArc = 170f;
            }
            else
            {
                float h = (info.h - info.surf) * esc * 0.95f;
                col.size = new Vector2(w * 0.72f, h);
                col.offset = new Vector2(0f, -h / 2f);
            }
            var sprite = Cargar(p.s);
            if (sprite == null) return;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = p.flip;
            sr.sortingOrder = 1;
            vis.transform.localScale = new Vector3(esc, esc, 1f);
            // el borde de hojas sobresale arriba de la superficie que se pisa
            float top = info.surf * esc;
            vis.transform.localPosition = new Vector3(0f, top - info.h * esc / 2f, 0f);
        }

        // ───────────── sprites ─────────────
        private static Sprite Cargar(string name)
        {
            if (sprites.TryGetValue(name, out var s)) return s;
            if (EsNivel2) s = AssetDatabase.LoadAssetAtPath<Sprite>(Est2 + name + ".png");
            if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(Est + name + ".png");
            if (s == null) Falta(name);
            sprites[name] = s;
            return s;
        }

        private static void Falta(string name)
        {
            faltantes++;
            Debug.LogWarning("[" + Nombre + "] Falta el sprite " + name + " en " + Est + (EsNivel2 ? " ni en " + Est2 : ""));
        }

        private static SpriteRenderer Poner(Transform parent, string name, float x, float y, float esc, int orden, string ancla,
            bool flipX = false, bool flipY = false, float rot = 0f)
        {
            var sprite = Cargar(name);
            if (sprite == null) return null;
            if (esc <= 0f) esc = 1f;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flipX; sr.flipY = flipY;
            sr.sortingOrder = orden;
            float h = sprite.bounds.size.y * esc;
            float py = ancla == "abajo" ? y + h / 2f : ancla == "arriba" ? y - h / 2f : y;
            go.transform.position = new Vector3(x, py, 0f);
            go.transform.localScale = new Vector3(esc, esc, 1f);
            if (Mathf.Abs(rot) > 0.01f)
            {
                // girar alrededor del punto de anclaje
                Vector3 pivote = new Vector3(x, y, 0f);
                go.transform.RotateAround(pivote, Vector3.forward, rot);
            }
            return sr;
        }

        private static SpriteRenderer Animado(Transform parent, string name, float x, float y, float esc, int orden, bool flipX, float fps, bool flipY = false)
        {
            if (!ani.TryGetValue(name, out var a)) { Falta(name); return null; }
            Texture2D tex = null;
            if (EsNivel2) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Ani2 + name + ".png");   // animaciones de la base
            if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Ani + name + ".png");
            if (tex == null) { Falta(name); return null; }
            var sr = Poner(parent, name + "_f0", x, y, esc, orden, "abajo", flipX, flipY);
            if (sr == null) return null;
            sr.name = name;
            sr.gameObject.AddComponent<AnimacionHoja>().Configure(sr, tex, a.cols, a.cuadros, a.fw, a.fh, a.pad, fps > 0f ? fps : a.fps);
            return sr;
        }

        private static Light2D CrearLuz(string name, Vector3 pos, Color color, float intensity, float radius, bool pulso, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Light2D));
            go.transform.SetParent(parent != null ? parent : lucesRoot, false);
            go.transform.position = pos;
            var l = go.GetComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color; l.intensity = intensity;
            l.pointLightOuterRadius = radius; l.pointLightInnerRadius = radius * 0.2f;
            l.falloffIntensity = 0.7f;
            if (pulso) go.AddComponent<PulsoLuz>();
            return l;
        }

        // ───────────── jugabilidad ─────────────
        private static void CrearPeligro(Transform parent, Peligro z)
        {
            var go = new GameObject(z.regresa ? "Foso_Espinas" : "Peligro");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(z.x, z.y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(z.w, z.h);
            go.AddComponent<ContactHazard>();
            var hz = go.AddComponent<HazardZone>();
            SetField(hz, "respawnOnTouch", z.regresa);
            SetField(hz, "height", Mathf.Max(0.2f, z.h));
            SetField(hz, "sideInset", 0.1f);
        }

        private static void CrearRebote(Transform parent, Rebote r)
        {
            if (!ani.TryGetValue("hongo_rebote", out var a)) { Falta("hongo_rebote"); return; }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Ani + "hongo_rebote.png");
            float esc = r.esc > 0f ? r.esc : 1f;
            var go = new GameObject("Hongo_Rebote");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(r.x, r.y, 0f);
            var sr = Poner(go.transform, "hongo_rebote_f0", r.x, r.y - 0.08f, esc, 5, "abajo");
            float h = a.h * esc, w = a.w * esc;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(w * 0.8f, 0.75f);
            box.offset = new Vector2(0f, h * 0.78f - 0.375f + 0.1f);
            go.AddComponent<HongoRebote>().Configure(sr, tex, a.cols, a.cuadros, a.fw, a.fh, a.pad, r.fuerza > 0f ? r.fuerza : 21f);
        }

        private static void CrearSlime(Transform parent, Enemigo e)
        {
            bool naranja = e.tipo == "naranja";
            string anim = naranja ? "slime_naranja" : "slime_verde";
            if (!ani.TryGetValue(anim, out var a)) { Falta(anim); return; }
            var go = new GameObject(naranja ? "Slime_Naranja" : "Slime_Verde");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(e.x, e.y + 0.05f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.mass = naranja ? 1.4f : 1f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(a.w * 0.62f, a.h * 0.62f);
            box.offset = new Vector2(0f, a.h * 0.31f);
            var sr = Animado(go.transform, anim, e.x, e.y, 1f, 9, false, 0f);
            if (sr != null) sr.transform.localPosition = new Vector3(0f, a.h / 2f - 0.05f, 0f);
            SetField(go.AddComponent<EnemyHealth>(), "health", naranja ? 6 : 4);
            var fb = go.AddComponent<EnemyFeedback>();
            fb.Configure(sr);
            var hurt = new GameObject("Golpe");
            hurt.transform.SetParent(go.transform, false);
            var hb = hurt.AddComponent<BoxCollider2D>();
            hb.isTrigger = true;
            hb.size = new Vector2(a.w * 0.55f, a.h * 0.5f);
            hb.offset = new Vector2(0f, a.h * 0.3f);
            hurt.AddComponent<ContactHazard>();
            go.AddComponent<SlimeMusgo>().Configure(naranja ? SlimeMusgo.Tipo.Naranja : SlimeMusgo.Tipo.Verde, e.min, e.max, sr,
                sr != null ? sr.GetComponent<AnimacionHoja>() : null);
        }

        // Centinelas biomecánicos de NÉMESIS. Se arman igual que los enemigos de los otros niveles
        // (RobotPatrol / WaspFlyer); al cargar la escena Centinelas.cs los convierte en el
        // tricerátops terrestre o en los alados, con su IA, tamaño y vida.
        private const string SpriteTerrestre = "Assets/Resources/CentinelaTerrestre/01_Idle/Idle_01.png";
        private const string SpriteVolador = "Assets/Resources/Centinelas/AlaCian/Idle.png";

        private static Sprite PrimerSprite(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();

        private static void CrearCentinela(Transform parent, Enemigo e)
        {
            bool volador = e.tipo == "volador";
            var go = new GameObject(volador ? "Centinela_Volador" : "Robot_Centinela");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(e.x, volador ? e.y : e.y + 0.02f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = PrimerSprite(volador ? SpriteVolador : SpriteTerrestre);
            sr.sortingOrder = 8;
            if (sr.sprite == null) Debug.LogWarning("[Nivel 1] No encontré el sprite de los centinelas; se verán al darle Play.");
            if (volador)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                var circle = go.AddComponent<CircleCollider2D>();
                circle.isTrigger = true; circle.radius = 0.55f;
                SetField(go.AddComponent<EnemyHealth>(), "health", 5);
                go.AddComponent<ContactHazard>();
                go.AddComponent<EnemyFeedback>().Configure(sr);
                go.AddComponent<WaspFlyer>().Configure(sr.transform);
                return;
            }
            rb.gravityScale = 3f; rb.mass = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.9f, 1.5f);
            box.offset = new Vector2(0f, 0.75f);
            SetField(go.AddComponent<EnemyHealth>(), "health", 10);
            var hurt = new GameObject("Golpe");
            hurt.transform.SetParent(go.transform, false);
            var hb = hurt.AddComponent<BoxCollider2D>();
            hb.isTrigger = true; hb.size = box.size * 0.9f; hb.offset = box.offset;
            SetField(hurt.AddComponent<ContactHazard>(), "damage", 1);
            go.AddComponent<EnemyFeedback>().Configure(sr);
            go.AddComponent<RobotPatrol>().Configure(1 << Terrain, sr.transform, 1.8f, 3.4f);
        }

        // Cinemática del inicio (Kael de espaldas en el risco ve el destello en el valle). Si el arte no está, no se pone.
        private const string CinePath = "Assets/Cinematicas/Nivel1/";
        private static void CrearCinematica(Transform parent)
        {
            string[] nombres = { "vista_1_cielo", "vista_2_valle", "vista_3_copas", "vista_4_risco", "vista_5_frente",
                                 "kael_espalda", "destello" };
            var tex = new Texture2D[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
            {
                string path = CinePath + nombres[i] + ".png";
                if (AssetImporter.GetAtPath(path) is TextureImporter imp)
                {
                    bool cambia = imp.maxTextureSize < 4096 || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.CompressedHQ
                                  || imp.wrapMode != TextureWrapMode.Clamp;
                    if (cambia)
                    {
                        imp.maxTextureSize = 4096;
                        imp.mipmapEnabled = false;
                        imp.textureCompression = TextureImporterCompression.CompressedHQ;
                        imp.wrapMode = TextureWrapMode.Clamp;
                        imp.SaveAndReimport();
                    }
                }
                tex[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex[i] == null) { Debug.LogWarning("[Nivel 1] Falta " + path + ": no se agrega la cinemática del inicio."); return; }
            }
            var go = new GameObject("Cinematica_Inicio");
            go.transform.SetParent(parent, false);
            go.AddComponent<CinematicaBosque>().Configure(tex.Take(5).ToArray(), tex[5], tex[6]);
        }

        private static void CrearCheckpoint(Transform parent, Cp c)
        {
            var go = new GameObject("Checkpoint_" + c.nombre);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(c.x, c.y + 0.02f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(1.8f, 2.6f); box.offset = new Vector2(0f, 1.3f);
            Animado(go.transform, "flor_azul_1", c.x + 0.25f, c.y - 0.1f, 1.15f, 4, false, 0f);
            var lamp = CrearLuz("Luz_" + c.nombre, new Vector3(c.x, c.y + 2.2f, 0f), new Color(0.45f, 0.95f, 1f), 1.2f, 5f, false);
            go.AddComponent<JungleCheckpoint>().Configure(c.nombre, lamp);
        }

        private static void CrearObjeto(Transform parent, float x, float y, ItemKind kind, string id, string title, string text)
        {
            var go = new GameObject(kind == ItemKind.Fruit ? "Fruta_Selva" : "Chip_Memoria");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y + 0.6f, 0f);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.6f;
            go.AddComponent<ItemPickup>().Configure(kind, id, title, text);
        }

        private static void CrearOrbe(Transform parent, Punto o)
        {
            var go = new GameObject("Orbe_Eco");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.x, o.y, 0f);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.5f;
            var sr = Poner(go.transform, "orbe_eco", o.x, o.y, 0.5f, 6, "centro");
            go.AddComponent<EchoShard>().Configure(sr != null ? sr.transform : null);
            CrearLuz("Brillo_Orbe", go.transform.position, new Color(0.35f, 0.92f, 1f), 0.9f, 2.4f, true, go.transform);
        }

        private static void CrearLlave(Transform parent, Punto p)
        {
            var go = new GameObject("Llave_Base");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(p.x, p.y, 0f);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.8f;
            var sr = Poner(go.transform, "cristal_llave", p.x, p.y, 0.6f, 6, "centro");
            go.AddComponent<KeyPickup>().Configure(sr != null ? sr.transform : null);
        }

        private static void CrearMeta(Transform parent, Datos d)
        {
            var go = new GameObject("Meta_Nivel");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(d.meta.x, d.meta.y + 1.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(1.6f, 3f);
            var goal = go.AddComponent<LevelGoal>();
            SetField(goal, "levelTitle", d.completado);
            SetField(goal, "levelSubtitle", string.IsNullOrEmpty(d.subMeta) ? "Siguiente: Base de NÉMESIS" : d.subMeta);
            go.AddComponent<NextLevelExit>().Configure(d.siguiente, !d.metaSinLlave);
        }

        private static NpcGuardian.Hoja HojaOrin(string nombre, int cuadros, float fps)
        {
            string ruta = Ani + "orin_" + nombre + ".png";
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (textura == null) Debug.LogWarning("[Nivel 1] Falta " + ruta);
            return new NpcGuardian.Hoja
            {
                textura = textura, columnas = cuadros, cuadros = cuadros, ancho = 256, alto = 432, fps = fps
            };
        }

        private static void CrearNpc(Transform parent, Npc n)
        {
            var go = new GameObject("Orin");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(n.x, n.y, 0f);
            float esc = n.esc > 0f ? n.esc : 1f;
            float escala = esc * NpcGuardian.EscalaArte;
            float centro = n.y - 0.05f + NpcGuardian.AlturaPivoteArte * escala;
            var sr = Poner(go.transform, "orin_diseno_v2", n.x, centro, escala, 9, "centro", n.flipX);
            var glow = CrearLuz("Escaner", new Vector3(n.x + (n.flipX ? -0.43f : 0.43f), n.y + 1.3f, 0f),
                new Color(0.55f, 0.85f, 1f), 0.35f, 1.2f, true, go.transform);
            ReparacionKiro kiro = n.kiroX != 0f ? CrearKiroApagado(parent, n.kiroX, n.y) : null;
            go.AddComponent<NpcGuardian>().Configure(n.lineas ?? Array.Empty<string>(), n.radio > 0f ? n.radio : 6.5f, n.desaparece,
                sr, HojaOrin("quieto", 6, 5f), HojaOrin("caminar", 8, 8f), HojaOrin("hablar", 8, 10f), glow.transform, kiro);
        }

        // Kiro apagado junto a Orin + la reparación (animación Kael_Reparar_Kiro del proyecto)
        private const string HojaReparar = "Assets/Sprites/Personajes/KaelAnimaciones/Acciones/Kael_Reparar_Kiro_Reactivacion_06frames.png";
        private const string HojaChispas = "Assets/Resources/KaelVFX/kiro_reparacion.png";
        private static ReparacionKiro CrearKiroApagado(Transform parent, float x, float y)
        {
            var off = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Kiro/kiro_apagado.png");
            var on = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Kiro/kiro_despierto.png");
            var frames = AssetDatabase.LoadAllAssetsAtPath(HojaReparar).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            var chispas = AssetDatabase.LoadAssetAtPath<Texture2D>(HojaChispas);
            if (off == null || on == null || frames.Length < 6)
            {
                Debug.LogWarning("[Nivel 1] Faltan los sprites de Kiro o la hoja de reparación; Orin hablará sin la escena de Kiro.");
                return null;
            }
            var go = new GameObject("Kiro_Apagado");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var rep = go.AddComponent<ReparacionKiro>();
            rep.Configure(off, on, frames, chispas, 170f);
            return rep;
        }

        private static void CrearNarracion(Transform parent, Narr n)
        {
            var go = new GameObject("Narracion");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(n.x, n.y + n.alto / 2f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(Mathf.Max(1f, n.ancho), Mathf.Max(2f, n.alto));
            go.AddComponent<NarracionZona>().Configure(n.texto, n.dur > 0f ? n.dur : 4.5f);
        }

        // ───────────── cámara y cueva ─────────────
        private static void ConfigurarCamara(Camera cam, Datos d, Vector3 playerPos)
        {
            Vector2 min = new Vector2(HalfWidth, HalfHeight);
            Vector2 max = new Vector2(Mathf.Max(min.x, d.ancho - HalfWidth), Mathf.Max(min.y, d.alto - HalfHeight));
            cam.transform.position = new Vector3(Mathf.Clamp(playerPos.x, min.x, max.x), Mathf.Clamp(playerPos.y + 2f, min.y, max.y), -10f);
            if (cam.TryGetComponent(out CameraFollow2D follow))
            {
                follow.SetBounds(min, max);
                EditorUtility.SetDirty(follow);
            }
            EditorUtility.SetDirty(cam.gameObject);
        }

        private static void ConfigurarCueva(Camera cam, Datos d)
        {
            var fondo = GameObject.Find("Fondo_Parallax");
            if (fondo == null) return;
            var zonas = (d.cuevas ?? Array.Empty<Rango>()).Select(c => Rect.MinMaxRect(c.x0, c.y0, c.x1, c.y1)).ToArray();
            if (!fondo.TryGetComponent(out CaveAmbience ambiente)) ambiente = fondo.AddComponent<CaveAmbience>();
            ambiente.Configure(zonas, Array.Empty<SpriteRenderer>(), Array.Empty<SpriteRenderer>());
            EditorUtility.SetDirty(ambiente);
            if (!cam.TryGetComponent(out CaveDarkness darkness)) darkness = cam.gameObject.AddComponent<CaveDarkness>();
            // Cueva muy oscura: solo se ve lo que alumbra la linterna de Kiro (ver CaveDarkness)
            SetField(darkness, "maxDarkness", 0.93f);
            SetField(darkness, "fadeSeconds", 1.4f);
            SetField(darkness, "lightRadius", 3.2f);
            SetField(darkness, "darkRadius", 8.5f);
        }

        // ───────────── Nivel 2 ─────────────
        private const string Respaldo = "Assets/Scenes/Respaldo/Nivel_02_Templo_Nemesis_anterior.unity";

        /// <summary>
        /// El Nivel 2 se arma sobre una copia limpia de SampleScene (Kael, cámara, luz y fondo del bosque).
        /// Se copia el contenido del archivo y se conserva el .meta, así la escena sigue en la lista de Build.
        /// La primera vez guarda la escena vieja en Assets/Scenes/Respaldo.
        /// </summary>
        private static void RehacerEscenaDesdeBase()
        {
            if (File.Exists(ScenePath) && !File.Exists(Respaldo))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Respaldo));
                File.Copy(ScenePath, Respaldo);
                AssetDatabase.ImportAsset(Respaldo);
                Debug.Log("[Nivel 2] Escena anterior respaldada en " + Respaldo);
            }
            if (!File.Exists(BaseScene)) throw new IOException("Falta " + BaseScene);
            File.Copy(BaseScene, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }

        private static void CrearFondoBase(Transform parent, Rango f)
        {
            var sprite = Cargar("base_fondo");
            if (sprite == null) return;
            var go = new GameObject("Fondo_Base");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((f.x0 + f.x1) / 2f, (f.y0 + f.y1) / 2f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(f.x1 - f.x0, f.y1 - f.y0);
            sr.sortingOrder = -50;
            sr.color = new Color(0.85f, 0.9f, 1f, 1f);
            // orillas con degradado hacia el bosque
            go.AddComponent<DegradadoCueva>().Configure(f.x0, f.x1, f.y0, f.y1, new Color(0.06f, 0.08f, 0.11f, 1f), 6f, sr.sortingOrder);
        }

        // Barrera láser de la entrada de la base: se apaga con la llave del Nivel 1
        private static void CrearPuerta(Transform parent, Punto p)
        {
            const string carpeta = "Assets/Sprites/Laboratorio/Barrera/Piezas";
            var idle = Enumerable.Range(0, 18).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{carpeta}/barrier_idle/tile{i:000}.png")).Where(sp => sp != null).ToArray();
            var open = Enumerable.Range(0, 18).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{carpeta}/barrier_deactivate/tile{i:000}.png")).Where(sp => sp != null).ToArray();
            var go = new GameObject("Puerta_Base") { layer = Terrain };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(p.x, p.y + 2.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 5f);
            SpriteRenderer sr = null;
            if (idle.Length > 0)
            {
                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = idle[0];
                sr.sortingOrder = 3;
                vis.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            }
            else Debug.LogWarning("[Nivel 2] Faltan los sprites de la barrera en " + carpeta);
            go.AddComponent<LabDoor>().Configure(sr, box, idle, open);
            CrearLuz("Brillo_Puerta", go.transform.position, new Color(1f, 0.3f, 0.25f), 1.2f, 4f, true, go.transform);
        }

        // ───────────── utilidades ─────────────
        private static Transform Hijo(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[{Nombre}] {target.GetType().Name} no tiene el campo {field}"); return; }
            switch (value)
            {
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Object o: p.objectReferenceValue = o; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
