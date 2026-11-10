using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Construye Nivel 1 y Nivel 2 desde los mapas de texto de Assets/Niveles usando la Tile Palette de Jungla.
    /// Copia SampleScene (Kael, cámara, parallax y luces) y repinta los tilemaps. SampleScene no se modifica.
    /// </summary>
    [InitializeOnLoad]
    public static class JungleLevelBuilder
    {
        // Los niveles YA NO se regeneran solos. Al guardar (Ctrl+S) un nivel, sus colisiones
        // se recalculan a partir de lo que esté pintado en las capas "mapa" y "Ramas_Un_Sentido".
        static JungleLevelBuilder()
        {
            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                if (generating || !Levels.Any(l => ScenePath(l.scene) == path)) return;
                ApplyCaveBackdrop(scene, quiet: false); // recalcula el fondo de cueva con lo que hayas pintado
                RebuildCollisionIn(scene);
            };
            // Después de compilar: si el nivel abierto tiene árboles o fondo de una versión anterior, se actualizan
            // (no regenera nada: solo árboles y fondo de cueva; tus cambios se quedan)
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var active = SceneManager.GetActiveScene();
                if (!Levels.Any(l => l.scene == active.name)) return;
                bool door = OpenCaveDoor(active);
                if (ApplyCaveBackdrop(active, quiet: !door) || door)
                {
                    RebuildCollisionIn(active);
                    EditorSceneManager.MarkSceneDirty(active);
                }
            };
            // Al abrir un nivel, si todavía tiene el relleno negro de tiles en las cuevas, se cambia por el parallax
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                if (generating || !Levels.Any(l => l.scene == scene.name)) return;
                bool door = OpenCaveDoor(scene);
                if (ApplyCaveBackdrop(scene, quiet: !door) || door)
                {
                    RebuildCollisionIn(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            };
        }

        private static bool generating;

        private const string MapsFolder = "Assets/Niveles";
        private const string ScenesFolder = "Assets/Scenes";
        private const string BaseScene = "Assets/Scenes/SampleScene.unity";
        private const string TilesFolder = "Assets/Tiles/Jungle";
        private const string EnemySprites = "Assets/Sprites/Enemigos";
        private const int TerrainLayer = 6;
        private const float HalfWidth = 684f / 2f / 32f, HalfHeight = 384f / 2f / 32f;

        private static readonly (string map, string scene)[] Levels =
        {
            ("Nivel_01_Valle_del_Eco.txt", "Nivel_01_Valle_del_Eco"),
            ("Nivel_02_Templo_Nemesis.txt", "Nivel_02_Templo_Nemesis"),
        };

        private static string ScenePath(string scene) => $"{ScenesFolder}/{scene}.unity";

        [MenuItem("Primal Echo/Niveles/Generar Nivel 1 y Nivel 2", false, 0)]
        public static void GenerateAll() => GenerateAll(askFirst: true);

        private static void GenerateAll(bool askFirst)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal del modo Play primero."); return; }
            if (!File.Exists(BaseScene)) { Debug.LogError("No encuentro " + BaseScene); return; }
            if (askFirst && Levels.Any(l => File.Exists(ScenePath(l.scene))) &&
                !EditorUtility.DisplayDialog("Primal Echo",
                    "Se regenerarán Nivel 1 y Nivel 2 desde los mapas de texto.\n\n" +
                    "Lo que pintaste o borraste en las capas de tiles SE CONSERVA y se vuelve a aplicar encima.\n" +
                    "Enemigos y objetos vuelven a la posición del mapa de texto.", "Regenerar", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // no borrar una escena abierta
            try
            {
                generating = true;
                foreach (var level in Levels) Build(level.map, level.scene);
                var ours = Levels.Select(l => new EditorBuildSettingsScene(ScenePath(l.scene), true)).ToList();
                var paths = new HashSet<string>(ours.Select(s => s.path));
                EditorBuildSettings.scenes = ours.Concat(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path))).ToArray();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Primal Echo", "No se pudo generar el nivel:\n" + e.Message, "OK");
                return;
            }
            finally { generating = false; EditorUtility.ClearProgressBar(); }
            EditorSceneManager.OpenScene(ScenePath(Levels[0].scene), OpenSceneMode.Single);
            Debug.Log("<color=#4CAF50><b>[Primal Echo]</b> Nivel 1 y Nivel 2 generados. Abre Nivel_01_Valle_del_Eco y pulsa Play.</color>");
        }

        [MenuItem("Primal Echo/Niveles/Actualizar colisiones del nivel abierto", false, 10)]
        public static void RebuildCollisionMenu()
        {
            var scene = SceneManager.GetActiveScene();
            RebuildCollisionIn(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Primal Echo/Niveles/Aplicar fondo parallax de cueva al nivel abierto", false, 11)]
        public static void ApplyCaveBackdropMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (!Levels.Any(l => l.scene == scene.name)) { EditorUtility.DisplayDialog("Primal Echo", "Abre primero Nivel_01 o Nivel_02.", "OK"); return; }
            ApplyCaveBackdrop(scene, quiet: false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Primal Echo] Fondo de cueva aplicado. Guarda la escena (Ctrl+S).");
        }

        // Devuelve true si cambió algo.
        private static bool ApplyCaveBackdrop(Scene scene, bool quiet)
        {
            var level = Levels.FirstOrDefault(l => l.scene == scene.name);
            if (level.map == null) return false;
            tiles = new Dictionary<string, Tile>();
            foreach (string g in AssetDatabase.FindAssets("t:Tile", new[] { TilesFolder }))
            {
                var t = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g));
                if (t != null) tiles[t.name] = t;
            }
            var grid = scene.GetRootGameObjects().FirstOrDefault(rootGo => rootGo.name == "Grid");
            var bg = grid != null ? grid.transform.Find("Fondo_Cueva_Y_Arboles")?.GetComponent<Tilemap>() : null;
            var group = scene.GetRootGameObjects().SelectMany(rootGo => rootGo.GetComponentsInChildren<Transform>(true)).FirstOrDefault(tr => tr.name == "Fondo_Parallax");
            Map map = Parse($"{MapsFolder}/{level.map}");
            bool jungleChanged = false;
            if (group != null && group.Find(JungleVersion) == null)
            {
                BuildJungleParallax(group, map.Theme);
                jungleChanged = true;
            }
            Tile fill = T("tile_jungle_wall_brown_r0_c2");
            bool hasBlack = false;
            if (bg != null && fill != null)
                for (int y = 0; y < map.H && !hasBlack; y++)
                    for (int x = 0; x < map.W; x++)
                        if (Cell(map, x, y) == 0 && IsCaveBack(map, x, y) && bg.GetTile(new Vector3Int(x, y, 0)) == fill) { hasBlack = true; break; }
            bool hasCave = group != null && group.Find("Cueva_Lejos") != null && group.GetComponent<CaveAmbience>() != null
                && scene.GetRootGameObjects().Any(rootGo => rootGo.name == "Mascara_Cuevas" && rootGo.transform.Find("_v3") != null);
            bool treesChanged = SetupJungleParticles();
            if (grid != null && grid.transform.Find(TreesVersion) == null)
            {
                PlantTrees(map, grid.transform, bg);
                treesChanged = true;
            }
            if (quiet && !hasBlack && hasCave && !jungleChanged) return treesChanged;
            SetupCaveBackdrop(map, bg);
            Debug.Log($"[Primal Echo] {scene.name}: fondo de cueva parallax aplicado (se quitó el relleno negro de tiles).");
            return true;
        }

        // Rediseño de la entrada de la cueva del Nivel 2: boca de roca estilo templo sobre el pozo.
        // Limpia la zona (x 18–62, y 30–40) en las capas de tiles y la vuelve a construir. Solo se hace una vez.
        private static bool OpenCaveDoor(Scene scene)
        {
            if (scene.name != "Nivel_02_Templo_Nemesis") return false;
            var grid = scene.GetRootGameObjects().FirstOrDefault(rootGo => rootGo.name == "Grid");
            if (grid == null || grid.transform.Find("Entrada_Cueva_v2") != null) return false;
            var solid = grid.transform.Find("mapa")?.GetComponent<Tilemap>();
            var bg = grid.transform.Find("Fondo_Cueva_Y_Arboles")?.GetComponent<Tilemap>();
            var deco = grid.transform.Find("Decoracion_Sin_Colision")?.GetComponent<Tilemap>();
            if (solid == null || bg == null || deco == null) return false;
            tiles = new Dictionary<string, Tile>();
            foreach (string g in AssetDatabase.FindAssets("t:Tile", new[] { TilesFolder }))
            {
                var t = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g));
                if (t != null) tiles[t.name] = t;
            }
            const int X0 = 18, X1 = 62, Y0 = 30, Y1 = 40;
            // 1) Limpiar lo pintado en la zona (casita, columna y techo)
            for (int x = X0; x <= X1; x++)
                for (int y = Y0; y <= Y1; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (x >= 56 && x <= 57 && y < Y1) continue; // pared derecha del pozo (del mapa)
                    solid.SetTile(pos, null); bg.SetTile(pos, null); deco.SetTile(pos, null);
                }
            // 2) Losa de roca sobre el pozo con estalactitas, y un saliente hacia la entrada
            var rock = new HashSet<Vector2Int>();
            void AddRect(int ax, int ay, int bx2, int by2) { for (int x = ax; x <= bx2; x++) for (int y = ay; y <= by2; y++) rock.Add(new Vector2Int(x, y)); }
            AddRect(29, 36, 57, 39);              // losa
            AddRect(27, 38, 28, 39);              // saliente sobre la columna
            foreach (var (sx, len) in new[] { (33, 1), (34, 2), (35, 1), (41, 1), (46, 1), (47, 2), (48, 1), (53, 1) })
                for (int k = 1; k <= len; k++) rock.Add(new Vector2Int(sx, 36 - k)); // estalactitas
            foreach (var c in rock) solid.SetTile(new Vector3Int(c.x, c.y, 0), T("tile_jungle_wall_brown_r0_c2"));
            // 3) Autotiling de la zona con piedra gris de ruinas
            bool Sol(int x, int y) => solid.HasTile(new Vector3Int(x, y, 0));
            Tile fill = T("tile_jungle_wall_brown_r0_c2");
            for (int x = X0 - 1; x <= X1 + 1; x++)
                for (int y = Y0 - 2; y <= Y1; y++)
                {
                    if (!Sol(x, y)) continue;
                    bool up = Sol(x, y + 1) || y >= Y1, down = Sol(x, y - 1), left = Sol(x - 1, y), right = Sol(x + 1, y);
                    if (!rock.Contains(new Vector2Int(x, y)) && up && down && left && right) continue; // no tocar el interior del terreno
                    Tile chosen = fill;
                    if (!up)
                    {
                        int row = Mathf.Abs(x) % 3 == 0 ? 3 : 1, col = !left ? 0 : (!right ? 2 : 1);
                        chosen = T($"tile_jungle_ground_grey_r{row}_c{col}") ?? T($"tile_jungle_ground_grey_r1_c{col}");
                        var cap = T($"tile_jungle_ground_grey_r{row - 1}_c{col}");
                        if (cap != null && !Sol(x, y + 1)) deco.SetTile(new Vector3Int(x, y + 1, 0), cap);
                    }
                    else if (!down) chosen = !left ? T("tile_jungle_bottom_grey_r0_c0") : (!right ? T("tile_jungle_bottom_grey_r0_c3") : T($"tile_jungle_bottom_grey_r0_c{x % 2 + 1}"));
                    else if (!left) chosen = T($"tile_jungle_wall_grey_r{y % 4}_c0") ?? fill;
                    else if (!right) chosen = T($"tile_jungle_wall_grey_r{y % 4}_c1") ?? fill;
                    solid.SetTile(new Vector3Int(x, y, 0), GridTile(chosen ?? fill));
                }
            // 4) Columna de templo (fondo, Kael pasa por delante) que sostiene el saliente
            for (int y = 30; y <= 37; y++)
            {
                bg.SetTile(new Vector3Int(27, y, 0), T($"tile_jungle_wall_grey_r{y % 4}_c0"));
                bg.SetTile(new Vector3Int(28, y, 0), T($"tile_jungle_wall_grey_r{y % 4}_c1"));
            }
            // 5) Farola junto a la entrada y lianas colgando de la losa
            Stamp(deco, "tile_jungle_plants_objects", 24, 30, 10, 11, 5, 5);
            Tile vine = T("tile_jungle_bg_vines_r6_c6") ?? T("tile_jungle_bg_vines_r4_c0");
            if (vine != null)
                foreach (var (vx, len) in new[] { (31, 3), (38, 4), (44, 2), (51, 3) })
                    for (int k = 0; k < len; k++)
                        if (!Sol(vx, 35 - k)) bg.SetTile(new Vector3Int(vx, 35 - k, 0), vine);
            // Quitar el marcador del arreglo anterior si existía
            var oldMarker = grid.transform.Find("Puerta_Cueva_Abierta");
            if (oldMarker != null) Object.DestroyImmediate(oldMarker.gameObject);
            new GameObject("Entrada_Cueva_v2").transform.SetParent(grid.transform, false);
            Debug.Log("[Primal Echo] Entrada de la cueva del Nivel 2 rediseñada (boca de roca con columna de templo).");
            return true;
        }

        [MenuItem("Primal Echo/Niveles/Abrir Nivel 1", false, 20)]
        public static void Open1() => OpenLevel(0);

        [MenuItem("Primal Echo/Niveles/Abrir Nivel 2", false, 21)]
        public static void Open2() => OpenLevel(1);

        private static void OpenLevel(int i)
        {
            if (!File.Exists(ScenePath(Levels[i].scene))) { GenerateAll(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath(Levels[i].scene), OpenSceneMode.Single);
        }

        // ───────────── Mapa de texto ─────────────

        private sealed class Map
        {
            public string Title = "", Subtitle = "", Completed = "", Next = "", Theme = "jungla";
            public readonly List<string> Checkpoints = new List<string>();
            public readonly List<(float x, float y, string text)> Signs = new List<(float, float, string)>();
            public char[,] C;
            public int W, H;
            public char At(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? ' ' : C[x, y];
            public IEnumerable<Vector2Int> Find(char c)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        if (C[x, y] == c) yield return new Vector2Int(x, y);
            }
        }

        private static Map Parse(string path)
        {
            string[] lines = File.ReadAllText(path).Replace("\r", "").Split('\n');
            int sep = Array.IndexOf(lines, "---");
            if (sep < 0) throw new FormatException(path + ": falta '---'.");
            var m = new Map();
            for (int i = 0; i < sep; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("@")) continue;
                int sp = line.IndexOf(' ');
                string key = sp < 0 ? line.Substring(1) : line.Substring(1, sp - 1);
                string val = sp < 0 ? "" : line.Substring(sp + 1).Replace("\\n", "\n");
                switch (key)
                {
                    case "titulo": m.Title = val; break;
                    case "subtitulo": m.Subtitle = val; break;
                    case "completado": m.Completed = val; break;
                    case "siguiente": m.Next = val; break;
                    case "tema": m.Theme = val; break;
                    case "checkpoint": m.Checkpoints.Add(val); break;
                    case "cartel":
                        var p = val.Split(new[] { ' ' }, 3);
                        m.Signs.Add((float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture), p[2]));
                        break;
                }
            }
            var rows = lines.Skip(sep + 1).Where(l => l.Length > 0).ToList();
            m.H = rows.Count; m.W = rows.Max(r => r.Length);
            m.C = new char[m.W, m.H];
            for (int r = 0; r < rows.Count; r++)
                for (int x = 0; x < m.W; x++)
                    m.C[x, m.H - 1 - r] = x < rows[r].Length ? rows[r][x] : ' ';
            if (!m.Find('P').Any() || !m.Find('X').Any()) throw new FormatException(path + ": falta 'P' o 'X'.");
            return m;
        }

        // ───────────── Construcción ─────────────

        private static Dictionary<string, Tile> tiles;
        private static Tile T(string name) => tiles.TryGetValue(name, out var t) ? t : null;
        private const string SolidTilesFolder = "Assets/Tiles/NivelesSolidos";
        private static readonly Dictionary<Tile, Tile> gridTiles = new Dictionary<Tile, Tile>();

        // Copia del tile con colisión de cuadro completo: suelo plano y paredes rectas (como Hollow Knight).
        // No modifica los tiles de tu Tile Palette.
        private static Tile GridTile(Tile source)
        {
            if (source == null) return null;
            if (gridTiles.TryGetValue(source, out var cached) && cached != null) return cached;
            if (!AssetDatabase.IsValidFolder(SolidTilesFolder)) AssetDatabase.CreateFolder("Assets/Tiles", "NivelesSolidos");
            string path = $"{SolidTilesFolder}/{source.name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = source.sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            gridTiles[source] = tile;
            return tile;
        }
        private static Sprite S(string tileName) => T(tileName) != null ? T(tileName).sprite : null;
        private static int Hash(int x, int y) => Mathf.Abs(x * 73856093 ^ y * 19349663);

        private static void Build(string mapFile, string sceneName)
        {
            EditorUtility.DisplayProgressBar("Primal Echo", "Construyendo " + sceneName, 0.2f);
            Map map = Parse($"{MapsFolder}/{mapFile}");
            tiles = new Dictionary<string, Tile>();
            foreach (string g in AssetDatabase.FindAssets("t:Tile", new[] { TilesFolder }))
            {
                var t = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g));
                if (t != null) tiles[t.name] = t;
            }
            if (T("tile_jungle_ground_brown_r1_c1") == null)
                throw new InvalidOperationException("No encuentro los tiles de jungla. Usa primero 'Primal Echo → Construir Paso del Rey' para cortarlos.");
            var art = LoadEnemyArt();

            string path = ScenePath(sceneName);
            List<TileEdit> edits = CaptureUserEdits(sceneName);
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(BaseScene, path)) throw new IOException("No se pudo copiar SampleScene a " + path);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("SampleScene no tiene a Kael (Player).");
            GameObject gridObj = GameObject.Find("Grid");
            if (gridObj == null) throw new InvalidOperationException("SampleScene no tiene Grid.");

            // Limpiar lo que dependía del mapa anterior
            foreach (var room in Object.FindObjectsByType<CameraRoom2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(room.gameObject);
            var oldRoot = GameObject.Find("Jugabilidad");
            if (oldRoot) Object.DestroyImmediate(oldRoot);
            var oldSigns = GameObject.Find("Carteles");
            if (oldSigns) Object.DestroyImmediate(oldSigns);

            Tilemap bg = Layer(gridObj, "Fondo_Cueva_Y_Arboles", -10, false);
            Tilemap solid = Layer(gridObj, "mapa", 0, false);
            Tilemap deco = Layer(gridObj, "Decoracion_Sin_Colision", 5, false);
            Tilemap branches = Layer(gridObj, "Ramas_Un_Sentido", 1, false);

            PaintTerrain(map, solid, deco, bg);
            PaintBranches(map, branches, deco);
            PaintDecoration(map, deco, bg);
            var layers = new Dictionary<string, Tilemap>
            {
                { bg.name, bg }, { solid.name, solid }, { deco.name, deco }, { branches.name, branches },
            };
            SaveBaseline(sceneName, layers);       // lo que generó la máquina
            ApplyUserEdits(edits, layers);         // + lo que tú cambiaste
            BuildCollisionFromTiles(gridObj.transform, solid, branches);
            var jungleGroup = GameObject.Find("Fondo_Parallax");
            if (jungleGroup != null) BuildJungleParallax(jungleGroup.transform, map.Theme);
            SetupCaveBackdrop(map, bg);
            PlantTrees(map, gridObj.transform, bg);
            AttachHandPainted(sceneName, gridObj.transform);

            Transform root = new GameObject("Jugabilidad").transform;
            Transform lights = (GameObject.Find("Iluminacion") ?? new GameObject("Iluminacion")).transform;
            for (int i = lights.childCount - 1; i >= 0; i--)
                if (lights.GetChild(i).name != "Luz_Global_2D") Object.DestroyImmediate(lights.GetChild(i).gameObject);

            // Kael
            Vector2Int start = map.Find('P').First();
            player.transform.position = new Vector3(start.x + 0.5f, start.y, 0f);
            var respawn = GetOrAdd<KaelRespawn>(player);
            SetField(respawn, "startName", "Inicio");
            EditorUtility.SetDirty(player);

            SetupCamera(map, player.transform.position);
            SetupJungleParticles();

            // Juego
            var enemies = new List<(Vector2Int cell, GameObject go)>();
            foreach (var c in map.Find('E')) enemies.Add((c, Robot(root, c, art, false)));
            foreach (var c in map.Find('B')) enemies.Add((c, Robot(root, c, art, true)));
            foreach (var c in map.Find('W')) enemies.Add((c, Wasp(root, c, art)));
            foreach (var c in map.Find('K')) enemies.Add((c, Plant(root, c, art)));

            var cps = map.Find('C').OrderBy(c => c.x).ToList();
            for (int i = 0; i < cps.Count; i++)
                Checkpoint(root, deco, lights, cps[i], i < map.Checkpoints.Count ? map.Checkpoints[i] : "Checkpoint " + (i + 1));

            Runs(map, '^', (x0, len, y) => Thorns(root, deco, x0, len, y));
            Runs(map, '%', (x0, len, y) => Crumble(root, x0, len, y));
            Runs(map, 'J', (x0, len, y) => Geyser(root, lights, x0, len, y));
            int shards = 0;
            foreach (var c in map.Find('o')) { Shard(root, lights, c, art.shard); shards++; }
            Arena(map, root, enemies);
            Goal(map, lights);
            Signs(map);

            var hud = new GameObject("HUD_Nivel").AddComponent<LevelHUD>();
            hud.transform.SetParent(root);
            hud.Configure(map.Title, map.Subtitle, shards);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Primal Echo] {sceneName}: {map.W}×{map.H} tiles, {enemies.Count} enemigos, {cps.Count} checkpoints, {shards} fragmentos.");
        }

        // En Unity, GetComponent() devuelve un "null falso" que el operador ?? no detecta.
        private static TC GetOrAdd<TC>(GameObject go) where TC : Component
        {
            return go.TryGetComponent(out TC existing) ? existing : go.AddComponent<TC>();
        }

        private static Tilemap Layer(GameObject grid, string name, int order, bool collider, bool oneWay = false)
        {
            Transform t = grid.transform.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform, false);
            go.transform.localPosition = Vector3.zero;
            var map = go.GetComponent<Tilemap>();
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            if (collider)
            {
                go.layer = TerrainLayer;
                var rb = GetOrAdd<Rigidbody2D>(go);
                rb.bodyType = RigidbodyType2D.Static;
                var tc = GetOrAdd<TilemapCollider2D>(go);
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                var cc = GetOrAdd<CompositeCollider2D>(go);
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
                if (oneWay)
                {
                    cc.usedByEffector = true;
                    var eff = GetOrAdd<PlatformEffector2D>(go);
                    eff.useOneWay = true; eff.surfaceArc = 170f; eff.useSideFriction = false; eff.useSideBounce = false;
                }
            }
            if (!collider)
            {
                foreach (var c in go.GetComponents<CompositeCollider2D>()) Object.DestroyImmediate(c);
                foreach (var c in go.GetComponents<PlatformEffector2D>()) Object.DestroyImmediate(c);
                foreach (var c in go.GetComponents<TilemapCollider2D>()) Object.DestroyImmediate(c);
                foreach (var c in go.GetComponents<Rigidbody2D>()) Object.DestroyImmediate(c);
            }
            map.ClearAllTiles();
            return map;
        }

        // 0 vacío · 1 tierra de jungla · 2 piedra de ruinas
        private static int Cell(Map m, int x, int y)
        {
            if (x < 0 || x >= m.W) return 2;
            if (y < 0) return IsPitColumn(m, x) ? 0 : 1;
            if (y >= m.H) return 0;
            char c = m.At(x, y);
            return c == '#' ? 1 : (c == '=' || c == 'J') ? 2 : 0;
        }

        // Pared de fondo de cueva: celdas ':' y también las que tienen espinas, enemigos, objetos
        // o huecos de 1–2 tiles junto a ':' (antes quedaban "sin fondo").
        private static bool IsCaveBack(Map m, int x, int y)
        {
            if (m.At(x, y) == ':') return true;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (m.At(x + dx, y + dy) == ':') return true;
            if (m.At(x, y) == ' ') return false;
            // objetos más anchos (fila de espinas, ramas): buscar ':' un poco más lejos
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (m.At(x + dx, y + dy) == ':') return true;
            return false;
        }

        private static bool IsPitColumn(Map m, int x) => Cell(m, x, 0) == 0 && m.At(x, 0) != '%';

        private static void PaintTerrain(Map m, Tilemap solid, Tilemap deco, Tilemap bg)
        {
            Tile[] brownL = Enumerable.Range(0, 4).Select(i => T($"tile_jungle_wall_brown_r{i}_c0")).ToArray();
            Tile[] brownR = Enumerable.Range(0, 4).Select(i => T($"tile_jungle_wall_brown_r{i}_c1")).ToArray();
            Tile[] greyL = Enumerable.Range(0, 4).Select(i => T($"tile_jungle_wall_grey_r{i}_c0")).ToArray();
            Tile[] greyR = Enumerable.Range(0, 4).Select(i => T($"tile_jungle_wall_grey_r{i}_c1")).ToArray();
            Tile fill = T("tile_jungle_wall_brown_r0_c2");
            Tile[] accents = { T("tile_jungle_wall_brown_r1_c2"), T("tile_jungle_wall_brown_r2_c3"), T("tile_jungle_wall_grey_r1_c2") };

            for (int x = 0; x < m.W; x++)
            {
                for (int y = 0; y < m.H; y++)
                {
                    int type = Cell(m, x, y);
                    if (type == 0)
                    {
                        continue; // el fondo de las cuevas ahora es parallax (SetupCaveBackdrop)
                    }
                    bool grey = type == 2;
                    string g = grey ? "tile_jungle_ground_grey" : "tile_jungle_ground_brown";
                    string b = grey ? "tile_jungle_bottom_grey" : "tile_jungle_bottom_brown";
                    bool up = Cell(m, x, y + 1) != 0, down = Cell(m, x, y - 1) != 0;
                    bool left = Cell(m, x - 1, y) != 0, right = Cell(m, x + 1, y) != 0;
                    Tile chosen = fill;
                    if (!up)
                    {
                        int row = Mathf.Abs(x) % 3 == 0 ? 3 : 1;
                        int col = !left ? 0 : (!right ? 2 : 1);
                        chosen = T($"{g}_r{row}_c{col}") ?? T($"{g}_r1_c{col}");
                        char above = m.At(x, y + 1);
                        if (above != '^' && above != '%' && Cell(m, x, y + 1) == 0)
                        {
                            Tile cap = T($"{g}_r{row - 1}_c{col}") ?? T($"{g}_r0_c{col}");
                            if (cap) deco.SetTile(new Vector3Int(x, y + 1, 0), cap);
                        }
                    }
                    else if (!down && y > 0)
                        chosen = !left ? T($"{b}_r0_c0") : (!right ? T($"{b}_r0_c3") : T($"{b}_r0_c{x % 2 + 1}"));
                    else if (!left) chosen = (grey ? greyL : brownL)[y % 4] ?? fill;
                    else if (!right) chosen = (grey ? greyR : brownR)[y % 4] ?? fill;
                    else if (Hash(x, y) % 11 == 0) chosen = accents[Hash(x, y) % accents.Length] ?? fill;
                    solid.SetTile(new Vector3Int(x, y, 0), GridTile(chosen ?? fill));
                }
            }
        }

        // ───────────── Árboles (como en guide_jungle.png) ─────────────
        // Copa redonda y frondosa en dos capas (azul atrás, verde oscuro adelante), tronco completo con ramas
        // y raíces, y racimos de hojas al frente.
        private static Tilemap TreeLayer(Transform grid, string name, int order)
        {
            Transform t = grid.Find(name);
            var go = t != null ? t.gameObject : new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.transform.localPosition = Vector3.zero;
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            var map = go.GetComponent<Tilemap>();
            map.ClearAllTiles();
            return map;
        }

        // Cambiar este nombre vuelve a plantar los árboles en los niveles ya generados (al abrirlos).
        private const string TreesVersion = "Arboles_v3";

        private static void PlantTrees(Map m, Transform grid, Tilemap bg)
        {
            Tilemap far = TreeLayer(grid, "Arboles_Copa_Lejana", -13);
            Tilemap canopy = TreeLayer(grid, "Arboles_Copa", -12);
            Tilemap leaves = TreeLayer(grid, "Arboles_Hojas", -9);
            foreach (string old in new[] { "Arboles_v2" })
            {
                var marker = grid.Find(old);
                if (marker != null) Object.DestroyImmediate(marker.gameObject);
            }
            if (grid.Find(TreesVersion) == null) new GameObject(TreesVersion).transform.SetParent(grid, false); // marcador de versión
            var spots = TreeSpots(m, 7, 8, 15, 18);
            // Borrar los troncos viejos: donde los ponía la versión anterior y donde van los nuevos
            // (así no quedan árboles "pelones" sin copa; lo pintado a mano en otros lugares no se toca)
            if (bg != null)
                foreach (var t in spots.Concat(TreeSpots(m, 6, 7, 12, 16)))
                    for (int x = t.x - 4; x <= t.x + 6; x++)
                        for (int y = t.y; y <= t.y + 12; y++)
                        {
                            var pos = new Vector3Int(x, y, 0);
                            var oldTile = bg.GetTile(pos);
                            if (oldTile != null && oldTile.name.StartsWith("tile_jungle_tree_")) bg.SetTile(pos, null);
                        }
            int n = 0;
            foreach (var t in spots)
            {
                string p = n++ % 2 == 0 ? "tile_jungle_tree_light" : "tile_jungle_tree_dark";
                int bx = t.x - 3, by = t.y; // el tronco (columnas 3-4 de la hoja) queda centrado en la marca
                // Copa frondosa y redonda: unión de círculos sobre la punta del tronco, con un poco de
                // variación por árbol. Azul (atrás) un poco más grande y alta; verde oscuro adelante.
                float J(int k, float a) => (Hash(t.x, t.y * 31 + k) % 3 - 1) * a;
                float cx = bx + 4f; // entre las columnas 3 y 4 del tronco
                var puffs = new[]
                {
                    (cx, by + 10f, 4.6f),
                    (cx - 4.5f + J(1, 0.5f), by + 8.5f, 3.2f),
                    (cx + 4.5f + J(2, 0.5f), by + 8.8f + J(3, 0.5f), 3.2f),
                    (cx - 2.2f, by + 12.6f, 3.0f),
                    (cx + 2.4f + J(4, 0.5f), by + 13f, 2.9f),
                };
                var blue = Opening(Puffs(puffs, 0.6f, 1.1f));
                var green = Opening(Erode(Puffs(puffs, 0f, 1f)));
                Canopy(far, p, blue, 2, 0);
                Canopy(canopy, p, green, 7, 5);
                // Tronco con ramas y raíces: filas 3-10, columnas 0-5 + punta de la rama derecha (fila 5, col 6-7)
                for (int r = 3; r <= 10; r++)
                    for (int c = 0; c <= 7; c++)
                    {
                        if (c >= 6 && r != 5) continue; // esas celdas son hojas, no tronco
                        var tile = T($"{p}_r{r}_c{c}");
                        if (tile != null && bg != null) bg.SetTile(new Vector3Int(bx + c, by + 10 - r, 0), tile);
                    }
                // Racimos de hojas al frente: en las puntas de las ramas, alrededor de la punta del tronco
                // (para que se funda con la copa) y en los bordes de la copa.
                Stamp(leaves, p, bx - 1, by + 5, 6, 7, 6, 7);  // punta de la rama izquierda
                Stamp(leaves, p, bx + 6, by + 5, 6, 7, 6, 7);  // punta de la rama derecha
                Stamp(leaves, p, bx + 1, by + 7, 8, 10, 6, 8); // junto a la punta del tronco
                Stamp(leaves, p, bx + 5, by + 7, 8, 10, 6, 8);
                Stamp(leaves, p, bx + 3, by + 9, 6, 7, 6, 7);  // tapa la punta del tronco
                Stamp(leaves, p, bx - 4, by + 7, 5, 6, 8, 9);  // bordes de la copa
                Stamp(leaves, p, bx + 10, by + 7, 5, 6, 8, 9);
            }
        }

        // Celdas dentro de una unión de círculos (centro x, centro y, radio), desplazados dy y agrandados dr.
        private static HashSet<Vector2Int> Puffs((float x, float y, float r)[] circles, float dy, float dr)
        {
            var mask = new HashSet<Vector2Int>();
            foreach (var (px, py, pr) in circles)
            {
                float cy = py + dy, r = pr + dr;
                for (int x = Mathf.FloorToInt(px - r) - 1; x <= Mathf.CeilToInt(px + r) + 1; x++)
                    for (int y = Mathf.FloorToInt(cy - r) - 1; y <= Mathf.CeilToInt(cy + r) + 1; y++)
                    {
                        float ddx = x + 0.5f - px, ddy = y + 0.5f - cy;
                        if (ddx * ddx + ddy * ddy <= r * r) mask.Add(new Vector2Int(x, y));
                    }
            }
            return mask;
        }

        // Quita las celdas sueltas o de 1 tile de grosor (el autotiling de la copa necesita bloques de 2x2).
        private static HashSet<Vector2Int> Opening(HashSet<Vector2Int> mask)
        {
            var keep = new HashSet<Vector2Int>();
            foreach (var cell in mask)
                for (int ox = -1; ox <= 0; ox++)
                    for (int oy = -1; oy <= 0; oy++)
                        if (mask.Contains(new Vector2Int(cell.x + ox, cell.y + oy)) && mask.Contains(new Vector2Int(cell.x + ox + 1, cell.y + oy))
                            && mask.Contains(new Vector2Int(cell.x + ox, cell.y + oy + 1)) && mask.Contains(new Vector2Int(cell.x + ox + 1, cell.y + oy + 1)))
                            keep.Add(cell);
            return keep;
        }

        // Interior de la máscara (celdas con sus 8 vecinas dentro).
        private static HashSet<Vector2Int> Erode(HashSet<Vector2Int> mask)
        {
            return new HashSet<Vector2Int>(mask.Where(cell =>
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (!mask.Contains(new Vector2Int(cell.x + dx, cell.y + dy))) return false;
                return true;
            }));
        }

        // Lugares para árboles: las marcas 'T' del mapa y, además, cada suelo plano al aire libre
        // con espacio para la copa (sin cuevas, techos ni plataformas), separados al menos 18 tiles.
        // left/right/height: espacio libre que necesita la copa alrededor de la marca; gap: separación mínima.
        private static List<Vector2Int> TreeSpots(Map m, int left, int right, int height, int gap)
        {
            var spots = m.Find('T').ToList();
            for (int x = 3; x < m.W - 8; x++)
                for (int y = 1; y < m.H - 13; y++)
                {
                    bool flat = true;
                    for (int dx = -2; dx <= 3 && flat; dx++)
                        flat = Cell(m, x + dx, y - 1) != 0 && Cell(m, x + dx, y) == 0;
                    if (!flat) continue;
                    bool clear = true;
                    for (int xx = x - left; xx <= x + right && clear; xx++)
                        for (int yy = y; yy <= y + height && clear; yy++)
                        {
                            char ch = m.At(xx, yy);
                            if (Cell(m, xx, yy) != 0 || ch == ':' || ch == '-' || ch == '%' || ch == 'G') clear = false;
                        }
                    if (clear && spots.All(sp => Mathf.Abs(sp.x - x) >= gap)) spots.Add(new Vector2Int(x, y));
                }
            return spots;
        }

        // Autotiling de la copa con las piezas de la hoja: esquinas y bordes (bloque de 3x3 en c0..c0+2)
        // y esquinas interiores + relleno (columnas ic, ic+1).
        private static void Canopy(Tilemap map, string p, HashSet<Vector2Int> mask, int c0, int ic)
        {
            bool In(int x, int y) => mask.Contains(new Vector2Int(x, y));
            foreach (var cell in mask)
            {
                int x = cell.x, y = cell.y;
                bool up = In(x, y + 1), down = In(x, y - 1), left = In(x - 1, y), right = In(x + 1, y);
                int r, c;
                if (!up && !left) { r = 0; c = c0; }
                else if (!up && !right) { r = 0; c = c0 + 2; }
                else if (!down && !left) { r = 2; c = c0; }
                else if (!down && !right) { r = 2; c = c0 + 2; }
                else if (!up) { r = 0; c = c0 + 1; }
                else if (!down) { r = 2; c = c0 + 1; }
                else if (!left) { r = 1; c = c0; }
                else if (!right) { r = 1; c = c0 + 2; }
                else if (!In(x - 1, y + 1)) { r = 0; c = ic; }
                else if (!In(x + 1, y + 1)) { r = 0; c = ic + 1; }
                else if (!In(x - 1, y - 1)) { r = 1; c = ic; }
                else if (!In(x + 1, y - 1)) { r = 1; c = ic + 1; }
                else if (Hash(x, y) % 5 == 0) { r = 1; c = c0 + 1; } // algún remolino de hojas
                else { r = 2; c = ic; }                               // relleno
                var tile = T($"{p}_r{r}_c{c}");
                if (tile != null) map.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        // ───────────── Fondo parallax de cueva ─────────────
        private const string CaveArt = "Assets/Sprites/FondosCueva";

        private static void SetupCaveBackdrop(Map m, Tilemap bg)
        {
            // Quitar el relleno oscuro de tiles que había detrás de las cuevas
            Tile fill = T("tile_jungle_wall_brown_r0_c2");
            if (bg != null && fill != null)
                for (int y = 0; y < m.H; y++)
                    for (int x = 0; x < m.W; x++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        if (Cell(m, x, y) == 0 && IsCaveBack(m, x, y) && bg.GetTile(pos) == fill) bg.SetTile(pos, null);
                    }

            GameObject group = GameObject.Find("Fondo_Parallax");
            if (group == null) return;
            foreach (string old in new[] { "Cueva_Lejos", "Cueva_Media", "Cueva_Cerca" })
            {
                var t = group.transform.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            var outdoor = new List<SpriteRenderer>();
            foreach (Transform layer in group.transform)
                if (layer.TryGetComponent(out SpriteRenderer sr)) outdoor.Add(sr);

            var cave = new List<SpriteRenderer>();
            var specs = new[] { ("Cueva_Lejos", -98, 0.04f), ("Cueva_Media", -78, 0.18f), ("Cueva_Cerca", -53, 0.4f) };
            Vector3 cam = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            foreach (var (name, order, factor) in specs)
            {
                var sprite = ImportBackdrop($"{CaveArt}/{name}.png");
                if (sprite == null) continue;
                var go = new GameObject(name);
                go.transform.SetParent(group.transform, false);
                go.transform.position = new Vector3(cam.x, cam.y, 20f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = order;
                go.AddComponent<BackdropLayer>().Configure(factor, 0.03f, false);
                cave.Add(sr);
            }

            // Zonas de cueva: celdas ':' del mapa de texto + cualquier espacio que TÚ cierres con techo y paredes
            Tilemap solidMap = bg != null ? bg.transform.parent.Find("mapa")?.GetComponent<Tilemap>() : null;
            var caveCells = CaveCells(m, solidMap);
            bool IsCave(int cx, int cy) => caveCells.Contains(new Vector2Int(cx, cy));
            var zones = new List<Rect>();
            var open = new Dictionary<(int, int), int>();
            for (int y = 0; y <= m.H + 10; y++)
            {
                var runs = new HashSet<(int, int)>();
                if (y < m.H + 10)
                    for (int x = 0; x < m.W;)
                    {
                        if (!IsCave(x, y)) { x++; continue; }
                        int x0 = x;
                        while (x < m.W && IsCave(x, y)) x++;
                        runs.Add((x0, x));
                    }
                foreach (var run in open.Keys.ToList())
                    if (!runs.Contains(run))
                    {
                        zones.Add(Rect.MinMaxRect(run.Item1, open[run], run.Item2, y));
                        open.Remove(run);
                    }
                foreach (var run in runs)
                    if (!open.ContainsKey(run)) open[run] = y;
            }

            // Máscara con la forma de las cuevas: dentro se ve el fondo de cueva, fuera la jungla
            var oldMask = GameObject.Find("Mascara_Cuevas");
            if (oldMask != null) Object.DestroyImmediate(oldMask);
            var maskRoot = new GameObject("Mascara_Cuevas");
            new GameObject("_v3").transform.SetParent(maskRoot.transform, false); // versión: incluye cuevas pintadas a mano
            var maskSprite = ImportMask($"{CaveArt}/Mascara.png");
            foreach (var z in zones)
            {
                var piece = new GameObject("Zona_Cueva");
                piece.transform.SetParent(maskRoot.transform, false);
                piece.transform.position = new Vector3(z.center.x, z.center.y, 0f);
                piece.transform.localScale = new Vector3(z.width, z.height, 1f);
                piece.AddComponent<SpriteMask>().sprite = maskSprite;
            }

            var ambience = GetOrAdd<CaveAmbience>(group);
            ambience.Configure(zones.ToArray(), outdoor.ToArray(), cave.ToArray());
            EditorUtility.SetDirty(group);
            Debug.Log($"[Primal Echo] Fondo de cueva: {zones.Count} zonas de cueva.");
        }

        private static HashSet<Vector2Int> CaveCells(Map m, Tilemap solid)
        {
            bool Solid(int x, int y) => solid != null ? solid.HasTile(new Vector3Int(x, y, 0)) : Cell(m, x, y) != 0;
            var cells = new HashSet<Vector2Int>();
            for (int y = 0; y < m.H + 10; y++)
                for (int x = 0; x < m.W; x++)
                {
                    if (Solid(x, y)) continue;
                    bool cave = y < m.H && IsCaveBack(m, x, y);
                    if (!cave)
                    {
                        // Techo arriba (máx. 12 tiles) y pared a algún lado (máx. 30 tiles) en la misma fila
                        bool roof = false, wallL = false, wallR = false;
                        for (int k = 1; k <= 12 && !roof; k++) roof = Solid(x, y + k);
                        for (int k = 1; k <= 30 && roof && !wallL; k++) wallL = Solid(x - k, y);
                        for (int k = 1; k <= 30 && roof && !wallR; k++) wallR = Solid(x + k, y);
                        cave = roof && (wallL || wallR);
                    }
                    if (cave) cells.Add(new Vector2Int(x, y));
                }
            return cells;
        }

        private static Sprite ImportMask(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Falta " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 4; // 4 px = 1 unidad
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite ImportBackdrop(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Falta " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 18;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ───────────── Fondo parallax (Assets/Sprites/Fondos/BosqueCaricatura, CraftPix "Cartoon Forest" 02) ─────────────
        // 5 capas de 1920x1080 que se repiten solas a lo ancho (sin espejo). Del más lejano al más cercano.
        // fx: 0 = quieto con la cámara (cielo) · 1 = igual que el mundo. sink: cuánto baja la capa cuando subes
        // (así al trepar se ve el cielo sobre los árboles y el pasto dibujado no parece piso).
        // Todo va detrás del terreno (orden < -50) y sin colisiones. Dentro de las cuevas lo tapa la máscara.
        private const string ParallaxFolder = "Assets/Sprites/Fondos/BosqueCaricatura/Imagenes/BosqueDos/Capas";
        private const string JungleVersion = "_Bosque_v2";
        // Cada capa mide 1.25 veces la pantalla: al subir o bajar se desliza un poco (parallax vertical)
        // pero nunca enseña sus orillas ni cambia de tamaño.
        private const float JungleCover = 1.25f;
        private static readonly (string file, int order, float fx, float fy, float sink)[] JungleLayers =
        {
            ("Sky",          -100, 0.00f, 0.00f, 0f),
            ("BG_Decor",      -90, 0.10f, 0.03f, 0f),
            ("Middle_Decor",  -80, 0.25f, 0.05f, 0f),
            ("Foreground",    -70, 0.45f, 0.08f, 0f),
            ("Ground",        -60, 0.60f, 0.10f, 0f),
        };

        // Borra el fondo anterior (menos las capas de cueva) y crea las 5 capas del bosque.
        private static void BuildJungleParallax(Transform group, string theme)
        {
            for (int i = group.childCount - 1; i >= 0; i--)
                if (!group.GetChild(i).name.StartsWith("Cueva_")) Object.DestroyImmediate(group.GetChild(i).gameObject);
            Vector3 cam = Camera.main != null ? Camera.main.transform.position : group.position;
            int made = 0;
            for (int i = 0; i < JungleLayers.Length; i++)
            {
                var (file, order, fx, fy, sink) = JungleLayers[i];
                var sprite = LoadKitSprite($"{ParallaxFolder}/{file}.png");
                if (sprite == null) continue;
                var go = new GameObject($"Capa_{i + 1}_{file}");
                go.transform.SetParent(group, false);
                go.transform.position = new Vector3(cam.x, cam.y, 20f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                var backdrop = go.AddComponent<BackdropLayer>();
                backdrop.Configure(fx, fy, false, sink, false, 0f);
                SetField(backdrop, "cover", JungleCover);
                made++;
            }
            new GameObject(JungleVersion).transform.SetParent(group, false); // marcador de versión
            EditorUtility.SetDirty(group.gameObject);
            Debug.Log($"[Primal Echo] Fondo parallax del bosque: {made}/{JungleLayers.Length} capas.");
        }

        // ───────────── Partículas de la jungla (luciérnagas azules, insectos, hojas) ─────────────
        private const string ParticlesFolder = "Assets/Sprites/Particulas";
        private const string ParticlesVersion = "_Particulas_v1";

        // Pone JungleParticles en la cámara del nivel abierto. Devuelve true si cambió algo.
        private static bool SetupJungleParticles()
        {
            Camera cam = Camera.main;
            if (cam == null || cam.transform.Find(ParticlesVersion) != null) return false;
            var glow = ParticleMaterial("Luciernaga");
            var leaf = ParticleMaterial("Hoja");
            var bug = ParticleMaterial("Insecto");
            if (glow == null && leaf == null && bug == null) return false;
            GetOrAdd<JungleParticles>(cam.gameObject).Configure(glow, leaf, bug);
            new GameObject(ParticlesVersion).transform.SetParent(cam.transform, false); // marcador de versión
            EditorUtility.SetDirty(cam.gameObject);
            Debug.Log("[Primal Echo] Partículas de la jungla añadidas a la cámara.");
            return true;
        }

        private static Material ParticleMaterial(string name)
        {
            string texPath = $"{ParticlesFolder}/{name}.png", matPath = $"{ParticlesFolder}/Mat_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) return mat;
            if (!File.Exists(texPath)) { Debug.LogWarning("[Primal Echo] Falta " + texPath); return null; }
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (tex == null || shader == null) return null;
            mat = new Material(shader) { mainTexture = tex };
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static Sprite LoadKitSprite(string path)
        {
            if (!File.Exists(path)) { Debug.LogWarning("[Primal Echo] Falta " + path); return null; }
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); imp = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (imp == null) return null;
            if (KitSelvaImporter.NeedsReimport(imp, path)) // KitSelvaImporter aplica los ajustes al reimportar
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ───────────── Capa pintada a mano ─────────────
        // Un prefab por nivel con tilemaps propios. Lo que pintes ahí (en modo prefab) no se borra al regenerar.
        private static void AttachHandPainted(string sceneName, Transform grid)
        {
            string path = $"{MapsFolder}/Pintado_A_Mano_{sceneName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject("Pintado_A_Mano", typeof(Grid));
                foreach (var (name, order) in new[] { ("Fondo_A_Mano", -9), ("Frente_A_Mano", 6) })
                {
                    var layer = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                    layer.transform.SetParent(root.transform, false);
                    layer.GetComponent<TilemapRenderer>().sortingOrder = order;
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            instance.name = "Pintado_A_Mano";
            instance.transform.position = grid.position;
        }

        // ───────────── Colisión ─────────────
        // Cajas rectangulares calculadas a partir de lo pintado en "mapa" (suelo/paredes) y
        // "Ramas_Un_Sentido" (plataformas que se atraviesan desde abajo).
        private static void RebuildCollisionIn(Scene scene)
        {
            GameObject grid = scene.GetRootGameObjects().Select(g => g.name == "Grid" ? g : null).FirstOrDefault(g => g != null);
            if (grid == null) return;
            var solid = grid.transform.Find("mapa")?.GetComponent<Tilemap>();
            var ramas = grid.transform.Find("Ramas_Un_Sentido")?.GetComponent<Tilemap>();
            if (solid == null) return;
            BuildCollisionFromTiles(grid.transform, solid, ramas);
        }

        private static void BuildCollisionFromTiles(Transform grid, Tilemap solid, Tilemap branches)
        {
            foreach (string old in new[] { "Colision_Suelo", "Colision_Ramas" })
            {
                var t = grid.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            var ground = new GameObject("Colision_Suelo") { layer = TerrainLayer };
            ground.transform.SetParent(grid, false);
            ground.isStatic = true;
            solid.CompressBounds();
            int boxes = MergeBoxes(solid.cellBounds, (x, y) => solid.HasTile(new Vector3Int(x, y, 0)), ground, false);

            int planks = 0;
            if (branches != null)
            {
                var ramas = new GameObject("Colision_Ramas") { layer = TerrainLayer };
                ramas.transform.SetParent(grid, false);
                branches.CompressBounds();
                planks = MergeBoxes(branches.cellBounds, (x, y) => branches.HasTile(new Vector3Int(x, y, 0)), ramas, true);
                var eff = ramas.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true; eff.surfaceArc = 170f; eff.useSideFriction = false; eff.useSideBounce = false;
            }
            Debug.Log($"[Primal Echo] Colisiones actualizadas: {boxes} cajas de suelo/pared, {planks} de ramas.");
        }

        private static int MergeBoxes(BoundsInt b, Func<int, int, bool> filled, GameObject owner, bool effector)
        {
            var open = new Dictionary<(int, int), int>(); // (x0,x1) -> fila inicial
            int count = 0;
            void Close((int x0, int x1) run, int y0, int y1)
            {
                var box = owner.AddComponent<BoxCollider2D>();
                box.offset = new Vector2((run.x0 + run.x1) / 2f, (y0 + y1) / 2f);
                box.size = new Vector2(run.x1 - run.x0, y1 - y0);
                box.usedByEffector = effector;
                count++;
            }
            for (int y = b.yMin; y <= b.yMax; y++)
            {
                var runs = new HashSet<(int, int)>();
                if (y < b.yMax)
                    for (int x = b.xMin; x < b.xMax;)
                    {
                        if (!filled(x, y)) { x++; continue; }
                        int x0 = x;
                        while (x < b.xMax && filled(x, y)) x++;
                        runs.Add((x0, x));
                    }
                foreach (var run in open.Keys.ToList())
                    if (!runs.Contains(run)) { Close(run, open[run], y); open.Remove(run); }
                foreach (var run in runs)
                    if (!open.ContainsKey(run)) open[run] = y;
            }
            return count;
        }

        // ───────────── Conservar lo que pintas a mano ─────────────
        [Serializable] private class TileEdit { public string layer; public int x, y; public string tile; } // tile "" = borrado
        [Serializable] private class TileList { public List<TileEdit> items = new List<TileEdit>(); }

        private static string BaselinePath(string scene) => $"{MapsFolder}/Generado_{scene}.json";
        private static string EditsPath(string scene) => $"{MapsFolder}/Tus_Cambios_{scene}.json";

        private static string TileKey(TileBase t) => t == null ? "" : AssetDatabase.GetAssetPath(t);

        private static void SaveBaseline(string scene, Dictionary<string, Tilemap> layers)
        {
            var list = new TileList();
            foreach (var kv in layers)
            {
                kv.Value.CompressBounds();
                foreach (var pos in kv.Value.cellBounds.allPositionsWithin)
                {
                    var t = kv.Value.GetTile(pos);
                    if (t != null) list.items.Add(new TileEdit { layer = kv.Key, x = pos.x, y = pos.y, tile = TileKey(t) });
                }
            }
            File.WriteAllText(BaselinePath(scene), JsonUtility.ToJson(list));
        }

        // Compara la escena actual con lo que generó la máquina la última vez: la diferencia son tus cambios.
        private static bool editsAreFullCopy;

        private static List<TileEdit> CaptureUserEdits(string scene)
        {
            var edits = new List<TileEdit>();
            editsAreFullCopy = false;
            string path = ScenePath(scene);
            if (File.Exists(path) && !File.Exists(BaselinePath(scene)))
            {
                // Escena hecha con una versión anterior del generador: se conserva tal cual está pintada
                editsAreFullCopy = true;
                var opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var grid = opened.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid");
                if (grid != null)
                    foreach (var map in grid.GetComponentsInChildren<Tilemap>(true))
                    {
                        if (map.transform.parent != grid.transform) continue;
                        map.CompressBounds();
                        foreach (var pos in map.cellBounds.allPositionsWithin)
                        {
                            string now = TileKey(map.GetTile(pos));
                            if (now != "") edits.Add(new TileEdit { layer = map.name, x = pos.x, y = pos.y, tile = now });
                        }
                    }
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return edits;
            }
            if (File.Exists(path) && File.Exists(BaselinePath(scene)))
            {
                var baseline = JsonUtility.FromJson<TileList>(File.ReadAllText(BaselinePath(scene)));
                var baseMap = new Dictionary<(string, int, int), string>();
                foreach (var e in baseline.items) baseMap[(e.layer, e.x, e.y)] = e.tile;
                var opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var grid = opened.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid");
                if (grid != null)
                {
                    var seen = new HashSet<(string, int, int)>();
                    foreach (var map in grid.GetComponentsInChildren<Tilemap>(true))
                    {
                        if (map.transform.parent != grid.transform) continue;
                        map.CompressBounds();
                        foreach (var pos in map.cellBounds.allPositionsWithin)
                        {
                            var key = (map.name, pos.x, pos.y);
                            string now = TileKey(map.GetTile(pos));
                            if (now == "") continue;
                            seen.Add(key);
                            if (!baseMap.TryGetValue(key, out var was) || was != now)
                                edits.Add(new TileEdit { layer = map.name, x = pos.x, y = pos.y, tile = now });
                        }
                    }
                    foreach (var kv in baseMap)
                        if (!seen.Contains(kv.Key) && grid.transform.Find(kv.Key.Item1) != null)
                            edits.Add(new TileEdit { layer = kv.Key.Item1, x = kv.Key.Item2, y = kv.Key.Item3, tile = "" });
                }
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // antes de borrar la escena
                File.WriteAllText(EditsPath(scene), JsonUtility.ToJson(new TileList { items = edits }, true));
                Debug.Log($"[Primal Echo] {scene}: se guardaron {edits.Count} cambios tuyos en {EditsPath(scene)}");
            }
            else if (File.Exists(EditsPath(scene)))
            {
                edits = JsonUtility.FromJson<TileList>(File.ReadAllText(EditsPath(scene))).items;
            }
            return edits;
        }

        private static void ApplyUserEdits(List<TileEdit> edits, Dictionary<string, Tilemap> layers)
        {
            if (editsAreFullCopy)
                foreach (var name in edits.Select(e => e.layer).Distinct())
                    if (layers.TryGetValue(name, out var m)) m.ClearAllTiles();
            foreach (var e in edits)
            {
                if (!layers.TryGetValue(e.layer, out var map)) continue;
                var tile = string.IsNullOrEmpty(e.tile) ? null : AssetDatabase.LoadAssetAtPath<TileBase>(e.tile);
                if (!string.IsNullOrEmpty(e.tile) && tile == null) continue;
                map.SetTile(new Vector3Int(e.x, e.y, 0), tile);
            }
        }

        private static void PaintBranches(Map m, Tilemap branches, Tilemap deco)
        {
            Runs(m, '-', (x0, len, y) =>
            {
                for (int i = 0; i < len; i++)
                {
                    int c = i == 0 ? 4 : (i == len - 1 ? 9 : 5 + (i - 1) % 4);
                    var body = T($"tile_jungle_treelimb_r1_c{c}");
                    if (body) branches.SetTile(new Vector3Int(x0 + i, y, 0), GridTile(body));
                    var top = T($"tile_jungle_treelimb_r0_c{c}");
                    if (top && m.At(x0 + i, y + 1) != 'o') deco.SetTile(new Vector3Int(x0 + i, y + 1, 0), top);
                    var moss = T($"tile_jungle_treelimb_r2_c{c}");
                    if (moss && Cell(m, x0 + i, y - 1) == 0) deco.SetTile(new Vector3Int(x0 + i, y - 1, 0), moss);
                }
            });
        }

        private static void Stamp(Tilemap map, string sheet, int x, int bottomY, int r0, int r1, int c0, int c1)
        {
            int h = r1 - r0 + 1;
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                {
                    var t = T($"{sheet}_r{r}_c{c}");
                    if (t) map.SetTile(new Vector3Int(x + c - c0, bottomY + (h - 1 - (r - r0)), 0), t);
                }
        }

        private static void PaintDecoration(Map m, Tilemap deco, Tilemap bg)
        {
            const string P = "tile_jungle_plants_objects";
            // Los árboles grandes ('T') los arma PlantTrees con su copa, tronco y hojas
            string[] small = { $"{P}_r10_c7", $"{P}_r10_c6", $"{P}_r9_c10", $"{P}_r9_c11", $"{P}_r8_c10", $"{P}_r8_c12" };
            Tile vine = T("tile_jungle_bg_vines_r6_c6") ?? T("tile_jungle_bg_vines_r4_c0");
            for (int x = 1; x < m.W - 1; x++)
            {
                // Agua al fondo de los fosos
                if (IsPitColumn(m, x))
                {
                    var surf = T($"tile_jungle_water_r1_c{(x % 2 == 0 ? 2 : 4)}");
                    var deep = T($"tile_jungle_water_r{2 + x % 2}_c{2 + (x / 2) % 2}");
                    if (surf) deco.SetTile(new Vector3Int(x, 0, 0), surf);
                    if (deep) deco.SetTile(new Vector3Int(x, -1, 0), deep);
                }
                for (int y = 0; y < m.H - 1; y++)
                {
                    int h = Hash(x, y);
                    bool surface = Cell(m, x, y) != 0 && Cell(m, x, y + 1) == 0 && m.At(x, y + 1) == ' ';
                    if (surface && Cell(m, x - 1, y) != 0 && Cell(m, x + 1, y) != 0 && h % 9 == 0)
                        deco.SetTile(new Vector3Int(x, y + 1, 0), T(small[h % small.Length]));
                    bool ceiling = Cell(m, x, y + 1) != 0 && Cell(m, x, y) == 0 && y > 2;
                    if (ceiling && vine && h % 7 == 0)
                        for (int k = 0; k < 2 + h % 3 && Cell(m, x, y - k) == 0 && m.At(x, y - k) != '-'; k++)
                            bg.SetTile(new Vector3Int(x, y - k, 0), vine);
                }
            }
            // Estandarte de calavera al borde de los fosos anchos
            for (int x = 2; x < m.W - 6; x++)
            {
                if (!IsPitColumn(m, x) && IsPitColumn(m, x + 1) && IsPitColumn(m, x + 5))
                {
                    int top = TopAt(m, x);
                    if (top > 0 && m.At(x, top) == ' ') Stamp(deco, P, x, top, 10, 11, 4, 4);
                }
            }
        }

        private static int TopAt(Map m, int x)
        {
            for (int y = m.H - 1; y >= 0; y--) if (Cell(m, x, y) != 0) return y + 1;
            return -1;
        }

        private static void Runs(Map m, char ch, Action<int, int, int> create)
        {
            for (int y = 0; y < m.H; y++)
                for (int x = 0; x < m.W;)
                {
                    if (m.At(x, y) != ch) { x++; continue; }
                    int x0 = x;
                    while (m.At(x, y) == ch) x++;
                    create(x0, x - x0, y);
                }
        }

        private static void SetupCamera(Map m, Vector3 playerPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 min = new Vector2(HalfWidth, HalfHeight);
            Vector2 max = new Vector2(Mathf.Max(min.x, m.W - HalfWidth), Mathf.Max(min.y, m.H - HalfHeight));
            Vector3 camPos = new Vector3(Mathf.Clamp(playerPos.x, min.x, max.x), Mathf.Clamp(playerPos.y + 2f, min.y, max.y), -10f);
            cam.transform.position = camPos;
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow != null)
            {
                follow.SetBounds(min, max);
                SetField(follow, "offset", new Vector3(0f, 1.5f, -10f));
                EditorUtility.SetDirty(follow);
            }
            // El fondo parallax lo construye BuildJungleParallax (kit de selva)
        }

        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Campo '{field}' no existe en {target.GetType().Name}"); return; }
            switch (value)
            {
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case Vector3 v: p.vector3Value = v; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────────── Enemigos y objetos ─────────────

        private sealed class Art
        {
            public Sprite[] robot, heavy, wasp, plant;
            public Sprite shard;
        }

        private static Art LoadEnemyArt()
        {
            Sprite[] Load(string name, int count, float ppu, bool bottom) =>
                Enumerable.Range(0, count).Select(i => Import($"{EnemySprites}/{name}_{i}.png", ppu, bottom)).ToArray();
            return new Art
            {
                robot = Load("Robot_Centinela", 2, 32, true),
                heavy = Load("Robot_Pesado", 2, 32, true),
                wasp = Load("Avispa", 11, 48, false),
                plant = Load("Planta_Carnivora", 2, 40, true),
                shard = Import($"{EnemySprites}/Fragmento_Eco.png", 32, false),
            };
        }

        private static Sprite Import(string path, float ppu, bool bottomPivot)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) throw new FileNotFoundException("Falta el sprite " + path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static SpriteRenderer Visual(Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static GameObject Robot(Transform root, Vector2Int c, Art art, bool heavy)
        {
            var go = new GameObject(heavy ? "Robot_Pesado" : "Robot_Centinela");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(c.x + 0.5f, c.y + 0.02f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f; rb.mass = heavy ? 8f : 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = heavy ? new Vector2(1.5f, 2.1f) : new Vector2(0.9f, 1.5f);
            box.offset = new Vector2(0f, box.size.y / 2f);
            var sr = Visual(go.transform, (heavy ? art.heavy : art.robot)[0], 4);
            go.AddComponent<SpriteFrames>().Configure(sr, heavy ? art.heavy : art.robot, 2.5f);
            SetField(go.AddComponent<EnemyHealth>(), "health", heavy ? 10 : 4);
            var hurt = new GameObject("Golpe");
            hurt.transform.SetParent(go.transform, false);
            var hurtBox = hurt.AddComponent<BoxCollider2D>();
            hurtBox.isTrigger = true; hurtBox.size = box.size * 0.9f; hurtBox.offset = box.offset;
            SetField(hurt.AddComponent<ContactHazard>(), "damage", heavy ? 2 : 1);
            go.AddComponent<EnemyFeedback>().Configure(sr);
            go.AddComponent<RobotPatrol>().Configure(1 << TerrainLayer, sr.transform, heavy ? 1.1f : 1.8f, heavy ? 2.4f : 3.4f);
            return go;
        }

        private static GameObject Wasp(Transform root, Vector2Int c, Art art)
        {
            var go = new GameObject("Avispa");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.55f;
            var sr = Visual(go.transform, art.wasp[0], 6);
            go.AddComponent<SpriteFrames>().Configure(sr, art.wasp, 12f);
            SetField(go.AddComponent<EnemyHealth>(), "health", 2);
            go.AddComponent<ContactHazard>();
            go.AddComponent<EnemyFeedback>().Configure(sr);
            go.AddComponent<WaspFlyer>().Configure(sr.transform);
            return go;
        }

        private static GameObject Plant(Transform root, Vector2Int c, Art art)
        {
            var go = new GameObject("Planta_Carnivora");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(c.x + 0.5f, c.y, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(1.1f, 1.8f); box.offset = new Vector2(0f, 0.9f);
            var sr = Visual(go.transform, art.plant[0], 4);
            go.AddComponent<CarnivorousPlant>().Configure(sr, art.plant[0], art.plant[1]);
            SetField(go.AddComponent<EnemyHealth>(), "health", 4);
            go.AddComponent<ContactHazard>();
            go.AddComponent<EnemyFeedback>().Configure(sr);
            return go;
        }

        private static Light2D Light(Transform parent, string name, Vector3 pos, Color color, float intensity, float radius)
        {
            var go = new GameObject(name, typeof(Light2D));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.GetComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color; l.intensity = intensity;
            l.pointLightOuterRadius = radius; l.pointLightInnerRadius = radius * 0.25f;
            return l;
        }

        private static void Checkpoint(Transform root, Tilemap deco, Transform lights, Vector2Int c, string label)
        {
            Stamp(deco, "tile_jungle_plants_objects", c.x, c.y, 10, 11, 5, 5); // farola
            var go = new GameObject("Checkpoint_" + label);
            go.transform.SetParent(root);
            go.transform.position = new Vector3(c.x + 0.5f, c.y + 0.02f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(1.6f, 2.4f); box.offset = new Vector2(0f, 1.2f);
            var lamp = Light(lights, "Luz_" + label, new Vector3(c.x + 0.5f, c.y + 1.5f, 0f), new Color(1f, 0.82f, 0.45f), 1.4f, 5f);
            go.AddComponent<JungleCheckpoint>().Configure(label, lamp);
        }

        private static void Thorns(Transform root, Tilemap deco, int x0, int len, int y)
        {
            for (int i = 0; i < len; i++)
                deco.SetTile(new Vector3Int(x0 + i, y, 0), T($"tile_jungle_plants_objects_r3_c{8 + i % 2}"));
            var go = new GameObject("Espinas");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(x0 + len / 2f, y + 0.35f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(len - 0.2f, 0.7f);
            go.AddComponent<ContactHazard>(); // permite pogo con la lanza hacia abajo
            go.AddComponent<HazardZone>();    // y si las tocas, vuelves al último suelo seguro
        }

        private static void Crumble(Transform root, int x0, int len, int y)
        {
            var go = new GameObject("Piedra_Que_Se_Derrumba") { layer = TerrainLayer };
            go.transform.SetParent(root);
            go.transform.position = new Vector3(x0 + len / 2f, y + 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(len, 1f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            for (int i = 0; i < len; i++)
            {
                int col = i == 0 ? 0 : (i == len - 1 ? 2 : 1);
                var body = Visual(visual, S($"tile_jungle_ground_grey_r3_c{col}"), 1);
                body.transform.localPosition = new Vector3(i - len / 2f + 0.5f, 0f, 0f);
                body.color = new Color(0.95f, 0.85f, 0.75f);
                var cap = Visual(visual, S($"tile_jungle_ground_grey_r2_c{col}"), 5);
                cap.transform.localPosition = new Vector3(i - len / 2f + 0.5f, 1f, 0f);
            }
            go.AddComponent<CrumbleBlock>().Configure(visual);
        }

        private static void Geyser(Transform root, Transform lights, int x0, int len, int y)
        {
            var go = new GameObject("Geiser");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(x0 + len / 2f, y + 1f + 2.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(Mathf.Max(0.8f, len - 0.1f), 5f);
            go.AddComponent<GeyserColumn>();
            Light(lights, "Luz_Geiser", new Vector3(x0 + len / 2f, y + 1.5f, 0f), new Color(0.3f, 0.92f, 1f), 1.3f, 4f);
        }

        private static void Shard(Transform root, Transform lights, Vector2Int c, Sprite sprite)
        {
            var go = new GameObject("Fragmento_Eco");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(c.x + 0.5f, c.y + 0.6f, 0f);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.45f;
            var sr = Visual(go.transform, sprite, 6);
            go.AddComponent<EchoShard>().Configure(sr.transform);
            var l = Light(go.transform, "Brillo", go.transform.position, new Color(0.3f, 0.92f, 1f), 0.9f, 2.2f);
            l.transform.localPosition = Vector3.zero;
        }

        private static void Arena(Map m, Transform root, List<(Vector2Int cell, GameObject go)> enemies)
        {
            var cells = m.Find('G').ToList();
            if (cells.Count == 0) return;
            var gates = new List<GameObject>();
            foreach (var col in cells.GroupBy(c => c.x))
            {
                int y0 = col.Min(c => c.y), y1 = col.Max(c => c.y) + 1;
                var gate = new GameObject("Compuerta") { layer = TerrainLayer };
                gate.transform.SetParent(root);
                gate.transform.position = new Vector3(col.Key + 0.5f, y0, 0f);
                var box = gate.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1f, y1 - y0); box.offset = new Vector2(0f, (y1 - y0) / 2f);
                for (int y = 0; y < y1 - y0; y++)
                {
                    var sr = Visual(gate.transform, S($"tile_jungle_wall_grey_r{y % 4}_c0"), 3);
                    sr.transform.localPosition = new Vector3(0f, y + 0.5f, 0f);
                    sr.color = new Color(0.55f, 1f, 1f);
                }
                gate.SetActive(false);
                gates.Add(gate);
            }
            int minX = cells.Min(c => c.x), maxX = cells.Max(c => c.x);
            int minY = cells.Min(c => c.y), maxY = cells.Max(c => c.y) + 1;
            var arena = new GameObject("Arena_Emboscada");
            arena.transform.SetParent(root);
            float left = minX + 4f, right = maxX - 1f;
            arena.transform.position = new Vector3((left + right) / 2f, (minY + maxY) / 2f, 0f);
            var trigger = arena.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true; trigger.size = new Vector2(right - left, maxY - minY);
            var inside = enemies.Where(e => e.cell.x > minX && e.cell.x < maxX).Select(e => e.go).ToArray();
            arena.AddComponent<ArenaLock>().Configure(gates.ToArray(), inside);

            var room = new GameObject("Sala_Arena");
            room.transform.SetParent(root);
            var poly = room.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            float rx0 = minX, rx1 = maxX + 1f, ry0 = 0f, ry1 = Mathf.Max(maxY + 1f, 12.5f);
            poly.points = new[] { new Vector2(rx0, ry0), new Vector2(rx0, ry1), new Vector2(rx1, ry1), new Vector2(rx1, ry0) };
            room.AddComponent<CameraRoom2D>();
        }

        private static void Goal(Map m, Transform lights)
        {
            Vector2Int x = m.Find('X').First();
            GameObject goal = GameObject.Find("Meta_Mirador_Nivel1");
            if (goal == null)
            {
                goal = new GameObject("Meta_Nivel");
                var box = goal.AddComponent<BoxCollider2D>();
                box.isTrigger = true; box.size = new Vector2(1.5f, 3f);
                goal.AddComponent<LevelGoal>();
            }
            goal.name = "Meta_Nivel";
            goal.transform.position = new Vector3(x.x + 0.5f, x.y + 1.5f, 0f);
            var lg = goal.GetComponent<LevelGoal>();
            if (lg != null)
            {
                SetField(lg, "levelTitle", m.Completed);
                SetField(lg, "levelSubtitle", string.IsNullOrEmpty(m.Next) ? "Fin de la demo · gracias por jugar" : "Siguiente: " + m.Next.Replace('_', ' '));
            }
            var exit = GetOrAdd<NextLevelExit>(goal);
            exit.Configure(m.Next);
            EditorUtility.SetDirty(goal);
            Light(lights, "Luz_Meta", goal.transform.position, new Color(0.3f, 0.92f, 1f), 1.8f, 7f);
        }

        private static void Signs(Map m)
        {
            var root = new GameObject("Carteles").transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var s in m.Signs)
            {
                var go = new GameObject("Cartel");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(s.x, s.y, 0f);
                var tm = go.AddComponent<TextMesh>();
                tm.text = s.text; tm.font = font; tm.fontSize = 48; tm.characterSize = 0.045f;
                tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.95f, 0.8f);
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = font.material; r.sortingOrder = 20;
            }
        }
    }
}
