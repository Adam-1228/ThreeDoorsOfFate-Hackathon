using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ThreeDoorsOfFate.Game
{
    public enum DoorType
    {
        Battle,
        Elite,
        Shop,
        Treasure,
        Event,
        Rest,
        Curse,
        Boss
    }

    [Serializable]
    public sealed class DoorSpriteBinding
    {
        public DoorType doorType;
        public Sprite sprite;
        public Sprite hoverSprite;
    }

    [Serializable]
    public sealed class EnemySpriteBinding
    {
        public string enemyId = string.Empty;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class RunItemIconBinding
    {
        public string itemId = string.Empty;
        public Sprite sprite;
    }

    public sealed partial class ThreeDoorsGameController : MonoBehaviour
    {
        private const int TargetRooms = 10;
        private const int MinimumPreBossCombats = 3;
        private const int MaxConsecutiveNonCombatDoors = 3;
        private const int StartingAction = 3;
        private const int StartingHandSize = 5;
        private const int StartingDeckSize = 30;
        private const int StartingCategoryCardCount = 7;
        private const int StartingExtraAttackCardCount = 2;
        private const int StartingExtraDefenseCardCount = 2;
        private const int StartingExtraSkillCardCount = 1;
        private const int BottleOfLightHealthBonus = 8;
        private const int MaxDeckSizeBeforeEndless = 50;
        private const int MaxDeckSizeEndless = 60;
        private const float EnemyRevealFadeInSeconds = 0.32f;
        private const float EnemyRevealHoldSeconds = 0.65f;
        private const float EnemyRevealFadeSeconds = 1.05f;
        private const float EnemyRevealFadeOutScale = 2.55f;
        private const float CombatVictoryEffectSeconds = 2.20f;
        private const float CombinationImpactSeconds = 1.05f;
        private const float CombatFeedbackSeconds = 1.20f;
        private const int GamblerCardFlowAwakenThreshold = 15;
        private const int OraclePredictionAwakenThreshold = 3;
        private const int ExileCurseRemovalAwakenThreshold = 2;
        private const int DebtClearGoldCostPerDebt = 25;
        private const int EndlessBossInterval = 5;
        private const float DebtDoorPressurePerDebt = 0.085f;
        private const string DifficultyUnlockKey = "ThreeDoorsOfFate.DifficultyUnlocked";
        private const string TrueEndingUnlockPrefix = "ThreeDoorsOfFate.TrueEnding.";
        private const string EndlessRecordPrefix = "ThreeDoorsOfFate.EndlessRecord.";
        private const string EndlessRecordSeenKey = "ThreeDoorsOfFate.EndlessRecord.Seen";
        private const string SurvivorTitleUnlockPrefix = "ThreeDoorsOfFate.SurvivorTitle.";
        private const string HardRunSaveKey = "ThreeDoorsOfFate.HardRunSave";
        private const int HardRunSaveVersion = 1;
        private const string EquippedItemKeyPrefix = "ThreeDoorsOfFate.EquippedItems.";
        private const string DiscoveredItemKeyPrefix = "ThreeDoorsOfFate.DiscoveredItems.";
        private const string RunItemUnlockKeyPrefix = "ThreeDoorsOfFate.RunItemUnlock.";
        private const int MaxEquippedRunItems = 3;
        private const float ShopRunItemOfferChance = 0.35f;
        private const float EliteRunItemDiscoveryChance = 0.22f;
        private const float CurseDoorRunItemDiscoveryChance = 0.34f;
        private const string EasyBossId = "boss_gatekeeper_third_door";
        private const string NormalBossId = "boss_debt_adjudicator_normal";
        private const string HardBossId = "boss_usurer_of_the_abyss_hard";
        private const string DebtClearBossId = "boss_bottomless_creditor_special";
        private const string SurvivorTitleText = "3번의 문을 열고 생존하여 돌아온자";
        private const string DebtClearedTitleText = "모든것을 청산한 자";

        private static readonly string[] JourneyEndingMessages =
        {
            "세 번째 문은 닫히지 않았다. 당신은 그 너머의 길을 선택했다.",
            "금화와 보석은 잠시 빛날 뿐, 다음 문은 이미 숨을 고른다.",
            "안식은 끝이 아니라 다음 걸음을 위한 짧은 침묵이다.",
            "끊어진 계약 위로 새 길이 열린다.",
            "동굴은 당신을 놓아주었지만, 운명은 아직 문을 남겨두었다.",
            "문지기의 그림자가 사라지고, 또 다른 여정의 문턱이 밝아온다."
        };

        private static readonly RunItemDefinition[] FallbackRunItemDefinitions =
        {
            new("relic_gate_shard", "문지기의 파편", RunItemType.Relic, "쉬움 보스 격파 보상", "기본 유물 효과입니다.", string.Empty),
            new("blessing_clear_flame", "맑은 불꽃의 축복", RunItemType.Blessing, "보통 보스 격파 보상", "기본 축복 효과입니다.", string.Empty),
            new("curse_abyss_brand", "무저갱의 낙인", RunItemType.Curse, "어려움 보스 격파 보상", "기본 저주 효과입니다.", string.Empty)
        };

        private static readonly EnemyTemplate[] BaseEnemyTemplates =
        {
            new("monster_cave_lurker", "동굴 잠복자", 36, 7, 3),
            new("monster_debt_hound", "빚 사냥개", 40, 8, 3),
            new("monster_ash_gambler", "잿빛 도박꾼", 42, 8, 4),
            new("monster_rune_thief", "룬 도둑", 44, 9, 3),
            new("monster_candle_warden", "촛불 감시자", 52, 9, 6),
            new("monster_contract_knight", "계약 기사", 58, 10, 7),
            new("monster_hollow_collector", "공허 수금원", 62, 11, 7),
            new("monster_rift_spider", "균열 거미", 66, 12, 5),
            new("monster_curse_bearer", "계약 운반자", 70, 12, 8),
            new("monster_gold_mimic", "금빛 미믹", 74, 13, 8),
        };

        private static readonly EnemyTemplate[] HardModeEnemyTemplates =
        {
            new("monster_abyss_bailiff", "무저갱 집행관", 78, 14, 8),
            new("monster_ledger_moth", "장부 나방", 60, 13, 6),
            new("monster_coin_sutured_husk", "동전 봉합 망자", 84, 13, 10),
            new("monster_broken_scale_acolyte", "부서진 저울 수행자", 72, 15, 8),
            new("monster_rift_lamprey", "균열 흡혈충", 66, 16, 5),
            new("monster_contract_marionette", "계약 꼭두각시", 74, 14, 9),
            new("monster_oath_candle_revenant", "맹세 촛불 망령", 76, 15, 7),
            new("monster_void_tax_scribe", "공허 징세 필경사", 68, 14, 11),
            new("monster_debt_pit_bruiser", "부채굴 파수꾼", 92, 16, 10),
            new("monster_doorless_penitent", "문잃은 참회자", 80, 15, 9),
        };

        [Header("Cards")]
        [SerializeField] private List<CardData> cardPool = new();
        [SerializeField] private Sprite cardBackSprite;

        [Header("Run Modifiers")]
        [SerializeField] private TextAsset runModifierCatalog;
        [SerializeField] private List<RunItemIconBinding> runItemIcons = new();

        [Header("Doors")]
        [SerializeField] private List<DoorSpriteBinding> doorSprites = new();
        [SerializeField] private Sprite easyBossDoorSprite;
        [SerializeField] private Sprite easyBossDoorHoverSprite;
        [SerializeField] private Sprite normalBossDoorSprite;
        [SerializeField] private Sprite normalBossDoorHoverSprite;
        [SerializeField] private Sprite hardBossDoorSprite;
        [SerializeField] private Sprite hardBossDoorHoverSprite;

        [Header("Backgrounds")]
        [SerializeField] private Sprite mainMenuBackground;
        [SerializeField] private Sprite classSelectBackground;
        [SerializeField] private Sprite battleBackground;
        [SerializeField] private Sprite shopBackground;
        [SerializeField] private Sprite eventBackground;
        [SerializeField] private Sprite restBackground;
        [SerializeField] private Sprite treasureBackground;
        [SerializeField] private Sprite curseBackground;
        [SerializeField] private Sprite rewardBackground;
        [SerializeField] private Sprite bossBackground;

        [Header("Game Over")]
        [SerializeField] private Sprite gameOverLogoSprite;
        [SerializeField] private List<Sprite> gameOverBackgroundSprites = new();
        [SerializeField] private List<Sprite> gameOverMessageSprites = new();
        [SerializeField] private Sprite gameOverCrackOverlaySprite;
        [SerializeField] private Sprite gamblerHiddenGameOverSprite;
        [SerializeField] private Sprite oracleHiddenGameOverSprite;
        [SerializeField] private Sprite exileHiddenGameOverSprite;
        [SerializeField, Range(0f, 1f)] private float hiddenGameOverChance = 0.20f;

        [Header("Journey Ending")]
        [SerializeField] private Sprite journeyEndingLogoSprite;
        [SerializeField] private List<Sprite> journeyEndingBackgroundSprites = new();
        [SerializeField] private Sprite gamblerJourneyEndingLogoSprite;
        [SerializeField] private Sprite gamblerJourneyEndingBackgroundSprite;
        [SerializeField] private Sprite oracleJourneyEndingLogoSprite;
        [SerializeField] private Sprite oracleJourneyEndingBackgroundSprite;
        [SerializeField] private Sprite exileJourneyEndingLogoSprite;
        [SerializeField] private Sprite exileJourneyEndingBackgroundSprite;

        [Header("Characters")]
        [SerializeField] private Sprite gamblerSelectSprite;
        [SerializeField] private Sprite gamblerSelectHoverSprite;
        [SerializeField] private Sprite oracleSelectSprite;
        [SerializeField] private Sprite oracleSelectHoverSprite;
        [SerializeField] private Sprite exileSelectSprite;
        [SerializeField] private Sprite exileSelectHoverSprite;
        [SerializeField] private Sprite shopkeeperSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite normalBossSprite;
        [SerializeField] private Sprite hardBossSprite;
        [SerializeField] private Sprite debtClearBossSprite;
        [SerializeField] private List<EnemySpriteBinding> enemySprites = new();
        [SerializeField] private List<EnemySpriteBinding> enemyHudFrameSprites = new();

        [Header("UI")]
        [SerializeField] private Font uiFontAsset;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite statusPanelFrameSprite;
        [SerializeField] private Sprite statusSectionFrameSprite;
        [SerializeField] private Sprite statusSectionWideFrameSprite;
        [SerializeField] private Sprite statusSectionTallFrameSprite;
        [SerializeField] private Sprite statusSectionMediumFrameSprite;
        [SerializeField] private Sprite statusHintFrameSprite;
        [SerializeField] private Sprite statusCategoryCardFrameSprite;
        [SerializeField] private Sprite shopCombinationPanelFrameSprite;
        [SerializeField] private Sprite buttonIdleSprite;
        [SerializeField] private Sprite buttonHoverSprite;
        [SerializeField] private Sprite buttonPressedSprite;
        [SerializeField] private Sprite settingsPanelSprite;
        [SerializeField] private Sprite settingsButtonSprite;
        [SerializeField] private Sprite settingsButtonHoverSprite;
        [SerializeField] private Sprite settingsButtonPressedSprite;
        [SerializeField] private Sprite settingsIconSprite;
        [SerializeField] private Sprite mainTitleLogoSprite;
        [SerializeField] private Sprite topBarFrameSprite;
        [SerializeField] private Sprite runStatusPanelFrameSprite;
        [SerializeField] private Sprite logPanelFrameSprite;
        [SerializeField] private Sprite eventMessageFrameSprite;
        [SerializeField] private Sprite doorChoiceFrameSprite;
        [SerializeField] private Sprite enemyStatusFrameSprite;
        [SerializeField] private Sprite playerCombatStatusFrameSprite;
        [SerializeField] private Sprite deckBoxFrameSprite;
        [SerializeField] private Sprite classBackButtonSprite;
        [SerializeField] private Sprite classConfirmButtonSprite;
        [SerializeField] private Sprite classInfoButtonSprite;
        [SerializeField] private Sprite mainMenuButtonSprite;
        [SerializeField] private Sprite mainMenuButtonHoverSprite;
        [SerializeField] private Sprite mainMenuButtonPressedSprite;
        [SerializeField] private Sprite volumeSliderBarSprite;
        [SerializeField] private Sprite mainOptionsPanelSprite;
        [SerializeField] private Sprite mainOptionToggleSprite;
        [SerializeField] private Sprite mainOptionToggleHoverSprite;
        [SerializeField] private Sprite mainOptionTogglePressedSprite;
        [SerializeField] private Sprite mainOptionSliderSprite;
        [SerializeField] private Sprite selectionFrameSprite;
        [SerializeField] private Sprite survivorTitleBadgeSprite;
        [SerializeField] private Sprite debtClearedTitleBadgeSprite;
        [SerializeField] private Sprite healthBarFrameSprite;
        [SerializeField] private Sprite healthBarFillSprite;
        [SerializeField] private Sprite attackCardFrameSprite;
        [SerializeField] private Sprite defenseCardFrameSprite;
        [SerializeField] private Sprite skillCardFrameSprite;
        [SerializeField] private Sprite victoryCrackOverlaySprite;
        [SerializeField] private Sprite victoryImpactSprite;
        [SerializeField] private Sprite victoryShardBurstSprite;
        [SerializeField] private Sprite victoryLogoSprite;
        [SerializeField] private List<Sprite> diceSprites = new();
        [SerializeField] private List<Sprite> gamblerDiceSprites = new();
        [SerializeField] private List<Sprite> oracleDiceSprites = new();
        [SerializeField] private List<Sprite> exileDiceSprites = new();
        [SerializeField] private List<Sprite> gamblerDiceRollSprites = new();
        [SerializeField] private List<Sprite> oracleDiceRollSprites = new();
        [SerializeField] private List<Sprite> exileDiceRollSprites = new();

        [Header("Audio")]
        [SerializeField] private AudioClip mainMenuMusicClip;
        [SerializeField] private AudioClip battleMusicClip;
        [SerializeField] private AudioClip nonCombatMusicClip;
        [SerializeField] private AudioClip bossMusicClip;
        [SerializeField] private AudioClip deathMusicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.46f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.86f;
        [SerializeField] private List<AudioClip> attackImpactClips = new();
        [SerializeField] private AudioClip criticalImpactClip;
        [SerializeField] private List<AudioClip> defenseImpactClips = new();
        [SerializeField] private List<AudioClip> blockedImpactClips = new();
        [SerializeField] private AudioClip plateSettleClip;
        [SerializeField] private AudioClip prophecyDetailClip;
        [SerializeField] private AudioClip traitDetailClip;
        [SerializeField] private AudioClip comboDetailClip;
        [SerializeField] private AudioClip curseDetailClip;
        [SerializeField] private AudioClip bossStartImpactClip;
        [SerializeField] private AudioClip bossVictoryImpactClip;

        [Header("Combat Feedback")]
        [SerializeField] private Sprite combatFeedbackAttackSprite;
        [SerializeField] private Sprite combatFeedbackDefenseSprite;
        [SerializeField] private Sprite combatFeedbackBlockedSprite;
        [SerializeField] private Sprite combatFeedbackCriticalSprite;
        [SerializeField] private Sprite combatFeedbackProphecySprite;
        [SerializeField] private Sprite combatFeedbackTraitSprite;
        [SerializeField] private Sprite combatFeedbackComboSprite;
        [SerializeField] private Sprite combatFeedbackCurseSprite;

        private readonly List<CardData> deck = new();
        private readonly List<CardData> drawPile = new();
        private readonly List<CardData> discardPile = new();
        private readonly List<CardData> hand = new();
        private readonly List<string> combatLog = new();
        private readonly HashSet<string> oncePerCombatUsed = new();
        private readonly Dictionary<string, int> buildUpgradeLevels = new();
        private readonly List<string> equippedRunItemIds = new();
        private readonly HashSet<string> discoveredRunItemIds = new();
        private readonly HashSet<string> runItemTriggersThisCombat = new();
        private List<RunItemDefinition> cachedRunItemDefinitions;
        private int runItemSkillDiscountsRemaining;
        private DoorType currentCombatDoorType = DoorType.Battle;
        private bool runItemBottleHealthBonusApplied;

        private Canvas canvas;
        private RectTransform canvasRoot;
        private RectTransform root;
        private RectTransform safeAreaRoot;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private Image sceneBackgroundImage;
        private RectTransform topBar;
        private RectTransform contentRoot;
        private RectTransform logRoot;
        private RectTransform logTitleRoot;
        private RectTransform logBodyRoot;
        private RectTransform subtitleFrame;
        private Text titleText;
        private Text subtitleText;
        private Text playerStatsText;
        private Text runStatsText;
        private Image diceImage;
        private Text diceText;
        private RectTransform combatDiceHudRoot;
        private Image combatDiceImage;
        private Text combatDiceText;
        private RectTransform diceRollRoot;
        private Image diceRollImage;
        private Text diceRollText;
        private Button primaryButton;
        private Button settingsButton;
        private RectTransform settingsOverlay;
        private RectTransform gameOverOverlay;
        private RectTransform enemyRevealRoot;
        private RectTransform runStatusPanel;
        private RectTransform runStatusMainPanel;
        private RectTransform runStatusDetailPanel;
        private readonly List<UiVisibilitySnapshot> runStatusHiddenUiSnapshots = new();
        private Image cardPreviewImage;
        private RectTransform cardPreviewTarget;
        private Font uiFont;
        private Coroutine enemyRevealRoutine;
        private Coroutine combatVictoryRoutine;
        private RectTransform combatVictoryOverlayRoot;
        private RectTransform combatVictoryEffectRoot;
        private Sprite cachedVictoryShardSprite;
        private AudioSource musicSource;
        private AudioSource impactSfxSource;
        private AudioSource detailSfxSource;
        private AudioClip currentMusicClip;
        private Coroutine sfxMusicDuckRoutine;
        private ImpactSfxCue lastImpactSfxCue = ImpactSfxCue.None;
        private float lastImpactSfxTime = -100f;
        private RectTransform combatFeedbackRoot;
        private Image combatFeedbackEffectImage;
        private Text combatFeedbackText;
        private CanvasGroup combatFeedbackGroup;
        private string activeCombatFeedbackMessage = string.Empty;
        private Sprite activeCombatFeedbackSprite;
        private Color activeCombatFeedbackColor = Color.white;
        private float activeCombatFeedbackStartTime = -100f;
        private int activeCombatFeedbackPriority;
        private readonly List<string> pendingEnemyRevealCombinationImpacts = new();
        private readonly List<CardData> currentShopCards = new();
        private readonly HashSet<int> purchasedShopCardSlots = new();
        private string currentShopRunItemId = string.Empty;
        private bool currentShopRunItemPurchased;
        private bool currentShopOffersReady;
        private string predictedBossRunItemRewardId = string.Empty;

        private CharacterClass selectedClass = CharacterClass.Gambler;
        private RunDifficulty currentDifficulty = RunDifficulty.Easy;
        private JourneyEndingKind currentJourneyEndingKind = JourneyEndingKind.Return;
        private bool endlessModeActive;
        private int nextEndlessBossRoom;
        private int endlessBossesDefeated;
        private int playerMaxHealth;
        private int playerHealth;
        private int playerBlock;
        private int action;
        private int luck;
        private int gold;
        private int debt;
        private int roomsCleared;
        private int combatEncountersCompleted;
        private int consecutiveNonCombatDoors;
        private int storedLuck;
        private int reflectedDamage;
        private int curseReduction;
        private int pendingDamageReduction;
        private bool hasStoredLuck;
        private bool keepLuckNextTurn;
        private int doorInsightLevel;
        private bool retainBlockNextTurn;
        private bool preventDeathThisTurn;
        private bool combatVictorySequenceActive;
        private float diceRollAnimationEndTime;
        private float diceRollAnimationStartTime;
        private int pendingDrawAnimationCount;
        private EnemyState enemy;
        private GamePhase phase;
        private bool oracleBuildTriggeredThisCombat;
        private bool exileBuildTriggeredThisCombat;
        private int combatDrawDiscardCount;
        private bool emptyDeckWarningLogged;
        private int gamblerLoadedDiceRollsRemaining;
        private bool gamblerCardReadingAwakened;
        private int oracleAttackDefenseResponses;
        private bool oraclePrecisePredictionAwakened;
        private int oracleNextCardCostReduction;
        private int exileCurseRemovalsThisCombat;
        private bool exileCurseEaterAwakened;
        private bool exileWoundOathTriggeredThisCombat;
        private int exileNextAttackDamageBonus;
        private int exileNextAttackVulnerableBonus;
        private bool gamblerHardHighLuckAttackUsedThisTurn;
        private bool gamblerHardLowLuckDefenseUsedThisTurn;
        private int gamblerHardGoldGainedThisCombat;
        private bool gamblerHardGoldSpikeTriggeredThisCombat;
        private bool oracleHardLuckHeldThisTurn;
        private bool oracleHardLowHandDrawTriggeredThisCombat;
        private bool oracleHardProphecyPrimed;
        private bool exileHardFatalOathTriggeredThisCombat;
        private readonly HashSet<string> cardsPlayedThisTurn = new();
        private readonly HashSet<string> cardsPlayedThisCombat = new();
        private readonly HashSet<string> combinationTriggersThisTurn = new();
        private readonly HashSet<string> combinationTriggersThisCombat = new();
        private CardData activeCard;
        private int activeCardHandIndex = -1;
        private bool activeCardDamageBonusApplied;
        private bool activeCardBlockBonusApplied;
        private bool activeCardRunItemDamageBonusApplied;
        private bool activeCardRunItemBlockBonusApplied;
        private bool forbiddenCycleActiveThisTurn;
        private int pendingCombinationDamageBonus;
        private string pendingCombinationDamageBonusSourceId = string.Empty;
        private string activeCombinationImpactId = string.Empty;
        private float activeCombinationImpactStartTime = -100f;
        private Text activeCombinationHudText;
        private Text activeCombinationImpactText;

        private enum GamePhase
        {
            MainMenu,
            ClassSelection,
            ClassDetails,
            DoorSelection,
            Combat,
            Reward,
            Shop,
            Event,
            Rest,
            Treasure,
            Curse,
            GameOver
        }

        private enum RunDifficulty
        {
            Easy,
            Normal,
            Hard
        }

        private enum JourneyEndingKind
        {
            Return,
            TrueDebtCleared,
            EndlessReturn
        }

        private enum ClassInfoSection
        {
            Features,
            Traits,
            RecommendedCards
        }

        private enum EnemySpecialEffect
        {
            None,
            GatekeeperSeal,
            DebtAdjudication,
            AbyssUsury,
            BottomlessAudit
        }

        private enum RunItemType
        {
            Relic,
            Blessing,
            Curse
        }

        private void Awake()
        {
            Random.InitState(Environment.TickCount);
            ApplyMobileRuntimeSettings();
            EnsureEventSystem();
            BuildShell();
            EnsureAudioSources();
            ShowMainMenu();
        }

        private void Update()
        {
            ApplySafeAreaIfNeeded();
            UpdateDiceRollAnimation();
            UpdateCardPreviewVisibility();
            UpdateCombinationImpactAnimation();
            UpdateCombatFeedbackAnimation();
        }

        private static void ApplyMobileRuntimeSettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.fullScreen = true;
            Application.targetFrameRate = 60;
#endif
        }

        private void BuildShell()
        {
            uiFont = uiFontAsset != null ? uiFontAsset : Font.CreateDynamicFontFromOSFont("Noto Sans KR", 18);
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            GameObject canvasObject = new("Three Doors Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            sceneBackgroundImage = AddImage(canvasRoot, "배경", Color.white);
            Stretch(sceneBackgroundImage.rectTransform);

            Image vignette = AddImage(canvasRoot, "비네트", new Color(0f, 0f, 0f, 0.24f));
            Stretch(vignette.rectTransform);

            safeAreaRoot = AddPanel(canvasRoot, "모바일 안전영역", new Color(1f, 1f, 1f, 0f));
            root = safeAreaRoot;
            ApplySafeAreaIfNeeded(true);

            topBar = AddPanel(root, "상단 정보", new Color(1f, 1f, 1f, 0f), topBarFrameSprite);
            topBar.GetComponent<Image>().raycastTarget = false;
            SetAnchors(topBar, new Vector2(0.025f, 0.880f), new Vector2(0.975f, 0.988f));

            Image topBarBacking = AddImage(topBar, "상단 정보 배경", new Color(0f, 0f, 0f, 0f));
            topBarBacking.raycastTarget = false;
            SetAnchors(topBarBacking.rectTransform, new Vector2(0.035f, 0.14f), new Vector2(0.965f, 0.86f));

            Sprite titleBoxSprite = classBackButtonSprite != null
                ? classBackButtonSprite
                : classConfirmButtonSprite != null
                    ? classConfirmButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : classInfoButtonSprite != null
                            ? classInfoButtonSprite
                            : buttonIdleSprite;
            RectTransform titleBox = AddPanel(topBar, "상단 제목 박스", Color.white, titleBoxSprite);
            Image titleBoxImage = titleBox.GetComponent<Image>();
            titleBoxImage.type = Image.Type.Simple;
            titleBoxImage.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            titleBoxImage.raycastTarget = false;
            SetAnchors(titleBox, new Vector2(0.035f, 0.165f), new Vector2(0.305f, 0.835f));

            titleText = AddText(titleBox, "제목", "세 개의 운명의 문", 24, TextAnchor.MiddleCenter, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignByGeometry = true;
            titleText.resizeTextForBestFit = false;
            titleText.resizeTextMinSize = 16;
            titleText.resizeTextMaxSize = 24;
            AddTextGlow(titleText, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.4f, -1.6f));
            SetAnchors(titleText.rectTransform, new Vector2(0.145f, 0.235f), new Vector2(0.855f, 0.765f));

            playerStatsText = AddText(topBar, "플레이어 상태", "", 22, TextAnchor.MiddleLeft, new Color(0.94f, 0.88f, 0.78f, 1f));
            SetAnchors(playerStatsText.rectTransform, new Vector2(0.315f, 0.25f), new Vector2(0.535f, 0.75f));

            diceImage = AddImage(topBar, "행운 주사위", Color.white);
            diceImage.preserveAspect = true;
            SetAnchors(diceImage.rectTransform, new Vector2(0.705f, 0.20f), new Vector2(0.738f, 0.80f));

            diceText = AddText(topBar, "행운 텍스트", "", 22, TextAnchor.MiddleLeft, new Color(0.70f, 1.0f, 0.90f, 1f));
            diceText.fontStyle = FontStyle.Bold;
            SetAnchors(diceText.rectTransform, new Vector2(0.746f, 0.25f), new Vector2(0.800f, 0.75f));

            runStatsText = AddText(topBar, "런 상태", "", 19, TextAnchor.MiddleRight, new Color(0.86f, 0.82f, 0.72f, 1f));
            SetAnchors(runStatsText.rectTransform, new Vector2(0.790f, 0.25f), new Vector2(0.915f, 0.75f));

            settingsButton = AddSettingsMenuButton(topBar, "설정 버튼", "설정", 15);
            SetAnchors(settingsButton.GetComponent<RectTransform>(), new Vector2(0.927f, 0.210f), new Vector2(0.987f, 0.790f));
            settingsButton.onClick.AddListener(ToggleSettingsPanel);

            subtitleFrame = AddEventMessagePanel(root, "부제 박스");
            subtitleFrame.gameObject.SetActive(false);

            subtitleText = AddText(root, "부제", "", 22, TextAnchor.MiddleCenter, new Color(0.90f, 0.98f, 0.96f, 1f));
            subtitleText.fontStyle = FontStyle.Bold;
            AddTextGlow(subtitleText, new Color(0f, 0f, 0f, 0.90f), new Color(0.05f, 0.42f, 0.42f, 0.62f), new Vector2(1.9f, -2.2f));
            SetAnchors(subtitleText.rectTransform, new Vector2(0.10f, 0.825f), new Vector2(0.90f, 0.875f));

            contentRoot = AddPanel(root, "주요 화면", new Color(1f, 1f, 1f, 0f));
            SetAnchors(contentRoot, new Vector2(0.04f, 0.11f), new Vector2(0.73f, 0.83f));

            logRoot = AddPanel(root, "기록", new Color(1f, 1f, 1f, 0.88f), logPanelFrameSprite);
            SetAnchors(logRoot, new Vector2(0.755f, 0.11f), new Vector2(0.96f, 0.83f));
            logRoot.gameObject.AddComponent<RectMask2D>();

            Sprite logTitleSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : eventMessageFrameSprite != null
                            ? eventMessageFrameSprite
                            : statusCategoryCardFrameSprite != null
                                ? statusCategoryCardFrameSprite
                                : classInfoButtonSprite;
            logTitleRoot = AddPanel(root, "기록 제목 박스", Color.white, logTitleSprite);
            Image logTitleImage = logTitleRoot.GetComponent<Image>();
            logTitleImage.type = Image.Type.Simple;
            logTitleImage.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            SetAnchors(logTitleRoot, new Vector2(0.765f, 0.827f), new Vector2(0.950f, 0.880f));

            Text logTitle = AddText(logTitleRoot, "기록 제목", "진행 기록", 24, TextAnchor.MiddleCenter, Color.white);
            logTitle.fontStyle = FontStyle.Bold;
            logTitle.alignByGeometry = true;
            logTitle.resizeTextMinSize = 17;
            logTitle.resizeTextMaxSize = 24;
            AddTextGlow(logTitle, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.5f, -1.7f));
            SetAnchors(logTitle.rectTransform, new Vector2(0.145f, 0.245f), new Vector2(0.855f, 0.755f));

            logBodyRoot = AddPanel(logRoot, "기록 내용 박스", new Color(1f, 1f, 1f, 0f));
            SetAnchors(logBodyRoot, new Vector2(0.135f, 0.185f), new Vector2(0.865f, 0.770f));
            logBodyRoot.gameObject.AddComponent<RectMask2D>();

            primaryButton = AddButton(root, "주요 버튼", "턴 종료", 25, Color.white);
            Image primaryButtonImage = primaryButton.GetComponent<Image>();
            primaryButtonImage.sprite = classConfirmButtonSprite != null ? classConfirmButtonSprite : mainMenuButtonSprite != null ? mainMenuButtonSprite : buttonIdleSprite;
            primaryButtonImage.type = GetImageType(primaryButtonImage.sprite);
            primaryButtonImage.color = Color.white;
            SetAnchors(primaryButton.GetComponent<RectTransform>(), new Vector2(0.755f, 0.03f), new Vector2(0.96f, 0.095f));
            primaryButton.onClick.AddListener(EndTurn);

            diceRollRoot = AddPanel(root, "주사위 굴림 연출", new Color(1f, 1f, 1f, 0f));
            SetAnchors(diceRollRoot, new Vector2(0.445f, 0.705f), new Vector2(0.555f, 0.90f));
            diceRollRoot.gameObject.SetActive(false);

            diceRollImage = AddImage(diceRollRoot, "굴림 주사위", Color.white);
            diceRollImage.preserveAspect = true;
            SetAnchors(diceRollImage.rectTransform, new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.88f));

            diceRollText = AddText(diceRollRoot, "굴림 결과", "", 18, TextAnchor.MiddleCenter, new Color(0.72f, 1f, 0.92f, 1f));
            diceRollText.fontStyle = FontStyle.Bold;
            SetAnchors(diceRollText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f));
        }

        private void ApplySafeAreaIfNeeded(bool force = false)
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                Stretch(safeAreaRoot);
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x = Mathf.Clamp01(anchorMin.x / Screen.width);
            anchorMin.y = Mathf.Clamp01(anchorMin.y / Screen.height);
            anchorMax.x = Mathf.Clamp01(anchorMax.x / Screen.width);
            anchorMax.y = Mathf.Clamp01(anchorMax.y / Screen.height);

            // Keep the game playable on desktop builds while respecting notches and gesture bars on mobile.
            if (anchorMax.x - anchorMin.x < 0.70f || anchorMax.y - anchorMin.y < 0.70f)
            {
                Stretch(safeAreaRoot);
                return;
            }

            SetAnchors(safeAreaRoot, anchorMin, anchorMax);
        }

        private void ShowMainMenu()
        {
            HideSettingsPanel();
            PlayMainMenuMusic();
            phase = GamePhase.MainMenu;
            SetBackground(mainMenuBackground != null ? mainMenuBackground : classSelectBackground);
            ClearContent();
            combatLog.Clear();
            titleText.text = "세 개의 운명의 문";
            subtitleText.text = string.Empty;
            SetSubtitleBoxVisible(false);
            primaryButton.gameObject.SetActive(false);
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0f, 0f), new Vector2(1f, 1f));

            if (mainTitleLogoSprite != null)
            {
                Image titleLogo = AddImage(contentRoot, "메인 타이틀 로고", Color.white);
                titleLogo.sprite = mainTitleLogoSprite;
                titleLogo.preserveAspect = true;
                titleLogo.raycastTarget = false;
                SetAnchors(titleLogo.rectTransform, new Vector2(0.20f, 0.695f), new Vector2(0.80f, 0.97f));
            }
            else
            {
                Text title = AddText(contentRoot, "메인 타이틀", "Three Doors Of Fate", 70, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.68f, 1f));
                title.fontStyle = FontStyle.Bold;
                AddTextGlow(title, new Color(0f, 0f, 0f, 0.92f), new Color(0.58f, 0.46f, 0.26f, 0.78f), new Vector2(3.4f, -4.0f));
                SetAnchors(title.rectTransform, new Vector2(0.18f, 0.80f), new Vector2(0.82f, 0.95f));
            }

            AddMainMenuCharacter("도박사 포즈", gamblerSelectSprite, gamblerSelectHoverSprite, new Vector2(0.045f, 0.20f), new Vector2(0.35f, 0.76f), 0.2f);
            AddMainMenuCharacter("점술가 포즈", oracleSelectSprite, oracleSelectHoverSprite, new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.78f), 1.8f);
            AddMainMenuCharacter("추방자 포즈", exileSelectSprite, exileSelectHoverSprite, new Vector2(0.65f, 0.19f), new Vector2(0.955f, 0.76f), 3.1f);
            AddMainMenuEndlessRecordBox();

            bool supportsDesktopWindowControls = SupportsDesktopWindowControls(Application.platform);

            Button startButton = AddMainMenuButton(contentRoot, "게임시작", "게임시작", 30);
            SetAnchors(
                startButton.GetComponent<RectTransform>(),
                supportsDesktopWindowControls ? new Vector2(0.17f, 0.055f) : new Vector2(0.27f, 0.055f),
                supportsDesktopWindowControls ? new Vector2(0.37f, 0.14f) : new Vector2(0.47f, 0.14f));
            startButton.onClick.AddListener(ShowClassSelection);

            Button optionsButton = AddMainMenuButton(contentRoot, "옵션", "옵션", 30);
            SetAnchors(
                optionsButton.GetComponent<RectTransform>(),
                supportsDesktopWindowControls ? new Vector2(0.40f, 0.055f) : new Vector2(0.53f, 0.055f),
                supportsDesktopWindowControls ? new Vector2(0.60f, 0.14f) : new Vector2(0.73f, 0.14f));
            optionsButton.onClick.AddListener(ShowSettingsPanel);

            if (supportsDesktopWindowControls)
            {
                Button quitButton = AddMainMenuButton(contentRoot, "게임종료", "게임종료", 30);
                SetAnchors(
                    quitButton.GetComponent<RectTransform>(),
                    new Vector2(0.63f, 0.055f),
                    new Vector2(0.83f, 0.14f));
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void AddMainMenuCharacter(string name, Sprite firstPose, Sprite secondPose, Vector2 anchorMin, Vector2 anchorMax, float phaseOffset)
        {
            RectTransform poseRoot = AddPanel(contentRoot, name, new Color(1f, 1f, 1f, 0f));
            poseRoot.GetComponent<Image>().raycastTarget = false;
            SetAnchors(poseRoot, anchorMin, anchorMax);

            Image first = AddImage(poseRoot, "기본 포즈", Color.white);
            first.sprite = firstPose;
            first.preserveAspect = true;
            SetAnchors(first.rectTransform, Vector2.zero, Vector2.one);

            if (secondPose == null)
            {
                return;
            }

            Image second = AddImage(poseRoot, "변화 포즈", Color.white);
            second.sprite = secondPose;
            second.preserveAspect = true;
            SetAnchors(second.rectTransform, Vector2.zero, Vector2.one);
            poseRoot.gameObject.AddComponent<PoseCycleAnimator>().Configure(first, second, phaseOffset);
        }

        private void AddMainMenuEndlessRecordBox()
        {
            string recordText = GetMainMenuEndlessRecordText();
            if (string.IsNullOrEmpty(recordText))
            {
                return;
            }

            Sprite recordFrameSprite = statusHintFrameSprite != null
                ? statusHintFrameSprite
                : mainOptionsPanelSprite != null
                    ? mainOptionsPanelSprite
                    : eventMessageFrameSprite != null
                        ? eventMessageFrameSprite
                        : panelSprite;
            RectTransform recordBox = AddPanel(contentRoot, "무한 기록 박스", Color.white, recordFrameSprite);
            Image recordImage = recordBox.GetComponent<Image>();
            recordImage.raycastTarget = false;
            recordImage.color = new Color(1.04f, 1.03f, 0.98f, 0.94f);
            SetAnchors(recordBox, new Vector2(0.770f, 0.770f), new Vector2(0.965f, 0.955f));

            Text title = AddText(recordBox, "무한 기록 제목", "기록", 20, TextAnchor.MiddleCenter, new Color(0.68f, 1f, 0.94f, 1f));
            title.fontStyle = FontStyle.Bold;
            title.resizeTextMinSize = 14;
            title.resizeTextMaxSize = 20;
            AddTextGlow(title, new Color(0f, 0f, 0f, 0.92f), new Color(0.06f, 0.72f, 0.68f, 0.56f), new Vector2(1.3f, -1.5f));
            SetAnchors(title.rectTransform, new Vector2(0.125f, 0.675f), new Vector2(0.875f, 0.900f));

            Text body = AddText(recordBox, "무한 기록 내용", recordText, 15, TextAnchor.UpperCenter, new Color(0.92f, 0.86f, 0.74f, 1f));
            body.fontStyle = FontStyle.Bold;
            body.lineSpacing = 0.95f;
            body.resizeTextMinSize = 10;
            body.resizeTextMaxSize = 15;
            AddTextGlow(body, new Color(0f, 0f, 0f, 0.86f), new Color(0.40f, 0.30f, 0.16f, 0.42f), new Vector2(1.1f, -1.2f));
            SetAnchors(body.rectTransform, new Vector2(0.080f, 0.095f), new Vector2(0.920f, 0.650f));
        }

        private void ToggleSettingsPanel()
        {
            if (settingsOverlay != null)
            {
                HideSettingsPanel();
                return;
            }

            ShowSettingsPanel();
        }

        private void HideSettingsPanel()
        {
            if (settingsOverlay == null)
            {
                return;
            }

            Destroy(settingsOverlay.gameObject);
            settingsOverlay = null;
        }

        private void ShowSettingsPanel()
        {
            HideSettingsPanel();

            Image overlay = AddImage(root, "설정 오버레이", new Color(0f, 0f, 0f, 0.66f));
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);
            settingsOverlay = overlay.rectTransform;
            settingsOverlay.SetAsLastSibling();

            Button overlayButton = overlay.gameObject.AddComponent<Button>();
            overlayButton.targetGraphic = overlay;
            overlayButton.colors = CreateFixedButtonColors(overlay.color);
            overlayButton.onClick.AddListener(HideSettingsPanel);

            Image modal = AddImage(settingsOverlay, "설정창", Color.white);
            modal.sprite = mainOptionsPanelSprite != null ? mainOptionsPanelSprite : settingsPanelSprite != null ? settingsPanelSprite : panelSprite;
            modal.raycastTarget = true;
            SetAnchors(modal.rectTransform, new Vector2(0.22f, 0.15f), new Vector2(0.78f, 0.86f));

            Button modalClickBlocker = modal.gameObject.AddComponent<Button>();
            modalClickBlocker.targetGraphic = modal;
            modalClickBlocker.colors = CreateFixedButtonColors(Color.white);

            Text title = AddText(modal.rectTransform, "설정 제목", "설정", 42, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.72f, 1f));
            title.fontStyle = FontStyle.Bold;
            AddTextGlow(title, new Color(0f, 0f, 0f, 0.88f), new Color(0.60f, 0.48f, 0.28f, 0.70f), new Vector2(2.4f, -2.8f));
            SetAnchors(title.rectTransform, new Vector2(0.22f, 0.80f), new Vector2(0.78f, 0.93f));

            bool supportsDesktopWindowControls = SupportsDesktopWindowControls(Application.platform);
            if (supportsDesktopWindowControls)
            {
                Text displayLabel = AddText(modal.rectTransform, "화면 모드 제목", "화면 모드", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.98f, 0.94f, 1f));
                displayLabel.fontStyle = FontStyle.Bold;
                SetAnchors(displayLabel.rectTransform, new Vector2(0.20f, 0.66f), new Vector2(0.80f, 0.74f));

                Button fullscreenButton = AddOptionToggleButton(modal.rectTransform, "전체화면", "전체화면", 22);
                SetAnchors(fullscreenButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0.54f), new Vector2(0.42f, 0.65f));
                fullscreenButton.onClick.AddListener(SetFullscreenMode);

                Button windowedButton = AddOptionToggleButton(modal.rectTransform, "창모드", "창모드", 22);
                SetAnchors(windowedButton.GetComponent<RectTransform>(), new Vector2(0.58f, 0.54f), new Vector2(0.84f, 0.65f));
                windowedButton.onClick.AddListener(SetWindowedMode);
            }

            Text volumeLabel = AddText(modal.rectTransform, "소리 제목", "소리", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.98f, 0.94f, 1f));
            volumeLabel.fontStyle = FontStyle.Bold;
            SetAnchors(
                volumeLabel.rectTransform,
                supportsDesktopWindowControls ? new Vector2(0.20f, 0.43f) : new Vector2(0.20f, 0.58f),
                supportsDesktopWindowControls ? new Vector2(0.80f, 0.51f) : new Vector2(0.80f, 0.66f));

            Slider volumeSlider = AddVolumeSlider(modal.rectTransform);
            SetAnchors(
                volumeSlider.GetComponent<RectTransform>(),
                supportsDesktopWindowControls ? new Vector2(0.20f, 0.33f) : new Vector2(0.20f, 0.47f),
                supportsDesktopWindowControls ? new Vector2(0.80f, 0.42f) : new Vector2(0.80f, 0.56f));

            AddHardRunSaveLoadControls(modal.rectTransform);

            Button continueButton = AddSettingsMenuButton(modal.rectTransform, "설정 닫기", "닫기", 20);
            SetAnchors(
                continueButton.GetComponent<RectTransform>(),
                supportsDesktopWindowControls ? new Vector2(0.12f, 0.13f) : new Vector2(0.24f, 0.13f),
                supportsDesktopWindowControls ? new Vector2(0.34f, 0.24f) : new Vector2(0.46f, 0.24f));
            continueButton.onClick.AddListener(HideSettingsPanel);

            Button titleButton = AddSettingsMenuButton(modal.rectTransform, "처음으로", "처음으로", 20);
            SetAnchors(
                titleButton.GetComponent<RectTransform>(),
                supportsDesktopWindowControls ? new Vector2(0.39f, 0.13f) : new Vector2(0.54f, 0.13f),
                supportsDesktopWindowControls ? new Vector2(0.61f, 0.24f) : new Vector2(0.76f, 0.24f));
            titleButton.onClick.AddListener(ReturnToTitle);

            if (supportsDesktopWindowControls)
            {
                Button quitButton = AddSettingsMenuButton(modal.rectTransform, "게임 종료", "게임 종료", 20);
                SetAnchors(quitButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0.13f), new Vector2(0.88f, 0.24f));
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void ReturnToTitle()
        {
            AutoSaveRunIfAllowed();
            HideSettingsPanel();
            ShowMainMenu();
        }

        private void SetLogVisible(bool visible)
        {
            if (logRoot != null)
            {
                logRoot.gameObject.SetActive(visible);
            }

            if (logTitleRoot != null)
            {
                logTitleRoot.gameObject.SetActive(visible);
            }
        }

        private static void SetFullscreenMode()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.fullScreen = true;
#else
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
#endif
        }

        private static bool SupportsDesktopWindowControls(RuntimePlatform platform)
        {
            return platform != RuntimePlatform.IPhonePlayer && platform != RuntimePlatform.Android;
        }

        private static void SetWindowedMode()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.fullScreen = true;
#else
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
#endif
        }

        private void QuitGame()
        {
            AutoSaveRunIfAllowed();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
            }

            if (impactSfxSource == null)
            {
                impactSfxSource = gameObject.AddComponent<AudioSource>();
                impactSfxSource.playOnAwake = false;
                impactSfxSource.loop = false;
                impactSfxSource.spatialBlend = 0f;
            }

            if (detailSfxSource == null)
            {
                detailSfxSource = gameObject.AddComponent<AudioSource>();
                detailSfxSource.playOnAwake = false;
                detailSfxSource.loop = false;
                detailSfxSource.spatialBlend = 0f;
            }

            ApplyAudioVolumes();
        }

        private void ApplyAudioVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }

            if (impactSfxSource != null)
            {
                impactSfxSource.volume = sfxVolume;
            }

            if (detailSfxSource != null)
            {
                detailSfxSource.volume = sfxVolume;
            }
        }

        private void PlayMainMenuMusic()
        {
            PlayMusic(mainMenuMusicClip);
        }

        private void PlayBattleMusic(bool bossBattle = false)
        {
            AudioClip clip = bossBattle && bossMusicClip != null ? bossMusicClip : battleMusicClip;
            PlayMusic(clip != null ? clip : mainMenuMusicClip);
        }

        private void PlayNonCombatMusic()
        {
            PlayMusic(nonCombatMusicClip != null ? nonCombatMusicClip : mainMenuMusicClip);
        }

        private void PlayDeathMusic()
        {
            PlayMusic(deathMusicClip != null ? deathMusicClip : mainMenuMusicClip);
        }

        private void PlayMusic(AudioClip clip)
        {
            EnsureAudioSources();
            if (clip == null)
            {
                return;
            }

            if (currentMusicClip == clip && musicSource.isPlaying)
            {
                musicSource.volume = musicVolume;
                return;
            }

            currentMusicClip = clip;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        private void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            ApplyAudioVolumes();
        }

        private void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale = 1f, float pitchJitter = 0f)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.pitch = pitchJitter > 0f ? UnityEngine.Random.Range(1f - pitchJitter, 1f + pitchJitter) : 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private void PlayVariedOneShot(AudioSource source, List<AudioClip> clips, float volumeScale = 1f, float pitchJitter = 0.035f)
        {
            if (source == null || clips == null || clips.Count == 0)
            {
                return;
            }

            PlayOneShot(source, clips[UnityEngine.Random.Range(0, clips.Count)], volumeScale, pitchJitter);
        }

        private void PlayCombatFeedbackSfx(string message)
        {
            ImpactSfxCue cue = ImpactSfxCueResolver.FromFeedbackMessage(message);
            if (cue == ImpactSfxCue.None)
            {
                return;
            }

            if (cue == lastImpactSfxCue && Time.unscaledTime - lastImpactSfxTime < 0.09f)
            {
                return;
            }

            EnsureAudioSources();
            lastImpactSfxCue = cue;
            lastImpactSfxTime = Time.unscaledTime;

            if (ImpactSfxCueResolver.UsesPlateLayer(cue))
            {
                PlayOneShot(detailSfxSource, plateSettleClip, 0.58f, 0.018f);
            }

            DuckMusicForSfx();

            switch (cue)
            {
                case ImpactSfxCue.Attack:
                    PlayVariedOneShot(impactSfxSource, attackImpactClips, 0.92f, 0.045f);
                    break;
                case ImpactSfxCue.Critical:
                    PlayOneShot(impactSfxSource, criticalImpactClip, 0.98f, 0.03f);
                    break;
                case ImpactSfxCue.Defense:
                    PlayVariedOneShot(impactSfxSource, defenseImpactClips, 0.88f, 0.035f);
                    break;
                case ImpactSfxCue.Blocked:
                    PlayVariedOneShot(impactSfxSource, blockedImpactClips, 0.92f, 0.03f);
                    break;
                case ImpactSfxCue.Prophecy:
                    PlayOneShot(impactSfxSource, prophecyDetailClip, 0.78f, 0.025f);
                    break;
                case ImpactSfxCue.Trait:
                    PlayOneShot(impactSfxSource, traitDetailClip, 0.78f, 0.025f);
                    break;
                case ImpactSfxCue.Combo:
                    PlayOneShot(impactSfxSource, comboDetailClip, 0.82f, 0.02f);
                    break;
                case ImpactSfxCue.Curse:
                    PlayOneShot(impactSfxSource, curseDetailClip, 0.84f, 0.018f);
                    break;
            }
        }

        private void DuckMusicForSfx()
        {
            if (musicSource == null || musicSource.clip == null)
            {
                return;
            }

            if (sfxMusicDuckRoutine != null)
            {
                StopCoroutine(sfxMusicDuckRoutine);
            }

            sfxMusicDuckRoutine = StartCoroutine(DuckMusicForSfxRoutine());
        }

        private IEnumerator DuckMusicForSfxRoutine()
        {
            float targetVolume = musicVolume * 0.72f;
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Min(musicSource.volume, targetVolume);
            }

            yield return new WaitForSecondsRealtime(0.18f);

            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }

            sfxMusicDuckRoutine = null;
        }

        private void PlayBossStartSfx()
        {
            EnsureAudioSources();
            DuckMusicForSfx();
            PlayOneShot(impactSfxSource, bossStartImpactClip, 0.90f, 0f);
        }

        private void PlayBossVictorySfx()
        {
            EnsureAudioSources();
            DuckMusicForSfx();
            PlayOneShot(impactSfxSource, bossVictoryImpactClip, 0.92f, 0f);
        }

        private void ShowClassSelection()
        {
            HideSettingsPanel();
            PlayMainMenuMusic();
            EnsureSelectedDifficultyIsUnlocked();
            phase = GamePhase.ClassSelection;
            SetBackground(classSelectBackground);
            ClearContent();
            combatLog.Clear();
            titleText.text = "세 개의 운명의 문";
            subtitleText.text = $"첫 문 앞에 설 운명을 선택하세요 - {GetDifficultyName(currentDifficulty)}";
            primaryButton.gameObject.SetActive(false);

            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetSubtitleBoxVisible(true);
            SetAnchors(contentRoot, new Vector2(0.04f, 0.045f), new Vector2(0.96f, 0.900f));

            CreateDifficultySelector(contentRoot);
            CreateClassChoice(CharacterClass.Gambler, gamblerSelectSprite, gamblerSelectHoverSprite, "도박사", new Vector2(0.02f, 0.03f), new Vector2(0.33f, 0.770f));
            CreateClassChoice(CharacterClass.Oracle, oracleSelectSprite, oracleSelectHoverSprite, "점술가", new Vector2(0.345f, 0.03f), new Vector2(0.655f, 0.770f));
            CreateClassChoice(CharacterClass.Exile, exileSelectSprite, exileSelectHoverSprite, "추방자", new Vector2(0.67f, 0.03f), new Vector2(0.98f, 0.770f));
        }

        private void CreateDifficultySelector(RectTransform parent)
        {
            RectTransform selectorRoot = AddPanel(parent, "난이도 선택", new Color(1f, 1f, 1f, 0f));
            SetAnchors(selectorRoot, new Vector2(0.225f, 0.920f), new Vector2(0.775f, 0.995f));

            CreateDifficultyButton(selectorRoot, RunDifficulty.Easy, new Vector2(0.000f, 0.040f), new Vector2(0.315f, 0.960f));
            CreateDifficultyButton(selectorRoot, RunDifficulty.Normal, new Vector2(0.342f, 0.040f), new Vector2(0.657f, 0.960f));
            CreateDifficultyButton(selectorRoot, RunDifficulty.Hard, new Vector2(0.685f, 0.040f), new Vector2(1.000f, 0.960f));
        }

        private void CreateDifficultyButton(RectTransform parent, RunDifficulty difficulty, Vector2 anchorMin, Vector2 anchorMax)
        {
            bool unlocked = IsDifficultyUnlocked(difficulty);
            bool selected = currentDifficulty == difficulty;
            string label = unlocked
                ? GetDifficultyName(difficulty)
                : $"{GetDifficultyName(difficulty)} 잠김";
            Button button = AddClassDetailButton(parent, $"{GetDifficultyName(difficulty)} 버튼", label, classConfirmButtonSprite, 19);
            SetAnchors(button.GetComponent<RectTransform>(), anchorMin, anchorMax);
            if (selected)
            {
                AddSelectedDifficultyBox(button.GetComponent<RectTransform>());
            }

            button.interactable = unlocked;
            if (!unlocked)
            {
                button.GetComponent<Image>().color = new Color(0.48f, 0.48f, 0.48f, 0.78f);
            }

            if (unlocked)
            {
                button.onClick.AddListener(() =>
                {
                    currentDifficulty = difficulty;
                    ShowClassSelection();
                });
            }
        }

        private void AddSelectedDifficultyBox(RectTransform buttonRoot)
        {
            Sprite frameSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classInfoButtonSprite != null
                    ? classInfoButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : buttonIdleSprite;
            if (frameSprite == null)
            {
                return;
            }

            Image frame = AddImage(buttonRoot, "Selected Difficulty Frame", Color.white);
            frame.sprite = frameSprite;
            frame.type = Image.Type.Simple;
            frame.color = new Color(1.10f, 1.06f, 0.92f, 1f);
            frame.raycastTarget = false;
            SetAnchors(frame.rectTransform, new Vector2(-0.085f, -0.470f), new Vector2(1.085f, 1.470f));
            frame.transform.SetAsLastSibling();

            Text labelText = buttonRoot.GetComponentInChildren<Text>(true);
            if (labelText != null)
            {
                labelText.transform.SetAsLastSibling();
            }
        }

        private void EnsureSelectedDifficultyIsUnlocked()
        {
            if (!IsDifficultyUnlocked(currentDifficulty))
            {
                currentDifficulty = GetHighestUnlockedDifficulty();
            }
        }

        private static bool IsDifficultyUnlocked(RunDifficulty difficulty)
        {
            return (int)difficulty <= PlayerPrefs.GetInt(DifficultyUnlockKey, (int)RunDifficulty.Easy);
        }

        private static RunDifficulty GetHighestUnlockedDifficulty()
        {
            int unlocked = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyUnlockKey, (int)RunDifficulty.Easy), 0, (int)RunDifficulty.Hard);
            return (RunDifficulty)unlocked;
        }

        private void UnlockNextDifficultyFromCurrentRun()
        {
            int unlocked = PlayerPrefs.GetInt(DifficultyUnlockKey, (int)RunDifficulty.Easy);
            int next = Mathf.Clamp((int)currentDifficulty + 1, (int)RunDifficulty.Easy, (int)RunDifficulty.Hard);
            if (next > unlocked)
            {
                PlayerPrefs.SetInt(DifficultyUnlockKey, next);
                PlayerPrefs.Save();
                AddLog($"{GetDifficultyName((RunDifficulty)next)} 난이도가 해금되었습니다.");
            }
        }

        private static string GetDifficultyName(RunDifficulty difficulty)
        {
            return difficulty switch
            {
                RunDifficulty.Normal => "보통",
                RunDifficulty.Hard => "어려움",
                _ => "쉬움"
            };
        }

        private static string GetDifficultyDescription(RunDifficulty difficulty)
        {
            return difficulty switch
            {
                RunDifficulty.Normal => "표준 난이도. 보상과 위험이 균형을 이룹니다.",
                RunDifficulty.Hard => "어려움 난이도. 적이 강하고 빚의 압박이 커집니다.",
                _ => "쉬움 난이도. 첫 10문을 익히고 클리어하기 쉽게 조정됩니다."
            };
        }

        private int GetDifficultyStartingHealthBonus()
        {
            return currentDifficulty switch
            {
                RunDifficulty.Easy => 18,
                RunDifficulty.Hard => -6,
                _ => 6
            };
        }

        private int GetDifficultyStartingGoldBonus()
        {
            return currentDifficulty switch
            {
                RunDifficulty.Easy => 25,
                RunDifficulty.Hard => -8,
                _ => 8
            };
        }

        private void CreateClassChoice(CharacterClass characterClass, Sprite characterSprite, Sprite hoverSprite, string className, Vector2 anchorMin, Vector2 anchorMax)
        {
            Sprite classChoiceSprite = statusSectionTallFrameSprite != null
                ? statusSectionTallFrameSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform panel = AddPanel(contentRoot, className, Color.white, classChoiceSprite);
            SetAnchors(panel, anchorMin, anchorMax);
            HoverFloatAnimator floatAnimator = panel.gameObject.AddComponent<HoverFloatAnimator>();
            floatAnimator.Configure(1f, 0.99f, 0f, 0f, 0.08f);

            Image character = AddImage(panel, "직업 이미지", Color.white);
            character.sprite = characterSprite;
            character.preserveAspect = true;
            character.raycastTarget = false;
            SetAnchors(character.rectTransform, new Vector2(0.075f, 0.125f), new Vector2(0.925f, 0.920f));

            if (hoverSprite != null)
            {
                Image hoverCharacter = AddImage(panel, "직업 이미지 Hover", Color.white);
                hoverCharacter.sprite = hoverSprite;
                hoverCharacter.preserveAspect = true;
                hoverCharacter.raycastTarget = false;
                SetAnchors(hoverCharacter.rectTransform, new Vector2(0.075f, 0.125f), new Vector2(0.925f, 0.920f));
                panel.gameObject.AddComponent<ClassPortraitHoverAnimator>().Configure(character, hoverCharacter);
            }

            AddClassSelectionNameBox(panel, className);
            if (IsTrueEndingUnlocked(characterClass))
            {
                AddClassTitleBadge(
                    panel,
                    className,
                    "debt cleared title",
                    debtClearedTitleBadgeSprite,
                    DebtClearedTitleText,
                    new Vector2(0.015f, 1.105f),
                    new Vector2(0.985f, 1.198f));
            }

            if (IsSurvivorTitleUnlocked(characterClass))
            {
                AddClassTitleBadge(
                    panel,
                    className,
                    "survivor title",
                    survivorTitleBadgeSprite,
                    SurvivorTitleText,
                    new Vector2(0.015f, 1.005f),
                    new Vector2(0.985f, 1.098f));
            }

            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            button.colors = CreateButtonColors();
            button.onClick.AddListener(() => ShowClassDetail(characterClass));
        }

        private void AddClassTitleBadge(
            RectTransform parent,
            string className,
            string badgeName,
            Sprite badgeSprite,
            string fallbackText,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform titleRoot = AddPanel(parent, $"{className} {badgeName}", new Color(1f, 1f, 1f, 0f));
            titleRoot.GetComponent<Image>().raycastTarget = false;
            SetAnchors(titleRoot, anchorMin, anchorMax);

            if (badgeSprite != null)
            {
                Image titleImage = AddImage(titleRoot, $"{className} {badgeName} image", Color.white);
                titleImage.sprite = badgeSprite;
                titleImage.preserveAspect = true;
                titleImage.raycastTarget = false;
                SetAnchors(titleImage.rectTransform, Vector2.zero, Vector2.one);
                return;
            }

            Text titleText = AddText(titleRoot, $"{className} {badgeName} text", fallbackText, 18, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.56f, 1f));
            titleText.fontStyle = FontStyle.Bold;
            titleText.resizeTextMinSize = 10;
            titleText.resizeTextMaxSize = 18;
            AddTextGlow(titleText, new Color(0f, 0f, 0f, 0.92f), new Color(0.05f, 0.76f, 0.70f, 0.48f), new Vector2(1.3f, -1.5f));
            SetAnchors(titleText.rectTransform, Vector2.zero, Vector2.one);
        }

        private RectTransform AddClassSelectionNameBox(RectTransform parent, string className)
        {
            Sprite labelSprite = classBackButtonSprite != null
                ? classBackButtonSprite
                : classConfirmButtonSprite != null
                    ? classConfirmButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : buttonIdleSprite;
            RectTransform labelBox = AddPanel(parent, "직업명 박스", Color.white, labelSprite);
            Image labelImage = labelBox.GetComponent<Image>();
            labelImage.type = Image.Type.Simple;
            labelImage.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            labelImage.raycastTarget = false;
            SetAnchors(labelBox, new Vector2(0.135f, 0.045f), new Vector2(0.865f, 0.165f));

            Text nameText = AddText(labelBox, "직업명", className, 28, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.72f, 1f));
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignByGeometry = true;
            nameText.raycastTarget = false;
            AddTextGlow(nameText, new Color(0.01f, 0.008f, 0.006f, 0.94f), new Color(0.72f, 0.56f, 0.30f, 0.72f), new Vector2(1.5f, -1.7f));
            SetAnchors(nameText.rectTransform, new Vector2(0.150f, 0.245f), new Vector2(0.850f, 0.755f));
            return labelBox;
        }

        private void ShowClassDetail(CharacterClass characterClass)
        {
            ClassProfile profile = GetClassProfile(characterClass);
            RectTransform detailPanel = CreateClassDetailLayout(characterClass, profile, $"{profile.Name} 세계관", profile.Tagline);
            RectTransform detailTextRoot = AddClassDetailStoneTextRoot(detailPanel, "세계관 텍스트 안전영역");
            Sprite classDetailBoxButtonSprite = GetClassDetailBoxButtonSprite();

            Text heading = AddText(detailTextRoot, "직업 제목", $"{profile.Name}  |  {profile.Role}", 28, TextAnchor.MiddleCenter, Color.white);
            heading.fontStyle = FontStyle.Bold;
            heading.resizeTextMinSize = 20;
            AddTextGlow(heading, new Color(0f, 0f, 0f, 0.88f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.4f, -1.7f));
            SetAnchors(heading.rectTransform, new Vector2(0.055f, 0.695f), new Vector2(0.945f, 0.875f));

            Text lore = AddText(detailTextRoot, "세계관", profile.WorldLore, 18, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.74f, 1f));
            lore.fontStyle = FontStyle.Bold;
            lore.lineSpacing = 1.04f;
            lore.resizeTextMinSize = 13;
            lore.resizeTextMaxSize = 18;
            AddTextGlow(lore, new Color(0f, 0f, 0f, 0.78f), new Color(0.34f, 0.25f, 0.14f, 0.40f), new Vector2(1.2f, -1.4f));
            SetAnchors(lore.rectTransform, new Vector2(0.055f, 0.345f), new Vector2(0.945f, 0.660f));

            Button featuresButton = AddClassDetailButton(detailTextRoot, "기능 설명 버튼", "기능", classDetailBoxButtonSprite, 20);
            SetAnchors(featuresButton.GetComponent<RectTransform>(), new Vector2(0.030f, 0.055f), new Vector2(0.315f, 0.250f));
            featuresButton.onClick.AddListener(() => ShowClassInfoDetail(characterClass, ClassInfoSection.Features));

            Button traitsButton = AddClassDetailButton(detailTextRoot, "특성 설명 버튼", "특성", classDetailBoxButtonSprite, 20);
            SetAnchors(traitsButton.GetComponent<RectTransform>(), new Vector2(0.357f, 0.055f), new Vector2(0.643f, 0.250f));
            traitsButton.onClick.AddListener(() => ShowClassInfoDetail(characterClass, ClassInfoSection.Traits));

            Button buildButton = AddClassDetailButton(detailTextRoot, "추천 빌드 설명 버튼", "추천 빌드 카드", classDetailBoxButtonSprite, 17);
            SetAnchors(buildButton.GetComponent<RectTransform>(), new Vector2(0.685f, 0.055f), new Vector2(0.970f, 0.250f));
            buildButton.onClick.AddListener(() => ShowClassInfoDetail(characterClass, ClassInfoSection.RecommendedCards));

            RectTransform actionBar = AddClassDetailActionBar(contentRoot);
            SetAnchors(actionBar, new Vector2(0.110f, 0.000f), new Vector2(0.890f, 0.142f));

            Button backButton = AddClassDetailActionButton(actionBar, "뒤로", "다시 선택", 18, classBackButtonSprite);
            SetAnchors(backButton.GetComponent<RectTransform>(), new Vector2(0.055f, 0.120f), new Vector2(0.350f, 0.880f));
            backButton.onClick.AddListener(ShowClassSelection);

            Button confirmButton = AddClassDetailActionButton(actionBar, "캐릭터 확정", "캐릭터 확정", 18, classConfirmButtonSprite);
            SetAnchors(confirmButton.GetComponent<RectTransform>(), new Vector2(0.650f, 0.120f), new Vector2(0.945f, 0.880f));
            confirmButton.onClick.AddListener(() => StartRun(characterClass));

            primaryButton.gameObject.SetActive(false);
        }

        private void ShowClassInfoDetail(CharacterClass characterClass, ClassInfoSection section)
        {
            ClassProfile profile = GetClassProfile(characterClass);
            string sectionTitle = GetClassInfoSectionTitle(section);
            RectTransform detailPanel = CreateClassDetailLayout(characterClass, profile, $"{profile.Name} {sectionTitle}", sectionTitle);
            RectTransform detailTextRoot = AddClassDetailStoneTextRoot(detailPanel, "상세 설명 텍스트 안전영역");
            Sprite classDetailBoxButtonSprite = GetClassDetailBoxButtonSprite();

            Text heading = AddText(detailTextRoot, "상세 제목", $"{profile.Name}  |  {sectionTitle}", 28, TextAnchor.MiddleCenter, Color.white);
            heading.fontStyle = FontStyle.Bold;
            heading.resizeTextMinSize = 20;
            AddTextGlow(heading, new Color(0f, 0f, 0f, 0.88f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.4f, -1.7f));
            SetAnchors(heading.rectTransform, new Vector2(0.055f, 0.695f), new Vector2(0.945f, 0.875f));

            Text body = AddText(detailTextRoot, "상세 설명", GetClassInfoSectionBody(profile, section), 18, TextAnchor.MiddleCenter, GetClassInfoSectionColor(section));
            body.fontStyle = FontStyle.Bold;
            body.lineSpacing = 1.02f;
            body.resizeTextMinSize = 13;
            body.resizeTextMaxSize = 18;
            AddTextGlow(body, new Color(0f, 0f, 0f, 0.76f), new Color(0.04f, 0.34f, 0.33f, 0.28f), new Vector2(1.0f, -1.2f));
            SetAnchors(body.rectTransform, new Vector2(0.055f, 0.345f), new Vector2(0.945f, 0.660f));

            Button closeButton = AddClassDetailButton(detailTextRoot, "상세 닫기", "닫기", classDetailBoxButtonSprite, 20);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.357f, 0.065f), new Vector2(0.643f, 0.260f));
            closeButton.onClick.AddListener(() => ShowClassDetail(characterClass));

            primaryButton.gameObject.SetActive(false);
        }

        private RectTransform CreateClassDetailLayout(CharacterClass characterClass, ClassProfile profile, string detailPanelName, string taglineText)
        {
            phase = GamePhase.ClassDetails;
            SetBackground(classSelectBackground);
            ClearContent();
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0.045f, 0.080f), new Vector2(0.955f, 0.915f));

            titleText.text = profile.Name;
            subtitleText.text = string.Empty;

            RectTransform taglinePanel = AddEventMessagePanel(contentRoot, "직업 한줄 설명");
            SetAnchors(taglinePanel, new Vector2(0.275f, 0.895f), new Vector2(0.725f, 0.995f));

            Text tagline = AddText(taglinePanel, "한줄 설명", taglineText, 22, TextAnchor.MiddleCenter, new Color(0.78f, 0.96f, 0.90f, 1f));
            tagline.fontStyle = FontStyle.Bold;
            SetAnchors(tagline.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));

            Sprite classArtSprite = statusSectionTallFrameSprite != null
                ? statusSectionTallFrameSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform artPanel = AddPanel(contentRoot, $"{profile.Name} 이미지", Color.white, classArtSprite);
            SetAnchors(artPanel, new Vector2(0.025f, 0.125f), new Vector2(0.365f, 0.885f));
            artPanel.gameObject.AddComponent<HoverFloatAnimator>();

            Image character = AddImage(artPanel, "직업 이미지", Color.white);
            character.sprite = GetClassSprite(characterClass);
            character.preserveAspect = true;
            SetAnchors(character.rectTransform, new Vector2(0.090f, 0.105f), new Vector2(0.910f, 0.895f));

            Sprite classDetailSprite = statusSectionWideFrameSprite != null
                ? statusSectionWideFrameSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform detailPanel = AddPanel(contentRoot, detailPanelName, Color.white, classDetailSprite);
            SetAnchors(detailPanel, new Vector2(0.405f, 0.125f), new Vector2(0.980f, 0.885f));
            detailPanel.gameObject.AddComponent<RectMask2D>();
            return detailPanel;
        }

        private RectTransform AddClassDetailStoneTextRoot(RectTransform detailPanel, string name)
        {
            RectTransform textRoot = AddPanel(detailPanel, name, new Color(1f, 1f, 1f, 0f));
            textRoot.gameObject.AddComponent<RectMask2D>();
            SetAnchors(textRoot, new Vector2(0.135f, 0.235f), new Vector2(0.865f, 0.695f));
            return textRoot;
        }

        private RectTransform AddClassDetailActionBar(RectTransform parent)
        {
            return AddPanel(parent, "하단 선택 버튼 컨테이너", new Color(1f, 1f, 1f, 0f));
        }

        private Button AddClassDetailActionButton(RectTransform parent, string name, string label, int fontSize, Sprite spriteOverride = null)
        {
            Sprite actionButtonSprite = spriteOverride != null ? spriteOverride : GetClassDetailActionButtonSprite();
            RectTransform buttonRoot = AddPanel(parent, name, Color.white, actionButtonSprite);
            Image image = buttonRoot.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            image.raycastTarget = true;

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();

            Text text = AddText(buttonRoot, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(11, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.3f, -1.5f));
            SetAnchors(text.rectTransform, new Vector2(0.135f, 0.240f), new Vector2(0.865f, 0.760f));
            return button;
        }

        private Sprite GetClassDetailActionButtonSprite()
        {
            return classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : classInfoButtonSprite != null
                            ? classInfoButtonSprite
                            : buttonIdleSprite;
        }

        private Sprite GetClassDetailButtonSprite()
        {
            return classBackButtonSprite != null
                ? classBackButtonSprite
                : classInfoButtonSprite != null
                    ? classInfoButtonSprite
                    : classConfirmButtonSprite;
        }

        private Sprite GetClassDetailBoxButtonSprite()
        {
            return classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : classInfoButtonSprite;
        }

        private Sprite GetShopButtonSprite()
        {
            return classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : classInfoButtonSprite != null
                            ? classInfoButtonSprite
                            : buttonIdleSprite;
        }

        private Sprite GetShopInfoBoxSprite()
        {
            return statusHintFrameSprite != null
                ? statusHintFrameSprite
                : classConfirmButtonSprite != null
                    ? classConfirmButtonSprite
                    : eventMessageFrameSprite != null
                        ? eventMessageFrameSprite
                        : panelSprite;
        }

        private Sprite GetRunStatusLabelFrameSprite()
        {
            return classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classInfoButtonSprite != null
                    ? classInfoButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : panelSprite;
        }

        private Sprite GetRunStatusBoxFrameSprite()
        {
            return statusSectionMediumFrameSprite != null
                ? statusSectionMediumFrameSprite
                : statusSectionFrameSprite != null
                    ? statusSectionFrameSprite
                    : statusSectionWideFrameSprite != null
                        ? statusSectionWideFrameSprite
                        : GetRunStatusSlotFrameSprite();
        }

        private Sprite GetRunStatusWideBoxFrameSprite()
        {
            return statusSectionWideFrameSprite != null
                ? statusSectionWideFrameSprite
                : statusSectionFrameSprite != null
                    ? statusSectionFrameSprite
                    : statusSectionMediumFrameSprite != null
                        ? statusSectionMediumFrameSprite
                        : GetRunStatusSlotFrameSprite();
        }

        private Sprite GetRunStatusDetailBoxFrameSprite()
        {
            return statusSectionTallFrameSprite != null
                ? statusSectionTallFrameSprite
                : statusSectionMediumFrameSprite != null
                    ? statusSectionMediumFrameSprite
                    : statusSectionFrameSprite != null
                        ? statusSectionFrameSprite
                        : GetRunStatusSlotFrameSprite();
        }

        private Sprite GetRunStatusSlotFrameSprite()
        {
            return statusSectionMediumFrameSprite != null
                ? statusSectionMediumFrameSprite
                : statusSectionFrameSprite != null
                    ? statusSectionFrameSprite
                    : classConfirmButtonSprite != null
                        ? classConfirmButtonSprite
                        : classInfoButtonSprite != null
                            ? classInfoButtonSprite
                            : statusHintFrameSprite != null
                                ? statusHintFrameSprite
                                : panelSprite;
        }

        private Sprite GetRunStatusButtonFrameSprite()
        {
            return classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classInfoButtonSprite != null
                    ? classInfoButtonSprite
                    : panelSprite;
        }

        private RectTransform AddShopInfoBox(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            Sprite boxSprite = GetShopInfoBoxSprite();
            RectTransform box = AddPanel(parent, name, Color.white, boxSprite);
            Image image = box.GetComponent<Image>();
            image.type = GetImageType(boxSprite);
            image.raycastTarget = false;
            SetAnchors(box, min, max);
            return box;
        }

        private Button AddShopActionButton(RectTransform parent, string name, string label, int fontSize)
        {
            Sprite buttonSprite = GetShopButtonSprite();
            RectTransform buttonRoot = AddPanel(parent, name, Color.white, buttonSprite);
            Image image = buttonRoot.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            image.raycastTarget = true;

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();

            Text text = AddText(buttonRoot, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 5);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.3f, -1.5f));
            SetAnchors(text.rectTransform, new Vector2(0.185f, 0.220f), new Vector2(0.815f, 0.780f));
            return button;
        }

        private RectTransform AddShopLabelBox(RectTransform parent, string name, string label, Vector2 min, Vector2 max, int fontSize)
        {
            Sprite labelSprite = GetShopButtonSprite();
            RectTransform labelRoot = AddPanel(parent, name, Color.white, labelSprite);
            Image image = labelRoot.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            image.raycastTarget = false;
            SetAnchors(labelRoot, min, max);

            Text text = AddText(labelRoot, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(11, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.3f, -1.5f));
            SetAnchors(text.rectTransform, new Vector2(0.185f, 0.220f), new Vector2(0.815f, 0.780f));
            return labelRoot;
        }

        private Text AddShopPanelText(
            RectTransform parent,
            string name,
            string label,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 min,
            Vector2 max,
            bool bold)
        {
            Text text = AddText(parent, name, label, fontSize, alignment, color);
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(9, fontSize - 5);
            text.resizeTextMaxSize = fontSize;
            text.lineSpacing = 0.94f;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.80f), new Color(0.05f, 0.42f, 0.40f, 0.34f), new Vector2(1.1f, -1.3f));
            SetAnchors(text.rectTransform, min, max);
            return text;
        }

        private static string GetClassInfoSectionTitle(ClassInfoSection section)
        {
            return section switch
            {
                ClassInfoSection.Features => "기능",
                ClassInfoSection.Traits => "특성",
                ClassInfoSection.RecommendedCards => "추천 빌드 카드",
                _ => "설명"
            };
        }

        private static string GetClassInfoSectionBody(ClassProfile profile, ClassInfoSection section)
        {
            return section switch
            {
                ClassInfoSection.Features => profile.Features,
                ClassInfoSection.Traits => profile.Traits,
                ClassInfoSection.RecommendedCards => profile.RecommendedCards,
                _ => profile.Tagline
            };
        }

        private static Color GetClassInfoSectionColor(ClassInfoSection section)
        {
            return section switch
            {
                ClassInfoSection.Features => new Color(0.76f, 1f, 0.94f, 1f),
                ClassInfoSection.Traits => new Color(0.82f, 0.96f, 1.00f, 1f),
                ClassInfoSection.RecommendedCards => new Color(0.95f, 0.77f, 0.46f, 1f),
                _ => Color.white
            };
        }

        private void StartRun(CharacterClass characterClass)
        {
            EnsureSelectedDifficultyIsUnlocked();
            selectedClass = characterClass;
            playerMaxHealth = characterClass switch
            {
                CharacterClass.Oracle => 58,
                CharacterClass.Exile => 86,
                _ => 70
            } + GetDifficultyStartingHealthBonus();
            playerHealth = playerMaxHealth;
            playerBlock = 0;
            action = StartingAction;
            luck = characterClass == CharacterClass.Gambler ? 4 : 3;
            gold = (characterClass == CharacterClass.Gambler ? 45 : 35) + GetDifficultyStartingGoldBonus();
            debt = characterClass == CharacterClass.Exile ? 1 : 0;
            roomsCleared = 0;
            combatEncountersCompleted = 0;
            consecutiveNonCombatDoors = 0;
            endlessModeActive = false;
            nextEndlessBossRoom = 0;
            endlessBossesDefeated = 0;
            currentJourneyEndingKind = JourneyEndingKind.Return;
            storedLuck = 0;
            reflectedDamage = 0;
            curseReduction = characterClass == CharacterClass.Exile ? 1 : 0;
            hasStoredLuck = false;
            keepLuckNextTurn = false;
            doorInsightLevel = GetBaseDoorInsightLevel();
            retainBlockNextTurn = false;
            preventDeathThisTurn = false;
            oncePerCombatUsed.Clear();
            buildUpgradeLevels.Clear();
            combatLog.Clear();
            runItemBottleHealthBonusApplied = false;
            predictedBossRunItemRewardId = string.Empty;
            LoadDiscoveredRunItemsForSelectedClass();
            LoadEquippedRunItemsForSelectedClass();
            EnsureEquippedRunItemsAreDiscovered();
            ApplyRunItemRunStartBonuses();

            InitializeStartingDeck(characterClass);

            topBar.gameObject.SetActive(true);
            SetLogVisible(true);
            SetAnchors(contentRoot, new Vector2(0.04f, 0.11f), new Vector2(0.73f, 0.83f));

            titleText.text = "세 개의 운명의 문";
            AddLog($"{GetClassName(selectedClass)}의 {GetDifficultyName(currentDifficulty)} 런을 시작했습니다.");
            AddLog(GetDifficultyDescription(currentDifficulty));
            AddLog(equippedRunItemIds.Count > 0
                ? $"장착 아이템 {equippedRunItemIds.Count}/{GetRunItemSlotLimit()}: {GetEquippedRunItemNames()}"
                : $"장착 아이템 없음. 현재 난이도에서는 최대 {GetRunItemSlotLimit()}개까지 장착할 수 있습니다.");
            ShowDoors();
        }

        private void AddStartingCard(string cardId, int count)
        {
            CardData card = cardPool.FirstOrDefault(candidate => candidate.CardId == cardId);
            if (card == null)
            {
                return;
            }

            for (int i = 0; i < count; i += 1)
            {
                deck.Add(card);
            }
        }

        private void InitializeStartingDeck(CharacterClass characterClass)
        {
            deck.Clear();
            AddRandomStartingCategoryCards(CardCategory.Attack, StartingCategoryCardCount, characterClass);
            AddRandomStartingCategoryCards(CardCategory.Defense, StartingCategoryCardCount, characterClass);
            AddRandomStartingCategoryCards(CardCategory.Skill, StartingCategoryCardCount, characterClass);
            AddRandomStartingCategoryCards(CardCategory.Attack, StartingExtraAttackCardCount, characterClass);
            AddRandomStartingCategoryCards(CardCategory.Defense, StartingExtraDefenseCardCount, characterClass);
            AddRandomStartingCategoryCards(CardCategory.Skill, StartingExtraSkillCardCount, characterClass);

            int classCardCount = Mathf.Max(0, StartingDeckSize - deck.Count);
            AddRandomStartingClassCards(characterClass, classCardCount);

            if (deck.Count < StartingDeckSize)
            {
                AddRandomStartingFallbackCards(StartingDeckSize - deck.Count, characterClass);
            }

            Shuffle(deck);
        }

        private void AddRandomStartingCategoryCards(CardCategory category, int count, CharacterClass characterClass)
        {
            List<CardData> candidates = GetStartingCardCandidates(characterClass)
                .Where(card => card.Category == category && card.CharacterClass == CharacterClass.Any)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = GetStartingCardCandidates(characterClass)
                    .Where(card => card.Category == category)
                    .ToList();
            }

            AddRandomCardsWithReplacement(candidates, count);
        }

        private void AddRandomStartingClassCards(CharacterClass characterClass, int count)
        {
            if (characterClass == CharacterClass.Any || count <= 0)
            {
                return;
            }

            List<CardData> candidates = GetStartingCardCandidates(characterClass)
                .Where(card => card.CharacterClass == characterClass)
                .ToList();
            AddRandomCardsWithReplacement(candidates, count);
        }

        private void AddRandomStartingFallbackCards(int count, CharacterClass characterClass)
        {
            AddRandomCardsWithReplacement(GetStartingCardCandidates(characterClass), count);
        }

        private List<CardData> GetStartingCardCandidates(CharacterClass characterClass)
        {
            return cardPool
                .Where(card => card != null
                    && card.Category != CardCategory.Curse
                    && card.Rarity != CardRarity.Rare
                    && card.Rarity != CardRarity.Curse
                    && card.Source != CardSource.HardReward
                    && (card.CharacterClass == CharacterClass.Any || card.CharacterClass == characterClass))
                .ToList();
        }

        private void AddRandomCardsWithReplacement(IReadOnlyList<CardData> candidates, int count)
        {
            for (int i = 0; i < count && candidates.Count > 0; i += 1)
            {
                deck.Add(candidates[Random.Range(0, candidates.Count)]);
            }
        }

        private void ShowDoors()
        {
            PlayMainMenuMusic();
            phase = GamePhase.DoorSelection;
            SetBackground(classSelectBackground);
            ClearContent();
            primaryButton.gameObject.SetActive(true);
            SetPrimaryButtonDefaultPlacement();
            ApplyPrimaryButtonFrame();
            SetButtonLabel(primaryButton, "상태확인");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(ToggleRunStatusPanel);
            bool bossDoorReady = IsBossDoorReady();
            SetLogVisible(!bossDoorReady);
            if (bossDoorReady)
            {
                SetAnchors(contentRoot, new Vector2(0.000f, 0.095f), new Vector2(1.000f, 0.850f));
            }
            else
            {
                SetDefaultContentRootPlacement();
            }

            subtitleText.text = bossDoorReady
                ? endlessModeActive ? "심연의 고리대금업자가 다시 깨어났습니다" : "마지막 문이 깨어났습니다"
                : ShouldForceCombatDoorOptions()
                    ? "동굴이 전투를 요구합니다"
                    : endlessModeActive ? "무한 기록을 향해 더 깊은 문을 선택하세요" : "세 개의 문 중 하나를 선택하세요";
            SetSubtitleBoxVisible(true);

            List<DoorOption> options = bossDoorReady
                ? new List<DoorOption> { CreateBossDoorOption() }
                : GenerateDoorOptions();

            if (bossDoorReady)
            {
                RenderBossDoorOption(options[0]);
                AddLog(endlessModeActive ? "심연의 고리대금업자가 기록을 시험합니다." : "마지막 문이 중앙 홀에서 깨어났습니다.");
                AutoSaveRunIfAllowed();
                RefreshTopBar();
                RefreshLog();
                return;
            }

            for (int i = 0; i < options.Count; i += 1)
            {
                DoorOption option = options[i];
                RectTransform card = AddPanel(contentRoot, $"문 {i + 1}", new Color(1f, 1f, 1f, 0f));
                float width = options.Count == 1 ? 0.42f : 0.292f;
                float start = options.Count == 1 ? 0.29f : 0.028f + i * 0.322f;
                SetAnchors(card, new Vector2(start, 0.060f), new Vector2(start + width, 0.820f));
                HoverFloatAnimator hoverAnimator = card.gameObject.AddComponent<HoverFloatAnimator>();
                hoverAnimator.Configure(1f, 0.99f, 0f, 0f, 0.08f);
                Image hitArea = card.GetComponent<Image>();
                hitArea.color = new Color(1f, 1f, 1f, 0f);
                hitArea.raycastTarget = true;
                Button cardButton = card.gameObject.AddComponent<Button>();
                cardButton.targetGraphic = hitArea;
                cardButton.transition = Selectable.Transition.None;
                cardButton.onClick.AddListener(() => ResolveDoor(option));

                RectTransform artViewport = AddPanel(card, "문 이미지 마스크 영역", new Color(1f, 1f, 1f, 0f));
                artViewport.GetComponent<Image>().raycastTarget = false;
                artViewport.gameObject.AddComponent<RectMask2D>();
                SetAnchors(artViewport, new Vector2(0.105f, 0.285f), new Vector2(0.895f, 0.860f));

                Image art = AddImage(artViewport, "문 이미지", Color.white);
                art.sprite = GetDoorSprite(option.Type);
                art.preserveAspect = false;
                art.raycastTarget = false;
                Stretch(art.rectTransform);

                Sprite hoverSprite = GetDoorHoverSprite(option.Type);
                if (hoverSprite != null)
                {
                    Image hoverArt = AddImage(artViewport, "문 열린 이미지", Color.white);
                    hoverArt.sprite = hoverSprite;
                    hoverArt.preserveAspect = false;
                    hoverArt.raycastTarget = false;
                    Stretch(hoverArt.rectTransform);
                    HoverImageSwapAnimator imageSwapAnimator = card.gameObject.AddComponent<HoverImageSwapAnimator>();
                    imageSwapAnimator.Configure(art, hoverArt, 13f);
                }

                Image frameOverlay = AddImage(card, "문 프레임 오버레이", Color.white);
                frameOverlay.sprite = doorChoiceFrameSprite;
                frameOverlay.type = GetImageType(doorChoiceFrameSprite);
                frameOverlay.raycastTarget = false;
                Stretch(frameOverlay.rectTransform);

                AddDoorChoiceLabelBox(
                    card,
                    "문 이름 박스",
                    option.Name,
                    new Vector2(0.085f, 1.016f),
                    new Vector2(0.915f, 1.150f),
                    17,
                    Color.white);

                Text hintText = AddText(card, "힌트", option.Hint, 12, TextAnchor.MiddleCenter, new Color(0.76f, 1.0f, 0.93f, 1f));
                hintText.resizeTextForBestFit = false;
                hintText.resizeTextMinSize = 8;
                hintText.resizeTextMaxSize = 12;
                hintText.lineSpacing = 0.90f;
                AddTextGlow(hintText, new Color(0f, 0f, 0f, 0.82f), new Color(0.04f, 0.38f, 0.35f, 0.34f), new Vector2(1.0f, -1.2f));
                SetAnchors(hintText.rectTransform, new Vector2(0.145f, 0.130f), new Vector2(0.855f, 0.265f));

                Button chooseButton = AddDoorChoiceButton(
                    card,
                    "선택",
                    option.Risk,
                    new Vector2(0.145f, -0.145f),
                    new Vector2(0.855f, -0.015f),
                    16);
                chooseButton.onClick.AddListener(() => ResolveDoor(option));
            }

            AddLog("동굴이 세 개의 문을 내밀었습니다.");
            AutoSaveRunIfAllowed();
            RefreshTopBar();
            RefreshLog();
        }

        private RectTransform AddDoorChoiceLabelBox(
            RectTransform parent,
            string name,
            string label,
            Vector2 min,
            Vector2 max,
            int fontSize,
            Color textColor)
        {
            Sprite labelSprite = GetDoorChoiceLabelSprite();
            RectTransform labelBox = AddPanel(parent, name, Color.white, labelSprite);
            Image image = labelBox.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            image.raycastTarget = false;
            SetAnchors(labelBox, min, max);

            Text text = AddText(labelBox, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, textColor);
            text.fontStyle = FontStyle.Bold;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.3f, -1.5f));
            SetAnchors(text.rectTransform, new Vector2(0.150f, 0.245f), new Vector2(0.850f, 0.755f));
            return labelBox;
        }

        private Button AddDoorChoiceButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 min,
            Vector2 max,
            int fontSize)
        {
            RectTransform buttonRoot = AddDoorChoiceLabelBox(
                parent,
                name,
                label,
                min,
                max,
                fontSize,
                new Color(1f, 0.93f, 0.78f, 1f));
            Image image = buttonRoot.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            return button;
        }

        private Sprite GetDoorChoiceLabelSprite()
        {
            return classBackButtonSprite != null
                ? classBackButtonSprite
                : classConfirmButtonSprite != null
                    ? classConfirmButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : classInfoButtonSprite != null
                            ? classInfoButtonSprite
                            : settingsButtonSprite != null
                                ? settingsButtonSprite
                                : buttonIdleSprite;
        }

        private void RenderBossDoorOption(DoorOption option)
        {
            RectTransform bossRoot = AddPanel(contentRoot, "보스 문 중앙 화면", new Color(1f, 1f, 1f, 0f));
            Stretch(bossRoot);

            Image focusShade = AddImage(bossRoot, "보스 문 집중 암막", new Color(0f, 0f, 0f, 0.26f));
            SetAnchors(focusShade.rectTransform, new Vector2(0.210f, 0.005f), new Vector2(0.790f, 0.985f));

            RectTransform bossDoor = AddPanel(bossRoot, "보스 문 클릭 영역", new Color(1f, 1f, 1f, 0f));
            SetAnchors(bossDoor, new Vector2(0.245f, -0.020f), new Vector2(0.755f, 0.995f));
            bossDoor.SetAsLastSibling();
            bossDoor.gameObject.AddComponent<RectMask2D>();
            Image hitImage = bossDoor.GetComponent<Image>();
            hitImage.raycastTarget = true;

            HoverFloatAnimator hoverAnimator = bossDoor.gameObject.AddComponent<HoverFloatAnimator>();
            hoverAnimator.Configure(1.018f, 0.985f, 0f, 4f, 0.08f);

            Button doorButton = bossDoor.gameObject.AddComponent<Button>();
            doorButton.targetGraphic = hitImage;
            doorButton.transition = Selectable.Transition.None;
            doorButton.onClick.AddListener(() => ResolveDoor(option));

            Image doorMatte = AddImage(bossDoor, "보스 문 투명 매트", new Color(0f, 0f, 0f, 0f));
            doorMatte.raycastTarget = false;
            Stretch(doorMatte.rectTransform);

            Image closedDoor = AddImage(bossDoor, "보스 문 닫힘", Color.white);
            closedDoor.sprite = GetDoorSprite(option.Type);
            closedDoor.preserveAspect = true;
            closedDoor.raycastTarget = false;
            SetAnchors(closedDoor.rectTransform, new Vector2(-0.085f, -0.080f), new Vector2(1.085f, 1.080f));

            Sprite hoverSprite = GetDoorHoverSprite(option.Type);
            if (hoverSprite != null)
            {
                Image openDoor = AddImage(bossDoor, "보스 문 열림", Color.white);
                openDoor.sprite = hoverSprite;
                openDoor.preserveAspect = true;
                openDoor.raycastTarget = false;
                SetAnchors(openDoor.rectTransform, new Vector2(-0.085f, -0.080f), new Vector2(1.085f, 1.080f));
                HoverImageSwapAnimator imageSwapAnimator = bossDoor.gameObject.AddComponent<HoverImageSwapAnimator>();
                imageSwapAnimator.Configure(closedDoor, openDoor, 9f, 0.18f);
            }

            AddDoorChoiceLabelBox(
                bossRoot,
                "보스 문 이름 박스",
                option.Name,
                new Vector2(0.340f, 0.895f),
                new Vector2(0.660f, 0.990f),
                24,
                Color.white);

            Text hintText = AddText(bossRoot, "보스 문 예언", option.Hint, 18, TextAnchor.MiddleCenter, new Color(0.78f, 1f, 0.94f, 1f));
            hintText.resizeTextForBestFit = false;
            hintText.resizeTextMinSize = 12;
            hintText.resizeTextMaxSize = 18;
            hintText.lineSpacing = 0.92f;
            AddTextGlow(hintText, new Color(0f, 0f, 0f, 0.86f), new Color(0.04f, 0.42f, 0.39f, 0.38f), new Vector2(1.3f, -1.5f));
            SetAnchors(hintText.rectTransform, new Vector2(0.260f, 0.095f), new Vector2(0.740f, 0.190f));

            Button chooseButton = AddDoorChoiceButton(
                bossRoot,
                "보스 문 선택",
                option.Risk,
                new Vector2(0.380f, 0.005f),
                new Vector2(0.620f, 0.085f),
                17);
            chooseButton.onClick.AddListener(() => ResolveDoor(option));
        }

        private void ToggleRunStatusPanel()
        {
            if (runStatusPanel != null)
            {
                HideRunStatusPanel();
                return;
            }

            ShowRunStatusPanel();
        }

        private void HideRunStatusPanel()
        {
            if (runStatusPanel != null)
            {
                Destroy(runStatusPanel.gameObject);
                runStatusPanel = null;
                runStatusMainPanel = null;
                runStatusDetailPanel = null;
            }

            RestoreUiBehindRunStatusPanel();
        }

        private void ShowRunStatusPanel()
        {
            HideRunStatusPanel();
            HideUiBehindRunStatusPanel();

            Sprite modalSprite = statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite;

            runStatusPanel = AddPanel(root, "런 상태 확인 루트", new Color(1f, 1f, 1f, 0f));
            SetAnchors(runStatusPanel, Vector2.zero, Vector2.one);
            runStatusPanel.SetAsLastSibling();

            Image blocker = AddImage(runStatusPanel, "상태 확인 배경 차단", new Color(0.003f, 0.004f, 0.006f, 0.88f));
            blocker.raycastTarget = true;
            Stretch(blocker.rectTransform);

            BuildRunStatusMainPanel(modalSprite);
        }

        private void BuildRunStatusMainPanel(Sprite modalSprite)
        {
            runStatusMainPanel = AddPanel(runStatusPanel, "상태 확인 메인 페이지", new Color(1f, 1f, 1f, 0f));
            Stretch(runStatusMainPanel);

            AddRunStatusTextButton(
                runStatusMainPanel,
                "상태창 닫기",
                "닫기",
                new Vector2(0.835f, 0.905f),
                new Vector2(0.955f, 0.985f),
                HideRunStatusPanel,
                18);

            AddRunStatusLabelBox(
                runStatusMainPanel,
                "상태 확인 헤더",
                "상태 확인",
                new Vector2(0.355f, 0.895f),
                new Vector2(0.645f, 0.995f),
                30);

            RectTransform statusWindow = AddPanel(runStatusMainPanel, "상태 확인 창", Color.white, modalSprite);
            SetAnchors(statusWindow, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.885f));

            AddRunItemEquipmentColumn(statusWindow);

            AddRunStatusCard(
                statusWindow,
                "카드 시너지 조합법",
                new Vector2(0.245f, 0.515f),
                new Vector2(0.565f, 0.790f),
                () => ShowRunStatusDetail("카드 시너지 조합법", BuildCombinationStatusText()));
            AddRunStatusCard(
                statusWindow,
                "보유카드",
                new Vector2(0.610f, 0.515f),
                new Vector2(0.935f, 0.790f),
                () => ShowRunStatusDetail($"보유카드 {deck.Count}/{GetMaxDeckSize()}장", $"{BuildDeckOverviewText()}\n\n{BuildDeckListText()}"));
            AddRunStatusCard(
                statusWindow,
                "전투 중 각성 요소",
                new Vector2(0.245f, 0.205f),
                new Vector2(0.565f, 0.480f),
                () => ShowRunStatusDetail("전투 중 각성 요소", BuildCombatAwakeningText()));
            AddRunStatusCard(
                statusWindow,
                "캐릭터 특성",
                new Vector2(0.610f, 0.205f),
                new Vector2(0.935f, 0.480f),
                () => ShowRunStatusDetail("캐릭터 특성", BuildCharacterTraitText()));
        }

        private void RefreshRunStatusMainPanel()
        {
            if (runStatusPanel == null)
            {
                return;
            }

            bool wasActive = runStatusMainPanel == null || runStatusMainPanel.gameObject.activeSelf;
            if (runStatusMainPanel != null)
            {
                Destroy(runStatusMainPanel.gameObject);
            }

            Sprite modalSprite = statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite;
            BuildRunStatusMainPanel(modalSprite);
            runStatusMainPanel.gameObject.SetActive(wasActive);
            if (runStatusDetailPanel != null)
            {
                runStatusDetailPanel.SetAsLastSibling();
            }
        }

        private void HideUiBehindRunStatusPanel()
        {
            runStatusHiddenUiSnapshots.Clear();
            HideUiForRunStatus(contentRoot);
            HideUiForRunStatus(topBar);
            HideUiForRunStatus(logRoot);
            HideUiForRunStatus(logTitleRoot);
            HideUiForRunStatus(subtitleFrame);
            HideUiForRunStatus(subtitleText);
            HideUiForRunStatus(primaryButton);
            HideUiForRunStatus(diceRollRoot);
        }

        private void HideUiForRunStatus(Component target)
        {
            if (target != null)
            {
                HideUiForRunStatus(target.gameObject);
            }
        }

        private void HideUiForRunStatus(GameObject target)
        {
            if (target == null || target == runStatusPanel?.gameObject)
            {
                return;
            }

            runStatusHiddenUiSnapshots.Add(new UiVisibilitySnapshot(target, target.activeSelf));
            target.SetActive(false);
        }

        private void RestoreUiBehindRunStatusPanel()
        {
            for (int i = runStatusHiddenUiSnapshots.Count - 1; i >= 0; i -= 1)
            {
                UiVisibilitySnapshot snapshot = runStatusHiddenUiSnapshots[i];
                if (snapshot.Target != null)
                {
                    snapshot.Target.SetActive(snapshot.WasActive);
                }
            }

            runStatusHiddenUiSnapshots.Clear();
        }

        private void AddStatusSectionTitle(RectTransform parent, string label)
        {
            Text title = AddText(parent, $"{label} 제목", label, 20, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            AddTextGlow(title, new Color(0f, 0f, 0f, 0.84f), new Color(0.10f, 0.72f, 0.68f, 0.42f), new Vector2(1.4f, -1.8f));
            SetAnchors(title.rectTransform, new Vector2(0.140f, 0.750f), new Vector2(0.860f, 0.880f));
        }

        private RectTransform AddRunStatusLabelBox(RectTransform parent, string name, string label, Vector2 min, Vector2 max, int fontSize)
        {
            Sprite labelSprite = GetRunStatusLabelFrameSprite();
            RectTransform labelBox = AddPanel(parent, name, Color.white, labelSprite);
            labelBox.GetComponent<Image>().type = GetImageType(labelSprite);
            SetAnchors(labelBox, min, max);

            Text text = AddText(labelBox, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.76f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.36f), new Vector2(1.0f, -1.1f));
            SetAnchors(text.rectTransform, new Vector2(0.185f, 0.260f), new Vector2(0.815f, 0.740f));
            return labelBox;
        }

        private Button AddRunStatusTextButton(RectTransform parent, string name, string label, Vector2 min, Vector2 max, UnityAction onClick, int fontSize)
        {
            RectTransform buttonRoot = AddRunStatusLabelBox(parent, name, label, min, max, fontSize);
            Image image = buttonRoot.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            button.onClick.AddListener(onClick);
            return button;
        }

        private Button AddRunStatusCard(RectTransform parent, string title, Vector2 min, Vector2 max, UnityAction onClick)
        {
            Sprite cardSprite = GetRunStatusWideBoxFrameSprite();
            RectTransform card = AddPanel(parent, title, Color.white, cardSprite);
            SetAnchors(card, min, max);

            Image image = card.GetComponent<Image>();
            image.raycastTarget = true;
            image.type = GetImageType(cardSprite);
            if (image.sprite == null)
            {
                image.color = new Color(0.035f, 0.045f, 0.045f, 0.94f);
            }

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            button.onClick.AddListener(onClick);

            Text titleText = AddText(card, $"{title} 제목", title, 26, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.76f, 1f));
            titleText.fontStyle = FontStyle.Bold;
            titleText.resizeTextForBestFit = false;
            titleText.resizeTextMinSize = 17;
            titleText.resizeTextMaxSize = 26;
            AddTextGlow(titleText, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.34f), new Vector2(1.0f, -1.1f));
            SetAnchors(titleText.rectTransform, new Vector2(0.180f, 0.275f), new Vector2(0.820f, 0.725f));

            return button;
        }

        private void AddRunItemEquipmentColumn(RectTransform parent)
        {
            AddRunStatusLabelBox(
                parent,
                "장착 아이템 헤더",
                $"유물 / 축복 / 저주 {equippedRunItemIds.Count}/{GetRunItemSlotLimit()}",
                new Vector2(0.050f, 0.725f),
                new Vector2(0.235f, 0.800f),
                15);

            RunItemType[] slotTypes =
            {
                RunItemType.Relic,
                RunItemType.Blessing,
                RunItemType.Curse
            };
            for (int i = 0; i < MaxEquippedRunItems; i += 1)
            {
                RunItemType type = slotTypes[i];
                RunItemDefinition item = GetEquippedRunItemByType(type);
                float top = 0.690f - i * 0.160f;
                AddRunItemSlotButton(
                    parent,
                    i,
                    type,
                    item,
                    IsRunItemSlotUnlocked(i),
                    new Vector2(0.055f, top - 0.140f),
                    new Vector2(0.235f, top));
            }
        }

        private Button AddRunItemSlotButton(RectTransform parent, int index, RunItemType type, RunItemDefinition item, bool unlocked, Vector2 min, Vector2 max)
        {
            Sprite slotSprite = GetRunStatusSlotFrameSprite();
            RectTransform slot = AddPanel(parent, $"장착 아이템 슬롯 {index + 1}", Color.white, slotSprite);
            SetAnchors(slot, min, max);

            Image image = slot.GetComponent<Image>();
            image.raycastTarget = true;
            image.type = GetImageType(slotSprite);
            if (image.sprite == null)
            {
                image.color = new Color(0.035f, 0.045f, 0.045f, 0.94f);
            }

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            button.interactable = unlocked;
            if (unlocked)
            {
                button.onClick.AddListener(() => ShowRunItemCollectionDetail(type));
            }
            else
            {
                image.color = new Color(0.45f, 0.45f, 0.45f, 0.76f);
            }

            Sprite iconSprite = GetRunItemIcon(item) ?? GetRunItemSilhouetteIcon(type);
            if (iconSprite != null)
            {
                Color iconColor = unlocked
                    ? item != null ? Color.white : new Color(0.05f, 0.07f, 0.07f, 0.74f)
                    : new Color(0.08f, 0.09f, 0.09f, 0.52f);
                Image icon = AddImage(slot, $"장착 아이템 {index + 1} 아이콘", iconColor);
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(icon.rectTransform, new Vector2(0.080f, 0.220f), new Vector2(0.395f, 0.790f));
            }
            else
            {
                Image emptyIcon = AddImage(slot, $"장착 아이템 {index + 1} 빈칸", new Color(0.04f, 0.06f, 0.06f, 0.68f));
                emptyIcon.raycastTarget = false;
                SetAnchors(emptyIcon.rectTransform, new Vector2(0.100f, 0.260f), new Vector2(0.375f, 0.760f));
            }

            Text typeText = AddText(slot, $"장착 아이템 {index + 1} 종류", GetRunItemTypeName(type), 11, TextAnchor.MiddleLeft, unlocked ? new Color(0.72f, 1f, 0.94f, 1f) : new Color(0.48f, 0.58f, 0.56f, 0.92f));
            typeText.fontStyle = FontStyle.Bold;
            typeText.resizeTextForBestFit = false;
            typeText.resizeTextMinSize = 8;
            typeText.resizeTextMaxSize = 11;
            SetAnchors(typeText.rectTransform, new Vector2(0.455f, 0.595f), new Vector2(0.825f, 0.865f));

            string label = unlocked ? item != null ? item.Name : $"선택 {index + 1}" : GetRunItemSlotUnlockLabel(index);
            Text labelText = AddText(slot, $"장착 아이템 {index + 1} 이름", label, 13, TextAnchor.MiddleLeft, unlocked ? new Color(1f, 0.92f, 0.76f, 1f) : new Color(0.62f, 0.60f, 0.54f, 0.94f));
            labelText.fontStyle = FontStyle.Bold;
            labelText.resizeTextForBestFit = false;
            labelText.resizeTextMinSize = 9;
            labelText.resizeTextMaxSize = 13;
            AddTextGlow(labelText, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.0f, -1.2f));
            SetAnchors(labelText.rectTransform, new Vector2(0.455f, 0.155f), new Vector2(0.825f, 0.545f));

            return button;
        }

        private void ShowRunItemCollectionDetail(RunItemType type, string selectedItemId = null)
        {
            HideRunStatusDetail();
            if (runStatusPanel == null)
            {
                return;
            }

            if (runStatusMainPanel != null)
            {
                runStatusMainPanel.gameObject.SetActive(false);
            }

            string title = GetRunItemCollectionTitle(type);
            runStatusDetailPanel = AddPanel(runStatusPanel, $"{title} 상세 페이지", new Color(1f, 1f, 1f, 0f));
            Stretch(runStatusDetailPanel);
            runStatusDetailPanel.SetAsLastSibling();

            Sprite detailSprite = statusPanelFrameSprite != null
                ? statusPanelFrameSprite
                : eventMessageFrameSprite != null
                    ? eventMessageFrameSprite
                    : panelSprite;
            RectTransform detailWindow = AddPanel(runStatusDetailPanel, $"{title} 상세 창", Color.white, detailSprite);
            detailWindow.gameObject.AddComponent<RectMask2D>();
            SetAnchors(detailWindow, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.885f));

            AddRunStatusLabelBox(
                runStatusDetailPanel,
                "아이템 목록 제목 박스",
                title,
                new Vector2(0.295f, 0.895f),
                new Vector2(0.705f, 0.995f),
                24);

            AddRunStatusTextButton(
                runStatusDetailPanel,
                "아이템 목록 닫기",
                "닫기",
                new Vector2(0.835f, 0.905f),
                new Vector2(0.955f, 0.985f),
                HideRunStatusDetail,
                18);

            List<RunItemDefinition> items = GetRunItemDefinitions()
                .Where(item => item.Type == type)
                .OrderBy(item => item.IconName, StringComparer.Ordinal)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList();
            RunItemDefinition selectedItem = ResolveSelectedRunItemForCollection(type, selectedItemId, items);

            RectTransform gridRoot = AddPanel(detailWindow, $"{title} 그리드", new Color(1f, 1f, 1f, 0f));
            SetAnchors(gridRoot, new Vector2(0.115f, 0.430f), new Vector2(0.885f, 0.775f));
            AddRunItemCollectionGrid(gridRoot, type, items, selectedItem?.Id);

            AddRunItemCollectionDescription(detailWindow, type, selectedItem);
        }

        private void AddRunItemCollectionGrid(RectTransform parent, RunItemType type, IReadOnlyList<RunItemDefinition> items, string selectedItemId)
        {
            const int columns = 5;
            const int rows = 2;
            for (int i = 0; i < items.Count; i += 1)
            {
                int row = i / columns;
                int column = i % columns;
                if (row >= rows)
                {
                    break;
                }

                float cellWidth = 1f / columns;
                float cellHeight = 1f / rows;
                float left = column * cellWidth + 0.030f;
                float right = (column + 1) * cellWidth - 0.030f;
                float top = 1f - row * cellHeight - 0.030f;
                float bottom = 1f - (row + 1) * cellHeight + 0.030f;
                AddRunItemCollectionSlot(
                    parent,
                    type,
                    items[i],
                    selectedItemId == items[i].Id,
                    new Vector2(left, bottom),
                    new Vector2(right, top));
            }
        }

        private void AddRunItemCollectionSlot(RectTransform parent, RunItemType type, RunItemDefinition item, bool selected, Vector2 min, Vector2 max)
        {
            bool discovered = IsRunItemDiscoveredForSelectedClass(item);
            Sprite slotSprite = statusSectionMediumFrameSprite != null
                    ? statusSectionMediumFrameSprite
                    : GetRunStatusSlotFrameSprite();
            RectTransform slot = AddPanel(parent, $"{item.Id} 목록 슬롯", Color.white, slotSprite);
            SetAnchors(slot, min, max);

            Image image = slot.GetComponent<Image>();
            image.raycastTarget = discovered;
            image.type = GetImageType(slotSprite);

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            button.interactable = discovered;
            if (discovered)
            {
                button.onClick.AddListener(() =>
                {
                    SelectRunItemFromCollection(item);
                    RefreshRunStatusMainPanel();
                    ShowRunItemCollectionDetail(type, item.Id);
                });
            }

            Sprite iconSprite = GetRunItemIcon(item);
            if (iconSprite != null)
            {
                Image icon = AddImage(slot, $"{item.Id} 아이콘", discovered ? Color.white : new Color(0.045f, 0.060f, 0.060f, 0.78f));
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(icon.rectTransform, new Vector2(0.135f, 0.185f), new Vector2(0.865f, 0.875f));
            }
            else
            {
                Image empty = AddImage(slot, $"{item.Id} 실루엣", new Color(0.035f, 0.050f, 0.050f, 0.82f));
                empty.raycastTarget = false;
                SetAnchors(empty.rectTransform, new Vector2(0.180f, 0.220f), new Vector2(0.820f, 0.840f));
            }

            if (selected && selectionFrameSprite != null)
            {
                Image selection = AddImage(slot, $"{item.Id} 선택 표시", new Color(0.65f, 1f, 0.94f, 0.78f));
                selection.sprite = selectionFrameSprite;
                selection.type = GetImageType(selectionFrameSprite);
                selection.raycastTarget = false;
                Stretch(selection.rectTransform);
            }

            if (!discovered)
            {
                Text locked = AddText(slot, $"{item.Id} 미발견", "미발견", 12, TextAnchor.MiddleCenter, new Color(0.62f, 0.68f, 0.64f, 0.86f));
                locked.resizeTextForBestFit = false;
                locked.resizeTextMinSize = 8;
                locked.resizeTextMaxSize = 12;
                SetAnchors(locked.rectTransform, new Vector2(0.115f, 0.045f), new Vector2(0.885f, 0.180f));
            }
        }

        private void AddRunItemCollectionDescription(RectTransform parent, RunItemType type, RunItemDefinition item)
        {
            Sprite detailSprite = GetRunStatusDetailBoxFrameSprite();
            RectTransform detail = AddPanel(parent, $"{GetRunItemTypeName(type)} 설명", Color.white, detailSprite);
            SetAnchors(detail, new Vector2(0.145f, 0.155f), new Vector2(0.855f, 0.392f));

            if (item == null)
            {
                Text empty = AddText(detail, $"{GetRunItemTypeName(type)} 미발견 설명", $"아직 발견한 {GetRunItemTypeName(type)}이 없습니다.", 21, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.78f, 1f));
                empty.resizeTextForBestFit = false;
                empty.resizeTextMinSize = 13;
                empty.resizeTextMaxSize = 21;
                AddTextGlow(empty, new Color(0f, 0f, 0f, 0.82f), new Color(0.08f, 0.62f, 0.58f, 0.28f), new Vector2(0.9f, -1.0f));
                SetAnchors(empty.rectTransform, new Vector2(0.085f, 0.220f), new Vector2(0.915f, 0.790f));
                return;
            }

            Sprite iconSprite = GetRunItemIcon(item);
            if (iconSprite != null)
            {
                Image icon = AddImage(detail, $"{item.Id} 선택 아이콘", Color.white);
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(icon.rectTransform, new Vector2(0.055f, 0.220f), new Vector2(0.205f, 0.780f));
            }

            Text name = AddText(detail, $"{item.Id} 선택 이름", $"{GetRunItemTypeName(item.Type)} | {item.Name}", 22, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.72f, 1f));
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = false;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 22;
            AddTextGlow(name, new Color(0f, 0f, 0f, 0.86f), new Color(0.08f, 0.62f, 0.58f, 0.30f), new Vector2(1.0f, -1.1f));
            SetAnchors(name.rectTransform, new Vector2(0.245f, 0.675f), new Vector2(0.915f, 0.860f));

            Text effect = AddText(detail, $"{item.Id} 선택 효과", item.Effect, 16, TextAnchor.UpperLeft, new Color(0.88f, 0.84f, 0.76f, 1f));
            effect.resizeTextForBestFit = false;
            effect.resizeTextMinSize = 10;
            effect.resizeTextMaxSize = 16;
            effect.horizontalOverflow = HorizontalWrapMode.Wrap;
            effect.verticalOverflow = VerticalWrapMode.Truncate;
            effect.lineSpacing = 0.92f;
            SetAnchors(effect.rectTransform, new Vector2(0.245f, 0.495f), new Vector2(0.915f, 0.650f));

            Text description = AddText(detail, $"{item.Id} 선택 설명", item.Description, 15, TextAnchor.UpperLeft, new Color(0.78f, 0.76f, 0.70f, 0.95f));
            description.resizeTextForBestFit = false;
            description.resizeTextMinSize = 10;
            description.resizeTextMaxSize = 15;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.lineSpacing = 0.90f;
            SetAnchors(description.rectTransform, new Vector2(0.245f, 0.285f), new Vector2(0.915f, 0.455f));
        }

        private RunItemDefinition ResolveSelectedRunItemForCollection(RunItemType type, string selectedItemId, IReadOnlyList<RunItemDefinition> items)
        {
            RunItemDefinition selected = !string.IsNullOrWhiteSpace(selectedItemId)
                ? items.FirstOrDefault(item => item.Id == selectedItemId && IsRunItemDiscoveredForSelectedClass(item))
                : null;
            selected ??= GetEquippedRunItemByType(type);
            if (selected != null && IsRunItemDiscoveredForSelectedClass(selected))
            {
                return selected;
            }

            return items.FirstOrDefault(IsRunItemDiscoveredForSelectedClass);
        }

        private static string GetRunItemCollectionTitle(RunItemType type)
        {
            return type switch
            {
                RunItemType.Blessing => "발견한 축복 목록",
                RunItemType.Curse => "발견한 저주 목록",
                _ => "발견한 유물 목록"
            };
        }

        private void ShowRunStatusDetail(string title, string body)
        {
            HideRunStatusDetail();
            if (runStatusPanel == null)
            {
                return;
            }

            if (runStatusMainPanel != null)
            {
                runStatusMainPanel.gameObject.SetActive(false);
            }

            runStatusDetailPanel = AddPanel(runStatusPanel, $"{title} 상세 페이지", new Color(1f, 1f, 1f, 0f));
            Stretch(runStatusDetailPanel);
            runStatusDetailPanel.SetAsLastSibling();

            Sprite detailSprite = statusPanelFrameSprite != null
                ? statusPanelFrameSprite
                : eventMessageFrameSprite != null
                    ? eventMessageFrameSprite
                    : panelSprite;
            RectTransform detailWindow = AddPanel(runStatusDetailPanel, $"{title} 상세 창", Color.white, detailSprite);
            detailWindow.gameObject.AddComponent<RectMask2D>();
            SetAnchors(detailWindow, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.885f));

            AddRunStatusLabelBox(
                runStatusDetailPanel,
                "상세 제목 박스",
                title,
                new Vector2(0.295f, 0.895f),
                new Vector2(0.705f, 0.995f),
                26);

            AddRunStatusTextButton(
                runStatusDetailPanel,
                "상세 닫기",
                "닫기",
                new Vector2(0.835f, 0.905f),
                new Vector2(0.955f, 0.985f),
                HideRunStatusDetail,
                18);

            RectTransform detailBodyRoot = AddPanel(detailWindow, "상세 내용 안전영역", new Color(1f, 1f, 1f, 0f));
            detailBodyRoot.gameObject.AddComponent<RectMask2D>();
            SetAnchors(detailBodyRoot, new Vector2(0.115f, 0.170f), new Vector2(0.885f, 0.760f));

            PopulateRunStatusDetailBody(detailBodyRoot, title, body);
        }

        private void PopulateRunStatusDetailBody(RectTransform parent, string title, string body)
        {
            if (title.StartsWith("보유카드", StringComparison.Ordinal))
            {
                AddDetailText(
                    parent,
                    "보유카드 요약",
                    BuildDeckOverviewCompactText(),
                    22,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.055f, 0.795f),
                    new Vector2(0.930f, 0.975f),
                    1.00f,
                    17);
                AddDetailColumns(
                    parent,
                    "보유카드 목록",
                    SplitNonEmptyLines(BuildDeckListText()),
                    3,
                    18,
                    new Vector2(0.010f, 0.030f),
                    new Vector2(0.980f, 0.750f),
                    0.96f,
                    14);
                return;
            }

            if (title == "카드 시너지 조합법")
            {
                IReadOnlyList<CombinationRecipe> recipes = GetOrderedCombinationRecipesForDisplay();
                int completedCount = recipes.Count(IsCombinationComplete);
                int nearlyCompleteCount = recipes.Count(recipe => !IsCombinationComplete(recipe) && GetCombinationOwnedCount(recipe) >= recipe.RequiredCardIds.Count - 1);
                AddDetailText(
                    parent,
                    "조합법 요약",
                    $"완성 {completedCount}/{recipes.Count}   거의 완성 {nearlyCompleteCount}\n공격/방어/특수 카드 3장을 모으면 조합 효과가 열립니다.",
                    22,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.055f, 0.795f),
                    new Vector2(0.930f, 0.975f),
                    1.00f,
                    17);

                int splitIndex = Mathf.CeilToInt(recipes.Count / 2f);
                AddDetailColumns(
                    parent,
                    "조합법 목록",
                    new List<string>
                    {
                        BuildCombinationColumnText(recipes.Take(splitIndex), 30),
                        BuildCombinationColumnText(recipes.Skip(splitIndex), 30)
                    },
                    2,
                    18,
                    new Vector2(0.015f, 0.030f),
                    new Vector2(0.975f, 0.750f),
                    0.94f,
                    14,
                    true);
                return;
            }

            List<string> columns = SplitParagraphsIntoColumns(SplitParagraphs(body), 2);
            AddDetailColumns(
                parent,
                $"{title} 문단",
                columns,
                2,
                20,
                new Vector2(0.030f, 0.050f),
                new Vector2(0.970f, 0.955f),
                1.05f,
                16,
                true);
        }

        private Text AddDetailText(
            RectTransform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 min,
            Vector2 max,
            float lineSpacing,
            int minSize)
        {
            Text detailText = AddText(parent, name, text, fontSize, alignment, new Color(0.87f, 0.96f, 0.90f, 1f));
            detailText.resizeTextMinSize = minSize;
            detailText.resizeTextMaxSize = fontSize;
            detailText.lineSpacing = lineSpacing;
            AddTextGlow(detailText, new Color(0f, 0f, 0f, 0.74f), new Color(0.04f, 0.34f, 0.33f, 0.26f), new Vector2(1.0f, -1.2f));
            SetAnchors(detailText.rectTransform, min, max);
            return detailText;
        }

        private void AddDetailColumns(
            RectTransform parent,
            string name,
            IReadOnlyList<string> entries,
            int columnCount,
            int fontSize,
            Vector2 min,
            Vector2 max,
            float lineSpacing,
            int minSize,
            bool entriesAreColumns = false)
        {
            if (columnCount <= 0)
            {
                return;
            }

            List<string> columns = entriesAreColumns
                ? entries.ToList()
                : SplitLinesIntoColumns(entries, columnCount);
            float gap = columnCount == 1 ? 0f : 0.040f;
            float totalWidth = max.x - min.x;
            float columnWidth = (totalWidth - gap * (columnCount - 1)) / columnCount;

            for (int i = 0; i < columnCount; i += 1)
            {
                string columnText = i < columns.Count ? columns[i] : string.Empty;
                float left = min.x + i * (columnWidth + gap);
                AddDetailText(
                    parent,
                    $"{name} {i + 1}",
                    columnText,
                    fontSize,
                    TextAnchor.UpperLeft,
                    new Vector2(left, min.y),
                    new Vector2(left + columnWidth, max.y),
                    lineSpacing,
                    minSize);
            }
        }

        private static List<string> SplitNonEmptyLines(string text)
        {
            return text
                .Split('\n')
                .Select(line => line.TrimEnd())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private static List<string> SplitLinesIntoColumns(IReadOnlyList<string> lines, int columnCount)
        {
            List<string> columns = new();
            int rowsPerColumn = Mathf.CeilToInt(lines.Count / (float)Mathf.Max(1, columnCount));
            for (int column = 0; column < columnCount; column += 1)
            {
                int start = column * rowsPerColumn;
                int count = Mathf.Clamp(lines.Count - start, 0, rowsPerColumn);
                columns.Add(count > 0 ? string.Join("\n", lines.Skip(start).Take(count)) : string.Empty);
            }

            return columns;
        }

        private static List<string> SplitParagraphs(string body)
        {
            return body
                .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(paragraph => paragraph.Trim())
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList();
        }

        private static List<string> SplitParagraphsIntoColumns(IReadOnlyList<string> paragraphs, int columnCount)
        {
            List<string> columns = new();
            if (paragraphs.Count == 0 || columnCount <= 1)
            {
                columns.Add(string.Join("\n\n", paragraphs));
                return columns;
            }

            int totalWeight = paragraphs.Sum(EstimateParagraphWeight);
            int targetWeight = Mathf.CeilToInt(totalWeight / (float)columnCount);
            int index = 0;
            for (int column = 0; column < columnCount; column += 1)
            {
                List<string> columnParagraphs = new();
                int columnWeight = 0;
                int remainingColumns = columnCount - column - 1;
                while (index < paragraphs.Count && paragraphs.Count - index > remainingColumns)
                {
                    string paragraph = paragraphs[index];
                    columnParagraphs.Add(paragraph);
                    columnWeight += EstimateParagraphWeight(paragraph);
                    index += 1;
                    if (columnWeight >= targetWeight && remainingColumns > 0)
                    {
                        break;
                    }
                }

                columns.Add(string.Join("\n\n", columnParagraphs));
            }

            return columns;
        }

        private static int EstimateParagraphWeight(string paragraph)
        {
            int lineCount = paragraph.Count(character => character == '\n') + 1;
            int longLineWeight = paragraph
                .Split('\n')
                .Sum(line => Mathf.Max(1, Mathf.CeilToInt(line.Length / 28f)));
            return Mathf.Max(lineCount, longLineWeight);
        }

        private void HideRunStatusDetail()
        {
            if (runStatusDetailPanel == null)
            {
                return;
            }

            Destroy(runStatusDetailPanel.gameObject);
            runStatusDetailPanel = null;
            if (runStatusMainPanel != null)
            {
                runStatusMainPanel.gameObject.SetActive(true);
            }
        }

        private List<DoorOption> GenerateDoorOptions()
        {
            if (ShouldForceCombatDoorOptions())
            {
                return GenerateForcedCombatDoorOptions();
            }

            WeightedDoorType[] pool = CreateDebtAdjustedDoorPool();

            List<DoorOption> options = new();
            while (options.Count < 3)
            {
                DoorType type = PickWeightedDoorType(pool);
                if (options.Any(option => option.Type == type) && type != DoorType.Battle)
                {
                    continue;
                }

                options.Add(CreateDoorOption(type));
            }

            return options;
        }

        private WeightedDoorType[] CreateDebtAdjustedDoorPool()
        {
            float pressure = GetDebtDoorPressure();
            float battleWeight = Mathf.Lerp(3.0f, 1.45f, pressure);
            float eliteWeight = Mathf.Lerp(1.0f, 3.25f, pressure);
            float safeDoorWeight = Mathf.Lerp(1.0f, 0.35f, pressure);
            float eventWeight = Mathf.Lerp(1.0f, 0.75f, pressure);
            float restWeight = safeDoorWeight;
            if (HasRunItem("blessing_open_dawn_gate"))
            {
                restWeight += 0.65f;
            }

            if (HasRunItem("curse_skeletal_key"))
            {
                eliteWeight += 0.85f;
            }

            return new[]
            {
                new WeightedDoorType(DoorType.Battle, battleWeight),
                new WeightedDoorType(DoorType.Elite, eliteWeight),
                new WeightedDoorType(DoorType.Shop, safeDoorWeight),
                new WeightedDoorType(DoorType.Treasure, safeDoorWeight),
                new WeightedDoorType(DoorType.Event, eventWeight),
                new WeightedDoorType(DoorType.Rest, restWeight),
                new WeightedDoorType(DoorType.Curse, 1.0f)
            };
        }

        private float GetDebtDoorPressure()
        {
            return Mathf.Clamp01(Mathf.Max(0, debt) * DebtDoorPressurePerDebt);
        }

        private static DoorType PickWeightedDoorType(IReadOnlyList<WeightedDoorType> pool)
        {
            float totalWeight = pool.Sum(candidate => Mathf.Max(0.001f, candidate.Weight));
            float roll = Random.value * totalWeight;
            foreach (WeightedDoorType candidate in pool)
            {
                roll -= Mathf.Max(0.001f, candidate.Weight);
                if (roll <= 0f)
                {
                    return candidate.Type;
                }
            }

            return pool[pool.Count - 1].Type;
        }

        private bool IsBossDoorReady()
        {
            if (endlessModeActive)
            {
                return nextEndlessBossRoom > 0 && roomsCleared >= nextEndlessBossRoom;
            }

            return roomsCleared >= TargetRooms && combatEncountersCompleted >= MinimumPreBossCombats;
        }

        private bool ShouldForceCombatDoorOptions()
        {
            int requiredCombats = Mathf.Max(0, MinimumPreBossCombats - combatEncountersCompleted);
            if (requiredCombats <= 0)
            {
                return false;
            }

            if (consecutiveNonCombatDoors >= MaxConsecutiveNonCombatDoors)
            {
                return true;
            }

            int roomsBeforeBoss = Mathf.Max(0, TargetRooms - roomsCleared);
            return requiredCombats >= roomsBeforeBoss;
        }

        private List<DoorOption> GenerateForcedCombatDoorOptions()
        {
            DoorType[] forcedTypes = roomsCleared >= 4
                ? new[] { DoorType.Battle, DoorType.Elite, DoorType.Curse }
                : new[] { DoorType.Battle, DoorType.Battle, DoorType.Curse };
            return forcedTypes.Select(CreateDoorOption).ToList();
        }

        private static bool IsCombatForcingDoor(DoorType type)
        {
            return type is DoorType.Battle or DoorType.Elite or DoorType.Curse;
        }

        private int GetBaseDoorInsightLevel()
        {
            return selectedClass == CharacterClass.Oracle ? 1 : 0;
        }

        private int GetDoorInsightLevel()
        {
            int itemBonus = 0;
            if (HasRunItem("relic_cracked_gate_key"))
            {
                itemBonus += 1;
            }

            if (HasRunItem("blessing_open_dawn_gate"))
            {
                itemBonus += 1;
            }

            if (HasRunItem("curse_broken_mirror"))
            {
                itemBonus += 1;
            }

            return Mathf.Clamp(Mathf.Max(doorInsightLevel, GetBaseDoorInsightLevel()) + itemBonus, 0, 3);
        }

        private void ResetDoorInsightAfterChoice()
        {
            doorInsightLevel = GetBaseDoorInsightLevel();
        }

        private void AddDoorInsight(int amount)
        {
            int currentLevel = Mathf.Max(doorInsightLevel, GetBaseDoorInsightLevel());
            doorInsightLevel = Mathf.Clamp(currentLevel + Mathf.Max(1, amount), 0, 3);
        }

        private DoorOption CreateBossDoorOption()
        {
            int insightLevel = GetDoorInsightLevel();
            if (endlessModeActive)
            {
                return new DoorOption(
                    DoorType.Boss,
                    "심연의 고리대금업자",
                    GetBossDoorHint(insightLevel),
                    insightLevel >= 3 ? "기록 갱신" : "기록전");
            }

            RunItemType rewardType = GetBossRewardItemType(currentDifficulty);
            return new DoorOption(
                DoorType.Boss,
                GetCurrentBossDoorName(),
                GetBossDoorHint(insightLevel),
                insightLevel >= 3 ? $"{GetRunItemTypeName(rewardType)} 보상" : "결전");
        }

        private DoorOption CreateDoorOption(DoorType type)
        {
            int insightLevel = GetDoorInsightLevel();
            return type switch
            {
                DoorType.Battle => new DoorOption(type, "전투의 문", GetDoorHint(type, insightLevel), insightLevel >= 2 ? "전투" : "입장"),
                DoorType.Elite => new DoorOption(type, "정예의 문", GetDoorHint(type, insightLevel), insightLevel >= 2 ? "정예" : "위험"),
                DoorType.Shop => new DoorOption(type, "상점의 문", GetDoorHint(type, insightLevel), "상점"),
                DoorType.Treasure => new DoorOption(type, "보물의 문", GetDoorHint(type, insightLevel), "획득"),
                DoorType.Event => new DoorOption(type, "사건의 문", GetDoorHint(type, insightLevel), "선택"),
                DoorType.Rest => new DoorOption(type, "휴식의 문", GetDoorHint(type, insightLevel), "휴식"),
                DoorType.Curse => new DoorOption(type, "대가의 문", GetDoorHint(type, insightLevel), "빚 +1"),
                _ => new DoorOption(DoorType.Boss, "문지기", "마지막 계약이 열립니다.", "결전")
            };
        }

        private string GetDoorHint(DoorType type, int insightLevel)
        {
            return type switch
            {
                DoorType.Battle => insightLevel switch
                {
                    <= 0 => "적의 숨소리가 들립니다.",
                    1 => "일반 적이 기다립니다. 카드 보상을 얻습니다.",
                    2 => "예언: 일반 전투. 승리 시 카드와 금화를 얻습니다.",
                    _ => $"예언 선명: 승리하면 카드 선택과 금화를 얻습니다. {GetPostCombatSustainHint()}"
                },
                DoorType.Elite => insightLevel switch
                {
                    <= 0 => "무거운 그림자가 문 뒤에 있습니다.",
                    1 => "강한 적과 높은 보상이 기다립니다.",
                    2 => "예언: 정예 전투. 피해 위험이 크지만 카드 보상이 좋아집니다.",
                    _ => $"예언 선명: 정예 전투입니다. 승리하면 카드 보상 강화, 금화, {GetRunItemDiscoveryHint(EliteRunItemDiscoveryChance)}"
                },
                DoorType.Shop => insightLevel switch
                {
                    <= 0 => "낡은 동전 소리가 들립니다.",
                    1 => "금화로 카드와 회복을 구매합니다.",
                    2 => "예언: 상점. 카드 구매와 직업 조합 강화를 확인합니다.",
                    _ => "예언 선명: 카드 3장 또는 카드 2장+아이템 1개가 판매됩니다. 직업 조합 강화도 가능합니다."
                },
                DoorType.Treasure => insightLevel switch
                {
                    <= 0 => "조용한 빛이 새어 나옵니다.",
                    1 => "금화와 카드가 함께 나올 수 있습니다.",
                    2 => "예언: 보물. 금화와 카드 보상을 기대할 수 있습니다.",
                    _ => "예언 선명: 전투 없이 금화 24~42와 보물 카드 1장을 즉시 얻습니다."
                },
                DoorType.Event => insightLevel switch
                {
                    <= 0 => "속삭임이 균열을 타고 흐릅니다.",
                    1 => "선택형 계약 이벤트입니다.",
                    2 => "예언: 사건. 보상과 대가 중 하나를 선택합니다.",
                    _ => "예언 선명: 체력 6을 잃고 금화 55를 받거나, 카드 1장을 받고 빚 +1을 선택합니다."
                },
                DoorType.Rest => insightLevel switch
                {
                    <= 0 => "따뜻한 빛이 문틈에 고입니다.",
                    1 => "체력을 회복하거나 금화를 얻습니다.",
                    2 => "예언: 휴식. 체력 회복 또는 안전한 보상이 있습니다.",
                    _ => $"예언 선명: 위험 없이 체력 {GetRestHealAmount()} 회복 또는 금화 30 획득을 선택합니다."
                },
                DoorType.Curse => insightLevel switch
                {
                    <= 0 => "문이 대가를 요구합니다.",
                    1 => "빚이 늘지만 보상도 커집니다.",
                    2 => "예언: 대가. 빚이 늘고 정예 위협 뒤에 큰 보상이 있습니다.",
                    _ => $"예언 선명: 입장 시 빚 +{GetCurseDoorDebtGain()} 후 정예 전투. 승리하면 {GetRunItemDiscoveryHint(CurseDoorRunItemDiscoveryChance)}"
                },
                _ => "마지막 계약이 열립니다."
            };
        }

        private string GetBossDoorHint(int insightLevel)
        {
            if (endlessModeActive)
            {
                return insightLevel switch
                {
                    <= 0 => "심연이 기록을 부릅니다.",
                    1 => "무한 보스가 기록을 시험합니다.",
                    2 => $"예언: 무한 {roomsCleared}문 보스. 승리하면 기록과 금화 보상이 갱신됩니다.",
                    _ => $"예언 선명: 승리하면 무한 기록 {roomsCleared}문을 갱신하고 보스 금화 보상을 받습니다. 아이템 보상은 없습니다."
                };
            }

            RunItemType rewardType = GetBossRewardItemType(currentDifficulty);
            return insightLevel switch
            {
                <= 0 => "마지막 계약이 열립니다.",
                1 => $"{GetDifficultyName(currentDifficulty)} 보스가 기다립니다.",
                2 => $"예언: {GetDifficultyName(currentDifficulty)} 보스. 승리하면 10문 이후 선택지가 열립니다.",
                _ => $"예언 선명: {GetBossRewardForecastText(rewardType)} 이후 10문 선택지가 열립니다."
            };
        }

        private string GetBossRewardForecastText(RunItemType rewardType)
        {
            RunItemDefinition item = GetPredictedBossRunItemReward(rewardType);
            if (item != null)
            {
                return $"승리하면 '{item.Name}' {GetRunItemTypeName(rewardType)} 아이템을 획득할 수 있습니다.";
            }

            return $"획득 가능한 {GetRunItemTypeName(rewardType)} 아이템은 이미 장착 중입니다.";
        }

        private string GetPostCombatSustainHint()
        {
            return ShouldOfferPostCombatSustain()
                ? "전투 후 체력 회복 또는 금화 정비도 선택합니다."
                : "전투 후 바로 카드 보상으로 이어집니다.";
        }

        private string GetRunItemDiscoveryHint(float chance)
        {
            string itemTypes = GetUnlockedRunItemTypeSummary();
            if (string.IsNullOrWhiteSpace(itemTypes))
            {
                return "아직 발견 가능한 장착 아이템은 없습니다.";
            }

            int percent = Mathf.RoundToInt(chance * 100f);
            return $"{percent}% 확률로 {itemTypes} 중 하나를 발견합니다.";
        }

        private string GetUnlockedRunItemTypeSummary()
        {
            RunItemType[] orderedTypes =
            {
                RunItemType.Relic,
                RunItemType.Blessing,
                RunItemType.Curse
            };
            List<string> names = orderedTypes
                .Where(IsRunItemTypeUnlockedForSelectedClass)
                .Select(type => GetRunItemTypeName(type))
                .ToList();
            return names.Count == 0 ? string.Empty : $"{string.Join("/", names)} 아이템";
        }

        private int GetCurseDoorDebtGain()
        {
            return Mathf.Max(0, 1 - curseReduction);
        }

        private void ResolveDoor(DoorOption option)
        {
            ResetDoorInsightAfterChoice();
            if (option.Type != DoorType.Boss)
            {
                roomsCleared += 1;
                if (IsCombatForcingDoor(option.Type))
                {
                    consecutiveNonCombatDoors = 0;
                }
                else
                {
                    consecutiveNonCombatDoors += 1;
                }
            }

            switch (option.Type)
            {
                case DoorType.Battle:
                    currentCombatDoorType = DoorType.Battle;
                    StartCombat(CreateEnemy(false, false));
                    break;
                case DoorType.Elite:
                    currentCombatDoorType = DoorType.Elite;
                    StartCombat(CreateEnemy(true, false));
                    break;
                case DoorType.Shop:
                    ResetCurrentShopOffers();
                    ShowShop();
                    break;
                case DoorType.Treasure:
                    ShowTreasure();
                    break;
                case DoorType.Event:
                    ShowEvent();
                    break;
                case DoorType.Rest:
                    ShowRest();
                    break;
                case DoorType.Curse:
                    ShowCurseEvent();
                    break;
                case DoorType.Boss:
                    currentCombatDoorType = DoorType.Boss;
                    StartCombat(CreateEnemy(true, true));
                    break;
            }
        }

        private EnemyState CreateEnemy(bool elite, bool boss)
        {
            if (boss)
            {
                return CreateBossEnemy();
            }

            EnemyTemplate[] templates = IsHardModeFeatureActive()
                ? BaseEnemyTemplates.Concat(HardModeEnemyTemplates).ToArray()
                : BaseEnemyTemplates;
            int index = IsHardModeFeatureActive()
                ? Random.Range(0, templates.Length)
                : Mathf.Clamp(roomsCleared - 1, 0, templates.Length - 1);
            EnemyTemplate template = templates[index];
            int health = template.Health + (elite ? 16 : 0);
            int attack = template.Attack + (elite ? 3 : 0);
            int block = template.Block + (elite ? 3 : 0);
            int reward = elite ? 26 : 14;
            return CreateScaledEnemyState(template.Id, template.Name, health, attack, block, elite, false, reward);
        }

        private EnemyState CreateBossEnemy()
        {
            if (currentDifficulty == RunDifficulty.Normal && !endlessModeActive)
            {
                return CreateScaledEnemyState(NormalBossId, "부채 심판관", 150, 18, 11, true, true, 0);
            }

            if (currentDifficulty == RunDifficulty.Hard || endlessModeActive)
            {
                return CreateScaledEnemyState(HardBossId, endlessModeActive ? "심연의 고리대금업자" : "무저갱의 고리대금업자", 182, 22, 14, true, true, 0);
            }

            return CreateScaledEnemyState(EasyBossId, "세 번째 문의 문지기", 126, 15, 9, true, true, 0);
        }

        private string GetCurrentBossDoorName()
        {
            return currentDifficulty switch
            {
                RunDifficulty.Normal => "부채 심판관",
                RunDifficulty.Hard => "무저갱의 고리대금업자",
                _ => "문지기"
            };
        }

        private EnemyState CreateDebtClearBoss()
        {
            return new EnemyState(DebtClearBossId, "무저갱의 채권자", 286, 31, 22, true, true, 0);
        }

        private EnemyState CreateScaledEnemyState(string id, string name, int health, int attack, int block, bool elite, bool boss, int reward)
        {
            float healthScale = currentDifficulty switch
            {
                RunDifficulty.Easy => 0.72f,
                RunDifficulty.Hard => 1.20f,
                _ => 0.96f
            };
            float attackScale = currentDifficulty switch
            {
                RunDifficulty.Easy => 0.68f,
                RunDifficulty.Hard => 1.22f,
                _ => 0.96f
            };
            float blockScale = currentDifficulty switch
            {
                RunDifficulty.Easy => 0.70f,
                RunDifficulty.Hard => 1.18f,
                _ => 0.95f
            };

            if (endlessModeActive)
            {
                int endlessDepth = Mathf.Max(0, roomsCleared - TargetRooms);
                healthScale *= 1f + endlessDepth * 0.075f + endlessBossesDefeated * 0.12f;
                attackScale *= 1f + endlessDepth * 0.050f + endlessBossesDefeated * 0.08f;
                blockScale *= 1f + endlessDepth * 0.040f + endlessBossesDefeated * 0.06f;
            }

            if (HasRunItem("curse_broken_mirror"))
            {
                attackScale *= 1.10f;
            }

            if (HasRunItem("curse_watchers_eye") && (elite || boss))
            {
                healthScale *= 1.15f;
            }

            int scaledHealth = Mathf.Max(boss ? 48 : 16, Mathf.RoundToInt(health * healthScale));
            int scaledAttack = Mathf.Max(1, Mathf.RoundToInt(attack * attackScale));
            int scaledBlock = Mathf.Max(0, Mathf.RoundToInt(block * blockScale));
            int scaledReward = Mathf.Max(0, Mathf.RoundToInt(reward * GetDifficultyRewardScale()) + (endlessModeActive ? Mathf.Max(0, roomsCleared - TargetRooms) : 0));
            return new EnemyState(id, name, scaledHealth, scaledAttack, scaledBlock, elite, boss, scaledReward);
        }

        private float GetDifficultyRewardScale()
        {
            return currentDifficulty switch
            {
                RunDifficulty.Easy => 1.18f,
                RunDifficulty.Hard => 0.92f,
                _ => 1.0f
            };
        }

        private void StartCombat(EnemyState newEnemy)
        {
            PlayBattleMusic(newEnemy.IsBoss);
            if (newEnemy.IsBoss)
            {
                PlayBossStartSfx();
            }

            StopCombatVictorySequence();
            phase = GamePhase.Combat;
            enemy = newEnemy;
            SetLogVisible(true);
            topBar.gameObject.SetActive(true);
            SetBackground(enemy.IsBoss ? bossBackground : battleBackground);
            playerBlock = retainBlockNextTurn ? playerBlock : 0;
            action = StartingAction;
            reflectedDamage = 0;
            pendingDamageReduction = 0;
            preventDeathThisTurn = false;
            oracleBuildTriggeredThisCombat = false;
            exileBuildTriggeredThisCombat = false;
            combatDrawDiscardCount = 0;
            emptyDeckWarningLogged = false;
            gamblerLoadedDiceRollsRemaining = 0;
            gamblerCardReadingAwakened = false;
            oracleAttackDefenseResponses = 0;
            oraclePrecisePredictionAwakened = false;
            oracleNextCardCostReduction = 0;
            exileCurseRemovalsThisCombat = 0;
            exileCurseEaterAwakened = false;
            exileWoundOathTriggeredThisCombat = false;
            exileNextAttackDamageBonus = 0;
            exileNextAttackVulnerableBonus = 0;
            gamblerHardHighLuckAttackUsedThisTurn = false;
            gamblerHardLowLuckDefenseUsedThisTurn = false;
            gamblerHardGoldGainedThisCombat = 0;
            gamblerHardGoldSpikeTriggeredThisCombat = false;
            oracleHardLuckHeldThisTurn = false;
            oracleHardLowHandDrawTriggeredThisCombat = false;
            oracleHardProphecyPrimed = false;
            exileHardFatalOathTriggeredThisCombat = false;
            runItemSkillDiscountsRemaining = HasRunItem("blessing_silver_feather") ? 1 : 0;
            cardsPlayedThisTurn.Clear();
            cardsPlayedThisCombat.Clear();
            combinationTriggersThisTurn.Clear();
            combinationTriggersThisCombat.Clear();
            runItemTriggersThisCombat.Clear();
            activeCard = null;
            activeCardHandIndex = -1;
            activeCardDamageBonusApplied = false;
            activeCardBlockBonusApplied = false;
            activeCardRunItemDamageBonusApplied = false;
            activeCardRunItemBlockBonusApplied = false;
            forbiddenCycleActiveThisTurn = false;
            pendingCombinationDamageBonus = 0;
            pendingCombinationDamageBonusSourceId = string.Empty;
            activeCombinationImpactId = string.Empty;
            activeCombinationImpactStartTime = -100f;
            activeCombinationHudText = null;
            activeCombinationImpactText = null;
            pendingEnemyRevealCombinationImpacts.Clear();
            ResetCombatFeedbackState();
            oncePerCombatUsed.Clear();
            hand.Clear();
            discardPile.Clear();
            drawPile.Clear();
            drawPile.AddRange(deck);
            Shuffle(drawPile);
            RollLuckForTurn();
            DrawUpToHandSize();
            combatDrawDiscardCount = 0;
            ApplyBuildCombatStartBonuses();
            ApplyCombinationCombatStartBonuses();
            ApplyRunItemCombatStartBonuses();
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            PrepareEnemyIntent();
            AddLog($"{enemy.Name}와 전투를 시작했습니다.");
            RenderCombat();
            if (!ShowEnemyReveal())
            {
                PlayPendingEnemyRevealCombinationImpacts();
            }
        }

        private void RenderCombat()
        {
            ClearContent();
            SetAnchors(contentRoot, new Vector2(0.035f, 0.085f), new Vector2(0.745f, 0.865f));
            subtitleText.text = enemy.IsBoss ? "보스가 세 번째 문의 계약을 비틀고 있습니다" : "카드, 행동력, 행운으로 턴을 설계하세요";
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(true);
            SetPrimaryButtonDefaultPlacement();
            SetButtonLabel(primaryButton, "턴 종료");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(EndTurn);

            Sprite embeddedEnemyHudSprite = GetEnemyHudFrameSprite(enemy.Id);
            RectTransform enemyPanel = AddPanel(contentRoot, "적 정보", Color.white, embeddedEnemyHudSprite != null ? embeddedEnemyHudSprite : enemyStatusFrameSprite);
            SetAnchors(enemyPanel, new Vector2(0.045f, 0.595f), new Vector2(0.995f, 0.970f));
            Image enemyPanelImage = enemyPanel.GetComponent<Image>();
            enemyPanelImage.type = Image.Type.Simple;
            enemyPanelImage.raycastTarget = false;

            if (embeddedEnemyHudSprite == null)
            {
                Image enemyArt = AddImage(enemyPanel, "적 이미지", Color.white);
                enemyArt.sprite = GetEnemyCombatSprite();
                enemyArt.preserveAspect = true;
                SetAnchors(enemyArt.rectTransform, new Vector2(0.062f, 0.205f), new Vector2(0.220f, 0.790f));
            }

            Text enemyName = AddText(enemyPanel, "적 이름", enemy.Name, 23, TextAnchor.MiddleLeft, Color.white);
            enemyName.fontStyle = FontStyle.Bold;
            enemyName.resizeTextMinSize = 15;
            SetAnchors(enemyName.rectTransform, new Vector2(0.280f, 0.585f), new Vector2(0.770f, 0.710f));

            Text enemyStats = AddText(enemyPanel, "적 상태", $"체력 {enemy.Health}/{enemy.MaxHealth}   방어 {enemy.Block}   출혈 {enemy.Bleed}   취약 {enemy.Vulnerable}", 16, TextAnchor.MiddleLeft, new Color(0.90f, 0.82f, 0.70f, 1f));
            enemyStats.resizeTextMinSize = 12;
            SetAnchors(enemyStats.rectTransform, new Vector2(0.280f, 0.430f), new Vector2(0.770f, 0.555f));

            Text candidate = AddText(enemyPanel, "적 후보 카드", $"후보: {enemy.CandidateLabel}", 13, TextAnchor.MiddleLeft, new Color(0.72f, 0.94f, 0.91f, 0.92f));
            candidate.resizeTextMinSize = 10;
            candidate.lineSpacing = 0.88f;
            SetAnchors(candidate.rectTransform, new Vector2(0.280f, 0.270f), new Vector2(0.770f, 0.405f));

            Text intent = AddText(enemyPanel, "의도", $"{enemy.IntentCardName}\n{enemy.IntentLabel}", 15, TextAnchor.MiddleCenter, GetIntentColor());
            intent.fontStyle = FontStyle.Bold;
            intent.resizeTextMinSize = 10;
            intent.lineSpacing = 0.88f;
            SetAnchors(intent.rectTransform, new Vector2(0.795f, 0.315f), new Vector2(0.955f, 0.690f));

            AddHealthBar(enemyPanel, enemy.Health, enemy.MaxHealth, new Vector2(0.174f, 0.072f), new Vector2(0.830f, 0.225f), new Color(0.62f, 0.12f, 0.10f, 1f));

            CreateDeckWidget();

            RectTransform handPanel = AddPanel(contentRoot, "손패", new Color(1f, 1f, 1f, 0f));
            SetAnchors(handPanel, new Vector2(0.145f, -0.100f), new Vector2(1.000f, 0.470f));

            for (int i = 0; i < hand.Count; i += 1)
            {
                CardData card = hand[i];
                Button cardButton = CreateCardButton(handPanel, card, i, hand.Count, true);
                int index = i;
                cardButton.onClick.AddListener(() => PlayCard(index));
                cardButton.interactable = CanPlay(card);
            }

            CreateDrawAnimationGhosts();
            CreatePlayerCombatHud();
            CreateCombatFeedbackOverlay();

            RefreshTopBar();
            RefreshLog();
        }

        private void CreatePlayerCombatHud()
        {
            RectTransform playerHud = AddPanel(
                contentRoot,
                "플레이어 전투 상태",
                Color.white,
                playerCombatStatusFrameSprite != null ? playerCombatStatusFrameSprite : eventMessageFrameSprite);
            SetAnchors(playerHud, new Vector2(0.325f, -0.125f), new Vector2(0.970f, 0.090f));

            Image playerHudImage = playerHud.GetComponent<Image>();
            playerHudImage.type = Image.Type.Simple;
            playerHudImage.raycastTarget = false;
            playerHud.SetAsLastSibling();

            Text title = AddText(playerHud, "플레이어 상태 제목", $"{GetClassName(selectedClass)} 상태", 16, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Normal;
            title.resizeTextMinSize = 13;
            SetAnchors(title.rectTransform, new Vector2(0.110f, 0.650f), new Vector2(0.500f, 0.820f));

            Text defenseText = AddText(playerHud, "플레이어 방어", $"방어 {playerBlock}", 14, TextAnchor.MiddleRight, new Color(0.92f, 0.86f, 0.72f, 1f));
            defenseText.fontStyle = FontStyle.Normal;
            defenseText.resizeTextMinSize = 12;
            SetAnchors(defenseText.rectTransform, new Vector2(0.565f, 0.650f), new Vector2(0.885f, 0.820f));

            activeCombinationHudText = AddText(
                playerHud,
                "현재 활성 조합",
                BuildActiveCombinationHudText(),
                13,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 1f, 0.93f, 0.96f));
            activeCombinationHudText.fontStyle = FontStyle.Bold;
            activeCombinationHudText.resizeTextForBestFit = true;
            activeCombinationHudText.resizeTextMinSize = 9;
            activeCombinationHudText.resizeTextMaxSize = 13;
            AddTextGlow(activeCombinationHudText, new Color(0f, 0f, 0f, 0.86f), new Color(0.05f, 0.45f, 0.43f, 0.42f), new Vector2(1.3f, -1.5f));
            SetAnchors(activeCombinationHudText.rectTransform, new Vector2(0.195f, 0.465f), new Vector2(0.805f, 0.640f));

            activeCombinationImpactText = null;
            if (IsCombinationImpactActive())
            {
                activeCombinationImpactText = AddText(
                    playerHud,
                    "조합 발동 텍스트",
                    GetCombinationImpactName(activeCombinationImpactId),
                    25,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.88f, 0.42f, 1f));
                activeCombinationImpactText.fontStyle = FontStyle.Bold;
                activeCombinationImpactText.resizeTextForBestFit = true;
                activeCombinationImpactText.resizeTextMinSize = 15;
                activeCombinationImpactText.resizeTextMaxSize = 28;
                AddTextGlow(activeCombinationImpactText, new Color(0f, 0f, 0f, 0.95f), new Color(0.05f, 0.92f, 0.86f, 0.78f), new Vector2(2.4f, -2.8f));
                Outline goldOutline = activeCombinationImpactText.gameObject.AddComponent<Outline>();
                goldOutline.effectColor = new Color(1f, 0.58f, 0.16f, 0.70f);
                goldOutline.effectDistance = new Vector2(1.4f, -1.4f);
                SetAnchors(activeCombinationImpactText.rectTransform, new Vector2(0.190f, 0.405f), new Vector2(0.810f, 0.735f));
                activeCombinationImpactText.transform.SetAsLastSibling();
                UpdateCombinationImpactAnimation();
            }

            AddStatusMeterBar(playerHud, "플레이어 체력", "체력", playerHealth, playerMaxHealth, new Vector2(0.110f, 0.315f), new Vector2(0.890f, 0.460f), new Color(0.66f, 0.11f, 0.10f, 1f));
            int maxAction = Mathf.Max(StartingAction, action, 1);
            AddStatusMeterBar(playerHud, "플레이어 행동력", "행동력", action, maxAction, new Vector2(0.110f, 0.190f), new Vector2(0.890f, 0.335f), new Color(0.08f, 0.66f, 0.62f, 1f));
            if (activeCombinationImpactText != null)
            {
                activeCombinationImpactText.transform.SetAsLastSibling();
            }
        }

        private string BuildActiveCombinationHudText()
        {
            List<CombinationRecipe> activeRecipes = GetCombinationRecipes()
                .Where(IsCombinationComplete)
                .OrderByDescending(GetCombinationOwnedCount)
                .ThenBy(recipe => recipe.Name)
                .ToList();
            if (activeRecipes.Count == 0)
            {
                return "현재 활성 조합: 없음";
            }

            const int visibleCount = 3;
            string names = string.Join(", ", activeRecipes.Take(visibleCount).Select(recipe => recipe.Name));
            int hiddenCount = activeRecipes.Count - visibleCount;
            return hiddenCount > 0
                ? $"현재 활성 조합: {names} 외 {hiddenCount}"
                : $"현재 활성 조합: {names}";
        }

        private static string GetCombinationImpactName(string combinationId)
        {
            string traitName = combinationId switch
            {
                "trait_gambler_card_reading" => "특성: 패 읽기",
                "trait_oracle_precise_prediction" => "특성: 정확한 예언",
                "trait_exile_curse_eater" => "특성: 저주 삼키기",
                "trait_exile_wound_oath" => "특성: 상처의 맹세",
                "hard_trait_gambler_ruin_wager" => "특성: 파멸의 판돈",
                "hard_trait_oracle_closed_fate" => "특성: 닫힌 운명 해석",
                "hard_trait_exile_endless_atonement" => "특성: 끝없는 속죄",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(traitName))
            {
                return traitName;
            }

            foreach (CombinationRecipe recipe in GetCombinationRecipes())
            {
                if (recipe.Id == combinationId)
                {
                    return recipe.Name;
                }
            }

            return "조합 발동";
        }

        private void TriggerCombinationImpact(string combinationId)
        {
            if (string.IsNullOrEmpty(combinationId))
            {
                return;
            }

            activeCombinationImpactId = combinationId;
            activeCombinationImpactStartTime = Time.time;
            TriggerCombinationCombatFeedback(combinationId);
            if (activeCombinationHudText != null)
            {
                activeCombinationHudText.gameObject.SetActive(false);
            }
        }

        private bool IsCombinationImpactActive()
        {
            return !string.IsNullOrEmpty(activeCombinationImpactId)
                && Time.time - activeCombinationImpactStartTime <= CombinationImpactSeconds;
        }

        private void UpdateCombinationImpactAnimation()
        {
            bool active = IsCombinationImpactActive() && activeCombinationImpactText != null;
            if (!active)
            {
                if (activeCombinationImpactText != null)
                {
                    activeCombinationImpactText.gameObject.SetActive(false);
                    activeCombinationImpactText = null;
                }

                if (activeCombinationHudText != null)
                {
                    activeCombinationHudText.gameObject.SetActive(true);
                }

                if (!IsCombinationImpactActive())
                {
                    activeCombinationImpactId = string.Empty;
                }

                return;
            }

            float elapsed = Time.time - activeCombinationImpactStartTime;
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.18f));
            float disappear = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.72f) / 0.33f));
            float alpha = appear * disappear;
            activeCombinationImpactText.color = new Color(1f, 0.88f + 0.10f * appear, 0.42f, alpha);
            activeCombinationImpactText.rectTransform.localScale = Vector3.one * (0.90f + appear * 0.18f + Mathf.Sin(elapsed * 24f) * 0.014f);

            if (activeCombinationHudText != null)
            {
                activeCombinationHudText.gameObject.SetActive(false);
            }
        }

        private void ResetCombatFeedbackState()
        {
            activeCombatFeedbackMessage = string.Empty;
            activeCombatFeedbackSprite = null;
            activeCombatFeedbackColor = Color.white;
            activeCombatFeedbackStartTime = -100f;
            activeCombatFeedbackPriority = 0;
        }

        private void ClearCombatFeedbackOverlay()
        {
            if (combatFeedbackRoot != null)
            {
                Destroy(combatFeedbackRoot.gameObject);
            }

            combatFeedbackRoot = null;
            combatFeedbackEffectImage = null;
            combatFeedbackText = null;
            combatFeedbackGroup = null;
        }

        private void CreateCombatFeedbackOverlay()
        {
            ClearCombatFeedbackOverlay();
            if (phase != GamePhase.Combat || root == null)
            {
                return;
            }

            combatFeedbackRoot = AddPanel(root, "전투 중앙 피드백", new Color(1f, 1f, 1f, 0f));
            combatFeedbackRoot.SetAsLastSibling();
            SetAnchors(combatFeedbackRoot, new Vector2(0.275f, 0.405f), new Vector2(0.655f, 0.550f));

            combatFeedbackGroup = combatFeedbackRoot.gameObject.AddComponent<CanvasGroup>();
            combatFeedbackGroup.alpha = 0f;
            combatFeedbackGroup.blocksRaycasts = false;
            combatFeedbackGroup.interactable = false;

            combatFeedbackEffectImage = AddImage(combatFeedbackRoot, "전투 피드백 임팩트", Color.white);
            combatFeedbackEffectImage.type = Image.Type.Simple;
            combatFeedbackEffectImage.preserveAspect = true;
            combatFeedbackEffectImage.raycastTarget = false;
            SetAnchors(combatFeedbackEffectImage.rectTransform, new Vector2(-0.02f, -0.18f), new Vector2(1.02f, 1.18f));

            combatFeedbackText = AddText(combatFeedbackRoot, "전투 피드백 텍스트", string.Empty, 34, TextAnchor.MiddleCenter, Color.white);
            combatFeedbackText.fontStyle = FontStyle.Bold;
            combatFeedbackText.resizeTextForBestFit = true;
            combatFeedbackText.resizeTextMinSize = 18;
            combatFeedbackText.resizeTextMaxSize = 36;
            combatFeedbackText.raycastTarget = false;
            AddTextGlow(combatFeedbackText, new Color(0f, 0f, 0f, 0.94f), new Color(0.04f, 0.86f, 0.82f, 0.62f), new Vector2(2.1f, -2.4f));
            Outline outline = combatFeedbackText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.63f, 0.20f, 0.70f);
            outline.effectDistance = new Vector2(1.3f, -1.3f);
            SetAnchors(combatFeedbackText.rectTransform, new Vector2(0.205f, 0.170f), new Vector2(0.925f, 0.840f));
            combatFeedbackText.transform.SetAsLastSibling();

            UpdateCombatFeedbackAnimation();
        }

        private void TriggerCombatFeedback(string message, Sprite sprite, Color color, int priority = 1)
        {
            if (phase != GamePhase.Combat || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (IsCombatFeedbackActive()
                && priority < activeCombatFeedbackPriority
                && Time.time - activeCombatFeedbackStartTime < 0.36f)
            {
                return;
            }

            activeCombatFeedbackMessage = message;
            activeCombatFeedbackSprite = sprite;
            activeCombatFeedbackColor = color;
            activeCombatFeedbackStartTime = Time.time;
            activeCombatFeedbackPriority = priority;
            PlayCombatFeedbackSfx(message);
            UpdateCombatFeedbackAnimation();
        }

        private void TriggerCombinationCombatFeedback(string combinationId)
        {
            if (string.IsNullOrEmpty(combinationId))
            {
                return;
            }

            if (combinationId == "trait_oracle_precise_prediction")
            {
                TriggerCombatFeedback("예언 성공", combatFeedbackProphecySprite, new Color(0.42f, 1f, 0.94f, 1f), 3);
                return;
            }

            if (combinationId.StartsWith("trait_", StringComparison.Ordinal)
                || combinationId.StartsWith("hard_trait_", StringComparison.Ordinal))
            {
                TriggerCombatFeedback("특성 발현", combatFeedbackTraitSprite, new Color(0.72f, 1f, 0.88f, 1f), 3);
                return;
            }

            if (combinationId.IndexOf("curse", StringComparison.Ordinal) >= 0
                || combinationId.IndexOf("contract", StringComparison.Ordinal) >= 0
                || combinationId.IndexOf("forbidden", StringComparison.Ordinal) >= 0)
            {
                TriggerCombatFeedback("계약 발현", combatFeedbackCurseSprite, new Color(1f, 0.42f, 0.30f, 1f), 3);
                return;
            }

            TriggerCombatFeedback("조합 발현", combatFeedbackComboSprite, new Color(0.72f, 1f, 0.92f, 1f), 3);
        }

        private void QueueEnemyRevealCombinationImpact(string combinationId)
        {
            if (!string.IsNullOrEmpty(combinationId))
            {
                pendingEnemyRevealCombinationImpacts.Add(combinationId);
            }
        }

        private void PlayPendingEnemyRevealCombinationImpacts()
        {
            if (pendingEnemyRevealCombinationImpacts.Count == 0)
            {
                return;
            }

            List<string> queuedImpacts = new(pendingEnemyRevealCombinationImpacts);
            pendingEnemyRevealCombinationImpacts.Clear();
            if (phase != GamePhase.Combat)
            {
                return;
            }

            foreach (string combinationId in queuedImpacts)
            {
                TriggerCombinationImpact(combinationId);
            }
        }

        private bool IsCombatFeedbackActive()
        {
            return !string.IsNullOrEmpty(activeCombatFeedbackMessage)
                && Time.time - activeCombatFeedbackStartTime <= CombatFeedbackSeconds;
        }

        private void UpdateCombatFeedbackAnimation()
        {
            if (phase != GamePhase.Combat)
            {
                return;
            }

            if (combatFeedbackRoot == null)
            {
                return;
            }

            bool active = IsCombatFeedbackActive();
            if (!active)
            {
                if (combatFeedbackEffectImage != null)
                {
                    combatFeedbackEffectImage.gameObject.SetActive(false);
                }

                if (combatFeedbackText != null)
                {
                    combatFeedbackText.gameObject.SetActive(false);
                }

                if (!IsCombatFeedbackActive())
                {
                    activeCombatFeedbackMessage = string.Empty;
                    activeCombatFeedbackSprite = null;
                    activeCombatFeedbackPriority = 0;
                }

                combatFeedbackRoot.localScale = Vector3.one;
                if (combatFeedbackGroup != null)
                {
                    combatFeedbackGroup.alpha = 0f;
                }

                return;
            }

            float elapsed = Time.time - activeCombatFeedbackStartTime;
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.16f));
            float disappear = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.82f) / 0.32f));
            float alpha = appear * disappear;
            float pulse = Mathf.Sin(elapsed * 25f) * 0.012f;
            combatFeedbackRoot.localScale = Vector3.one * (0.965f + appear * 0.055f + pulse);
            if (combatFeedbackGroup != null)
            {
                combatFeedbackGroup.alpha = Mathf.Clamp01(alpha);
            }

            if (combatFeedbackEffectImage != null)
            {
                combatFeedbackEffectImage.gameObject.SetActive(activeCombatFeedbackSprite != null);
                combatFeedbackEffectImage.sprite = activeCombatFeedbackSprite;
                combatFeedbackEffectImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 0.94f));
            }

            if (combatFeedbackText != null)
            {
                combatFeedbackText.gameObject.SetActive(true);
                combatFeedbackText.text = activeCombatFeedbackMessage;
                combatFeedbackText.color = new Color(
                    activeCombatFeedbackColor.r,
                    activeCombatFeedbackColor.g,
                    activeCombatFeedbackColor.b,
                    alpha);
            }
        }

        private void CreateDeckWidget()
        {
            RectTransform deckPanel = AddPanel(contentRoot, "덱 더미", new Color(1f, 1f, 1f, 0.86f), deckBoxFrameSprite);
            Image deckPanelImage = deckPanel.GetComponent<Image>();
            deckPanelImage.type = Image.Type.Simple;
            deckPanelImage.color = Color.white;
            deckPanelImage.raycastTarget = false;
            SetAnchors(deckPanel, new Vector2(0.000f, 0.015f), new Vector2(0.135f, 0.625f));

            if (cardBackSprite != null)
            {
                Image cardShadow = AddImage(deckPanel, "덱 카드 받침", new Color(0f, 0f, 0f, 0.78f));
                cardShadow.sprite = cardBackSprite;
                cardShadow.preserveAspect = true;
                cardShadow.raycastTarget = false;
                SetAnchors(cardShadow.rectTransform, new Vector2(0.200f, 0.410f), new Vector2(0.800f, 0.792f));

                Image cardGlow = AddImage(deckPanel, "덱 카드 청록 보강", new Color(0.08f, 0.92f, 0.88f, 0.32f));
                cardGlow.sprite = cardBackSprite;
                cardGlow.preserveAspect = true;
                cardGlow.raycastTarget = false;
                SetAnchors(cardGlow.rectTransform, new Vector2(0.215f, 0.425f), new Vector2(0.785f, 0.785f));
            }

            for (int i = 0; i < 3; i += 1)
            {
                float offset = (i - 1) * 0.008f;
                Vector2 cardMin = new Vector2(0.220f + offset, 0.430f + offset);
                Vector2 cardMax = new Vector2(0.780f + offset, 0.785f + offset);

                Image backBase = AddImage(deckPanel, $"카드 뒷면 선명도 보강 {i}", new Color(0.86f, 0.92f, 0.92f, 1f));
                backBase.sprite = cardBackSprite;
                backBase.preserveAspect = true;
                backBase.raycastTarget = false;
                backBase.color = new Color(1.18f, 1.20f, 1.12f, 1f);
                SetAnchors(backBase.rectTransform, cardMin, cardMax);

                Image back = AddImage(deckPanel, $"카드 뒷면 {i}", new Color(1.08f, 1.08f, 1.08f, 1f));
                back.sprite = cardBackSprite;
                back.preserveAspect = true;
                back.raycastTarget = false;
                back.color = new Color(1.34f, 1.32f, 1.18f, 1f);
                back.canvasRenderer.SetAlpha(1f);
                SetAnchors(back.rectTransform, cardMin, cardMax);
            }

            Text deckText = AddText(deckPanel, "덱 수", $"덱 {drawPile.Count}", 15, TextAnchor.MiddleCenter, Color.white);
            deckText.fontStyle = FontStyle.Bold;
            deckText.resizeTextMinSize = 11;
            AddTextGlow(deckText, new Color(0f, 0f, 0f, 0.86f), new Color(0f, 0f, 0f, 0.55f), new Vector2(1.2f, -1.2f));
            SetAnchors(deckText.rectTransform, new Vector2(0.255f, 0.212f), new Vector2(0.745f, 0.280f));

            Text discardText = AddText(deckPanel, "버림 수", $"버림 {discardPile.Count}", 15, TextAnchor.MiddleCenter, new Color(0.86f, 0.82f, 0.72f, 1f));
            discardText.fontStyle = FontStyle.Bold;
            discardText.resizeTextMinSize = 11;
            AddTextGlow(discardText, new Color(0f, 0f, 0f, 0.86f), new Color(0f, 0f, 0f, 0.50f), new Vector2(1.2f, -1.2f));
            SetAnchors(discardText.rectTransform, new Vector2(0.255f, 0.108f), new Vector2(0.745f, 0.176f));

            CreateCombatDiceHud();
        }

        private void CreateCombatDiceHud()
        {
            Sprite diceHudSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : statusHintFrameSprite != null
                        ? statusHintFrameSprite
                        : buttonIdleSprite;
            combatDiceHudRoot = AddPanel(contentRoot, "전투 행운 주사위 박스", Color.white, diceHudSprite);
            Image rootImage = combatDiceHudRoot.GetComponent<Image>();
            rootImage.type = Image.Type.Simple;
            rootImage.color = new Color(1.10f, 1.08f, 1.02f, 1f);
            rootImage.raycastTarget = false;
            SetAnchors(combatDiceHudRoot, new Vector2(0.000f, -0.060f), new Vector2(0.135f, 0.010f));

            combatDiceImage = AddImage(combatDiceHudRoot, "전투 행운 주사위", Color.white);
            combatDiceImage.preserveAspect = true;
            combatDiceImage.raycastTarget = false;
            SetAnchors(combatDiceImage.rectTransform, new Vector2(0.130f, 0.220f), new Vector2(0.365f, 0.780f));

            combatDiceText = AddText(combatDiceHudRoot, "전투 행운 텍스트", $"행운 {luck}", 18, TextAnchor.MiddleCenter, new Color(0.70f, 1.0f, 0.90f, 1f));
            combatDiceText.fontStyle = FontStyle.Bold;
            combatDiceText.alignByGeometry = true;
            combatDiceText.resizeTextForBestFit = true;
            combatDiceText.resizeTextMinSize = 12;
            combatDiceText.resizeTextMaxSize = 18;
            AddTextGlow(combatDiceText, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.42f), new Vector2(1.2f, -1.4f));
            SetAnchors(combatDiceText.rectTransform, new Vector2(0.380f, 0.230f), new Vector2(0.870f, 0.770f));
        }

        private void CreateDrawAnimationGhosts()
        {
            if (pendingDrawAnimationCount <= 0 || cardBackSprite == null)
            {
                pendingDrawAnimationCount = 0;
                return;
            }

            int ghostCount = Mathf.Min(5, pendingDrawAnimationCount);
            for (int i = 0; i < ghostCount; i += 1)
            {
                Image ghost = AddImage(contentRoot, $"뽑는 카드 {i}", Color.white);
                ghost.sprite = cardBackSprite;
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;

                RectTransform rect = ghost.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(150f, 225f);

                DrawCardAnimator animator = ghost.gameObject.AddComponent<DrawCardAnimator>();
                animator.Configure(
                    new Vector2(100f + i * 8f, 205f + i * 5f),
                    new Vector2(330f + i * 116f, 190f + i * 5f),
                    0.82f,
                    i * 0.10f);
            }

            pendingDrawAnimationCount = 0;
        }

        private static void ApplyHandFanLayout(RectTransform cardPanel, int index, int count)
        {
            int safeCount = Mathf.Max(1, count);
            float middle = (safeCount - 1) * 0.5f;
            float fan = middle <= 0f ? 0f : (index - middle) / middle;
            float absFan = Mathf.Abs(fan);

            float width = safeCount <= 5
                ? 0.160f
                : Mathf.Clamp(0.770f / safeCount, 0.105f, 0.148f);
            float spread = safeCount <= 1
                ? 0f
                : Mathf.Clamp(0.132f * (safeCount - 1), 0.0f, 0.620f);
            float centerX = 0.5f + (middle <= 0f ? 0f : fan * spread * 0.5f);
            centerX = Mathf.Clamp(centerX, width * 0.5f + 0.015f, 0.985f - width * 0.5f);

            float bottom = -0.048f + (1f - absFan) * 0.052f - absFan * 0.030f;
            float height = 0.790f;
            SetAnchors(
                cardPanel,
                new Vector2(centerX - width * 0.5f, bottom),
                new Vector2(centerX + width * 0.5f, bottom + height));

            cardPanel.pivot = new Vector2(0.5f, 0.05f);
            cardPanel.localEulerAngles = new Vector3(0f, 0f, -fan * 8.0f);
        }

        private Button CreateCardButton(RectTransform parent, CardData card, int index, int count, bool useFanLayout = false, bool enablePreview = true)
        {
            RectTransform cardPanel = AddPanel(parent, $"카드 {index}", Color.white);
            if (useFanLayout)
            {
                ApplyHandFanLayout(cardPanel, index, count);
            }
            else
            {
                float width = count <= 5
                    ? 0.168f
                    : Mathf.Min(0.140f, 0.98f / Mathf.Max(1, count) - 0.014f);
                float gap = count <= 1 ? 0f : (0.992f - width * count) / (count - 1);
                float start = count <= 1 ? 0.5f - width * 0.5f : 0.004f + index * (width + gap);
                SetAnchors(cardPanel, new Vector2(start, -0.030f), new Vector2(start + width, 0.855f));
            }

            HoverFloatAnimator hoverAnimator = cardPanel.gameObject.AddComponent<HoverFloatAnimator>();
            hoverAnimator.Configure(useFanLayout ? 1.070f : 1.035f, 0.985f, useFanLayout ? 18f : 10f, 1.0f, 0.08f);

            Image frame = cardPanel.GetComponent<Image>();
            if (card.FullCardSprite != null)
            {
                frame.sprite = card.FullCardSprite;
                frame.type = Image.Type.Simple;
                frame.color = Color.white;
                frame.preserveAspect = true;

                Button fullCardButton = cardPanel.gameObject.AddComponent<Button>();
                fullCardButton.targetGraphic = frame;
                fullCardButton.colors = CreateStaticButtonColors();
                if (enablePreview)
                {
                    AddCardPreviewHandlers(cardPanel.gameObject, card.FullCardSprite);
                }

                return fullCardButton;
            }

            frame.sprite = GetCardFrameSprite(card.Category);
            frame.type = Image.Type.Simple;
            frame.color = Color.white;

            Image art = AddImage(cardPanel, "일러스트", Color.white);
            art.sprite = card.Illustration != null ? card.Illustration : cardBackSprite;
            art.preserveAspect = true;
            SetAnchors(art.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.88f));

            Text cost = AddText(cardPanel, "비용", card.Cost.ToString(), 22, TextAnchor.MiddleCenter, Color.white);
            cost.fontStyle = FontStyle.Bold;
            cost.resizeTextMinSize = 18;
            cost.horizontalOverflow = HorizontalWrapMode.Overflow;
            cost.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextGlow(cost, new Color(0f, 0f, 0f, 0.78f), new Color(0f, 0f, 0f, 0.55f), new Vector2(1.1f, -1.1f));
            SetAnchors(cost.rectTransform, new Vector2(0.035f, 0.825f), new Vector2(0.155f, 0.93f));

            Text name = AddText(cardPanel, "카드명", card.DisplayName, 19, TextAnchor.MiddleCenter, Color.white);
            name.fontStyle = FontStyle.Bold;
            SetAnchors(name.rectTransform, new Vector2(0.10f, 0.31f), new Vector2(0.90f, 0.40f));

            Text rules = AddText(cardPanel, "효과", card.RulesText, 13, TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.80f, 1f));
            rules.lineSpacing = 0.88f;
            rules.resizeTextMinSize = 8;
            rules.alignByGeometry = false;
            SetAnchors(rules.rectTransform, new Vector2(0.115f, 0.085f), new Vector2(0.885f, 0.205f));

            Button button = cardPanel.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.colors = CreateStaticButtonColors();
            return button;
        }

        private void AddCardPreviewHandlers(GameObject target, Sprite sprite)
        {
            EventTrigger trigger = target.AddComponent<EventTrigger>();
            RectTransform previewTarget = target.GetComponent<RectTransform>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowCardPreview(sprite, previewTarget));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => HideCardPreview());
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new()
            {
                eventID = eventType
            };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private void ShowCardPreview(Sprite sprite, RectTransform previewTarget)
        {
            if (sprite == null || phase == GamePhase.GameOver)
            {
                return;
            }

            if (cardPreviewImage == null)
            {
                cardPreviewImage = AddImage(root, "카드 확대 프리뷰", Color.white);
                cardPreviewImage.preserveAspect = true;
                cardPreviewImage.raycastTarget = false;
                SetAnchors(cardPreviewImage.rectTransform, new Vector2(0.390f, 0.265f), new Vector2(0.610f, 0.850f));
            }

            cardPreviewImage.sprite = sprite;
            cardPreviewTarget = previewTarget;
            cardPreviewImage.gameObject.SetActive(true);
            cardPreviewImage.transform.SetAsLastSibling();
        }

        private void UpdateCardPreviewVisibility()
        {
            if (cardPreviewImage == null || !cardPreviewImage.gameObject.activeSelf || cardPreviewTarget == null)
            {
                return;
            }

            if (!TryGetPointerScreenPosition(out Vector2 pointerPosition)
                || !RectTransformUtility.RectangleContainsScreenPoint(cardPreviewTarget, pointerPosition, null))
            {
                HideCardPreview();
            }
        }

        private static bool TryGetPointerScreenPosition(out Vector2 pointerPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }

            pointerPosition = default;
            return false;
#else
            pointerPosition = Input.mousePosition;
            return true;
#endif
        }

        private void HideCardPreview()
        {
            if (cardPreviewImage != null)
            {
                cardPreviewImage.gameObject.SetActive(false);
            }

            cardPreviewTarget = null;
        }

        private bool CanPlay(CardData card)
        {
            return phase == GamePhase.Combat
                && !combatVictorySequenceActive
                && GetEffectiveCardCost(card) <= action
                && !(card.OncePerCombat && oncePerCombatUsed.Contains(card.CardId));
        }

        private int GetEffectiveCardCost(CardData card)
        {
            if (card == null)
            {
                return 0;
            }

            int cost = GetCardCostBeforeTrait(card);
            int traitReduction = GetTraitCostReduction(cost);
            int costAfterTrait = Mathf.Max(0, cost - traitReduction);
            return Mathf.Max(0, costAfterTrait - GetRunItemCostReduction(card, costAfterTrait));
        }

        private int GetCardCostBeforeTrait(CardData card)
        {
            int cost = card.Cost;
            if (card.CardId == "card_heavy_blow"
                && IsCombinationComplete("heavy_pressure")
                && (cardsPlayedThisTurn.Contains("card_endure") || cardsPlayedThisTurn.Contains("card_fix_fate")))
            {
                cost -= 1;
            }

            return Mathf.Max(0, cost);
        }

        private int GetTraitCostReduction(int currentCost)
        {
            if (selectedClass != CharacterClass.Oracle || oracleNextCardCostReduction <= 0 || currentCost <= 0)
            {
                return 0;
            }

            return Mathf.Min(oracleNextCardCostReduction, currentCost);
        }

        private int GetRunItemCostReduction(CardData card, int currentCost)
        {
            if (card == null
                || currentCost <= 0
                || card.Category != CardCategory.Skill
                || runItemSkillDiscountsRemaining <= 0
                || !HasRunItem("blessing_silver_feather"))
            {
                return 0;
            }

            return 1;
        }

        private void PlayCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                return;
            }

            CardData card = hand[handIndex];
            if (!CanPlay(card))
            {
                AddLog($"{card.DisplayName}은 지금 사용할 수 없습니다.");
                RenderCombat();
                return;
            }

            int costBeforeTrait = GetCardCostBeforeTrait(card);
            int traitCostReduction = GetTraitCostReduction(costBeforeTrait);
            int costAfterTrait = Mathf.Max(0, costBeforeTrait - traitCostReduction);
            int runItemCostReduction = GetRunItemCostReduction(card, costAfterTrait);
            int effectiveCost = Mathf.Max(0, costAfterTrait - runItemCostReduction);
            action -= effectiveCost;
            if (card.OncePerCombat)
            {
                oncePerCombatUsed.Add(card.CardId);
            }

            if (costBeforeTrait < card.Cost)
            {
                TriggerCombinationImpact("heavy_pressure");
                AddLog($"{card.DisplayName} 비용 {card.Cost}→{costBeforeTrait}.");
            }

            if (traitCostReduction > 0)
            {
                oracleNextCardCostReduction = 0;
                TriggerCombinationImpact("trait_oracle_precise_prediction");
                AddLog($"특성 발동: 정확한 예언. {card.DisplayName} 비용 -{traitCostReduction}.");
            }

            AddLog($"{card.DisplayName} 사용.");
            if (runItemCostReduction > 0)
            {
                runItemSkillDiscountsRemaining = Mathf.Max(0, runItemSkillDiscountsRemaining - 1);
                AddLog($"Run item: {card.DisplayName} cost -{runItemCostReduction}.");
            }

            RecordOracleDefenseResponse(card);
            activeCard = card;
            activeCardHandIndex = handIndex;
            activeCardDamageBonusApplied = false;
            activeCardBlockBonusApplied = false;
            activeCardRunItemDamageBonusApplied = false;
            activeCardRunItemBlockBonusApplied = false;
            int debtBeforePlay = debt;
            foreach (CardEffectDefinition effect in card.Effects)
            {
                ApplyEffect(effect);
                if (phase == GamePhase.GameOver)
                {
                    activeCard = null;
                    activeCardHandIndex = -1;
                    return;
                }
            }
            activeCard = null;
            int playedCardIndex = activeCardHandIndex;
            activeCardHandIndex = -1;

            if (playedCardIndex >= 0 && playedCardIndex < hand.Count && hand[playedCardIndex] == card)
            {
                hand.RemoveAt(playedCardIndex);
            }
            else
            {
                hand.Remove(card);
            }
            discardPile.Add(card);
            cardsPlayedThisTurn.Add(card.CardId);
            cardsPlayedThisCombat.Add(card.CardId);
            ApplyPostPlayCombinationEffects(card, debtBeforePlay);
            ApplyPostPlayRunItemEffects(card);
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            TryTriggerOracleHardLowHandDraw();
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            if (enemy.Health <= 0)
            {
                StartCombatVictorySequence();
                return;
            }

            if (TryLoseFromEmptyCombatDeck())
            {
                return;
            }

            RenderCombat();
        }

        private void RecordOracleDefenseResponse(CardData card)
        {
            if (selectedClass != CharacterClass.Oracle
                || oraclePrecisePredictionAwakened
                || card == null
                || card.Category != CardCategory.Defense
                || enemy == null
                || enemy.IntentAttack <= 0)
            {
                return;
            }

            oracleAttackDefenseResponses += 1;
            if (oracleAttackDefenseResponses < OraclePredictionAwakenThreshold)
            {
                return;
            }

            oraclePrecisePredictionAwakened = true;
            int buildLevel = GetBuildUpgradeLevel(GetBuildRecipe(CharacterClass.Oracle).Id);
            oracleNextCardCostReduction = IsBuildUnlocked(GetBuildRecipe(CharacterClass.Oracle)) && buildLevel >= 1 ? 2 : 1;
            TriggerCombinationImpact("trait_oracle_precise_prediction");
            AddLog($"특성 발동: 정확한 예언. 다음 카드 비용 -{oracleNextCardCostReduction}.");
        }

        private void RecordGamblerCardFlow(int count)
        {
            if (selectedClass != CharacterClass.Gambler || gamblerCardReadingAwakened || phase != GamePhase.Combat || count <= 0)
            {
                return;
            }

            combatDrawDiscardCount += count;
            if (combatDrawDiscardCount < GamblerCardFlowAwakenThreshold)
            {
                return;
            }

            BuildRecipe recipe = GetBuildRecipe(CharacterClass.Gambler);
            int extraRolls = IsBuildUnlocked(recipe) && GetBuildUpgradeLevel(recipe.Id) >= 2 ? 1 : 0;
            gamblerCardReadingAwakened = true;
            gamblerLoadedDiceRollsRemaining = 3 + extraRolls;
            TriggerCombinationImpact("trait_gambler_card_reading");
            AddLog($"특성 발동: 패 읽기. 다음 행운 주사위 {gamblerLoadedDiceRollsRemaining}회가 고점으로 기웁니다.");
        }

        private void RecordExileCurseRemoval(int removedDebt)
        {
            if (selectedClass != CharacterClass.Exile || phase != GamePhase.Combat || removedDebt <= 0)
            {
                return;
            }

            if (IsHardModeFeatureActive())
            {
                playerBlock += 6 + GetHardTraitEndlessBonus();
                TriggerCombinationImpact("hard_trait_exile_endless_atonement");
                AddLog("Hard trait: debt clear granted block.");
            }

            if (IsCombinationComplete("debt_reversal"))
            {
                pendingCombinationDamageBonus += 8;
                pendingCombinationDamageBonusSourceId = "debt_reversal";
                if (combinationTriggersThisCombat.Add("debt_reversal_action"))
                {
                    action += 1;
                }

                TriggerCombinationImpact("debt_reversal");
                AddLog("Combination: debt reversal primed next attack.");
            }

            if (IsCombinationComplete("no_return_path"))
            {
                playerBlock += 8;
                Heal(4);
                TriggerCombinationImpact("no_return_path");
                AddLog("Combination: no return path restored you.");
            }

            if (exileCurseEaterAwakened)
            {
                return;
            }

            exileCurseRemovalsThisCombat += removedDebt;
            if (exileCurseRemovalsThisCombat < ExileCurseRemovalAwakenThreshold)
            {
                return;
            }

            BuildRecipe recipe = GetBuildRecipe(CharacterClass.Exile);
            int level = GetBuildUpgradeLevel(recipe.Id);
            exileCurseEaterAwakened = true;
            exileNextAttackDamageBonus = IsBuildUnlocked(recipe) ? 6 + level * 2 : 5;
            exileNextAttackVulnerableBonus = 1;
            TriggerCombinationImpact("trait_exile_curse_eater");
            AddLog($"특성 발동: 저주 삼키기. 다음 공격 피해 +{exileNextAttackDamageBonus}, 취약 +1.");
        }

        private void TryTriggerExileWoundOath()
        {
            if (selectedClass != CharacterClass.Exile
                || exileWoundOathTriggeredThisCombat
                || phase != GamePhase.Combat
                || playerHealth <= 0
                || playerHealth * 100 > playerMaxHealth * 35)
            {
                return;
            }

            BuildRecipe recipe = GetBuildRecipe(CharacterClass.Exile);
            int level = GetBuildUpgradeLevel(recipe.Id);
            int block = IsBuildUnlocked(recipe) ? 12 + level * 3 : 8;
            exileWoundOathTriggeredThisCombat = true;
            playerBlock += block;
            TriggerCombinationImpact("trait_exile_wound_oath");
            AddLog($"특성 발동: 상처의 맹세. 방어도 +{block}.");
        }

        private void TryTriggerRiftSurvival()
        {
            if (phase != GamePhase.Combat
                || playerHealth <= 0
                || playerHealth * 2 > playerMaxHealth
                || !IsCombinationComplete("rift_survival")
                || !combinationTriggersThisCombat.Add("rift_survival"))
            {
                return;
            }

            playerBlock += 10;
            Heal(5);
            TriggerCombinationImpact("rift_survival");
            AddLog("Combination: rift survival restored you.");
        }

        private void ApplyEffect(CardEffectDefinition effect)
        {
            if (!ConditionMet(effect))
            {
                return;
            }

            switch (effect.EffectType)
            {
                case CardEffectType.DealDamage:
                case CardEffectType.ConditionalBonusDamage:
                    DealDamage(effect.Amount);
                    break;
                case CardEffectType.RepeatDamageByLuck:
                    int repetitions = luck + GetCombinationRepeatDamageBonus();
                    for (int i = 0; i < repetitions; i += 1)
                    {
                        DealDamage(effect.Amount);
                    }
                    break;
                case CardEffectType.GainBlock:
                case CardEffectType.ConditionalBonusBlock:
                    GainBlock(effect.Amount);
                    break;
                case CardEffectType.Heal:
                    Heal(effect.Amount);
                    break;
                case CardEffectType.DrawCards:
                    DrawCards(effect.Amount);
                    AddLog($"카드 {effect.Amount}장 뽑기.");
                    break;
                case CardEffectType.DiscardCards:
                    DiscardRandom(effect.Amount);
                    break;
                case CardEffectType.RerollLuck:
                    luck = RollLuck();
                    AddLog($"행운을 다시 굴려 {luck}.");
                    break;
                case CardEffectType.KeepLuckNextTurn:
                    keepLuckNextTurn = true;
                    AddLog("다음 턴에도 현재 행운을 유지합니다.");
                    break;
                case CardEffectType.GainAction:
                    action += effect.Amount;
                    AddLog($"행동력 +{effect.Amount}.");
                    break;
                case CardEffectType.LoseHealth:
                    LoseHealth(effect.Amount, true);
                    break;
                case CardEffectType.ApplyBleed:
                    enemy.Bleed += effect.Amount;
                    AddLog($"출혈 +{effect.Amount}.");
                    break;
                case CardEffectType.ApplyVulnerable:
                    enemy.Vulnerable += effect.Amount;
                    AddLog($"취약 +{effect.Amount}.");
                    break;
                case CardEffectType.ReduceNextDamage:
                    pendingDamageReduction += effect.Amount;
                    AddLog($"Next damage -{effect.Amount}.");
                    break;
                case CardEffectType.RevealDoorEffect:
                    AddDoorInsight(effect.Amount + (HasRunItem("curse_watchers_eye") ? 1 : 0));
                    PrimeHardProphecy();
                    TriggerCombatFeedback("예언 성공", combatFeedbackProphecySprite, new Color(0.42f, 1f, 0.94f, 1f), 3);
                    AddLog(GetDoorInsightLevel() >= 3
                        ? "균열이 완전히 열려 다음 문 예언이 선명해집니다."
                        : "다음 문 예언이 더 선명해집니다.");
                    ApplyRunItemProphecyUsed();
                    break;
                case CardEffectType.GainGold:
                    GainGold(effect.Amount);
                    break;
                case CardEffectType.AddCurse:
                    int addedDebt = GetRunItemReducedDebtGain(effect.Amount);
                    debt += addedDebt;
                    if (addedDebt <= 0)
                    {
                        AddLog("Debt +0.");
                        break;
                    }
                    TriggerCombatFeedback("계약 발현", combatFeedbackCurseSprite, new Color(1f, 0.42f, 0.30f, 1f), 3);
                    AddLog($"빚 +{effect.Amount}.");
                    break;
                case CardEffectType.RemoveCurse:
                    int debtBeforeRemoval = debt;
                    debt = Mathf.Max(0, debt - effect.Amount);
                    int removedDebt = debtBeforeRemoval - debt;
                    AddLog(removedDebt > 0 ? $"빚 -{removedDebt}." : "제거할 빚이 없습니다.");
                    RecordExileCurseRemoval(removedDebt);
                    ApplyRunItemDebtReduced(removedDebt);
                    if (removedDebt <= 0)
                    {
                        TryTriggerExileHardDebtlessCleanse();
                    }
                    break;
                case CardEffectType.ReflectDamage:
                    reflectedDamage += effect.Amount;
                    AddLog($"반사 피해 {effect.Amount} 준비.");
                    break;
                case CardEffectType.PreventDeathThisTurn:
                    preventDeathThisTurn = true;
                    AddLog("이번 턴 죽음을 1회 버팁니다.");
                    break;
                case CardEffectType.ChangeLuck:
                    luck = Mathf.Clamp(effect.Amount, 1, 6);
                    AddLog($"행운이 {luck}으로 고정됩니다.");
                    break;
                case CardEffectType.StoreLuck:
                    storedLuck = luck;
                    hasStoredLuck = true;
                    AddLog($"행운 {storedLuck} 저장.");
                    break;
                case CardEffectType.ReduceCurseDamage:
                    curseReduction += effect.Amount;
                    AddLog($"빚 저항 +{effect.Amount}.");
                    break;
                case CardEffectType.RetainBlockNextTurn:
                    retainBlockNextTurn = true;
                    AddLog("다음 턴까지 방어도를 유지합니다.");
                    break;
                default:
                    AddLog($"{effect.EffectType} 효과는 이후 확장 예정입니다.");
                    break;
            }
        }

        private bool ConditionMet(CardEffectDefinition effect)
        {
            return effect.Condition switch
            {
                CardConditionType.None => true,
                CardConditionType.LuckAtLeast => luck >= effect.LuckThreshold,
                CardConditionType.LuckAtMost => luck <= effect.LuckThreshold,
                CardConditionType.LuckIsOdd => luck % 2 == 1,
                CardConditionType.EnemyIntentIsAttack => enemy != null && enemy.IntentAttack > 0,
                CardConditionType.PlayerHealthAtOrBelowPercent => playerHealth * 100 <= playerMaxHealth * effect.PercentThreshold,
                CardConditionType.EnemyHealthAtOrBelowPercent => enemy != null && enemy.Health * 100 <= enemy.MaxHealth * effect.PercentThreshold,
                CardConditionType.EnemyHasBlock => enemy != null && enemy.Block > 0,
                CardConditionType.EnemyHasBleed => enemy != null && enemy.Bleed > 0,
                CardConditionType.PlayerHasDebt => debt > 0,
                CardConditionType.OncePerCombat => true,
                _ => true
            };
        }

        private void GainGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gold += amount;
            AddLog($"Gold +{amount}.");
            if (phase == GamePhase.Combat && selectedClass == CharacterClass.Gambler && IsHardModeFeatureActive())
            {
                gamblerHardGoldGainedThisCombat += amount;
                if (!gamblerHardGoldSpikeTriggeredThisCombat && gamblerHardGoldGainedThisCombat >= 20)
                {
                    gamblerHardGoldSpikeTriggeredThisCombat = true;
                    int bonus = 6 + GetHardTraitEndlessBonus();
                    pendingCombinationDamageBonus += bonus;
                    pendingCombinationDamageBonusSourceId = "hard_trait_gambler_ruin_wager";
                    TriggerCombinationImpact("hard_trait_gambler_ruin_wager");
                    AddLog($"Hard trait: next attack damage +{bonus}.");
                }
            }
        }

        private void PrimeHardProphecy()
        {
            if (!IsHardModeFeatureActive() || selectedClass != CharacterClass.Oracle)
            {
                return;
            }

            oracleHardProphecyPrimed = true;
            oracleNextCardCostReduction = Mathf.Max(oracleNextCardCostReduction, 1);
            TriggerCombinationImpact("hard_trait_oracle_closed_fate");
            AddLog("Hard trait: next card cost -1.");
        }

        private void TryTriggerExileHardDebtlessCleanse()
        {
            if (!IsHardModeFeatureActive() || selectedClass != CharacterClass.Exile || phase != GamePhase.Combat || debt > 0)
            {
                return;
            }

            Heal(5 + GetHardTraitEndlessBonus());
            TriggerCombinationImpact("hard_trait_exile_endless_atonement");
            AddLog("Hard trait: debtless cleanse restored health.");
        }

        private void TryTriggerOracleHardLowHandDraw()
        {
            if (!IsHardModeFeatureActive()
                || selectedClass != CharacterClass.Oracle
                || oracleHardLowHandDrawTriggeredThisCombat
                || phase != GamePhase.Combat
                || hand.Count > 2)
            {
                return;
            }

            oracleHardLowHandDrawTriggeredThisCombat = true;
            DrawCards(1);
            TriggerCombinationImpact("hard_trait_oracle_closed_fate");
            AddLog("Hard trait: low hand drew 1 card.");
        }

        private int GetHardTraitEndlessBonus()
        {
            return endlessModeActive ? Mathf.Clamp(Mathf.Max(0, roomsCleared - TargetRooms) / 10, 0, 4) : 0;
        }

        private void ApplyBuildCombatStartBonuses()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (!IsBuildUnlocked(recipe) || recipe.Id != "oracle_rift_engine" || oracleBuildTriggeredThisCombat)
            {
                return;
            }

            oracleBuildTriggeredThisCombat = true;
            int level = GetBuildUpgradeLevel(recipe.Id);
            int extraDraw = 1 + level;
            DrawCards(extraDraw);
            AddDoorInsight(1);
            if (level >= 2)
            {
                action += 1;
            }

            AddLog(level >= 2
                ? $"{recipe.Name}: 카드 {extraDraw}장 추가, 행동력 +1."
                : $"{recipe.Name}: 카드 {extraDraw}장 추가.");
        }

        private void ApplyCombinationCombatStartBonuses()
        {
            if (IsCombinationComplete("old_gear_mastery"))
            {
                playerBlock += 3;
                combinationTriggersThisCombat.Add("old_gear_mastery_start");
                QueueEnemyRevealCombinationImpact("old_gear_mastery");
                AddLog("조합 발동: 낡은 장비 숙련. 전투 시작 방어도 +3.");
            }
        }

        private void ApplyRunItemCombatStartBonuses()
        {
            if (HasRunItem("blessing_star_seal"))
            {
                playerBlock += 6;
                AddLog("Run item: start block +6.");
            }

            if (HasRunItem("relic_gate_knocker") && enemy != null && (enemy.WasElite || enemy.IsBoss))
            {
                playerBlock += 10;
                AddLog("Run item: elite/boss start block +10.");
            }

            if (HasRunItem("relic_black_candle"))
            {
                action += 1;
                LoseHealth(2, true);
                AddLog("Run item: start action +1, health -2.");
                if (phase == GamePhase.GameOver)
                {
                    return;
                }
            }

            if (HasRunItem("curse_red_contract_seal"))
            {
                action += 1;
                int addedDebt = GetRunItemReducedDebtGain(1);
                debt += addedDebt;
                AddLog($"Run item: start action +1, debt +{addedDebt}.");
            }

            if (HasRunItem("curse_rusted_shackle"))
            {
                action = Mathf.Max(0, action - 1);
                AddLog("Run item: first turn action -1.");
            }

            if (HasRunItem("relic_teal_hourglass"))
            {
                DrawCards(1);
                AddLog("Run item: first turn draw +1.");
            }

            if (HasRunItem("blessing_sacred_flame"))
            {
                DrawCards(1);
                AddLog("Run item: first turn draw +1.");
            }

            if (HasRunItem("curse_blood_candle"))
            {
                LoseHealth(3, true);
                if (phase == GamePhase.GameOver)
                {
                    return;
                }

                DrawCards(2);
                AddLog("Run item: health -3, first turn draw +2.");
            }

            ApplyRunItemTurnStartBonuses();
        }

        private void ApplyRunItemTurnStartBonuses()
        {
            if (phase != GamePhase.Combat)
            {
                return;
            }

            if (HasRunItem("relic_exile_brand") && playerHealth * 2 <= playerMaxHealth)
            {
                playerBlock += 4;
                AddLog("Run item: low health block +4.");
            }

            TryTriggerRunItemLowHealthBlock();
        }

        private void TryTriggerRunItemLowHealthBlock()
        {
            if (phase != GamePhase.Combat
                || !HasRunItem("blessing_lion_ward")
                || playerHealth * 100 > playerMaxHealth * 30
                || !runItemTriggersThisCombat.Add("blessing_lion_ward"))
            {
                return;
            }

            playerBlock += 18;
            AddLog("Run item: emergency block +18.");
        }

        private void ApplyPostPlayRunItemEffects(CardData card)
        {
            if (card == null)
            {
                return;
            }

            if (card.Category == CardCategory.Attack && HasRunItem("curse_thorn_crown"))
            {
                LoseHealth(1, true);
                AddLog("Run item: attack health -1.");
            }
        }

        private void ApplyRunItemProphecyUsed()
        {
            if (phase != GamePhase.Combat
                || !HasRunItem("relic_oracle_crystal")
                || !runItemTriggersThisCombat.Add("relic_oracle_crystal"))
            {
                return;
            }

            DrawCards(1);
            AddLog("Run item: prophecy draw +1.");
        }

        private void ApplyRunItemDebtReduced(int amount)
        {
            if (amount <= 0 || phase != GamePhase.Combat || !HasRunItem("relic_broken_chain"))
            {
                return;
            }

            playerBlock += 5;
            AddLog("Run item: debt reduced, block +5.");
        }

        private int GetRunItemReducedDebtGain(int amount)
        {
            int addedDebt = Mathf.Max(0, amount);
            if (phase == GamePhase.Combat
                && addedDebt > 0
                && HasRunItem("blessing_purified_chain")
                && runItemTriggersThisCombat.Add("blessing_purified_chain"))
            {
                addedDebt = Mathf.Max(0, addedDebt - 1);
                AddLog("Run item: debt gain -1.");
            }

            return addedDebt;
        }

        private int GetRunItemAdjustedCombatGoldReward(int baseGold, bool bossVictory)
        {
            int reward = Mathf.Max(0, baseGold);
            if (HasRunItem("relic_fate_coin"))
            {
                reward = Mathf.CeilToInt(reward * 1.15f);
            }

            if (HasRunItem("curse_cracked_debt_coin"))
            {
                reward = Mathf.CeilToInt(reward * 1.25f);
            }

            if (HasRunItem("relic_debt_ledger"))
            {
                reward += Mathf.Max(0, debt) * 2;
            }

            if (HasRunItem("curse_rusted_shackle"))
            {
                reward += 20;
            }

            if (bossVictory && HasRunItem("blessing_three_door_coin"))
            {
                reward += 25;
            }

            return reward;
        }

        private void ApplyRunItemVictorySideEffects(bool bossVictory)
        {
            if (HasRunItem("blessing_mercy_hand"))
            {
                Heal(4);
            }

            if (HasRunItem("curse_cracked_debt_coin"))
            {
                int addedDebt = GetRunItemReducedDebtGain(1);
                debt += addedDebt;
                AddLog($"Run item: victory debt +{addedDebt}.");
            }

            if (bossVictory && HasRunItem("curse_locked_blood_gate"))
            {
                GrantRunItemBossCardReward();
            }
        }

        private void GrantRunItemBossCardReward()
        {
            List<CardData> rewards = PickCombatRewards(1, true);
            CardData card = rewards.FirstOrDefault();
            if (card == null)
            {
                return;
            }

            if (TryAddCardToDeck(card, "보스 추가 보상"))
            {
                CheckBuildUnlocks();
                AddLog($"Run item: boss bonus card {card.DisplayName}.");
            }
        }

        private int GetMaxDeckSize()
        {
            return endlessModeActive ? MaxDeckSizeEndless : MaxDeckSizeBeforeEndless;
        }

        private bool CanAddCardToDeck()
        {
            return deck.Count < GetMaxDeckSize();
        }

        private bool TryAddCardToDeck(CardData card, string sourceLabel)
        {
            if (card == null)
            {
                return false;
            }

            int maxDeckSize = GetMaxDeckSize();
            if (deck.Count >= maxDeckSize)
            {
                AddLog($"{sourceLabel}: 덱 한도 {deck.Count}/{maxDeckSize}에 도달해 카드를 받을 수 없습니다.");
                return false;
            }

            deck.Add(card);
            return true;
        }

        private int GetRunItemRewardChoiceCount(int baseCount, bool eliteReward)
        {
            int count = Mathf.Max(1, baseCount);
            if (HasRunItem("blessing_star_compass") && Random.value <= 0.20f)
            {
                count += 1;
                AddLog("Run item: reward choice +1.");
            }

            if (eliteReward && HasRunItem("curse_skeletal_key"))
            {
                count += 1;
                AddLog("Run item: elite reward choice +1.");
            }

            return count;
        }

        private int GetRunItemAdjustedShopPrice(int basePrice)
        {
            int price = Mathf.Max(0, basePrice);
            if (HasRunItem("curse_sealed_debt_scroll"))
            {
                price = Mathf.CeilToInt(price * 1.15f);
            }

            return price;
        }

        private int GetRestHealAmount()
        {
            int amount = 18;
            if (HasRunItem("curse_locked_blood_gate"))
            {
                amount = Mathf.FloorToInt(amount * 0.70f);
            }

            return Mathf.Max(1, amount);
        }

        private void ApplyPostPlayCombinationEffects(CardData card, int debtBeforePlay)
        {
            if (card == null)
            {
                return;
            }

            if (card.CardId == "card_card_exchange" && IsCombinationComplete("safe_rebuild"))
            {
                DrawCards(1);
                TriggerCombinationImpact("safe_rebuild");
                AddLog("조합 발동: 안전한 재정비. 카드 1장 추가.");
            }

            if (card.CardId == "card_exploit_opening"
                && IsCombinationComplete("gap_breaker")
                && enemy != null
                && enemy.IntentAttack > 0
                && combinationTriggersThisTurn.Add("gap_breaker_block"))
            {
                playerBlock += 3;
                TriggerCombinationImpact("gap_breaker");
                AddLog("조합 발동: 틈새 공략. 방어도 +3.");
            }

            if (card.CardId == "card_forbidden_choice" && IsCombinationComplete("forbidden_cycle"))
            {
                forbiddenCycleActiveThisTurn = true;
                TriggerCombinationImpact("forbidden_cycle");
                AddLog("조합 발동: 금지된 순환. 이번 턴 공격 강화.");
            }

            if (card.CardId == "card_starlight_barrage"
                && IsCombinationComplete("starlight_guard")
                && combinationTriggersThisTurn.Add("starlight_guard_block"))
            {
                playerBlock += 4;
                TriggerCombinationImpact("starlight_guard");
                AddLog("조합 발동: 별빛 방어식. 방어도 +4.");
            }

            if (card.CardId == "card_fate_beheading"
                && IsCombinationComplete("fate_cleaver")
                && playerHealth * 100 <= playerMaxHealth * 30)
            {
                preventDeathThisTurn = true;
                TriggerCombinationImpact("fate_cleaver");
                AddLog("조합 발동: 운명 절단. 이번 턴 죽음을 1회 버팁니다.");
            }

            if (card.CardId == "card_absorb_curse"
                && IsCombinationComplete("curse_backflow")
                && debtBeforePlay > debt
                && combinationTriggersThisCombat.Add("curse_backflow"))
            {
                pendingCombinationDamageBonus += 5;
                pendingCombinationDamageBonusSourceId = "curse_backflow";
                Heal(3);
                TriggerCombinationImpact("curse_backflow");
                AddLog("조합 발동: 저주 역류. 다음 공격 피해 +5.");
            }

            if (card.CardId == "hard_attack_chain_rend" && enemy != null && enemy.Bleed > 0)
            {
                DrawCards(1);
                AddLog("Hard card: drew 1 from bleeding enemy.");
            }

            if (card.CardId == "hard_attack_gate_execution" && enemy != null && enemy.Health <= 0)
            {
                GainGold(18);
                AddLog("Hard card: execution gold gained.");
            }

            if (card.CardId == "hard_skill_debt_writ" && debtBeforePlay <= debt)
            {
                GainGold(18);
                AddLog("Hard card: no debt to clear, gold gained.");
            }

            if (card.CardId == "hard_exile_no_return" && debtBeforePlay > debt)
            {
                action += 1;
                TriggerCombinationImpact("hard_trait_exile_endless_atonement");
                AddLog("Hard card: action +1 after debt cleared.");
            }

            if (card.Category == CardCategory.Defense
                && IsCombinationComplete("brass_counter_ritual")
                && enemy != null
                && enemy.IntentAttack > 0
                && combinationTriggersThisTurn.Add("brass_counter_ritual"))
            {
                DrawCards(1);
                pendingCombinationDamageBonus += 3;
                pendingCombinationDamageBonusSourceId = "brass_counter_ritual";
                TriggerCombinationImpact("brass_counter_ritual");
                AddLog("Combination: brass counter ritual primed next attack.");
            }

            if (IsCombinationComplete("triple_omen_circle")
                && selectedClass == CharacterClass.Oracle
                && card.Effects.Any(effect => effect.EffectType == CardEffectType.RevealDoorEffect))
            {
                oracleNextCardCostReduction = Mathf.Max(oracleNextCardCostReduction, 1);
                TriggerCombinationImpact("triple_omen_circle");
                AddLog("Combination: next card cost -1.");
            }

            if (IsCombinationComplete("bloody_contract")
                && GetCombinationRecipe("bloody_contract").RequiredCardIds.All(cardsPlayedThisCombat.Contains)
                && combinationTriggersThisCombat.Add("bloody_contract"))
            {
                pendingCombinationDamageBonus += 6;
                pendingCombinationDamageBonusSourceId = "bloody_contract";
                TriggerCombinationImpact("bloody_contract");
                AddLog("조합 발동: 피 묻은 계약. 다음 공격 피해 +6.");
                LoseHealth(2, true);
            }
        }

        private int GetCombinationRepeatDamageBonus()
        {
            if (activeCard == null || activeCard.CardId != "card_starlight_barrage" || !IsCombinationComplete("starlight_guard"))
            {
                return 0;
            }

            AddLog("조합 발동: 별빛 방어식. 반복 공격 +1.");
            TriggerCombinationImpact("starlight_guard");
            return 1;
        }

        private void GainBlock(int amount)
        {
            int bonus = GetBuildBlockBonus();
            int combinationBonus = GetCombinationBlockBonus(activeCard) + GetRunItemBlockBonus(activeCard);
            int total = amount + bonus + combinationBonus;
            playerBlock += total;
            if (total > 0)
            {
                TriggerCombatFeedback("방어 성공", combatFeedbackDefenseSprite, new Color(0.50f, 1f, 0.94f, 1f));
            }

            if (bonus > 0 || combinationBonus > 0)
            {
                AddLog($"방어도 +{total}. 빌드 +{bonus}, 조합 +{combinationBonus}.");
            }
            else
            {
                AddLog($"방어도 +{amount}.");
            }
        }

        private int GetBuildDamageBonus()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (recipe.Id != "gambler_high_roll" || !IsBuildUnlocked(recipe) || luck < 5)
            {
                return 0;
            }

            return 2 + GetBuildUpgradeLevel(recipe.Id) * 2;
        }

        private int GetBuildBlockBonus()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (recipe.Id != "exile_last_oath" || !IsBuildUnlocked(recipe) || playerHealth * 2 > playerMaxHealth)
            {
                return 0;
            }

            int bonus = 2 + GetBuildUpgradeLevel(recipe.Id) * 2;
            if (!exileBuildTriggeredThisCombat)
            {
                exileBuildTriggeredThisCombat = true;
                AddLog($"{recipe.Name}: 저체력 방어 태세.");
            }

            return bonus;
        }

        private int GetCombinationBlockBonus(CardData card)
        {
            if (card == null || activeCardBlockBonusApplied)
            {
                return 0;
            }

            int bonus = 0;
            if (card.Category == CardCategory.Defense
                && IsCombinationComplete("survival_instinct")
                && playerHealth * 2 <= playerMaxHealth)
            {
                bonus += 3;
                combinationTriggersThisTurn.Add("survival_instinct_block");
                TriggerCombinationImpact("survival_instinct");
            }

            if (card.Category == CardCategory.Defense
                && IsHardModeFeatureActive()
                && selectedClass == CharacterClass.Gambler
                && luck <= 2
                && !gamblerHardLowLuckDefenseUsedThisTurn)
            {
                int hardBonus = 5 + GetHardTraitEndlessBonus();
                bonus += hardBonus;
                gamblerHardLowLuckDefenseUsedThisTurn = true;
                TriggerCombinationImpact("hard_trait_gambler_ruin_wager");
            }

            if (card.Category == CardCategory.Defense
                && IsHardModeFeatureActive()
                && selectedClass == CharacterClass.Oracle
                && oracleHardLuckHeldThisTurn
                && combinationTriggersThisTurn.Add("hard_trait_oracle_luck_held_block"))
            {
                bonus += 6;
                TriggerCombinationImpact("hard_trait_oracle_closed_fate");
            }

            if (card.CardId == "hard_oracle_crystal_sentence"
                && IsHardModeFeatureActive()
                && selectedClass == CharacterClass.Oracle
                && oracleHardProphecyPrimed)
            {
                bonus += 6;
                TriggerCombinationImpact("hard_trait_oracle_closed_fate");
            }

            if (card.Category == CardCategory.Defense
                && IsHardModeFeatureActive()
                && selectedClass == CharacterClass.Exile
                && playerHealth * 2 <= playerMaxHealth)
            {
                bonus += 4 + GetHardTraitEndlessBonus();
                TriggerCombinationImpact("hard_trait_exile_endless_atonement");
            }

            if (card.Category == CardCategory.Defense
                && IsCombinationComplete("gatekeeper_hunt")
                && enemy != null
                && (enemy.IsBoss || enemy.WasElite)
                && combinationTriggersThisCombat.Add("gatekeeper_hunt_defense"))
            {
                bonus += 8;
                TriggerCombinationImpact("gatekeeper_hunt");
            }

            if (card.Category == CardCategory.Defense
                && IsCombinationComplete("triple_omen_circle")
                && selectedClass == CharacterClass.Oracle
                && oracleHardLuckHeldThisTurn
                && combinationTriggersThisTurn.Add("triple_omen_circle_block"))
            {
                bonus += 6;
                TriggerCombinationImpact("triple_omen_circle");
            }

            if (bonus > 0)
            {
                activeCardBlockBonusApplied = true;
                AddLog($"조합 방어 보너스 +{bonus}.");
            }

            return bonus;
        }

        private int GetRunItemBlockBonus(CardData card)
        {
            if (card == null || activeCardRunItemBlockBonusApplied || card.Category != CardCategory.Defense)
            {
                return 0;
            }

            int bonus = 0;
            if (HasRunItem("relic_bone_dice") && luck <= 2)
            {
                bonus += 3;
            }

            if (bonus > 0)
            {
                activeCardRunItemBlockBonusApplied = true;
                AddLog($"Run item block +{bonus}.");
            }

            return bonus;
        }

        private void DealDamage(int amount)
        {
            int buildBonus = GetBuildDamageBonus();
            int combinationBonus = GetCombinationDamageBonus(activeCard) + GetRunItemDamageBonus(activeCard);
            int boostedAmount = amount + buildBonus + combinationBonus;
            int rawDamage = enemy.Vulnerable > 0 ? Mathf.CeilToInt(boostedAmount * 1.5f) : boostedAmount;
            int blocked = Mathf.Min(enemy.Block, rawDamage);
            enemy.Block -= blocked;
            int damage = Mathf.Max(0, rawDamage - blocked);
            enemy.Health = Mathf.Max(0, enemy.Health - damage);
            if (damage > 0)
            {
                bool criticalHit = enemy.Vulnerable > 0 || buildBonus > 0 || combinationBonus > 0;
                TriggerCombatFeedback(
                    criticalHit ? "치명타" : "공격 성공",
                    criticalHit ? combatFeedbackCriticalSprite : combatFeedbackAttackSprite,
                    criticalHit ? new Color(1f, 0.42f, 0.26f, 1f) : new Color(1f, 0.72f, 0.36f, 1f),
                    criticalHit ? 2 : 1);
            }
            else if (blocked > 0)
            {
                TriggerCombatFeedback("방어에 막힘", combatFeedbackBlockedSprite, new Color(0.70f, 0.90f, 1f, 1f));
            }

            if (buildBonus > 0 || combinationBonus > 0)
            {
                AddLog($"피해 {damage}. 빌드 +{buildBonus}, 조합 +{combinationBonus}.");
            }
            else
            {
                AddLog($"피해 {damage}.");
            }
        }

        private int GetCombinationDamageBonus(CardData card)
        {
            if (activeCardDamageBonusApplied)
            {
                return 0;
            }

            int bonus = 0;
            if (pendingCombinationDamageBonus > 0 && card != null && card.Category == CardCategory.Attack)
            {
                bonus += pendingCombinationDamageBonus;
                TriggerCombinationImpact(pendingCombinationDamageBonusSourceId);
                pendingCombinationDamageBonus = 0;
                pendingCombinationDamageBonusSourceId = string.Empty;
            }

            if (card != null && card.Category == CardCategory.Attack)
            {
                if (selectedClass == CharacterClass.Exile && exileNextAttackDamageBonus > 0)
                {
                    bonus += exileNextAttackDamageBonus;
                    if (enemy != null && exileNextAttackVulnerableBonus > 0)
                    {
                        enemy.Vulnerable += exileNextAttackVulnerableBonus;
                        AddLog($"저주 삼키기: 취약 +{exileNextAttackVulnerableBonus}.");
                    }

                    exileNextAttackDamageBonus = 0;
                    exileNextAttackVulnerableBonus = 0;
                    TriggerCombinationImpact("trait_exile_curse_eater");
                }

                if (IsCombinationComplete("fate_counter")
                    && luck >= 5
                    && combinationTriggersThisTurn.Add("fate_counter"))
                {
                    bonus += 3;
                    TriggerCombinationImpact("fate_counter");
                }

                if (card.CardId == "card_worn_dagger"
                    && IsCombinationComplete("old_gear_mastery")
                    && combinationTriggersThisCombat.Add("old_gear_mastery_attack"))
                {
                    bonus += 2;
                    TriggerCombinationImpact("old_gear_mastery");
                }

                if (card.CardId == "card_deep_stab" && IsCombinationComplete("edge_premonition"))
                {
                    bonus += 2;
                    TriggerCombinationImpact("edge_premonition");
                }

                if (card.CardId == "card_finish"
                    && IsCombinationComplete("execution_setup")
                    && cardsPlayedThisCombat.Contains("card_find_weakness")
                    && enemy != null
                    && enemy.Health * 100 <= enemy.MaxHealth * 30)
                {
                    bonus += 8;
                    TriggerCombinationImpact("execution_setup");
                }

                if (card.CardId == "card_exploit_opening"
                    && IsCombinationComplete("gap_breaker")
                    && enemy != null
                    && enemy.IntentAttack > 0)
                {
                    bonus += 4;
                    TriggerCombinationImpact("gap_breaker");
                }

                if (forbiddenCycleActiveThisTurn && IsCombinationComplete("forbidden_cycle"))
                {
                    bonus += debt > 0 ? 4 : 2;
                    TriggerCombinationImpact("forbidden_cycle");
                }

                if (IsCombinationComplete("gambling_bulwark") && luck >= 5)
                {
                    bonus += 4;
                    TriggerCombinationImpact("gambling_bulwark");
                }

                if (IsHardModeFeatureActive()
                    && selectedClass == CharacterClass.Gambler
                    && luck >= 5
                    && !gamblerHardHighLuckAttackUsedThisTurn)
                {
                    bonus += 4 + GetHardTraitEndlessBonus();
                    gamblerHardHighLuckAttackUsedThisTurn = true;
                    TriggerCombinationImpact("hard_trait_gambler_ruin_wager");
                }

                if (card.CardId == "hard_gambler_final_wager")
                {
                    int goldBonus = Mathf.Clamp(gold / 30, 0, 6) * 4;
                    if (goldBonus > 0)
                    {
                        bonus += goldBonus;
                        TriggerCombinationImpact("hard_trait_gambler_ruin_wager");
                    }
                }

                if (card.CardId == "hard_oracle_crystal_sentence"
                    && IsHardModeFeatureActive()
                    && selectedClass == CharacterClass.Oracle
                    && oracleHardProphecyPrimed)
                {
                    bonus += 6;
                    TriggerCombinationImpact("hard_trait_oracle_closed_fate");
                    oracleHardProphecyPrimed = false;
                }

                if (IsCombinationComplete("abyss_breakthrough")
                    && combinationTriggersThisTurn.Add("abyss_breakthrough_attack"))
                {
                    int abyssBonus = 6;
                    if (enemy != null && enemy.Health * 2 >= enemy.MaxHealth)
                    {
                        abyssBonus += 4;
                    }

                    bonus += abyssBonus;
                    TriggerCombinationImpact("abyss_breakthrough");
                }

                if (IsCombinationComplete("gold_execution"))
                {
                    int goldExecutionBonus = Mathf.Min(10, (gold / 50) * 2);
                    if (luck >= 5)
                    {
                        goldExecutionBonus += 4;
                    }

                    if (goldExecutionBonus > 0)
                    {
                        bonus += goldExecutionBonus;
                        TriggerCombinationImpact("gold_execution");
                    }
                }

                if (IsCombinationComplete("gatekeeper_hunt")
                    && enemy != null
                    && (enemy.IsBoss || enemy.WasElite)
                    && combinationTriggersThisCombat.Add("gatekeeper_hunt_attack"))
                {
                    bonus += 10;
                    TriggerCombinationImpact("gatekeeper_hunt");
                }

                if (card.CardId == "card_fate_beheading"
                    && IsCombinationComplete("fate_cleaver")
                    && playerHealth * 100 <= playerMaxHealth * 30)
                {
                    bonus += 12;
                    TriggerCombinationImpact("fate_cleaver");
                }
            }

            if (bonus > 0)
            {
                activeCardDamageBonusApplied = true;
                AddLog($"조합 공격 보너스 +{bonus}.");
            }

            return bonus;
        }

        private int GetRunItemDamageBonus(CardData card)
        {
            if (card == null || activeCardRunItemDamageBonusApplied || card.Category != CardCategory.Attack)
            {
                return 0;
            }

            int bonus = 0;
            if (HasRunItem("relic_bone_dice") && luck >= 5)
            {
                bonus += 3;
            }

            if (HasRunItem("curse_thorn_crown"))
            {
                bonus += 3;
            }

            if (bonus > 0)
            {
                activeCardRunItemDamageBonusApplied = true;
                AddLog($"Run item damage +{bonus}.");
            }

            return bonus;
        }

        private void StartCombatVictorySequence()
        {
            if (combatVictorySequenceActive || enemy == null)
            {
                return;
            }

            combatVictorySequenceActive = true;
            if (combatVictoryRoutine != null)
            {
                StopCoroutine(combatVictoryRoutine);
            }

            combatVictoryRoutine = StartCoroutine(PlayCombatVictorySequence());
        }

        private IEnumerator PlayCombatVictorySequence()
        {
            AddLog("마지막 일격이 적을 무너뜨렸습니다.");
            RenderCombat();
            ClearEnemyReveal();
            HideCardPreview();
            primaryButton.gameObject.SetActive(false);

            Image blocker = AddImage(root, "승리 입력 차단", new Color(0f, 0f, 0f, 0.001f));
            blocker.raycastTarget = true;
            combatVictoryOverlayRoot = blocker.rectTransform;
            Stretch(combatVictoryOverlayRoot);
            combatVictoryOverlayRoot.SetAsLastSibling();

            Sprite revealSprite = GetEnemyCombatSprite();
            CombatVictoryEffect effect = revealSprite != null ? CreateCombatVictoryEffect(combatVictoryOverlayRoot, revealSprite) : null;
            if (effect == null)
            {
                yield return new WaitForSeconds(0.65f);
            }
            else
            {
                float elapsed = 0f;
                bool flashFrameShown = false;
                while (elapsed < CombatVictoryEffectSeconds)
                {
                    if (effect.Root == null)
                    {
                        break;
                    }

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / CombatVictoryEffectSeconds);
                    float revealIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.24f));
                    float impactIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.10f) / 0.14f));
                    float impactOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.31f) / 0.18f));
                    float crackIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.24f) / 0.24f));
                    float crackOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.70f) / 0.20f));
                    float shatterIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.49f) / 0.24f));
                    float shardOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.84f) / 0.14f));
                    float logoIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.68f) / 0.18f));
                    float logoOut = Mathf.Clamp01((t - 0.94f) / 0.06f);
                    float fadeOut = Mathf.Clamp01((t - 0.90f) / 0.10f);
                    float impactAlpha = impactIn * (1f - impactOut) * (1f - fadeOut);
                    float crackAlpha = crackIn * (1f - crackOut) * (1f - fadeOut);
                    float shardAlpha = shatterIn * (1f - shardOut) * (1f - fadeOut);
                    float impactPulse = Mathf.Clamp01(1f - Mathf.Abs(t - 0.20f) / 0.045f);
                    float shake = impactAlpha * 7.0f + crackAlpha * 4.0f + shardAlpha * 2.2f;

                    effect.Group.alpha = 1f - fadeOut * 0.18f;
                    effect.PortraitRoot.localScale = Vector3.one * (0.94f + revealIn * 0.055f + crackAlpha * 0.035f);
                    effect.PortraitRoot.anchoredPosition = new Vector2(
                        Mathf.Sin(elapsed * 74f) * shake,
                        Mathf.Cos(elapsed * 61f) * shake * 0.7f);
                    effect.PortraitRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 28f) * shake * 0.11f);

                    if (effect.PortraitImage != null)
                    {
                        float portraitAlpha = 0.90f * revealIn * (1f - fadeOut * 0.35f);
                        effect.PortraitImage.color = new Color(0.96f, 0.93f - crackAlpha * 0.10f, 0.86f - crackAlpha * 0.16f, portraitAlpha);
                    }

                    effect.BurnOverlay.color = new Color(0.68f, 0.04f, 0.02f, (0.09f * impactAlpha + 0.08f * impactPulse) * (1f - fadeOut));
                    effect.AshOverlay.color = new Color(0.02f, 0.015f, 0.012f, (0.07f * crackAlpha + 0.12f * shardAlpha + 0.10f * logoIn) * (1f - fadeOut * 0.55f));

                    if (effect.ImpactOverlay != null)
                    {
                        effect.ImpactOverlay.color = new Color(1f, 0.82f, 0.68f, Mathf.Clamp01(impactAlpha * 0.78f + impactPulse * 0.18f));
                        effect.ImpactOverlay.rectTransform.localScale = Vector3.one * (0.86f + impactIn * 0.17f + impactPulse * 0.07f);
                        effect.ImpactOverlay.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 19f) * impactAlpha * 1.4f);
                    }

                    if (effect.CrackOverlay != null)
                    {
                        effect.CrackOverlay.color = new Color(1f, 0.86f, 0.48f, 0.68f * crackAlpha);
                        effect.CrackOverlay.rectTransform.anchoredPosition = Vector2.zero;
                        effect.CrackOverlay.rectTransform.localScale = Vector3.one * (0.99f + crackIn * 0.018f);
                    }

                    if (effect.ShardBurstOverlay != null)
                    {
                        effect.ShardBurstOverlay.color = new Color(1f, 0.90f, 0.70f, 0.92f * shardAlpha);
                        effect.ShardBurstOverlay.rectTransform.anchoredPosition = new Vector2(
                            Mathf.Sin(elapsed * 24f) * 8f * shardAlpha,
                            Mathf.Cos(elapsed * 19f) * 4f * shardAlpha);
                        effect.ShardBurstOverlay.rectTransform.localScale = Vector3.one * (0.94f + shatterIn * 0.08f);
                    }

                    foreach (VictoryCrackLine crackLine in effect.CrackLines)
                    {
                        float lineAlpha = crackLine.Color.a * crackIn * (1f - shatterIn * 0.45f) * (1f - fadeOut);
                        crackLine.Image.color = new Color(crackLine.Color.r, crackLine.Color.g, crackLine.Color.b, lineAlpha);
                        crackLine.RectTransform.localScale = Vector3.one * (0.72f + crackIn * 0.28f + shatterIn * 0.18f);
                    }

                    if (effect.VictoryLogo != null)
                    {
                        effect.VictoryLogo.color = new Color(1f, 1f, 1f, logoIn * (1f - logoOut));
                        effect.VictoryLogo.rectTransform.localScale = Vector3.one * (0.90f + logoIn * 0.10f);
                    }

                    if (effect.VictoryText != null)
                    {
                        effect.VictoryText.rectTransform.localScale = Vector3.one * (0.90f + logoIn * 0.10f);
                        effect.VictoryText.color = new Color(1f, 0.86f, 0.42f, logoIn * (1f - logoOut));
                    }

                    foreach (VictoryShard shard in effect.Shards)
                    {
                        float travel = shatterIn * shard.Speed;
                        shard.RectTransform.anchoredPosition = shard.BasePosition + shard.Direction * travel;
                        shard.RectTransform.localRotation = Quaternion.Euler(0f, 0f, shard.StartRotation + shard.RotationSpeed * shatterIn);
                        shard.RectTransform.localScale = Vector3.one * (0.72f + shatterIn * 0.34f);
                        shard.Image.color = new Color(shard.Color.r, shard.Color.g, shard.Color.b, shatterIn * (1f - fadeOut) * shard.Color.a);
                    }

                    if (effect.FlashOverlay != null)
                    {
                        effect.FlashOverlay.color = Color.clear;
                    }

                    if (!flashFrameShown && t >= 0.20f)
                    {
                        if (effect.FlashOverlay != null)
                        {
                            effect.FlashOverlay.color = new Color(0.82f, 0.03f, 0.01f, 0.16f);
                        }

                        flashFrameShown = true;
                        yield return null;

                        if (effect.FlashOverlay != null)
                        {
                            effect.FlashOverlay.color = Color.clear;
                        }

                        continue;
                    }

                    yield return null;
                }
            }

            DestroyCombatVictoryVisuals();
            combatVictorySequenceActive = false;
            combatVictoryRoutine = null;
            CompleteCombat();
        }

        private CombatVictoryEffect CreateCombatVictoryEffect(RectTransform overlayRoot, Sprite revealSprite)
        {
            RectTransform effectRoot = AddPanel(overlayRoot, "승리 연출", new Color(1f, 1f, 1f, 0f));
            Stretch(effectRoot);
            effectRoot.SetAsLastSibling();
            combatVictoryEffectRoot = effectRoot;

            CanvasGroup group = effectRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Image backdrop = AddImage(effectRoot, "승리 암전", new Color(0f, 0f, 0f, 0.42f));
            backdrop.raycastTarget = false;
            Stretch(backdrop.rectTransform);

            RectTransform portraitRoot = AddPanel(effectRoot, "몬스터 파괴 연출", new Color(1f, 1f, 1f, 0f));
            SetAnchors(portraitRoot, new Vector2(0.115f, 0.020f), new Vector2(0.885f, 0.980f));
            portraitRoot.pivot = new Vector2(0.5f, 0.5f);

            Image portrait = AddImage(portraitRoot, "승리 몬스터 이미지", new Color(1f, 1f, 1f, 0.86f));
            portrait.sprite = revealSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            Stretch(portrait.rectTransform);

            Image burnOverlay = AddImage(portraitRoot, "불타는 균열", new Color(1f, 0.20f, 0.02f, 0.28f));
            burnOverlay.raycastTarget = false;
            Stretch(burnOverlay.rectTransform);
            burnOverlay.color = new Color(1f, 0.45f, 0.10f, 0f);

            Image ashOverlay = AddImage(portraitRoot, "붕괴 암전", new Color(0.02f, 0.015f, 0.012f, 0.04f));
            ashOverlay.raycastTarget = false;
            Stretch(ashOverlay.rectTransform);

            CombatVictoryEffect effect = new(effectRoot, portraitRoot, group, burnOverlay, ashOverlay)
            {
                PortraitImage = portrait
            };

            if (victoryCrackOverlaySprite != null)
            {
                Image crackOverlay = AddImage(portraitRoot, "Victory crack overlay", Color.clear);
                crackOverlay.sprite = victoryCrackOverlaySprite;
                crackOverlay.preserveAspect = false;
                crackOverlay.raycastTarget = false;
                Stretch(crackOverlay.rectTransform);
                effect.CrackOverlay = crackOverlay;
            }

            if (victoryImpactSprite != null)
            {
                Image impactOverlay = AddImage(portraitRoot, "Victory red impact overlay", Color.clear);
                impactOverlay.sprite = victoryImpactSprite;
                impactOverlay.preserveAspect = true;
                impactOverlay.raycastTarget = false;
                SetAnchors(impactOverlay.rectTransform, new Vector2(0.040f, 0.000f), new Vector2(0.960f, 1.000f));
                effect.ImpactOverlay = impactOverlay;
            }

            Image flashOverlay = AddImage(portraitRoot, "Victory red impact pulse", Color.clear);
            flashOverlay.raycastTarget = false;
            Stretch(flashOverlay.rectTransform);
            effect.FlashOverlay = flashOverlay;

            if (victoryShardBurstSprite != null)
            {
                Image shardBurstOverlay = AddImage(effectRoot, "Victory ember shard burst overlay", Color.clear);
                shardBurstOverlay.sprite = victoryShardBurstSprite;
                shardBurstOverlay.preserveAspect = true;
                shardBurstOverlay.raycastTarget = false;
                SetAnchors(shardBurstOverlay.rectTransform, new Vector2(0.035f, 0.090f), new Vector2(0.965f, 0.900f));
                effect.ShardBurstOverlay = shardBurstOverlay;
            }

            Text victoryText = AddText(effectRoot, "승리 문구", "승리", 72, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.42f, 1f));
            victoryText.fontStyle = FontStyle.Bold;
            victoryText.resizeTextMinSize = 42;
            victoryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            victoryText.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextGlow(victoryText, new Color(0f, 0f, 0f, 0.92f), new Color(0.90f, 0.42f, 0.12f, 0.74f), new Vector2(3.0f, -3.5f));
            SetAnchors(victoryText.rectTransform, new Vector2(0.250f, 0.405f), new Vector2(0.750f, 0.610f));
            effect.VictoryText = victoryText;

            if (victoryLogoSprite != null)
            {
                victoryText.gameObject.SetActive(false);
                Image victoryLogo = AddImage(effectRoot, "Victory logo", Color.clear);
                victoryLogo.sprite = victoryLogoSprite;
                victoryLogo.preserveAspect = true;
                victoryLogo.raycastTarget = false;
                SetAnchors(victoryLogo.rectTransform, new Vector2(0.200f, 0.315f), new Vector2(0.800f, 0.715f));
                effect.VictoryLogo = victoryLogo;
            }

            effect.FlashOverlay.rectTransform.SetAsLastSibling();
            return effect;
        }

        private CombatVictoryEffect CreateCombatVictoryEffect(RectTransform enemyPanel)
        {
            RectTransform effectRoot = AddPanel(enemyPanel, "승리 연출", new Color(1f, 1f, 1f, 0f));
            Stretch(effectRoot);
            effectRoot.SetAsLastSibling();
            combatVictoryEffectRoot = effectRoot;

            CanvasGroup group = effectRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform portraitRoot = AddPanel(effectRoot, "프로필 파괴 연출", new Color(1f, 1f, 1f, 0f));
            SetAnchors(portraitRoot, new Vector2(0.010f, 0.110f), new Vector2(0.240f, 0.900f));
            portraitRoot.pivot = new Vector2(0.5f, 0.5f);

            Image burnOverlay = AddImage(portraitRoot, "화염 덮개", new Color(1f, 0.20f, 0.02f, 0.64f));
            Stretch(burnOverlay.rectTransform);

            Image ashOverlay = AddImage(portraitRoot, "붕괴 암전", new Color(0.02f, 0.015f, 0.012f, 0.04f));
            Stretch(ashOverlay.rectTransform);

            CombatVictoryEffect effect = new(effectRoot, portraitRoot, group, burnOverlay, ashOverlay);
            AddVictoryCrackLines(effect);

            Text victoryText = AddText(effectRoot, "승리 문구", "승리", 56, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.42f, 1f));
            victoryText.fontStyle = FontStyle.Bold;
            victoryText.resizeTextMinSize = 36;
            victoryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            victoryText.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextGlow(victoryText, new Color(0f, 0f, 0f, 0.92f), new Color(0.90f, 0.42f, 0.12f, 0.74f), new Vector2(3.0f, -3.5f));
            SetAnchors(victoryText.rectTransform, new Vector2(0.255f, 0.420f), new Vector2(0.790f, 0.760f));
            effect.VictoryText = victoryText;
            return effect;
        }

        private void AddVictoryCrackLines(CombatVictoryEffect effect)
        {
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.50f, 0.50f), new Vector2(650f, 3f), 24f, new Color(1f, 0.78f, 0.30f, 0.86f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.50f, 0.50f), new Vector2(590f, 3f), -36f, new Color(1f, 0.42f, 0.12f, 0.78f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.44f, 0.56f), new Vector2(430f, 2f), 78f, new Color(1f, 0.92f, 0.58f, 0.76f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.59f, 0.48f), new Vector2(410f, 2f), -82f, new Color(1f, 0.62f, 0.20f, 0.72f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.52f, 0.38f), new Vector2(360f, 2f), 8f, new Color(1f, 0.74f, 0.28f, 0.70f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.47f, 0.46f), new Vector2(300f, 2f), -18f, new Color(1f, 0.95f, 0.70f, 0.66f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.57f, 0.58f), new Vector2(280f, 2f), 42f, new Color(1f, 0.82f, 0.36f, 0.64f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.43f, 0.42f), new Vector2(330f, 2f), -68f, new Color(0.08f, 0.04f, 0.02f, 0.52f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.54f, 0.45f), new Vector2(360f, 2f), 57f, new Color(0.08f, 0.04f, 0.02f, 0.48f)));
            effect.CrackLines.Add(AddVictoryLine(effect.PortraitRoot, new Vector2(0.49f, 0.52f), new Vector2(500f, 2f), -4f, new Color(0.06f, 0.03f, 0.02f, 0.46f)));
        }

        private static VictoryCrackLine AddVictoryLine(RectTransform parent, Vector2 anchor, Vector2 size, float rotation, Color color)
        {
            GameObject child = new("균열선", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = child.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            return new VictoryCrackLine(rect, image, color);
        }

        private void AddVictoryShards(CombatVictoryEffect effect)
        {
            Sprite shardSprite = GetVictoryShardSprite();
            for (int i = 0; i < 46; i += 1)
            {
                GameObject child = new($"승리 파편 {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = child.GetComponent<RectTransform>();
                rect.SetParent(effect.PortraitRoot, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(Random.Range(26f, 118f), Random.Range(20f, 92f));

                Vector2 basePosition = new(Random.Range(-235f, 235f), Random.Range(-165f, 185f));
                Vector2 outward = basePosition.sqrMagnitude > 0.01f ? basePosition.normalized : Random.insideUnitCircle.normalized;
                Vector2 direction = (outward + new Vector2(Random.Range(-0.42f, 0.42f), Random.Range(-0.30f, 0.58f))).normalized;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector2.up;
                }

                rect.anchoredPosition = basePosition;
                float startRotation = Random.Range(-70f, 70f);
                rect.localRotation = Quaternion.Euler(0f, 0f, startRotation);
                Image image = child.GetComponent<Image>();
                image.sprite = shardSprite;
                image.type = Image.Type.Simple;
                Color shardColor = i % 3 == 0
                    ? new Color(1f, 0.90f, 0.48f, 0.88f)
                    : i % 3 == 1
                        ? new Color(1f, 0.52f, 0.16f, 0.78f)
                        : new Color(0.18f, 0.08f, 0.04f, 0.74f);
                image.color = Color.clear;
                image.raycastTarget = false;

                effect.Shards.Add(new VictoryShard(
                    rect,
                    image,
                    basePosition,
                    direction,
                    Random.Range(260f, 760f),
                    startRotation,
                    Random.Range(-260f, 260f),
                    shardColor));
            }
        }

        private Sprite GetVictoryShardSprite()
        {
            if (cachedVictoryShardSprite != null)
            {
                return cachedVictoryShardSprite;
            }

            const int textureSize = 64;
            Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 a = new(8f, 54f);
            Vector2 b = new(34f, 7f);
            Vector2 c = new(58f, 50f);
            for (int y = 0; y < textureSize; y += 1)
            {
                for (int x = 0; x < textureSize; x += 1)
                {
                    Vector2 point = new(x + 0.5f, y + 0.5f);
                    float w0 = EdgeSign(point, b, c);
                    float w1 = EdgeSign(point, c, a);
                    float w2 = EdgeSign(point, a, b);
                    bool hasNegative = w0 < 0f || w1 < 0f || w2 < 0f;
                    bool hasPositive = w0 > 0f || w1 > 0f || w2 > 0f;
                    if (hasNegative && hasPositive)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float edgeDistance = Mathf.Min(Mathf.Abs(w0), Mathf.Abs(w1), Mathf.Abs(w2)) / textureSize;
                    float alpha = edgeDistance < 0.22f ? 0.95f : 0.55f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            cachedVictoryShardSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
            cachedVictoryShardSprite.name = "Runtime Victory Shard";
            return cachedVictoryShardSprite;
        }

        private static float EdgeSign(Vector2 point, Vector2 a, Vector2 b)
        {
            return (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
        }

        private void StopCombatVictorySequence()
        {
            if (combatVictoryRoutine != null)
            {
                StopCoroutine(combatVictoryRoutine);
                combatVictoryRoutine = null;
            }

            combatVictorySequenceActive = false;
            DestroyCombatVictoryVisuals();
        }

        private void DestroyCombatVictoryVisuals()
        {
            if (combatVictoryOverlayRoot != null)
            {
                combatVictoryOverlayRoot.gameObject.SetActive(false);
                Destroy(combatVictoryOverlayRoot.gameObject);
                combatVictoryOverlayRoot = null;
            }

            if (combatVictoryEffectRoot != null)
            {
                combatVictoryEffectRoot.gameObject.SetActive(false);
                Destroy(combatVictoryEffectRoot.gameObject);
                combatVictoryEffectRoot = null;
            }
        }

        private void CompleteCombat()
        {
            int baseRewardGold = enemy.IsBoss ? 0 : enemy.BaseGoldReward + debt * 2;
            int rewardGold = GetRunItemAdjustedCombatGoldReward(baseRewardGold, enemy.IsBoss);
            if (enemy.IsBoss)
            {
                PlayBossVictorySfx();
                if (IsDebtClearBoss(enemy))
                {
                    CompleteDebtClearBossVictory();
                    return;
                }

                if (endlessModeActive)
                {
                    CompleteEndlessBoss();
                    return;
                }

                AddLog("문지기를 쓰러뜨렸습니다. 열 번째 문 너머의 선택이 열립니다.");
                if (rewardGold > 0)
                {
                    gold += rewardGold;
                    AddLog($"Boss reward gold +{rewardGold}.");
                }

                ApplyRunItemVictorySideEffects(true);
                RunItemType bossRewardType = GetBossRewardItemType(currentDifficulty);
                UnlockRunItemTypeForSelectedClass(bossRewardType);
                if (TryShowBossItemReward(bossRewardType, ShowTenDoorClearChoice))
                {
                    return;
                }

                ShowTenDoorClearChoice();
                return;
            }

            combatEncountersCompleted += 1;
            gold += rewardGold;
            ApplyRunItemVictorySideEffects(false);
            AddLog($"전투 승리. 금화 +{rewardGold}. 보스 전 전투 {combatEncountersCompleted}/{MinimumPreBossCombats}.");
            int rewardChoiceCount = 3;
            if (IsCombinationComplete("third_answer") && Random.value <= 0.20f)
            {
                rewardChoiceCount = 4;
                TriggerCombinationImpact("third_answer");
                AddLog("조합 발동: 세 번째 해답. 카드 보상 선택지 +1.");
            }

            bool eliteReward = enemy.WasElite;
            rewardChoiceCount = GetRunItemRewardChoiceCount(rewardChoiceCount, eliteReward);
            List<CardData> rewards = PickCombatRewards(rewardChoiceCount, eliteReward);
            UnityAction continueRewards = () =>
            {
                if (ShouldOfferPostCombatSustain())
                {
                    ShowPostCombatSustainChoice(rewards, eliteReward);
                    return;
                }

                ShowReward(rewards);
            };
            if (TryShowDoorItemDiscovery(currentCombatDoorType, continueRewards))
            {
                return;
            }

            continueRewards.Invoke();
        }

        private bool ShouldOfferPostCombatSustain()
        {
            return IsHardModeFeatureActive();
        }

        private bool IsHardModeFeatureActive()
        {
            return currentDifficulty == RunDifficulty.Hard || endlessModeActive;
        }

        private bool TryShowBossItemReward(RunItemType rewardType, UnityAction onComplete)
        {
            RunItemDefinition item = GetPredictedBossRunItemReward(rewardType);
            predictedBossRunItemRewardId = string.Empty;
            if (item == null)
            {
                AddLog("이번 보스 보상으로 새로 장착할 아이템이 없습니다.");
                return false;
            }

            ShowRunItemReward(item, onComplete);
            return true;
        }

        private bool TryShowDoorItemDiscovery(DoorType sourceType, UnityAction onComplete)
        {
            float chance = sourceType switch
            {
                DoorType.Elite => EliteRunItemDiscoveryChance,
                DoorType.Curse => CurseDoorRunItemDiscoveryChance,
                _ => 0f
            };
            if (chance <= 0f || Random.value > chance)
            {
                return false;
            }

            RunItemDefinition item = PickUnlockedRunItem();
            if (item == null)
            {
                return false;
            }

            AddLog($"{(sourceType == DoorType.Curse ? "대가의 문" : "정예의 문")}에서 {GetRunItemTypeName(item.Type)}을 발견했습니다.");
            ShowRunItemReward(item, onComplete);
            return true;
        }

        private RunItemDefinition PickRunItemByType(RunItemType rewardType)
        {
            List<RunItemDefinition> candidates = GetRunItemDefinitions()
                .Where(item => item.Type == rewardType && !equippedRunItemIds.Contains(item.Id))
                .ToList();
            return PickPreferredUndiscoveredRunItem(candidates);
        }

        private RunItemDefinition GetPredictedBossRunItemReward(RunItemType rewardType)
        {
            if (!string.IsNullOrWhiteSpace(predictedBossRunItemRewardId))
            {
                RunItemDefinition predictedItem = GetRunItemDefinition(predictedBossRunItemRewardId);
                if (predictedItem != null
                    && predictedItem.Type == rewardType
                    && !equippedRunItemIds.Contains(predictedItem.Id))
                {
                    return predictedItem;
                }

                predictedBossRunItemRewardId = string.Empty;
            }

            RunItemDefinition item = PickRunItemByType(rewardType);
            if (item != null)
            {
                predictedBossRunItemRewardId = item.Id;
            }

            return item;
        }

        private RunItemDefinition PickUnlockedRunItem()
        {
            List<RunItemDefinition> candidates = GetRunItemDefinitions()
                .Where(item => IsRunItemTypeUnlockedForSelectedClass(item.Type) && !equippedRunItemIds.Contains(item.Id))
                .ToList();
            return PickPreferredUndiscoveredRunItem(candidates);
        }

        private RunItemDefinition PickPreferredUndiscoveredRunItem(List<RunItemDefinition> candidates)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            List<RunItemDefinition> undiscovered = candidates
                .Where(item => !IsRunItemDiscoveredForSelectedClass(item))
                .ToList();
            List<RunItemDefinition> pool = undiscovered.Count > 0 ? undiscovered : candidates;
            return pool[Random.Range(0, pool.Count)];
        }

        private static RunItemType GetBossRewardItemType(RunDifficulty difficulty)
        {
            return difficulty switch
            {
                RunDifficulty.Normal => RunItemType.Blessing,
                RunDifficulty.Hard => RunItemType.Curse,
                _ => RunItemType.Relic
            };
        }

        private void ShowRunItemReward(RunItemDefinition item, UnityAction onComplete)
        {
            DiscoverRunItemForSelectedClass(item);
            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(rewardBackground != null ? rewardBackground : bossBackground);
            ClearContent();
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0.100f, 0.130f), new Vector2(0.900f, 0.865f));
            primaryButton.gameObject.SetActive(false);
            subtitleText.text = $"{GetRunItemTypeName(item.Type)} 보상";
            SetSubtitleBoxVisible(true);

            RectTransform panel = AddPanel(contentRoot, "아이템 보상", new Color(1f, 1f, 1f, 0.88f), statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite);
            SetAnchors(panel, new Vector2(0.000f, 0.000f), new Vector2(1.000f, 0.840f));

            RectTransform titleBox = AddRunStatusLabelBox(
                contentRoot,
                "아이템 보상 제목 박스",
                $"{GetRunItemTypeName(item.Type)} 획득",
                new Vector2(0.325f, 0.870f),
                new Vector2(0.675f, 1.000f),
                34);
            titleBox.SetAsLastSibling();

            Sprite itemIcon = GetRunItemIcon(item);
            Vector2 bodyMin = new(0.120f, 0.505f);
            Vector2 bodyMax = new(0.880f, 0.760f);
            if (itemIcon != null)
            {
                Image icon = AddImage(panel, "아이템 아이콘", Color.white);
                icon.sprite = itemIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(icon.rectTransform, new Vector2(0.145f, 0.525f), new Vector2(0.315f, 0.750f));
                bodyMin = new Vector2(0.355f, 0.505f);
            }

            int slotLimit = GetRunItemSlotLimit();
            bool slotUnlocked = IsRunItemTypeSlotUnlocked(item.Type);
            bool canEquipDirectly = slotUnlocked
                && (equippedRunItemIds.Count < slotLimit || FindEquippedRunItemIndexByType(item.Type) >= 0);
            Text body = AddText(panel, "아이템 보상 설명", $"{item.Name}\n{item.Effect}\n{item.Description}\n\n장착 슬롯 {equippedRunItemIds.Count}/{slotLimit}", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.84f, 0.76f, 1f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 15;
            body.resizeTextMaxSize = 22;
            SetAnchors(body.rectTransform, bodyMin, bodyMax);

            if (canEquipDirectly)
            {
                AddRunStatusTextButton(
                    panel,
                    "아이템 장착 버튼",
                    "장착하고 계속",
                    new Vector2(0.165f, 0.185f),
                    new Vector2(0.465f, 0.320f),
                    () =>
                    {
                        if (EquipRunItem(item))
                        {
                            AddLog($"{GetRunItemTypeName(item.Type)} 장착: {item.Name}. 현재 장착 {equippedRunItemIds.Count}/{GetRunItemSlotLimit()}.");
                        }
                        RefreshTopBar();
                        onComplete.Invoke();
                    },
                    22);
                AddRunStatusTextButton(
                    panel,
                    "아이템 보관 버튼",
                    "보관하기",
                    new Vector2(0.535f, 0.185f),
                    new Vector2(0.835f, 0.320f),
                    () =>
                    {
                        AddLog($"{GetRunItemTypeName(item.Type)} 보관: {item.Name}.");
                        onComplete.Invoke();
                    },
                    22);
                AddLog($"{GetRunItemTypeName(item.Type)} 발견: {item.Name}.");
                RefreshTopBar();
                return;
            }

            string replaceHintText = slotUnlocked
                ? "장착 슬롯이 가득 찼습니다. 교체할 아이템을 고르거나 새 아이템을 보관하세요."
                : $"{GetRunItemTypeName(item.Type)} 슬롯은 현재 난이도에서 잠겨 있습니다. 보관한 뒤 상위 난이도에서 장착할 수 있습니다.";
            Text replaceHint = AddText(panel, "아이템 교체 안내", replaceHintText, 18, TextAnchor.MiddleCenter, new Color(0.78f, 1f, 0.94f, 1f));
            replaceHint.resizeTextForBestFit = true;
            replaceHint.resizeTextMinSize = 13;
            replaceHint.resizeTextMaxSize = 18;
            SetAnchors(replaceHint.rectTransform, new Vector2(0.10f, 0.425f), new Vector2(0.90f, 0.500f));

            if (slotUnlocked)
            {
                for (int i = 0; i < equippedRunItemIds.Count; i += 1)
                {
                    int replaceIndex = i;
                    RunItemDefinition equippedItem = GetRunItemDefinition(equippedRunItemIds[i]);
                    string equippedName = equippedItem?.Name ?? equippedRunItemIds[i];
                    AddPostTenChoice(
                        panel,
                        $"{equippedName} 교체",
                        $"{equippedName} 대신 {item.Name}을 장착합니다.",
                        new Vector2(0.070f, 0.285f - i * 0.090f),
                        new Vector2(0.930f, 0.360f - i * 0.090f),
                        () =>
                        {
                            ReplaceRunItem(replaceIndex, item);
                            AddLog($"{equippedName}을 빼고 {item.Name}을 장착했습니다.");
                            onComplete.Invoke();
                        },
                        true);
                }
            }

            AddPostTenChoice(
                panel,
                "보관하기",
                $"{item.Name}을 장착하지 않고 발견 목록에 보관합니다.",
                new Vector2(0.070f, 0.020f),
                new Vector2(0.930f, 0.095f),
                () =>
                {
                    AddLog($"{GetRunItemTypeName(item.Type)} 보관: {item.Name}.");
                    onComplete.Invoke();
                },
                true);
            RefreshTopBar();
        }

        private bool EquipRunItem(RunItemDefinition item)
        {
            if (item == null || equippedRunItemIds.Contains(item.Id) || !IsRunItemTypeSlotUnlocked(item.Type))
            {
                return false;
            }

            bool hadBottleBefore = HasBottleOfLightEquipped();
            DiscoverRunItemForSelectedClass(item);
            int sameTypeIndex = FindEquippedRunItemIndexByType(item.Type);
            if (sameTypeIndex >= 0)
            {
                equippedRunItemIds[sameTypeIndex] = item.Id;
                SyncBottleHealthBonusAfterEquipmentChange(hadBottleBefore);
                SaveEquippedRunItemsForSelectedClass();
                return true;
            }

            if (equippedRunItemIds.Count >= GetRunItemSlotLimit())
            {
                return false;
            }

            equippedRunItemIds.Add(item.Id);
            SyncBottleHealthBonusAfterEquipmentChange(hadBottleBefore);
            SaveEquippedRunItemsForSelectedClass();
            return true;
        }

        private void ReplaceRunItem(int index, RunItemDefinition item)
        {
            if (item == null || index < 0 || index >= equippedRunItemIds.Count || !IsRunItemTypeSlotUnlocked(item.Type))
            {
                return;
            }

            bool hadBottleBefore = HasBottleOfLightEquipped();
            DiscoverRunItemForSelectedClass(item);
            equippedRunItemIds[index] = item.Id;
            SyncBottleHealthBonusAfterEquipmentChange(hadBottleBefore);
            SaveEquippedRunItemsForSelectedClass();
        }

        private bool SelectRunItemFromCollection(RunItemDefinition item)
        {
            if (item == null || !IsRunItemDiscoveredForSelectedClass(item) || !IsRunItemTypeSlotUnlocked(item.Type))
            {
                return false;
            }

            if (equippedRunItemIds.Contains(item.Id))
            {
                return true;
            }

            bool hadBottleBefore = HasBottleOfLightEquipped();
            int sameTypeIndex = FindEquippedRunItemIndexByType(item.Type);
            if (sameTypeIndex >= 0)
            {
                equippedRunItemIds[sameTypeIndex] = item.Id;
            }
            else if (equippedRunItemIds.Count < GetRunItemSlotLimit())
            {
                equippedRunItemIds.Add(item.Id);
            }
            else
            {
                int replaceIndex = FindFirstDuplicateRunItemTypeIndex();
                equippedRunItemIds[replaceIndex >= 0 ? replaceIndex : 0] = item.Id;
            }

            SyncBottleHealthBonusAfterEquipmentChange(hadBottleBefore);
            SaveEquippedRunItemsForSelectedClass();
            AddLog($"{GetRunItemTypeName(item.Type)} 선택: {item.Name}.");
            RefreshTopBar();
            return true;
        }

        private int FindEquippedRunItemIndexByType(RunItemType type)
        {
            for (int i = 0; i < equippedRunItemIds.Count; i += 1)
            {
                if (GetRunItemDefinition(equippedRunItemIds[i])?.Type == type)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindFirstDuplicateRunItemTypeIndex()
        {
            HashSet<RunItemType> seenTypes = new();
            for (int i = 0; i < equippedRunItemIds.Count; i += 1)
            {
                RunItemDefinition item = GetRunItemDefinition(equippedRunItemIds[i]);
                if (item == null)
                {
                    return i;
                }

                if (!seenTypes.Add(item.Type))
                {
                    return i;
                }
            }

            return -1;
        }

        private void LoadEquippedRunItemsForSelectedClass()
        {
            equippedRunItemIds.Clear();
            string json = PlayerPrefs.GetString(GetEquippedItemKey(selectedClass), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            EquippedItemSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<EquippedItemSaveData>(json);
            }
            catch (ArgumentException)
            {
                return;
            }

            if (saveData?.itemIds == null)
            {
                return;
            }

            foreach (string itemId in saveData.itemIds)
            {
                if (equippedRunItemIds.Count >= GetRunItemSlotLimit())
                {
                    break;
                }

                RunItemDefinition item = GetRunItemDefinition(itemId);
                if (!string.IsNullOrWhiteSpace(itemId)
                    && item != null
                    && IsRunItemTypeSlotUnlocked(item.Type)
                    && !equippedRunItemIds.Contains(itemId))
                {
                    equippedRunItemIds.Add(itemId);
                }
            }
        }

        private void SaveEquippedRunItemsForSelectedClass()
        {
            EquippedItemSaveData saveData = new()
            {
                itemIds = equippedRunItemIds.Take(MaxEquippedRunItems).ToList()
            };
            PlayerPrefs.SetString(GetEquippedItemKey(selectedClass), JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private static string GetEquippedItemKey(CharacterClass characterClass)
        {
            return $"{EquippedItemKeyPrefix}{characterClass}";
        }

        private void LoadDiscoveredRunItemsForSelectedClass()
        {
            discoveredRunItemIds.Clear();
            string json = PlayerPrefs.GetString(GetDiscoveredItemKey(selectedClass), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            EquippedItemSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<EquippedItemSaveData>(json);
            }
            catch (ArgumentException)
            {
                return;
            }

            if (saveData?.itemIds == null)
            {
                return;
            }

            foreach (string itemId in saveData.itemIds)
            {
                if (!string.IsNullOrWhiteSpace(itemId) && GetRunItemDefinition(itemId) != null)
                {
                    discoveredRunItemIds.Add(itemId);
                }
            }
        }

        private void SaveDiscoveredRunItemsForSelectedClass()
        {
            EquippedItemSaveData saveData = new()
            {
                itemIds = discoveredRunItemIds
                    .OrderBy(itemId => itemId, StringComparer.Ordinal)
                    .ToList()
            };
            PlayerPrefs.SetString(GetDiscoveredItemKey(selectedClass), JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private void EnsureEquippedRunItemsAreDiscovered()
        {
            bool changed = false;
            foreach (string itemId in equippedRunItemIds)
            {
                if (!string.IsNullOrWhiteSpace(itemId) && GetRunItemDefinition(itemId) != null)
                {
                    changed |= discoveredRunItemIds.Add(itemId);
                }
            }

            if (changed)
            {
                SaveDiscoveredRunItemsForSelectedClass();
            }
        }

        private bool DiscoverRunItemForSelectedClass(RunItemDefinition item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                return false;
            }

            if (!discoveredRunItemIds.Add(item.Id))
            {
                return false;
            }

            SaveDiscoveredRunItemsForSelectedClass();
            return true;
        }

        private bool IsRunItemDiscoveredForSelectedClass(RunItemDefinition item)
        {
            return item != null && (discoveredRunItemIds.Contains(item.Id) || equippedRunItemIds.Contains(item.Id));
        }

        private static string GetDiscoveredItemKey(CharacterClass characterClass)
        {
            return $"{DiscoveredItemKeyPrefix}{characterClass}";
        }

        private bool IsRunItemTypeUnlockedForSelectedClass(RunItemType type)
        {
            return PlayerPrefs.GetInt(GetRunItemUnlockKey(selectedClass, type), 0) == 1;
        }

        private void UnlockRunItemTypeForSelectedClass(RunItemType type)
        {
            string key = GetRunItemUnlockKey(selectedClass, type);
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            AddLog($"{GetClassName(selectedClass)}에게 {GetRunItemTypeName(type)} 발견이 해금되었습니다.");
        }

        private static string GetRunItemUnlockKey(CharacterClass characterClass, RunItemType type)
        {
            return $"{RunItemUnlockKeyPrefix}{characterClass}.{type}";
        }

        private IReadOnlyList<RunItemDefinition> GetRunItemDefinitions()
        {
            if (cachedRunItemDefinitions != null)
            {
                return cachedRunItemDefinitions;
            }

            cachedRunItemDefinitions = LoadRunItemDefinitionsFromCatalog();
            if (cachedRunItemDefinitions.Count == 0)
            {
                cachedRunItemDefinitions.AddRange(FallbackRunItemDefinitions);
            }

            return cachedRunItemDefinitions;
        }

        private List<RunItemDefinition> LoadRunItemDefinitionsFromCatalog()
        {
            List<RunItemDefinition> definitions = new();
            if (runModifierCatalog == null || string.IsNullOrWhiteSpace(runModifierCatalog.text))
            {
                return definitions;
            }

            RunModifierCatalogData catalog;
            try
            {
                catalog = JsonUtility.FromJson<RunModifierCatalogData>(runModifierCatalog.text);
            }
            catch (ArgumentException)
            {
                return definitions;
            }

            if (catalog?.modifiers == null)
            {
                return definitions;
            }

            foreach (RunModifierCatalogEntry entry in catalog.modifiers)
            {
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.id)
                    || !Enum.TryParse(entry.category, true, out RunItemType type))
                {
                    continue;
                }

                definitions.Add(new RunItemDefinition(
                    entry.id.Trim(),
                    string.IsNullOrWhiteSpace(entry.name) ? entry.id.Trim() : entry.name.Trim(),
                    type,
                    entry.effect?.Trim() ?? string.Empty,
                    entry.description?.Trim() ?? string.Empty,
                    entry.icon?.Trim() ?? string.Empty));
            }

            return definitions
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .ToList();
        }

        private RunItemDefinition GetRunItemDefinition(string itemId)
        {
            return GetRunItemDefinitions().FirstOrDefault(item => item.Id == itemId);
        }

        private bool HasRunItem(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && equippedRunItemIds.Contains(itemId);
        }

        private bool HasBottleOfLightEquipped()
        {
            return HasRunItem("blessing_bottle_of_light");
        }

        private void SyncBottleHealthBonusAfterEquipmentChange(bool hadBottleBefore)
        {
            bool hasBottleNow = HasBottleOfLightEquipped();
            if (hasBottleNow && !hadBottleBefore)
            {
                ApplyBottleHealthBonusIfNeeded();
                return;
            }

            if (!hasBottleNow && hadBottleBefore)
            {
                RemoveBottleHealthBonusIfNeeded();
                return;
            }
        }

        private void ApplyBottleHealthBonusIfNeeded()
        {
            if (runItemBottleHealthBonusApplied)
            {
                return;
            }

            runItemBottleHealthBonusApplied = true;
            playerMaxHealth += BottleOfLightHealthBonus;
            playerHealth = Mathf.Min(playerMaxHealth, playerHealth + BottleOfLightHealthBonus);
            AddLog($"Run item: max health +{BottleOfLightHealthBonus}, health +{BottleOfLightHealthBonus}.");
        }

        private void RemoveBottleHealthBonusIfNeeded()
        {
            if (!runItemBottleHealthBonusApplied)
            {
                return;
            }

            runItemBottleHealthBonusApplied = false;
            playerMaxHealth = Mathf.Max(1, playerMaxHealth - BottleOfLightHealthBonus);
            playerHealth = Mathf.Clamp(playerHealth, 1, playerMaxHealth);
            AddLog($"Run item: max health -{BottleOfLightHealthBonus}.");
        }

        private void ApplyRunItemRunStartBonuses()
        {
            if (!HasBottleOfLightEquipped())
            {
                return;
            }

            ApplyBottleHealthBonusIfNeeded();
        }

        private Sprite GetRunItemIcon(RunItemDefinition item)
        {
            if (item == null)
            {
                return null;
            }

            return runItemIcons.FirstOrDefault(binding => binding.itemId == item.Id)?.sprite;
        }

        private Sprite GetRunItemSilhouetteIcon(RunItemType type)
        {
            RunItemDefinition firstItemOfType = GetRunItemDefinitions()
                .Where(item => item.Type == type)
                .OrderBy(item => item.IconName, StringComparer.Ordinal)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            return GetRunItemIcon(firstItemOfType);
        }

        private List<RunItemDefinition> GetEquippedRunItems()
        {
            return equippedRunItemIds
                .Select(GetRunItemDefinition)
                .Where(item => item != null)
                .ToList();
        }

        private RunItemDefinition GetEquippedRunItemByType(RunItemType type)
        {
            return equippedRunItemIds
                .Select(GetRunItemDefinition)
                .FirstOrDefault(item => item != null && item.Type == type);
        }

        private string GetEquippedRunItemNames()
        {
            List<RunItemDefinition> items = GetEquippedRunItems();
            return items.Count == 0 ? "없음" : string.Join(", ", items.Select(item => item.Name));
        }

        private string BuildEquippedRunItemsText()
        {
            List<RunItemDefinition> items = GetEquippedRunItems();
            if (items.Count == 0)
            {
                return $"장착 아이템 없음\n현재 난이도에서는 최대 {GetRunItemSlotLimit()}개까지 장착할 수 있습니다.";
            }

            List<string> lines = new()
            {
                $"장착 {items.Count}/{GetRunItemSlotLimit()}"
            };
            lines.AddRange(items.Select(item => $"{GetRunItemTypeName(item.Type)} | {item.Name}\n{item.Effect}\n{item.Description}"));
            return string.Join("\n\n", lines);
        }

        private int GetRunItemSlotLimit()
        {
            if (endlessModeActive || currentDifficulty == RunDifficulty.Hard)
            {
                return MaxEquippedRunItems;
            }

            return currentDifficulty == RunDifficulty.Normal ? 2 : 1;
        }

        private bool IsRunItemSlotUnlocked(int index)
        {
            return index >= 0 && index < GetRunItemSlotLimit();
        }

        private static int GetRunItemSlotIndex(RunItemType type)
        {
            return type switch
            {
                RunItemType.Blessing => 1,
                RunItemType.Curse => 2,
                _ => 0
            };
        }

        private bool IsRunItemTypeSlotUnlocked(RunItemType type)
        {
            return IsRunItemSlotUnlocked(GetRunItemSlotIndex(type));
        }

        private static string GetRunItemSlotUnlockLabel(int index)
        {
            return index switch
            {
                1 => "보통 해금",
                2 => "어려움 해금",
                _ => "잠김"
            };
        }

        private static string GetRunItemTypeName(RunItemType type)
        {
            return type switch
            {
                RunItemType.Blessing => "축복",
                RunItemType.Curse => "저주",
                _ => "유물"
            };
        }

        private void ShowPostCombatSustainChoice(List<CardData> rewards, bool eliteReward)
        {
            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(rewardBackground != null ? rewardBackground : battleBackground);
            ClearContent();
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0.130f, 0.180f), new Vector2(0.870f, 0.825f));
            primaryButton.gameObject.SetActive(false);
            subtitleText.text = "긴 싸움 뒤에 다음 준비를 선택하세요";
            SetSubtitleBoxVisible(true);

            RectTransform panel = AddPanel(contentRoot, "전투 후 정비", new Color(1f, 1f, 1f, 0.88f), statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite);
            SetAnchors(panel, Vector2.zero, Vector2.one);

            Text title = AddText(panel, "전투 후 정비 제목", "전투 후 정비", 36, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.54f, 1f));
            title.fontStyle = FontStyle.Bold;
            AddTextGlow(title, new Color(0f, 0f, 0f, 0.94f), new Color(0.12f, 0.78f, 0.76f, 0.46f), new Vector2(2.5f, -2.8f));
            SetAnchors(title.rectTransform, new Vector2(0.08f, 0.735f), new Vector2(0.92f, 0.895f));

            Text body = AddText(panel, "전투 후 정비 설명", "어려움과 무한 모드에서는 전투가 길어집니다. 카드 보상 전에 작은 정비를 선택합니다.", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.84f, 0.76f, 1f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 15;
            body.resizeTextMaxSize = 22;
            SetAnchors(body.rectTransform, new Vector2(0.10f, 0.570f), new Vector2(0.90f, 0.705f));

            int healAmount = GetPostCombatHealAmount(eliteReward);
            int goldAmount = GetPostCombatGoldAmount(eliteReward);
            AddPostTenChoice(
                panel,
                "체력 일부 회복하기",
                $"체력 {healAmount} 회복. 현재 체력 {playerHealth}/{playerMaxHealth}.",
                new Vector2(0.070f, 0.330f),
                new Vector2(0.930f, 0.500f),
                () => ApplyPostCombatSustain(true, healAmount, rewards),
                playerHealth < playerMaxHealth);
            AddPostTenChoice(
                panel,
                "금화 얻기",
                $"금화 {goldAmount} 획득. 상점과 빚 청산을 위한 자금을 확보합니다.",
                new Vector2(0.070f, 0.130f),
                new Vector2(0.930f, 0.300f),
                () => ApplyPostCombatSustain(false, goldAmount, rewards),
                true);

            RefreshTopBar();
        }

        private int GetPostCombatHealAmount(bool eliteReward)
        {
            int baseHeal = Mathf.CeilToInt(playerMaxHealth * (eliteReward ? 0.14f : 0.10f));
            return Mathf.Max(eliteReward ? 9 : 6, baseHeal);
        }

        private static int GetPostCombatGoldAmount(bool eliteReward)
        {
            return eliteReward ? 18 : 11;
        }

        private void ApplyPostCombatSustain(bool recoverHealth, int amount, List<CardData> rewards)
        {
            if (recoverHealth)
            {
                int before = playerHealth;
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + amount);
                AddLog($"전투 후 정비: 체력 +{playerHealth - before}.");
            }
            else
            {
                gold += amount;
                AddLog($"전투 후 정비: 금화 +{amount}.");
            }

            ShowReward(rewards);
        }

        private void ShowTenDoorClearChoice()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(rewardBackground != null ? rewardBackground : bossBackground);
            ClearContent();
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0.090f, 0.135f), new Vector2(0.910f, 0.855f));
            primaryButton.gameObject.SetActive(false);
            subtitleText.text = "열 번째 문 너머에서 다음 목표를 선택하세요";
            SetSubtitleBoxVisible(true);

            RectTransform panel = AddPanel(contentRoot, "10문 이후 선택", new Color(1f, 1f, 1f, 0.88f), statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite);
            SetAnchors(panel, new Vector2(0.000f, 0.000f), new Vector2(1.000f, 0.840f));

            RectTransform titleBox = AddRunStatusLabelBox(
                contentRoot,
                "10문 클리어 제목 박스",
                $"{GetDifficultyName(currentDifficulty)} 10문 클리어",
                new Vector2(0.265f, 0.870f),
                new Vector2(0.735f, 1.000f),
                34);
            titleBox.SetAsLastSibling();

            string recordText = $"현재 빚 {debt} / 금화 {gold} / 무한 최고 기록 {GetEndlessRecord()}문";
            Text body = AddText(panel, "10문 클리어 설명", recordText, 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.84f, 0.76f, 1f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 16;
            body.resizeTextMaxSize = 22;
            SetAnchors(body.rectTransform, new Vector2(0.135f, 0.735f), new Vector2(0.865f, 0.820f));

            bool canEnterEndless = currentDifficulty == RunDifficulty.Hard;
            Vector2 returnMin = canEnterEndless ? new Vector2(0.100f, 0.515f) : new Vector2(0.100f, 0.470f);
            Vector2 returnMax = canEnterEndless ? new Vector2(0.900f, 0.650f) : new Vector2(0.900f, 0.620f);
            Vector2 clearDebtMin = canEnterEndless ? new Vector2(0.100f, 0.320f) : new Vector2(0.100f, 0.280f);
            Vector2 clearDebtMax = canEnterEndless ? new Vector2(0.900f, 0.455f) : new Vector2(0.900f, 0.430f);

            AddPostTenChoice(panel, "귀환한다", "현재 런을 성공으로 마무리하고 다음 난이도를 해금합니다.", returnMin, returnMax, CompleteReturnEnding, true);
            int debtClearCost = GetDebtClearCost();
            string debtText = debtClearCost > 0
                ? $"금화 {debtClearCost}로 모든 빚을 청산하고 직업별 진엔딩을 영구 해금합니다."
                : "빚이 없습니다. 직업별 진엔딩을 영구 해금합니다.";
            AddPostTenChoice(panel, "빚을 청산한다", debtText, clearDebtMin, clearDebtMax, BeginDebtClearBossBattle, CanClearDebt());
            if (canEnterEndless)
            {
                AddPostTenChoice(panel, "더 깊은 문으로 간다", $"무한 기록 모드에 진입합니다. 이후 {EndlessBossInterval}문마다 심연의 고리대금업자가 등장합니다.", new Vector2(0.100f, 0.125f), new Vector2(0.900f, 0.260f), EnterEndlessMode, true);
            }

            RefreshTopBar();
        }

        private void AddPostTenChoice(RectTransform parent, string title, string description, Vector2 anchorMin, Vector2 anchorMax, UnityAction action, bool interactable)
        {
            RectTransform rootPanel = AddPanel(parent, $"{title} 선택지", new Color(1f, 1f, 1f, 0f));
            SetAnchors(rootPanel, anchorMin, anchorMax);

            Button button = AddClassDetailButton(rootPanel, $"{title} 버튼", title, classConfirmButtonSprite, 24);
            SetAnchors(button.GetComponent<RectTransform>(), new Vector2(0.000f, 0.150f), new Vector2(0.300f, 0.850f));
            button.interactable = interactable;
            if (interactable)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                button.GetComponent<Image>().color = new Color(0.42f, 0.42f, 0.42f, 0.74f);
            }

            Text detail = AddText(rootPanel, $"{title} 설명", description, 20, TextAnchor.MiddleLeft, interactable ? new Color(0.88f, 0.84f, 0.76f, 1f) : new Color(0.62f, 0.58f, 0.52f, 1f));
            detail.resizeTextForBestFit = true;
            detail.resizeTextMinSize = 14;
            detail.resizeTextMaxSize = 20;
            SetAnchors(detail.rectTransform, new Vector2(0.335f, 0.140f), new Vector2(0.965f, 0.860f));
        }

        private void CompleteReturnEnding()
        {
            if (currentDifficulty == RunDifficulty.Hard)
            {
                UnlockSurvivorTitleForSelectedClass();
            }

            UnlockNextDifficultyFromCurrentRun();
            currentJourneyEndingKind = JourneyEndingKind.Return;
            ShowJourneyEnding();
        }

        private int GetDebtClearCost()
        {
            return Mathf.Max(0, debt) * DebtClearGoldCostPerDebt;
        }

        private bool CanClearDebt()
        {
            int cost = GetDebtClearCost();
            return cost <= 0 || gold >= cost;
        }

        private void BeginDebtClearBossBattle()
        {
            int cost = GetDebtClearCost();
            if (cost > 0)
            {
                if (gold < cost)
                {
                    AddLog("빚을 청산하기에 금화가 부족합니다.");
                    ShowTenDoorClearChoice();
                    return;
                }

                gold -= cost;
                debt = 0;
            }

            AddLog("모든 빚을 청산하려는 순간, 마지막 채권자가 문 너머에서 모습을 드러냅니다.");
            RefreshTopBar();
            StartCombat(CreateDebtClearBoss());
        }

        private void CompleteDebtClearBossVictory()
        {
            UnlockNextDifficultyFromCurrentRun();
            UnlockTrueEndingForSelectedClass();
            currentJourneyEndingKind = JourneyEndingKind.TrueDebtCleared;
            ShowJourneyEnding();
        }

        private void EnterEndlessMode()
        {
            endlessModeActive = true;
            endlessBossesDefeated = 0;
            nextEndlessBossRoom = roomsCleared + EndlessBossInterval;
            playerHealth = Mathf.Min(playerMaxHealth, Mathf.Max(playerHealth, Mathf.CeilToInt(playerMaxHealth * 0.70f)));
            RecordEndlessProgress();
            SetLogVisible(true);
            AddLog($"무한 기록 모드 진입. 다음 심연 보스는 {nextEndlessBossRoom}번째 문 뒤에 나타납니다.");
            ShowDoors();
        }

        private void CompleteEndlessBoss()
        {
            endlessBossesDefeated += 1;
            int rewardGold = GetRunItemAdjustedCombatGoldReward(36 + endlessBossesDefeated * 8 + debt * 2, true);
            gold += rewardGold;
            ApplyRunItemVictorySideEffects(true);
            playerHealth = Mathf.Min(playerMaxHealth, playerHealth + Mathf.Max(10, playerMaxHealth / 5));
            AddLog($"심연의 고리대금업자 격파. 금화 +{rewardGold}. 무한 기록 {roomsCleared}문.");
            RecordEndlessProgress();
            nextEndlessBossRoom = roomsCleared + EndlessBossInterval;
            ShowEndlessCheckpoint();
        }

        private void ShowEndlessCheckpoint()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(rewardBackground != null ? rewardBackground : bossBackground);
            ClearContent();
            SetLogVisible(false);
            SetAnchors(contentRoot, new Vector2(0.120f, 0.160f), new Vector2(0.880f, 0.835f));
            primaryButton.gameObject.SetActive(false);
            subtitleText.text = "무한 기록 체크포인트";
            SetSubtitleBoxVisible(true);

            RectTransform panel = AddPanel(contentRoot, "무한 체크포인트", new Color(1f, 1f, 1f, 0.88f), statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite);
            SetAnchors(panel, Vector2.zero, Vector2.one);

            Text title = AddText(panel, "무한 체크포인트 제목", $"무한 기록 {roomsCleared}문", 36, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.54f, 1f));
            title.fontStyle = FontStyle.Bold;
            AddTextGlow(title, new Color(0f, 0f, 0f, 0.94f), new Color(0.12f, 0.78f, 0.76f, 0.46f), new Vector2(2.5f, -2.8f));
            SetAnchors(title.rectTransform, new Vector2(0.08f, 0.750f), new Vector2(0.92f, 0.900f));

            Text detail = AddText(panel, "무한 체크포인트 설명", $"최고 기록 {GetEndlessRecord()}문 / 다음 심연 보스 {nextEndlessBossRoom}문 / 현재 빚 {debt}", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.84f, 0.76f, 1f));
            SetAnchors(detail.rectTransform, new Vector2(0.10f, 0.610f), new Vector2(0.90f, 0.720f));

            AddPostTenChoice(panel, "계속 내려간다", "기록을 이어갑니다. 적은 더 강해지고 보상도 조금씩 커집니다.", new Vector2(0.070f, 0.405f), new Vector2(0.930f, 0.575f), ShowDoors, true);
            AddPostTenChoice(panel, "기록하고 귀환한다", "현재 무한 기록을 보존하고 런을 성공으로 마무리합니다.", new Vector2(0.070f, 0.220f), new Vector2(0.930f, 0.390f), CompleteEndlessReturnEnding, true);
            AddPostTenChoice(panel, "빚을 청산한다", GetDebtClearCost() > 0 ? $"금화 {GetDebtClearCost()}로 마지막 채권자에게 도전합니다." : "빚 없이 마지막 채권자에게 도전합니다.", new Vector2(0.070f, 0.035f), new Vector2(0.930f, 0.205f), BeginDebtClearBossBattle, CanClearDebt());
        }

        private void CompleteEndlessReturnEnding()
        {
            RecordEndlessProgress();
            UnlockNextDifficultyFromCurrentRun();
            currentJourneyEndingKind = JourneyEndingKind.EndlessReturn;
            ShowJourneyEnding();
        }

        private void UnlockTrueEndingForSelectedClass()
        {
            PlayerPrefs.SetInt(GetTrueEndingKey(selectedClass), 1);
            PlayerPrefs.Save();
            AddLog($"{GetClassName(selectedClass)} 진엔딩이 영구 해금되었습니다.");
        }

        private static string GetTrueEndingKey(CharacterClass characterClass)
        {
            return $"{TrueEndingUnlockPrefix}{characterClass}";
        }

        private bool IsTrueEndingUnlocked(CharacterClass characterClass)
        {
            return PlayerPrefs.GetInt(GetTrueEndingKey(characterClass), 0) == 1;
        }

        private void UnlockSurvivorTitleForSelectedClass()
        {
            if (IsSurvivorTitleUnlocked(selectedClass))
            {
                return;
            }

            PlayerPrefs.SetInt(GetSurvivorTitleKey(selectedClass), 1);
            PlayerPrefs.Save();
            AddLog($"{GetClassName(selectedClass)}에게 생존 귀환 칭호가 새겨졌습니다.");
        }

        private static string GetSurvivorTitleKey(CharacterClass characterClass)
        {
            return $"{SurvivorTitleUnlockPrefix}{characterClass}";
        }

        private bool IsSurvivorTitleUnlocked(CharacterClass characterClass)
        {
            return PlayerPrefs.GetInt(GetSurvivorTitleKey(characterClass), 0) == 1;
        }

        private string GetEndlessRecordKey()
        {
            return GetEndlessRecordKey(selectedClass, currentDifficulty);
        }

        private static string GetEndlessRecordKey(CharacterClass characterClass, RunDifficulty difficulty)
        {
            return $"{EndlessRecordPrefix}{characterClass}.{difficulty}";
        }

        private int GetEndlessRecord()
        {
            return PlayerPrefs.GetInt(GetEndlessRecordKey(), 0);
        }

        private static int GetEndlessRecord(CharacterClass characterClass, RunDifficulty difficulty)
        {
            return PlayerPrefs.GetInt(GetEndlessRecordKey(characterClass, difficulty), 0);
        }

        private string GetMainMenuEndlessRecordText()
        {
            CharacterClass[] classes = { CharacterClass.Gambler, CharacterClass.Oracle, CharacterClass.Exile };
            List<string> lines = new();
            foreach (CharacterClass characterClass in classes)
            {
                if (TryGetBestEndlessRecord(characterClass, out RunDifficulty difficulty, out int record))
                {
                    lines.Add($"{GetClassName(characterClass)}  {GetDifficultyName(difficulty)}  {record}문");
                }
            }

            if (lines.Count > 0)
            {
                return string.Join("\n", lines);
            }

            return PlayerPrefs.GetInt(EndlessRecordSeenKey, 0) == 1 ? "아직 무한 기록 없음" : string.Empty;
        }

        private static bool TryGetBestEndlessRecord(CharacterClass characterClass, out RunDifficulty bestDifficulty, out int bestRecord)
        {
            RunDifficulty[] difficulties = { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard };
            bestDifficulty = RunDifficulty.Easy;
            bestRecord = 0;
            foreach (RunDifficulty difficulty in difficulties)
            {
                int record = GetEndlessRecord(characterClass, difficulty);
                if (record > bestRecord || (record == bestRecord && record > 0 && (int)difficulty > (int)bestDifficulty))
                {
                    bestRecord = record;
                    bestDifficulty = difficulty;
                }
            }

            return bestRecord > 0;
        }

        private void RecordEndlessProgress()
        {
            bool changed = PlayerPrefs.GetInt(EndlessRecordSeenKey, 0) == 0;
            if (changed)
            {
                PlayerPrefs.SetInt(EndlessRecordSeenKey, 1);
            }

            int currentRecord = GetEndlessRecord();
            if (roomsCleared > currentRecord)
            {
                PlayerPrefs.SetInt(GetEndlessRecordKey(), roomsCleared);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        private void ShowReward(List<CardData> rewards)
        {
            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(rewardBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            int maxDeckSize = GetMaxDeckSize();
            if (!CanAddCardToDeck())
            {
                subtitleText.text = $"덱 한도 {deck.Count}/{maxDeckSize}";
                SetSubtitleBoxVisible(true);
                AddCenteredMessage("덱이 가득 찼습니다", endlessModeActive
                    ? "무한 모드 덱 한도 60장에 도달했습니다.\n카드 보상을 받을 수 없습니다."
                    : "덱 한도 50장에 도달했습니다.\n무한 모드에 진입하면 한도가 60장으로 늘어납니다.");
                ShowContinueButton();
                RefreshTopBar();
                RefreshLog();
                return;
            }

            subtitleText.text = $"카드 보상 {rewards.Count}장 중 1장을 선택하세요";
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(true);
            SetButtonLabel(primaryButton, "건너뛰기");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(ShowDoors);

            int rewardCount = Mathf.Max(1, rewards.Count);
            float cardWidth = rewardCount >= 4 ? 0.238f : 0.318f;
            float cardGap = rewardCount >= 4 ? 0.010f : 0.013f;
            float totalWidth = rewardCount * cardWidth + (rewardCount - 1) * cardGap;
            float startLeft = Mathf.Max(0.005f, (1f - totalWidth) * 0.5f);
            for (int i = 0; i < rewards.Count; i += 1)
            {
                CardData card = rewards[i];
                RectTransform slot = AddPanel(contentRoot, $"보상 {i}", new Color(1f, 1f, 1f, 0f));
                float left = startLeft + i * (cardWidth + cardGap);
                SetAnchors(slot, new Vector2(left, 0.040f), new Vector2(left + cardWidth, 0.940f));

                Button button = CreateCardButton(slot, card, 0, 1, false, false);
                Stretch(button.GetComponent<RectTransform>());
                button.onClick.AddListener(() =>
                {
                    if (TryAddCardToDeck(card, "카드 보상"))
                    {
                        AddLog($"{card.DisplayName} 획득.");
                        CheckBuildUnlocks();
                    }

                    ShowDoors();
                });
            }

            RefreshTopBar();
            RefreshLog();
        }

        private List<CardData> PickCombatRewards(int count, bool eliteReward)
        {
            List<CardSource> sources = new() { CardSource.CombatReward };
            if (eliteReward || roomsCleared >= 7)
            {
                sources.Add(CardSource.BossReward);
            }

            if (IsHardModeFeatureActive())
            {
                sources.Add(CardSource.HardReward);
            }

            List<CardData> rewards = PickWeightedCards(count, sources);
            if (ShouldGuaranteeHardReward(eliteReward))
            {
                EnsureHardReward(rewards);
            }

            return rewards;
        }

        private List<CardData> PickShopCards(int count)
        {
            List<CardSource> sources = new() { CardSource.ShopOnly, CardSource.CombatReward };
            if (IsHardModeFeatureActive())
            {
                sources.Add(CardSource.HardReward);
            }

            List<CardData> cards = PickWeightedCards(count, sources);
            if (IsHardModeFeatureActive() && Random.value <= 0.45f)
            {
                EnsureHardReward(cards);
            }

            return cards;
        }

        private CardData PickEventCard()
        {
            return PickWeightedCards(1, new[] { CardSource.EventOnly, CardSource.ShopOnly }).FirstOrDefault()
                ?? PickWeightedCards(1, new[] { CardSource.CombatReward }).FirstOrDefault();
        }

        private CardData PickTreasureCard()
        {
            List<CardSource> sources = new() { CardSource.CombatReward, CardSource.ShopOnly, CardSource.BossReward };
            if (IsHardModeFeatureActive())
            {
                sources.Add(CardSource.HardReward);
            }

            return PickWeightedCards(1, sources).FirstOrDefault();
        }

        private bool ShouldGuaranteeHardReward(bool eliteReward)
        {
            return IsHardModeFeatureActive()
                && (eliteReward || endlessModeActive || roomsCleared >= 7);
        }

        private void EnsureHardReward(List<CardData> rewards)
        {
            if (rewards == null || rewards.Count == 0 || rewards.Any(card => card != null && card.Source == CardSource.HardReward))
            {
                return;
            }

            CardData hardCard = PickWeightedCards(1, new[] { CardSource.HardReward }).FirstOrDefault();
            if (hardCard == null || rewards.Any(card => card.CardId == hardCard.CardId))
            {
                return;
            }

            rewards[^1] = hardCard;
        }

        private List<CardData> PickWeightedCards(int count, IEnumerable<CardSource> sources)
        {
            HashSet<CardSource> allowedSources = new(sources);
            List<CardData> candidates = cardPool
                .Where(card => IsCardEligible(card, allowedSources))
                .ToList();

            List<CardData> picks = new();
            BuildRecipe recipe = GetCurrentBuildRecipe();
            foreach (string cardId in recipe.RequiredCardIds.OrderBy(_ => Random.value))
            {
                if (picks.Count >= count || HasDeckCard(cardId))
                {
                    continue;
                }

                CardData buildCard = candidates.FirstOrDefault(card => card.CardId == cardId);
                if (buildCard != null && Random.value <= 0.74f)
                {
                    picks.Add(buildCard);
                }
            }

            while (picks.Count < count && candidates.Count > 0)
            {
                CardData selected = PickWeightedCard(candidates, picks);
                if (selected == null)
                {
                    break;
                }

                picks.Add(selected);
            }

            return picks;
        }

        private bool IsCardEligible(CardData card, HashSet<CardSource> allowedSources)
        {
            if (card == null || card.Rarity == CardRarity.Curse || !allowedSources.Contains(card.Source))
            {
                return false;
            }

            if (card.Source == CardSource.HardReward && !IsHardModeFeatureActive())
            {
                return false;
            }

            if (card.MinimumRoom > roomsCleared)
            {
                return false;
            }

            return card.CharacterClass == CharacterClass.Any || card.CharacterClass == selectedClass;
        }

        private CardData PickWeightedCard(IReadOnlyList<CardData> candidates, IReadOnlyList<CardData> existingPicks)
        {
            List<(CardData Card, int Weight)> weighted = candidates
                .Where(card => existingPicks.All(pick => pick.CardId != card.CardId))
                .Select(card => (Card: card, Weight: GetRewardWeight(card)))
                .Where(entry => entry.Weight > 0)
                .ToList();

            if (weighted.Count == 0)
            {
                return null;
            }

            int totalWeight = weighted.Sum(entry => entry.Weight);
            int roll = Random.Range(0, totalWeight);
            foreach ((CardData card, int weight) in weighted)
            {
                if (roll < weight)
                {
                    return card;
                }

                roll -= weight;
            }

            return weighted[^1].Card;
        }

        private int GetRewardWeight(CardData card)
        {
            int weight = card.Source == CardSource.ShopOnly ? card.ShopWeight : 1;
            weight += card.Rarity == CardRarity.Rare ? 1 : 0;
            if (HasRunItem("curse_sealed_debt_scroll") && card.Rarity == CardRarity.Rare)
            {
                weight += 3;
            }

            weight += card.CharacterClass == selectedClass ? 2 : 0;
            if (card.Source == CardSource.HardReward)
            {
                weight += currentDifficulty == RunDifficulty.Hard ? 2 : 0;
                weight += endlessModeActive ? 3 : 0;
            }

            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (recipe.RequiredCardIds.Contains(card.CardId) && !HasDeckCard(card.CardId))
            {
                weight += 5;
            }

            IReadOnlyList<BuildTag> preferredTags = GetPreferredBuildTags(recipe.Id);
            weight += card.BuildTags.Count(tag => preferredTags.Contains(tag));
            return Mathf.Max(1, weight);
        }

        private void ShowShop()
        {
            EnsureCurrentShopOffers();
            PlayNonCombatMusic();
            phase = GamePhase.Shop;
            SetBackground(shopBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = "상점: 금화로 덱을 비틀 수 있습니다";
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(true);
            SetButtonLabel(primaryButton, "상점 나가기");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(() =>
            {
                ResetCurrentShopOffers();
                ShowDoors();
            });

            CreateBuildShopPanel();

            for (int i = 0; i < currentShopCards.Count; i += 1)
            {
                if (purchasedShopCardSlots.Contains(i))
                {
                    AddShopSoldSlot(i, "구매 완료", "이미 덱에 넣은 카드입니다.");
                    continue;
                }

                CardData card = currentShopCards[i];
                int price = GetRunItemAdjustedShopPrice(24 + card.Cost * 8 + debt * 4);
                RectTransform slot = AddShopOfferSlotRoot($"상품 {i}", i);
                int cardSlotIndex = i;

                UnityAction purchase = () =>
                {
                    if (!CanAddCardToDeck())
                    {
                        AddLog($"상점: 덱 한도 {deck.Count}/{GetMaxDeckSize()}에 도달했습니다.");
                        return;
                    }

                    if (gold < price)
                    {
                        AddLog($"{card.DisplayName}: 금화 부족.");
                        return;
                    }

                    gold -= price;
                    if (TryAddCardToDeck(card, "상점"))
                    {
                        AddLog($"{card.DisplayName} 구매.");
                        CheckBuildUnlocks();
                    }

                    purchasedShopCardSlots.Add(cardSlotIndex);
                    ShowShop();
                };

                Button cardButton = CreateCardButton(slot, card, 0, 1);
                SetAnchors(cardButton.GetComponent<RectTransform>(), new Vector2(0.000f, 0.165f), new Vector2(1.000f, 1.000f));
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(purchase);

                Button buy = AddShopActionButton(slot, "구매", $"구매 {price}금", 16);
                SetAnchors(buy.GetComponent<RectTransform>(), new Vector2(0.050f, 0.018f), new Vector2(0.950f, 0.165f));
                bool canBuyCard = gold >= price && CanAddCardToDeck();
                cardButton.interactable = CanAddCardToDeck();
                buy.interactable = canBuyCard;
                buy.onClick.AddListener(purchase);
            }

            if (!string.IsNullOrWhiteSpace(currentShopRunItemId))
            {
                int itemSlotIndex = Mathf.Clamp(currentShopCards.Count, 0, 2);
                if (currentShopRunItemPurchased)
                {
                    AddShopSoldSlot(itemSlotIndex, "구매 완료", "이미 획득한 아이템입니다.");
                }
                else
                {
                    AddShopRunItemSlot(GetRunItemDefinition(currentShopRunItemId), itemSlotIndex);
                }
            }

            RefreshTopBar();
            RefreshLog();
        }

        private void EnsureCurrentShopOffers()
        {
            if (currentShopOffersReady)
            {
                return;
            }

            currentShopCards.Clear();
            purchasedShopCardSlots.Clear();
            currentShopRunItemId = string.Empty;
            currentShopRunItemPurchased = false;

            RunItemDefinition shopRunItem = PickShopRunItemOffer();
            currentShopCards.AddRange(PickShopCards(shopRunItem == null ? 3 : 2));
            currentShopRunItemId = shopRunItem?.Id ?? string.Empty;
            currentShopOffersReady = true;
        }

        private void ResetCurrentShopOffers()
        {
            currentShopCards.Clear();
            purchasedShopCardSlots.Clear();
            currentShopRunItemId = string.Empty;
            currentShopRunItemPurchased = false;
            currentShopOffersReady = false;
        }

        private RectTransform AddShopOfferSlotRoot(string name, int slotIndex)
        {
            RectTransform slot = AddPanel(contentRoot, name, new Color(1f, 1f, 1f, 0f));
            slot.GetComponent<Image>().raycastTarget = false;
            const float cardSlotWidth = 0.240f;
            const float cardSlotGap = 0.008f;
            float slotLeft = 0.255f + slotIndex * (cardSlotWidth + cardSlotGap);
            SetAnchors(slot, new Vector2(slotLeft, 0.052f), new Vector2(slotLeft + cardSlotWidth, 0.885f));
            return slot;
        }

        private void AddShopSoldSlot(int slotIndex, string title, string description)
        {
            RectTransform slot = AddShopOfferSlotRoot($"판매 완료 {slotIndex}", slotIndex);
            RectTransform panel = AddPanel(slot, "판매 완료 프레임", Color.white, GetRunStatusSlotFrameSprite());
            SetAnchors(panel, new Vector2(0.000f, 0.165f), new Vector2(1.000f, 1.000f));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.type = Image.Type.Simple;
            panelImage.raycastTarget = false;
            panelImage.color = new Color(0.56f, 0.56f, 0.56f, 0.72f);

            Text titleText = AddText(panel, "판매 완료 제목", title, 22, TextAnchor.MiddleCenter, new Color(0.72f, 0.92f, 0.88f, 0.95f));
            titleText.fontStyle = FontStyle.Bold;
            AddTextGlow(titleText, new Color(0f, 0f, 0f, 0.88f), new Color(0.05f, 0.40f, 0.38f, 0.32f), new Vector2(1.2f, -1.4f));
            SetAnchors(titleText.rectTransform, new Vector2(0.100f, 0.520f), new Vector2(0.900f, 0.655f));

            Text descText = AddText(panel, "판매 완료 설명", description, 15, TextAnchor.MiddleCenter, new Color(0.72f, 0.70f, 0.64f, 0.92f));
            descText.resizeTextMinSize = 10;
            descText.resizeTextMaxSize = 15;
            SetAnchors(descText.rectTransform, new Vector2(0.105f, 0.355f), new Vector2(0.895f, 0.490f));

            Button disabled = AddShopActionButton(slot, "판매 완료 버튼", "구매 완료", 16);
            SetAnchors(disabled.GetComponent<RectTransform>(), new Vector2(0.050f, 0.018f), new Vector2(0.950f, 0.165f));
            disabled.interactable = false;
        }

        private RunItemDefinition PickShopRunItemOffer()
        {
            if (Random.value > ShopRunItemOfferChance)
            {
                return null;
            }

            return PickUnlockedRunItem();
        }

        private void AddShopRunItemSlot(RunItemDefinition item, int slotIndex)
        {
            if (item == null)
            {
                return;
            }

            int price = GetRunItemAdjustedShopPrice(GetRunItemShopPrice(item.Type));
            RectTransform slot = AddShopOfferSlotRoot($"아이템 상품 {slotIndex}", slotIndex);

            RectTransform panel = AddPanel(slot, "아이템 상품 프레임", Color.white, GetRunStatusSlotFrameSprite());
            SetAnchors(panel, new Vector2(0.000f, 0.165f), new Vector2(1.000f, 1.000f));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.type = Image.Type.Simple;
            panelImage.raycastTarget = true;

            Button itemButton = panel.gameObject.AddComponent<Button>();
            itemButton.targetGraphic = panelImage;
            itemButton.colors = CreateButtonColors();

            Sprite icon = GetRunItemIcon(item);
            if (icon != null)
            {
                Image itemIcon = AddImage(panel, "아이템 상품 아이콘", Color.white);
                itemIcon.sprite = icon;
                itemIcon.preserveAspect = true;
                itemIcon.raycastTarget = false;
                SetAnchors(itemIcon.rectTransform, new Vector2(0.185f, 0.490f), new Vector2(0.815f, 0.905f));
            }

            Text typeText = AddText(panel, "아이템 상품 종류", GetRunItemTypeName(item.Type), 18, TextAnchor.MiddleCenter, new Color(0.74f, 1f, 0.94f, 1f));
            typeText.fontStyle = FontStyle.Bold;
            AddTextGlow(typeText, new Color(0f, 0f, 0f, 0.90f), new Color(0.08f, 0.62f, 0.58f, 0.46f), new Vector2(1.2f, -1.4f));
            SetAnchors(typeText.rectTransform, new Vector2(0.110f, 0.390f), new Vector2(0.890f, 0.465f));

            Text nameText = AddText(panel, "아이템 상품 이름", item.Name, 19, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.70f, 1f));
            nameText.fontStyle = FontStyle.Bold;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 12;
            nameText.resizeTextMaxSize = 19;
            AddTextGlow(nameText, new Color(0f, 0f, 0f, 0.92f), new Color(0.08f, 0.62f, 0.58f, 0.36f), new Vector2(1.2f, -1.4f));
            SetAnchors(nameText.rectTransform, new Vector2(0.090f, 0.290f), new Vector2(0.910f, 0.375f));

            Text effectText = AddText(panel, "아이템 상품 효과", item.Effect, 13, TextAnchor.UpperCenter, new Color(0.86f, 0.82f, 0.72f, 1f));
            effectText.resizeTextForBestFit = true;
            effectText.resizeTextMinSize = 9;
            effectText.resizeTextMaxSize = 13;
            SetAnchors(effectText.rectTransform, new Vector2(0.095f, 0.055f), new Vector2(0.905f, 0.270f));

            UnityAction purchase = () =>
            {
                if (gold < price)
                {
                    AddLog($"{item.Name}: 금화 부족.");
                    return;
                }

                gold -= price;
                currentShopRunItemPurchased = true;
                AddLog($"{GetRunItemTypeName(item.Type)} 구매: {item.Name}.");
                ShowRunItemReward(item, ShowShop);
            };

            itemButton.onClick.AddListener(purchase);
            Button buy = AddShopActionButton(slot, "아이템 구매", $"구매 {price}금", 16);
            SetAnchors(buy.GetComponent<RectTransform>(), new Vector2(0.050f, 0.018f), new Vector2(0.950f, 0.165f));
            buy.interactable = gold >= price;
            buy.onClick.AddListener(purchase);
        }

        private static int GetRunItemShopPrice(RunItemType type)
        {
            return type switch
            {
                RunItemType.Blessing => 92,
                RunItemType.Curse => 64,
                _ => 78
            };
        }

        private void CreateBuildShopPanel()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            bool unlocked = IsBuildUnlocked(recipe);
            int level = GetBuildUpgradeLevel(recipe.Id);
            int nextCost = GetRunItemAdjustedShopPrice(GetBuildUpgradeCost(level));

            RectTransform panel = AddPanel(
                contentRoot,
                "상점 조합 정보",
                Color.white,
                logPanelFrameSprite != null
                    ? logPanelFrameSprite
                    : shopCombinationPanelFrameSprite != null
                        ? shopCombinationPanelFrameSprite
                        : statusSectionTallFrameSprite != null
                            ? statusSectionTallFrameSprite
                            : panelSprite);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.type = Image.Type.Simple;
            panelImage.raycastTarget = false;
            SetAnchors(panel, new Vector2(0.010f, 0.000f), new Vector2(0.238f, 0.850f));
            AddShopLabelBox(
                contentRoot,
                "조합 상점 제목 박스",
                "조합 상점",
                new Vector2(0.030f, 0.880f),
                new Vector2(0.218f, 0.965f),
                20);

            string status = unlocked
                ? level >= recipe.MaxUpgradeLevel
                    ? $"완성됨\n강화 최대 {level}/{recipe.MaxUpgradeLevel}"
                    : $"완성됨\n강화 {level}/{recipe.MaxUpgradeLevel}\n다음 {nextCost}금"
                : $"필요 카드\n{WrapDisplayLine(GetMissingBuildCardNames(recipe), 12, string.Empty)}";
            string upgradeHintText = unlocked
                ? level >= recipe.MaxUpgradeLevel
                    ? "강화 최대 단계입니다"
                    : "강화하면 직업 조합 효과가 1단계 오릅니다"
                : "필요 카드를 모으면\n강화가 열립니다";

            AddShopPanelText(panel, "직업 조합 제목", "직업 조합", 15, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.70f, 1f), new Vector2(0.120f, 0.785f), new Vector2(0.880f, 0.835f), true);
            AddShopPanelText(panel, "직업 조합 이름", recipe.Name, 18, TextAnchor.MiddleCenter, Color.white, new Vector2(0.095f, 0.710f), new Vector2(0.905f, 0.775f), true);
            AddShopPanelText(panel, "직업 조합 상태", status, 13, TextAnchor.MiddleCenter, new Color(0.84f, 0.96f, 0.90f, 1f), new Vector2(0.100f, 0.575f), new Vector2(0.900f, 0.695f), false);
            AddShopPanelText(panel, "강화 설명", upgradeHintText, 12, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.72f, 1f), new Vector2(0.100f, 0.455f), new Vector2(0.900f, 0.545f), false);

            string label = unlocked
                ? level >= recipe.MaxUpgradeLevel ? "최대 강화" : $"강화 {nextCost}금"
                : "조합 미완성";
            Button upgrade = AddShopActionButton(panel, "빌드 강화 버튼", label, 14);
            SetAnchors(upgrade.GetComponent<RectTransform>(), new Vector2(0.110f, 0.320f), new Vector2(0.890f, 0.430f));

            AddShopPanelText(panel, "카드 조합 요약", BuildCombinationOverviewText(), 12, TextAnchor.MiddleCenter, new Color(0.82f, 0.98f, 0.92f, 1f), new Vector2(0.095f, 0.190f), new Vector2(0.905f, 0.295f), false);

            Button combinationGuide = AddShopActionButton(panel, "카드 조합법 버튼", "카드 조합법", 14);
            SetAnchors(combinationGuide.GetComponent<RectTransform>(), new Vector2(0.110f, 0.060f), new Vector2(0.890f, 0.170f));
            combinationGuide.onClick.AddListener(ShowShopCombinationGuide);

            upgrade.interactable = unlocked && level < recipe.MaxUpgradeLevel && gold >= nextCost;
            upgrade.onClick.AddListener(() =>
            {
                int currentLevel = GetBuildUpgradeLevel(recipe.Id);
                int cost = GetRunItemAdjustedShopPrice(GetBuildUpgradeCost(currentLevel));
                if (!IsBuildUnlocked(recipe) || currentLevel >= recipe.MaxUpgradeLevel || gold < cost)
                {
                    return;
                }

                gold -= cost;
                buildUpgradeLevels[recipe.Id] = currentLevel + 1;
                AddLog($"{recipe.Name} 강화 {currentLevel + 1}단계.");
                ShowShop();
            });
        }

        private void ShowShopCombinationGuide()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Shop;
            SetBackground(shopBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = "상점: 카드 조합 효과를 확인합니다";
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(true);
            SetButtonLabel(primaryButton, "상점으로");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(ShowShop);

            RectTransform detailRoot = AddPanel(contentRoot, "상점 카드 조합법 루트", new Color(1f, 1f, 1f, 0f));
            Stretch(detailRoot);

            Sprite detailSprite = statusPanelFrameSprite != null
                ? statusPanelFrameSprite
                : eventMessageFrameSprite != null
                    ? eventMessageFrameSprite
                    : panelSprite;
            RectTransform detailWindow = AddPanel(detailRoot, "상점 카드 조합법 창", Color.white, detailSprite);
            Image detailWindowImage = detailWindow.GetComponent<Image>();
            detailWindowImage.type = Image.Type.Simple;
            detailWindowImage.raycastTarget = false;
            detailWindow.gameObject.AddComponent<RectMask2D>();
            SetAnchors(detailWindow, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.885f));

            AddRunStatusLabelBox(
                detailRoot,
                "상점 카드 조합법 제목 박스",
                "카드 조합법",
                new Vector2(0.325f, 0.895f),
                new Vector2(0.675f, 0.995f),
                26);

            AddRunStatusTextButton(
                detailRoot,
                "상점 카드 조합법 닫기",
                "닫기",
                new Vector2(0.835f, 0.905f),
                new Vector2(0.955f, 0.985f),
                ShowShop,
                18);

            IReadOnlyList<CombinationRecipe> orderedRecipes = GetOrderedCombinationRecipesForDisplay();
            int completedCount = orderedRecipes.Count(IsCombinationComplete);
            int nearlyCompleteCount = orderedRecipes.Count(recipe => !IsCombinationComplete(recipe) && GetCombinationOwnedCount(recipe) >= recipe.RequiredCardIds.Count - 1);

            Text summary = AddText(
                detailWindow,
                "카드 조합법 요약",
                $"완성 {completedCount}/{orderedRecipes.Count}   거의 완성 {nearlyCompleteCount}\n공격/방어/특수 카드 3장을 모으면 전투 중 효과가 발동합니다.",
                18,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.90f, 0.70f, 1f));
            summary.fontStyle = FontStyle.Bold;
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 14;
            summary.resizeTextMaxSize = 18;
            summary.lineSpacing = 1.02f;
            AddTextGlow(summary, new Color(0f, 0f, 0f, 0.84f), new Color(0.06f, 0.48f, 0.48f, 0.38f), new Vector2(1.2f, -1.4f));
            SetAnchors(summary.rectTransform, new Vector2(0.120f, 0.680f), new Vector2(0.880f, 0.790f));

            RectTransform listRoot = AddPanel(detailWindow, "카드 조합법 목록 안전영역", new Color(1f, 1f, 1f, 0f));
            listRoot.gameObject.AddComponent<RectMask2D>();
            SetAnchors(listRoot, new Vector2(0.105f, 0.155f), new Vector2(0.895f, 0.655f));

            int splitIndex = Mathf.CeilToInt(orderedRecipes.Count / 2f);
            Text leftColumn = AddText(
                listRoot,
                "카드 조합법 왼쪽 목록",
                BuildShopCombinationColumnText(orderedRecipes.Take(splitIndex)),
                16,
                TextAnchor.UpperLeft,
                new Color(0.87f, 0.96f, 0.90f, 1f));
            leftColumn.resizeTextMinSize = 12;
            leftColumn.resizeTextMaxSize = 16;
            leftColumn.lineSpacing = 1.00f;
            AddTextGlow(leftColumn, new Color(0f, 0f, 0f, 0.74f), new Color(0.04f, 0.34f, 0.33f, 0.28f), new Vector2(1.0f, -1.2f));
            SetAnchors(leftColumn.rectTransform, new Vector2(0.000f, 0.025f), new Vector2(0.492f, 0.985f));

            Text rightColumn = AddText(
                listRoot,
                "카드 조합법 오른쪽 목록",
                BuildShopCombinationColumnText(orderedRecipes.Skip(splitIndex)),
                16,
                TextAnchor.UpperLeft,
                new Color(0.87f, 0.96f, 0.90f, 1f));
            rightColumn.resizeTextMinSize = 12;
            rightColumn.resizeTextMaxSize = 16;
            rightColumn.lineSpacing = 1.00f;
            AddTextGlow(rightColumn, new Color(0f, 0f, 0f, 0.74f), new Color(0.04f, 0.34f, 0.33f, 0.28f), new Vector2(1.0f, -1.2f));
            SetAnchors(rightColumn.rectTransform, new Vector2(0.508f, 0.025f), new Vector2(1.000f, 0.985f));

            RefreshTopBar();
            RefreshLog();
        }

        private void ShowTreasure()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Treasure;
            SetBackground(treasureBackground != null ? treasureBackground : rewardBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            int rewardGold = Random.Range(24, 43);
            gold += rewardGold;
            CardData card = PickTreasureCard();
            bool cardAdded = false;
            if (card != null)
            {
                cardAdded = TryAddCardToDeck(card, "보물");
                if (cardAdded)
                {
                    CheckBuildUnlocks();
                }
            }
            subtitleText.text = string.Empty;
            AddCenteredMessage(
                "보물: 봉인된 상자가 열렸습니다",
                cardAdded
                    ? $"금화 +{rewardGold}\n카드 획득: {card.DisplayName}"
                    : card == null
                        ? $"금화 +{rewardGold}"
                        : $"금화 +{rewardGold}\n덱 한도 {deck.Count}/{GetMaxDeckSize()}로 카드 보상은 넘겼습니다.");
            ShowContinueButton();
            AddLog(cardAdded
                ? $"보물: 금화 {rewardGold}, {card.DisplayName}."
                : $"보물: 금화 {rewardGold}.");
            RefreshTopBar();
            RefreshLog();
        }

        private void ShowEvent()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Event;
            SetBackground(eventBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = string.Empty;
            primaryButton.gameObject.SetActive(false);

            RectTransform message = AddEventMessagePanel(contentRoot, "중개인 메시지");
            SetAnchors(message, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.76f));

            Text heading = AddText(message, "제목", "가려진 중개인이 조건을 내밉니다", 31, TextAnchor.MiddleCenter, Color.white);
            heading.fontStyle = FontStyle.Bold;
            AddTextGlow(heading, new Color(0f, 0f, 0f, 0.86f), new Color(0.46f, 0.36f, 0.22f, 0.68f), new Vector2(2.2f, -2.6f));
            SetAnchors(heading.rectTransform, new Vector2(0.12f, 0.515f), new Vector2(0.88f, 0.735f));

            Text body = AddText(message, "본문", "동굴은 자비와 탐욕을 모두 기억합니다.", 24, TextAnchor.UpperCenter, new Color(0.88f, 0.83f, 0.74f, 1f));
            SetAnchors(body.rectTransform, new Vector2(0.14f, 0.260f), new Vector2(0.86f, 0.485f));

            Button blood = AddSettingsMenuButton(contentRoot, "피의 거래", "체력 6 잃기 / 금화 55", 21);
            SetAnchors(blood.GetComponent<RectTransform>(), new Vector2(0.16f, 0.12f), new Vector2(0.45f, 0.23f));
            blood.onClick.AddListener(() =>
            {
                LoseHealth(6, true);
                gold += 55;
                AddLog("피의 거래 수락.");
                ShowDoors();
            });

            Button read = AddSettingsMenuButton(contentRoot, "운명 읽기", CanAddCardToDeck() ? "카드 1장 / 빚 +1" : $"덱 한도 {deck.Count}/{GetMaxDeckSize()}", 21);
            SetAnchors(read.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.84f, 0.23f));
            read.interactable = CanAddCardToDeck();
            read.onClick.AddListener(() =>
            {
                if (!CanAddCardToDeck())
                {
                    AddLog($"이벤트: 덱 한도 {deck.Count}/{GetMaxDeckSize()}에 도달했습니다.");
                    ShowDoors();
                    return;
                }

                CardData card = PickEventCard();
                if (card == null)
                {
                    ShowDoors();
                    return;
                }

                if (TryAddCardToDeck(card, "이벤트"))
                {
                    debt += Mathf.Max(0, 1 - curseReduction);
                    AddLog($"{card.DisplayName} 획득. 빚 증가.");
                    CheckBuildUnlocks();
                }

                ShowDoors();
            });

            RefreshTopBar();
            RefreshLog();
        }

        private void ShowRest()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Rest;
            SetBackground(restBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = string.Empty;
            AddCenteredMessage("휴식: 다음 위험 전에 숨을 고르세요", "작은 촛불이 동굴의 숨결을 밀어냅니다.\n체력을 회복하거나 금화를 챙길 수 있습니다.");
            primaryButton.gameObject.SetActive(false);

            int restHealAmount = GetRestHealAmount();
            Button heal = AddSettingsMenuButton(contentRoot, "회복", $"체력 {restHealAmount} 회복", 21);
            SetAnchors(heal.GetComponent<RectTransform>(), new Vector2(0.16f, 0.12f), new Vector2(0.45f, 0.23f));
            heal.onClick.AddListener(() =>
            {
                Heal(restHealAmount);
                ShowDoors();
            });

            Button purse = AddSettingsMenuButton(contentRoot, "주머니", "금화 30 획득", 21);
            SetAnchors(purse.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.84f, 0.23f));
            purse.onClick.AddListener(() =>
            {
                gold += 30;
                AddLog("휴식에서 금화를 선택했습니다.");
                ShowDoors();
            });

            RefreshTopBar();
            RefreshLog();
        }

        private void ShowCurseEvent()
        {
            PlayNonCombatMusic();
            phase = GamePhase.Curse;
            SetBackground(curseBackground != null ? curseBackground : eventBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = string.Empty;
            AddCenteredMessage("대가: 문이 빚을 요구합니다", "붉은 계약이 문 앞에 펼쳐집니다.\n빚을 새기고 정예 위협을 마주해야 합니다.");
            primaryButton.gameObject.SetActive(false);

            Button enter = AddSettingsMenuButton(contentRoot, "대가 입장", "빚을 지고 입장", 21);
            SetAnchors(enter.GetComponent<RectTransform>(), new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.23f));
            enter.onClick.AddListener(() =>
            {
                debt += GetCurseDoorDebtGain();
                AddLog("빚이 계약서에 새겨졌습니다.");
                currentCombatDoorType = DoorType.Curse;
                StartCombat(CreateEnemy(true, false));
            });

            RefreshTopBar();
            RefreshLog();
        }

        private void EndTurn()
        {
            if (phase != GamePhase.Combat || enemy == null || combatVictorySequenceActive)
            {
                return;
            }

            ResolveEnemyIntent();
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            if (enemy.Health <= 0)
            {
                StartCombatVictorySequence();
                return;
            }

            if (enemy.Bleed > 0)
            {
                enemy.Health = Mathf.Max(0, enemy.Health - enemy.Bleed);
                AddLog($"출혈 피해 {enemy.Bleed}.");
                enemy.Bleed = Mathf.Max(0, enemy.Bleed - 1);
                if (enemy.Health <= 0)
                {
                    StartCombatVictorySequence();
                    return;
                }
            }

            if (enemy.Vulnerable > 0)
            {
                enemy.Vulnerable -= 1;
            }

            if (!retainBlockNextTurn)
            {
                playerBlock = 0;
            }
            retainBlockNextTurn = false;
            reflectedDamage = 0;
            pendingDamageReduction = 0;
            preventDeathThisTurn = false;
            action = StartingAction;
            cardsPlayedThisTurn.Clear();
            combinationTriggersThisTurn.Clear();
            activeCard = null;
            activeCardHandIndex = -1;
            activeCardDamageBonusApplied = false;
            activeCardBlockBonusApplied = false;
            activeCardRunItemDamageBonusApplied = false;
            activeCardRunItemBlockBonusApplied = false;
            forbiddenCycleActiveThisTurn = false;
            gamblerHardHighLuckAttackUsedThisTurn = false;
            gamblerHardLowLuckDefenseUsedThisTurn = false;
            oracleHardLuckHeldThisTurn = false;
            RollLuckForTurn();
            DrawUpToHandSize();
            ApplyRunItemTurnStartBonuses();
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            PrepareEnemyIntent();
            RenderCombat();
        }

        private void ResolveEnemyIntent()
        {
            if (enemy.IntentHeal > 0)
            {
                int before = enemy.Health;
                enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + enemy.IntentHeal);
                AddLog($"{enemy.Name} 체력 {enemy.Health - before} 회복.");
            }

            if (enemy.IntentAttack > 0)
            {
                int incoming = enemy.IsBoss && luck <= 2 ? enemy.IntentAttack + 5 : enemy.IntentAttack;
                LoseHealth(incoming, false);
                AddLog($"{enemy.Name}의 공격 {incoming}.");
                if (reflectedDamage > 0)
                {
                    DealDamage(reflectedDamage);
                    AddLog($"반사 피해 {reflectedDamage}.");
                }
            }

            if (enemy.IntentBlock > 0)
            {
                enemy.Block += enemy.IntentBlock;
                AddLog($"{enemy.Name} 방어도 +{enemy.IntentBlock}.");
            }

            if (enemy.IntentDebt > 0)
            {
                int totalDebtReduction = curseReduction;
                if (IsCombinationComplete("survival_instinct")
                    && playerHealth * 2 <= playerMaxHealth
                    && combinationTriggersThisCombat.Add("survival_instinct_debt"))
                {
                    totalDebtReduction += 1;
                    TriggerCombinationImpact("survival_instinct");
                    AddLog("조합 발동: 생존 본능. 빚 증가 1 감소.");
                }

                int addedDebt = GetRunItemReducedDebtGain(Mathf.Max(0, enemy.IntentDebt - totalDebtReduction));
                debt += addedDebt;
                if (addedDebt > 0)
                {
                    TriggerCombatFeedback("계약 발현", combatFeedbackCurseSprite, new Color(1f, 0.42f, 0.30f, 1f), 3);
                }

                AddLog(addedDebt > 0 ? $"{enemy.Name}가 빚 +{addedDebt}를 새겼습니다." : "빚 저항으로 빚을 막았습니다.");
            }

            ResolveEnemySpecialIntent();
        }

        private void ResolveEnemySpecialIntent()
        {
            if (enemy == null || enemy.IntentSpecialEffect == EnemySpecialEffect.None || phase == GamePhase.GameOver)
            {
                return;
            }

            int amount = Mathf.Max(1, enemy.IntentSpecialAmount);
            switch (enemy.IntentSpecialEffect)
            {
                case EnemySpecialEffect.GatekeeperSeal:
                    int sealedBlock = Mathf.Min(playerBlock, amount * 4 + (luck <= 2 ? 2 : 0));
                    if (sealedBlock > 0)
                    {
                        playerBlock -= sealedBlock;
                        AddLog($"{enemy.Name}의 봉인이 방어도 {sealedBlock}을 지웠습니다.");
                    }
                    else
                    {
                        int guardBlock = amount * 3;
                        enemy.Block += guardBlock;
                        AddLog($"{enemy.Name}의 봉인이 문 주변에 방어도 +{guardBlock}을 둘렀습니다.");
                    }

                    break;
                case EnemySpecialEffect.DebtAdjudication:
                    int judgmentBlock = Mathf.Clamp(debt * (2 + amount) + amount * 3, 3, 22);
                    enemy.Block += judgmentBlock;
                    AddLog($"{enemy.Name}의 판결이 현재 빚을 근거로 방어도 +{judgmentBlock}을 얻었습니다.");
                    break;
                case EnemySpecialEffect.AbyssUsury:
                    int interestDamage = Mathf.Clamp(debt + amount * 3 + (endlessModeActive ? endlessBossesDefeated : 0), 3, 15);
                    LoseHealth(interestDamage, true);
                    if (phase == GamePhase.GameOver)
                    {
                        return;
                    }

                    AddLog($"{enemy.Name}의 심연 이자가 체력 {interestDamage}을 직접 징수했습니다.");
                    break;
                case EnemySpecialEffect.BottomlessAudit:
                    int auditDamage = Mathf.Clamp(amount * 4 + deck.Count / 9 + roomsCleared / 5, 6, 20);
                    LoseHealth(auditDamage, true);
                    if (phase == GamePhase.GameOver)
                    {
                        return;
                    }

                    int before = enemy.Health;
                    int heal = Mathf.Max(4, auditDamage / 2);
                    enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + heal);
                    AddLog($"{enemy.Name}의 무저갱 감사가 체력 {auditDamage}을 거두고 {enemy.Health - before} 회복했습니다.");
                    break;
            }
        }

        private void PrepareEnemyIntent()
        {
            enemy.IntentAttack = 0;
            enemy.IntentBlock = 0;
            enemy.IntentDebt = 0;
            enemy.IntentHeal = 0;
            enemy.IntentSpecialEffect = EnemySpecialEffect.None;
            enemy.IntentSpecialAmount = 0;
            enemy.IntentSpecialLabel = string.Empty;
            enemy.IntentCardName = string.Empty;
            enemy.CandidateLabel = string.Empty;

            List<EnemyCardDefinition> candidates = DrawEnemyCandidateCards(enemy);
            EnemyCardDefinition selected = candidates[0];
            float bestScore = float.MinValue;
            foreach (EnemyCardDefinition card in candidates)
            {
                float score = ScoreEnemyCard(card);
                if (score > bestScore)
                {
                    bestScore = score;
                    selected = card;
                }
            }

            enemy.IntentAttack = selected.Attacks ? Mathf.Max(0, enemy.BaseAttack + selected.AttackBonus) : 0;
            enemy.IntentBlock = selected.BlockAmount;
            enemy.IntentDebt = selected.DebtAmount;
            enemy.IntentHeal = selected.HealAmount;
            enemy.IntentSpecialEffect = selected.SpecialEffect;
            enemy.IntentSpecialAmount = selected.SpecialAmount;
            enemy.IntentSpecialLabel = selected.SpecialLabel;
            enemy.IntentCardName = selected.Name;
            enemy.CandidateLabel = string.Join(", ", candidates.Select(card => card.Name));
            enemy.IntentLabel = BuildEnemyIntentLabel(selected, enemy.IntentAttack);

            AddLog($"{enemy.Name} 카드 후보: {enemy.CandidateLabel} -> {selected.Name}.");
        }

        private List<EnemyCardDefinition> DrawEnemyCandidateCards(EnemyState state)
        {
            List<EnemyCardDefinition> pool = BuildEnemyCardPool(state);
            int drawCount = state.IsBoss ? 4 : state.WasElite ? 3 : 2;
            List<EnemyCardDefinition> candidates = new(drawCount);
            for (int i = 0; i < drawCount; i += 1)
            {
                candidates.Add(pool[Random.Range(0, pool.Count)]);
            }

            return candidates;
        }

        private List<EnemyCardDefinition> BuildEnemyCardPool(EnemyState state)
        {
            List<EnemyCardDefinition> cards = new()
            {
                new("그림자 일격", attacks: true),
                new("깊은 할퀴기", attacks: true, attackBonus: 2),
                new("불길한 징수", attacks: true, attackBonus: -2, debtAmount: 1),
                new("어둠 속 방어", blockAmount: state.BaseBlock + 3)
            };

            if (roomsCleared >= 4 || state.WasElite || state.IsBoss)
            {
                cards.Add(new EnemyCardDefinition("균열 강타", attacks: true, attackBonus: 4));
                cards.Add(new EnemyCardDefinition("동굴 재생", healAmount: Mathf.Max(4, state.MaxHealth / 12)));
            }

            if (state.WasElite)
            {
                cards.Add(new EnemyCardDefinition("철벽 계약", blockAmount: state.BaseBlock + 8, debtAmount: 1));
                cards.Add(new EnemyCardDefinition("연속 추심", attacks: true, attackBonus: 1, debtAmount: 1));
            }

            if (state.IsBoss)
            {
                cards.Add(new EnemyCardDefinition("문 파괴", attacks: true, attackBonus: 6));
                cards.Add(new EnemyCardDefinition("운명 압류", attacks: true, attackBonus: 1, debtAmount: 2));
                cards.Add(new EnemyCardDefinition("세 번째 봉인", blockAmount: state.BaseBlock + 12, healAmount: 8));
                AddBossUniqueCards(cards, state);
            }

            return cards;
        }

        private void AddBossUniqueCards(List<EnemyCardDefinition> cards, EnemyState state)
        {
            switch (state.Id)
            {
                case EasyBossId:
                    cards.Add(new EnemyCardDefinition(
                        "문턱 봉인",
                        blockAmount: state.BaseBlock + 8,
                        debtAmount: 1,
                        specialEffect: EnemySpecialEffect.GatekeeperSeal,
                        specialAmount: 1,
                        specialLabel: "방어 봉인"));
                    cards.Add(new EnemyCardDefinition(
                        "세 문의 압박",
                        attacks: true,
                        attackBonus: 3,
                        specialEffect: EnemySpecialEffect.GatekeeperSeal,
                        specialAmount: 1,
                        specialLabel: "방어 봉인"));
                    break;
                case NormalBossId:
                    cards.Add(new EnemyCardDefinition(
                        "채무 판결",
                        blockAmount: state.BaseBlock + 6,
                        debtAmount: 2,
                        specialEffect: EnemySpecialEffect.DebtAdjudication,
                        specialAmount: 1,
                        specialLabel: "빚 비례 방어"));
                    cards.Add(new EnemyCardDefinition(
                        "압류 명령",
                        attacks: true,
                        attackBonus: 2,
                        debtAmount: 1,
                        specialEffect: EnemySpecialEffect.DebtAdjudication,
                        specialAmount: 1,
                        specialLabel: "빚 비례 방어"));
                    break;
                case HardBossId:
                    cards.Add(new EnemyCardDefinition(
                        "심연 이자",
                        attacks: true,
                        attackBonus: 4,
                        debtAmount: 2,
                        specialEffect: EnemySpecialEffect.AbyssUsury,
                        specialAmount: 2,
                        specialLabel: "이자 피해"));
                    cards.Add(new EnemyCardDefinition(
                        "복리 계약",
                        blockAmount: state.BaseBlock + 10,
                        debtAmount: 2,
                        specialEffect: EnemySpecialEffect.AbyssUsury,
                        specialAmount: 1,
                        specialLabel: "이자 피해"));
                    break;
                case DebtClearBossId:
                    cards.Add(new EnemyCardDefinition(
                        "무저갱 감사",
                        attacks: true,
                        attackBonus: 5,
                        specialEffect: EnemySpecialEffect.BottomlessAudit,
                        specialAmount: 2,
                        specialLabel: "감사 피해"));
                    cards.Add(new EnemyCardDefinition(
                        "최종 상환",
                        blockAmount: state.BaseBlock + 16,
                        healAmount: 10,
                        specialEffect: EnemySpecialEffect.BottomlessAudit,
                        specialAmount: 1,
                        specialLabel: "감사 피해"));
                    break;
            }
        }

        private float ScoreEnemyCard(EnemyCardDefinition card)
        {
            float score = Random.Range(0f, 2.5f);
            if (card.Attacks)
            {
                int attack = Mathf.Max(0, enemy.BaseAttack + card.AttackBonus);
                int bossLuckBonus = enemy.IsBoss && luck <= 2 ? 5 : 0;
                int expectedDamage = Mathf.Max(0, attack + bossLuckBonus - playerBlock);
                score += attack * 1.4f + expectedDamage * 4.8f;
                if (playerHealth <= expectedDamage)
                {
                    score += 1000f;
                }

                if (playerBlock == 0)
                {
                    score += 3.5f;
                }
            }

            if (card.BlockAmount > 0)
            {
                float healthRatio = enemy.MaxHealth <= 0 ? 0f : (float)enemy.Health / enemy.MaxHealth;
                float blockWeight = healthRatio <= 0.4f ? 4.2f : 1.8f;
                if (enemy.Health <= 18)
                {
                    blockWeight += 1.4f;
                }

                score += card.BlockAmount * blockWeight;
                if (enemy.Block == 0)
                {
                    score += 2f;
                }
            }

            if (card.DebtAmount > 0)
            {
                int effectiveDebt = Mathf.Max(0, card.DebtAmount - curseReduction);
                score += effectiveDebt * (debt < 3 ? 12f : 5f);
            }

            if (card.HealAmount > 0)
            {
                int missingHealth = enemy.MaxHealth - enemy.Health;
                score += Mathf.Min(missingHealth, card.HealAmount) * (enemy.Health * 2 <= enemy.MaxHealth ? 5.2f : 1.1f);
            }

            if (card.SpecialEffect != EnemySpecialEffect.None)
            {
                score += ScoreEnemySpecialEffect(card.SpecialEffect, Mathf.Max(1, card.SpecialAmount));
            }

            return score;
        }

        private float ScoreEnemySpecialEffect(EnemySpecialEffect specialEffect, int amount)
        {
            return specialEffect switch
            {
                EnemySpecialEffect.GatekeeperSeal => playerBlock > 0 ? 14f + amount * 3f : 5f + amount * 2f,
                EnemySpecialEffect.DebtAdjudication => Mathf.Max(1, debt) * (4.5f + amount) + 5f,
                EnemySpecialEffect.AbyssUsury => Mathf.Max(1, debt + amount * 2) * 4.2f + (playerHealth <= 18 ? 14f : 0f),
                EnemySpecialEffect.BottomlessAudit => 18f + amount * 5f + (enemy.Health * 2 <= enemy.MaxHealth ? 8f : 0f),
                _ => 0f
            };
        }

        private static string BuildEnemyIntentLabel(EnemyCardDefinition card, int attack)
        {
            List<string> parts = new();
            if (attack > 0)
            {
                parts.Add($"공격 {attack}");
            }

            if (card.BlockAmount > 0)
            {
                parts.Add($"방어 {card.BlockAmount}");
            }

            if (card.DebtAmount > 0)
            {
                parts.Add($"빚 +{card.DebtAmount}");
            }

            if (card.HealAmount > 0)
            {
                parts.Add($"회복 {card.HealAmount}");
            }

            if (!string.IsNullOrWhiteSpace(card.SpecialLabel))
            {
                parts.Add(card.SpecialLabel);
            }

            return parts.Count == 0 ? "대기" : string.Join(" / ", parts);
        }

        private void StartDiceRollAnimation()
        {
            List<Sprite> rollSprites = GetCurrentDiceRollSprites();
            if (rollSprites.Count == 0)
            {
                return;
            }

            diceRollAnimationStartTime = Time.unscaledTime;
            diceRollAnimationEndTime = diceRollAnimationStartTime + 0.85f;

            if (diceRollRoot != null)
            {
                diceRollRoot.gameObject.SetActive(true);
            }

            if (diceRollText != null)
            {
                diceRollText.text = "굴림";
            }
        }

        private void UpdateDiceRollAnimation()
        {
            if (diceRollRoot == null || !diceRollRoot.gameObject.activeSelf)
            {
                return;
            }

            List<Sprite> rollSprites = GetCurrentDiceRollSprites();
            if (rollSprites.Count == 0)
            {
                diceRollRoot.gameObject.SetActive(false);
                return;
            }

            float now = Time.unscaledTime;
            if (now >= diceRollAnimationEndTime)
            {
                Sprite finalSprite = GetDiceSprite(luck);
                if (diceImage != null)
                {
                    diceImage.sprite = finalSprite;
                }
                if (diceRollImage != null)
                {
                    diceRollImage.sprite = finalSprite;
                }
                if (diceRollText != null)
                {
                    diceRollText.text = $"행운 {luck}";
                }
                if (now >= diceRollAnimationEndTime + 0.55f)
                {
                    diceRollRoot.gameObject.SetActive(false);
                }
                return;
            }

            int frame = Mathf.FloorToInt((now - diceRollAnimationStartTime) * 18f) % rollSprites.Count;
            Sprite sprite = rollSprites[frame];
            if (diceRollImage != null)
            {
                diceRollImage.sprite = sprite;
                diceRollImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 12f) * 8f);
            }
            if (diceImage != null)
            {
                diceImage.sprite = sprite;
            }
        }

        private void RollLuckForTurn()
        {
            oracleHardLuckHeldThisTurn = false;
            if (keepLuckNextTurn)
            {
                keepLuckNextTurn = false;
                oracleHardLuckHeldThisTurn = selectedClass == CharacterClass.Oracle && IsHardModeFeatureActive();
                AddLog($"행운 {luck} 유지.");
                return;
            }

            if (hasStoredLuck)
            {
                luck = storedLuck;
                hasStoredLuck = false;
                AddLog($"저장한 행운 {luck} 사용.");
                return;
            }

            luck = RollLuck();
            StartDiceRollAnimation();
            if (selectedClass == CharacterClass.Gambler && luck <= 2)
            {
                debt += 1;
                AddLog($"낮은 행운 {luck}. 빚 +1.");
            }
            else
            {
                AddLog($"행운 {luck}.");
            }
        }

        private int RollLuck()
        {
            if (selectedClass == CharacterClass.Gambler && gamblerLoadedDiceRollsRemaining > 0)
            {
                gamblerLoadedDiceRollsRemaining -= 1;
                TriggerCombinationImpact("trait_gambler_card_reading");
                AddLog($"패 읽기: 강화 주사위 사용. 남은 횟수 {gamblerLoadedDiceRollsRemaining}.");
                return Random.value < 0.90f ? Random.Range(4, 7) : Random.Range(1, 4);
            }

            return Random.Range(1, 7);
        }

        private void Heal(int amount)
        {
            int before = playerHealth;
            playerHealth = Mathf.Min(playerMaxHealth, playerHealth + amount);
            AddLog($"체력 {playerHealth - before} 회복.");
        }

        private void LoseHealth(int amount, bool direct)
        {
            int damage = amount;
            int blocked = 0;
            if (!direct)
            {
                if (IsCombinationComplete("gambling_bulwark") && luck <= 2)
                {
                    int reduced = Mathf.Min(2, damage);
                    damage = Mathf.Max(0, damage - reduced);
                    if (reduced > 0)
                    {
                        TriggerCombinationImpact("gambling_bulwark");
                        AddLog("조합 발동: 도박의 방벽. 피해 -2.");
                    }
                }

                if (pendingDamageReduction > 0)
                {
                    int reduced = Mathf.Min(pendingDamageReduction, damage);
                    pendingDamageReduction = 0;
                    damage = Mathf.Max(0, damage - reduced);
                    if (reduced > 0)
                    {
                        AddLog($"Damage reduced by {reduced}.");
                    }
                }

                blocked = Mathf.Min(playerBlock, damage);
                playerBlock -= blocked;
                damage = Mathf.Max(0, damage - blocked);
            }

            if (damage <= 0)
            {
                if (!direct && blocked > 0)
                {
                    TriggerCombatFeedback("방어 성공", combatFeedbackDefenseSprite, new Color(0.50f, 1f, 0.94f, 1f));
                }

                return;
            }

            if (IsHardModeFeatureActive()
                && selectedClass == CharacterClass.Exile
                && !exileHardFatalOathTriggeredThisCombat
                && playerHealth - damage <= 0)
            {
                playerHealth = 1;
                exileHardFatalOathTriggeredThisCombat = true;
                if (debt > 0)
                {
                    debt -= 1;
                    RecordExileCurseRemoval(1);
                    ApplyRunItemDebtReduced(1);
                }

                TriggerCombinationImpact("hard_trait_exile_endless_atonement");
                AddLog("Hard trait: fatal damage endured at 1 health.");
                return;
            }

            if (preventDeathThisTurn && playerHealth - damage <= 0)
            {
                playerHealth = 1;
                preventDeathThisTurn = false;
                AddLog("불굴로 죽음을 버텼습니다.");
                return;
            }

            playerHealth = Mathf.Max(0, playerHealth - damage);
            if (direct)
            {
                AddLog($"체력 {damage} 잃음.");
            }

            TryTriggerRunItemLowHealthBlock();
            TryTriggerExileWoundOath();
            TryTriggerRiftSurvival();

            if (playerHealth <= 0)
            {
                ShowGameOver(false, "동굴이 또 하나의 이름을 삼켰습니다.");
            }
        }

        private void DrawUpToHandSize()
        {
            DrawCards(Mathf.Max(0, StartingHandSize - hand.Count));
        }

        private void DrawCards(int count)
        {
            if (count <= 0 || phase == GamePhase.GameOver)
            {
                return;
            }

            int drawn = 0;
            for (int i = 0; i < count; i += 1)
            {
                if (drawPile.Count == 0)
                {
                    if (!TryLoseFromEmptyCombatDeck())
                    {
                        NotifyEmptyDeckWithCardsInHand();
                    }

                    break;
                }

                CardData card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                drawn += 1;
            }

            if (drawn > 0)
            {
                pendingDrawAnimationCount += drawn;
                RecordGamblerCardFlow(drawn);
            }
        }

        private bool TryLoseFromEmptyCombatDeck()
        {
            if (phase != GamePhase.Combat
                || combatVictorySequenceActive
                || enemy == null
                || drawPile.Count > 0
                || hand.Count > 0)
            {
                return false;
            }

            LoseFromEmptyDeck();
            return true;
        }

        private void NotifyEmptyDeckWithCardsInHand()
        {
            if (emptyDeckWarningLogged || phase != GamePhase.Combat || hand.Count <= 0)
            {
                return;
            }

            emptyDeckWarningLogged = true;
            AddLog("덱이 비었습니다. 손패를 모두 사용하면 전투를 이어갈 수 없습니다.");
        }

        private void LoseFromEmptyDeck()
        {
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            ShowGameOver(false, "덱과 손패가 모두 소진되었습니다. 더 이상 카드를 사용할 수 없어 패배했습니다.");
        }

        private void DiscardRandom(int count)
        {
            int discardedCount = 0;
            for (int i = 0; i < count && hand.Count > 0; i += 1)
            {
                int protectedIndex = activeCardHandIndex >= 0 && activeCardHandIndex < hand.Count ? activeCardHandIndex : -1;
                int eligibleCount = protectedIndex >= 0 ? hand.Count - 1 : hand.Count;
                if (eligibleCount <= 0)
                {
                    break;
                }

                int index = Random.Range(0, eligibleCount);
                if (protectedIndex >= 0 && index >= protectedIndex)
                {
                    index += 1;
                }

                CardData discarded = hand[index];
                hand.RemoveAt(index);
                if (activeCardHandIndex > index)
                {
                    activeCardHandIndex -= 1;
                }

                discardPile.Add(discarded);
                discardedCount += 1;
                AddLog($"{discarded.DisplayName} 버림.");
            }

            RecordGamblerCardFlow(discardedCount);
            if (activeCard == null)
            {
                TryLoseFromEmptyCombatDeck();
            }
        }

        private void ShowGameOver(bool victory, string message)
        {
            if (CanUseRunSaveSystem())
            {
                ClearHardRunSave();
            }

            if (!victory && endlessModeActive)
            {
                RecordEndlessProgress();
            }

            if (victory)
            {
                PlayMainMenuMusic();
            }
            else
            {
                PlayDeathMusic();
            }

            phase = GamePhase.GameOver;
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetSubtitleBoxVisible(false);
            subtitleText.text = string.Empty;
            diceRollRoot.gameObject.SetActive(false);
            ClearContent();
            primaryButton.gameObject.SetActive(false);

            if (victory)
            {
                primaryButton.gameObject.SetActive(true);
                primaryButton.onClick.RemoveAllListeners();
                primaryButton.onClick.AddListener(ShowClassSelection);
                SetPrimaryButtonDefaultPlacement();
                SetButtonLabel(primaryButton, "다시하기");
                SetBackground(bossBackground);
                topBar.gameObject.SetActive(true);
                SetLogVisible(true);
                subtitleText.text = "런 클리어";
                AddCenteredMessage("문지기가 무너졌습니다", message);
                RefreshTopBar();
                RefreshLog();
                return;
            }

            if (TryShowHiddenGameOver())
            {
                return;
            }

            int variantIndex = gameOverBackgroundSprites.Count == 0
                ? -1
                : Random.Range(0, gameOverBackgroundSprites.Count);
            Sprite background = variantIndex >= 0 ? gameOverBackgroundSprites[variantIndex] : battleBackground;
            SetBackground(background);
            CreateGameOverOverlay("Game Over Overlay", new Color(0f, 0f, 0f, 0.38f));

            if (gameOverLogoSprite != null)
            {
                Image logoImage = AddImage(gameOverOverlay, "Game Over Logo", Color.white);
                logoImage.sprite = gameOverLogoSprite;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;
                SetAnchors(logoImage.rectTransform, new Vector2(0.160f, 0.690f), new Vector2(0.840f, 0.955f));
            }
            else
            {
                Text logoText = AddText(gameOverOverlay, "Game Over 텍스트", "GAME OVER", 112, TextAnchor.MiddleCenter, new Color(0.95f, 0.28f, 0.18f, 1f));
                logoText.fontStyle = FontStyle.Bold;
                AddTextGlow(logoText, new Color(0f, 0f, 0f, 0.92f), new Color(0.82f, 0.56f, 0.22f, 0.76f), new Vector2(3.4f, -4.0f));
                SetAnchors(logoText.rectTransform, new Vector2(0.160f, 0.720f), new Vector2(0.840f, 0.935f));
            }

            Sprite messageSprite = GetGameOverMessageSprite(variantIndex);
            if (messageSprite != null)
            {
                Image messageImage = AddImage(gameOverOverlay, "Game Over Message Image", Color.white);
                messageImage.sprite = messageSprite;
                messageImage.preserveAspect = true;
                messageImage.raycastTarget = false;
                SetAnchors(messageImage.rectTransform, new Vector2(0.120f, 0.365f), new Vector2(0.880f, 0.570f));
            }
            else
            {
                Text deathMessage = AddText(gameOverOverlay, "Game Over Message", message, 46, TextAnchor.MiddleCenter, new Color(0.94f, 0.88f, 0.76f, 1f));
                deathMessage.fontStyle = FontStyle.Bold;
                deathMessage.resizeTextForBestFit = true;
                deathMessage.resizeTextMinSize = 30;
                deathMessage.resizeTextMaxSize = 48;
                deathMessage.lineSpacing = 0.94f;
                AddTextGlow(deathMessage, new Color(0f, 0f, 0f, 0.94f), new Color(0.68f, 0.48f, 0.18f, 0.72f), new Vector2(2.8f, -3.2f));
                SetAnchors(deathMessage.rectTransform, new Vector2(0.180f, 0.390f), new Vector2(0.820f, 0.560f));
            }

            if (gameOverCrackOverlaySprite != null)
            {
                Image crackOverlay = AddImage(gameOverOverlay, "Full Screen Shatter Overlay", new Color(1f, 1f, 1f, 0.82f));
                crackOverlay.sprite = gameOverCrackOverlaySprite;
                crackOverlay.type = Image.Type.Simple;
                crackOverlay.raycastTarget = false;
                Stretch(crackOverlay.rectTransform);
                crackOverlay.transform.SetAsLastSibling();
            }

            AddGameOverRestartButton();
        }

        private bool TryShowHiddenGameOver()
        {
            Sprite hiddenSprite = GetHiddenGameOverSprite();
            if (hiddenSprite == null || Random.value >= Mathf.Clamp01(hiddenGameOverChance))
            {
                return false;
            }

            SetBackground(hiddenSprite);
            CreateGameOverOverlay("Hidden Game Over Overlay", new Color(0f, 0f, 0f, 0.001f));
            AddGameOverRestartButton();
            return true;
        }

        private RectTransform CreateGameOverOverlay(string name, Color color)
        {
            Image overlayImage = AddImage(root, name, color);
            overlayImage.raycastTarget = true;
            gameOverOverlay = overlayImage.rectTransform;
            Stretch(gameOverOverlay);
            gameOverOverlay.SetAsLastSibling();
            return gameOverOverlay;
        }

        private Sprite GetGameOverMessageSprite(int variantIndex)
        {
            if (variantIndex >= 0 && variantIndex < gameOverMessageSprites.Count)
            {
                return gameOverMessageSprites[variantIndex];
            }

            return gameOverMessageSprites.Count > 0
                ? gameOverMessageSprites[Random.Range(0, gameOverMessageSprites.Count)]
                : null;
        }

        private Sprite GetHiddenGameOverSprite()
        {
            return selectedClass switch
            {
                CharacterClass.Gambler => gamblerHiddenGameOverSprite,
                CharacterClass.Oracle => oracleHiddenGameOverSprite,
                CharacterClass.Exile => exileHiddenGameOverSprite,
                _ => null
            };
        }

        private void AddGameOverRestartButton()
        {
            Button restartButton = AddGameOverButton(gameOverOverlay, "Restart Button", "다시하기", 24);
            SetAnchors(restartButton.GetComponent<RectTransform>(), new Vector2(0.380f, 0.055f), new Vector2(0.620f, 0.165f));
            restartButton.onClick.AddListener(() => StartRun(selectedClass));
        }

        private Button AddGameOverButton(RectTransform parent, string name, string label, int fontSize)
        {
            Sprite buttonSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : classBackButtonSprite != null
                    ? classBackButtonSprite
                    : mainMenuButtonSprite != null
                        ? mainMenuButtonSprite
                        : buttonIdleSprite;
            RectTransform buttonRoot = AddPanel(parent, name, Color.white, buttonSprite);
            Image image = buttonRoot.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = new Color(1.12f, 1.10f, 1.04f, 1f);
            image.raycastTarget = true;

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();

            Text text = AddText(buttonRoot, $"{name} 라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.92f), new Color(0.08f, 0.62f, 0.58f, 0.44f), new Vector2(1.5f, -1.7f));
            SetAnchors(text.rectTransform, new Vector2(0.150f, 0.255f), new Vector2(0.850f, 0.745f));
            return button;
        }

        private void ShowJourneyEnding()
        {
            if (CanUseRunSaveSystem())
            {
                ClearHardRunSave();
            }

            PlayMainMenuMusic();
            phase = GamePhase.GameOver;
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetSubtitleBoxVisible(false);
            subtitleText.text = string.Empty;
            diceRollRoot.gameObject.SetActive(false);
            ClearContent();
            ClearGameOverOverlay();
            primaryButton.gameObject.SetActive(false);

            Sprite background = GetJourneyEndingBackgroundSprite();
            SetBackground(background);

            Image overlayImage = AddImage(root, "여정 엔딩 오버레이", new Color(0f, 0f, 0f, 0.36f));
            overlayImage.raycastTarget = true;
            gameOverOverlay = overlayImage.rectTransform;
            Stretch(gameOverOverlay);
            gameOverOverlay.SetAsLastSibling();

            Sprite logoSprite = GetJourneyEndingLogoSprite();
            if (logoSprite != null)
            {
                Image logoImage = AddImage(gameOverOverlay, "또 다른 여정 로고", Color.white);
                logoImage.sprite = logoSprite;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;
                SetAnchors(logoImage.rectTransform, new Vector2(0.045f, 0.675f), new Vector2(0.955f, 0.975f));
            }
            else
            {
                Text logoText = AddText(
                    gameOverOverlay,
                    "또 다른 여정 텍스트",
                    GetJourneyEndingTitleText(),
                    74,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.86f, 0.52f, 1f));
                logoText.fontStyle = FontStyle.Bold;
                logoText.resizeTextMinSize = 42;
                AddTextGlow(logoText, new Color(0f, 0f, 0f, 0.94f), new Color(0.16f, 0.74f, 0.72f, 0.58f), new Vector2(3.2f, -3.6f));
                SetAnchors(logoText.rectTransform, new Vector2(0.075f, 0.710f), new Vector2(0.925f, 0.930f));
            }

            string endingMessage = GetJourneyEndingMessage();
            RectTransform messagePanel = AddPanel(
                gameOverOverlay,
                "여정 엔딩 문구 패널",
                new Color(1f, 1f, 1f, 0.86f),
                eventMessageFrameSprite != null ? eventMessageFrameSprite : panelSprite);
            SetAnchors(messagePanel, new Vector2(0.185f, 0.330f), new Vector2(0.815f, 0.545f));

            Text journeyMessage = AddText(messagePanel, "여정 엔딩 문구", endingMessage, 24, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.74f, 1f));
            journeyMessage.resizeTextForBestFit = true;
            journeyMessage.resizeTextMinSize = 15;
            journeyMessage.resizeTextMaxSize = 24;
            journeyMessage.horizontalOverflow = HorizontalWrapMode.Wrap;
            journeyMessage.verticalOverflow = VerticalWrapMode.Truncate;
            journeyMessage.lineSpacing = 0.94f;
            AddTextGlow(journeyMessage, new Color(0f, 0f, 0f, 0.92f), new Color(0.50f, 0.38f, 0.18f, 0.58f), new Vector2(1.7f, -2.0f));
            SetAnchors(journeyMessage.rectTransform, new Vector2(0.145f, 0.245f), new Vector2(0.855f, 0.755f));

            string nextButtonLabel = currentJourneyEndingKind == JourneyEndingKind.TrueDebtCleared ? "해금 확인" : "다음 여정으로";
            Button nextJourneyButton = AddClassDetailButton(gameOverOverlay, "다음 여정 버튼", nextButtonLabel, classConfirmButtonSprite, 24);
            SetAnchors(nextJourneyButton.GetComponent<RectTransform>(), new Vector2(0.360f, 0.070f), new Vector2(0.640f, 0.165f));
            nextJourneyButton.onClick.AddListener(ShowMainMenu);
        }

        private Sprite GetJourneyEndingBackgroundSprite()
        {
            Sprite classBackground = selectedClass switch
            {
                CharacterClass.Oracle => oracleJourneyEndingBackgroundSprite,
                CharacterClass.Exile => exileJourneyEndingBackgroundSprite,
                _ => gamblerJourneyEndingBackgroundSprite
            };
            if (classBackground != null)
            {
                return classBackground;
            }

            if (journeyEndingBackgroundSprites.Count > 0)
            {
                return journeyEndingBackgroundSprites[Random.Range(0, journeyEndingBackgroundSprites.Count)];
            }

            return bossBackground != null ? bossBackground : battleBackground;
        }

        private Sprite GetJourneyEndingLogoSprite()
        {
            Sprite classLogo = selectedClass switch
            {
                CharacterClass.Oracle => oracleJourneyEndingLogoSprite,
                CharacterClass.Exile => exileJourneyEndingLogoSprite,
                _ => gamblerJourneyEndingLogoSprite
            };
            return classLogo != null ? classLogo : journeyEndingLogoSprite;
        }

        private string GetJourneyEndingTitleText()
        {
            if (currentJourneyEndingKind == JourneyEndingKind.TrueDebtCleared)
            {
                return selectedClass switch
                {
                    CharacterClass.Oracle => "The prophecy is paid in full",
                    CharacterClass.Exile => "The exile owns the road",
                    _ => "The final debt leaves the table"
                };
            }

            if (currentJourneyEndingKind == JourneyEndingKind.EndlessReturn)
            {
                return "The record remains beyond the door";
            }

            return selectedClass switch
            {
                CharacterClass.Oracle => "I witness yet another ending",
                CharacterClass.Exile => "To me, exile is freedom",
                _ => "The start of another gamble"
            };
        }

        private string GetJourneyEndingMessage()
        {
            if (currentJourneyEndingKind == JourneyEndingKind.TrueDebtCleared)
            {
                return selectedClass switch
                {
                    CharacterClass.Oracle => "점술가는 마지막 빚을 태우고 자신이 보았던 결말을 처음으로 바꾸었다. 진엔딩이 영구 해금되었습니다.",
                    CharacterClass.Exile => "추방자는 이름을 담보로 잡던 계약을 찢었다. 이제 추방은 형벌이 아니라 귀환할 수 있는 자유가 되었다. 진엔딩이 영구 해금되었습니다.",
                    _ => "도박사는 마지막 판돈을 내려놓고 빚 장부를 닫았다. 더 이상 운명에 빌리지 않아도 다음 판을 시작할 수 있다. 진엔딩이 영구 해금되었습니다."
                };
            }

            if (currentJourneyEndingKind == JourneyEndingKind.EndlessReturn)
            {
                return $"{GetClassName(selectedClass)}는 {roomsCleared}번째 문까지 기록을 새기고 귀환했다. 심연은 남았지만, 이번 기록은 사라지지 않는다.";
            }

            return selectedClass switch
            {
                CharacterClass.Oracle => "점술가는 결말을 끝이라 부르지 않았다. 그녀에게 결말은 다음 예언의 첫 장면이었다.",
                CharacterClass.Exile => "추방자는 동굴 밖에서 깨달았다. 자신을 밀어낸 길이야말로 자유였음을.",
                _ => "도박사는 마지막 문 앞에서도 웃었다. 끝난 것은 한 판뿐, 다음 판돈은 이미 놓여 있었다."
            };
        }

        private void ShowContinueButton()
        {
            primaryButton.gameObject.SetActive(true);
            SetPrimaryButtonDefaultPlacement();
            SetButtonLabel(primaryButton, "계속");
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(ShowDoors);
        }

        private void SetPrimaryButtonDefaultPlacement()
        {
            SetAnchors(primaryButton.GetComponent<RectTransform>(), new Vector2(0.765f, 0.025f), new Vector2(0.955f, 0.115f));
            ApplyPrimaryButtonFrame();
            primaryButton.transform.SetAsLastSibling();
        }

        private void SetDefaultContentRootPlacement()
        {
            SetAnchors(contentRoot, new Vector2(0.04f, 0.11f), new Vector2(0.73f, 0.83f));
        }

        private void ApplyPrimaryButtonFrame()
        {
            Image image = primaryButton.GetComponent<Image>();
            Sprite sprite = settingsButtonSprite != null
                ? settingsButtonSprite
                : classConfirmButtonSprite != null
                    ? classConfirmButtonSprite
                    : mainMenuButtonSprite != null
                        ? mainMenuButtonSprite
                        : buttonIdleSprite;
            if (sprite == null)
            {
                image.color = new Color(0.035f, 0.030f, 0.026f, 0.92f);
                return;
            }

            image.sprite = sprite;
            image.type = GetImageType(sprite);
            image.color = Color.white;
            image.raycastTarget = true;
        }

        private void SetSubtitleBoxVisible(bool visible)
        {
            if (subtitleFrame == null)
            {
                return;
            }

            subtitleFrame.gameObject.SetActive(visible);
            UpdatePlayerStatsTextVisibility();
            if (visible)
            {
                SetAnchors(subtitleFrame, new Vector2(0.285f, 0.905f), new Vector2(0.715f, 0.975f));
                SetAnchors(subtitleText.rectTransform, new Vector2(0.320f, 0.923f), new Vector2(0.680f, 0.958f));
                return;
            }

            SetAnchors(subtitleText.rectTransform, new Vector2(0.10f, 0.825f), new Vector2(0.90f, 0.875f));
        }

        private bool ShouldShowTopPlayerStats()
        {
            return phase == GamePhase.Combat
                && subtitleFrame != null
                && !subtitleFrame.gameObject.activeSelf;
        }

        private void UpdatePlayerStatsTextVisibility()
        {
            if (playerStatsText == null)
            {
                return;
            }

            playerStatsText.gameObject.SetActive(ShouldShowTopPlayerStats());
        }

        private void AddCenteredMessage(string heading, string body)
        {
            RectTransform panel = AddEventMessagePanel(contentRoot, "메시지");
            SetAnchors(panel, new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.76f));

            Text headingText = AddText(panel, "제목", heading, 34, TextAnchor.MiddleCenter, Color.white);
            headingText.fontStyle = FontStyle.Bold;
            AddTextGlow(headingText, new Color(0f, 0f, 0f, 0.88f), new Color(0.46f, 0.36f, 0.22f, 0.68f), new Vector2(2.2f, -2.6f));
            SetAnchors(headingText.rectTransform, new Vector2(0.12f, 0.520f), new Vector2(0.88f, 0.750f));

            Text bodyText = AddText(panel, "본문", body, 24, TextAnchor.UpperCenter, new Color(0.88f, 0.83f, 0.74f, 1f));
            SetAnchors(bodyText.rectTransform, new Vector2(0.14f, 0.235f), new Vector2(0.86f, 0.500f));
        }

        private void RefreshTopBar()
        {
            if (playerStatsText != null)
            {
                playerStatsText.text = $"체력 {playerHealth}/{playerMaxHealth}   방어 {playerBlock}   행동 {action}";
                UpdatePlayerStatsTextVisibility();
            }

            string roomText = endlessModeActive
                ? $"무한 {roomsCleared}문"
                : $"방 {Mathf.Min(roomsCleared, TargetRooms)}/{TargetRooms}";
            runStatsText.text = $"{GetDifficultyName(currentDifficulty)}  {roomText}  금 {gold}  빚 {debt}  덱 {deck.Count}/{GetMaxDeckSize()}  아이템 {equippedRunItemIds.Count}/{GetRunItemSlotLimit()}";
            diceText.text = $"행운 {luck}";
            Sprite luckSprite = GetDiceSprite(luck);
            bool showTopBarDice = phase != GamePhase.Combat;
            diceText.gameObject.SetActive(showTopBarDice);
            diceImage.gameObject.SetActive(showTopBarDice);
            diceImage.sprite = luckSprite;
            diceImage.enabled = luckSprite != null;

            if (combatDiceHudRoot != null)
            {
                combatDiceHudRoot.gameObject.SetActive(phase == GamePhase.Combat);
            }

            if (combatDiceImage != null)
            {
                combatDiceImage.sprite = luckSprite;
                combatDiceImage.enabled = luckSprite != null;
            }

            if (combatDiceText != null)
            {
                combatDiceText.text = $"행운 {luck}";
            }
        }

        private void RefreshLog()
        {
            if (logBodyRoot == null)
            {
                return;
            }

            for (int i = logBodyRoot.childCount - 1; i >= 0; i -= 1)
            {
                Destroy(logBodyRoot.GetChild(i).gameObject);
            }

            int maxVisibleEntries = phase == GamePhase.Combat ? 8 : 6;
            int start = Mathf.Max(0, combatLog.Count - maxVisibleEntries);
            List<string> entries = new();
            for (int i = start; i < combatLog.Count; i += 1)
            {
                entries.Add(WrapDisplayLine($"- {combatLog[i]}", 18, "  "));
            }

            Text body = AddText(logBodyRoot, "기록 내용", string.Join("\n", entries), 14, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.78f, 1f));
            body.lineSpacing = 0.94f;
            body.resizeTextMinSize = 10;
            body.resizeTextMaxSize = 14;
            SetAnchors(body.rectTransform, new Vector2(0.045f, 0.075f), new Vector2(0.955f, 0.930f));
        }

        private void AddLog(string message)
        {
            combatLog.Add(message);
            if (combatLog.Count > 80)
            {
                combatLog.RemoveAt(0);
            }
        }

        private bool ShowEnemyReveal()
        {
            ClearEnemyReveal();
            if (enemy == null || root == null)
            {
                return false;
            }

            Sprite revealSprite = GetEnemyCombatSprite();
            if (revealSprite == null)
            {
                return false;
            }

            Image overlay = AddImage(root, "Enemy Reveal Overlay", new Color(0f, 0f, 0f, 0.44f));
            overlay.raycastTarget = true;
            enemyRevealRoot = overlay.rectTransform;
            Stretch(enemyRevealRoot);
            enemyRevealRoot.SetAsLastSibling();

            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = false;

            Image portrait = AddImage(enemyRevealRoot, "Enemy Reveal Portrait", new Color(1f, 1f, 1f, 0.78f));
            portrait.sprite = revealSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetAnchors(portrait.rectTransform, new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.98f));

            Image veil = AddImage(enemyRevealRoot, "Enemy Reveal Veil", new Color(0f, 0f, 0f, 0.12f));
            veil.raycastTarget = false;
            Stretch(veil.rectTransform);

            enemyRevealRoutine = StartCoroutine(FadeEnemyReveal(group, portrait.rectTransform));
            return true;
        }

        private IEnumerator FadeEnemyReveal(CanvasGroup group, RectTransform portrait)
        {
            float fadeInElapsed = 0f;
            while (fadeInElapsed < EnemyRevealFadeInSeconds)
            {
                fadeInElapsed += Time.deltaTime;
                if (group == null)
                {
                    yield break;
                }

                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeInElapsed / EnemyRevealFadeInSeconds));
                group.alpha = t;
                if (portrait != null)
                {
                    portrait.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, t);
                }

                yield return null;
            }

            float holdElapsed = 0f;
            while (holdElapsed < EnemyRevealHoldSeconds)
            {
                holdElapsed += Time.deltaTime;
                if (group == null)
                {
                    yield break;
                }

                group.alpha = 1f;
                if (portrait != null)
                {
                    portrait.localScale = Vector3.one;
                }

                yield return null;
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < EnemyRevealFadeSeconds)
            {
                fadeElapsed += Time.deltaTime;
                if (group == null)
                {
                    yield break;
                }

                float t = Mathf.Clamp01(fadeElapsed / EnemyRevealFadeSeconds);
                float fade = Mathf.SmoothStep(0f, 1f, t);
                float expand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.62f));
                group.alpha = 1f - fade;
                if (portrait != null)
                {
                    portrait.localScale = Vector3.one * Mathf.Lerp(1f, EnemyRevealFadeOutScale, expand);
                }

                yield return null;
            }

            DestroyEnemyReveal();
            PlayPendingEnemyRevealCombinationImpacts();
        }

        private void ClearEnemyReveal()
        {
            if (enemyRevealRoutine != null)
            {
                StopCoroutine(enemyRevealRoutine);
                enemyRevealRoutine = null;
            }

            DestroyEnemyReveal();
        }

        private void DestroyEnemyReveal()
        {
            if (enemyRevealRoot != null)
            {
                enemyRevealRoot.gameObject.SetActive(false);
                Destroy(enemyRevealRoot.gameObject);
                enemyRevealRoot = null;
            }

            enemyRevealRoutine = null;
        }

        private void ClearGameOverOverlay()
        {
            if (gameOverOverlay == null)
            {
                return;
            }

            gameOverOverlay.gameObject.SetActive(false);
            Destroy(gameOverOverlay.gameObject);
            gameOverOverlay = null;
        }

        private void ClearContent()
        {
            if (phase != GamePhase.Combat)
            {
                ResetCombatFeedbackState();
                ClearCombatFeedbackOverlay();
            }

            ClearGameOverOverlay();
            ClearEnemyReveal();
            HideCardPreview();
            HideRunStatusPanel();
            combatDiceHudRoot = null;
            combatDiceImage = null;
            combatDiceText = null;
            SetSubtitleBoxVisible(false);
            for (int i = contentRoot.childCount - 1; i >= 0; i -= 1)
            {
                GameObject child = contentRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void SetBackground(Sprite sprite)
        {
            sceneBackgroundImage.sprite = sprite;
            sceneBackgroundImage.color = sprite != null ? Color.white : new Color(0.018f, 0.019f, 0.024f, 1f);
            sceneBackgroundImage.preserveAspect = false;
        }

        private Sprite GetDoorSprite(DoorType type)
        {
            if (type == DoorType.Boss)
            {
                Sprite bossDoorSprite = GetBossDoorSprite(false);
                if (bossDoorSprite != null)
                {
                    return bossDoorSprite;
                }
            }

            return doorSprites.FirstOrDefault(binding => binding.doorType == type)?.sprite;
        }

        private Sprite GetDoorHoverSprite(DoorType type)
        {
            if (type == DoorType.Boss)
            {
                Sprite bossDoorHoverSprite = GetBossDoorSprite(true);
                if (bossDoorHoverSprite != null)
                {
                    return bossDoorHoverSprite;
                }

                Sprite bossDoorSprite = GetBossDoorSprite(false);
                if (bossDoorSprite != null)
                {
                    return bossDoorSprite;
                }
            }

            DoorSpriteBinding binding = doorSprites.FirstOrDefault(candidate => candidate.doorType == type);
            return binding?.hoverSprite != null ? binding.hoverSprite : binding?.sprite;
        }

        private Sprite GetBossDoorSprite(bool hover)
        {
            if (currentDifficulty == RunDifficulty.Normal && !endlessModeActive)
            {
                return hover ? normalBossDoorHoverSprite : normalBossDoorSprite;
            }

            if (currentDifficulty == RunDifficulty.Hard || endlessModeActive)
            {
                return hover ? hardBossDoorHoverSprite : hardBossDoorSprite;
            }

            return hover ? easyBossDoorHoverSprite : easyBossDoorSprite;
        }

        private Sprite GetEnemySprite(string enemyId)
        {
            return enemySprites.FirstOrDefault(binding => binding.enemyId == enemyId)?.sprite;
        }

        private Sprite GetEnemyCombatSprite()
        {
            if (enemy == null)
            {
                return null;
            }

            if (IsDebtClearBoss(enemy))
            {
                return debtClearBossSprite != null ? debtClearBossSprite : hardBossSprite != null ? hardBossSprite : bossSprite;
            }

            return enemy.IsBoss ? GetBossCombatSprite(enemy.Id) : GetEnemySprite(enemy.Id);
        }

        private Sprite GetBossCombatSprite(string enemyId)
        {
            return enemyId switch
            {
                NormalBossId => normalBossSprite != null ? normalBossSprite : bossSprite,
                HardBossId => hardBossSprite != null ? hardBossSprite : bossSprite,
                _ => bossSprite
            };
        }

        private static bool IsDebtClearBoss(EnemyState enemyState)
        {
            return enemyState != null && enemyState.Id == DebtClearBossId;
        }

        private Sprite GetEnemyHudFrameSprite(string enemyId)
        {
            return enemyHudFrameSprites.FirstOrDefault(binding => binding.enemyId == enemyId)?.sprite;
        }

        private Sprite GetClassSprite(CharacterClass characterClass)
        {
            return characterClass switch
            {
                CharacterClass.Oracle => oracleSelectSprite,
                CharacterClass.Exile => exileSelectSprite,
                _ => gamblerSelectSprite
            };
        }

        private BuildRecipe GetCurrentBuildRecipe()
        {
            return GetBuildRecipe(selectedClass);
        }

        private static BuildRecipe GetBuildRecipe(CharacterClass characterClass)
        {
            return characterClass switch
            {
                CharacterClass.Oracle => new BuildRecipe(
                    "oracle_rift_engine",
                    "균열 엔진",
                    "전투 시작 카드 추가, 문 정보 선명화. 강화하면 정확한 예언의 비용 감소도 커집니다.",
                    2,
                    "class_oracle_attack_constellation_cut",
                    "class_oracle_defense_foreseen_barrier",
                    "class_oracle_skill_three_door_omen"),
                CharacterClass.Exile => new BuildRecipe(
                    "exile_last_oath",
                    "최후의 맹세",
                    "체력이 절반 이하일 때 방어 카드가 강해지고, 상처의 맹세와 저주 삼키기가 강화됩니다.",
                    2,
                    "class_exile_attack_chain_execution",
                    "class_exile_defense_oath_of_exile",
                    "class_exile_skill_brand_purification"),
                _ => new BuildRecipe(
                    "gambler_high_roll",
                    "고점 도박",
                    "행운이 5 이상일 때 공격 피해가 증가하고, 강화하면 패 읽기의 고점 주사위 횟수가 늘어납니다.",
                    2,
                    "class_gambler_attack_wager_dagger",
                    "class_gambler_defense_stake_shield",
                    "class_gambler_skill_turn_the_table")
            };
        }

        private static IReadOnlyList<BuildTag> GetPreferredBuildTags(string buildId)
        {
            return buildId switch
            {
                "oracle_rift_engine" => new[] { BuildTag.Prophecy, BuildTag.DeckControl, BuildTag.Door },
                "exile_last_oath" => new[] { BuildTag.Curse, BuildTag.LowHealth, BuildTag.Defense },
                _ => new[] { BuildTag.Dice, BuildTag.Debt, BuildTag.Attack }
            };
        }

        private static IReadOnlyList<CombinationRecipe> GetCombinationRecipes()
        {
            return new[]
            {
                new CombinationRecipe("fate_counter", "운명 반격", "고행운 턴 1회 공격 +3", "card_fate_strike", "card_guard_stance", "card_reroll"),
                new CombinationRecipe("bloody_contract", "피 묻은 계약", "3장 사용 후 다음 공격 +6", "card_reckless_attack", "card_last_defense", "card_small_contract"),
                new CombinationRecipe("execution_setup", "처형 준비", "약점 포착 후 마무리 +8", "card_finish", "card_shield_bash", "card_find_weakness"),
                new CombinationRecipe("edge_premonition", "칼끝의 예감", "출혈 공격 +2", "card_deep_stab", "card_evade", "card_read_the_rift"),
                new CombinationRecipe("safe_rebuild", "안전한 재정비", "교환 후 1장 추가", "card_throwing_dagger", "card_catch_breath", "card_card_exchange"),
                new CombinationRecipe("old_gear_mastery", "낡은 장비 숙련", "전투 시작 방어 +3 / 첫 공격 +2", "card_worn_dagger", "card_worn_shield", "card_store_luck"),
                new CombinationRecipe("heavy_pressure", "무거운 압박", "준비 후 묵직한 일격 비용 -1", "card_heavy_blow", "card_endure", "card_fix_fate"),
                new CombinationRecipe("gap_breaker", "틈새 공략", "공격 의도 피해 +4 / 방어 +3", "card_exploit_opening", "card_duck_low", "card_reroll"),
                new CombinationRecipe("survival_instinct", "생존 본능", "저체력 방어 +3 / 빚 -1", "card_counter_ready", "card_emergency_treatment", "card_purify"),
                new CombinationRecipe("forbidden_cycle", "금지된 순환", "금지된 선택 후 공격 +2~4", "card_double_slash", "card_protection_charm", "card_forbidden_choice"),
                new CombinationRecipe("gambling_bulwark", "도박의 방벽", "저행운 피해 -2 / 고행운 공격 +4", "card_blood_gamble", "card_absolute_barrier", "card_odd_pouch"),
                new CombinationRecipe("starlight_guard", "별빛 방어식", "반복 공격 +1 / 방어 +4", "card_starlight_barrage", "card_mirror_shield", "card_store_luck"),
                new CombinationRecipe("fate_cleaver", "운명 절단", "저체력 참수 +12 / 불굴", "card_fate_beheading", "card_indomitable", "card_fate_manipulation"),
                new CombinationRecipe("curse_backflow", "저주 역류", "빚 감소 후 공격 +5 / 회복 +3", "card_reckless_attack", "card_protection_charm", "card_absorb_curse"),
                new CombinationRecipe("third_answer", "세 번째 해답", "보상 4장 확률 20%", "card_fate_strike", "card_absolute_barrier", "card_third_door")
            }.Concat(GetHardCombinationRecipes()).ToArray();
        }

        private static IReadOnlyList<CombinationRecipe> GetHardCombinationRecipes()
        {
            return new[]
            {
                new CombinationRecipe("abyss_breakthrough", "심연 돌파", "턴당 1회 공격 +6 / 고체력 적 +4", "hard_attack_abyss_cleave", "hard_defense_lion_aegis", "hard_skill_fate_convergence"),
                new CombinationRecipe("brass_counter_ritual", "황동 반격식", "공격 의도에 방어 후 1장 뽑기 / 다음 공격 +3", "hard_attack_bronze_javelin", "hard_defense_crystal_wall", "hard_skill_door_breath"),
                new CombinationRecipe("rift_survival", "균열 생존술", "전투마다 1회 저체력 방어 +10 / 회복 +5", "hard_attack_crushing_handle", "hard_defense_broken_bulwark", "hard_skill_iron_tonic"),
                new CombinationRecipe("debt_reversal", "채무 역전", "빚 감소 시 다음 공격 +8 / 전투 1회 행동력 +1", "hard_skill_debt_writ", "hard_exile_red_oath", "hard_exile_chain_breaker"),
                new CombinationRecipe("gold_execution", "금화 처형식", "금화 50마다 공격 +2 / 고행운 추가 +4", "hard_skill_gold_rain", "hard_gambler_debt_jackpot", "hard_gambler_final_wager"),
                new CombinationRecipe("triple_omen_circle", "삼중 예언진", "예언 후 다음 카드 비용 -1 / 행운 유지 방어 +6", "hard_oracle_three_omens", "hard_oracle_crystal_sentence", "hard_oracle_fixed_star"),
                new CombinationRecipe("no_return_path", "돌아오지 않는 길", "빚 감소 시 방어 +8 / 회복 +4", "hard_exile_no_return", "hard_exile_red_oath", "hard_defense_silent_plate"),
                new CombinationRecipe("gatekeeper_hunt", "문지기 사냥", "정예/보스 첫 공격 +10 / 첫 방어 +8", "hard_attack_gate_execution", "hard_defense_glass_guard", "hard_skill_debt_writ")
            };
        }

        private static CombinationRecipe GetCombinationRecipe(string id)
        {
            return GetCombinationRecipes().First(recipe => recipe.Id == id);
        }

        private bool IsCombinationComplete(string id)
        {
            return IsCombinationComplete(GetCombinationRecipe(id));
        }

        private bool IsCombinationComplete(CombinationRecipe recipe)
        {
            return recipe.RequiredCardIds.All(HasDeckCard);
        }

        private int GetCombinationOwnedCount(CombinationRecipe recipe)
        {
            return recipe.RequiredCardIds.Count(HasDeckCard);
        }

        private bool IsBuildUnlocked(BuildRecipe recipe)
        {
            return buildUpgradeLevels.ContainsKey(recipe.Id) || HasRequiredBuildCards(recipe);
        }

        private int GetBuildUpgradeLevel(string buildId)
        {
            return buildUpgradeLevels.TryGetValue(buildId, out int level) ? level : 0;
        }

        private static int GetBuildUpgradeCost(int currentLevel)
        {
            return 60 + currentLevel * 45;
        }

        private bool HasRequiredBuildCards(BuildRecipe recipe)
        {
            return recipe.RequiredCardIds.All(HasDeckCard);
        }

        private bool HasDeckCard(string cardId)
        {
            return deck.Any(card => card.CardId == cardId);
        }

        private string GetMissingBuildCardNames(BuildRecipe recipe)
        {
            List<string> names = recipe.RequiredCardIds
                .Where(cardId => !HasDeckCard(cardId))
                .Select(GetCardDisplayName)
                .ToList();
            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }

        private string GetCardDisplayName(string cardId)
        {
            return cardPool.FirstOrDefault(card => card.CardId == cardId)?.DisplayName ?? cardId;
        }

        private void CheckBuildUnlocks()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (buildUpgradeLevels.ContainsKey(recipe.Id) || !HasRequiredBuildCards(recipe))
            {
                return;
            }

            buildUpgradeLevels[recipe.Id] = 0;
            AddLog($"빌드 완성: {recipe.Name}. 상점에서 강화할 수 있습니다.");
        }

        private string GetBuildStatusLabel()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (!IsBuildUnlocked(recipe))
            {
                int missingCount = recipe.RequiredCardIds.Count(cardId => !HasDeckCard(cardId));
                return $"빌드 {missingCount}장 필요";
            }

            return $"빌드 {recipe.Name}+{GetBuildUpgradeLevel(recipe.Id)}";
        }

        private string BuildRunOverviewText()
        {
            int attackCount = deck.Count(card => card.Category == CardCategory.Attack);
            int defenseCount = deck.Count(card => card.Category == CardCategory.Defense);

            return string.Join("\n", new[]
            {
                $"난이도 {GetDifficultyName(currentDifficulty)}   체력 {playerHealth}/{playerMaxHealth}   행운 {luck}",
                endlessModeActive
                    ? $"무한 기록 {roomsCleared}문   최고 {GetEndlessRecord()}문"
                    : $"방 {Mathf.Min(roomsCleared, TargetRooms)}/{TargetRooms}   전투 {combatEncountersCompleted}/{MinimumPreBossCombats}",
                $"덱 {deck.Count}/{GetMaxDeckSize()}장   공격 {attackCount} / 방어 {defenseCount}"
            });
        }

        private string BuildRunSummaryText()
        {
            int attackCount = deck.Count(card => card.Category == CardCategory.Attack);
            int defenseCount = deck.Count(card => card.Category == CardCategory.Defense);
            int skillCount = deck.Count(card => card.Category == CardCategory.Skill);
            int curseCount = deck.Count(card => card.Category == CardCategory.Curse);

            return string.Join("\n", new[]
            {
                $"난이도 {GetDifficultyName(currentDifficulty)}   체력 {playerHealth}/{playerMaxHealth}   행운 {luck}",
                $"금화 {gold}   빚 {debt}",
                endlessModeActive
                    ? $"무한 기록 {roomsCleared}문   최고 {GetEndlessRecord()}문   다음 보스 {nextEndlessBossRoom}문"
                    : $"방 {Mathf.Min(roomsCleared, TargetRooms)}/{TargetRooms}   전투 {combatEncountersCompleted}/{MinimumPreBossCombats}",
                $"연속 비전투 {consecutiveNonCombatDoors}/{MaxConsecutiveNonCombatDoors}",
                $"덱 {deck.Count}/{GetMaxDeckSize()}장   공격 {attackCount} / 방어 {defenseCount}",
                $"특수 {skillCount} / 저주카드 {curseCount}"
            });
        }

        private string BuildRunJudgementText()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            bool unlocked = IsBuildUnlocked(recipe);
            string healthState = playerHealth * 100 <= playerMaxHealth * 35
                ? "위험"
                : playerHealth * 100 <= playerMaxHealth * 60
                    ? "주의"
                    : "안정";
            string debtState = debt >= 10 ? "높음" : debt >= 5 ? "주의" : "낮음";
            string nextTarget = unlocked ? "강화 또는 생존 카드 확보" : $"필요 카드: {GetMissingBuildCardNames(recipe)}";

            return string.Join("\n", new[]
            {
                $"생존 {healthState} / 빚 {debtState}",
                $"빌드 {recipe.Name}: {(unlocked ? "완성" : "미완성")}",
                nextTarget
            });
        }

        private string BuildStatusDetailText()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            bool unlocked = IsBuildUnlocked(recipe);
            int level = GetBuildUpgradeLevel(recipe.Id);
            string status = unlocked ? $"완성 +{level}" : "미완성";
            string upgrade = unlocked
                ? level >= recipe.MaxUpgradeLevel
                    ? "강화: 최대"
                    : $"다음 강화: {GetBuildUpgradeCost(level)}금"
                : $"부족: {GetMissingBuildCardNames(recipe)}";

            List<string> requiredCards = recipe.RequiredCardIds
                .Select(cardId => $"{(HasDeckCard(cardId) ? "보유" : "필요")}: {GetCardDisplayName(cardId)}")
                .ToList();

            return $"{recipe.Name} ({status})\n효과: {recipe.Description}\n{string.Join("\n", requiredCards)}\n{upgrade}";
        }

        private string BuildCombinationOverviewText()
        {
            IReadOnlyList<CombinationRecipe> recipes = GetCombinationRecipes();
            int completedCount = recipes.Count(IsCombinationComplete);
            int nearlyCompleteCount = recipes.Count(recipe => !IsCombinationComplete(recipe) && GetCombinationOwnedCount(recipe) >= recipe.RequiredCardIds.Count - 1);
            string activeNames = string.Join(", ", recipes
                .Where(IsCombinationComplete)
                .OrderBy(recipe => recipe.Name)
                .Select(recipe => recipe.Name)
                .Take(2));

            return string.Join("\n", new[]
            {
                $"완성 {completedCount}/{recipes.Count}  근접 {nearlyCompleteCount}",
                "공격/방어/특수 조합",
                string.IsNullOrEmpty(activeNames) ? "카드 조합법에서 전체 확인" : $"활성: {activeNames}"
            });
        }

        private string BuildCombinationStatusText()
        {
            IReadOnlyList<CombinationRecipe> recipes = GetCombinationRecipes();
            int completedCount = recipes.Count(IsCombinationComplete);
            int nearlyCompleteCount = recipes.Count(recipe => !IsCombinationComplete(recipe) && GetCombinationOwnedCount(recipe) >= recipe.RequiredCardIds.Count - 1);

            List<string> lines = new()
            {
                $"완성 {completedCount}/{recipes.Count}   거의 완성 {nearlyCompleteCount}",
                "카드 3장을 모으면 조합 효과가 열립니다."
            };

            lines.AddRange(recipes
                .OrderByDescending(IsCombinationComplete)
                .ThenByDescending(GetCombinationOwnedCount)
                .ThenBy(recipe => recipe.Name)
                .Select(recipe =>
                {
                    int ownedCount = GetCombinationOwnedCount(recipe);
                    string state = IsCombinationComplete(recipe) ? "완성" : $"{ownedCount}/{recipe.RequiredCardIds.Count}";
                    string line = $"{state} {recipe.Name}: {CondenseCombinationSummary(recipe.EffectSummary)}";
                    return WrapDisplayLine(line, 36, "  ");
                }));

            return string.Join("\n", lines);
        }

        private string BuildShopCombinationText()
        {
            IReadOnlyList<CombinationRecipe> recipes = GetCombinationRecipes();
            int completedCount = recipes.Count(IsCombinationComplete);
            int nearlyCompleteCount = recipes.Count(recipe => !IsCombinationComplete(recipe) && GetCombinationOwnedCount(recipe) >= recipe.RequiredCardIds.Count - 1);

            List<string> lines = new()
            {
                $"완성 {completedCount}/{recipes.Count}  거의 완성 {nearlyCompleteCount}"
            };

            lines.AddRange(recipes
                .OrderByDescending(IsCombinationComplete)
                .ThenByDescending(GetCombinationOwnedCount)
                .ThenBy(recipe => recipe.Name)
                .Select(recipe =>
                {
                    string state = IsCombinationComplete(recipe)
                        ? "완성"
                        : $"{GetCombinationOwnedCount(recipe)}/{recipe.RequiredCardIds.Count}";
                    return $"{state} {recipe.Name}: {CondenseCombinationSummary(recipe.EffectSummary)}";
                }));

            return string.Join("\n", lines);
        }

        private List<CombinationRecipe> GetOrderedCombinationRecipesForDisplay()
        {
            return GetCombinationRecipes()
                .OrderByDescending(IsCombinationComplete)
                .ThenByDescending(GetCombinationOwnedCount)
                .ThenBy(recipe => recipe.Name)
                .ToList();
        }

        private string BuildShopCombinationColumnText(IEnumerable<CombinationRecipe> recipes)
        {
            return BuildCombinationColumnText(recipes, 32);
        }

        private string BuildCombinationColumnText(IEnumerable<CombinationRecipe> recipes, int maxCharacters)
        {
            return string.Join("\n", recipes.Select(recipe =>
            {
                string state = IsCombinationComplete(recipe)
                    ? "완성"
                    : $"{GetCombinationOwnedCount(recipe)}/{recipe.RequiredCardIds.Count}";
                string line = $"{state} {recipe.Name}: {CondenseCombinationSummary(recipe.EffectSummary)}";
                return WrapDisplayLine(line, maxCharacters, "  ");
            }));
        }

        private static string CondenseCombinationSummary(string summary)
        {
            return summary
                .Replace("전투 시작 ", string.Empty, StringComparison.Ordinal)
                .Replace("이번 턴 ", string.Empty, StringComparison.Ordinal)
                .Replace("고행운 ", string.Empty, StringComparison.Ordinal)
                .Replace("저체력 ", string.Empty, StringComparison.Ordinal)
                .Replace("공격 의도 ", string.Empty, StringComparison.Ordinal)
                .Replace(" / ", "/", StringComparison.Ordinal);
        }

        private static string WrapDisplayLine(string text, int maxCharacters, string continuationPrefix)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxCharacters)
            {
                return text;
            }

            List<string> lines = new();
            string remaining = text.Trim();
            string prefix = string.Empty;
            int lineLimit = maxCharacters;

            while (remaining.Length > lineLimit)
            {
                int breakIndex = FindDisplayLineBreak(remaining, lineLimit);
                lines.Add(prefix + remaining.Substring(0, breakIndex).TrimEnd());
                remaining = remaining.Substring(breakIndex).TrimStart();
                prefix = continuationPrefix;
                lineLimit = Mathf.Max(6, maxCharacters - continuationPrefix.Length);
            }

            lines.Add(prefix + remaining);
            return string.Join("\n", lines);
        }

        private static int FindDisplayLineBreak(string text, int maxCharacters)
        {
            int searchStart = Mathf.Max(1, maxCharacters - 8);
            int searchEnd = Mathf.Min(maxCharacters, text.Length - 1);
            for (int i = searchEnd; i >= searchStart; i -= 1)
            {
                char candidate = text[i - 1];
                if (char.IsWhiteSpace(candidate) || candidate == ':' || candidate == '/' || candidate == ',')
                {
                    return i;
                }
            }

            return Mathf.Min(maxCharacters, text.Length);
        }

        private string BuildDeckOverviewText()
        {
            int attackCount = deck.Count(card => card.Category == CardCategory.Attack);
            int defenseCount = deck.Count(card => card.Category == CardCategory.Defense);
            int skillCount = deck.Count(card => card.Category == CardCategory.Skill);
            int curseCount = deck.Count(card => card.Category == CardCategory.Curse);

            return string.Join("\n", new[]
            {
                $"총 {deck.Count}/{GetMaxDeckSize()}장",
                $"공격 {attackCount} / 방어 {defenseCount} / 특수 {skillCount}",
                curseCount > 0 ? $"저주카드 {curseCount}장 포함" : "눌러서 카드 목록 확인"
            });
        }

        private string BuildDeckOverviewCompactText()
        {
            int attackCount = deck.Count(card => card.Category == CardCategory.Attack);
            int defenseCount = deck.Count(card => card.Category == CardCategory.Defense);
            int skillCount = deck.Count(card => card.Category == CardCategory.Skill);
            int curseCount = deck.Count(card => card.Category == CardCategory.Curse);
            string curseText = curseCount > 0 ? $"저주카드 {curseCount}장 포함" : "저주카드 없음";
            return $"총 {deck.Count}/{GetMaxDeckSize()}장   공격 {attackCount} / 방어 {defenseCount} / 특수 {skillCount}\n{curseText}";
        }

        private string BuildDeckListText()
        {
            if (deck.Count == 0)
            {
                return "보유 카드가 없습니다.";
            }

            List<string> lines = deck
                .GroupBy(card => card.CardId)
                .Select(group => new { Card = group.First(), Count = group.Count() })
                .OrderBy(group => group.Card.Category)
                .ThenBy(group => group.Card.Cost)
                .ThenBy(group => group.Card.DisplayName)
                .Take(28)
                .Select(group =>
                {
                    string count = group.Count > 1 ? $" x{group.Count}" : string.Empty;
                    return $"{GetCardCategoryName(group.Card.Category)} {group.Card.Cost}  {group.Card.DisplayName}{count}";
                })
                .ToList();

            int hiddenCount = deck.GroupBy(card => card.CardId).Count() - lines.Count;
            if (hiddenCount > 0)
            {
                lines.Add($"외 {hiddenCount}종");
            }

            return string.Join("\n", lines);
        }

        private string BuildCharacterTraitSummaryText()
        {
            ClassProfile profile = GetClassProfile(selectedClass);
            BuildRecipe recipe = GetCurrentBuildRecipe();
            return string.Join("\n", new[]
            {
                $"{profile.Name} | {profile.Role}",
                profile.Tagline,
                IsHardModeFeatureActive() ? $"어려움 이상 특성 활성: {GetHardClassTraitName(selectedClass)}" : $"어려움 이상 특성 잠김: {GetHardClassTraitName(selectedClass)}",
                $"직업 조합: {recipe.Name}"
            });
        }

        private string BuildCharacterTraitText()
        {
            ClassProfile profile = GetClassProfile(selectedClass);
            return string.Join("\n\n", new[]
            {
                $"{profile.Name} | {profile.Role}",
                profile.Tagline,
                profile.Lore,
                $"기능\n{profile.Features}",
                $"특성\n{profile.Traits}",
                $"어려움 이상 전용 특성\n{GetHardClassTraitText(selectedClass)}",
                $"추천 빌드 카드\n{profile.RecommendedCards}",
                $"현재 직업 조합\n{BuildStatusDetailText()}"
            });
        }

        private static string GetHardClassTraitName(CharacterClass characterClass)
        {
            switch (characterClass)
            {
                case CharacterClass.Gambler:
                    return "파멸의 판돈";
                case CharacterClass.Oracle:
                    return "닫힌 운명 해석";
                case CharacterClass.Exile:
                    return "끝없는 속죄";
                default:
                    return "잠김";
            }
        }

        private static string GetHardClassTraitText(CharacterClass characterClass)
        {
            switch (characterClass)
            {
                case CharacterClass.Gambler:
                    return "행운 5 이상 첫 공격 강화, 행운 2 이하 첫 방어 강화. 전투 중 금화를 20 이상 얻으면 다음 공격이 강화됩니다.";
                case CharacterClass.Oracle:
                    return "예언으로 다음 카드 비용을 낮추고, 보존된 행운으로 첫 방어가 강화됩니다. 손패가 2장 이하가 되면 전투당 1회 카드를 뽑습니다.";
                case CharacterClass.Exile:
                    return "빚을 지우면 방어를 얻고, 빚이 없을 때 정화하면 체력을 회복합니다. 전투당 1회 치명상을 버티며 빚을 1 지웁니다.";
                default:
                    return "어려움 이상에서 해금됩니다.";
            }
        }

        private string BuildCombatAwakeningSummaryText()
        {
            ClassProfile profile = GetClassProfile(selectedClass);
            return string.Join("\n", new[]
            {
                "직업 특성 발동 가능",
                "카드 시너지 조합 발동",
                $"{profile.Name}: {profile.Role}"
            });
        }

        private string BuildCombatAwakeningText()
        {
            IReadOnlyList<CombinationRecipe> completedRecipes = GetCombinationRecipes()
                .Where(IsCombinationComplete)
                .OrderBy(recipe => recipe.Name)
                .ToList();
            string completedText = completedRecipes.Count == 0
                ? "아직 완성된 카드 시너지 조합이 없습니다."
                : string.Join("\n", completedRecipes.Select(recipe => $"- {recipe.Name}: {recipe.EffectSummary}"));

            return string.Join("\n\n", new[]
            {
                "전투 중 각성 특성과 완성된 카드 시너지는 조건을 만족할 때 실제 수치 효과로 발동합니다.",
                $"현재 직업 특성\n{GetClassProfile(selectedClass).Traits}",
                "전투 하단 상태 칸에는 현재 활성 조합이 표시되고, 조합 효과가 발동되는 순간에는 조합명이 임팩트 텍스트로 잠시 덮어 씌워집니다.",
                $"현재 완성 조합\n{completedText}",
                $"직업 조합 강화\n{BuildStatusDetailText()}"
            });
        }

        private string BuildDecisionHintText()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            int level = GetBuildUpgradeLevel(recipe.Id);
            if (playerHealth * 100 <= playerMaxHealth * 35)
            {
                return "체력이 낮습니다. 휴식이나 방어 카드를 우선 고려하세요.";
            }

            if (IsBuildUnlocked(recipe) && level < recipe.MaxUpgradeLevel && gold >= GetBuildUpgradeCost(level))
            {
                return "상점에서 빌드 강화를 바로 할 수 있습니다.";
            }

            if (!IsBuildUnlocked(recipe))
            {
                return $"빌드 완성까지 {GetMissingBuildCardNames(recipe)} 필요.";
            }

            if (debt >= 10)
            {
                return "빚이 높습니다. 대가의 문과 빚 증가 효과를 조심하세요.";
            }

            return "덱이 안정적이면 전투, 강화가 필요하면 상점을 노리세요.";
        }

        private static string GetCardCategoryName(CardCategory category)
        {
            return category switch
            {
                CardCategory.Attack => "공격",
                CardCategory.Defense => "방어",
                CardCategory.Skill => "특수",
                CardCategory.Curse => "저주카드",
                _ => "카드"
            };
        }

        private static ClassProfile GetClassProfile(CharacterClass characterClass)
        {
            return characterClass switch
            {
                CharacterClass.Oracle => new ClassProfile(
                    "점술가",
                    "정보 우위",
                    "문틈의 균열을 읽는 예언자입니다.",
                    "잊힌 신탁의 방에서 살아남은 마지막 해석자입니다. 세 문이 속삭이는 대가와 적의 첫 움직임을 남보다 먼저 읽지만, 몸으로 맞서는 힘은 약합니다.",
                    "잊힌 신탁의 방에서 살아남은 마지막 해석자입니다. 그녀는 문이 열리기 전 균열의 속삭임을 읽고, 보상 뒤에 숨은 대가와 적의 첫 움직임을 먼저 짚어냅니다. 예언은 완전하지 않지만, 한 호흡 빠른 판단이 점술가의 생존 방식입니다.",
                    "- 문 뒤의 위험과 보상을 더 선명하게 파악\n- 첫 전투부터 균열 읽기 보유\n- 낮은 최대 체력, 직접 화력은 부족",
                    "- 정확한 예언: 공격 의도에 방어 3회 대응\n- 다음 카드 비용 감소\n- 균열 엔진 강화 시 감소폭 증가",
                    "- 별자리 절단: 행운 기반 반복 피해\n- 예견된 방벽: 다음 턴까지 방어 유지\n- 세 문 점지: 문 정보와 드로우 확보"),
                CharacterClass.Exile => new ClassProfile(
                    "추방자",
                    "빚 저항",
                    "문 밖으로 버려졌으나 끝까지 버팁니다.",
                    "계약을 거부했다는 이유로 동굴 깊은 곳에 묶인 전사입니다. 쇠사슬과 대가에 익숙해 빚과 불행을 덜 받아내며, 체력이 낮을수록 방어적인 카드 가치가 커집니다.",
                    "계약을 거부했다는 이유로 동굴 깊은 곳에 묶인 전사입니다. 쇠사슬과 대가에 익숙해진 몸은 빚과 불행을 남보다 덜 받아내며, 상처가 깊어질수록 버티는 감각이 날카로워집니다. 그는 보상보다 다음 문까지 살아서 걷는 일을 먼저 선택합니다.",
                    "- 높은 최대 체력과 빚 저항\n- 시작 빚 1, 최후의 방어 보유\n- 문 힌트가 부족해 위험 판단이 어렵다",
                    "- 상처의 맹세: 저체력 진입 시 방어 획득\n- 저주 삼키기: 빚 제거 2회 후 다음 공격 강화\n- 최후의 맹세 완성 시 특성 강화",
                    "- 속박 절단: 피해, 취약, 빚 감소\n- 추방자의 맹세: 죽음 방지 생존 축\n- 낙인의 정화: 빚을 방어로 전환"),
                _ => new ClassProfile(
                    "도박사",
                    "행운 조작",
                    "주사위와 계약 사이에서 승부합니다.",
                    "문이 열리기 전부터 빚쟁이들의 이름을 외운 승부사입니다. 높은 행운을 만들면 폭발적인 피해를 내지만, 낮은 행운은 곧 빚과 상점 가격 압박으로 돌아옵니다.",
                    "문이 열리기 전부터 빚쟁이들의 이름을 외운 승부사입니다. 그는 문 앞에서도 판돈을 세고, 주사위가 멈추기 전의 떨림에서 흐름을 읽습니다. 높은 행운은 폭발적인 피해가 되지만, 낮은 행운은 곧 빚과 상점의 압박으로 돌아옵니다.",
                    "- 행운 주사위 결과를 활용한 폭발력\n- 시작 금화가 많아 초반 상점 선택이 좋음\n- 행운 1~2에서는 빚 관리가 핵심",
                    "- 패 읽기: 드로우/버리기 15회 달성\n- 다음 행운 주사위 3회 고점 확률 증가\n- 고점 도박 강화 시 횟수 증가",
                    "- 승부의 단검: 고행운 추가 피해\n- 판돈 방패: 저행운 턴 보정\n- 판세 뒤집기: 행운 재굴림과 드로우")
            };
        }

        private Sprite GetDiceSprite(int value)
        {
            List<Sprite> currentSprites = GetCurrentDiceSprites();
            int index = Mathf.Clamp(value - 1, 0, currentSprites.Count - 1);
            return currentSprites.Count == 0 ? null : currentSprites[index];
        }

        private List<Sprite> GetCurrentDiceSprites()
        {
            List<Sprite> classSprites = selectedClass switch
            {
                CharacterClass.Oracle => oracleDiceSprites,
                CharacterClass.Exile => exileDiceSprites,
                _ => gamblerDiceSprites
            };
            return classSprites.Count > 0 ? classSprites : diceSprites;
        }

        private List<Sprite> GetCurrentDiceRollSprites()
        {
            return selectedClass switch
            {
                CharacterClass.Oracle => oracleDiceRollSprites,
                CharacterClass.Exile => exileDiceRollSprites,
                _ => gamblerDiceRollSprites
            };
        }

        private Sprite GetCardFrameSprite(CardCategory category)
        {
            return category switch
            {
                CardCategory.Attack => attackCardFrameSprite,
                CardCategory.Defense => defenseCardFrameSprite,
                CardCategory.Skill => skillCardFrameSprite,
                _ => skillCardFrameSprite
            };
        }

        private Color GetIntentColor()
        {
            if (enemy.IntentAttack > 0)
            {
                return new Color(0.95f, 0.34f, 0.24f, 1f);
            }

            if (enemy.IntentDebt > 0)
            {
                return new Color(0.92f, 0.66f, 0.28f, 1f);
            }

            if (enemy.IntentHeal > 0)
            {
                return new Color(0.44f, 0.84f, 0.52f, 1f);
            }

            return new Color(0.44f, 0.90f, 0.88f, 1f);
        }

        private static string GetClassName(CharacterClass characterClass)
        {
            return characterClass switch
            {
                CharacterClass.Gambler => "도박사",
                CharacterClass.Oracle => "점술가",
                CharacterClass.Exile => "추방자",
                _ => "방랑자"
            };
        }

        private void AddHealthBar(RectTransform parent, int value, int max, Vector2 min, Vector2 maxAnchor, Color fillColor)
        {
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)value / max);
            RectTransform frameRoot = AddPanel(parent, "체력 프레임", new Color(1f, 1f, 1f, 0f));
            SetAnchors(frameRoot, min, maxAnchor);

            Image backing = AddImage(frameRoot, "체력 배경", new Color(0.025f, 0.018f, 0.02f, 0.94f));
            SetAnchors(backing.rectTransform, new Vector2(0.030f, 0.30f), new Vector2(0.970f, 0.70f));

            Image fill = AddImage(frameRoot, "체력 채움", fillColor);
            if (healthBarFillSprite != null)
            {
                fill.sprite = healthBarFillSprite;
                fill.color = Color.white;
            }

            SetAnchors(fill.rectTransform, new Vector2(0.030f, 0.30f), new Vector2(0.030f + 0.94f * ratio, 0.70f));

            if (healthBarFrameSprite != null)
            {
                Image frame = AddImage(frameRoot, "체력 장식", Color.white);
                frame.sprite = healthBarFrameSprite;
                Stretch(frame.rectTransform);
            }

            Text text = AddText(frameRoot, "체력 수치", $"{value} / {max}", 18, TextAnchor.MiddleCenter, new Color(0.98f, 0.90f, 0.75f, 1f));
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform);
        }

        private void AddStatusMeterBar(RectTransform parent, string name, string label, int value, int max, Vector2 min, Vector2 maxAnchor, Color fillColor)
        {
            int safeMax = Mathf.Max(1, max);
            int safeValue = Mathf.Clamp(value, 0, safeMax);
            float ratio = Mathf.Clamp01((float)safeValue / safeMax);

            RectTransform frameRoot = AddPanel(parent, $"{name} 프레임", new Color(1f, 1f, 1f, 0f));
            SetAnchors(frameRoot, min, maxAnchor);

            Text labelText = AddText(frameRoot, $"{name} 라벨", label, 14, TextAnchor.MiddleLeft, new Color(0.92f, 0.86f, 0.72f, 1f));
            labelText.fontStyle = FontStyle.Normal;
            labelText.resizeTextMinSize = 12;
            SetAnchors(labelText.rectTransform, new Vector2(0.012f, 0.080f), new Vector2(0.185f, 0.920f));

            RectTransform barRoot = AddPanel(frameRoot, $"{name} 바", new Color(1f, 1f, 1f, 0f));
            SetAnchors(barRoot, new Vector2(0.205f, 0.040f), new Vector2(0.985f, 0.960f));

            Image backing = AddImage(barRoot, $"{name} 배경", new Color(0.025f, 0.018f, 0.020f, 0.96f));
            SetAnchors(backing.rectTransform, new Vector2(0.030f, 0.290f), new Vector2(0.970f, 0.710f));

            Image fill = AddImage(barRoot, $"{name} 채움", fillColor);
            SetAnchors(fill.rectTransform, new Vector2(0.030f, 0.290f), new Vector2(0.030f + 0.940f * ratio, 0.710f));

            Image highlight = AddImage(barRoot, $"{name} 광택", new Color(0.92f, 1f, 0.96f, 0.22f));
            SetAnchors(highlight.rectTransform, new Vector2(0.030f, 0.610f), new Vector2(0.030f + 0.940f * ratio, 0.710f));

            if (healthBarFrameSprite != null)
            {
                Image frame = AddImage(barRoot, $"{name} 장식", Color.white);
                frame.sprite = healthBarFrameSprite;
                Stretch(frame.rectTransform);
            }

            Text valueText = AddText(frameRoot, $"{name} 수치", $"{safeValue} / {safeMax}", 13, TextAnchor.MiddleCenter, new Color(0.98f, 0.90f, 0.75f, 1f));
            valueText.fontStyle = FontStyle.Normal;
            valueText.resizeTextMinSize = 11;
            SetAnchors(valueText.rectTransform, new Vector2(0.205f, 0.000f), new Vector2(0.985f, 1.000f));
        }

        private RectTransform AddPanel(RectTransform parent, string name, Color color, Sprite spriteOverride = null)
        {
            Image image = AddImage(parent, name, color);
            image.raycastTarget = true;
            Sprite sprite = spriteOverride != null ? spriteOverride : panelSprite;
            if (sprite != null && color.a > 0f)
            {
                image.sprite = sprite;
                image.type = GetImageType(sprite);
                image.color = color;
            }
            return image.rectTransform;
        }

        private RectTransform AddEventMessagePanel(RectTransform parent, string name)
        {
            Image image = AddImage(parent, name, Color.white);
            image.raycastTarget = true;
            image.sprite = eventMessageFrameSprite != null
                ? eventMessageFrameSprite
                : settingsPanelSprite != null
                    ? settingsPanelSprite
                    : mainOptionsPanelSprite != null
                        ? mainOptionsPanelSprite
                        : panelSprite;
            image.type = GetImageType(image.sprite);
            return image.rectTransform;
        }

        private Image AddImage(RectTransform parent, string name, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text AddText(RectTransform parent, string name, string text, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            Text uiText = child.GetComponent<Text>();
            uiText.font = uiFont;
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.raycastTarget = false;
            uiText.supportRichText = false;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
            uiText.alignByGeometry = true;
            uiText.resizeTextForBestFit = false;
            uiText.resizeTextMinSize = Mathf.Max(9, fontSize - 8);
            uiText.resizeTextMaxSize = fontSize;
            return uiText;
        }

        private static void AddTextGlow(Text text, Color shadowColor, Color outlineColor, Vector2 shadowDistance)
        {
            Vector2 crispShadowDistance = new(
                Mathf.Clamp(shadowDistance.x, -1.15f, 1.15f),
                Mathf.Clamp(shadowDistance.y, -1.15f, 1.15f));

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = crispShadowDistance;
            shadow.useGraphicAlpha = true;

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(0.65f, -0.65f);
            outline.useGraphicAlpha = true;
        }

        private static Image.Type GetImageType(Sprite sprite)
        {
            return sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        }

        private Button AddButton(RectTransform parent, string name, string label, int fontSize, Color color)
        {
            RectTransform panel = AddPanel(parent, name, color);
            Image image = panel.GetComponent<Image>();
            Sprite idleSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : mainMenuButtonSprite != null
                    ? mainMenuButtonSprite
                    : settingsButtonSprite != null
                        ? settingsButtonSprite
                        : buttonIdleSprite;
            if (idleSprite != null)
            {
                image.sprite = idleSprite;
                image.type = GetImageType(idleSprite);
                image.color = Color.white;
            }

            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = mainMenuButtonHoverSprite != null
                ? mainMenuButtonHoverSprite
                : settingsButtonHoverSprite != null
                    ? settingsButtonHoverSprite
                    : buttonHoverSprite;
            spriteState.pressedSprite = mainMenuButtonPressedSprite != null
                ? mainMenuButtonPressedSprite
                : settingsButtonPressedSprite != null
                    ? settingsButtonPressedSprite
                    : buttonPressedSprite;
            spriteState.selectedSprite = spriteState.highlightedSprite;
            button.spriteState = spriteState;

            Text text = AddText(panel, "라벨", label, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));
            return button;
        }

        private Button AddSettingsMenuButton(RectTransform parent, string name, string label, int fontSize)
        {
            Image image = AddImage(parent, name, Color.white);
            image.sprite = settingsButtonSprite != null ? settingsButtonSprite : buttonIdleSprite;
            image.type = GetImageType(image.sprite);
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = settingsButtonHoverSprite != null ? settingsButtonHoverSprite : buttonHoverSprite;
            spriteState.pressedSprite = settingsButtonPressedSprite != null ? settingsButtonPressedSprite : buttonPressedSprite;
            spriteState.selectedSprite = spriteState.highlightedSprite;
            button.spriteState = spriteState;

            Text text = AddText(image.rectTransform, "라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.86f), new Color(0.54f, 0.42f, 0.24f, 0.72f), new Vector2(2.0f, -2.4f));
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.86f));
            return button;
        }

        private Button AddClassDetailButton(RectTransform parent, string name, string label, Sprite sprite, int fontSize)
        {
            Image image = AddImage(parent, name, Color.white);
            image.sprite = sprite != null ? sprite : settingsButtonSprite != null ? settingsButtonSprite : buttonIdleSprite;
            image.type = GetImageType(image.sprite);
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();

            Text text = AddText(image.rectTransform, "라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.88f), new Color(0.54f, 0.42f, 0.24f, 0.72f), new Vector2(2.0f, -2.4f));
            SetAnchors(text.rectTransform, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.82f));
            return button;
        }

        private Button AddMainMenuButton(RectTransform parent, string name, string label, int fontSize)
        {
            Image image = AddImage(parent, name, Color.white);
            image.sprite = mainMenuButtonSprite != null ? mainMenuButtonSprite : settingsButtonSprite != null ? settingsButtonSprite : buttonIdleSprite;
            image.type = GetImageType(image.sprite);
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = mainMenuButtonHoverSprite != null ? mainMenuButtonHoverSprite : settingsButtonHoverSprite;
            spriteState.pressedSprite = mainMenuButtonPressedSprite != null ? mainMenuButtonPressedSprite : settingsButtonPressedSprite;
            spriteState.selectedSprite = spriteState.highlightedSprite;
            button.spriteState = spriteState;

            Text text = AddText(image.rectTransform, "라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.88f), new Color(0.54f, 0.42f, 0.24f, 0.72f), new Vector2(2.0f, -2.4f));
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));
            return button;
        }

        private Button AddOptionToggleButton(RectTransform parent, string name, string label, int fontSize)
        {
            Image image = AddImage(parent, name, Color.white);
            image.sprite = mainOptionToggleSprite != null ? mainOptionToggleSprite : settingsButtonSprite != null ? settingsButtonSprite : buttonIdleSprite;
            image.type = GetImageType(image.sprite);
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = mainOptionToggleHoverSprite != null ? mainOptionToggleHoverSprite : settingsButtonHoverSprite;
            spriteState.pressedSprite = mainOptionTogglePressedSprite != null ? mainOptionTogglePressedSprite : settingsButtonPressedSprite;
            spriteState.selectedSprite = spriteState.highlightedSprite;
            button.spriteState = spriteState;

            Text text = AddText(image.rectTransform, "라벨", label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            AddTextGlow(text, new Color(0f, 0f, 0f, 0.86f), new Color(0.54f, 0.42f, 0.24f, 0.72f), new Vector2(1.8f, -2.1f));
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.86f));
            return button;
        }

        private Slider AddVolumeSlider(RectTransform parent)
        {
            Image rootImage = AddImage(parent, "소리 슬라이더", new Color(1f, 1f, 1f, 0f));
            rootImage.raycastTarget = true;

            Image frame = AddImage(rootImage.rectTransform, "소리 슬라이더 프레임", Color.white);
            frame.sprite = volumeSliderBarSprite != null
                ? volumeSliderBarSprite
                : mainOptionSliderSprite != null
                ? mainOptionSliderSprite
                : statusHintFrameSprite != null
                    ? statusHintFrameSprite
                    : settingsButtonSprite;
            frame.type = Image.Type.Simple;
            frame.raycastTarget = false;
            SetAnchors(frame.rectTransform, Vector2.zero, Vector2.one);

            RectTransform fillArea = AddPanel(rootImage.rectTransform, "소리 채움 안전영역", new Color(1f, 1f, 1f, 0f));
            SetAnchors(fillArea, new Vector2(0.055f, 0.405f), new Vector2(0.945f, 0.595f));

            Image track = AddImage(fillArea, "소리 홈", new Color(0.020f, 0.025f, 0.026f, 0.78f));
            track.raycastTarget = false;
            SetAnchors(track.rectTransform, Vector2.zero, Vector2.one);

            Image fill = AddImage(fillArea, "소리 채움", new Color(0.15f, 0.88f, 0.82f, 0.70f));
            fill.raycastTarget = false;
            SetAnchors(fill.rectTransform, Vector2.zero, Vector2.one);

            Image fillGlow = AddImage(fill.rectTransform, "소리 채움 빛", new Color(1f, 0.78f, 0.38f, 0.26f));
            fillGlow.raycastTarget = false;
            SetAnchors(fillGlow.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f));

            RectTransform handleArea = AddPanel(rootImage.rectTransform, "소리 손잡이 영역", new Color(1f, 1f, 1f, 0f));
            SetAnchors(handleArea, new Vector2(0.055f, 0.125f), new Vector2(0.945f, 0.875f));

            Image handle = AddImage(handleArea, "손잡이", Color.white);
            handle.sprite = settingsIconSprite != null
                ? settingsIconSprite
                : classInfoButtonSprite != null
                    ? classInfoButtonSprite
                    : settingsButtonSprite;
            handle.type = Image.Type.Simple;
            handle.preserveAspect = true;
            handle.raycastTarget = true;
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(42f, 42f);

            Slider slider = rootImage.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = AudioListener.volume;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(SetMasterVolume);
            return slider;
        }

        private Button AddIconButton(RectTransform parent, string name, Sprite sprite)
        {
            Image image = AddImage(parent, name, Color.white);
            image.sprite = sprite != null ? sprite : buttonIdleSprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            return button;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
            return colors;
        }

        private static ColorBlock CreateStaticButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            return colors;
        }

        private static ColorBlock CreateFixedButtonColors(Color color)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            colors.disabledColor = color;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            return colors;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i -= 1)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystem);
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetAnchors(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 min, Vector2 max)
        {
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private sealed class CombatVictoryEffect
        {
            public CombatVictoryEffect(
                RectTransform root,
                RectTransform portraitRoot,
                CanvasGroup group,
                Image burnOverlay,
                Image ashOverlay)
            {
                Root = root;
                PortraitRoot = portraitRoot;
                Group = group;
                BurnOverlay = burnOverlay;
                AshOverlay = ashOverlay;
            }

            public RectTransform Root { get; }
            public RectTransform PortraitRoot { get; }
            public CanvasGroup Group { get; }
            public Image PortraitImage { get; set; }
            public Image BurnOverlay { get; }
            public Image AshOverlay { get; }
            public Image CrackOverlay { get; set; }
            public Image ImpactOverlay { get; set; }
            public Image FlashOverlay { get; set; }
            public Image ShardBurstOverlay { get; set; }
            public Image VictoryLogo { get; set; }
            public Text VictoryText { get; set; }
            public List<VictoryCrackLine> CrackLines { get; } = new();
            public List<VictoryShard> Shards { get; } = new();
        }

        private sealed class VictoryCrackLine
        {
            public VictoryCrackLine(RectTransform rectTransform, Image image, Color color)
            {
                RectTransform = rectTransform;
                Image = image;
                Color = color;
            }

            public RectTransform RectTransform { get; }
            public Image Image { get; }
            public Color Color { get; }
        }

        private sealed class VictoryShard
        {
            public VictoryShard(
                RectTransform rectTransform,
                Image image,
                Vector2 basePosition,
                Vector2 direction,
                float speed,
                float startRotation,
                float rotationSpeed,
                Color color)
            {
                RectTransform = rectTransform;
                Image = image;
                BasePosition = basePosition;
                Direction = direction;
                Speed = speed;
                StartRotation = startRotation;
                RotationSpeed = rotationSpeed;
                Color = color;
            }

            public RectTransform RectTransform { get; }
            public Image Image { get; }
            public Vector2 BasePosition { get; }
            public Vector2 Direction { get; }
            public float Speed { get; }
            public float StartRotation { get; }
            public float RotationSpeed { get; }
            public Color Color { get; }
        }

        private readonly struct WeightedDoorType
        {
            public WeightedDoorType(DoorType type, float weight)
            {
                Type = type;
                Weight = weight;
            }

            public DoorType Type { get; }
            public float Weight { get; }
        }

        private sealed class DoorOption
        {
            public DoorOption(DoorType type, string name, string hint, string risk)
            {
                Type = type;
                Name = name;
                Hint = hint;
                Risk = risk;
            }

            public DoorType Type { get; }
            public string Name { get; }
            public string Hint { get; }
            public string Risk { get; }
        }

        private readonly struct UiVisibilitySnapshot
        {
            public UiVisibilitySnapshot(GameObject target, bool wasActive)
            {
                Target = target;
                WasActive = wasActive;
            }

            public GameObject Target { get; }
            public bool WasActive { get; }
        }

        private readonly struct ClassProfile
        {
            public ClassProfile(string name, string role, string tagline, string lore, string worldLore, string features, string traits, string recommendedCards)
            {
                Name = name;
                Role = role;
                Tagline = tagline;
                Lore = lore;
                WorldLore = worldLore;
                Features = features;
                Traits = traits;
                RecommendedCards = recommendedCards;
            }

            public string Name { get; }
            public string Role { get; }
            public string Tagline { get; }
            public string Lore { get; }
            public string WorldLore { get; }
            public string Features { get; }
            public string Traits { get; }
            public string RecommendedCards { get; }
        }

        private readonly struct BuildRecipe
        {
            public BuildRecipe(string id, string name, string description, int maxUpgradeLevel, params string[] requiredCardIds)
            {
                Id = id;
                Name = name;
                Description = description;
                MaxUpgradeLevel = maxUpgradeLevel;
                RequiredCardIds = requiredCardIds;
            }

            public string Id { get; }
            public string Name { get; }
            public string Description { get; }
            public int MaxUpgradeLevel { get; }
            public IReadOnlyList<string> RequiredCardIds { get; }
        }

        private readonly struct CombinationRecipe
        {
            public CombinationRecipe(string id, string name, string effectSummary, params string[] requiredCardIds)
            {
                Id = id;
                Name = name;
                EffectSummary = effectSummary;
                RequiredCardIds = requiredCardIds;
            }

            public string Id { get; }
            public string Name { get; }
            public string EffectSummary { get; }
            public IReadOnlyList<string> RequiredCardIds { get; }
        }

        [Serializable]
        private sealed class RunSaveData
        {
            public int version;
            public int selectedClass;
            public int currentDifficulty;
            public int currentJourneyEndingKind;
            public bool endlessModeActive;
            public int nextEndlessBossRoom;
            public int endlessBossesDefeated;
            public int playerMaxHealth;
            public int playerHealth;
            public int playerBlock;
            public int action;
            public int luck;
            public int gold;
            public int debt;
            public int roomsCleared;
            public int combatEncountersCompleted;
            public int consecutiveNonCombatDoors;
            public int storedLuck;
            public int curseReduction;
            public bool hasStoredLuck;
            public bool keepLuckNextTurn;
            public int doorInsightLevel;
            public bool retainBlockNextTurn;
            public List<string> deckCardIds = new();
            public List<string> equippedItemIds = new();
            public List<string> combatLog = new();
            public List<RunSaveBuildUpgrade> buildUpgradeLevels = new();
        }

        [Serializable]
        private sealed class EquippedItemSaveData
        {
            public List<string> itemIds = new();
        }

        [Serializable]
        private sealed class RunModifierCatalogData
        {
            public int slotLimitPerCharacter = MaxEquippedRunItems;
            public string iconRoot = string.Empty;
            public List<RunModifierCatalogEntry> modifiers = new();
        }

        [Serializable]
        private sealed class RunModifierCatalogEntry
        {
            public string id = string.Empty;
            public string category = string.Empty;
            public string icon = string.Empty;
            public string name = string.Empty;
            public string effect = string.Empty;
            public string description = string.Empty;
        }

        [Serializable]
        private sealed class RunSaveBuildUpgrade
        {
            public string id = string.Empty;
            public int level;
        }

        private sealed class RunItemDefinition
        {
            public RunItemDefinition(string id, string name, RunItemType type, string effect, string description, string iconName)
            {
                Id = id;
                Name = name;
                Type = type;
                Effect = effect;
                Description = description;
                IconName = iconName;
            }

            public string Id { get; }
            public string Name { get; }
            public RunItemType Type { get; }
            public string Effect { get; }
            public string Description { get; }
            public string IconName { get; }
        }

        private readonly struct EnemyTemplate
        {
            public EnemyTemplate(string id, string name, int health, int attack, int block)
            {
                Id = id;
                Name = name;
                Health = health;
                Attack = attack;
                Block = block;
            }

            public string Id { get; }
            public string Name { get; }
            public int Health { get; }
            public int Attack { get; }
            public int Block { get; }
        }

        private sealed class EnemyState
        {
            public EnemyState(string id, string name, int maxHealth, int baseAttack, int baseBlock, bool wasElite, bool isBoss, int baseGoldReward)
            {
                Id = id;
                Name = name;
                MaxHealth = maxHealth;
                Health = maxHealth;
                BaseAttack = baseAttack;
                BaseBlock = baseBlock;
                WasElite = wasElite;
                IsBoss = isBoss;
                BaseGoldReward = baseGoldReward;
            }

            public string Id { get; }
            public string Name { get; }
            public int MaxHealth { get; }
            public int Health { get; set; }
            public int Block { get; set; }
            public int Bleed { get; set; }
            public int Vulnerable { get; set; }
            public int BaseAttack { get; }
            public int BaseBlock { get; }
            public int IntentAttack { get; set; }
            public int IntentBlock { get; set; }
            public int IntentDebt { get; set; }
            public int IntentHeal { get; set; }
            public EnemySpecialEffect IntentSpecialEffect { get; set; }
            public int IntentSpecialAmount { get; set; }
            public string IntentSpecialLabel { get; set; } = string.Empty;
            public string IntentCardName { get; set; } = string.Empty;
            public string CandidateLabel { get; set; } = string.Empty;
            public string IntentLabel { get; set; } = string.Empty;
            public bool WasElite { get; }
            public bool IsBoss { get; }
            public int BaseGoldReward { get; }
        }

        private sealed class EnemyCardDefinition
        {
            public EnemyCardDefinition(
                string name,
                bool attacks = false,
                int attackBonus = 0,
                int blockAmount = 0,
                int debtAmount = 0,
                int healAmount = 0,
                EnemySpecialEffect specialEffect = EnemySpecialEffect.None,
                int specialAmount = 0,
                string specialLabel = "")
            {
                Name = name;
                Attacks = attacks;
                AttackBonus = attackBonus;
                BlockAmount = blockAmount;
                DebtAmount = debtAmount;
                HealAmount = healAmount;
                SpecialEffect = specialEffect;
                SpecialAmount = specialAmount;
                SpecialLabel = specialLabel;
            }

            public string Name { get; }
            public bool Attacks { get; }
            public int AttackBonus { get; }
            public int BlockAmount { get; }
            public int DebtAmount { get; }
            public int HealAmount { get; }
            public EnemySpecialEffect SpecialEffect { get; }
            public int SpecialAmount { get; }
            public string SpecialLabel { get; }
        }
    }
}

