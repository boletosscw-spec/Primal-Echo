using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace PrimalEcho.Editor
{
    [InitializeOnLoad]
    public static class KaelAnimationImporter
    {
        public const float DEFAULT_FPS = 10f;
        public const float PROJECT_PPU = 270f;

        private const string BASE_FOLDER = "Assets/Sprites/Personajes/Kael_Primal_Echo_Animaciones_Finales";
        private const string INVENTORY_PATH = BASE_FOLDER + "/inventario.json";
        private const string ANIM_OUTPUT_FOLDER = "Assets/Animations/Kael";
        private const string CONTROLLER_PATH = ANIM_OUTPUT_FOLDER + "/Kael.controller";
        private const string PREFABS_FOLDER = "Assets/Prefabs";
        private const string ARROW_PREFAB_PATH = PREFABS_FOLDER + "/Arrow.prefab";

        [Serializable]
        private class InventarioItem
        {
            public string nombre;
            public string categoria;
            public int fotogramas_solicitados;
            public int ancho_px;
            public int alto_px;
            public int ancho_frame_px;
            public int alto_frame_px;
            public float fps;
            public bool loop;
            public string sha256;
        }

        [Serializable]
        private class InventarioWrapper
        {
            public List<InventarioItem> items;
        }

        static KaelAnimationImporter()
        {
            EditorApplication.delayCall += AutoRunIfNotImported;
        }

        private static void AutoRunIfNotImported()
        {
            PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (pc != null && pc.GetComponent<KaelAnimationDriver>() == null)
                {
                    pc.gameObject.AddComponent<KaelAnimationDriver>();
                }
                return;
            }
            bool missingDriver = pc != null && pc.GetComponent<KaelAnimationDriver>() == null;
            if (!File.Exists(CONTROLLER_PATH) || !File.Exists(ARROW_PREFAB_PATH) || missingDriver)
            {
                ImportAllKaelAnimations();
            }
        }

        [MenuItem("Tools/Kael/Importar animaciones")]
        public static void ImportAllKaelAnimations()
        {
            EnsureDirectory(ANIM_OUTPUT_FOLDER);
            EnsureDirectory(PREFABS_FOLDER);

            List<InventarioItem> inventory = LoadInventory();
            if (inventory == null || inventory.Count == 0)
            {
                Debug.LogError("[KaelAnimationImporter] No se pudo leer inventario.json en: " + INVENTORY_PATH);
                return;
            }

            Dictionary<string, AnimationClip> clipsByAnimName = new Dictionary<string, AnimationClip>();
            int createdClips = 0;
            int skippedUnchangedClips = 0;
            int conflictClips = 0;

            // PASO 2: Importar cada PNG de 01_Movimiento, 02_Combate y 03_Acciones
            foreach (var item in inventory)
            {
                if (item.categoria == "04_Proyectiles") continue;

                string assetPath = $"{BASE_FOLDER}/{item.categoria}/{item.nombre}";
                if (!File.Exists(assetPath))
                {
                    Debug.LogWarning($"[KaelAnimationImporter] Archivo del inventario no encontrado: {assetPath}");
                    continue;
                }

                string animName = GetCleanAnimationName(item.nombre);
                int numFrames = Mathf.Max(1, item.fotogramas_solicitados);
                int cellWidth = item.ancho_frame_px > 0 ? item.ancho_frame_px : (item.ancho_px / numFrames);
                int cellHeight = item.alto_frame_px > 0 ? item.alto_frame_px : item.alto_px;
                float fps = item.fps > 0f ? item.fps : DEFAULT_FPS;
                bool shouldLoop = DetermineLoop(animName, item);

                Sprite[] frames = ConfigureAndSliceSpriteSheet(
                    assetPath,
                    animName,
                    numFrames,
                    cellWidth,
                    cellHeight,
                    SpriteAlignment.BottomCenter,
                    new Vector2(0.5f, 0f)
                );

                if (frames == null || frames.Length == 0)
                {
                    Debug.LogError($"[KaelAnimationImporter] No se pudieron generar sprites para {assetPath}");
                    continue;
                }

                string clipPath = $"{ANIM_OUTPUT_FOLDER}/{animName}.anim";
                AnimationClip clip = CreateOrUpdateAnimationClip(
                    clipPath,
                    frames,
                    fps,
                    shouldLoop,
                    ref createdClips,
                    ref skippedUnchangedClips,
                    ref conflictClips
                );

                if (clip != null)
                {
                    clipsByAnimName[animName] = clip;
                }
            }

            // PASO 3: Crear / Actualizar Animator Controller (Assets/Animations/Kael/Kael.controller)
            AnimatorController controller = BuildKaelAnimatorController(CONTROLLER_PATH, clipsByAnimName);

            // PASO 4: Proyectiles (04_Proyectiles)
            ConfigureProjectiles(inventory, ref createdClips, ref skippedUnchangedClips, ref conflictClips);

            // PASO 5: Integración con el Player existente en la escena
            IntegrateWithScenePlayer(controller, clipsByAnimName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"<color=#4CAF50><b>[KaelAnimationImporter]</b> Importación completada: " +
                $"{clipsByAnimName.Count} hojas procesadas, {createdClips} clips creados/actualizados, " +
                $"{skippedUnchangedClips} clips sin cambios omitidos, {conflictClips} conflictos respetados.</color>"
            );
        }

        private static List<InventarioItem> LoadInventory()
        {
            if (!File.Exists(INVENTORY_PATH)) return null;
            string rawJson = File.ReadAllText(INVENTORY_PATH, Encoding.UTF8).Trim();
            string wrappedJson = "{\"items\":" + rawJson + "}";
            InventarioWrapper wrapper = JsonUtility.FromJson<InventarioWrapper>(wrappedJson);
            return wrapper?.items;
        }

        public static string GetCleanAnimationName(string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            // Quitar sufijo _XXframes
            int framesIdx = baseName.IndexOf("frames", StringComparison.OrdinalIgnoreCase);
            if (framesIdx >= 3 && baseName[framesIdx - 3] == '_')
            {
                baseName = baseName.Substring(0, framesIdx - 3);
            }
            baseName = baseName.Replace("_Sin_Numeros", "").Replace("_Reactivacion", "");
            return baseName;
        }

        private static bool DetermineLoop(string animName, InventarioItem item)
        {
            string lower = animName.ToLowerInvariant();
            if (lower.Contains("idle") ||
                lower.Contains("caminar") ||
                lower.Contains("correr") ||
                lower.Contains("caida") ||
                lower.Contains("trepar") ||
                lower.Contains("escalar"))
            {
                return true;
            }
            return item.loop;
        }

        private static Sprite[] ConfigureAndSliceSpriteSheet(
            string assetPath,
            string animName,
            int numFrames,
            int cellWidth,
            int cellHeight,
            SpriteAlignment alignment,
            Vector2 pivot)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = PROJECT_PPU;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.maxTextureSize = 8192;
            defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(defaultSettings);

            TextureImporterSettings tis = new TextureImporterSettings();
            importer.ReadTextureSettings(tis);
            tis.spriteMeshType = SpriteMeshType.FullRect;
            tis.spriteAlignment = (int)alignment;
            tis.spritePivot = pivot;
            tis.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(tis);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider != null)
            {
                dataProvider.InitSpriteEditorDataProvider();
                SpriteRect[] existingRects = dataProvider.GetSpriteRects();
                SpriteRect[] newRects = new SpriteRect[numFrames];

                for (int i = 0; i < numFrames; i++)
                {
                    string frameName = $"{animName}_{i}";
                    GUID existingGuid = GUID.Generate();
                    if (existingRects != null)
                    {
                        foreach (var er in existingRects)
                        {
                            if (er.name == frameName)
                            {
                                existingGuid = er.spriteID;
                                break;
                            }
                        }
                    }

                    newRects[i] = new SpriteRect
                    {
                        name = frameName,
                        spriteID = existingGuid,
                        rect = new Rect(i * cellWidth, 0f, cellWidth, cellHeight),
                        alignment = alignment,
                        pivot = pivot
                    };
                }

                dataProvider.SetSpriteRects(newRects);
                dataProvider.Apply();
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            UnityEngine.Object[] allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (var obj in allObjects)
            {
                if (obj is Sprite s)
                {
                    sprites.Add(s);
                }
            }

            sprites.Sort((a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));
            return sprites.ToArray();
        }

        private static int ExtractFrameIndex(string spriteName)
        {
            int lastUnderscore = spriteName.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(spriteName.Substring(lastUnderscore + 1), out int idx))
            {
                return idx;
            }
            return 0;
        }

        private static AnimationClip CreateOrUpdateAnimationClip(
            string clipPath,
            Sprite[] frames,
            float fps,
            bool loop,
            ref int createdCount,
            ref int skippedCount,
            ref int conflictCount)
        {
            string expectedSignature = ComputeExpectedClipSignature(frames, fps, loop);
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            if (existingClip != null)
            {
                if (IsClipMatchingExpected(existingClip, frames, fps, loop))
                {
                    skippedCount++;
                    return existingClip;
                }

                // Verificar si fue modificado a mano por el usuario
                string metaPath = clipPath + ".meta";
                string savedSignature = ReadSignatureFromMeta(metaPath);
                string currentActualSignature = ComputeActualClipSignature(existingClip);

                if (!string.IsNullOrEmpty(savedSignature) && savedSignature != currentActualSignature)
                {
                    conflictCount++;
                    Debug.LogWarning(
                        $"[KaelAnimationImporter] CONFLICTO DETECTADO: El clip '{clipPath}' fue modificado manualmente. " +
                        "No se sobrescribirá para preservar tus cambios."
                    );
                    return existingClip;
                }
            }

            AnimationClip clip = existingClip != null ? existingClip : new AnimationClip();
            clip.name = Path.GetFileNameWithoutExtension(clipPath);
            clip.frameRate = fps;

            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = frames.Length / fps;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (existingClip == null)
            {
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
            WriteSignatureToMeta(clipPath + ".meta", expectedSignature);
            createdCount++;
            return clip;
        }

        private static bool IsClipMatchingExpected(AnimationClip clip, Sprite[] frames, float fps, bool loop)
        {
            if (Mathf.Abs(clip.frameRate - fps) > 0.01f) return false;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime != loop) return false;

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length != frames.Length) return false;

            for (int i = 0; i < frames.Length; i++)
            {
                if (keys[i].value != frames[i]) return false;
                if (Mathf.Abs(keys[i].time - (i / fps)) > 0.001f) return false;
            }

            // Si el usuario agregó eventos de animación a mano, lo consideramos modificado
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events != null && events.Length > 0) return false;

            return true;
        }

        private static string ComputeExpectedClipSignature(Sprite[] frames, float fps, bool loop)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("fps:{0:F2}|loop:{1}|events:0|", fps, loop ? 1 : 0);
            foreach (var s in frames)
            {
                sb.Append(s != null ? s.name : "null").Append(",");
            }
            return ComputeSha256(sb.ToString());
        }

        private static string ComputeActualClipSignature(AnimationClip clip)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("fps:{0:F2}|loop:{1}|events:{2}|",
                clip.frameRate,
                settings.loopTime ? 1 : 0,
                events != null ? events.Length : 0);
            if (keys != null)
            {
                foreach (var k in keys)
                {
                    sb.Append(k.value != null ? k.value.name : "null").Append(",");
                }
            }
            return ComputeSha256(sb.ToString());
        }

        private static string ComputeSha256(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 8; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static void WriteSignatureToMeta(string metaPath, string signature)
        {
            if (!File.Exists(metaPath)) return;
            string content = File.ReadAllText(metaPath, Encoding.UTF8);
            string marker = "userData: KaelImporter_";
            if (content.Contains("userData:"))
            {
                string[] lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("userData:"))
                    {
                        lines[i] = "  " + marker + signature;
                    }
                }
                File.WriteAllText(metaPath, string.Join("\n", lines), Encoding.UTF8);
            }
        }

        private static string ReadSignatureFromMeta(string metaPath)
        {
            if (!File.Exists(metaPath)) return null;
            string[] lines = File.ReadAllLines(metaPath, Encoding.UTF8);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("userData: KaelImporter_"))
                {
                    return trimmed.Substring("userData: KaelImporter_".Length).Trim();
                }
            }
            return null;
        }

        private static AnimatorController BuildKaelAnimatorController(
            string controllerPath,
            Dictionary<string, AnimationClip> clips)
        {
            if (File.Exists(controllerPath))
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Parámetros requeridos en PASO 3
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);

            string[] triggers = new[]
            {
                "Jump",
                "Roll",
                "AttackSpear",
                "AttackSpearAir",
                "AttackBow",
                "AttackBowDiagonal",
                "Block",
                "Hurt",
                "Die",
                "Interact",
                "Victory",
                "RepairKiro"
            };
            foreach (var trig in triggers)
            {
                controller.AddParameter(trig, AnimatorControllerParameterType.Trigger);
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Crear todos los estados
            AnimatorState stIdle = AddClipState(sm, "Idle", clips, "Kael_Idle", new Vector3(250, 0, 0));
            AnimatorState stCaminar = AddClipState(sm, "Caminar", clips, "Kael_Caminar", new Vector3(520, -60, 0));
            AnimatorState stCorrer = AddClipState(sm, "Correr", clips, "Kael_Correr", new Vector3(780, -60, 0));
            AnimatorState stAgacharse = AddClipState(sm, "Agacharse", clips, "Kael_Agacharse", new Vector3(520, 80, 0));

            AnimatorState stSalto = AddClipState(sm, "Salto", clips, "Kael_Salto", new Vector3(250, -220, 0));
            AnimatorState stCaida = AddClipState(sm, "Caida", clips, "Kael_Caida", new Vector3(520, -220, 0));
            AnimatorState stAterrizaje = AddClipState(sm, "Aterrizaje", clips, "Kael_Aterrizaje", new Vector3(780, -220, 0));

            AnimatorState stTrepar = AddClipState(sm, "Trepar", clips, "Kael_Trepar", new Vector3(-20, -140, 0));
            AnimatorState stRodar = AddClipState(sm, "Rodar", clips, "Kael_Rodar", new Vector3(-20, 120, 0));

            AnimatorState stAtaqueLanza = AddClipState(sm, "Ataque_Lanza", clips, "Kael_Ataque_Lanza", new Vector3(250, 220, 0));
            AnimatorState stAtaqueAereo = AddClipState(sm, "Ataque_Aereo_Lanza", clips, "Kael_Ataque_Aereo_Lanza", new Vector3(520, -340, 0));
            AnimatorState stAtaqueArco = AddClipState(sm, "Ataque_Arco", clips, "Kael_Ataque_Arco", new Vector3(250, 300, 0));
            AnimatorState stArcoDiag = AddClipState(sm, "Arco_Diagonal_Arriba", clips, "Kael_Arco_Diagonal_Arriba", new Vector3(250, 380, 0));
            AnimatorState stBloquear = AddClipState(sm, "Bloquear", clips, "Kael_Bloquear", new Vector3(520, 220, 0));
            AnimatorState stDanio = AddClipState(sm, "Danio", clips, "Kael_Danio", new Vector3(520, 300, 0));
            AnimatorState stMuerte = AddClipState(sm, "Muerte", clips, "Kael_Muerte", new Vector3(-280, -220, 0));

            AnimatorState stInteractuar = AddClipState(sm, "Interactuar", clips, "Kael_Interactuar", new Vector3(520, 380, 0));
            AnimatorState stVictoria = AddClipState(sm, "Victoria", clips, "Kael_Victoria", new Vector3(250, 460, 0));
            AnimatorState stRepararKiro = AddClipState(sm, "Reparar_Kiro", clips, "Kael_Reparar_Kiro", new Vector3(520, 460, 0));

            sm.defaultState = stIdle;

            // 1. Idle <-> Caminar <-> Correr según Speed
            AddInstantTransition(stIdle, stCaminar, AnimatorConditionMode.Greater, 0.1f, "Speed");
            AddInstantTransition(stCaminar, stIdle, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddInstantTransition(stCaminar, stCorrer, AnimatorConditionMode.Greater, 0.6f, "Speed");
            AddInstantTransition(stCorrer, stCaminar, AnimatorConditionMode.Less, 0.6f, "Speed");
            AddInstantTransition(stIdle, stCorrer, AnimatorConditionMode.Greater, 0.6f, "Speed");
            AddInstantTransition(stCorrer, stIdle, AnimatorConditionMode.Less, 0.1f, "Speed");

            // 2. Idle <-> Agacharse con IsCrouching
            AddInstantTransition(stIdle, stAgacharse, AnimatorConditionMode.If, 0f, "IsCrouching");
            AddInstantTransition(stAgacharse, stIdle, AnimatorConditionMode.IfNot, 0f, "IsCrouching");

            // 3. Cualquier estado -> Salto con Jump; Salto -> Caída con VerticalVelocity < 0; Caída -> Aterrizaje con IsGrounded; Aterrizaje -> Idle al terminar
            var tJump = AddAnyStateTriggerTransition(sm, stSalto, "Jump");
            tJump.canTransitionToSelf = true; // Permite reiniciar Salto en el Doble Salto
            AddInstantTransition(stSalto, stCaida, AnimatorConditionMode.Less, 0f, "VerticalVelocity");
            var tSaltoGround = AddInstantTransition(stSalto, stAterrizaje, AnimatorConditionMode.If, 0f, "IsGrounded");
            tSaltoGround.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");
            AddInstantTransition(stCaida, stAterrizaje, AnimatorConditionMode.If, 0f, "IsGrounded");
            AddExitTimeTransition(stAterrizaje, stIdle);
            AddInstantTransition(stAterrizaje, stCorrer, AnimatorConditionMode.Greater, 0.6f, "Speed");
            AddInstantTransition(stAterrizaje, stCaminar, AnimatorConditionMode.Greater, 0.1f, "Speed");

            // También caída libre al salir de plataforma desde suelo
            var tIdleFall = AddInstantTransition(stIdle, stCaida, AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            tIdleFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");
            var tWalkFall = AddInstantTransition(stCaminar, stCaida, AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            tWalkFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");
            var tRunFall = AddInstantTransition(stCorrer, stCaida, AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            tRunFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");

            // 4. Trepar con IsClimbing
            var tClimb = sm.AddAnyStateTransition(stTrepar);
            tClimb.duration = 0f;
            tClimb.hasExitTime = false;
            tClimb.canTransitionToSelf = false;
            tClimb.AddCondition(AnimatorConditionMode.If, 0f, "IsClimbing");
            AddInstantTransition(stTrepar, stIdle, AnimatorConditionMode.IfNot, 0f, "IsClimbing");

            // 5. Rodar, ataques terrestres, daño, interactuar, victoria y reparar_kiro vuelven a Idle al terminar (Has Exit Time = true)
            AddAnyStateTriggerTransition(sm, stRodar, "Roll");
            AddExitTimeTransition(stRodar, stIdle);

            AddAnyStateTriggerTransition(sm, stAtaqueLanza, "AttackSpear");
            AddExitTimeTransition(stAtaqueLanza, stIdle);

            AddAnyStateTriggerTransition(sm, stAtaqueArco, "AttackBow");
            AddExitTimeTransition(stAtaqueArco, stIdle);

            AddAnyStateTriggerTransition(sm, stArcoDiag, "AttackBowDiagonal");
            AddExitTimeTransition(stArcoDiag, stIdle);

            AddAnyStateTriggerTransition(sm, stDanio, "Hurt");
            AddExitTimeTransition(stDanio, stIdle);

            AddAnyStateTriggerTransition(sm, stInteractuar, "Interact");
            AddExitTimeTransition(stInteractuar, stIdle);

            AddAnyStateTriggerTransition(sm, stVictoria, "Victory");
            AddExitTimeTransition(stVictoria, stIdle);

            AddAnyStateTriggerTransition(sm, stRepararKiro, "RepairKiro");
            AddExitTimeTransition(stRepararKiro, stIdle);

            // 6. Ataque aéreo con lanza vuelve a Caída al terminar
            AddAnyStateTriggerTransition(sm, stAtaqueAereo, "AttackSpearAir");
            AddExitTimeTransition(stAtaqueAereo, stCaida);

            // 7. Bloquear: entra con trigger Block (o bool IsBlocking) y se mantiene en el último frame mientras IsBlocking sea true
            AddAnyStateTriggerTransition(sm, stBloquear, "Block");
            var tBlockEnd = AddExitTimeTransition(stBloquear, stIdle);
            tBlockEnd.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBlocking");

            // 8. Die desde cualquier estado, sin salida
            AddAnyStateTriggerTransition(sm, stMuerte, "Die");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddClipState(
            AnimatorStateMachine sm,
            string stateName,
            Dictionary<string, AnimationClip> clips,
            string clipKey,
            Vector3 pos)
        {
            AnimatorState st = sm.AddState(stateName, pos);
            if (clips != null && clips.TryGetValue(clipKey, out AnimationClip clip))
            {
                st.motion = clip;
            }
            return st;
        }

        private static AnimatorStateTransition AddInstantTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            float threshold,
            string param)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.duration = 0f;
            t.offset = 0f;
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.AddCondition(mode, threshold, param);
            return t;
        }

        private static AnimatorStateTransition AddAnyStateTriggerTransition(
            AnimatorStateMachine sm,
            AnimatorState to,
            string triggerParam)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(to);
            t.duration = 0f;
            t.offset = 0f;
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
            return t;
        }

        private static AnimatorStateTransition AddExitTimeTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.duration = 0f;
            t.offset = 0f;
            t.hasExitTime = true;
            t.exitTime = 1.0f;
            t.hasFixedDuration = true;
            return t;
        }

        private static void ConfigureProjectiles(
            List<InventarioItem> inventory,
            ref int createdClips,
            ref int skippedClips,
            ref int conflictClips)
        {
            foreach (var item in inventory)
            {
                if (item.categoria != "04_Proyectiles") continue;

                string assetPath = $"{BASE_FOLDER}/{item.categoria}/{item.nombre}";
                if (!File.Exists(assetPath)) continue;

                string projName = Path.GetFileNameWithoutExtension(item.nombre);
                int numFrames = Mathf.Max(1, item.fotogramas_solicitados);
                int cellW = item.ancho_frame_px > 0 ? item.ancho_frame_px : (item.ancho_px / numFrames);
                int cellH = item.alto_frame_px > 0 ? item.alto_frame_px : item.alto_px;

                Sprite[] sprites = ConfigureAndSliceSpriteSheet(
                    assetPath,
                    projName,
                    numFrames,
                    cellW,
                    cellH,
                    SpriteAlignment.Center,
                    new Vector2(0.5f, 0.5f)
                );

                if (sprites != null && sprites.Length > 0)
                {
                    string clipPath = $"{ANIM_OUTPUT_FOLDER}/{projName}.anim";
                    CreateOrUpdateAnimationClip(
                        clipPath,
                        sprites,
                        DEFAULT_FPS,
                        false,
                        ref createdClips,
                        ref skippedClips,
                        ref conflictClips
                    );

                    CreateArrowPrefab(ARROW_PREFAB_PATH, sprites[0]);
                }
            }
        }

        private static void CreateArrowPrefab(string prefabPath, Sprite arrowSprite)
        {
            GameObject arrowGo = new GameObject("Arrow");
            try
            {
                SpriteRenderer sr = arrowGo.AddComponent<SpriteRenderer>();
                sr.sprite = arrowSprite;
                sr.sortingOrder = 12;

                Rigidbody2D rb = arrowGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0.15f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                BoxCollider2D col = arrowGo.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                if (arrowSprite != null)
                {
                    col.size = new Vector2(
                        arrowSprite.bounds.size.x * 0.85f,
                        arrowSprite.bounds.size.y * 0.45f
                    );
                }
                else
                {
                    col.size = new Vector2(0.5f, 0.12f);
                }

                arrowGo.AddComponent<ArrowProjectile>();

                PrefabUtility.SaveAsPrefabAsset(arrowGo, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(arrowGo);
            }
        }

        private static void IntegrateWithScenePlayer(
            AnimatorController controller,
            Dictionary<string, AnimationClip> clips)
        {
            Sprite defaultIdleSprite = null;
            string idleSheetPath = $"{BASE_FOLDER}/01_Movimiento/Kael_Idle_06frames.png";
            UnityEngine.Object[] idleAssets = AssetDatabase.LoadAllAssetsAtPath(idleSheetPath);
            foreach (var obj in idleAssets)
            {
                if (obj is Sprite s && s.name == "Kael_Idle_0")
                {
                    defaultIdleSprite = s;
                    break;
                }
            }

            PlayerController existingPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (existingPlayer == null && File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                existingPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            }
            if (existingPlayer != null)
            {
                GameObject playerGo = existingPlayer.gameObject;
                Animator animator = playerGo.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<Animator>(playerGo);
                }
                animator.runtimeAnimatorController = controller;

                KaelAnimationDriver driver = playerGo.GetComponent<KaelAnimationDriver>();
                if (driver == null)
                {
                    driver = Undo.AddComponent<KaelAnimationDriver>(playerGo);
                }
                GameObject arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARROW_PREFAB_PATH);
                driver.SetArrowPrefab(arrowPrefab);

                SpriteRenderer sr = playerGo.GetComponent<SpriteRenderer>();
                if (sr != null && defaultIdleSprite != null)
                {
                    sr.sprite = defaultIdleSprite;
                }

                FirstMapBuilder.UpdateKaelAnimationsMenu();
                EditorUtility.SetDirty(playerGo);
                EditorUtility.SetDirty(animator);
                EditorUtility.SetDirty(driver);
                EditorSceneManager.MarkSceneDirty(playerGo.scene);
                EditorSceneManager.SaveScene(playerGo.scene);
            }
            else
            {
                // Si no existe Player en escena, crear el prefab Kael
                string kaelPrefabPath = $"{PREFABS_FOLDER}/Kael.prefab";
                GameObject kaelGo = new GameObject("Kael");
                try
                {
                    SpriteRenderer sr = kaelGo.AddComponent<SpriteRenderer>();
                    sr.sprite = defaultIdleSprite;
                    Animator anim = kaelGo.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller;
                    Rigidbody2D rb = kaelGo.AddComponent<Rigidbody2D>();
                    rb.freezeRotation = true;
                    CapsuleCollider2D col = kaelGo.AddComponent<CapsuleCollider2D>();
                    col.size = new Vector2(0.55f, 1.51f);
                    PrefabUtility.SaveAsPrefabAsset(kaelGo, kaelPrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(kaelGo);
                }
            }
        }

        private static void EnsureDirectory(string assetFolder)
        {
            if (!Directory.Exists(assetFolder))
            {
                Directory.CreateDirectory(assetFolder);
            }
        }
    }
}
