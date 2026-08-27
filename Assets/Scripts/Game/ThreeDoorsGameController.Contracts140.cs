using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string StarterContractResourcePath =
            "GameData/V140/starter_contracts";

        private StarterContractCatalog cachedStarterContractCatalog;
        private bool starterContractCatalogLoadAttempted;
        private string selectedStarterContractId = string.Empty;

        private void ShowStarterContractSelection(CharacterClass characterClass)
        {
            selectedClass = characterClass;
            if (!TryGetStarterContractCatalog(out StarterContractCatalog catalog))
            {
                StartRun(characterClass, string.Empty);
                return;
            }

            IReadOnlyList<StarterContractDefinition> contracts =
                catalog.GetContracts(characterClass);
            if (contracts.Count != 3)
            {
                Debug.LogError(
                    $"Starter contract selection expected 3 entries for {characterClass}.");
                StartRun(characterClass, string.Empty);
                return;
            }

            phase = GamePhase.ContractSelection;
            SetBackground(classSelectBackground);
            ClearContent();
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(
                contentRoot,
                new Vector2(0.045f, 0.070f),
                new Vector2(0.955f, 0.925f));

            titleText.text = L("contract.ui.title");
            BindLocalizedText(titleText, "contract.ui.title");
            subtitleText.text = L("contract.ui.subtitle");
            BindLocalizedText(subtitleText, "contract.ui.subtitle");

            Button backButton = AddLocalizedSettingsMenuButton(
                contentRoot,
                "계약 뒤로",
                "contract.ui.back",
                15);
            SetAnchors(
                backButton.GetComponent<RectTransform>(),
                new Vector2(0.020f, 0.905f),
                new Vector2(0.160f, 0.985f));
            backButton.onClick.AddListener(() => ShowClassDetail(characterClass));

            Button settings = AddLocalizedSettingsMenuButton(
                contentRoot,
                "계약 설정",
                "menu.settings",
                15);
            SetAnchors(
                settings.GetComponent<RectTransform>(),
                new Vector2(0.840f, 0.905f),
                new Vector2(0.980f, 0.985f));
            settings.onClick.AddListener(ToggleSettingsPanel);
            settings.transform.SetAsLastSibling();

            for (int index = 0; index < contracts.Count; index += 1)
            {
                AddStarterContractChoice(
                    contentRoot,
                    characterClass,
                    contracts[index],
                    index);
            }

            primaryButton.gameObject.SetActive(false);
        }

        private void AddStarterContractChoice(
            RectTransform parent,
            CharacterClass characterClass,
            StarterContractDefinition contract,
            int index)
        {
            const float gap = 0.020f;
            const float leftEdge = 0.018f;
            const float width = 0.308f;
            float left = leftEdge + index * (width + gap);
            Sprite frame = statusSectionTallFrameSprite != null
                ? statusSectionTallFrameSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform panel = AddPanel(
                parent,
                $"운명 계약 {index + 1}",
                Color.white,
                frame);
            SetAnchors(
                panel,
                new Vector2(left, 0.115f),
                new Vector2(left + width, 0.875f));

            Text name = AddLocalizedText(
                panel,
                "계약 이름",
                contract.NameKey,
                27,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.87f, 0.56f, 1f));
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 18;
            name.resizeTextMaxSize = 27;
            SetAnchors(name.rectTransform, new Vector2(0.10f, 0.805f), new Vector2(0.90f, 0.930f));

            Text role = AddLocalizedText(
                panel,
                "계약 역할",
                contract.RoleKey,
                18,
                TextAnchor.MiddleCenter,
                new Color(0.62f, 1f, 0.94f, 1f));
            role.fontStyle = FontStyle.Bold;
            SetAnchors(role.rectTransform, new Vector2(0.10f, 0.720f), new Vector2(0.90f, 0.805f));

            Text description = AddLocalizedText(
                panel,
                "계약 설명",
                contract.DescriptionKey,
                17,
                TextAnchor.UpperCenter,
                new Color(0.91f, 0.87f, 0.78f, 1f));
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 13;
            description.resizeTextMaxSize = 17;
            description.lineSpacing = 1.02f;
            SetAnchors(
                description.rectTransform,
                new Vector2(0.095f, 0.535f),
                new Vector2(0.905f, 0.705f));

            Text changes = AddText(
                panel,
                "계약 변경점",
                BuildStarterContractChangeSummary(contract),
                15,
                TextAnchor.UpperLeft,
                new Color(0.84f, 0.96f, 0.93f, 1f));
            changes.resizeTextForBestFit = true;
            changes.resizeTextMinSize = 11;
            changes.resizeTextMaxSize = 15;
            changes.lineSpacing = 0.94f;
            SetAnchors(
                changes.rectTransform,
                new Vector2(0.105f, 0.235f),
                new Vector2(0.895f, 0.515f));

            Button select = AddClassDetailActionButton(
                panel,
                $"운명 계약 선택 {index + 1}",
                L("contract.ui.select"),
                18,
                classConfirmButtonSprite,
                GameSfxCue.ImportantConfirm);
            BindLocalizedText(
                select.GetComponentInChildren<Text>(),
                "contract.ui.select");
            SetAnchors(
                select.GetComponent<RectTransform>(),
                new Vector2(0.130f, 0.065f),
                new Vector2(0.870f, 0.205f));
            select.onClick.AddListener(() => StartRun(characterClass, contract.Id));
        }

        private string BuildStarterContractChangeSummary(
            StarterContractDefinition contract)
        {
            List<string> lines = new();
            foreach (StarterCardSwapDefinition swap in contract.Swaps)
            {
                CardData removed = cardPool.FirstOrDefault(card =>
                    card != null && card.CardId == swap.RemoveCardId);
                CardData added = cardPool.FirstOrDefault(card =>
                    card != null && card.CardId == swap.AddCardId);
                string removedName = removed != null
                    ? GetLocalizedCardName(removed)
                    : swap.RemoveCardId;
                string addedName = added != null
                    ? GetLocalizedCardName(added)
                    : swap.AddCardId;
                lines.Add(LF(
                    "contract.ui.swapLine",
                    removedName,
                    addedName,
                    swap.Count));
            }

            if (lines.Count == 0)
            {
                lines.Add(L("contract.ui.noSwap"));
            }

            lines.Add(LF(
                "contract.ui.resourceDelta",
                FormatSigned(contract.StartingGoldDelta),
                FormatSigned(contract.StartingHealthDelta),
                FormatSigned(contract.StartingLuckDelta),
                FormatSigned(contract.StartingDebtDelta)));
            return string.Join("\n", lines);
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private bool TryInitializeStarterContractDeck(
            CharacterClass characterClass,
            string contractId)
        {
            if (!TryGetStarterContractCatalog(out StarterContractCatalog catalog))
            {
                return false;
            }

            string resolvedContractId = string.IsNullOrWhiteSpace(contractId)
                ? GetDefaultStarterContractId(characterClass)
                : contractId;
            try
            {
                Dictionary<string, CardData> cards = cardPool
                    .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CardId))
                    .GroupBy(card => card.CardId, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First(),
                        StringComparer.Ordinal);
                List<CardData> builtDeck = new StarterDeckBuilder(catalog).Build(
                    characterClass,
                    resolvedContractId,
                    cards);
                deck.Clear();
                deck.AddRange(builtDeck);
                Shuffle(deck);
                selectedStarterContractId = resolvedContractId;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starter contract deck fallback for {characterClass}: {exception.Message}");
                return false;
            }
        }

        private StarterContractDefinition GetSelectedStarterContract(
            CharacterClass characterClass,
            string contractId)
        {
            if (!TryGetStarterContractCatalog(out StarterContractCatalog catalog))
            {
                return null;
            }

            string resolvedContractId = string.IsNullOrWhiteSpace(contractId)
                ? GetDefaultStarterContractId(characterClass)
                : contractId;
            try
            {
                StarterContractDefinition contract = catalog.GetContract(resolvedContractId);
                return contract.CharacterClassName == characterClass.ToString()
                    ? contract
                    : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void ApplyStarterContractResources(
            StarterContractDefinition contract)
        {
            if (contract == null)
            {
                return;
            }

            playerMaxHealth = Mathf.Max(1, playerMaxHealth + contract.StartingHealthDelta);
            playerHealth = playerMaxHealth;
            gold = Mathf.Max(0, gold + contract.StartingGoldDelta);
            luck = Mathf.Clamp(luck + contract.StartingLuckDelta, 1, 6);
            debt = Mathf.Max(0, debt + contract.StartingDebtDelta);
        }

        private string GetDefaultStarterContractId(CharacterClass characterClass)
        {
            if (!TryGetStarterContractCatalog(out StarterContractCatalog catalog))
            {
                return string.Empty;
            }

            return catalog.GetContracts(characterClass).FirstOrDefault()?.Id
                ?? string.Empty;
        }

        private bool TryGetStarterContractCatalog(
            out StarterContractCatalog catalog)
        {
            if (!starterContractCatalogLoadAttempted)
            {
                starterContractCatalogLoadAttempted = true;
                TextAsset source = Resources.Load<TextAsset>(
                    StarterContractResourcePath);
                try
                {
                    cachedStarterContractCatalog =
                        StarterContractCatalog.Load(source);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Starter contract catalog fallback: {exception.Message}");
                }
            }

            catalog = cachedStarterContractCatalog;
            return catalog != null;
        }
    }
}
