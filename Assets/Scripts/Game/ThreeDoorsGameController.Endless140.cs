using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Game.V140;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string EndlessMutationCatalogResourcePath =
            "GameData/V140/endless_mutations";

        private EndlessMutationCatalog cachedEndlessMutationCatalog;
        private bool endlessMutationCatalogLoadAttempted;
        private IReadOnlyList<EndlessMutationDefinition> pendingEndlessMutationChoices =
            Array.Empty<EndlessMutationDefinition>();

        private void ShowEndlessMutationSelection()
        {
            if (!TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog))
            {
                ShowEndlessCheckpoint();
                return;
            }

            if (runRandom == null)
            {
                ResetRunRandom(runSeed == 0 ? 1 : runSeed);
            }

            pendingEndlessMutationChoices = catalog.GetChoices(
                activeEndlessMutationIds,
                runRandom,
                3);
            if (pendingEndlessMutationChoices.Count == 0)
            {
                AddLog(L("endlessMutation.log.allActive"));
                ShowEndlessCheckpoint();
                return;
            }

            PlayNonCombatMusic();
            phase = GamePhase.Reward;
            SetBackground(
                rewardBackground != null ? rewardBackground : bossBackground);
            ClearContent();
            SetLogVisible(false);
            SetAnchors(
                contentRoot,
                new Vector2(0.090f, 0.135f),
                new Vector2(0.910f, 0.855f));
            primaryButton.gameObject.SetActive(false);
            subtitleText.text = L("endlessMutation.selection.subtitle");
            BindLocalizedText(
                subtitleText,
                "endlessMutation.selection.subtitle");
            SetSubtitleBoxVisible(true);

            RectTransform panel = AddPanel(
                contentRoot,
                "심연 변칙 선택",
                new Color(1f, 1f, 1f, 0.88f),
                statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite);
            SetFramedModalPanelAnchors(panel);
            AddFramedModalTitle(
                contentRoot,
                "심연 변칙 선택 제목 박스",
                L("endlessMutation.selection.title"),
                0.285f,
                0.715f);

            Text summary = AddText(
                panel,
                "심연 변칙 선택 설명",
                LF(
                    "endlessMutation.selection.summary",
                    activeEndlessMutationIds.Count),
                20,
                TextAnchor.MiddleCenter,
                new Color(0.88f, 0.84f, 0.76f, 1f));
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 14;
            summary.resizeTextMaxSize = 20;
            SetAnchors(
                summary.rectTransform,
                new Vector2(0.115f, 0.730f),
                new Vector2(0.885f, 0.815f));

            Vector2[] minimums =
            {
                new(0.070f, 0.535f),
                new(0.070f, 0.335f),
                new(0.070f, 0.135f)
            };
            Vector2[] maximums =
            {
                new(0.930f, 0.705f),
                new(0.930f, 0.505f),
                new(0.930f, 0.305f)
            };
            for (int index = 0;
                index < pendingEndlessMutationChoices.Count;
                index += 1)
            {
                EndlessMutationDefinition mutation =
                    pendingEndlessMutationChoices[index];
                string detail = LF(
                    "endlessMutation.choice.detail",
                    L(mutation.RiskKey),
                    L(mutation.RewardKey));
                AddPostTenChoice(
                    panel,
                    L(mutation.NameKey),
                    detail,
                    minimums[index],
                    maximums[index],
                    () => SelectEndlessMutation(mutation.Id),
                    true);
            }

            RefreshTopBar();
            RefreshLog();
        }

        private void SelectEndlessMutation(string mutationId)
        {
            if (!ActivateEndlessMutation(mutationId))
            {
                ShowEndlessMutationSelection();
                return;
            }

            pendingEndlessMutationChoices =
                Array.Empty<EndlessMutationDefinition>();
            checkpointResumePhase = GamePhase.DoorSelection;
            pendingResolvedDoorTypeId = NoPendingDoorType;
            SaveRunCheckpointAtResolvedSurface();
            ShowEndlessCheckpoint();
        }

        private bool ActivateEndlessMutation(string mutationId)
        {
            if (!TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog)
                || !catalog.TryGet(
                    mutationId,
                    out EndlessMutationDefinition mutation)
                || !activeEndlessMutationIds.Add(mutation.Id))
            {
                return false;
            }

            AddLog(LF(
                "endlessMutation.log.activated",
                L(mutation.NameKey),
                L(mutation.RiskKey),
                L(mutation.RewardKey)));
            return true;
        }

        private void AddEndlessMutationStatusButton(RectTransform parent)
        {
            AddRunStatusTextButton(
                parent,
                "심연 변칙 상태 버튼",
                LF(
                    "endlessMutation.status.button",
                    activeEndlessMutationIds.Count),
                new Vector2(0.045f, 0.905f),
                new Vector2(0.205f, 0.985f),
                () => ShowRunStatusDetail(
                    L("endlessMutation.status.title"),
                    BuildActiveEndlessMutationStatusText()),
                17);
        }

        private string BuildActiveEndlessMutationStatusText()
        {
            if (activeEndlessMutationIds.Count == 0)
            {
                return L("endlessMutation.status.none");
            }

            if (!TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog))
            {
                return string.Join("\n", activeEndlessMutationIds);
            }

            List<string> lines = new();
            foreach (EndlessMutationDefinition mutation in catalog.Mutations)
            {
                if (!activeEndlessMutationIds.Contains(mutation.Id))
                {
                    continue;
                }

                lines.Add(LF(
                    "endlessMutation.status.entry",
                    L(mutation.NameKey),
                    L(mutation.RiskKey),
                    L(mutation.RewardKey)));
            }

            foreach (string unknownId in activeEndlessMutationIds
                .Where(id => !catalog.TryGet(id, out _))
                .OrderBy(id => id, StringComparer.Ordinal))
            {
                lines.Add(LF(
                    "endlessMutation.status.unknown",
                    unknownId));
            }

            return string.Join("\n\n", lines);
        }

        private string BuildActiveEndlessMutationNameSummary()
        {
            if (activeEndlessMutationIds.Count == 0)
            {
                return L("endlessMutation.status.noneShort");
            }

            if (!TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog))
            {
                return string.Join(", ", activeEndlessMutationIds);
            }

            List<string> names = catalog.Mutations
                .Where(mutation => activeEndlessMutationIds.Contains(mutation.Id))
                .Select(mutation => L(mutation.NameKey))
                .ToList();
            names.AddRange(activeEndlessMutationIds
                .Where(id => !catalog.TryGet(id, out _))
                .OrderBy(id => id, StringComparer.Ordinal));
            return string.Join(" · ", names);
        }

        private float GetEndlessEnemyAttackMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.EnemyAttackMultiplier,
                1f);
        }

        private float GetEndlessCombatGoldMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.CombatGoldMultiplier,
                1f);
        }

        private float GetEndlessEnemyBlockMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.EnemyBlockMultiplier,
                1f);
        }

        private float GetEndlessRareCardWeightMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.RareCardWeightMultiplier,
                1f);
        }

        private float GetEndlessRestHealingMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.RestHealingMultiplier,
                1f);
        }

        private float GetEndlessRemovalCostMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.RemovalCostMultiplier,
                1f);
        }

        private int GetEndlessDebtGainBonus()
        {
            return Mathf.RoundToInt(GetEndlessMutationValue(
                EndlessMutationEffectType.DebtGainBonus,
                0f));
        }

        private int GetEndlessDoorInsightBonus()
        {
            return Mathf.RoundToInt(GetEndlessMutationValue(
                EndlessMutationEffectType.DoorInsightBonus,
                0f));
        }

        private float GetEndlessShopPriceMultiplier()
        {
            return GetEndlessMutationValue(
                EndlessMutationEffectType.ShopPriceMultiplier,
                1f);
        }

        private int GetEndlessShopOfferBonus()
        {
            return Mathf.RoundToInt(GetEndlessMutationValue(
                EndlessMutationEffectType.ShopOfferBonus,
                0f));
        }

        private int GetEndlessOpeningHandAdjustment()
        {
            return Mathf.RoundToInt(GetEndlessMutationValue(
                EndlessMutationEffectType.OpeningHandPenalty,
                0f));
        }

        private int GetEndlessFirstTurnActionBonus()
        {
            return Mathf.RoundToInt(GetEndlessMutationValue(
                EndlessMutationEffectType.FirstTurnActionBonus,
                0f));
        }

        private int GetEndlessAdjustedDebtGain(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            return safeAmount == 0
                ? 0
                : Mathf.Max(0, safeAmount + GetEndlessDebtGainBonus());
        }

        private void DrawOpeningHandWithEndlessMutation()
        {
            int target = Mathf.Clamp(
                StartingHandSize + GetEndlessOpeningHandAdjustment(),
                1,
                StartingHandSize);
            DrawCards(Mathf.Max(0, target - hand.Count));
        }

        private void ApplyEndlessCombatStartBonuses()
        {
            int actionBonus = GetEndlessFirstTurnActionBonus();
            if (actionBonus <= 0)
            {
                return;
            }

            action += actionBonus;
            AddLog(LF(
                "endlessMutation.log.firstTurnAction",
                actionBonus));
        }

        private float GetEndlessMutationValue(
            EndlessMutationEffectType effectType,
            float fallback)
        {
            if (!endlessModeActive
                || activeEndlessMutationIds.Count == 0
                || !TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog))
            {
                return fallback;
            }

            foreach (string mutationId in activeEndlessMutationIds)
            {
                if (!catalog.TryGet(
                        mutationId,
                        out EndlessMutationDefinition mutation))
                {
                    continue;
                }

                EndlessMutationEffectDefinition effect = mutation.AllEffects
                    .FirstOrDefault(candidate => candidate.Type == effectType);
                if (effect != null)
                {
                    return effect.ClampedValue;
                }
            }

            return fallback;
        }

        private bool TryGetEndlessMutationCatalog(
            out EndlessMutationCatalog catalog)
        {
            if (!endlessMutationCatalogLoadAttempted)
            {
                endlessMutationCatalogLoadAttempted = true;
                TextAsset source = Resources.Load<TextAsset>(
                    EndlessMutationCatalogResourcePath);
                try
                {
                    cachedEndlessMutationCatalog =
                        EndlessMutationCatalog.Load(source);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Endless mutation catalog fallback: {exception.Message}");
                }
            }

            catalog = cachedEndlessMutationCatalog;
            return catalog != null;
        }
    }
}
