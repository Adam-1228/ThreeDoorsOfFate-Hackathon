using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ThreeDoorsOfFate.Cards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Editor
{
    public static class HowToPlaySourceQACapture
    {
        private const string ScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly Vector2Int[] TutorialQaResolutions =
        {
            new(1920, 1080),
            new(2778, 1284),
            new(2732, 2048)
        };

        private static readonly string[] TutorialAssetPaths =
        {
            "Assets/Resources/Tutorial/how_to_play_01_class.png",
            "Assets/Resources/Tutorial/how_to_play_02_doors.png",
            "Assets/Resources/Tutorial/how_to_play_03_combat.png",
            "Assets/Resources/Tutorial/how_to_play_04_card_use.png",
            "Assets/Resources/Tutorial/how_to_play_05_growth.png"
        };

        public static void ConfigureTutorialImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string assetPath in TutorialAssetPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(assetPath)
                    as TextureImporter
                    ?? throw new InvalidOperationException(
                        $"Tutorial texture importer was not found: {assetPath}");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("How-to-play tutorial texture imports configured.");
        }

        public static void Capture()
        {
            string outputDirectory = Environment.GetEnvironmentVariable(
                "TDOF_TUTORIAL_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "../../Builds/Tutorial/Sources"));
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

            SetEnumField(controllerType, controller, "currentDifficulty", "Easy");

            Invoke(controllerType, controller, "ShowClassSelection");
            CaptureCurrentCanvas(
                controllerType,
                controller,
                outputDirectory,
                "how_to_play_source_01_class.png");

            Invoke(controllerType, controller, "StartRun", CharacterClass.Gambler);
            CaptureCurrentCanvas(
                controllerType,
                controller,
                outputDirectory,
                "how_to_play_source_02_doors.png");

            object enemy = Invoke(controllerType, controller, "CreateEnemy", false, false);
            Invoke(controllerType, controller, "StartCombat", enemy);
            Invoke(controllerType, controller, "DestroyEnemyReveal");
            HideTransientDiceRoll(controllerType, controller);
            Invoke(controllerType, controller, "RenderCombat");
            CaptureCurrentCanvas(
                controllerType,
                controller,
                outputDirectory,
                "how_to_play_source_03_combat.png");

            ShowFirstPlayableCardPreview(controllerType, controller);
            CaptureCurrentCanvas(
                controllerType,
                controller,
                outputDirectory,
                "how_to_play_source_04_card_use.png");

            Invoke(controllerType, controller, "ShowClassDetail", CharacterClass.Exile);
            HideTransientDiceRoll(controllerType, controller);
            CaptureCurrentCanvas(
                controllerType,
                controller,
                outputDirectory,
                "how_to_play_source_05_growth.png");

            Debug.Log($"How-to-play source QA captures written: {outputDirectory}");
        }

        public static void CaptureTutorialGallery()
        {
            string outputDirectory = Environment.GetEnvironmentVariable(
                "TDOF_TUTORIAL_QA_DIR");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "../../Builds/Tutorial/QA"));
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

            List<Sprite> tutorialSprites = TutorialAssetPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)
                    ?? throw new InvalidOperationException(
                        $"Tutorial sprite was not found: {path}"))
                .ToList();
            SetField(
                controllerType,
                controller,
                "howToPlaySprites",
                tutorialSprites);

            Invoke(controllerType, controller, "ShowMainMenu");
            ConfigureMobileMainMenuButtons(controllerType, controller);
            CaptureQaState(
                controllerType,
                controller,
                outputDirectory,
                "main_menu_mobile3");

            Invoke(controllerType, controller, "ShowHowToPlay");
            CaptureQaState(
                controllerType,
                controller,
                outputDirectory,
                "page_01");

            Invoke(controllerType, controller, "ShowHowToPlayPage", 3);
            CaptureQaState(
                controllerType,
                controller,
                outputDirectory,
                "page_04_long_caption");

            Invoke(controllerType, controller, "ShowHowToPlayPage", 4);
            CaptureQaState(
                controllerType,
                controller,
                outputDirectory,
                "page_05_complete");

            Sprite pageThree = tutorialSprites[2];
            tutorialSprites[2] = null;
            Invoke(controllerType, controller, "ShowHowToPlayPage", 2);
            CaptureQaState(
                controllerType,
                controller,
                outputDirectory,
                "page_03_missing_fallback");
            tutorialSprites[2] = pageThree;

            Debug.Log($"How-to-play gallery QA captures written: {outputDirectory}");
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
            string[] buttonNames = { "게임시작", "플레이 방법", "설정" };
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
                placementMethod.Invoke(null, new object[] { button, index, 3 });
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

        private static void CaptureQaState(
            Type controllerType,
            Component controller,
            string outputDirectory,
            string stateName)
        {
            Canvas canvas = GetField<Canvas>(controllerType, controller, "canvas")
                ?? throw new InvalidOperationException("Runtime canvas was not created.");
            foreach (Vector2Int resolution in TutorialQaResolutions)
            {
                string fileName =
                    $"qa_{resolution.x}x{resolution.y}_{stateName}.png";
                RenderCanvas(
                    canvas,
                    Path.Combine(outputDirectory, fileName),
                    resolution.x,
                    resolution.y);
            }
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

        private static void ShowFirstPlayableCardPreview(
            Type controllerType,
            Component controller)
        {
            IList hand = GetField<IList>(controllerType, controller, "hand")
                ?? throw new InvalidOperationException("Combat hand was not initialized.");

            for (int index = 0; index < hand.Count; index += 1)
            {
                if (hand[index] is not CardData card
                    || card.FullCardSprite == null
                    || !(bool)Invoke(controllerType, controller, "CanPlay", card))
                {
                    continue;
                }

                Invoke(
                    controllerType,
                    controller,
                    "SelectCombatCardForPreview",
                    index,
                    card);
                return;
            }

            throw new InvalidOperationException(
                "No playable combat card with a full-card sprite was available.");
        }

        private static void CaptureCurrentCanvas(
            Type controllerType,
            Component controller,
            string outputDirectory,
            string fileName)
        {
            Canvas canvas = GetField<Canvas>(controllerType, controller, "canvas")
                ?? throw new InvalidOperationException("Runtime canvas was not created.");
            RenderCanvas(canvas, Path.Combine(outputDirectory, fileName));
        }

        private static void RenderCanvas(Canvas canvas, string outputPath)
        {
            RenderCanvas(canvas, outputPath, CaptureWidth, CaptureHeight);
        }

        private static void RenderCanvas(
            Canvas canvas,
            string outputPath,
            int width,
            int height)
        {
            GameObject cameraObject = new("How To Play QA Camera", typeof(Camera));
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
            Texture2D capture = new(
                width,
                height,
                TextureFormat.RGB24,
                false);
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
                capture.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
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
            return method.Invoke(controller, arguments);
        }

        private static T GetField<T>(
            Type controllerType,
            Component controller,
            string fieldName)
            where T : class
        {
            return GetFieldInfo(controllerType, fieldName).GetValue(controller) as T;
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

        private static void SetField(
            Type controllerType,
            Component controller,
            string fieldName,
            object value)
        {
            GetFieldInfo(controllerType, fieldName).SetValue(controller, value);
        }

        private static FieldInfo GetFieldInfo(Type controllerType, string fieldName)
        {
            return controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    $"Controller field was not found: {fieldName}");
        }
    }
}
