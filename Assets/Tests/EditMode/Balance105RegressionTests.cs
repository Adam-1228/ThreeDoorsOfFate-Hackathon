using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Balance105RegressionTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string CardCategoryTypeName =
            "ThreeDoorsOfFate.Cards.CardCategory, Assembly-CSharp";
        private const string CardSourceTypeName =
            "ThreeDoorsOfFate.Cards.CardSource, Assembly-CSharp";
        private const string CardRarityTypeName =
            "ThreeDoorsOfFate.Cards.CardRarity, Assembly-CSharp";
        private const string CharacterClassTypeName =
            "ThreeDoorsOfFate.Cards.CharacterClass, Assembly-CSharp";

        private Type controllerType;
        private Type cardType;
        private Type cardCategoryType;
        private Type cardSourceType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;
        private readonly List<ScriptableObject> createdCards = new();

        [SetUp]
        public void SetUp()
        {
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            cardCategoryType = Type.GetType(CardCategoryTypeName);
            cardSourceType = Type.GetType(CardSourceTypeName);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(cardType, Is.Not.Null);
            Assert.That(cardCategoryType, Is.Not.Null);
            Assert.That(cardSourceType, Is.Not.Null);

            controllerHost = new GameObject("Balance 1.0.5 Regression Test Host");
            controller = controllerHost.AddComponent(controllerType);
            canvasRoot = TryGetField<RectTransform>("canvasRoot");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject card in createdCards)
            {
                UnityEngine.Object.DestroyImmediate(card);
            }

            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            if (controllerHost != null)
            {
                UnityEngine.Object.DestroyImmediate(controllerHost);
            }

            if (originalEventSystem == null)
            {
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }
        }

        [Test]
        public void NormalBoss_FinalStatsAre132Health15Attack9Block()
        {
            SetEnumField("currentDifficulty", "Normal");

            object boss = Invoke("CreateBossEnemy");

            Assert.That(ReadProperty<int>(boss, "MaxHealth"), Is.EqualTo(132));
            Assert.That(ReadProperty<int>(boss, "BaseAttack"), Is.EqualTo(15));
            Assert.That(ReadProperty<int>(boss, "BaseBlock"), Is.EqualTo(9));
        }

        [Test]
        public void NormalLateStandardEnemy_UsesRetunedStats()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetField("endlessModeActive", false);

            object enemy = CreateScaledEnemy("normal_late_enemy", false);

            Assert.That(ReadProperty<int>(enemy, "MaxHealth"), Is.EqualTo(58));
            Assert.That(ReadProperty<int>(enemy, "BaseAttack"), Is.EqualTo(10));
            Assert.That(ReadProperty<int>(enemy, "BaseBlock"), Is.EqualTo(7));
        }

        [TestCase("Easy", 50, 8, 6)]
        [TestCase("Hard", 84, 15, 9)]
        public void NonNormalStandardEnemyScaling_RemainsUnchanged(
            string difficulty,
            int expectedHealth,
            int expectedAttack,
            int expectedBlock)
        {
            SetEnumField("currentDifficulty", difficulty);
            SetField("endlessModeActive", false);

            object enemy = CreateScaledEnemy("comparison_enemy", false);

            Assert.That(ReadProperty<int>(enemy, "MaxHealth"), Is.EqualTo(expectedHealth));
            Assert.That(ReadProperty<int>(enemy, "BaseAttack"), Is.EqualTo(expectedAttack));
            Assert.That(ReadProperty<int>(enemy, "BaseBlock"), Is.EqualTo(expectedBlock));
        }

        [Test]
        public void NormalStandardEnemy_RegenerationIsCappedAtFour()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetField("endlessModeActive", false);
            object enemy = CreateScaledEnemy("normal_regenerator", false, 120);

            Assert.That(Invoke("GetEnemyRegenerationAmount", enemy), Is.EqualTo(4));
        }

        [Test]
        public void NormalBoss_RegenerationKeepsBossValue()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetField("endlessModeActive", false);
            object boss = Invoke("CreateBossEnemy");

            Assert.That(Invoke("GetEnemyRegenerationAmount", boss), Is.EqualTo(11));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void AttackGuarantee_InsertsOneEligibleAttackWithoutDuplicates(
            int offerCount)
        {
            ScriptableObject defense = CreateCard("defense", "Defense");
            ScriptableObject skillA = CreateCard("skill_a", "Skill");
            ScriptableObject skillB = CreateCard("skill_b", "Skill");
            ScriptableObject skillC = CreateCard("skill_c", "Skill");
            ScriptableObject attack = CreateCard("attack", "Attack");
            ScriptableObject[] nonAttacks =
            {
                defense,
                skillA,
                skillB,
                skillC
            };
            IList offers = CreateOfferFixture(
                nonAttacks.Append(attack),
                nonAttacks.Take(offerCount).ToArray());

            Invoke("EnsureAttackOffer", offers, CreateSources("CombatReward"));

            Assert.That(offers, Has.Count.EqualTo(offerCount));
            Assert.That(
                offers.Cast<object>().Count(card =>
                    ReadProperty<object>(card, "Category").ToString() == "Attack"),
                Is.EqualTo(1));
            Assert.That(
                offers.Cast<object>()
                    .Select(card => ReadProperty<string>(card, "CardId"))
                    .Distinct()
                    .Count(),
                Is.EqualTo(offerCount));
        }

        [Test]
        public void AttackGuarantee_LeavesSingleCardOfferUnchanged()
        {
            ScriptableObject defense = CreateCard("defense", "Defense");
            ScriptableObject attack = CreateCard("attack", "Attack");
            IList offers = CreateOfferFixture(
                new[] { defense, attack },
                defense);

            Invoke("EnsureAttackOffer", offers, CreateSources("CombatReward"));

            Assert.That(offers, Has.Count.EqualTo(1));
            Assert.That(ContainsAttack(offers), Is.False);
        }

        [Test]
        public void AttackGuarantee_LeavesFiveCardOfferUnchanged()
        {
            ScriptableObject defenseA = CreateCard("defense_a", "Defense");
            ScriptableObject defenseB = CreateCard("defense_b", "Defense");
            ScriptableObject skillA = CreateCard("skill_a", "Skill");
            ScriptableObject skillB = CreateCard("skill_b", "Skill");
            ScriptableObject skillC = CreateCard("skill_c", "Skill");
            ScriptableObject attack = CreateCard("attack", "Attack");
            IList offers = CreateOfferFixture(
                new[] { defenseA, defenseB, skillA, skillB, skillC, attack },
                defenseA,
                defenseB,
                skillA,
                skillB,
                skillC);

            Invoke("EnsureAttackOffer", offers, CreateSources("CombatReward"));

            Assert.That(offers, Has.Count.EqualTo(5));
            Assert.That(ContainsAttack(offers), Is.False);
        }

        [Test]
        public void AttackGuarantee_DoesNotInjectRareAttack()
        {
            ScriptableObject defense = CreateCard("defense", "Defense");
            ScriptableObject skillA = CreateCard("skill_a", "Skill");
            ScriptableObject skillB = CreateCard("skill_b", "Skill");
            ScriptableObject rareAttack = CreateCard(
                "rare_attack",
                "Attack",
                "Rare");
            IList offers = CreateOfferFixture(
                new[] { defense, skillA, skillB, rareAttack },
                defense,
                skillA,
                skillB);

            Invoke("EnsureAttackOffer", offers, CreateSources("CombatReward"));

            Assert.That(ContainsAttack(offers), Is.False);
        }

        [Test]
        public void AttackGuarantee_DoesNotInjectHardOnlyAttack()
        {
            SetEnumField("currentDifficulty", "Hard");
            ScriptableObject defense = CreateCard("defense", "Defense");
            ScriptableObject skillA = CreateCard("skill_a", "Skill");
            ScriptableObject skillB = CreateCard("skill_b", "Skill");
            ScriptableObject hardAttack = CreateCard(
                "hard_attack",
                "Attack",
                "Common",
                "HardReward");
            IList offers = CreateOfferFixture(
                new[] { defense, skillA, skillB, hardAttack },
                defense,
                skillA,
                skillB);

            Invoke(
                "EnsureAttackOffer",
                offers,
                CreateSources("CombatReward", "HardReward"));

            Assert.That(ContainsAttack(offers), Is.False);
        }

        [TestCase(2, 20)]
        [TestCase(3, 15)]
        public void BossAttackDamage_UsesTheSameValueForPreviewAndResolution(
            int currentLuck,
            int expectedDamage)
        {
            SetEnumField("currentDifficulty", "Normal");
            SetEnumField("phase", "Combat");
            SetField("luck", currentLuck);
            SetField("playerMaxHealth", 100);
            SetField("playerHealth", 100);
            SetField("playerBlock", 0);
            object boss = CreateScaledEnemy("intent_boss", true);
            SetProperty(boss, "IntentAttack", 15);
            SetField("enemy", boss);

            Assert.That(
                Invoke("GetEnemyIntentAttackDamage", boss),
                Is.EqualTo(expectedDamage));

            Invoke("ResolveEnemyIntent");

            Assert.That(GetField<int>("playerHealth"), Is.EqualTo(100 - expectedDamage));
        }

        [Test]
        public void NonBossAttackDamage_DoesNotGainLowLuckBonus()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetField("luck", 1);
            object standardEnemy = CreateScaledEnemy("intent_enemy", false);
            SetProperty(standardEnemy, "IntentAttack", 15);

            Assert.That(
                Invoke("GetEnemyIntentAttackDamage", standardEnemy),
                Is.EqualTo(15));
        }

        [Test]
        public void BossIntentLabel_RefreshesWhenLuckChangesDuringTheTurn()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetEnumField("phase", "Combat");
            SetField("luck", 2);
            object boss = CreateScaledEnemy("dynamic_intent_boss", true);
            SetProperty(boss, "IntentAttack", 15);
            SetProperty(boss, "IntentBlock", 9);
            SetField("enemy", boss);

            Invoke("RefreshEnemyIntentLabelForCurrentLuck");

            Assert.That(ReadProperty<string>(boss, "IntentLabel"),
                Is.EqualTo("공격 20 / 방어 9"));

            SetField("luck", 3);
            Invoke("RefreshEnemyIntentLabelForCurrentLuck");

            Assert.That(ReadProperty<string>(boss, "IntentLabel"),
                Is.EqualTo("공격 15 / 방어 9"));
        }

        [Test]
        public void NormalBossFullHandWithoutAttack_SwapsOneAttackOnlyOnce()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetEnumField("phase", "Combat");
            SetField("enemy", CreateScaledEnemy("smoothing_boss", true));
            IList hand = GetField<IList>("hand");
            IList drawPile = GetField<IList>("drawPile");
            hand.Clear();
            drawPile.Clear();

            for (int index = 0; index < 5; index += 1)
            {
                hand.Add(CreateCard($"defense_{index}", "Defense"));
            }

            ScriptableObject attack = CreateCard("smoothing_attack", "Attack");
            drawPile.Add(CreateCard("draw_skill", "Skill"));
            drawPile.Add(attack);
            string[] allIdsBefore = hand.Cast<object>()
                .Concat(drawPile.Cast<object>())
                .Select(card => ReadProperty<string>(card, "CardId"))
                .OrderBy(id => id)
                .ToArray();

            Assert.That(Invoke("TrySmoothBossNoAttackHand"), Is.EqualTo(true));
            Assert.That(ContainsAttack(hand), Is.True);
            Assert.That(hand, Has.Count.EqualTo(5));
            Assert.That(drawPile, Has.Count.EqualTo(2));
            Assert.That(
                hand.Cast<object>()
                    .Concat(drawPile.Cast<object>())
                    .Select(card => ReadProperty<string>(card, "CardId"))
                    .OrderBy(id => id),
                Is.EqualTo(allIdsBefore));

            Assert.That(Invoke("TrySmoothBossNoAttackHand"), Is.EqualTo(false));
        }

        [TestCase("Hard", true)]
        [TestCase("Normal", false)]
        public void HandSmoothing_DoesNotApplyOutsideEasyOrNormalBossCombat(
            string difficulty,
            bool isBoss)
        {
            SetEnumField("currentDifficulty", difficulty);
            SetEnumField("phase", "Combat");
            SetField("enemy", CreateScaledEnemy("excluded_smoothing", isBoss));
            IList hand = GetField<IList>("hand");
            IList drawPile = GetField<IList>("drawPile");
            hand.Clear();
            drawPile.Clear();
            for (int index = 0; index < 5; index += 1)
            {
                hand.Add(CreateCard($"excluded_defense_{index}", "Defense"));
            }
            drawPile.Add(CreateCard("excluded_attack", "Attack"));

            Assert.That(Invoke("TrySmoothBossNoAttackHand"), Is.EqualTo(false));
            Assert.That(ContainsAttack(hand), Is.False);
        }

        private object CreateScaledEnemy(
            string id,
            bool boss,
            int health = 70)
        {
            return Invoke(
                "CreateScaledEnemyState",
                id,
                id,
                health,
                12,
                8,
                false,
                boss,
                14);
        }

        private bool ContainsAttack(IList offers)
        {
            return offers.Cast<object>().Any(card =>
                ReadProperty<object>(card, "Category").ToString() == "Attack");
        }

        private IList CreateOfferFixture(
            IEnumerable<ScriptableObject> poolCards,
            params ScriptableObject[] initialOffers)
        {
            IList pool = GetField<IList>("cardPool");
            pool.Clear();
            foreach (ScriptableObject card in poolCards)
            {
                pool.Add(card);
            }

            IList offers = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(cardType));
            foreach (ScriptableObject card in initialOffers)
            {
                offers.Add(card);
            }

            return offers;
        }

        private Array CreateSources(params string[] values)
        {
            Array sources = Array.CreateInstance(cardSourceType, values.Length);
            for (int index = 0; index < values.Length; index += 1)
            {
                sources.SetValue(Enum.Parse(cardSourceType, values[index]), index);
            }

            return sources;
        }

        private ScriptableObject CreateCard(
            string id,
            string category,
            string rarity = "Common",
            string source = "CombatReward")
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            createdCards.Add(card);
            SetObjectField(card, "cardId", id);
            SetObjectField(card, "displayName", id);
            SetObjectField(card, "category", Enum.Parse(cardCategoryType, category));
            SetObjectField(card, "rarity", ParseEnum(CardRarityTypeName, rarity));
            SetObjectField(card, "source", ParseEnum(CardSourceTypeName, source));
            SetObjectField(
                card,
                "characterClass",
                ParseEnum(CharacterClassTypeName, "Any"));
            return card;
        }

        private static object ParseEnum(string typeName, string value)
        {
            Type enumType = Type.GetType(typeName);
            Assert.That(enumType, Is.Not.Null);
            return Enum.Parse(enumType, value);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(target);
        }

        private static void SetObjectField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            property.SetValue(target, value);
        }

        private object Invoke(string methodName, params object[] arguments)
        {
            return FindMethod(methodName, arguments.Length).Invoke(controller, arguments);
        }

        private MethodInfo FindMethod(string methodName, int parameterCount)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
                .SingleOrDefault(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == parameterCount);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            return method;
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private T TryGetField<T>(string fieldName)
            where T : class
        {
            return controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as T;
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private void SetEnumField(string fieldName, string enumValue)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(controller, Enum.Parse(field.FieldType, enumValue));
        }
    }
}
