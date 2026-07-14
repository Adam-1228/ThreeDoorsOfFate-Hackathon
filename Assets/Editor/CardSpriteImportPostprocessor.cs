using System;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Editor
{
    public sealed class CardSpriteImportPostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";

        private void OnPreprocessTexture()
        {
            if (!IsGameArt(assetPath))
            {
                return;
            }

            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.spritePixelsPerUnit = 100;
            textureImporter.alphaIsTransparency = true;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.maxTextureSize = IsFullRenderedCardArt(assetPath) ? 2048 : IsCardIllustration(assetPath) || IsDoorArt(assetPath) ? 1024 : 2048;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.sRGBTexture = true;

            if (IsFullRenderedCardArt(assetPath))
            {
                textureImporter.mipmapEnabled = false;
                textureImporter.anisoLevel = 1;
                textureImporter.filterMode = FilterMode.Bilinear;
            }
            else if (IsCardIllustration(assetPath) || IsDoorArt(assetPath))
            {
                textureImporter.mipmapEnabled = true;
                textureImporter.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                textureImporter.mipMapBias = IsDoorArt(assetPath) ? 0.45f : 0.32f;
                textureImporter.anisoLevel = 4;
                textureImporter.filterMode = FilterMode.Trilinear;
            }
            else
            {
                textureImporter.mipmapEnabled = false;
                textureImporter.anisoLevel = 1;
            }

            TextureImporterSettings textureSettings = new();
            textureImporter.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            if (IsGeneratedFrameArt(assetPath))
            {
                textureSettings.spriteBorder = GetGeneratedFrameBorder(assetPath);
            }
            textureImporter.SetTextureSettings(textureSettings);
        }

        private static bool IsGameArt(string path)
        {
            return path.StartsWith(ArtRoot, StringComparison.Ordinal)
                && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCardIllustration(string path)
        {
            return path.IndexOf("/Cards/Illustrations/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\Cards\\Illustrations\\", StringComparison.Ordinal) >= 0;
        }

        private static bool IsFullRenderedCardArt(string path)
        {
            return path.IndexOf("/Cards/FullRendered/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\Cards\\FullRendered\\", StringComparison.Ordinal) >= 0
                || path.IndexOf("/Cards/UnifiedRendered/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\Cards\\UnifiedRendered\\", StringComparison.Ordinal) >= 0
                || path.IndexOf("/Cards/HardRendered/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\Cards\\HardRendered\\", StringComparison.Ordinal) >= 0;
        }

        private static bool IsDoorArt(string path)
        {
            return path.IndexOf("/Doors/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\Doors\\", StringComparison.Ordinal) >= 0;
        }

        private static bool IsGeneratedFrameArt(string path)
        {
            return path.IndexOf("/UI/GeneratedFrames/", StringComparison.Ordinal) >= 0
                || path.IndexOf("\\UI\\GeneratedFrames\\", StringComparison.Ordinal) >= 0;
        }

        private static Vector4 GetGeneratedFrameBorder(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.EndsWith("ui_top_bar_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(240f, 56f, 240f, 56f);
            }

            if (normalized.EndsWith("ui_log_panel_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(110f, 130f, 110f, 130f);
            }

            if (normalized.EndsWith("ui_event_message_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(160f, 120f, 160f, 120f);
            }

            if (normalized.EndsWith("ui_door_choice_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(115f, 145f, 115f, 145f);
            }

            if (normalized.EndsWith("ui_enemy_status_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(180f, 130f, 180f, 130f);
            }

            if (normalized.EndsWith("ui_deck_box_frame.png", StringComparison.Ordinal)
                || normalized.EndsWith("ui_deck_box_frame_v2.png", StringComparison.Ordinal)
                || normalized.EndsWith("ui_deck_box_frame_v3.png", StringComparison.Ordinal))
            {
                return new Vector4(110f, 160f, 110f, 160f);
            }

            if (normalized.EndsWith("ui_inner_panel_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(150f, 110f, 150f, 110f);
            }

            if (normalized.EndsWith("ui_status_modal_frame_v2.png", StringComparison.Ordinal))
            {
                return new Vector4(190f, 150f, 190f, 150f);
            }

            if (normalized.EndsWith("ui_status_section_wide_frame_v2.png", StringComparison.Ordinal))
            {
                return new Vector4(170f, 130f, 170f, 130f);
            }

            if (normalized.EndsWith("ui_status_section_tall_frame_v2.png", StringComparison.Ordinal))
            {
                return new Vector4(135f, 175f, 135f, 175f);
            }

            if (normalized.EndsWith("ui_status_section_medium_frame_v2.png", StringComparison.Ordinal))
            {
                return new Vector4(145f, 145f, 145f, 145f);
            }

            if (normalized.EndsWith("ui_status_hint_bar_frame_v2.png", StringComparison.Ordinal))
            {
                return new Vector4(230f, 90f, 230f, 90f);
            }

            if (normalized.EndsWith("ui_status_category_card_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(230f, 150f, 230f, 150f);
            }

            if (normalized.EndsWith("ui_shop_combination_panel_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(160f, 190f, 160f, 190f);
            }

            if (normalized.EndsWith("ui_class_back_button_frame.png", StringComparison.Ordinal)
                || normalized.EndsWith("ui_class_confirm_button_frame.png", StringComparison.Ordinal))
            {
                return new Vector4(240f, 80f, 240f, 80f);
            }

            return new Vector4(96f, 96f, 96f, 96f);
        }
    }
}
