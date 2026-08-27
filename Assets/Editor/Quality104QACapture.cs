using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using ThreeDoorsOfFate.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Editor
{
    public static class Quality104QACapture
    {
        private const string ScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private static readonly CaptureLayout[] Layouts =
        {
            new("16x9", 1920, 1080, Vector2.zero, Vector2.one),
            new(
                "iphone14_pro_max_landscape",
                2796,
                1290,
                new Vector2(0.064f, 0.030f),
                new Vector2(0.936f, 0.970f)),
            new("4x3", 2048, 1536, Vector2.zero, Vector2.one)
        };

        public static void CaptureReleaseMatrix()
        {
            CaptureMatrix(false);
        }

        public static void CaptureHistoryMatrix()
        {
            CaptureMatrix(true);
        }

        private static void CaptureMatrix(bool historyOnly)
        {
            string outputDirectory = Environment.GetEnvironmentVariable(
                "TDOF_140_QA_DIR");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "../../Builds/QA/1.4.0"));
            }

            Directory.CreateDirectory(outputDirectory);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Type controllerType = Type.GetType(ControllerTypeName)
                ?? throw new InvalidOperationException(
                    "ThreeDoorsGameController could not be loaded.");
            Component controller = Resources.FindObjectsOfTypeAll(controllerType)
                .OfType<Component>()
                .FirstOrDefault(candidate => candidate.gameObject.scene == scene)
                ?? throw new InvalidOperationException(
                    "ThreeDoorsGameController was not found in the playable scene.");

            if (GetField<RectTransform>(controllerType, controller, "contentRoot") == null)
            {
                Invoke(controllerType, controller, "BuildShell");
            }

            Canvas canvas = GetField<Canvas>(controllerType, controller, "canvas")
                ?? throw new InvalidOperationException("Runtime canvas was not created.");
            RectTransform safeAreaRoot = GetField<RectTransform>(
                controllerType,
                controller,
                "safeAreaRoot")
                ?? throw new InvalidOperationException("Safe-area root was not created.");

            bool hadLanguagePreference = PlayerPrefs.HasKey(
                GameLanguagePolicy.PreferenceKey);
            string previousLanguagePreference = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            string previousRunHistoryPrefix = GetField<string>(
                controllerType,
                controller,
                "runHistoryKeyPrefix");
            string previousHardRunSaveKey = GetField<string>(
                controllerType,
                controller,
                "hardRunSaveKey");
            string previousHardRunSaveBackupKey = GetField<string>(
                controllerType,
                controller,
                "hardRunSaveBackupKey");
            string qaRunHistoryPrefix =
                $"ThreeDoorsOfFate.QA.V140.{Guid.NewGuid():N}.";
            string qaHardRunSaveKey = qaRunHistoryPrefix + "HardRunSave";
            string qaHardRunSaveBackupKey =
                qaRunHistoryPrefix + "HardRunSave.BackupV1";
            SetField(
                controllerType,
                controller,
                "runHistoryKeyPrefix",
                qaRunHistoryPrefix);
            SetField(
                controllerType,
                controller,
                "hardRunSaveKey",
                qaHardRunSaveKey);
            SetField(
                controllerType,
                controller,
                "hardRunSaveBackupKey",
                qaHardRunSaveBackupKey);
            List<string> manifest = new()
            {
                "language,state,layout,width,height,file"
            };

            try
            {
                if (historyOnly)
                {
                    CaptureHistoryLanguage(
                        GameLanguage.Korean,
                        "ko",
                        controllerType,
                        controller,
                        canvas,
                        safeAreaRoot,
                        outputDirectory,
                        manifest);
                    CaptureHistoryLanguage(
                        GameLanguage.English,
                        "en",
                        controllerType,
                        controller,
                        canvas,
                        safeAreaRoot,
                        outputDirectory,
                        manifest);
                }
                else
                {
                    CaptureLanguage(
                        GameLanguage.Korean,
                        "ko",
                        controllerType,
                        controller,
                        canvas,
                        safeAreaRoot,
                        outputDirectory,
                        manifest);
                    CaptureLanguage(
                        GameLanguage.English,
                        "en",
                        controllerType,
                        controller,
                        canvas,
                        safeAreaRoot,
                        outputDirectory,
                        manifest);
                }

                File.WriteAllLines(
                    Path.Combine(outputDirectory, "capture_manifest.csv"),
                    manifest,
                    new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(outputDirectory, "capture_completed_utc.txt"),
                    DateTime.UtcNow.ToString("O") + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
                PlayerPrefs.DeleteKey(
                    RunHistoryStore.GetStorageKey(qaRunHistoryPrefix));
                PlayerPrefs.DeleteKey(qaHardRunSaveKey);
                PlayerPrefs.DeleteKey(qaHardRunSaveBackupKey);
                PlayerPrefs.DeleteKey(qaHardRunSaveKey + ".DeletedRunIds");
                SetField(
                    controllerType,
                    controller,
                    "runHistoryKeyPrefix",
                    previousRunHistoryPrefix);
                SetField(
                    controllerType,
                    controller,
                    "hardRunSaveKey",
                    previousHardRunSaveKey);
                SetField(
                    controllerType,
                    controller,
                    "hardRunSaveBackupKey",
                    previousHardRunSaveBackupKey);
                if (hadLanguagePreference)
                {
                    PlayerPrefs.SetString(
                        GameLanguagePolicy.PreferenceKey,
                        previousLanguagePreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
                }

                PlayerPrefs.Save();
                GameLocalization.Initialize(Application.systemLanguage);
                ApplySafeArea(safeAreaRoot, Vector2.zero, Vector2.one);
            }

            Debug.Log(
                $"1.4.0 QA {(historyOnly ? "history" : "release")} matrix written: {outputDirectory}");
        }

        private static void CaptureHistoryLanguage(
            GameLanguage language,
            string languageCode,
            Type controllerType,
            Component controller,
            Canvas canvas,
            RectTransform safeAreaRoot,
            string outputDirectory,
            List<string> manifest)
        {
            GameLocalization.SetLanguage(language);
            UnityEngine.Random.InitState(
                language == GameLanguage.English ? 104 : 410);
            HideRunStatusPanelImmediately(controllerType, controller);

            StartKnownRun(controllerType, controller);
            SetField(controllerType, controller, "roomsCleared", 7);
            SetField(
                controllerType,
                controller,
                "combatEncountersCompleted",
                4);
            SetField(controllerType, controller, "playerHealth", 0);
            SetField(controllerType, controller, "playerMaxHealth", 60);
            SetField(controllerType, controller, "gold", 123);
            SetField(controllerType, controller, "debt", 5);
            SetField(controllerType, controller, "hiddenGameOverChance", 0f);
            object finalEnemy = Invoke(
                controllerType,
                controller,
                "CreateEnemy",
                false,
                false);
            SetField(controllerType, controller, "enemy", finalEnemy);
            Invoke(
                controllerType,
                controller,
                "ShowGameOver",
                false,
                GameLocalization.Text("gameOver.default"));
            CaptureState(
                languageCode,
                "game_over",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            Invoke(controllerType, controller, "ShowRunHistory");
            CaptureState(
                languageCode,
                "run_history",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);
            Invoke(controllerType, controller, "ShowRunHistoryDetail", 0);
            CaptureState(
                languageCode,
                "run_history_detail",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);
        }

        private static void CaptureLanguage(
            GameLanguage language,
            string languageCode,
            Type controllerType,
            Component controller,
            Canvas canvas,
            RectTransform safeAreaRoot,
            string outputDirectory,
            List<string> manifest)
        {
            GameLocalization.SetLanguage(language);
            UnityEngine.Random.InitState(language == GameLanguage.English ? 104 : 410);
            HideRunStatusPanelImmediately(controllerType, controller);

            Invoke(controllerType, controller, "ShowMainMenu");
            ConfigureMobileMainMenuButtons(controllerType, controller);
            CaptureState(
                languageCode,
                "main_menu_settings_icon",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            Invoke(
                controllerType,
                controller,
                "ShowStarterContractSelection",
                CharacterClass.Gambler);
            CaptureState(
                languageCode,
                "starter_contracts",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            Invoke(controllerType, controller, "ShowHowToPlay");
            for (int page = 0; page < 5; page += 1)
            {
                Invoke(controllerType, controller, "ShowHowToPlayPage", page);
                CaptureState(
                    languageCode,
                    $"tutorial_page_{page + 1}",
                    controllerType,
                    controller,
                    canvas,
                    safeAreaRoot,
                    outputDirectory,
                    manifest);
            }

            Invoke(controllerType, controller, "ShowHowToPlayPage", 3);
            Invoke(controllerType, controller, "HandlePracticeEndTurn");
            Invoke(controllerType, controller, "SelectPracticeCard", 0);
            Invoke(controllerType, controller, "UseSelectedPracticeCard");
            CaptureState(
                languageCode,
                "tutorial_hand_flow_complete",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            SetField(controllerType, controller, "roomsCleared", 3);
            SetField(controllerType, controller, "combatEncountersCompleted", 1);
            SetField(controllerType, controller, "consecutiveNonCombatDoors", 0);
            UnityEngine.Random.InitState(language == GameLanguage.English ? 104 : 410);
            Invoke(controllerType, controller, "ShowDoors");
            CaptureState(
                languageCode,
                "doors_normal",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            SetField(controllerType, controller, "roomsCleared", 9);
            SetField(controllerType, controller, "combatEncountersCompleted", 2);
            SetField(controllerType, controller, "consecutiveNonCombatDoors", 0);
            Invoke(controllerType, controller, "ShowDoors");
            CaptureState(
                languageCode,
                "doors_forced",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            SetKnownDecisionState(controllerType, controller);
            Invoke(controllerType, controller, "ShowRunStatusPanel");
            CaptureState(
                languageCode,
                "run_status",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            SetField(controllerType, controller, "gold", 999);
            Invoke(controllerType, controller, "ShowShop");
            CaptureState(
                languageCode,
                "shop",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            object enemy = Invoke(controllerType, controller, "CreateEnemy", false, false);
            Invoke(controllerType, controller, "StartCombat", enemy);
            Invoke(controllerType, controller, "DestroyEnemyReveal");
            HideTransientDiceRoll(controllerType, controller);
            SetField(controllerType, controller, "playerHealth", 42);
            SetField(controllerType, controller, "playerMaxHealth", 60);
            SetField(controllerType, controller, "playerBlock", 7);
            SetField(controllerType, controller, "action", 2);
            SetField(controllerType, controller, "gold", 75);
            SetField(controllerType, controller, "debt", 3);
            SetField(controllerType, controller, "roomsCleared", 4);
            Invoke(controllerType, controller, "RenderCombat");
            Invoke(controllerType, controller, "SetSubtitleBoxVisible", true);
            Invoke(controllerType, controller, "RefreshTopBar");
            CaptureState(
                languageCode,
                "combat_hud_with_subtitle",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            SetKnownDecisionState(controllerType, controller);
            Invoke(controllerType, controller, "ShowRest");
            CaptureState(
                languageCode,
                "rest_decision",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            SetKnownDecisionState(controllerType, controller);
            Invoke(controllerType, controller, "ShowEvent");
            CaptureState(
                languageCode,
                "event_decision",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            UnityEngine.Random.InitState(language == GameLanguage.English ? 140 : 401);
            Invoke(controllerType, controller, "ShowTreasure");
            CaptureState(
                languageCode,
                "treasure_preview",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            Invoke(controllerType, controller, "ShowShopCombinationGuide");
            CaptureState(
                languageCode,
                "synergy_guide",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            Invoke(controllerType, controller, "ShowMainMenu");
            Invoke(controllerType, controller, "ShowAchievements");
            CaptureState(
                languageCode,
                "achievements_page_1",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);
            Invoke(controllerType, controller, "ShowAchievementPage", 1);
            CaptureState(
                languageCode,
                "achievements_page_2",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            StartKnownRun(controllerType, controller);
            SetField(controllerType, controller, "roomsCleared", 7);
            SetField(controllerType, controller, "combatEncountersCompleted", 4);
            SetField(controllerType, controller, "playerHealth", 0);
            SetField(controllerType, controller, "playerMaxHealth", 60);
            SetField(controllerType, controller, "gold", 123);
            SetField(controllerType, controller, "debt", 5);
            SetField(controllerType, controller, "hiddenGameOverChance", 0f);
            object finalEnemy = Invoke(
                controllerType,
                controller,
                "CreateEnemy",
                false,
                false);
            SetField(controllerType, controller, "enemy", finalEnemy);
            Invoke(
                controllerType,
                controller,
                "ShowGameOver",
                false,
                GameLocalization.Text("gameOver.default"));
            CaptureState(
                languageCode,
                "game_over",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);

            Invoke(controllerType, controller, "ShowRunHistory");
            CaptureState(
                languageCode,
                "run_history",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);
            Invoke(controllerType, controller, "ShowRunHistoryDetail", 0);
            CaptureState(
                languageCode,
                "run_history_detail",
                controllerType,
                controller,
                canvas,
                safeAreaRoot,
                outputDirectory,
                manifest);
        }

        private static void StartKnownRun(Type controllerType, Component controller)
        {
            HideRunStatusPanelImmediately(controllerType, controller);
            Invoke(controllerType, controller, "HideHowToPlay");
            Invoke(controllerType, controller, "HideAchievements");
            Invoke(controllerType, controller, "HideSettingsPanel");
            SetEnumField(controllerType, controller, "currentDifficulty", "Normal");
            Invoke(
                controllerType,
                controller,
                "StartRun",
                CharacterClass.Oracle);
        }

        private static void SetKnownDecisionState(
            Type controllerType,
            Component controller)
        {
            SetField(controllerType, controller, "playerHealth", 40);
            SetField(controllerType, controller, "playerMaxHealth", 60);
            SetField(controllerType, controller, "gold", 20);
            SetField(controllerType, controller, "debt", 2);
            SetField(controllerType, controller, "roomsCleared", 3);
        }

        private static void HideTransientDiceRoll(
            Type controllerType,
            Component controller)
        {
            RectTransform diceRollRoot = GetField<RectTransform>(
                controllerType,
                controller,
                "diceRollRoot");
            if (diceRollRoot != null)
            {
                diceRollRoot.gameObject.SetActive(false);
            }
        }

        private static void HideRunStatusPanelImmediately(
            Type controllerType,
            Component controller)
        {
            RectTransform panel = GetField<RectTransform>(
                controllerType,
                controller,
                "runStatusPanel");
            Invoke(controllerType, controller, "HideRunStatusPanel");
            if (panel != null)
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }
        }

        private static void ConfigureMobileMainMenuButtons(
            Type controllerType,
            Component controller)
        {
            RectTransform contentRoot = GetField<RectTransform>(
                controllerType,
                controller,
                "contentRoot")
                ?? throw new InvalidOperationException("Main-menu content root is missing.");
            string[] buttonNames =
            {
                "게임시작",
                "플레이 방법",
                "운명 기록",
                "업적",
                "설정"
            };
            MethodInfo placementMethod = controllerType.GetMethod(
                "SetMainMenuButtonPlacement",
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "Main-menu placement method was not found.");

            for (int index = 0; index < buttonNames.Length; index += 1)
            {
                RectTransform buttonRoot = FindDescendant(
                    contentRoot,
                    buttonNames[index])
                    ?? throw new InvalidOperationException(
                        $"Main-menu button was not found: {buttonNames[index]}");
                Button button = buttonRoot.GetComponent<Button>()
                    ?? throw new InvalidOperationException(
                        $"Main-menu object is not a button: {buttonNames[index]}");
                placementMethod.Invoke(null, new object[] { button, index, 5 });
            }

            RectTransform quitButton = FindDescendant(contentRoot, "게임종료");
            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(false);
            }
        }

        private static RectTransform FindDescendant(
            RectTransform parent,
            string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index) is not RectTransform child)
                {
                    continue;
                }

                RectTransform found = FindDescendant(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CaptureState(
            string languageCode,
            string stateName,
            Type controllerType,
            Component controller,
            Canvas canvas,
            RectTransform safeAreaRoot,
            string outputDirectory,
            List<string> manifest)
        {
            RefreshLocalizedBindings(canvas);
            WriteVisibleTextAudit(
                languageCode,
                stateName,
                controllerType,
                controller,
                outputDirectory);

            foreach (CaptureLayout layout in Layouts)
            {
                ApplySafeArea(safeAreaRoot, layout.SafeMin, layout.SafeMax);
                string fileName =
                    $"{languageCode}_{stateName}_{layout.Name}_{layout.Width}x{layout.Height}.png";
                RenderCanvas(
                    canvas,
                    Path.Combine(outputDirectory, fileName),
                    layout.Width,
                    layout.Height);
                manifest.Add(string.Join(",",
                    languageCode,
                    stateName,
                    layout.Name,
                    layout.Width,
                    layout.Height,
                    fileName));
            }

            ApplySafeArea(safeAreaRoot, Vector2.zero, Vector2.one);
        }

        private static void RefreshLocalizedBindings(Canvas canvas)
        {
            MethodInfo lateUpdate = typeof(LocalizedTextBinding).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "LocalizedTextBinding.LateUpdate was not found.");
            foreach (LocalizedTextBinding binding in
                canvas.GetComponentsInChildren<LocalizedTextBinding>(true))
            {
                if (binding != null && binding.gameObject.activeInHierarchy)
                {
                    lateUpdate.Invoke(binding, null);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void WriteVisibleTextAudit(
            string languageCode,
            string stateName,
            Type controllerType,
            Component controller,
            string outputDirectory)
        {
            Canvas canvas = GetField<Canvas>(controllerType, controller, "canvas")
                ?? throw new InvalidOperationException("Runtime canvas was not created.");
            string[] lines = canvas
                .GetComponentsInChildren<Text>(true)
                .Where(text => text.gameObject.activeInHierarchy)
                .Select(text => text.text?.Replace("\r", string.Empty) ?? string.Empty)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
            File.WriteAllLines(
                Path.Combine(
                    outputDirectory,
                    $"{languageCode}_{stateName}_visible_text.txt"),
                lines,
                new UTF8Encoding(false));
        }

        private static void ApplySafeArea(
            RectTransform safeAreaRoot,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private static void RenderCanvas(
            Canvas canvas,
            string outputPath,
            int width,
            int height)
        {
            GameObject cameraObject = new("1.4.0 QA Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            RenderTexture target = new(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            Texture2D capture = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;

            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = target;

                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply(false, false);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static object Invoke(
            Type controllerType,
            Component controller,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length)
                ?? throw new InvalidOperationException(
                    $"Controller method was not found: {methodName}/{arguments.Length}");
            try
            {
                return method.Invoke(controller, arguments);
            }
            catch (TargetInvocationException exception)
                when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }

        private static T GetField<T>(
            Type controllerType,
            Component controller,
            string fieldName)
            where T : class
        {
            return GetFieldInfo(controllerType, fieldName).GetValue(controller) as T;
        }

        private static void SetField(
            Type controllerType,
            Component controller,
            string fieldName,
            object value)
        {
            GetFieldInfo(controllerType, fieldName).SetValue(controller, value);
        }

        private static void SetEnumField(
            Type controllerType,
            Component controller,
            string fieldName,
            string value)
        {
            FieldInfo field = GetFieldInfo(controllerType, fieldName);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static FieldInfo GetFieldInfo(Type controllerType, string fieldName)
        {
            return controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    $"Controller field was not found: {fieldName}");
        }

        private readonly struct CaptureLayout
        {
            public CaptureLayout(
                string name,
                int width,
                int height,
                Vector2 safeMin,
                Vector2 safeMax)
            {
                Name = name;
                Width = width;
                Height = height;
                SafeMin = safeMin;
                SafeMax = safeMax;
            }

            public string Name { get; }
            public int Width { get; }
            public int Height { get; }
            public Vector2 SafeMin { get; }
            public Vector2 SafeMax { get; }
        }
    }
}
