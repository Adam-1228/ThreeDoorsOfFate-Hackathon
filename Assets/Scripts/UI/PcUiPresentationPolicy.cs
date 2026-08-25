using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDoorsOfFate.UI
{
    public enum CardPresentationMode
    {
        Compact,
        Standard,
        Detail
    }

    public readonly struct UiNormalizedRect
    {
        public UiNormalizedRect(float minX, float minY, float maxX, float maxY)
        {
            if (minX < 0f || minY < 0f || maxX > 1f || maxY > 1f || minX >= maxX || minY >= maxY)
            {
                throw new ArgumentOutOfRangeException(nameof(minX), "Normalized UI bounds must be ordered and stay inside 0..1.");
            }

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public bool IsValid => MinX >= 0f && MinY >= 0f && MaxX <= 1f && MaxY <= 1f && MinX < MaxX && MinY < MaxY;

        public UiNormalizedRect MapChild(UiNormalizedRect child)
        {
            return new UiNormalizedRect(
                MinX + Width * child.MinX,
                MinY + Height * child.MinY,
                MinX + Width * child.MaxX,
                MinY + Height * child.MaxY);
        }
    }

    public readonly struct CardPresentationStyle
    {
        public CardPresentationStyle(
            bool showCost,
            bool showTitle,
            bool showRules,
            int titleFontSize,
            int rulesFontSize,
            UiNormalizedRect illustration,
            UiNormalizedRect title,
            UiNormalizedRect rules)
        {
            ShowCost = showCost;
            ShowTitle = showTitle;
            ShowRules = showRules;
            TitleFontSize = titleFontSize;
            RulesFontSize = rulesFontSize;
            Illustration = illustration;
            Title = title;
            Rules = rules;
        }

        public bool ShowCost { get; }
        public bool ShowTitle { get; }
        public bool ShowRules { get; }
        public int TitleFontSize { get; }
        public int RulesFontSize { get; }
        public UiNormalizedRect Illustration { get; }
        public UiNormalizedRect Title { get; }
        public UiNormalizedRect Rules { get; }
    }

    public static class CardPresentationPolicy
    {
        public static CardPresentationStyle For(CardPresentationMode mode)
        {
            switch (mode)
            {
                case CardPresentationMode.Compact:
                    return new CardPresentationStyle(
                        true,
                        true,
                        false,
                        19,
                        0,
                        new UiNormalizedRect(0.08f, 0.30f, 0.92f, 0.91f),
                        new UiNormalizedRect(0.10f, 0.13f, 0.90f, 0.30f),
                        new UiNormalizedRect(0.10f, 0.04f, 0.90f, 0.13f));

                case CardPresentationMode.Standard:
                    return new CardPresentationStyle(
                        true,
                        true,
                        true,
                        22,
                        16,
                        new UiNormalizedRect(0.08f, 0.42f, 0.92f, 0.91f),
                        new UiNormalizedRect(0.10f, 0.29f, 0.90f, 0.42f),
                        new UiNormalizedRect(0.10f, 0.07f, 0.90f, 0.28f));

                case CardPresentationMode.Detail:
                    return new CardPresentationStyle(
                        true,
                        true,
                        true,
                        30,
                        22,
                        new UiNormalizedRect(0.07f, 0.43f, 0.93f, 0.93f),
                        new UiNormalizedRect(0.09f, 0.30f, 0.91f, 0.43f),
                        new UiNormalizedRect(0.09f, 0.07f, 0.91f, 0.29f));

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown card presentation mode.");
            }
        }
    }

    public static class SettingsGearSpriteFactory
    {
        private const float CropScale = 0.56f;

        public static Sprite Create(Sprite source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Rect sourceRect = source.rect;
            float cropSize = Mathf.Min(sourceRect.width, sourceRect.height) * CropScale;
            Rect cropRect = new(
                sourceRect.center.x - cropSize * 0.5f,
                sourceRect.center.y - cropSize * 0.5f,
                cropSize,
                cropSize);
            Sprite cropped = Sprite.Create(
                source.texture,
                cropRect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            cropped.name = $"{source.name}_gear_crop";
            return cropped;
        }
    }

    public static class PcUiLayoutPolicy
    {
        public static readonly UiNormalizedRect TopBar = new UiNormalizedRect(0.025f, 0.880f, 0.975f, 0.988f);
        public static readonly UiNormalizedRect HeaderTitle = new UiNormalizedRect(0.025f, 0.18f, 0.285f, 0.82f);
        public static readonly UiNormalizedRect HeaderPrompt = new UiNormalizedRect(0.295f, 0.18f, 0.665f, 0.82f);
        public static readonly UiNormalizedRect HeaderPromptRoot = TopBar.MapChild(HeaderPrompt);
        public static readonly UiNormalizedRect HeaderStats = new UiNormalizedRect(0.675f, 0.18f, 0.915f, 0.82f);
        public static readonly UiNormalizedRect HeaderSettings = new UiNormalizedRect(0.925f, 0.18f, 0.985f, 0.82f);
        public static readonly UiNormalizedRect ClassDetailTagline = new UiNormalizedRect(0.300f, 0.905f, 0.700f, 0.985f);

        public static readonly UiNormalizedRect AchievementWindow = new UiNormalizedRect(0.045f, 0.055f, 0.955f, 0.885f);
        public static readonly UiNormalizedRect AchievementHeading = new UiNormalizedRect(0.340f, 0.900f, 0.660f, 0.975f);
        public static readonly UiNormalizedRect AchievementProgress = new UiNormalizedRect(0.045f, 0.900f, 0.205f, 0.970f);
        public static readonly UiNormalizedRect AchievementClose = new UiNormalizedRect(0.840f, 0.900f, 0.955f, 0.970f);
        public static readonly UiNormalizedRect AchievementCardIcon = new UiNormalizedRect(0.035f, 0.120f, 0.235f, 0.880f);
        public static readonly UiNormalizedRect AchievementCardTitle = new UiNormalizedRect(0.265f, 0.565f, 0.965f, 0.900f);
        public static readonly UiNormalizedRect AchievementCardBody = new UiNormalizedRect(0.265f, 0.100f, 0.965f, 0.525f);
        public static readonly UiNormalizedRect AchievementCardTextSafe = new UiNormalizedRect(0.070f, 0.140f, 0.930f, 0.860f);

        public static readonly UiNormalizedRect StatusModalSafe = new UiNormalizedRect(0.07f, 0.09f, 0.93f, 0.86f);
        public static readonly UiNormalizedRect StatusEquipmentPanel = new UiNormalizedRect(0.070f, 0.175f, 0.315f, 0.815f);
        public static readonly UiNormalizedRect StatusSynergyCard = new UiNormalizedRect(0.340f, 0.515f, 0.625f, 0.815f);
        public static readonly UiNormalizedRect StatusDeckCard = new UiNormalizedRect(0.650f, 0.515f, 0.930f, 0.815f);
        public static readonly UiNormalizedRect StatusAwakeningCard = new UiNormalizedRect(0.340f, 0.175f, 0.625f, 0.485f);
        public static readonly UiNormalizedRect StatusTraitsCard = new UiNormalizedRect(0.650f, 0.175f, 0.930f, 0.485f);
        public static readonly UiNormalizedRect StatusDetailBody = new UiNormalizedRect(0.090f, 0.160f, 0.910f, 0.770f);
        public static readonly UiNormalizedRect StatusFramedTextSafe = new UiNormalizedRect(0.120f, 0.120f, 0.880f, 0.880f);
        public static readonly UiNormalizedRect StatusCompactFrameTextSafe = new UiNormalizedRect(0.080f, 0.190f, 0.920f, 0.810f);
        public static readonly UiNormalizedRect CollectionGrid = new UiNormalizedRect(0.10f, 0.46f, 0.90f, 0.80f);
        public static readonly UiNormalizedRect CollectionDetail = new UiNormalizedRect(0.11f, 0.19f, 0.89f, 0.43f);
        public static readonly UiNormalizedRect LogPanel = new UiNormalizedRect(0.765f, 0.120f, 0.950f, 0.880f);
        public static readonly UiNormalizedRect LogHeading = new UiNormalizedRect(0.04f, 0.91f, 0.96f, 0.985f);
        public static readonly UiNormalizedRect LogBody = new UiNormalizedRect(0.105f, 0.075f, 0.895f, 0.855f);
        public static readonly UiNormalizedRect LogTextSafe = new UiNormalizedRect(0.090f, 0.055f, 0.930f, 0.945f);
        public static readonly UiNormalizedRect DoorHintSafe = new UiNormalizedRect(0.160f, 0.180f, 0.840f, 0.275f);

        private static readonly UiNormalizedRect[] SafeAreas =
        {
            TopBar,
            HeaderPromptRoot,
            ClassDetailTagline,
            AchievementWindow,
            AchievementHeading,
            AchievementProgress,
            AchievementClose,
            AchievementCardIcon,
            AchievementCardTitle,
            AchievementCardBody,
            AchievementCardTextSafe,
            StatusModalSafe,
            StatusEquipmentPanel,
            StatusSynergyCard,
            StatusDeckCard,
            StatusAwakeningCard,
            StatusTraitsCard,
            StatusDetailBody,
            StatusFramedTextSafe,
            StatusCompactFrameTextSafe,
            CollectionGrid,
            CollectionDetail,
            LogPanel,
            LogHeading,
            LogBody,
            LogTextSafe,
            DoorHintSafe
        };

        public static IReadOnlyList<UiNormalizedRect> AllSafeAreas => SafeAreas;
    }
}
