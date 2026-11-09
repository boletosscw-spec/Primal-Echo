using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using PrimalEcho;
using PrimalEcho.Environment;
using System.Collections.Generic;

namespace PrimalEcho.Editor
{
    public class FirstMapBuilder
    {
        // Auto-construcción desactivada para que el usuario pueda pintar su propio mapa libremente con la Tile Palette.

        [MenuItem("Primal Echo/Actualizar Animaciones de Kael (19 Sprites Transparentes)")]
        public static void UpdateKaelAnimationsMenu()
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    ApplyKaelTransparentSprites(pc);
                    EditorUtility.SetDirty(pc);
                }
            }
        }

        [MenuItem("Primal Echo/Construir Primer Mapa (Valle del Eco)")]
        [MenuItem("Tools/Kael/Reconstruir Mapa y Parallax")]
        public static void BuildFirstMap()
        {
            ConfigureEnvironmentSprites();

            var scene = EditorSceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Construir Primer Mapa - Valle del Eco");
            int undoGroup = Undo.GetCurrentGroup();

            Transform envRoot = GetOrCreateRoot("=== ENTORNO ===");
            Transform lightRoot = GetOrCreateRoot("=== ILUMINACION ===");

            // 1. Setup Player first so Camera and Parallax align to starting view
            GameObject player = SetupPlayer( null );

            // 2. Setup Camera with Smooth Follow
            Camera cam = SetupCamera( null, player.transform );
            Vector3 startCamPos = cam != null ? cam.transform.position : new Vector3(-8f, 3.3f, -10f);

            // 3. Create Multi-Layer Seamless Parallax Backgrounds centered on Camera
            CreateParallaxBackgrounds(envRoot, startCamPos);

            // 4. Setup Grid & Tilemaps (Solid Ground + Decorative Water/Underside)
            GameObject existingGrid = GameObject.Find("Grid");
            if (existingGrid != null && existingGrid.transform.parent == null)
            {
                Undo.DestroyObjectImmediate(existingGrid);
            }

            GameObject gridObj = new GameObject("Grid", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Grid");
            gridObj.transform.SetParent(envRoot);
            gridObj.transform.position = Vector3.zero;
            Grid grid = gridObj.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // Main solid ground tilemap
            GameObject mapObj = new GameObject("mapa", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(CompositeCollider2D));
            mapObj.layer = LayerMask.NameToLayer("Terrain");
            Undo.RegisterCreatedObjectUndo(mapObj, "Create Tilemap");
            mapObj.transform.SetParent(gridObj.transform);
            mapObj.transform.position = Vector3.zero;

            Tilemap tilemap = mapObj.GetComponent<Tilemap>();
            TilemapRenderer renderer = mapObj.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;

            Rigidbody2D rb = mapObj.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            TilemapCollider2D tileCol = mapObj.GetComponent<TilemapCollider2D>();
            tileCol.compositeOperation = Collider2D.CompositeOperation.Merge;

            CompositeCollider2D compCol = mapObj.GetComponent<CompositeCollider2D>();
            compCol.geometryType = CompositeCollider2D.GeometryType.Polygons;

            // Decorative Tilemap (Water & hanging roots underside without collider)
            GameObject decoMapObj = new GameObject("Agua_Y_Detalles", typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(decoMapObj, "Create Deco Tilemap");
            decoMapObj.transform.SetParent(gridObj.transform);
            decoMapObj.transform.position = Vector3.zero;
            Tilemap decoTilemap = decoMapObj.GetComponent<Tilemap>();
            TilemapRenderer decoRenderer = decoMapObj.GetComponent<TilemapRenderer>();
            decoRenderer.sortingOrder = 5;
            decoTilemap.color = new Color(1f, 1f, 1f, 0.85f);

            // 5. Load Tiles and Paint Cohesive Level
            TileBase[] tiles = LoadTiles();
            PaintLevel(tilemap, decoTilemap, tiles);

            // 6. Place Grounded Relics, Fossils & Monuments
            CreateRelicsAndMonuments(envRoot);

            // 7. Place Grounded Nature Props (Willows, Trees, Bushes, Stones, Grass)
            CreateNatureProps(envRoot);

            // 8. Setup 2D Atmospheric Lighting
            SetupLighting(lightRoot);

            // 9. Setup Level Goal
            SetupLevelGoal(envRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("<color=#4CAF50><b>[Primal Echo]</b> ¡Mapa 'Valle del Eco' y sistema Parallax reconstruidos con éxito!</color>");
        }

        private static void ConfigureEnvironmentSprites()
        {
            string[] folders = new string[]
            {
                "Assets/Sprites/background",
                "Assets/Sprites/Environment/Backgrounds",
                "Assets/Sprites/Environment/Props",
                "Assets/Sprites/Environment/Relics"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }

                    // Ensure single-image backgrounds/props are in Single mode so pivot & PPU apply cleanly
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        changed = true;
                    }

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }

                    float targetPPU = 64f;
                    if (path.Contains("/background/"))
                    {
                        targetPPU = 100f;
                    }
                    else if (path.Contains("/Backgrounds/"))
                    {
                        targetPPU = path.Contains("Swamp") ? 32f : 100f;
                    }
                    else if (path.Contains("/Relics/"))
                    {
                        targetPPU = 64f;
                    }
                    else if (path.Contains("/Props/"))
                    {
                        if (path.Contains("Stones_")) targetPPU = 48f;
                        else targetPPU = 32f; // Trees, Willows, Bushes, Grass, Boxes, Fence, Pointers
                    }

                    if (Mathf.Abs(importer.spritePixelsPerUnit - targetPPU) > 0.1f)
                    {
                        importer.spritePixelsPerUnit = targetPPU;
                        changed = true;
                    }

                    // Center pivot for full-screen panorama/forest backgrounds; BottomCenter for grounded props/relics/trees
                    bool useCenterPivot = path.Contains("/background/") || path.Contains("Bosque pixelado") || path.Contains("arboles_parallax");
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);

                    if (useCenterPivot)
                    {
                        if (settings.spriteAlignment != (int)SpriteAlignment.Center)
                        {
                            settings.spriteAlignment = (int)SpriteAlignment.Center;
                            settings.spritePivot = new Vector2(0.5f, 0.5f);
                            importer.SetTextureSettings(settings);
                            changed = true;
                        }
                    }
                    else
                    {
                        if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
                        {
                            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                            settings.spritePivot = new Vector2(0.5f, 0f);
                            importer.SetTextureSettings(settings);
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object o in all)
            {
                if (o is Sprite sp) return sp;
            }
            return null;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                for (int i = existing.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(existing.transform.GetChild(i).gameObject);
                }
                return existing.transform;
            }

            GameObject root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create Root " + name);
            root.transform.position = Vector3.zero;
            return root.transform;
        }

        private static TileBase[] LoadTiles()
        {
            TileBase[] tiles = new TileBase[240];
            for (int i = 0; i < 224; i++)
            {
                string path = $"Assets/Tiles/Tiles1/Tileset_{i}.asset";
                tiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            return tiles;
        }

        /// <summary>
        /// Devuelve la altura Y exacta de la superficie superior del suelo en cada columna X.
        /// En Unity Tilemap, una celda (x, topY) ocupa de Y = topY a Y = topY + 1,
        /// por lo que su superficie caminable está en Y = topY + 1.
        /// </summary>
        private static int GetGroundTopTileY(int x)
        {
            if (x <= 7) return 0;          // Sección 1: Refugio del Eco (superficie Y = 1.0)
            if (x <= 10) return -1;        // Escalón 1 hacia el valle (superficie Y = 0.0)
            if (x <= 13) return -2;        // Escalón 2 hacia el valle (superficie Y = -1.0)
            if (x <= 34) return -4;        // Sección 2: Valle Inundado (superficie Y = -3.0)
            if (x <= 37) return 1;         // Escalón de ascenso a la meseta (superficie Y = 2.0)
            if (x <= 56) return 3;         // Sección 3: Meseta del Fósil (superficie Y = 4.0)
            if (x <= 72) return -6;        // Sección 4: Fondo del Cañón del Puente Roto (superficie Y = -5.0)
            if (x <= 76) return 5;         // Ascenso 1 al Mirador (superficie Y = 6.0)
            if (x <= 81) return 7;         // Ascenso 2 al Mirador (superficie Y = 8.0)
            if (x <= 98) return 9;         // Sección 5: Cumbre del Mirador (superficie Y = 10.0)
            return 11;                     // Muro final (superficie Y = 12.0)
        }

        private static float SurfaceY(int tileTopY)
        {
            // Restamos 0.04 unidades para que las raíces/bases se integren con el césped sin flotar
            return tileTopY + 1f - 0.04f;
        }

        private static void PaintLevel(Tilemap tilemap, Tilemap decoTilemap, TileBase[] t)
        {
            const int minX = -18;
            const int maxX = 108;
            const int minY = -10;
            const int maxY = 16;

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            bool[,] solid = new bool[width, height];

            bool IsSolid(int wx, int wy)
            {
                if (wx < minX) wx = minX;
                if (wx > maxX) wx = maxX;
                if (wy < minY) return true;
                if (wy > maxY) return false;
                return solid[wx - minX, wy - minY];
            }

            // 1. Marcar masa de terreno principal continua (de minY hasta topY de cada columna)
            for (int x = minX; x <= maxX; x++)
            {
                int topY = (x < -12) ? 3 : GetGroundTopTileY(x);
                for (int y = minY; y <= topY; y++)
                {
                    solid[x - minX, y - minY] = true;
                }
            }

            // Tiles verificados de Tileset.png (320x192, 16x16):
            TileBase grassTopLeft = t[0];
            TileBase[] grassTopMid = new TileBase[] { t[1], t[2], t[3], t[4] };
            TileBase grassTopRight = t[5];
            TileBase singleCap = t[166] ?? t[2];

            TileBase[] leftWall = new TileBase[] { t[20], t[34], t[54], t[74] };
            TileBase[] rightWall = new TileBase[] { t[25], t[39], t[59], t[79] };

            TileBase innerCornerLeft = t[46] ?? t[21];
            TileBase innerCornerRight = t[50] ?? t[24];

            TileBase bottomLeft = t[94];
            TileBase[] bottomMid = new TileBase[] { t[95], t[96], t[97], t[98] };
            TileBase bottomRight = t[99];

            // Relleno subterráneo orgánico (tierra oscura + guijarros suaves)
            TileBase[] darkEarth = new TileBase[]
            {
                t[7], t[8], t[10], t[11], t[31], t[40], t[45], t[60], t[65],
                t[21], t[22], t[23], t[24], t[36], t[37], t[56], t[57]
            };

            // 2. Auto-tile inteligente del terreno sólido según sus vecinos
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!IsSolid(x, y)) continue;

                    bool up = IsSolid(x, y + 1);
                    bool down = IsSolid(x, y - 1);
                    bool left = IsSolid(x - 1, y);
                    bool right = IsSolid(x + 1, y);
                    bool upLeft = IsSolid(x - 1, y + 1);
                    bool upRight = IsSolid(x + 1, y + 1);

                    TileBase chosen;
                    int hash = Mathf.Abs(x * 37 + y * 13);

                    if (!up)
                    {
                        if (!left && !right) chosen = singleCap;
                        else if (!left) chosen = grassTopLeft;
                        else if (!right) chosen = grassTopRight;
                        else chosen = grassTopMid[Mathf.Abs(x) % grassTopMid.Length];
                    }
                    else if (!down && y > minY)
                    {
                        if (!left) chosen = bottomLeft;
                        else if (!right) chosen = bottomRight;
                        else chosen = bottomMid[Mathf.Abs(x) % bottomMid.Length];
                    }
                    else if (!left)
                    {
                        chosen = leftWall[Mathf.Abs(y) % leftWall.Length];
                    }
                    else if (!right)
                    {
                        chosen = rightWall[Mathf.Abs(y) % rightWall.Length];
                    }
                    else if (!upLeft)
                    {
                        chosen = innerCornerLeft;
                    }
                    else if (!upRight)
                    {
                        chosen = innerCornerRight;
                    }
                    else
                    {
                        chosen = darkEarth[hash % darkEarth.Length];
                    }

                    tilemap.SetTile(new Vector3Int(x, y, 0), chosen);
                }
            }

            // 3. Plataformas flotantes con relieve de roca musgosa (2 tiles de grosor visual, 1 tile colisionable)
            TileBase platThinL = t[12];
            TileBase[] platThinM = new TileBase[] { t[13], t[14], t[15], t[16] };
            TileBase platThinR = t[17];

            TileBase islTopL = t[116];
            TileBase[] islTopM = new TileBase[] { t[117], t[118] };
            TileBase islTopR = t[119];

            TileBase islBotL = t[136];
            TileBase[] islBotM = new TileBase[] { t[137], t[138] };
            TileBase islBotR = t[139];

            void CreateMossyIsland(int startX, int length, int y, bool thickUnderside = true)
            {
                for (int i = 0; i < length; i++)
                {
                    int x = startX + i;
                    if (thickUnderside && length >= 3)
                    {
                        TileBase topT = (i == 0) ? islTopL : (i == length - 1 ? islTopR : islTopM[i % islTopM.Length]);
                        TileBase botT = (i == 0) ? islBotL : (i == length - 1 ? islBotR : islBotM[i % islBotM.Length]);
                        tilemap.SetTile(new Vector3Int(x, y, 0), topT);
                        decoTilemap.SetTile(new Vector3Int(x, y - 1, 0), botT);
                    }
                    else
                    {
                        TileBase pTile = (i == 0) ? platThinL : (i == length - 1 ? platThinR : platThinM[i % platThinM.Length]);
                        tilemap.SetTile(new Vector3Int(x, y, 0), pTile);
                    }
                }
            }

            // Islas flotantes sobre el Valle Inundado (Sección 2)
            CreateMossyIsland(15, 3, -1, true);
            CreateMossyIsland(20, 4, 1, true);
            CreateMossyIsland(26, 3, 2, true);
            CreateMossyIsland(30, 4, 0, true);

            // Pasarelas elevadas sobre la Meseta del Fósil (Sección 3)
            CreateMossyIsland(41, 4, 6, false);
            CreateMossyIsland(48, 5, 7, true);

            // Pilares suspendidos en el Cañón del Puente Roto (Sección 4)
            CreateMossyIsland(58, 3, 4, true);
            CreateMossyIsland(63, 4, 5, true);
            CreateMossyIsland(69, 3, 6, true);

            // 4. Agua de pantano con nenúfares en las cuencas bajas (capa decorativa sin colisión)
            TileBase[] waterSurface = new TileBase[] { t[18], t[19], t[52], t[53], t[92], t[93] };
            TileBase[] waterDeep = new TileBase[] { t[32], t[33], t[72], t[73], t[112], t[113] };

            // Cuenca inundada de la Sección 2 (x = 14..34, suelo en y = -4 -> agua en y = -3)
            for (int x = 14; x <= 34; x++)
            {
                decoTilemap.SetTile(new Vector3Int(x, -3, 0), waterSurface[Mathf.Abs(x) % waterSurface.Length]);
            }

            // Laguna profunda bajo el Puente Roto en la Sección 4 (x = 57..72, suelo en y = -6 -> agua en y = -4 y -5)
            for (int x = 57; x <= 72; x++)
            {
                decoTilemap.SetTile(new Vector3Int(x, -4, 0), waterSurface[Mathf.Abs(x) % waterSurface.Length]);
                decoTilemap.SetTile(new Vector3Int(x, -5, 0), waterDeep[Mathf.Abs(x) % waterDeep.Length]);
            }
        }

        private static void CreateParallaxBackgrounds(Transform root, Vector3 startCamPos)
        {
            GameObject bgGroup = new GameObject("Fondo_Parallax");
            Undo.RegisterCreatedObjectUndo(bgGroup, "Create Parallax Group");
            bgGroup.transform.SetParent(root);
            bgGroup.transform.position = Vector3.zero;

            float cx = startCamPos.x;
            float cy = startCamPos.y;

            // 1. Panorama principal del Valle de Ruinas y Cascadas al Atardecer (fondo.png, 1672x941)
            // Ajustado exactamente al ancho y límite superior de la cámara (sin sobresalir por arriba ni por debajo del suelo)
            CreateBgLayer(
                bgGroup.transform,
                "Assets/Sprites/background/fondo.png",
                "01_Panorama_Valle",
                new Vector3(cx, cy + 0.8f, 30f),
                new Vector2(0.92f, 1.0f),
                new Vector3(1.28f, 1.12f, 1f),
                -100,
                Color.white,
                mirrorAlternate: true
            );

            // 2. Dosel de Bosque Tropical Pixelado Transparente (1672x941) - 100% OPACO y encajado en la cámara
            CreateBgLayer(
                bgGroup.transform,
                "Assets/Sprites/Environment/Backgrounds/Bosque pixelado tropical transparente.png",
                "02_Bosque_Tropical_Parallax",
                new Vector3(cx, cy + 0.8f, 24f),
                new Vector2(0.72f, 1.0f),
                new Vector3(1.28f, 1.12f, 1f),
                -80,
                Color.white,
                mirrorAlternate: true
            );

            // 3. Vegetación cercana en silueta apoyada justo sobre el nivel del suelo (sin sobresalir)
            CreateBgLayer(
                bgGroup.transform,
                "Assets/Sprites/Environment/Backgrounds/SwampNearTrees.png",
                "03_Vegetacion_Cercana",
                new Vector3(cx, 0.85f, 8f),
                new Vector2(0.48f, 1.0f),
                new Vector3(1.185f, 0.85f, 1f),
                -55,
                Color.white,
                mirrorAlternate: true
            );
        }

        private static GameObject CreateBgLayer(
            Transform parent,
            string path,
            string name,
            Vector3 pos,
            Vector2 followMultiplier,
            Vector3 scale,
            int order,
            Color tint,
            bool mirrorAlternate = true)
        {
            Sprite sprite = LoadSprite(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[FirstMapBuilder] No se encontró el sprite de fondo: {path}");
                return null;
            }

            GameObject obj = new GameObject(name, typeof(SpriteRenderer), typeof(ParallaxLayer));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = scale;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = tint;

            float localStepX = sprite.bounds.size.x;
            float worldWidth = localStepX * Mathf.Abs(scale.x);

            ParallaxLayer pl = obj.GetComponent<ParallaxLayer>();
            pl.Configure(followMultiplier, infinite: true, mirrorAlternate: mirrorAlternate, width: worldWidth);

            return obj;
        }

        private static void CreateRelicsAndMonuments(Transform root)
        {
            GameObject relicsGroup = new GameObject("Reliquias_Y_Monumentos");
            Undo.RegisterCreatedObjectUndo(relicsGroup, "Create Relics Group");
            relicsGroup.transform.SetParent(root);
            relicsGroup.transform.position = Vector3.zero;

            // === SECCIÓN 1: Refugio del Eco (tile topY = 0 -> superficie Y = 1.0) ===
            float s1Y = SurfaceY(0);
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ArbolDelEco.png", "ArbolDelEco_Refugio", new Vector3(-9.5f, s1Y, 0f), -8, new Vector3(1.15f, 1.15f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/BalizaRefugio.png", "BalizaRefugio", new Vector3(-5.5f, s1Y, 0f), 2, new Vector3(1.05f, 1.05f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ArcoRuinado.png", "ArcoRuinado_Entrada", new Vector3(5.2f, s1Y, 0f), -5, new Vector3(1.25f, 1.25f, 1f));

            // === SECCIÓN 2: Valle Inundado (tile topY = -4 -> superficie Y = -3.0) ===
            float s2Y = SurfaceY(-4);
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/RaizTecnologica.png", "RaizTecnologica_Entrada", new Vector3(12f, SurfaceY(-2), 0f), -6, new Vector3(1.1f, 1.1f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/HongosDeLuz.png", "HongosDeLuz_Valle1", new Vector3(18f, s2Y, 0f), 3, new Vector3(0.95f, 0.95f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/NidoMecanico.png", "NidoMecanico_Pantano", new Vector3(24f, s2Y, 0f), -2, new Vector3(1.0f, 1.0f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/HongosDeLuz.png", "HongosDeLuz_Valle2", new Vector3(29f, s2Y, 0f), 3, new Vector3(1.05f, 1.05f, 1f), true);
            // Cristal sobre la isla flotante (x = 20..23, tile y = 1 -> superficie Y = 2.0)
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/CristalesEnergia.png", "CristalesEnergia_1", new Vector3(21.5f, SurfaceY(1), 0f), -1, new Vector3(0.75f, 0.75f, 1f));

            // === SECCIÓN 3: Meseta del Coloso Fósil (tile topY = 3 -> superficie Y = 4.0) ===
            float s3Y = SurfaceY(3);
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ColumnaAncestral.png", "Columna_1", new Vector3(39f, s3Y, 0f), -5, new Vector3(1.1f, 1.1f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/TerminalAncestral.png", "TerminalAncestral", new Vector3(41.2f, s3Y, 0f), -1, new Vector3(0.95f, 0.95f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/FosilDinorobot.png", "Fosil_Dinorobot_Ancestral", new Vector3(46.5f, s3Y, 0f), -4, new Vector3(1.45f, 1.45f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/CristalesEnergia.png", "CristalesEnergia_2", new Vector3(51.2f, s3Y, 0f), -1, new Vector3(0.9f, 0.9f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/AntenaCaida.png", "AntenaCaida_Ruinas", new Vector3(54f, s3Y, 0f), -3, new Vector3(1.15f, 1.15f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ColumnaAncestral.png", "Columna_2", new Vector3(55.8f, s3Y, 0f), -5, new Vector3(1.15f, 1.15f, 1f), true);
            // Vestigios en la pasarela superior (tile y = 7 -> superficie Y = 8.0)
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/RestosMecanicos.png", "RestosMecanicos_Pasarela", new Vector3(50.5f, SurfaceY(7), 0f), -2, new Vector3(0.85f, 0.85f, 1f));

            // === SECCIÓN 4: Abismo del Puente Roto (pilares en y = 4, 5, 6) ===
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/PuenteRuinado.png", "PuenteRuinado", new Vector3(65f, SurfaceY(5) - 1.1f, 0f), -2, new Vector3(1.25f, 1.25f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/CristalesEnergia.png", "CristalesEnergia_3", new Vector3(65f, SurfaceY(5), 0f), -1, new Vector3(0.75f, 0.75f, 1f));

            // === SECCIÓN 5: Acantilado y Cumbre del Mirador (tile topY = 9 -> superficie Y = 10.0) ===
            float s5Y = SurfaceY(9);
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ArcoRuinado.png", "ArcoRuinado_Mirador", new Vector3(79f, SurfaceY(7), 0f), -5, new Vector3(1.2f, 1.2f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ColumnaAncestral.png", "Columna_Mirador_1", new Vector3(84.5f, s5Y, 0f), -4, new Vector3(1.15f, 1.15f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ReleAncestral.png", "ReleAncestral_Mirador", new Vector3(88f, s5Y, 0f), 2, new Vector3(1.3f, 1.3f, 1f));
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ColumnaAncestral.png", "Columna_Mirador_2", new Vector3(91.5f, s5Y, 0f), -4, new Vector3(1.15f, 1.15f, 1f), true);
            CreateProp(relicsGroup.transform, "Assets/Sprites/Environment/Relics/ArbolDelEco.png", "ArbolDelEco_Cumbre", new Vector3(95.5f, s5Y, 0f), -8, new Vector3(1.25f, 1.25f, 1f), true);
        }

        private static void CreateNatureProps(Transform root)
        {
            GameObject natureGroup = new GameObject("Vegetacion_Y_Props");
            Undo.RegisterCreatedObjectUndo(natureGroup, "Create Nature Group");
            natureGroup.transform.SetParent(root);
            natureGroup.transform.position = Vector3.zero;

            // 1. Sauces Colosales (Willows) sobre la superficie real del terreno
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Willows_1.png", "Sauce_Refugio", new Vector3(-11f, SurfaceY(0), 0f), -15, new Vector3(1.1f, 1.1f, 1f));
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Willows_2.png", "Sauce_Valle", new Vector3(16f, SurfaceY(-4), 0f), -15, new Vector3(1.15f, 1.15f, 1f));
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Willows_3.png", "Sauce_Meseta", new Vector3(48.5f, SurfaceY(3), 0f), -15, new Vector3(1.15f, 1.15f, 1f));
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Willows_1.png", "Sauce_Mirador", new Vector3(82.8f, SurfaceY(9), 0f), -15, new Vector3(1.15f, 1.15f, 1f), true);

            // 2. Árboles Ancestrales
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Trees_1.png", "Arbol_1", new Vector3(1.5f, SurfaceY(0), 0f), -12, new Vector3(1.1f, 1.1f, 1f));
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Trees_2.png", "Arbol_2", new Vector3(32.5f, SurfaceY(-4), 0f), -12, new Vector3(1.1f, 1.1f, 1f));
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Trees_3.png", "Arbol_3", new Vector3(74.8f, SurfaceY(5), 0f), -12, new Vector3(1.1f, 1.1f, 1f));

            // 3. Rocas Musgosas
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Stones_1.png", "Roca_1", new Vector3(-2.2f, SurfaceY(0), 0f), 1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Stones_2.png", "Roca_2", new Vector3(14.8f, SurfaceY(-4), 0f), 1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Stones_3.png", "Roca_3", new Vector3(33.8f, SurfaceY(-4), 0f), 1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Stones_4.png", "Roca_4", new Vector3(43.5f, SurfaceY(3), 0f), 1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Stones_5.png", "Roca_5", new Vector3(78f, SurfaceY(7), 0f), 1, Vector3.one);

            // 4. Arbustos Frondosos
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_5.png", "Arbusto_1", new Vector3(-3.8f, SurfaceY(0), 0f), -2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_7.png", "Arbusto_2", new Vector3(3.2f, SurfaceY(0), 0f), -2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_8.png", "Arbusto_3", new Vector3(20.5f, SurfaceY(-4), 0f), -2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_6.png", "Arbusto_4", new Vector3(40.2f, SurfaceY(3), 0f), -2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_9.png", "Arbusto_5", new Vector3(52.5f, SurfaceY(3), 0f), -2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Bushes_4.png", "Arbusto_6", new Vector3(85.8f, SurfaceY(9), 0f), -2, Vector3.one);

            // 5. Hierbas y Cañas de Ribera bien apoyadas sobre el césped
            int[] grassX = new int[] { -10, -7, -4, -1, 2, 4, 6, 9, 15, 19, 22, 27, 31, 36, 40, 44, 48, 52, 74, 78, 83, 86, 90, 93 };
            for (int i = 0; i < grassX.Length; i++)
            {
                int gx = grassX[i];
                float gy = SurfaceY(GetGroundTopTileY(gx));
                int grassNum = (i % 10) + 1;
                CreateProp(natureGroup.transform, $"Assets/Sprites/Environment/Props/Grass_{grassNum}.png", $"Hierba_{i + 1}", new Vector3(gx + 0.5f, gy, 0f), 4, Vector3.one);
            }

            // 6. Vallas, Cajas y Señalizaciones del Refugio
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Fence_1.png", "Valla_Refugio_1", new Vector3(-0.8f, SurfaceY(0), 0f), -1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Fence_2.png", "Valla_Refugio_2", new Vector3(0.1f, SurfaceY(0), 0f), -1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Pointers_1.png", "Cartel_Guia", new Vector3(6.6f, SurfaceY(0), 0f), 2, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Boxes_1.png", "Cajas_Suministros_1", new Vector3(-6.8f, SurfaceY(0), 0f), 1, Vector3.one);
            CreateProp(natureGroup.transform, "Assets/Sprites/Environment/Props/Boxes_3.png", "Cajas_Suministros_2", new Vector3(-6.0f, SurfaceY(0), 0f), 2, new Vector3(0.85f, 0.85f, 1f));
        }

        private static GameObject CreateProp(Transform parent, string path, string name, Vector3 pos, int order, Vector3 scale, bool flipX = false)
        {
            Sprite sprite = LoadSprite(path);
            if (sprite == null) return null;

            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            Undo.RegisterCreatedObjectUndo(obj, "Create Prop " + name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = scale;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.flipX = flipX;

            return obj;
        }

        private static void SetupLighting(Transform root)
        {
            GameObject globalLightObj = new GameObject("Luz_Global_2D", typeof(Light2D));
            Undo.RegisterCreatedObjectUndo(globalLightObj, "Create Global Light");
            globalLightObj.transform.SetParent(root);
            globalLightObj.transform.position = Vector3.zero;

            Light2D globalLight = globalLightObj.GetComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(0.88f, 0.90f, 0.96f, 1f);
            globalLight.intensity = 0.95f;

            // Luces puntuales sobre la altura real de las reliquias
            CreatePointLight(root, "Luz_BalizaRefugio", new Vector3(-5.5f, SurfaceY(0) + 1.8f, 0f), new Color(0.3f, 0.95f, 1f, 1f), 1.6f, 6.5f, 1.5f);
            CreatePointLight(root, "Luz_Hongos1", new Vector3(18f, SurfaceY(-4) + 1.2f, 0f), new Color(0.3f, 1f, 0.85f, 1f), 1.3f, 4.5f, 2.0f);
            CreatePointLight(root, "Luz_Hongos2", new Vector3(29f, SurfaceY(-4) + 1.2f, 0f), new Color(0.3f, 1f, 0.75f, 1f), 1.3f, 4.5f, 1.8f);
            CreatePointLight(root, "Luz_Cristal1", new Vector3(21.5f, SurfaceY(1) + 1.2f, 0f), new Color(0f, 0.95f, 0.9f, 1f), 1.5f, 5f, 2f);
            CreatePointLight(root, "Luz_Cristal2", new Vector3(51.2f, SurfaceY(3) + 1.4f, 0f), new Color(0f, 0.95f, 0.9f, 1f), 1.6f, 5.5f, 2f);
            CreatePointLight(root, "Luz_Cristal3", new Vector3(65f, SurfaceY(5) + 1.2f, 0f), new Color(0f, 0.95f, 0.9f, 1f), 1.4f, 4.5f, 2f);
            CreatePointLight(root, "Luz_Terminal", new Vector3(41.2f, SurfaceY(3) + 1.2f, 0f), new Color(1f, 0.75f, 0.35f, 1f), 1.2f, 3.8f, 2.5f);
            CreatePointLight(root, "Luz_ReleMirador", new Vector3(88f, SurfaceY(9) + 2.2f, 0f), new Color(0.35f, 0.9f, 1f, 1f), 2.2f, 9f, 1.2f);
        }

        private static GameObject CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float radius, float pulseSpeed)
        {
            GameObject obj = new GameObject(name, typeof(Light2D), typeof(AtmosphereGlow));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;

            Light2D light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.2f;

            AtmosphereGlow glow = obj.GetComponent<AtmosphereGlow>();
            glow.SetGlow(intensity * 0.75f, intensity * 1.25f, pulseSpeed);

            return obj;
        }

        private static GameObject SetupPlayer(Transform root)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
            }
            if (root != null) player.transform.SetParent(root);
            // Colocar a Kael sobre la superficie Y = 1.0 del Refugio
            player.transform.position = new Vector3(-8f, 1.85f, 0f);
            player.tag = "Player";

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>() ?? player.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            CapsuleCollider2D col = player.GetComponent<CapsuleCollider2D>() ?? player.AddComponent<CapsuleCollider2D>();
            col.offset = new Vector2(0f, 0f);
            col.size = new Vector2(0.6f, 1.35f);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1f;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;

            PlayerController pc = player.GetComponent<PlayerController>() ?? player.AddComponent<PlayerController>();
            ApplyKaelTransparentSprites(pc);
            if (!player.GetComponent<KaelSpear>()) player.AddComponent<KaelSpear>();
            if (!player.GetComponent<KaelHealth>()) player.AddComponent<KaelHealth>();

            // Asegurar Animator + KaelAnimationDriver conectados
            Animator anim = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Kael/Kael.controller");
            if (controller != null) anim.runtimeAnimatorController = controller;

            KaelAnimationDriver driver = player.GetComponent<KaelAnimationDriver>() ?? player.AddComponent<KaelAnimationDriver>();
            GameObject arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arrow.prefab");
            if (arrowPrefab != null)
            {
                SerializedObject so = new SerializedObject(driver);
                SerializedProperty arrowProp = so.FindProperty("arrowPrefab");
                if (arrowProp != null)
                {
                    arrowProp.objectReferenceValue = arrowPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return player;
        }

        private static void ApplyKaelTransparentSprites(PlayerController pc)
        {
            const string baseDir = "Assets/Sprites/Personajes/Kael_Primal_Echo_Animaciones_Finales/";
            Sprite[] idle = LoadSpriteFrames(baseDir + "01_Movimiento/Kael_Idle_06frames.png", "Kael_Idle");
            Sprite[] run = LoadSpriteFrames(baseDir + "01_Movimiento/Kael_Correr_08frames.png", "Kael_Correr");
            Sprite[] jumpRising = LoadSpriteFrames(baseDir + "01_Movimiento/Kael_Salto_03frames.png", "Kael_Salto");
            Sprite[] fallDescending = LoadSpriteFrames(baseDir + "01_Movimiento/Kael_Caida_02frames.png", "Kael_Caida");
            Sprite[] land = LoadSpriteFrames(baseDir + "01_Movimiento/Kael_Aterrizaje_03frames.png", "Kael_Aterrizaje");
            Sprite[] hurt = LoadSpriteFrames(baseDir + "02_Combate/Kael_Danio_03frames.png", "Kael_Danio");
            Sprite[] death = LoadSpriteFrames(baseDir + "02_Combate/Kael_Muerte_07frames.png", "Kael_Muerte");
            Sprite[] victory = LoadSpriteFrames(baseDir + "03_Acciones/Kael_Victoria_Sin_Numeros_05frames.png", "Kael_Victoria");
            Sprite[] spawn = LoadSpriteFrames(baseDir + "03_Acciones/Kael_Interactuar_05frames.png", "Kael_Interactuar");

            SpriteRenderer sr = pc.GetComponent<SpriteRenderer>();
            if (sr != null && idle != null && idle.Length > 0)
            {
                sr.sprite = idle[0];
            }

            pc.SetSprites(idle, run, jumpRising, land, fallDescending);
            pc.SetExtraSprites(hurt, death, victory, spawn);
        }

        private static Sprite[] LoadSpriteFrames(string path, string prefix)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> list = new List<Sprite>();
            foreach (var a in allAssets)
            {
                if (a is Sprite s && s.name.StartsWith(prefix))
                {
                    if (s.rect.width >= 40f && s.rect.height >= 40f)
                    {
                        list.Add(s);
                    }
                }
            }
            list.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));
            return list.ToArray();
        }

        private static int ExtractNumber(string name)
        {
            int idx = name.LastIndexOf('_');
            if (idx >= 0 && int.TryParse(name.Substring(idx + 1), out int val))
            {
                return val;
            }
            return 0;
        }

        private static Camera SetupCamera(Transform root, Transform playerTarget)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = GameObject.Find("Main Camera") ?? new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
                cam = camObj.GetComponent<Camera>();
                cam.tag = "MainCamera";
            }

            if (root != null) cam.transform.SetParent(root);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.14f, 0.16f, 0.25f, 1f);

            CameraFollow2D follow = cam.GetComponent<CameraFollow2D>() ?? cam.gameObject.AddComponent<CameraFollow2D>();
            follow.SetBounds(new Vector2(-8f, -1.5f), new Vector2(94f, 16f));
            follow.SetTarget(playerTarget);

            return cam;
        }

        private static void SetupLevelGoal(Transform root)
        {
            GameObject goalObj = new GameObject("Meta_Mirador_Nivel1", typeof(BoxCollider2D), typeof(LevelGoal));
            Undo.RegisterCreatedObjectUndo(goalObj, "Create Level Goal");
            goalObj.transform.SetParent(root);
            goalObj.transform.position = new Vector3(88f, SurfaceY(9) + 1.5f, 0f);

            BoxCollider2D col = goalObj.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 4f);
        }
    }
}
