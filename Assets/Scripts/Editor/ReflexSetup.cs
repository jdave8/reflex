using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Reflex.Core;
using Reflex.Circle;
using Reflex.Input;
using Reflex.Visual;
using Reflex.Audio;
using Reflex.UI;

namespace Reflex.Editor
{
    public static class ReflexSetup
    {
        // =====================================================================
        //  MAIN ENTRY — runs all steps in order (safe to re-run)
        // =====================================================================

        [MenuItem("Reflex/Setup Project", false, 0)]
        public static void SetupProject()
        {
            CreateGameConfig();
            CreateCircleMaterial();
            CreateEdgeGlowMaterial();
            CreateCirclePrefab();
            PatchCirclePrefab();
            SetupGameScene();
            SetupVFX();
            SetupAudio();
            SetupUI();
            CleanMissingScripts();

            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("=== Reflex project setup complete! ===");
            Debug.Log("Press Play to test. Tap / click the screen to start.");
        }

        // =====================================================================
        //  STEP 1 — GameConfig ScriptableObject
        // =====================================================================

        [MenuItem("Reflex/1. Create GameConfig Asset", false, 100)]
        public static void CreateGameConfig()
        {
            string path = "Assets/Scripts/Core/GameConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(path) != null)
            {
                Debug.Log("GameConfig already exists at " + path);
                return;
            }

            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Created GameConfig at " + path);
        }

        // =====================================================================
        //  STEP 2 — Circle material (custom ring shader)
        // =====================================================================

        [MenuItem("Reflex/2. Create Circle Material", false, 101)]
        public static void CreateCircleMaterial()
        {
            string matPath = "Assets/Art/Materials/CircleRingMat.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
            {
                Debug.Log("CircleRingMat already exists.");
                return;
            }

            Shader shader = Shader.Find("Reflex/CircleRing");
            if (shader == null)
            {
                Debug.LogError("Cannot find shader 'Reflex/CircleRing'. Make sure the shader file exists.");
                return;
            }

            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f, 0.8f));
            mat.SetColor("_RingColor", new Color(0f, 0.9f, 1f, 1f));
            mat.SetFloat("_GlowIntensity", 0.5f);
            mat.SetFloat("_InnerRadius", 0.15f);
            mat.SetFloat("_OuterRadius", 0.45f);
            mat.SetFloat("_RingThickness", 0.025f);
            mat.SetFloat("_EdgeSoftness", 0.008f);
            mat.SetFloat("_GlowWidth", 0.04f);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created CircleRingMat at " + matPath);
        }

        // =====================================================================
        //  STEP 2b — EdgeGlow material (screen edge glow shader)
        // =====================================================================

        public static void CreateEdgeGlowMaterial()
        {
            string matPath = "Assets/Art/Materials/EdgeGlowMat.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
            {
                Debug.Log("EdgeGlowMat already exists.");
                return;
            }

            Shader shader = Shader.Find("Reflex/EdgeGlow");
            if (shader == null)
            {
                Debug.LogError("Cannot find shader 'Reflex/EdgeGlow'. Make sure the shader file exists.");
                return;
            }

            Material mat = new Material(shader);
            mat.SetColor("_GlowColor", new Color(0f, 0.9f, 1f, 0.6f));
            mat.SetFloat("_Intensity", 0f);
            mat.SetFloat("_Falloff", 2f);
            mat.SetFloat("_EdgeWidth", 0.35f);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created EdgeGlowMat at " + matPath);
        }

        // =====================================================================
        //  STEP 3 — Circle prefab (Quad + material + controllers)
        // =====================================================================

        [MenuItem("Reflex/3. Create Circle Prefab", false, 102)]
        public static void CreateCirclePrefab()
        {
            string prefabPath = "Assets/Prefabs/CirclePrefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("CirclePrefab already exists.");
                return;
            }

            GameObject circleGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circleGO.name = "Circle";

            Object.DestroyImmediate(circleGO.GetComponent<MeshCollider>());

            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CircleRingMat.mat");
            if (mat != null)
            {
                circleGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            circleGO.AddComponent<CircleController>();
            circleGO.AddComponent<CircleVisual>();
            circleGO.AddComponent<DeathFizzle>();

            PrefabUtility.SaveAsPrefabAsset(circleGO, prefabPath);
            Object.DestroyImmediate(circleGO);

            Debug.Log("Created CirclePrefab at " + prefabPath);
        }

        // =====================================================================
        //  STEP 3b — Patch existing prefab (add DeathFizzle if missing)
        // =====================================================================

        public static void PatchCirclePrefab()
        {
            string prefabPath = "Assets/Prefabs/CirclePrefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Open prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            bool changed = false;

            if (prefabRoot.GetComponent<DeathFizzle>() == null)
            {
                prefabRoot.AddComponent<DeathFizzle>();
                changed = true;
                Debug.Log("Added DeathFizzle to CirclePrefab.");
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // =====================================================================
        //  STEP 4 — Wire up the game scene (managers, spawner, input)
        // =====================================================================

        [MenuItem("Reflex/4. Setup Game Scene", false, 103)]
        public static void SetupGameScene()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Scripts/Core/GameConfig.asset");
            if (config == null)
            {
                Debug.LogError("GameConfig not found. Run 'Create GameConfig Asset' first.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CirclePrefab.prefab");
            if (prefab == null)
            {
                Debug.LogError("CirclePrefab not found. Run 'Create Circle Prefab' first.");
                return;
            }

            // --- Main Camera ---
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = config.backgroundColor;
                mainCam.orthographicSize = 5f;

                if (mainCam.GetComponent<BackgroundController>() == null)
                {
                    var bg = mainCam.gameObject.AddComponent<BackgroundController>();
                    SetSerializedField(bg, "config", config);
                }
            }

            // --- EventSystem: swap to new Input System module ---
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
            {
                var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Object.DestroyImmediate(standaloneModule);
                }

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            // --- GameManager ---
            GameObject gmGO = FindOrCreateGameObject("GameManager");
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm == null) gm = gmGO.AddComponent<GameManager>();

            // --- CircleSpawner + CirclePool (same object) ---
            GameObject spawnerGO = FindOrCreateGameObject("CircleSpawner");
            CircleSpawner spawner = spawnerGO.GetComponent<CircleSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<CircleSpawner>();

            CirclePool pool = spawnerGO.GetComponent<CirclePool>();
            if (pool == null) pool = spawnerGO.AddComponent<CirclePool>();

            // --- TouchInputHandler ---
            GameObject inputGO = FindOrCreateGameObject("TouchInputHandler");
            TouchInputHandler inputHandler = inputGO.GetComponent<TouchInputHandler>();
            if (inputHandler == null) inputHandler = inputGO.AddComponent<TouchInputHandler>();

            // --- Wire serialized references ---
            SetSerializedField(gm, "config", config);
            SetSerializedField(gm, "circleSpawner", spawner);

            SetSerializedField(spawner, "config", config);
            SetSerializedField(pool, "circlePrefab", prefab);

            SetSerializedField(inputHandler, "config", config);
            SetSerializedField(inputHandler, "circleSpawner", spawner);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("Game scene setup complete.");
        }

        // =====================================================================
        //  STEP 5 — Full UI: Canvas, HUD, TapToStart, ScoreCard, HitFeedback
        // =====================================================================

        [MenuItem("Reflex/5. Setup UI", false, 104)]
        public static void SetupUI()
        {
            // ------ Canvas ------
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasObj;

            if (canvas == null)
            {
                canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // ==================================================================
            //  TAP-TO-START PANEL  (visible during ReadyToPlay)
            // ==================================================================
            GameObject tapPanel = FindOrCreateChild(canvasObj, "TapToStartPanel");
            StretchFull(tapPanel);

            var titleText = EnsureText(tapPanel, "TitleText");
            titleText.text = "REFLEX";
            titleText.fontSize = 96;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0f, 0.9f, 1f);
            titleText.alignment = TextAlignmentOptions.Center;
            SetRect(titleText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, 200), new Vector2(800, 150));

            var tapText = EnsureText(tapPanel, "TapText");
            tapText.text = "TAP TO START";
            tapText.fontSize = 42;
            tapText.color = new Color(0.7f, 0.7f, 0.7f);
            tapText.alignment = TextAlignmentOptions.Center;
            SetRect(tapText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    Vector2.zero, new Vector2(600, 80));

            var bestText = EnsureText(tapPanel, "BestScoreText");
            bestText.text = "";
            bestText.fontSize = 36;
            bestText.color = new Color(0.5f, 0.5f, 0.5f);
            bestText.alignment = TextAlignmentOptions.Center;
            SetRect(bestText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, -100), new Vector2(400, 60));

            // ==================================================================
            //  HUD PANEL  (visible during Playing)
            // ==================================================================
            GameObject hudPanel = FindOrCreateChild(canvasObj, "HUDPanel");
            StretchFull(hudPanel);
            // Let taps pass through the HUD to reach circles during gameplay
            var hudCG = hudPanel.GetComponent<CanvasGroup>();
            if (hudCG == null) hudCG = hudPanel.AddComponent<CanvasGroup>();
            hudCG.blocksRaycasts = false;
            hudCG.interactable = false;
            hudPanel.SetActive(false);

            var scoreText = EnsureText(hudPanel, "ScoreText");
            scoreText.text = "0";
            scoreText.fontSize = 72;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.color = Color.white;
            scoreText.alignment = TextAlignmentOptions.Center;
            SetRect(scoreText, 0.5f, 1f, 0.5f, 1f, 0.5f, 1f,
                    new Vector2(0, -80), new Vector2(600, 100));

            var streakText = EnsureText(hudPanel, "StreakText");
            streakText.text = "";
            streakText.fontSize = 36;
            streakText.color = new Color(1f, 0.84f, 0f);
            streakText.alignment = TextAlignmentOptions.Center;
            SetRect(streakText, 0.5f, 1f, 0.5f, 1f, 0.5f, 1f,
                    new Vector2(0, -160), new Vector2(300, 60));

            var tierText = EnsureText(hudPanel, "TierText");
            tierText.text = "Rookie";
            tierText.fontSize = 28;
            tierText.color = new Color(0.6f, 0.6f, 0.6f);
            tierText.alignment = TextAlignmentOptions.TopRight;
            SetRect(tierText, 1f, 1f, 1f, 1f, 1f, 1f,
                    new Vector2(-40, -40), new Vector2(200, 50));

            // ==================================================================
            //  SCORE-CARD PANEL  (visible during ScoreCard state)
            // ==================================================================
            GameObject scoreCardPanel = FindOrCreateChild(canvasObj, "ScoreCardPanel");
            StretchFull(scoreCardPanel);
            scoreCardPanel.SetActive(false);

            var gameOverText = EnsureText(scoreCardPanel, "GameOverText");
            gameOverText.text = "GAME OVER";
            gameOverText.fontSize = 72;
            gameOverText.fontStyle = FontStyles.Bold;
            gameOverText.color = new Color(1f, 0.3f, 0.3f);
            gameOverText.alignment = TextAlignmentOptions.Center;
            SetRect(gameOverText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, 300), new Vector2(800, 120));

            var finalScoreText = EnsureText(scoreCardPanel, "FinalScoreText");
            finalScoreText.text = "0";
            finalScoreText.fontSize = 96;
            finalScoreText.fontStyle = FontStyles.Bold;
            finalScoreText.color = Color.white;
            finalScoreText.alignment = TextAlignmentOptions.Center;
            SetRect(finalScoreText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, 120), new Vector2(600, 150));

            var statsText = EnsureText(scoreCardPanel, "StatsText");
            statsText.text = "";
            statsText.fontSize = 32;
            statsText.color = new Color(0.7f, 0.7f, 0.7f);
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.enableWordWrapping = true;
            SetRect(statsText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, -20), new Vector2(600, 120));

            var newHighText = EnsureText(scoreCardPanel, "NewHighText");
            newHighText.text = "NEW HIGH SCORE!";
            newHighText.fontSize = 42;
            newHighText.fontStyle = FontStyles.Bold;
            newHighText.color = new Color(1f, 0.84f, 0f);
            newHighText.alignment = TextAlignmentOptions.Center;
            SetRect(newHighText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, -140), new Vector2(600, 70));
            newHighText.gameObject.SetActive(false);

            var tapRestartText = EnsureText(scoreCardPanel, "TapRestartText");
            tapRestartText.text = "TAP TO CONTINUE";
            tapRestartText.fontSize = 36;
            tapRestartText.color = new Color(0.5f, 0.5f, 0.5f);
            tapRestartText.alignment = TextAlignmentOptions.Center;
            SetRect(tapRestartText, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                    new Vector2(0, -300), new Vector2(600, 60));

            // ==================================================================
            //  HIT FEEDBACK CONTAINER  (floating labels spawn here at runtime)
            // ==================================================================
            GameObject feedbackContainer = FindOrCreateChild(canvasObj, "HitFeedbackContainer");
            StretchFull(feedbackContainer);

            // ==================================================================
            //  ADD UI SCRIPTS & WIRE SERIALIZED FIELDS
            // ==================================================================

            // TapToStartUI
            TapToStartUI tapUI = canvasObj.GetComponent<TapToStartUI>();
            if (tapUI == null) tapUI = canvasObj.AddComponent<TapToStartUI>();
            SetSerializedField(tapUI, "panel", tapPanel);
            SetSerializedField(tapUI, "bestScoreText", bestText);

            // HUDController
            HUDController hud = canvasObj.GetComponent<HUDController>();
            if (hud == null) hud = canvasObj.AddComponent<HUDController>();
            SetSerializedField(hud, "panel", hudPanel);
            SetSerializedField(hud, "scoreText", scoreText);
            SetSerializedField(hud, "streakText", streakText);
            SetSerializedField(hud, "tierText", tierText);

            // ScoreCardUI
            ScoreCardUI scoreCard = canvasObj.GetComponent<ScoreCardUI>();
            if (scoreCard == null) scoreCard = canvasObj.AddComponent<ScoreCardUI>();
            SetSerializedField(scoreCard, "panel", scoreCardPanel);
            SetSerializedField(scoreCard, "finalScoreText", finalScoreText);
            SetSerializedField(scoreCard, "statsText", statsText);
            SetSerializedField(scoreCard, "newHighText", newHighText);

            // HitFeedbackLabel
            HitFeedbackLabel feedback = canvasObj.GetComponent<HitFeedbackLabel>();
            if (feedback == null) feedback = canvasObj.AddComponent<HitFeedbackLabel>();
            SetSerializedField(feedback, "container", feedbackContainer.GetComponent<RectTransform>());

            Debug.Log("UI setup complete.");
        }

        // =====================================================================
        //  STEP 6 — VFX: Hit particles + Edge glow quad
        // =====================================================================

        [MenuItem("Reflex/6. Setup VFX", false, 105)]
        public static void SetupVFX()
        {
            // ------ VFX Parent ------
            GameObject vfxParent = FindOrCreateGameObject("VFX");

            // ==================================================================
            //  HIT PARTICLES
            // ==================================================================
            GameObject hitParticlesGO = FindOrCreateChild(vfxParent, "HitParticles");

            ParticleSystem ps = hitParticlesGO.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = hitParticlesGO.AddComponent<ParticleSystem>();

                // Stop auto-play
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Main module
                var main = ps.main;
                main.playOnAwake = false;
                main.duration = 1f;
                main.loop = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                main.startColor = new Color(0f, 0.9f, 1f, 1f);
                main.gravityModifier = 0.5f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 200;

                // Emission — no continuous emission, only manual Emit() calls
                var emission = ps.emission;
                emission.rateOverTime = 0f;

                // Shape — sphere burst
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;

                // Color over lifetime — fade out
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

                // Size over lifetime — shrink
                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

                // Renderer — use default particle material
                var renderer = hitParticlesGO.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                // Use the built-in Default-Particle material
                renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            }

            // HitEffectPlayer script
            HitEffectPlayer hitFx = hitParticlesGO.GetComponent<HitEffectPlayer>();
            if (hitFx == null) hitFx = hitParticlesGO.AddComponent<HitEffectPlayer>();
            SetSerializedField(hitFx, "hitParticles", ps);

            // ==================================================================
            //  EDGE GLOW QUAD  (world-space, sized to camera at runtime)
            // ==================================================================
            GameObject edgeGlowGO = FindOrCreateChild(vfxParent, "EdgeGlowQuad");

            MeshFilter mf = edgeGlowGO.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = edgeGlowGO.AddComponent<MeshFilter>();
                // Use the built-in Quad mesh
                GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tempQuad);
            }

            MeshRenderer edgeRenderer = edgeGlowGO.GetComponent<MeshRenderer>();
            if (edgeRenderer == null) edgeRenderer = edgeGlowGO.AddComponent<MeshRenderer>();

            Material edgeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/EdgeGlowMat.mat");
            if (edgeMat != null)
            {
                edgeRenderer.sharedMaterial = edgeMat;
            }

            // ScreenEdgeGlow script handles sizing at runtime
            if (edgeGlowGO.GetComponent<ScreenEdgeGlow>() == null)
            {
                edgeGlowGO.AddComponent<ScreenEdgeGlow>();
            }

            Debug.Log("VFX setup complete.");
        }

        // =====================================================================
        //  STEP 7 — Audio Manager with pooled sources
        // =====================================================================

        [MenuItem("Reflex/7. Setup Audio", false, 106)]
        public static void SetupAudio()
        {
            GameObject audioGO = FindOrCreateGameObject("AudioManager");

            AudioManager audioMgr = audioGO.GetComponent<AudioManager>();
            if (audioMgr == null)
            {
                audioMgr = audioGO.AddComponent<AudioManager>();
            }

            Debug.Log("Audio setup complete. AudioManager uses generated tones as placeholders.");
        }

        // =====================================================================
        //  STEP 8 — Clean up any missing script references in the scene
        // =====================================================================

        [MenuItem("Reflex/8. Clean Missing Scripts", false, 107)]
        public static void CleanMissingScripts()
        {
            int cleaned = 0;
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    cleaned += count;
                }
            }

            if (cleaned > 0)
            {
                Debug.Log($"Cleaned {cleaned} missing script reference(s).");
            }
        }

        // =====================================================================
        //  HELPERS
        // =====================================================================

        private static GameObject FindOrCreateGameObject(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }
            return go;
        }

        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            Transform existing = parent.transform.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// <summary>
        /// Set RectTransform to fill the full parent area.
        /// </summary>
        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Find or create a TextMeshProUGUI on a child with the given name.
        /// </summary>
        private static TextMeshProUGUI EnsureText(GameObject parent, string name)
        {
            Transform existing = parent.transform.Find(name);
            if (existing != null)
            {
                var existingTmp = existing.GetComponent<TextMeshProUGUI>();
                if (existingTmp != null) return existingTmp;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        /// <summary>
        /// Set RectTransform anchors, pivot, position, and size.
        /// </summary>
        private static void SetRect(TMP_Text text,
            float anchorMinX, float anchorMinY,
            float anchorMaxX, float anchorMaxY,
            float pivotX, float pivotY,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.pivot = new Vector2(pivotX, pivotY);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void SetSerializedField(object target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target as Object);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"Could not find serialized field '{fieldName}' on {target.GetType().Name}");
            }
        }
    }
}
