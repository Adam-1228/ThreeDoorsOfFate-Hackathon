using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;

namespace ThreeDoorsOfFate.Editor
{
    public static class PlayableGameBuilder
    {
        private const string PlayableScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string CardDataRoot = "Assets/Data/Cards/MVP";
        private const string CardBackPath = "Assets/Art/Cards/Illustrations/MVP/card_back_three_doors_v2.png";
        private const string EnglishLocalizedCardRoot = "Assets/Resources/Cards/EnglishLocalized";
        private const string RunModifierCatalogPath = "Assets/Data/RunModifiers/run_modifier_catalog.json";
        private const string RunModifierIconRoot = "Assets/Art/RunModifiers/Icons512BoxMatched";
        private const string MainMenuMusicPath = "Assets/Audio/Music/Payment_in_Iron.mp3";
        private const string BattleMusicPath = "Assets/Audio/Music/Bronze_Gates_Closing.mp3";
        private const string NonCombatMusicPath = "Assets/Audio/Music/The_Merchant_s_Toll.mp3";
        private const string BossMusicPath = "Assets/Audio/Music/The_Gatekeeper_s_Toll.mp3";
        private const string DeathMusicPath = "Assets/Audio/Music/The_Iron_Seal.mp3";
        private const string MusicRoot = "Assets/Audio/Music";
        private const string ImpactSfxRoot = "Assets/Audio/SFX/Impact";
        private const string GameSfxRoot = "Assets/Audio/SFX";
        private const string IOSAppIconPath =
            "Assets/Art/Branding/AppIcon/three_doors_app_icon_1024.png";
        private const string WindowsBuildFolderFromProjectRoot = "../Builds/Windows";
        private const string WindowsBuildFileName = "ThreeDoorsOfFate.exe";
        private const string WebGLBuildFolderFromProjectRoot = "../Builds/WebGL";
        private const string MacOSBuildFolderFromProjectRoot = "../Builds/macOS";
        private const string MacOSBuildFileName = "ThreeDoorsOfFate.app";
        private const string IOSBuildFolderFromProjectRoot = "../Builds/iOS";
        private const string IOSDeviceBuildFolderName = "Device";
        private const string IOSSimulatorBuildFolderName = "Simulator";
        private const string AndroidBuildFolderFromProjectRoot = "../Builds/Android";
        private const string AndroidBuildFileName = "ThreeDoorsOfFate.apk";

        private static readonly (DoorType Type, string Path, string HoverPath)[] DoorSpritePaths =
        {
            (DoorType.Battle, "Assets/Art/Doors/Fate/door_battle.png", "Assets/Art/Doors/Fate/Hover/door_battle_hover_open_v4.png"),
            (DoorType.Elite, "Assets/Art/Doors/Fate/door_elite.png", "Assets/Art/Doors/Fate/Hover/door_elite_hover_open_v4.png"),
            (DoorType.Shop, "Assets/Art/Doors/Fate/door_shop.png", "Assets/Art/Doors/Fate/Hover/door_shop_hover_open_v4.png"),
            (DoorType.Treasure, "Assets/Art/Doors/Fate/door_treasure.png", "Assets/Art/Doors/Fate/Hover/door_treasure_hover_open_v4.png"),
            (DoorType.Event, "Assets/Art/Doors/Fate/door_event.png", "Assets/Art/Doors/Fate/Hover/door_event_hover_open_v4.png"),
            (DoorType.Rest, "Assets/Art/Doors/Fate/door_rest.png", "Assets/Art/Doors/Fate/Hover/door_rest_hover_open_v4.png"),
            (DoorType.Curse, "Assets/Art/Doors/Fate/door_curse.png", "Assets/Art/Doors/Fate/Hover/door_curse_hover_open_v4.png"),
            (DoorType.Boss, "Assets/Art/Doors/Fate/door_boss.png", "Assets/Art/Doors/Fate/Hover/door_boss_hover_open_v4.png")
        };

        private static readonly (string Id, string Path)[] EnemySpritePaths =
        {
            ("monster_cave_lurker", "Assets/Art/Characters/Monsters/monster_cave_lurker.png"),
            ("monster_debt_hound", "Assets/Art/Characters/Monsters/monster_debt_hound.png"),
            ("monster_ash_gambler", "Assets/Art/Characters/Monsters/monster_ash_gambler.png"),
            ("monster_rune_thief", "Assets/Art/Characters/Monsters/monster_rune_thief.png"),
            ("monster_candle_warden", "Assets/Art/Characters/Monsters/monster_candle_warden.png"),
            ("monster_contract_knight", "Assets/Art/Characters/Monsters/monster_contract_knight.png"),
            ("monster_hollow_collector", "Assets/Art/Characters/Monsters/monster_hollow_collector.png"),
            ("monster_rift_spider", "Assets/Art/Characters/Monsters/monster_rift_spider.png"),
            ("monster_curse_bearer", "Assets/Art/Characters/Monsters/monster_curse_bearer.png"),
            ("monster_gold_mimic", "Assets/Art/Characters/Monsters/monster_gold_mimic.png"),
            ("monster_abyss_bailiff", "Assets/Art/Characters/Monsters/monster_abyss_bailiff.png"),
            ("monster_ledger_moth", "Assets/Art/Characters/Monsters/monster_ledger_moth.png"),
            ("monster_coin_sutured_husk", "Assets/Art/Characters/Monsters/monster_coin_sutured_husk.png"),
            ("monster_broken_scale_acolyte", "Assets/Art/Characters/Monsters/monster_broken_scale_acolyte.png"),
            ("monster_rift_lamprey", "Assets/Art/Characters/Monsters/monster_rift_lamprey.png"),
            ("monster_contract_marionette", "Assets/Art/Characters/Monsters/monster_contract_marionette.png"),
            ("monster_oath_candle_revenant", "Assets/Art/Characters/Monsters/monster_oath_candle_revenant.png"),
            ("monster_void_tax_scribe", "Assets/Art/Characters/Monsters/monster_void_tax_scribe.png"),
            ("monster_debt_pit_bruiser", "Assets/Art/Characters/Monsters/monster_debt_pit_bruiser.png"),
            ("monster_doorless_penitent", "Assets/Art/Characters/Monsters/monster_doorless_penitent.png")
        };

        private static readonly string[] EnemyHudFrameIds =
        {
            "monster_cave_lurker",
            "monster_debt_hound",
            "monster_ash_gambler",
            "monster_rune_thief",
            "monster_candle_warden",
            "monster_contract_knight",
            "monster_hollow_collector",
            "monster_rift_spider",
            "monster_curse_bearer",
            "monster_gold_mimic",
            "monster_abyss_bailiff",
            "monster_ledger_moth",
            "monster_coin_sutured_husk",
            "monster_broken_scale_acolyte",
            "monster_rift_lamprey",
            "monster_contract_marionette",
            "monster_oath_candle_revenant",
            "monster_void_tax_scribe",
            "monster_debt_pit_bruiser",
            "monster_doorless_penitent",
            "boss_gatekeeper_third_door",
            "boss_debt_adjudicator_normal",
            "boss_usurer_of_the_abyss_hard",
            "boss_bottomless_creditor_special"
        };

        [MenuItem("Three Doors of Fate/Create Playable Scene")]
        public static void CreatePlayableScene()
        {
            CardAssetBootstrapper.GenerateMvpCardAssets();
            HardCardAssetBootstrapper.GenerateHardCardAssets();
            ImportArt();
            EnsureFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ThreeDoorsPlayable";

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.019f, 0.024f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.tag = "MainCamera";

            CreateEventSystem();

            GameObject controllerObject = new("Three Doors Game", typeof(ThreeDoorsGameController));
            ThreeDoorsGameController controller = controllerObject.GetComponent<ThreeDoorsGameController>();
            ConfigureController(controller);

            EditorSceneManager.SaveScene(scene, PlayableScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PlayableScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Playable scene created: {PlayableScenePath}");
        }

        [MenuItem("Three Doors of Fate/Build Windows Playable")]
        public static void BuildWindowsPlayable()
        {
            BuildStandalonePlayable(
                "Windows",
                BuildTarget.StandaloneWindows64,
                WindowsBuildFolderFromProjectRoot,
                WindowsBuildFileName);
        }

        [MenuItem("Three Doors of Fate/Build WebGL Playable")]
        public static void BuildWebGLPlayable()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new InvalidOperationException("WebGL Build Support is not installed.");
            }

            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            ImportEnglishLocalizedCardsForWebGL();
            CreatePlayableScene();
            ConfigurePlayerSettings();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            string output = GetBuildOutputPath(WebGLBuildFolderFromProjectRoot, string.Empty);
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }
            Directory.CreateDirectory(output);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PlayableScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }
        }

        [MenuItem("Three Doors of Fate/Build macOS Playable")]
        public static void BuildMacOSPlayable()
        {
            BuildStandalonePlayable(
                "macOS",
                BuildTarget.StandaloneOSX,
                MacOSBuildFolderFromProjectRoot,
                MacOSBuildFileName);
        }

        [MenuItem("Three Doors of Fate/Build iOS Device Xcode Project")]
        public static void BuildIOSPlayable()
        {
            BuildIOSXcodeProject("iOS device", iOSSdkVersion.DeviceSDK, IOSDeviceBuildFolderName);
        }

        [MenuItem("Three Doors of Fate/Build iOS Simulator Xcode Project")]
        public static void BuildIOSSimulatorPlayable()
        {
            BuildIOSXcodeProject("iOS Simulator", iOSSdkVersion.SimulatorSDK, IOSSimulatorBuildFolderName);
        }

        [MenuItem("Three Doors of Fate/Build Android Landscape")]
        public static void BuildAndroidLandscape()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException(
                    "Android Build Support is not installed. Install Android Build Support with SDK/NDK Tools and OpenJDK from Unity Hub.");
            }

            CreatePlayableScene();
            ConfigurePlayerSettings();
            ConfigureAndroidLandscapePlayerSettings();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException("Failed to switch Unity build target to Android.");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDirectory = Path.GetFullPath(Path.Combine(projectRoot, AndroidBuildFolderFromProjectRoot));
            Directory.CreateDirectory(buildDirectory);
            string apkPath = Path.Combine(buildDirectory, AndroidBuildFileName);

            BuildPlayerOptions options = new()
            {
                scenes = new[] { PlayableScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {summary.result}");
            }

            Debug.Log($"Android landscape build succeeded: {apkPath} ({summary.totalSize} bytes)");
        }

        private static void BuildStandalonePlayable(
            string platformLabel,
            BuildTarget target,
            string buildFolderFromProjectRoot,
            string buildFileName)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                throw new InvalidOperationException(
                    $"{platformLabel} Build Support is not installed. Install the {platformLabel} standalone module from Unity Hub.");
            }

            CreatePlayableScene();
            ConfigurePlayerSettings();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target))
            {
                throw new InvalidOperationException($"Failed to switch Unity build target to {platformLabel}.");
            }

            string outputPath = GetBuildOutputPath(buildFolderFromProjectRoot, buildFileName);
            PrepareCleanStandaloneBuildDirectory(outputPath);
            BuildPlayerOptions options = new()
            {
                scenes = new[] { PlayableScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{platformLabel} build failed: {summary.result}");
            }

            Debug.Log($"{platformLabel} build succeeded: {outputPath} ({summary.totalSize} bytes)");
        }

        private static void BuildIOSXcodeProject(
            string platformLabel,
            iOSSdkVersion sdkVersion,
            string outputFolderName)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                throw new InvalidOperationException(
                    "iOS Build Support is not installed. Install the iOS module for this Unity Editor version.");
            }

            CreatePlayableScene();
            ConfigurePlayerSettings();
            ConfigureIOSPlayerSettings(sdkVersion);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                throw new InvalidOperationException("Failed to switch Unity build target to iOS.");
            }

            string outputPath = GetBuildOutputPath(IOSBuildFolderFromProjectRoot, outputFolderName);
            BuildOptions buildOptions = Directory.Exists(outputPath)
                ? BuildOptions.AcceptExternalModificationsToPlayer
                : BuildOptions.None;
            BuildPlayerOptions options = new()
            {
                scenes = new[] { PlayableScenePath },
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{platformLabel} Xcode export failed: {summary.result}");
            }

            Debug.Log($"{platformLabel} Xcode project succeeded: {outputPath} ({summary.totalSize} bytes)");
        }

        private static string GetBuildOutputPath(string buildFolderFromProjectRoot, string buildFileName)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDirectory = Path.GetFullPath(Path.Combine(projectRoot, buildFolderFromProjectRoot));
            Directory.CreateDirectory(buildDirectory);
            return Path.Combine(buildDirectory, buildFileName);
        }

        private static void PrepareCleanStandaloneBuildDirectory(string outputPath)
        {
            string fullOutputPath = Path.GetFullPath(outputPath);
            string buildDirectory = Path.GetDirectoryName(fullOutputPath);
            if (string.IsNullOrWhiteSpace(buildDirectory)
                || string.Equals(buildDirectory, Path.GetPathRoot(buildDirectory), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsafe standalone build output path: {outputPath}");
            }

            if (Directory.Exists(buildDirectory))
            {
                Directory.Delete(buildDirectory, true);
            }

            Directory.CreateDirectory(buildDirectory);
        }

        [MenuItem("Three Doors of Fate/Refresh Playable Scene Assets")]
        public static void RefreshPlayableSceneAssets()
        {
            ImportArt();
            if (!File.Exists(PlayableScenePath))
            {
                CreatePlayableScene();
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
            ThreeDoorsGameController controller = UnityEngine.Object.FindAnyObjectByType<ThreeDoorsGameController>();
            if (controller == null)
            {
                throw new InvalidOperationException($"No {nameof(ThreeDoorsGameController)} found in {PlayableScenePath}.");
            }

            ConfigureController(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Playable scene assets refreshed: {PlayableScenePath}");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "ADAM";
            PlayerSettings.productName = "Three Doors of Fate";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone,
                IOSReleaseConfiguration.BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.iOS,
                IOSReleaseConfiguration.BundleIdentifier);
        }

        private static void ConfigureAndroidLandscapePlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                IOSReleaseConfiguration.BundleIdentifier);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        private static void ConfigureIOSPlayerSettings(iOSSdkVersion sdkVersion)
        {
            PlayerSettings.bundleVersion = IOSReleaseConfiguration.GetEnvironmentOverride(
                "UNITY_IOS_VERSION",
                IOSReleaseConfiguration.DefaultVersion);
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.iOS,
                IOSReleaseConfiguration.BundleIdentifier);
            ConfigureIOSAppIcons();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.iOS.targetOSVersionString = IOSReleaseConfiguration.MinimumOSVersion;
            PlayerSettings.iOS.buildNumber = IOSReleaseConfiguration.GetEnvironmentOverride(
                "UNITY_IOS_BUILD_NUMBER",
                IOSReleaseConfiguration.DefaultBuildNumber);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = sdkVersion;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.Universal;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            string developmentTeam = Environment.GetEnvironmentVariable("UNITY_IOS_DEVELOPMENT_TEAM");
            if (!string.IsNullOrWhiteSpace(developmentTeam))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = developmentTeam.Trim();
            }
        }

        private static void ConfigureIOSAppIcons()
        {
            if (AssetImporter.GetAtPath(IOSAppIconPath) is not TextureImporter importer)
            {
                throw new BuildFailedException($"Missing iOS app icon: {IOSAppIconPath}");
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IOSAppIconPath);
            if (icon == null)
            {
                throw new BuildFailedException($"Unable to import iOS app icon: {IOSAppIconPath}");
            }

            foreach (PlatformIconKind kind in
                     PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(
                    NamedBuildTarget.iOS,
                    kind);
                foreach (PlatformIcon slot in slots)
                {
                    slot.SetTexture(icon, 0);
                }

                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, slots);
            }
        }

        private static void ConfigureController(ThreeDoorsGameController controller)
        {
            SerializedObject serializedObject = new(controller);

            AssignCardPool(serializedObject);
            serializedObject.FindProperty("cardBackSprite").objectReferenceValue = LoadSprite(CardBackPath);
            AssignRunModifiers(serializedObject);
            AssignDoorSprites(serializedObject);
            AssignBossDoorSprites(serializedObject);
            AssignBackgrounds(serializedObject);
            AssignCharacters(serializedObject);
            AssignUi(serializedObject);
            AssignGameOverSprites(serializedObject);
            AssignJourneyEndingSprites(serializedObject);
            AssignDiceSprites(serializedObject);
            AssignClassDiceSprites(serializedObject);
            AssignAudio(serializedObject);
            AssignEnemySprites(serializedObject);
            AssignEnemyHudFrameSprites(serializedObject);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module = eventSystem.AddComponent<InputSystemUIInputModule>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions != null)
            {
                module.actionsAsset = actions;
            }
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void AssignCardPool(SerializedObject serializedObject)
        {
            CardData[] cards = Directory.GetFiles(CardDataRoot, "*.asset")
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(NormalizeAssetPath(path)))
                .Where(card => card != null)
                .OrderBy(card => card.CardId, StringComparer.Ordinal)
                .ToArray();

            SerializedProperty cardPool = serializedObject.FindProperty("cardPool");
            cardPool.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i += 1)
            {
                cardPool.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
        }

        private static void AssignRunModifiers(SerializedObject serializedObject)
        {
            TextAsset catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(RunModifierCatalogPath);
            serializedObject.FindProperty("runModifierCatalog").objectReferenceValue = catalogAsset;

            SerializedProperty runItemIcons = serializedObject.FindProperty("runItemIcons");
            if (catalogAsset == null || string.IsNullOrWhiteSpace(catalogAsset.text))
            {
                runItemIcons.arraySize = 0;
                return;
            }

            RunModifierCatalogData catalog = JsonUtility.FromJson<RunModifierCatalogData>(catalogAsset.text);
            List<RunModifierCatalogEntry> entries = catalog?.modifiers?
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id) && !string.IsNullOrWhiteSpace(entry.icon))
                .ToList() ?? new List<RunModifierCatalogEntry>();

            runItemIcons.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i += 1)
            {
                SerializedProperty element = runItemIcons.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("itemId").stringValue = entries[i].id;
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite($"{RunModifierIconRoot}/{entries[i].icon}");
            }
        }

        private static void AssignDoorSprites(SerializedObject serializedObject)
        {
            SerializedProperty doorSprites = serializedObject.FindProperty("doorSprites");
            doorSprites.arraySize = DoorSpritePaths.Length;
            for (int i = 0; i < DoorSpritePaths.Length; i += 1)
            {
                SerializedProperty element = doorSprites.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("doorType").enumValueIndex = (int)DoorSpritePaths[i].Type;
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(DoorSpritePaths[i].Path);
                element.FindPropertyRelative("hoverSprite").objectReferenceValue = LoadSprite(DoorSpritePaths[i].HoverPath);
            }
        }

        private static void AssignBossDoorSprites(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("easyBossDoorSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/door_boss_gatekeeper_easy.png");
            serializedObject.FindProperty("easyBossDoorHoverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/Hover/door_boss_gatekeeper_easy_hover_open.png");
            serializedObject.FindProperty("normalBossDoorSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/door_boss_debt_adjudicator_normal.png");
            serializedObject.FindProperty("normalBossDoorHoverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/Hover/door_boss_debt_adjudicator_normal_hover_open.png");
            serializedObject.FindProperty("hardBossDoorSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/door_boss_usurer_of_the_abyss_hard.png");
            serializedObject.FindProperty("hardBossDoorHoverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Doors/Fate/Hover/door_boss_usurer_of_the_abyss_hard_hover_open.png");
        }

        private static void AssignBackgrounds(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("mainMenuBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_main_menu_three_doors.png");
            serializedObject.FindProperty("classSelectBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_class_select_empty_three_doors.png");
            serializedObject.FindProperty("battleBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_battle_arena.png");
            serializedObject.FindProperty("shopBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_shop_alcove.png");
            serializedObject.FindProperty("eventBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_event_contract_broker_v2.png");
            serializedObject.FindProperty("restBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_event_rest_candle.png");
            serializedObject.FindProperty("treasureBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_event_treasure_vault.png");
            serializedObject.FindProperty("curseBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_event_curse_ritual.png");
            serializedObject.FindProperty("rewardBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_reward_cards.png");
            serializedObject.FindProperty("bossBackground").objectReferenceValue = LoadSprite("Assets/Art/Backgrounds/bg_boss_arena.png");
        }

        private static void AssignCharacters(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("gamblerSelectSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_gambler_cutout.png");
            serializedObject.FindProperty("gamblerSelectHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_gambler_hover_cutout.png");
            serializedObject.FindProperty("oracleSelectSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_oracle_cutout.png");
            serializedObject.FindProperty("oracleSelectHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_oracle_hover_cutout.png");
            serializedObject.FindProperty("exileSelectSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_exile_cutout.png");
            serializedObject.FindProperty("exileSelectHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/ClassSelectLayers/class_select_exile_hover_cutout.png");
            serializedObject.FindProperty("shopkeeperSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/NPCs/shopkeeper_cave_contract_merchant.png");
            serializedObject.FindProperty("bossSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/Bosses/boss_gatekeeper_third_door.png");
            serializedObject.FindProperty("normalBossSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/Bosses/boss_debt_adjudicator_normal.png");
            serializedObject.FindProperty("hardBossSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/Bosses/boss_usurer_of_the_abyss_hard.png");
            serializedObject.FindProperty("debtClearBossSprite").objectReferenceValue = LoadSprite("Assets/Art/Characters/Bosses/boss_bottomless_creditor_special.png");
        }

        private static void AssignUi(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("uiFontAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR-VF.ttf");
            serializedObject.FindProperty("titleFontAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/GowunBatang-Bold.ttf");
            serializedObject.FindProperty("panelSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_inner_panel_frame.png");
            serializedObject.FindProperty("statusPanelFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_modal_frame_v2.png");
            serializedObject.FindProperty("statusSectionFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_section_medium_frame_v2.png");
            serializedObject.FindProperty("statusSectionWideFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_section_wide_frame_v2.png");
            serializedObject.FindProperty("statusSectionTallFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_section_tall_frame_v2.png");
            serializedObject.FindProperty("statusSectionMediumFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_section_medium_frame_v2.png");
            serializedObject.FindProperty("statusHintFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_hint_bar_frame_v2.png");
            serializedObject.FindProperty("statusCategoryCardFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_category_card_frame.png");
            serializedObject.FindProperty("statusInnerPanelFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png");
            serializedObject.FindProperty("statusInnerHeaderFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_inner_header_frame_ai.png");
            serializedObject.FindProperty("statusItemSlotFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_status_item_slot_frame_ai.png");
            serializedObject.FindProperty("shopCombinationPanelFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_shop_combination_panel_frame.png");
            serializedObject.FindProperty("buttonIdleSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Buttons/button_idle.png");
            serializedObject.FindProperty("buttonHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Buttons/button_hover.png");
            serializedObject.FindProperty("buttonPressedSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Buttons/button_pressed.png");
            serializedObject.FindProperty("settingsPanelSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/settings_panel_generated.png");
            serializedObject.FindProperty("settingsButtonSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/settings_button_generated.png");
            serializedObject.FindProperty("settingsButtonHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/settings_button_hover_generated.png");
            serializedObject.FindProperty("settingsButtonPressedSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/settings_button_pressed_generated.png");
            serializedObject.FindProperty("settingsIconSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/settings_icon_generated.png");
            serializedObject.FindProperty("mainTitleLogoSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/title_logo_three_doors.png");
            serializedObject.FindProperty("topBarFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_top_bar_frame.png");
            serializedObject.FindProperty("runStatusPanelFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_run_status_panel_frame.png");
            serializedObject.FindProperty("logPanelFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_log_panel_frame.png");
            serializedObject.FindProperty("eventMessageFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_event_message_frame.png");
            serializedObject.FindProperty("doorChoiceFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_door_choice_frame.png");
            serializedObject.FindProperty("enemyStatusFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_enemy_status_frame.png");
            serializedObject.FindProperty("playerCombatStatusFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_player_combat_status_frame_v2.png");
            serializedObject.FindProperty("deckBoxFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_deck_box_frame_v3.png");
            serializedObject.FindProperty("classBackButtonSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_class_back_button_frame.png");
            serializedObject.FindProperty("classConfirmButtonSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_class_confirm_button_frame.png");
            serializedObject.FindProperty("classInfoButtonSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/ui_class_info_button_frame.png");
            serializedObject.FindProperty("mainMenuButtonSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_menu_button_generated.png");
            serializedObject.FindProperty("mainMenuButtonHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_menu_button_hover_generated.png");
            serializedObject.FindProperty("mainMenuButtonPressedSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_menu_button_pressed_generated.png");
            serializedObject.FindProperty("mainOptionsPanelSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_options_panel_generated.png");
            serializedObject.FindProperty("mainOptionToggleSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_option_toggle_generated.png");
            serializedObject.FindProperty("mainOptionToggleHoverSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_option_toggle_hover_generated.png");
            serializedObject.FindProperty("mainOptionTogglePressedSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_option_toggle_pressed_generated.png");
            serializedObject.FindProperty("mainOptionSliderSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/MainMenu/main_option_slider_generated.png");
            serializedObject.FindProperty("volumeSliderBarSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Settings/volume_slider_bar_no_center_ornament.png");
            serializedObject.FindProperty("selectionFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Frames/selection_hover_frame.png");
            serializedObject.FindProperty("survivorTitleBadgeSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/ClassSelection/survivor_title_three_doors.png");
            serializedObject.FindProperty("debtClearedTitleBadgeSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/ClassSelection/debt_cleared_title_all_debts.png");
            serializedObject.FindProperty("healthBarFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Bars/health_bar_frame.png");
            serializedObject.FindProperty("healthBarFillSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Bars/health_bar_fill.png");
            serializedObject.FindProperty("attackCardFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/card_frame_attack_v2.png");
            serializedObject.FindProperty("defenseCardFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/card_frame_defense_v2.png");
            serializedObject.FindProperty("skillCardFrameSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/GeneratedFrames/card_frame_skill_v2.png");
            serializedObject.FindProperty("victoryCrackOverlaySprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Victory/victory_crack_overlay.png");
            serializedObject.FindProperty("victoryImpactSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Victory/victory_red_impact_overlay.png");
            serializedObject.FindProperty("victoryShardBurstSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Victory/victory_ember_shards_overlay.png");
            serializedObject.FindProperty("victoryLogoSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/Victory/victory_logo.png");
            serializedObject.FindProperty("combatFeedbackAttackSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_attack_success.png");
            serializedObject.FindProperty("combatFeedbackDefenseSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_defense_success.png");
            serializedObject.FindProperty("combatFeedbackBlockedSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_blocked.png");
            serializedObject.FindProperty("combatFeedbackCriticalSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_critical.png");
            serializedObject.FindProperty("combatFeedbackProphecySprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_prophecy_success.png");
            serializedObject.FindProperty("combatFeedbackTraitSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_trait_manifest.png");
            serializedObject.FindProperty("combatFeedbackComboSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_combo_trigger.png");
            serializedObject.FindProperty("combatFeedbackCurseSprite").objectReferenceValue = LoadSprite("Assets/Art/UI/CombatFeedback/combat_feedback_curse_contract.png");
        }

        private static void AssignGameOverSprites(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("gameOverLogoSprite").objectReferenceValue = LoadSprite("Assets/Art/GameOver/Text/game_over_logo.png");
            serializedObject.FindProperty("gameOverCrackOverlaySprite").objectReferenceValue =
                LoadSprite("Assets/Art/GameOver/Overlays/game_over_fullscreen_shatter_overlay_v2.png");
            AssignSpriteList(
                serializedObject,
                "gameOverBackgroundSprites",
                Enumerable.Range(1, 9).Select(index => $"Assets/Art/GameOver/Backgrounds/game_over_bg_{index:00}.png"));
            AssignSpriteList(
                serializedObject,
                "gameOverMessageSprites",
                Enumerable.Range(1, 9).Select(index => $"Assets/Art/GameOver/Text/game_over_message_{index:00}.png"));
            serializedObject.FindProperty("gamblerHiddenGameOverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/GameOver/Hidden/game_over_hidden_gambler.png");
            serializedObject.FindProperty("oracleHiddenGameOverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/GameOver/Hidden/game_over_hidden_oracle.png");
            serializedObject.FindProperty("exileHiddenGameOverSprite").objectReferenceValue =
                LoadSprite("Assets/Art/GameOver/Hidden/game_over_hidden_exile.png");
            serializedObject.FindProperty("hiddenGameOverChance").floatValue = 0.20f;
        }

        private static void AssignJourneyEndingSprites(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("journeyEndingLogoSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Text/the_start_of_another_journey_logo.png");
            AssignSpriteList(
                serializedObject,
                "journeyEndingBackgroundSprites",
                Enumerable.Range(1, 4).Select(index => $"Assets/Art/Ending/Backgrounds/journey_ending_bg_{index:00}.png"));
            serializedObject.FindProperty("gamblerJourneyEndingLogoSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Text/hidden_ending_gambler_logo.png");
            serializedObject.FindProperty("gamblerJourneyEndingBackgroundSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Backgrounds/hidden_ending_gambler.png");
            serializedObject.FindProperty("oracleJourneyEndingLogoSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Text/hidden_ending_oracle_logo.png");
            serializedObject.FindProperty("oracleJourneyEndingBackgroundSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Backgrounds/hidden_ending_oracle.png");
            serializedObject.FindProperty("exileJourneyEndingLogoSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Text/hidden_ending_exile_logo.png");
            serializedObject.FindProperty("exileJourneyEndingBackgroundSprite").objectReferenceValue =
                LoadSprite("Assets/Art/Ending/Backgrounds/hidden_ending_exile.png");
        }

        private static void AssignDiceSprites(SerializedObject serializedObject)
        {
            SerializedProperty diceSprites = serializedObject.FindProperty("diceSprites");
            diceSprites.arraySize = 6;
            for (int i = 0; i < 6; i += 1)
            {
                diceSprites.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"Assets/Art/UI/Dice/dice_{i + 1}.png");
            }
        }

        private static void AssignClassDiceSprites(SerializedObject serializedObject)
        {
            AssignSpriteList(serializedObject, "gamblerDiceSprites", StaticDicePaths("Gambler", "dice_gambler"));
            AssignSpriteList(serializedObject, "oracleDiceSprites", StaticDicePaths("Oracle", "dice_oracle"));
            AssignSpriteList(serializedObject, "exileDiceSprites", StaticDicePaths("Exile", "dice_exile"));

            AssignSpriteList(serializedObject, "gamblerDiceRollSprites", RollDicePaths("Gambler", "dice_gambler"));
            AssignSpriteList(serializedObject, "oracleDiceRollSprites", RollDicePaths("Oracle", "dice_oracle"));
            AssignSpriteList(serializedObject, "exileDiceRollSprites", RollDicePaths("Exile", "dice_exile"));
        }

        private static IEnumerable<string> StaticDicePaths(string folder, string prefix)
        {
            for (int value = 1; value <= 6; value += 1)
            {
                yield return $"Assets/Art/UI/Dice/{folder}/{prefix}_{value}.png";
            }
        }

        private static IEnumerable<string> RollDicePaths(string folder, string prefix)
        {
            for (int frame = 0; frame < 12; frame += 1)
            {
                yield return $"Assets/Art/UI/Dice/{folder}/Roll/{prefix}_roll_{frame:00}.png";
            }
        }

        private static void AssignAudio(SerializedObject serializedObject)
        {
            serializedObject.FindProperty("mainMenuMusicClip").objectReferenceValue = LoadAudioClip(MainMenuMusicPath);
            serializedObject.FindProperty("battleMusicClip").objectReferenceValue = LoadAudioClip(BattleMusicPath);
            serializedObject.FindProperty("nonCombatMusicClip").objectReferenceValue = LoadAudioClip(NonCombatMusicPath);
            serializedObject.FindProperty("bossMusicClip").objectReferenceValue = LoadAudioClip(BossMusicPath);
            serializedObject.FindProperty("deathMusicClip").objectReferenceValue = LoadAudioClip(DeathMusicPath);
            serializedObject.FindProperty("musicVolume").floatValue = 0.46f;
            serializedObject.FindProperty("sfxVolume").floatValue = 0.86f;
            AssignAudioClipList(
                serializedObject,
                "attackImpactClips",
                new[]
                {
                    $"{ImpactSfxRoot}/impact_attack_01.wav",
                    $"{ImpactSfxRoot}/impact_attack_02.wav",
                    $"{ImpactSfxRoot}/impact_attack_03.wav"
                });
            serializedObject.FindProperty("criticalImpactClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/impact_critical.wav");
            AssignAudioClipList(
                serializedObject,
                "defenseImpactClips",
                new[]
                {
                    $"{ImpactSfxRoot}/impact_defense_01.wav",
                    $"{ImpactSfxRoot}/impact_defense_02.wav"
                });
            AssignAudioClipList(
                serializedObject,
                "blockedImpactClips",
                new[]
                {
                    $"{ImpactSfxRoot}/impact_blocked_01.wav",
                    $"{ImpactSfxRoot}/impact_blocked_02.wav"
                });
            serializedObject.FindProperty("plateSettleClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/detail_plate_settle.wav");
            serializedObject.FindProperty("prophecyDetailClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/detail_prophecy.wav");
            serializedObject.FindProperty("traitDetailClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/detail_trait.wav");
            serializedObject.FindProperty("comboDetailClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/detail_combo.wav");
            serializedObject.FindProperty("curseDetailClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/detail_curse.wav");
            serializedObject.FindProperty("bossStartImpactClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/impact_boss_start.wav");
            serializedObject.FindProperty("bossVictoryImpactClip").objectReferenceValue =
                LoadAudioClip($"{ImpactSfxRoot}/impact_boss_victory.wav");
            AssignAudioClip(serializedObject, "uiDeniedClip", "UI/ui_denied.wav");
            AssignAudioClipList(
                serializedObject,
                "cardDrawClips",
                new[] { $"{GameSfxRoot}/Cards/card_draw_01.wav", $"{GameSfxRoot}/Cards/card_draw_02.wav" });
            AssignAudioClipList(
                serializedObject,
                "cardPlayClips",
                new[] { $"{GameSfxRoot}/Cards/card_play_01.wav", $"{GameSfxRoot}/Cards/card_play_02.wav" });
            AssignAudioClip(serializedObject, "cardDiscardClip", "Cards/card_discard.wav");
            AssignAudioClip(serializedObject, "doorOpenClip", "World/door_open.wav");
            AssignAudioClip(serializedObject, "turnCommitClip", "World/turn_commit.wav");
            AssignAudioClip(serializedObject, "diceRollClip", "World/dice_roll.wav");
            AssignAudioClip(serializedObject, "playerHitClip", "World/player_hit.wav");
            AssignAudioClip(serializedObject, "healClip", "World/heal.wav");
            AssignAudioClip(serializedObject, "combatStartClip", "World/combat_start.wav");
            AssignAudioClip(serializedObject, "enemyDefeatClip", "World/enemy_defeat.wav");
            AssignAudioClip(serializedObject, "treasureOpenClip", "World/treasure_open.wav");
            AssignAudioClip(serializedObject, "eventChoiceClip", "World/event_choice.wav");
            AssignAudioClip(serializedObject, "restClip", "World/rest.wav");
            AssignAudioClip(serializedObject, "curseAcceptClip", "World/curse_accept.wav");
            AssignAudioClip(serializedObject, "defeatClip", "World/defeat.wav");
            AssignAudioClip(serializedObject, "victoryClip", "World/victory.wav");
            AssignAudioClip(serializedObject, "endingClip", "World/ending.wav");
            AssignAudioClip(serializedObject, "rewardRevealClip", "Rewards/reward_reveal.wav");
            AssignAudioClip(serializedObject, "rewardClaimClip", "Rewards/reward_claim.wav");
            AssignAudioClip(serializedObject, "goldGainClip", "Rewards/gold_gain.wav");
            AssignAudioClip(serializedObject, "purchaseClip", "Rewards/purchase.wav");
            AssignAudioClip(serializedObject, "upgradeClip", "Rewards/upgrade.wav");
            AssignAudioClip(serializedObject, "itemEquipClip", "Rewards/item_equip.wav");
            AssignAudioClip(serializedObject, "saveSuccessClip", "Rewards/save_success.wav");
            AssignAudioClip(serializedObject, "saveFailureClip", "Rewards/save_failure.wav");
            AssignAudioClip(serializedObject, "loadSuccessClip", "Rewards/load_success.wav");
            AssignAudioClip(serializedObject, "loadFailureClip", "Rewards/load_failure.wav");
        }

        private static void AssignAudioClip(SerializedObject serializedObject, string propertyName, string relativePath)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue =
                LoadAudioClip($"{GameSfxRoot}/{relativePath}");
        }

        private static void AssignAudioClipList(SerializedObject serializedObject, string propertyName, IEnumerable<string> assetPaths)
        {
            string[] paths = assetPaths.ToArray();
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i += 1)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = LoadAudioClip(paths[i]);
            }
        }

        private static void AssignSpriteList(SerializedObject serializedObject, string propertyName, IEnumerable<string> assetPaths)
        {
            string[] paths = assetPaths.ToArray();
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i += 1)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(paths[i]);
            }
        }

        private static void AssignEnemySprites(SerializedObject serializedObject)
        {
            SerializedProperty enemySprites = serializedObject.FindProperty("enemySprites");
            enemySprites.arraySize = EnemySpritePaths.Length;
            for (int i = 0; i < EnemySpritePaths.Length; i += 1)
            {
                SerializedProperty element = enemySprites.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("enemyId").stringValue = EnemySpritePaths[i].Id;
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(EnemySpritePaths[i].Path);
            }
        }

        private static void AssignEnemyHudFrameSprites(SerializedObject serializedObject)
        {
            SerializedProperty enemyHudFrameSprites = serializedObject.FindProperty("enemyHudFrameSprites");
            enemyHudFrameSprites.arraySize = EnemyHudFrameIds.Length;
            for (int i = 0; i < EnemyHudFrameIds.Length; i += 1)
            {
                SerializedProperty element = enemyHudFrameSprites.GetArrayElementAtIndex(i);
                string enemyId = EnemyHudFrameIds[i];
                element.FindPropertyRelative("enemyId").stringValue = enemyId;
                element.FindPropertyRelative("sprite").objectReferenceValue =
                    LoadSprite($"Assets/Art/UI/MonsterHudFrames/ui_enemy_hud_{enemyId}.png");
            }
        }

        private static void ImportArt()
        {
            foreach (string file in Directory.GetFiles("Assets/Art", "*.png", SearchOption.AllDirectories))
            {
                string assetPath = NormalizeAssetPath(file);
                ConfigureArtTextureImporter(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            foreach (string file in Directory.GetFiles("Assets/Fonts", "*.ttf", SearchOption.AllDirectories))
            {
                AssetDatabase.ImportAsset(NormalizeAssetPath(file), ImportAssetOptions.ForceUpdate);
            }

            foreach (string file in Directory.GetFiles(MusicRoot, "*.mp3", SearchOption.AllDirectories))
            {
                string assetPath = NormalizeAssetPath(file);
                ConfigureMusicAudioImporter(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            foreach (string file in Directory.GetFiles(GameSfxRoot, "*.wav", SearchOption.AllDirectories))
            {
                string assetPath = NormalizeAssetPath(file);
                ConfigureSfxAudioImporter(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void ImportEnglishLocalizedCardsForWebGL()
        {
            if (!Directory.Exists(EnglishLocalizedCardRoot))
            {
                throw new DirectoryNotFoundException(
                    $"English localized card root was not found: {EnglishLocalizedCardRoot}");
            }

            string[] files = Directory.GetFiles(
                EnglishLocalizedCardRoot,
                "*.png",
                SearchOption.TopDirectoryOnly);
            foreach (string file in files.OrderBy(path => path, StringComparer.Ordinal))
            {
                string assetPath = NormalizeAssetPath(file);
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    throw new InvalidOperationException(
                        $"English localized card is not a texture asset: {assetPath}");
                }

                ApplyEnglishLocalizedWebGLTextureSettings(importer);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
        }

        private static void ApplyEnglishLocalizedWebGLTextureSettings(TextureImporter importer)
        {
            TextureImporterPlatformSettings settings =
                importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = true;
            settings.maxTextureSize = 2048;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 100;
            settings.format = TextureImporterFormat.DXT5;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void ConfigureArtTextureImporter(string assetPath)
        {
            bool usesHighQualityDesktopSettings =
                assetPath.StartsWith("Assets/Art/Cards/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/Doors/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/MonsterHudFrames/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/Ending/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/Victory/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/CombatFeedback/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/GeneratedFrames/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/Settings/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/GameOver/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/UI/ClassSelection/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/Characters/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/Art/RunModifiers/", StringComparison.Ordinal);

            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            ApplyMobileTextureSettings(importer, "iPhone");
            if (!usesHighQualityDesktopSettings)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipMapBias = 0f;

            if (assetPath.EndsWith("ui_status_inner_panel_frame_ai.png", StringComparison.Ordinal))
            {
                importer.spriteBorder = new Vector4(96f, 96f, 96f, 96f);
            }
            else if (assetPath.EndsWith("ui_status_inner_header_frame_ai.png", StringComparison.Ordinal))
            {
                importer.spriteBorder = new Vector4(84f, 54f, 84f, 54f);
            }
            else if (assetPath.EndsWith("ui_status_item_slot_frame_ai.png", StringComparison.Ordinal))
            {
                importer.spriteBorder = new Vector4(96f, 82f, 96f, 82f);
            }

            ApplyCardPlatformTextureSettings(importer, "Standalone");
            ApplyWebGLTextureSettings(importer);
            ApplyAndroidTextureSettings(importer);
        }

        private static void ConfigureMusicAudioImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not AudioImporter importer)
            {
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
        }

        private static void ConfigureSfxAudioImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not AudioImporter importer)
            {
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.quality = 1f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
        }

        private static void ApplyCardPlatformTextureSettings(TextureImporter importer, string buildTarget)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(buildTarget);
            settings.overridden = true;
            settings.maxTextureSize = 4096;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.compressionQuality = 100;
            settings.format = TextureImporterFormat.Automatic;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void ApplyWebGLTextureSettings(TextureImporter importer)
        {
            TextureImporterPlatformSettings settings =
                importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = true;
            settings.maxTextureSize = 4096;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 100;
            settings.format = TextureImporterFormat.DXT5;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void ApplyAndroidTextureSettings(TextureImporter importer)
        {
            ApplyMobileTextureSettings(importer, "Android");
        }

        private static void ApplyMobileTextureSettings(TextureImporter importer, string buildTarget)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(buildTarget);
            settings.overridden = true;
            settings.maxTextureSize = 2048;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 85;
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(normalizedPath);
            if (sprite == null)
            {
                Debug.LogWarning($"Sprite not found: {normalizedPath}");
            }
            return sprite;
        }

        private static AudioClip LoadAudioClip(string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalizedPath));
            if (File.Exists(fullPath))
            {
                AssetDatabase.ImportAsset(normalizedPath, ImportAssetOptions.ForceUpdate);
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(normalizedPath);
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip not found: {normalizedPath}");
            }
            return clip;
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        [Serializable]
        private sealed class RunModifierCatalogData
        {
            public List<RunModifierCatalogEntry> modifiers = new();
        }

        [Serializable]
        private sealed class RunModifierCatalogEntry
        {
            public string id = string.Empty;
            public string icon = string.Empty;
        }
    }
}
