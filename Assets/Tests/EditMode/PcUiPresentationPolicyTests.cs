using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class PcUiPresentationPolicyTests
    {
        private const string CardModeTypeName = "ThreeDoorsOfFate.UI.CardPresentationMode, Assembly-CSharp";
        private const string CardPolicyTypeName = "ThreeDoorsOfFate.UI.CardPresentationPolicy, Assembly-CSharp";
        private const string LayoutPolicyTypeName = "ThreeDoorsOfFate.UI.PcUiLayoutPolicy, Assembly-CSharp";
        private const string SettingsGearFactoryTypeName = "ThreeDoorsOfFate.UI.SettingsGearSpriteFactory, Assembly-CSharp";
        private const string ControllerPath = "Assets/Scripts/Game/ThreeDoorsGameController.cs";
        private const string AchievementControllerPath = "Assets/Scripts/Game/ThreeDoorsGameController.Achievements.cs";
        private const string LocalizationControllerPath = "Assets/Scripts/Game/ThreeDoorsGameController.Localization.cs";
        private const string ScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string PlayableBuilderPath = "Assets/Editor/PlayableGameBuilder.cs";

        [Test]
        public void CompactCards_HideRulesButKeepCostAndTitle()
        {
            object style = GetCardStyle("Compact");

            Assert.That(GetProperty<bool>(style, "ShowCost"), Is.True);
            Assert.That(GetProperty<bool>(style, "ShowTitle"), Is.True);
            Assert.That(GetProperty<bool>(style, "ShowRules"), Is.False);
            Assert.That(GetProperty<int>(style, "TitleFontSize"), Is.GreaterThanOrEqualTo(18));
        }

        [Test]
        public void StandardAndDetailCards_KeepReadableRules()
        {
            object standard = GetCardStyle("Standard");
            object detail = GetCardStyle("Detail");

            Assert.That(GetProperty<bool>(standard, "ShowRules"), Is.True);
            Assert.That(GetProperty<int>(standard, "RulesFontSize"), Is.GreaterThanOrEqualTo(16));
            Assert.That(GetProperty<int>(detail, "RulesFontSize"), Is.GreaterThan(GetProperty<int>(standard, "RulesFontSize")));
        }

        [Test]
        public void HeaderRegions_ShareVerticalBoundsAndNeverOverlap()
        {
            object title = GetLayoutField("HeaderTitle");
            object prompt = GetLayoutField("HeaderPrompt");
            object stats = GetLayoutField("HeaderStats");
            object settings = GetLayoutField("HeaderSettings");

            Assert.That(GetProperty<float>(title, "MinY"), Is.EqualTo(GetProperty<float>(prompt, "MinY")));
            Assert.That(GetProperty<float>(title, "MaxY"), Is.EqualTo(GetProperty<float>(prompt, "MaxY")));
            Assert.That(GetProperty<float>(prompt, "MaxX"), Is.LessThanOrEqualTo(GetProperty<float>(stats, "MinX")));
            Assert.That(GetProperty<float>(stats, "MaxX"), Is.LessThanOrEqualTo(GetProperty<float>(settings, "MinX")));
        }

        [Test]
        public void HeaderPromptRoot_HasExactlyTheRenderedTitleBoxHeight()
        {
            object topBar = GetLayoutField("TopBar");
            object title = GetLayoutField("HeaderTitle");
            object promptRoot = GetLayoutField("HeaderPromptRoot");

            float expectedHeight = GetProperty<float>(topBar, "Height") * GetProperty<float>(title, "Height");
            Assert.That(GetProperty<float>(promptRoot, "Height"), Is.EqualTo(expectedHeight).Within(0.0001f));
        }

        [Test]
        public void HeaderPrompt_UsesTheTitleFrameAndKeepsItsTextAsAChild()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("Sprite titleBoxSprite = GetTopHeaderBoxSprite();", source);
            StringAssert.Contains("subtitleFrame = AddPanel(topBar, \"부제 박스\", Color.white, titleBoxSprite);", source);
            StringAssert.Contains("subtitleText = AddText(subtitleFrame,", source);
            StringAssert.Contains("SetAnchors(subtitleFrame, PcUiLayoutPolicy.HeaderPrompt);", source);
        }

        [Test]
        public void LogAndDoorTextSafeAreas_ClearTheirOrnamentalBorders()
        {
            object logBody = GetLayoutField("LogBody");
            object logText = GetLayoutField("LogTextSafe");
            object doorHint = GetLayoutField("DoorHintSafe");

            Assert.That(GetProperty<float>(logBody, "MinX"), Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(GetProperty<float>(logBody, "MaxX"), Is.LessThanOrEqualTo(0.90f));
            Assert.That(GetProperty<float>(logBody, "MaxY"), Is.LessThanOrEqualTo(0.86f));
            Assert.That(GetProperty<float>(logText, "MinX"), Is.GreaterThanOrEqualTo(0.09f));
            Assert.That(GetProperty<float>(logText, "MaxX"), Is.LessThanOrEqualTo(0.93f));
            Assert.That(GetProperty<float>(doorHint, "MinX"), Is.GreaterThanOrEqualTo(0.16f));
            Assert.That(GetProperty<float>(doorHint, "MaxX"), Is.LessThanOrEqualTo(0.84f));
            Assert.That(GetProperty<float>(doorHint, "MinY"), Is.GreaterThanOrEqualTo(0.18f));

            string source = File.ReadAllText(ControllerPath);
            StringAssert.Contains("SetAnchors(body.rectTransform, PcUiLayoutPolicy.LogTextSafe);", source);
            StringAssert.Contains("PcUiLayoutPolicy.DoorHintSafe", source);
            StringAssert.Contains("\"문 설명 안전영역\"", source);
        }

        [Test]
        public void ClassDetailLayout_AlwaysProvidesASettingsEntry()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains(
                "AddLocalizedSettingsMenuButton(\n                contentRoot,\n                \"캐릭터 상세 설정\"",
                source);
            StringAssert.Contains(
                "classDetailSettingsButton.onClick.AddListener(ToggleSettingsPanel);",
                source);
            StringAssert.Contains(
                "new Vector2(0.835f, 0.905f),\n                new Vector2(0.965f, 0.985f)",
                source);
        }

        [Test]
        public void ShopRunItem_ClipsArtworkBelowOneExistingFrameOverlay()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("\"아이템 상품 그림 마스크 영역\"", source);
            StringAssert.Contains("artViewport.gameObject.AddComponent<RectMask2D>();", source);
            StringAssert.Contains("AddImage(artViewport, \"아이템 상품 아이콘\"", source);
            StringAssert.Contains("\"아이템 상품 프레임 오버레이\"", source);
            StringAssert.Contains("frameOverlay.sprite = itemFrame;", source);
            StringAssert.Contains("frameOverlay.rectTransform.SetAsLastSibling();", source);
            StringAssert.DoesNotContain(
                "AddPanel(slot, \"아이템 상품 프레임\", Color.white, GetRunStatusSlotFrameSprite())",
                source);
        }

        [Test]
        public void StatusAndCollectionSafeAreas_StayInsideNormalizedBounds()
        {
            Type policyType = Type.GetType(LayoutPolicyTypeName);
            Assert.That(policyType, Is.Not.Null);
            IEnumerable safeAreas = (IEnumerable)policyType.GetProperty("AllSafeAreas", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (object rect in safeAreas)
            {
                Assert.That(GetProperty<bool>(rect, "IsValid"), Is.True);
                Assert.That(GetProperty<float>(rect, "MinX"), Is.InRange(0f, 1f));
                Assert.That(GetProperty<float>(rect, "MaxX"), Is.InRange(0f, 1f));
                Assert.That(GetProperty<float>(rect, "MinY"), Is.InRange(0f, 1f));
                Assert.That(GetProperty<float>(rect, "MaxY"), Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void RunStatusMainRegions_HaveConsistentGapsAndNeverOverlap()
        {
            object equipment = GetLayoutField("StatusEquipmentPanel");
            object synergy = GetLayoutField("StatusSynergyCard");
            object deck = GetLayoutField("StatusDeckCard");
            object awakening = GetLayoutField("StatusAwakeningCard");
            object traits = GetLayoutField("StatusTraitsCard");

            Assert.That(GetProperty<float>(equipment, "MaxX"), Is.LessThan(GetProperty<float>(synergy, "MinX")));
            Assert.That(GetProperty<float>(synergy, "MaxX"), Is.LessThan(GetProperty<float>(deck, "MinX")));
            Assert.That(GetProperty<float>(awakening, "MaxX"), Is.LessThan(GetProperty<float>(traits, "MinX")));
            Assert.That(GetProperty<float>(awakening, "MaxY"), Is.LessThan(GetProperty<float>(synergy, "MinY")));
            Assert.That(GetProperty<float>(traits, "MaxY"), Is.LessThan(GetProperty<float>(deck, "MinY")));
        }

        [Test]
        public void RunStatusMainRegions_StayInsideTheOrnamentalFrameSafeInset()
        {
            string[] regionNames =
            {
                "StatusEquipmentPanel",
                "StatusSynergyCard",
                "StatusDeckCard",
                "StatusAwakeningCard",
                "StatusTraitsCard"
            };

            foreach (string regionName in regionNames)
            {
                object region = GetLayoutField(regionName);
                Assert.That(
                    GetProperty<float>(region, "MinX"),
                    Is.GreaterThanOrEqualTo(0.065f),
                    regionName);
                Assert.That(
                    GetProperty<float>(region, "MaxX"),
                    Is.LessThanOrEqualTo(0.935f),
                    regionName);
                Assert.That(
                    GetProperty<float>(region, "MinY"),
                    Is.GreaterThanOrEqualTo(0.170f),
                    regionName);
                Assert.That(
                    GetProperty<float>(region, "MaxY"),
                    Is.LessThanOrEqualTo(0.820f),
                    regionName);
            }
        }

        [Test]
        public void RunStatusMainAndDetailText_UseContainedUiBoxes()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("보유 효과 패널", source);
            StringAssert.Contains("AddRunStatusContentBox(parent, $\"{name} 박스\", min, max, frameSprite)", source);
            StringAssert.Contains("BuildCombinationOverviewText()", source);
            StringAssert.Contains("BuildCombatAwakeningSummaryText()", source);
            StringAssert.Contains("BuildCharacterTraitSummaryText()", source);
        }

        [Test]
        public void StatusDetailFrames_ReserveSafeInsetsForDecorationsAndDenseText()
        {
            object detailBody = GetLayoutField("StatusDetailBody");
            Assert.That(GetProperty<float>(detailBody, "MinY"), Is.GreaterThanOrEqualTo(0.15f));
            Assert.That(GetProperty<float>(detailBody, "MaxY"), Is.LessThanOrEqualTo(0.77f));

            Type policyType = Type.GetType(LayoutPolicyTypeName);
            Assert.That(policyType, Is.Not.Null);
            FieldInfo safeInsetField = policyType.GetField("StatusFramedTextSafe", BindingFlags.Public | BindingFlags.Static);
            Assert.That(safeInsetField, Is.Not.Null);
            object safeInset = safeInsetField.GetValue(null);
            Assert.That(GetProperty<float>(safeInset, "MinX"), Is.GreaterThanOrEqualTo(0.12f));
            Assert.That(GetProperty<float>(safeInset, "MaxX"), Is.LessThanOrEqualTo(0.88f));
            Assert.That(GetProperty<float>(safeInset, "MinY"), Is.GreaterThanOrEqualTo(0.11f));
            Assert.That(GetProperty<float>(safeInset, "MaxY"), Is.LessThanOrEqualTo(0.89f));

            string source = File.ReadAllText(ControllerPath);
            StringAssert.Contains("PcUiLayoutPolicy.StatusFramedTextSafe", source);
            StringAssert.Contains("detailText.resizeTextForBestFit = true", source);
        }

        [Test]
        public void CollectionDescription_UsesGeneratedFrameAboveTheOuterOrnament()
        {
            object collectionDetail = GetLayoutField("CollectionDetail");
            Assert.That(GetProperty<float>(collectionDetail, "MinY"), Is.GreaterThanOrEqualTo(0.18f));

            string source = File.ReadAllText(ControllerPath);
            int start = source.IndexOf("private void AddRunItemCollectionDescription", StringComparison.Ordinal);
            int end = source.IndexOf("private RunItemDefinition ResolveSelectedRunItemForCollection", start, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            Assert.That(end, Is.GreaterThan(start));
            string method = source.Substring(start, end - start);

            StringAssert.Contains("AddRunStatusContentBox(", method);
            StringAssert.Contains("PcUiLayoutPolicy.CollectionDetail", method);
            StringAssert.DoesNotContain("AddComponent<Outline>", method);
            StringAssert.DoesNotContain("설명 강조선", method);
        }

        [Test]
        public void RunStatusContentBoxes_UseFlatNestedBordersWithoutOrnamentalFrameSpikes()
        {
            string source = File.ReadAllText(ControllerPath);
            int start = source.IndexOf("private RectTransform AddRunStatusContentBox", StringComparison.Ordinal);
            int end = source.IndexOf("private void AddDetailColumns", start, StringComparison.Ordinal);

            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            Assert.That(end, Is.GreaterThan(start));
            string method = source.Substring(start, end - start);
            StringAssert.Contains("내용 내부 테두리", method);
            StringAssert.Contains("내용 암석 배경", method);
            StringAssert.DoesNotContain("GetRunStatusBoxFrameSprite", method);
            StringAssert.DoesNotContain("AddComponent<Outline>", method);
            StringAssert.Contains("AddRunStatusFlatLabelBox(", source);
        }

        [TestCase("Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png")]
        [TestCase("Assets/Art/UI/GeneratedFrames/ui_status_inner_header_frame_ai.png")]
        [TestCase("Assets/Art/UI/GeneratedFrames/ui_status_item_slot_frame_ai.png")]
        public void GeneratedStatusFrames_HaveRealTransparentCenters(string assetPath)
        {
            Assert.That(File.Exists(assetPath), Is.True, assetPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(assetPath)), Is.True, assetPath);
                Color center = texture.GetPixel(texture.width / 2, texture.height / 2);
                Assert.That(center.a, Is.LessThan(0.05f), $"{assetPath} center must be transparent.");
                Assert.That(texture.GetPixels32().Any(pixel => pixel.a > 200), Is.True, $"{assetPath} frame pixels are missing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void StatusFrameAssets_AreBoundByTheProtectedSceneBuilder()
        {
            string source = File.ReadAllText(ControllerPath);
            string builder = File.ReadAllText(PlayableBuilderPath);

            StringAssert.Contains("statusInnerPanelFrameSprite", source);
            StringAssert.Contains("statusInnerHeaderFrameSprite", source);
            StringAssert.Contains("statusItemSlotFrameSprite", source);
            StringAssert.Contains("ui_status_inner_panel_frame_ai.png", builder);
            StringAssert.Contains("ui_status_inner_header_frame_ai.png", builder);
            StringAssert.Contains("ui_status_item_slot_frame_ai.png", builder);
        }

        [Test]
        public void RuntimeCards_UseStructuredLayersInsteadOfShrinkingFullCardPngText()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.DoesNotContain("frame.sprite = card.FullCardSprite", source);
            StringAssert.Contains("PopulateCardPresentation(cardPanel, card, mode)", source);
            StringAssert.Contains("CardPresentationMode.Detail", source);
        }

        [Test]
        public void PlayableScene_BindsPortableKoreanBodyAndTitleFonts()
        {
            string scene = File.ReadAllText(ScenePath);

            StringAssert.Contains("uiFontAsset: {fileID: 12800000, guid: f55990bdfa1d2814fb128f2ffcdf8a9f", scene);
            StringAssert.Contains("titleFontAsset: {fileID: 12800000, guid: d7d707c9a21b8254ab2076d6ad826e30", scene);

            string builder = File.ReadAllText(PlayableBuilderPath);
            StringAssert.Contains("Assets/Fonts/NotoSansKR-VF.ttf", builder);
            StringAssert.Contains("Assets/Fonts/GowunBatang-Bold.ttf", builder);
        }

        [Test]
        public void TopBarSettingsControl_UsesIntegratedFramedLabel()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("AddLocalizedSettingsMenuButton(topBar, \"설정 버튼\", \"menu.settings\", 15)", source);
            StringAssert.DoesNotContain("AddIconButton(topBar, \"설정 버튼\", GetSettingsGearSprite())", source);
        }

        [Test]
        public void MainMenuOptionsControl_ShowsDedicatedGearIconBesideLabel()
        {
            string source = File.ReadAllText(ControllerPath);
            string localizationSource = File.ReadAllText(LocalizationControllerPath);

            StringAssert.Contains("AddMainMenuIconButton(", source);
            StringAssert.Contains("\"설정 톱니바퀴\"", localizationSource);
            StringAssert.Contains("new Vector2(0.36f, 0.12f)", localizationSource);
            StringAssert.Contains("new Vector2(0.035f, 0.12f)", localizationSource);
        }

        [Test]
        public void SettingsGearSprite_CropsOrnamentalFrameToReadableCenter()
        {
            Sprite source = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/Settings/settings_icon_generated.png");
            Assert.That(source, Is.Not.Null);

            Type factoryType = Type.GetType(SettingsGearFactoryTypeName);
            Assert.That(factoryType, Is.Not.Null, "A dedicated crop policy must keep the gear readable at button size.");
            MethodInfo create = factoryType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            Assert.That(create, Is.Not.Null);

            Sprite cropped = (Sprite)create.Invoke(null, new object[] { source });
            try
            {
                Assert.That(cropped, Is.Not.Null);
                Assert.That(cropped.rect.width, Is.LessThan(source.rect.width * 0.70f));
                Assert.That(cropped.rect.width, Is.GreaterThan(source.rect.width * 0.45f));
                Assert.That(cropped.rect.center.x, Is.EqualTo(source.rect.center.x).Within(0.01f));
                Assert.That(cropped.rect.center.y, Is.EqualTo(source.rect.center.y).Within(0.01f));
            }
            finally
            {
                if (cropped != null)
                {
                    UnityEngine.Object.DestroyImmediate(cropped);
                }
            }
        }

        [Test]
        public void AchievementGallery_CombinesFourLegacyAndSixteenReleaseAchievements()
        {
            string source = File.ReadAllText(AchievementControllerPath);

            StringAssert.Contains("DifficultyUnlockKey", source);
            StringAssert.Contains("GetTrueEndingKey(CharacterClass.Gambler)", source);
            StringAssert.Contains("GetTrueEndingKey(CharacterClass.Oracle)", source);
            StringAssert.Contains("GetTrueEndingKey(CharacterClass.Exile)", source);
            StringAssert.Contains("AchievementProgress.NewDefinitions", source);
            StringAssert.Contains("AchievementSlotsPerPage = 10", source);
            StringAssert.Contains("GetAchievementCardModels()", source);
        }

        [Test]
        public void AchievementAndCollectionDetails_UseNonOverlappingPanels()
        {
            string source = File.ReadAllText(ControllerPath);
            string achievements = File.ReadAllText(AchievementControllerPath);

            StringAssert.Contains("achievementCardsRoot", achievements);
            StringAssert.Contains("achievementDetailRoot", achievements);
            StringAssert.Contains("AddAchievementSlot(", achievements);
            StringAssert.Contains("const float columnGap", achievements);
            StringAssert.Contains("const float rowGap", achievements);
            StringAssert.Contains("AddRunStatusContentBox(", source);
            StringAssert.Contains("PcUiLayoutPolicy.CollectionDetail", source);
        }

        [Test]
        public void ClassDetailTagline_UsesTheSameTopHeaderFrameAndSafeTextInset()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("AddTopHeaderLabelBox(", source);
            StringAssert.Contains("\"직업 한줄 설명\"", source);
            StringAssert.Contains("GetTopHeaderBoxSprite()", source);
            StringAssert.Contains("PcUiLayoutPolicy.ClassDetailTagline", source);
            StringAssert.DoesNotContain("AddEventMessagePanel(contentRoot, \"직업 한줄 설명\")", source);
        }

        [Test]
        public void AchievementCardTextSafeAreas_DoNotTouchDecorativeBorders()
        {
            object title = GetLayoutField("AchievementCardTitle");
            object body = GetLayoutField("AchievementCardBody");
            object icon = GetLayoutField("AchievementCardIcon");
            object safeText = GetLayoutField("AchievementCardTextSafe");

            Assert.That(GetProperty<float>(title, "MinX"), Is.GreaterThan(GetProperty<float>(icon, "MaxX")));
            Assert.That(GetProperty<float>(body, "MinX"), Is.GreaterThan(GetProperty<float>(icon, "MaxX")));
            Assert.That(GetProperty<float>(body, "MaxY"), Is.LessThan(GetProperty<float>(title, "MinY")));
            Assert.That(GetProperty<float>(safeText, "MinX"), Is.GreaterThanOrEqualTo(0.07f));
            Assert.That(GetProperty<float>(safeText, "MaxX"), Is.LessThanOrEqualTo(0.93f));
            Assert.That(GetProperty<float>(safeText, "MinY"), Is.GreaterThanOrEqualTo(0.14f));
            Assert.That(GetProperty<float>(safeText, "MaxY"), Is.LessThanOrEqualTo(0.86f));
        }

        [Test]
        public void CombatHand_IsRaisedAboveThePlayerStatusHud()
        {
            string source = File.ReadAllText(ControllerPath);

            StringAssert.Contains("SetAnchors(handPanel, new Vector2(0.145f, 0.040f), new Vector2(1.000f, 0.550f))", source);
            StringAssert.Contains("float bottom = 0.020f", source);
        }

        private static object GetCardStyle(string modeName)
        {
            Type modeType = Type.GetType(CardModeTypeName);
            Type policyType = Type.GetType(CardPolicyTypeName);
            Assert.That(modeType, Is.Not.Null);
            Assert.That(policyType, Is.Not.Null);
            object mode = Enum.Parse(modeType, modeName);
            return policyType.GetMethod("For", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { mode });
        }

        private static object GetLayoutField(string name)
        {
            Type policyType = Type.GetType(LayoutPolicyTypeName);
            Assert.That(policyType, Is.Not.Null);
            FieldInfo field = policyType.GetField(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing layout policy field: {name}");
            return field.GetValue(null);
        }

        private static T GetProperty<T>(object target, string name)
        {
            return (T)target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }
    }
}
