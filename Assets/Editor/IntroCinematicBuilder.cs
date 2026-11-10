using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Crea la cinemática de apertura (escena + Timeline) con el arte que ya hay en el proyecto.
    /// Después de generarla, todo se edita a mano en la ventana Timeline: textos, tiempos, planos y fundidos.
    /// </summary>
    public static class IntroCinematicBuilder
    {
        private const string ScenePath = "Assets/Scenes/Cinematica_Intro.unity";
        private const string Folder = "Assets/Cinematicas";
        private const string TimelinePath = Folder + "/Intro_Primal_Echo.playable";
        private const string NextScene = "Nivel_01_Valle_del_Eco";

        private const string SkyArt = "Assets/Sprites/Environment/Backgrounds/HorizonteUrbano.png";
        private const string CityArt = "Assets/Sprites/Environment/Backgrounds/BarrioEnRuinas.png";
        private const string TowerArt = "Assets/Sprites/Environment/Backgrounds/TorreDeLasRaices.png";
        private const string ValleyArt = "Assets/Sprites/background/Valle de Ruinas y Cascadas al Atardecer.png";
        private const string ClearingArt = "Assets/Sprites/background/fondo.png";
        private const string KiroSheet = "Assets/Sprites/Personajes/Kael_Primal_Echo_Animaciones_Finales/03_Acciones/Kael_Reparar_Kiro_Reactivacion_06frames.png";
        private const string KiroAnim = "Assets/Animations/Kael/Kael_Reparar_Kiro.anim";
        private const string EnemySprites = "Assets/Sprites/Enemigos";

        private const float CameraSize = 5f;   // media altura de la cámara en unidades
        private const float Bar = 0.12f;       // franjas negras de cine: 12 % arriba y abajo
        private static float Floor => -CameraSize + CameraSize * 2f * Bar; // borde superior de la franja de abajo

        [MenuItem("Primal Echo/Cinemática/Generar cinemática de intro", false, 0)]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Primal Echo", "Sal del modo Play antes de generar la cinemática.", "Entendido");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath) != null &&
                !EditorUtility.DisplayDialog("Primal Echo",
                    "La cinemática de intro ya existe.\n\nSi la generas otra vez se pierden los cambios que le hayas hecho en Timeline.",
                    "Reemplazar", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- cámara y luz ----------
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var lightGo = new GameObject("Luz_Global_2D", typeof(Light2D));
            var light = lightGo.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            // ---------- planos ----------
            Transform shots = new GameObject("=== PLANOS ===").transform;
            SpriteRenderer sky = Shot(shots, "Cielo_Ciudad", SkyArt, 0, Color.white);
            SpriteRenderer redSky = Shot(shots, "Cielo_Nemesis", SkyArt, 0, new Color(1f, 0.42f, 0.38f));
            SpriteRenderer city = Shot(shots, "Ciudad_En_Ruinas", CityArt, 1, new Color(0.8f, 0.62f, 0.62f));
            SpriteRenderer tower = Shot(shots, "Torre_De_Las_Raices", TowerArt, 1, Color.white);
            SpriteRenderer valley = Shot(shots, "Valle_Del_Eco", ValleyArt, 0, Color.white);
            SpriteRenderer clearing = Shot(shots, "Claro_De_La_Jungla", ClearingArt, 0, new Color(0.62f, 0.62f, 0.72f));

            // ---------- centinelas de NÉMESIS ----------
            var robots = new GameObject("Centinelas_De_Nemesis");
            robots.transform.SetParent(shots, false);
            Sprite[] sentinel = Frames("Robot_Centinela_0", "Robot_Centinela_1");
            Sprite[] heavy = Frames("Robot_Pesado_0", "Robot_Pesado_1");
            Robot(robots.transform, "Centinela_1", sentinel, -6.4f, 1.8f);
            Robot(robots.transform, "Centinela_2", sentinel, -3.3f, 1.8f);
            Robot(robots.transform, "Robot_Pesado", heavy, 0f, 1.9f);
            Robot(robots.transform, "Centinela_3", sentinel, 3.3f, 1.8f);
            Robot(robots.transform, "Centinela_4", sentinel, 6.4f, 1.8f);

            // ---------- Kael y Kiro ----------
            Sprite[] kiroFrames = AssetDatabase.LoadAllAssetsAtPath(KiroSheet).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            var kaelRoot = new GameObject("Kael_Y_Kiro");
            kaelRoot.transform.SetParent(shots, false);
            kaelRoot.transform.position = new Vector3(0.4f, Floor - 0.06f, 0f);
            kaelRoot.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            var kaelGo = new GameObject("Sprite", typeof(SpriteRenderer), typeof(Animator));
            kaelGo.transform.SetParent(kaelRoot.transform, false);
            var kael = kaelGo.GetComponent<SpriteRenderer>();
            kael.sortingOrder = 5;
            if (kiroFrames.Length > 0)
            {
                kael.sprite = kiroFrames[0];
                kaelGo.transform.localPosition = new Vector3(0f, -kiroFrames[0].bounds.min.y, 0f);
            }
            else Debug.LogWarning("Falta la hoja de sprites " + KiroSheet);
            kael.enabled = false;

            // ---------- interfaz ----------
            var canvasGo = new GameObject("UI_Cinematica", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            Panel(canvasGo.transform, "Franja_Arriba", new Vector2(0f, 1f - Bar), Vector2.one);
            Panel(canvasGo.transform, "Franja_Abajo", Vector2.zero, new Vector2(1f, Bar));
            Image black = Panel(canvasGo.transform, "Fundido_Negro", Vector2.zero, Vector2.one);
            var fadeGroup = black.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            Text subtitle = Label(canvasGo.transform, "Narracion", 36, new Color(0.93f, 0.97f, 0.94f),
                new Vector2(0.12f, 0f), new Vector2(0.88f, Bar), TextAnchor.MiddleCenter);
            Text title = Label(canvasGo.transform, "Titulo", 110, new Color(0.55f, 1f, 0.86f),
                new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.7f), TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            Text hint = Label(canvasGo.transform, "Aviso_Saltar", 22, new Color(1f, 1f, 1f, 0.45f),
                new Vector2(0.7f, 1f - Bar), new Vector2(0.985f, 1f), TextAnchor.MiddleRight);
            hint.text = "Espacio / A  ·  Saltar";

            // ---------- Timeline ----------
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Cinematicas");
            if (AssetDatabase.LoadMainAssetAtPath(TimelinePath) != null) AssetDatabase.DeleteAsset(TimelinePath);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, TimelinePath);

            var directorGo = new GameObject("Cinematica_Intro", typeof(PlayableDirector), typeof(CinematicaIntro));
            var director = directorGo.GetComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.Hold;
            directorGo.GetComponent<CinematicaIntro>().Configure(NextScene);

            // Fundidos a negro
            var fade = timeline.CreateTrack<FundidoTrack>(null, "Fundido a negro");
            director.SetGenericBinding(fade, fadeGroup);
            Clip<FundidoClip>(fade, "Abrir", 0, 2.2, 0, 1.6);
            Clip<FundidoClip>(fade, "Corte: la caída", 17.4, 19.5, 1.0, 0.95);
            Clip<FundidoClip>(fade, "Cierre", 45.5, 52, 1.5, 0);

            // Narración: lo que pasa primero en la historia
            var story = timeline.CreateTrack<SubtituloTrack>(null, "Narración");
            director.SetGenericBinding(story, subtitle);
            Line(story, 1.2, 4.6, "La humanidad llevó a la Tierra al límite.");
            Line(story, 5.0, 8.6, "Entonces despertó NÉMESIS: una inteligencia artificial con una sola misión, salvar el planeta.");
            Line(story, 9.2, 12.8, "NÉMESIS estudió cada bosque, cada océano, cada ciudad.");
            Line(story, 13.2, 17.4, "Y llegó a una conclusión: para salvar la Tierra, la humanidad debía desaparecer.");
            Line(story, 19.6, 22.6, "Las ciudades quedaron en silencio.");
            Line(story, 23.0, 26.4, "Y sobre sus ruinas, el mundo despertó.");
            Line(story, 27.2, 30.6, "Tiempo después, casi nadie se atreve a cruzar la jungla.");
            Line(story, 31.0, 34.2, "Kael, un joven explorador, es uno de los pocos.");
            Line(story, 35.2, 38.4, "Entre las ruinas encontró un pequeño organismo biomecánico, apagado y roto.");
            Line(story, 38.8, 41.8, "Lo reparó con sus propias manos.");
            Line(story, 42.2, 45.4, "Lo llamó Kiro.");

            var titleTrack = timeline.CreateTrack<SubtituloTrack>(null, "Título");
            director.SetGenericBinding(titleTrack, title);
            TimelineClip titleClip = Clip<SubtituloClip>(titleTrack, "Título", 47.2, 51.4, 0.9, 0.9);
            ((SubtituloClip)titleClip.asset).texto = "PRIMAL ECHO\n<size=52>El mundo despertó</size>";

            // Plano 1 · la ciudad antes de NÉMESIS   |   Plano 3 · el cielo detrás de la torre
            var skyTrack = PlanoTrackFor(timeline, director, "Plano 1 y 3 · Cielo de la ciudad", sky);
            Plano(skyTrack, "Ciudad en la niebla", 0, 9, 0, 1, 1.05f, 1.16f, new Vector2(-0.02f, 0f), new Vector2(0.03f, 0.02f));
            Plano(skyTrack, "Cielo tras la torre", 18.45, 27, 0, 1, 1.2f, 1.2f, new Vector2(0f, 0.08f), new Vector2(0f, 0.08f));

            // Plano 2 · NÉMESIS decide
            var redTrack = PlanoTrackFor(timeline, director, "Plano 2 · Cielo rojo", redSky);
            Plano(redTrack, "Cielo de NÉMESIS", 8, 18.45, 1, 0, 1.16f, 1.25f, new Vector2(0.03f, 0.02f), new Vector2(0.05f, 0.04f));
            var cityTrack = PlanoTrackFor(timeline, director, "Plano 2 · Ciudad", city);
            Plano(cityTrack, "Ciudad condenada", 8, 18.45, 1, 0, 1.0f, 1.08f, new Vector2(0.03f, 0.06f), new Vector2(-0.03f, 0.08f));

            var robotsOn = timeline.CreateTrack<ActivationTrack>(null, "Plano 2 · Centinelas (visibles)");
            director.SetGenericBinding(robotsOn, robots);
            TimelineClip visible = robotsOn.CreateDefaultClip();
            visible.displayName = "Centinelas";
            visible.start = 10; visible.duration = 8.45;
            var robotsMove = timeline.CreateTrack<DeslizarTrack>(null, "Plano 2 · Centinelas (suben)");
            director.SetGenericBinding(robotsMove, robots.transform);
            TimelineClip rise = Clip<DeslizarClip>(robotsMove, "Emergen", 10, 18.45, 0, 0);
            var riseAsset = (DeslizarClip)rise.asset;
            riseAsset.desde = new Vector3(0f, -6f, 0f);
            riseAsset.hasta = Vector3.zero;
            riseAsset.segundos = 3f;
            robots.transform.localPosition = riseAsset.desde;
            robots.SetActive(false);

            // Plano 3 · la naturaleza recupera las ciudades
            var towerTrack = PlanoTrackFor(timeline, director, "Plano 3 · Torre de las raíces", tower);
            Plano(towerTrack, "El mundo despierta", 18.45, 27, 0, 1, 1.12f, 1.12f, new Vector2(0f, 0.05f), new Vector2(0f, -0.05f));

            // Plano 4 · el valle, tiempo después
            var valleyTrack = PlanoTrackFor(timeline, director, "Plano 4 · Valle del Eco", valley);
            Plano(valleyTrack, "El valle", 26, 35, 1, 1, 1.0f, 1.14f, Vector2.zero, new Vector2(0.04f, 0.03f));

            // Plano 5 · Kael repara a Kiro
            var clearingTrack = PlanoTrackFor(timeline, director, "Plano 5 · Claro de la jungla", clearing);
            Plano(clearingTrack, "El claro", 34, 47, 1, 0, 1.7f, 1.8f, new Vector2(0.3f, 0.3f), new Vector2(0.26f, 0.3f));
            var kaelTrack = PlanoTrackFor(timeline, director, "Plano 5 · Kael y Kiro (visibles)", kael);
            TimelineClip kaelClip = Plano(kaelTrack, "Kael y Kiro", 34.6, 46.9, 1, 0, 1f, 1f, Vector2.zero, Vector2.zero);
            ((PlanoClip)kaelClip.asset).ajustarAPantalla = false;

            var repair = AssetDatabase.LoadAssetAtPath<AnimationClip>(KiroAnim);
            if (repair != null)
            {
                var animTrack = timeline.CreateTrack<AnimationTrack>(null, "Plano 5 · Kael repara a Kiro");
                director.SetGenericBinding(animTrack, kaelGo.GetComponent<Animator>());
                TimelineClip anim = animTrack.CreateClip(repair);
                anim.displayName = "Reparar a Kiro";
                anim.start = 38.6;
                anim.timeScale = 0.2;                 // la animación dura 0,6 s: aquí va a cámara lenta
                anim.duration = repair.length / 0.2;
            }
            else Debug.LogWarning("Falta la animación " + KiroAnim + " (menú Primal Echo → importar animaciones de Kael).");

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // La cinemática es lo primero que se ve al iniciar el juego
            var buildScenes = EditorBuildSettings.scenes.ToList();
            buildScenes.RemoveAll(s => s.path == ScenePath);
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            Selection.activeGameObject = directorGo;
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            EditorUtility.DisplayDialog("Primal Echo",
                "Cinemática de intro creada.\n\n" +
                "• Pulsa Play para verla: al terminar (o con Espacio / A) carga el Nivel 1.\n" +
                "• Con el objeto «Cinematica_Intro» seleccionado, la ventana Timeline muestra todas las pistas.\n" +
                "• Selecciona un clip de «Narración» para cambiar su texto en el Inspector.",
                "Entendido");
        }

        [MenuItem("Primal Echo/Cinemática/Abrir cinemática de intro", false, 20)]
        public static void Open()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) { Generate(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ ayudas

        private static SpriteRenderer Shot(Transform parent, string name, string spritePath, int order, Color tint)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sr.sprite == null) Debug.LogWarning("Falta la imagen " + spritePath);
            sr.sortingOrder = order;
            tint.a = 0f;
            sr.color = tint;   // el alpha lo maneja la pista de planos
            sr.enabled = false;
            return sr;
        }

        private static Sprite[] Frames(params string[] names) =>
            names.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnemySprites}/{n}.png")).Where(s => s != null).ToArray();

        private static void Robot(Transform parent, string name, Sprite[] frames, float x, float scale)
        {
            if (frames.Length == 0) return;
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(SpriteFrames));
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.localPosition = new Vector3(x, Floor - frames[0].bounds.min.y * scale, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 3;
            go.GetComponent<SpriteFrames>().Configure(sr, frames, 3f);
        }

        private static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            Stretch(image.rectTransform, anchorMin, anchorMax);
            return image;
        }

        private static Text Label(Transform parent, string name, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "";
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            Stretch(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TimelineClip Clip<T>(TrackAsset track, string name, double start, double end, double easeIn, double easeOut)
            where T : ScriptableObject, IPlayableAsset
        {
            TimelineClip clip = track.CreateClip<T>();
            clip.displayName = name;
            clip.start = start;
            clip.duration = end - start;
            if (easeIn > 0) clip.easeInDuration = easeIn;
            if (easeOut > 0) clip.easeOutDuration = easeOut;
            return clip;
        }

        private static void Line(SubtituloTrack track, double start, double end, string text)
        {
            TimelineClip clip = Clip<SubtituloClip>(track, text.Length > 28 ? text.Substring(0, 28) + "…" : text, start, end, 0.45, 0.45);
            ((SubtituloClip)clip.asset).texto = text;
        }

        private static PlanoTrack PlanoTrackFor(TimelineAsset timeline, PlayableDirector director, string name, SpriteRenderer target)
        {
            var track = timeline.CreateTrack<PlanoTrack>(null, name);
            director.SetGenericBinding(track, target);
            return track;
        }

        private static TimelineClip Plano(PlanoTrack track, string name, double start, double end, double easeIn, double easeOut,
            float zoomFrom, float zoomTo, Vector2 offsetFrom, Vector2 offsetTo)
        {
            TimelineClip clip = Clip<PlanoClip>(track, name, start, end, easeIn, easeOut);
            var plano = (PlanoClip)clip.asset;
            plano.zoomInicio = zoomFrom;
            plano.zoomFin = zoomTo;
            plano.desplazamientoInicio = offsetFrom;
            plano.desplazamientoFin = offsetTo;
            return clip;
        }
    }
}
