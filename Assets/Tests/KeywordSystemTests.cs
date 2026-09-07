using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [Category("Keyword")]
    public class KeywordSystemTests
    {
        private const long TwisterKeywordID = 10201004;
        private const long TwisterSkillID = 10101005;
        private const long TwisterGrantID = 90212006;
        private const long TwisterEffectID = 10202006;
        private const long SmokeStepSkillID = 10101002;
        private const long CounterSkillID = 90101001;
        private const long CounterDamageID = 90102001;
        private const long CounterRangeID = 90103001;
        private const long ParryReadyKeywordID = 10201001;
        private const long ParryReadyEffectID = 10202001;
        private const long ParryReadyGrantID = 90212001;
        private const long BanMoveKeywordID = 90201001;
        private const long BanMoveEffectID = 90202001;
        private const long BanMoveGrantID = 90212002;

        private string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private string CsvRoot => Path.Combine(
            Application.dataPath,
            "GamePlay", "Common", "CommonResources", "CSV", "MEMCSV");

        [Test]
        public void TwisterDataContract_MatchesVerticalSliceDecision()
        {
            KeywordData keyword = Parse<KeywordData>()[TwisterKeywordID] as KeywordData;
            KeywordGrantData grant = Parse<KeywordGrantData>()[TwisterGrantID] as KeywordGrantData;
            KeywordEffectData effect = Parse<KeywordEffectData>()[TwisterEffectID] as KeywordEffectData;

            Assert.That(keyword, Is.Not.Null);
            Assert.That(keyword.keywordType, Is.EqualTo(SystemEnum.eKeyword.Twister));
            Assert.That(keyword.Stackable, Is.False);
            Assert.That(keyword.MaxStack, Is.EqualTo(1));
            Assert.That(keyword.RemovePolicy, Is.EqualTo(SystemEnum.eRemovePolicy.ByDuration));

            Assert.That(grant, Is.Not.Null);
            Assert.That(grant.SourceType, Is.EqualTo(SystemEnum.eSourceType.Skill));
            Assert.That(grant.SouceRefID, Is.EqualTo(TwisterSkillID));
            Assert.That(grant.KeywordID, Is.EqualTo(TwisterKeywordID));
            Assert.That(grant.ApplyTarget, Is.EqualTo(SystemEnum.eApplyTarget.TARGET_ENEMY));
            Assert.That(grant.Duration, Is.EqualTo(2));
            Assert.That(grant.InitStack, Is.EqualTo(1));
            Assert.That(grant.Probability, Is.EqualTo(1000));
            Assert.That(grant.StackBehavior, Is.EqualTo(SystemEnum.eStackBehavior.Refresh));

            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.KeywordID, Is.EqualTo(TwisterKeywordID));
            Assert.That(effect.EffectType, Is.EqualTo(SystemEnum.eEffectType.Dot));
            Assert.That(effect.TriggerCondition, Is.EqualTo(SystemEnum.eTriggerCondition.EndOfTurn));
        }

        [Test]
        public void TwisterPresentationAssets_AreConnected()
        {
            KeywordData keyword = Parse<KeywordData>()[TwisterKeywordID] as KeywordData;
            Assert.That(keyword, Is.Not.Null);

            string iconAddress = $"skillicon_{keyword.keywordIcon}";
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup("Battle.UI.YeonSkill");
            AddressableAssetEntry icon = null;
            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry.address != iconAddress)
                    continue;

                icon = entry;
                break;
            }

            Assert.That(icon, Is.Not.Null, "Twister MVP icon must remain Addressable.");
            Assert.That(
                icon.AssetPath.Replace('\\', '/'),
                Is.EqualTo("Assets/GamePlay/Features/Battle/BattleResources/Sprites/Ally/Yeon/Skill/Icon/skillicon_Twister.png"));

            string timelinePath = Path.Combine(
                ProjectRoot,
                "Assets", "GamePlay", "Features", "Battle", "BattleResources", "Timeline",
                "Yeon", "Skill", "Yeon_Twister.playable");
            string timeline = File.ReadAllText(timelinePath);
            Assert.That(timeline, Does.Contain("guid: ca802c9a8e6b4080addb8301cfaf710a"));
            Assert.That(timeline, Does.Contain("m_Name: Apply Keyword Grants"));
        }

        [Test]
        public void SmokeStepDataContract_ConnectsParryAndCounterattack()
        {
            DokkaebiSkillData smokeStep = Parse<DokkaebiSkillData>()[SmokeStepSkillID] as DokkaebiSkillData;
            SkillData counter = Parse<SkillData>()[CounterSkillID] as SkillData;
            SkillDamageData counterDamage = Parse<SkillDamageData>()[CounterDamageID] as SkillDamageData;
            SkillRangeData counterRange = Parse<SkillRangeData>()[CounterRangeID] as SkillRangeData;
            KeywordData parryReady = Parse<KeywordData>()[ParryReadyKeywordID] as KeywordData;
            KeywordData banMove = Parse<KeywordData>()[BanMoveKeywordID] as KeywordData;
            KeywordGrantData parryGrant = Parse<KeywordGrantData>()[ParryReadyGrantID] as KeywordGrantData;
            KeywordGrantData banMoveGrant = Parse<KeywordGrantData>()[BanMoveGrantID] as KeywordGrantData;
            KeywordEffectData parryEffect = Parse<KeywordEffectData>()[ParryReadyEffectID] as KeywordEffectData;
            KeywordEffectData banMoveEffect = Parse<KeywordEffectData>()[BanMoveEffectID] as KeywordEffectData;

            Assert.That(smokeStep, Is.Not.Null);
            Assert.That(smokeStep.skillTimeLine, Is.EqualTo("timeline_SmokeStep"));

            Assert.That(parryReady, Is.Not.Null);
            Assert.That(parryReady.keywordType, Is.EqualTo(SystemEnum.eKeyword.ParryReady));
            Assert.That(parryReady.RemovePolicy, Is.EqualTo(SystemEnum.eRemovePolicy.ByCondition));
            Assert.That(parryGrant.SouceRefID, Is.EqualTo(SmokeStepSkillID));
            Assert.That(parryGrant.KeywordID, Is.EqualTo(ParryReadyKeywordID));
            Assert.That(parryGrant.ApplyTarget, Is.EqualTo(SystemEnum.eApplyTarget.TARGET_SELF));

            Assert.That(parryEffect.KeywordID, Is.EqualTo(ParryReadyKeywordID));
            Assert.That(parryEffect.EffectType, Is.EqualTo(SystemEnum.eEffectType.TriggerSkill));
            Assert.That(parryEffect.TriggerCondition, Is.EqualTo(SystemEnum.eTriggerCondition.OnHit));
            Assert.That(parryEffect.ActionID, Is.EqualTo(CounterSkillID));

            Assert.That(banMove, Is.Not.Null);
            Assert.That(banMove.keywordType, Is.EqualTo(SystemEnum.eKeyword.BanMove));
            Assert.That(banMove.RemovePolicy, Is.EqualTo(SystemEnum.eRemovePolicy.ByDuration));
            Assert.That(banMoveGrant.SouceRefID, Is.EqualTo(SmokeStepSkillID));
            Assert.That(banMoveGrant.KeywordID, Is.EqualTo(BanMoveKeywordID));
            Assert.That(banMoveEffect.KeywordID, Is.EqualTo(BanMoveKeywordID));
            Assert.That(banMoveEffect.EffectType, Is.EqualTo(SystemEnum.eEffectType.ControlMove));

            Assert.That(counter, Is.Not.Null);
            Assert.That(counter.skillDamage, Is.EqualTo(CounterDamageID));
            Assert.That(counter.skillRangeID, Is.EqualTo(CounterRangeID));
            Assert.That(counter.skillTimeLine, Is.EqualTo("timeline_Counterattack"));
            Assert.That(counterDamage, Is.Not.Null);
            Assert.That(counterDamage.SkillType, Is.EqualTo(SystemEnum.eSkillType.PhysicalAttack));
            Assert.That(counterRange, Is.Not.Null);
            Assert.That(counterRange.skillPivot, Is.EqualTo(SystemEnum.ePivot.TARGET_ENEMY));
        }

        [Test]
        public void TwisterRuntime_RefreshesThenExpiresAfterTwoTurnEnds()
        {
            Type keywordInfoType = FindType("GamePlay.Common.Scripts.Keyword.KeywordInfo");
            object keywordInfo = Activator.CreateInstance(keywordInfoType, new object[] { null });

            KeywordData keyword = new()
            {
                index = TwisterKeywordID,
                KeywordName = "Twister",
                Stackable = false,
                MaxStack = 1,
                RemovePolicy = SystemEnum.eRemovePolicy.ByDuration,
                RefreshPolicy = SystemEnum.eRefreshPolicy.Extend,
                keywordType = SystemEnum.eKeyword.Twister,
                keywordIcon = "Twister"
            };
            KeywordGrantData grant = new()
            {
                index = TwisterGrantID,
                KeywordID = TwisterKeywordID,
                Duration = 2,
                InitStack = 1,
                Probability = 1000,
                StackBehavior = SystemEnum.eStackBehavior.Refresh
            };
            List<KeywordEffectData> effects = new()
            {
                new KeywordEffectData
                {
                    index = TwisterEffectID,
                    KeywordID = TwisterKeywordID,
                    EffectType = SystemEnum.eEffectType.Dot,
                    TriggerCondition = SystemEnum.eTriggerCondition.EndOfTurn
                }
            };

            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Add\].*keyword=Twister\(10201004\).*remainingTurns=2"));
            Assert.That(Invoke<bool>(keywordInfo, "Apply", keyword, grant, effects), Is.True);
            Assert.That(Invoke<int>(keywordInfo, "GetKeywordCount", SystemEnum.eKeyword.Twister), Is.EqualTo(1));
            Assert.That(Invoke<int>(keywordInfo, "GetRemainingTurns", SystemEnum.eKeyword.Twister), Is.EqualTo(2));

            grant.InitStack = 99;
            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Refresh\].*stacks=1->1.*remainingTurns=2->2"));
            Assert.That(Invoke<bool>(keywordInfo, "Apply", keyword, grant, effects), Is.True);
            Assert.That(Invoke<int>(keywordInfo, "GetKeywordCount", SystemEnum.eKeyword.Twister), Is.EqualTo(1));
            Assert.That(Invoke<int>(keywordInfo, "GetRemainingTurns", SystemEnum.eKeyword.Twister), Is.EqualTo(2));

            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Effect\].*effect=Dot\(10202006\).*remainingTurns=2"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Duration\].*remainingTurns=2->1"));
            InvokeAsync(keywordInfo, "TriggerAsync", SystemEnum.eTriggerCondition.EndOfTurn, CancellationToken.None);
            Assert.That(Invoke<int>(keywordInfo, "GetRemainingTurns", SystemEnum.eKeyword.Twister), Is.EqualTo(1));

            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Effect\].*effect=Dot\(10202006\).*remainingTurns=1"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Duration\].*remainingTurns=1->0"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[Keyword\]\[Remove\].*reason=DurationExpired.*remainingTurns=0"));
            InvokeAsync(keywordInfo, "TriggerAsync", SystemEnum.eTriggerCondition.EndOfTurn, CancellationToken.None);
            Assert.That(Invoke<bool>(keywordInfo, "HasKeyword", SystemEnum.eKeyword.Twister), Is.False);
        }

        [Test]
        public void KeywordFactory_CreatesDataDrivenKeywordForRegularKeyword()
        {
            KeywordData data = new()
            {
                index = 10201002,
                KeywordName = "Smoke",
                Stackable = true,
                MaxStack = 9,
                RemovePolicy = SystemEnum.eRemovePolicy.ByDuration,
                RefreshPolicy = SystemEnum.eRefreshPolicy.Stack,
                keywordType = SystemEnum.eKeyword.Smoke,
                keywordIcon = "Smoke"
            };
            KeywordGrantData grant = new()
            {
                KeywordID = data.index,
                Duration = 2,
                InitStack = 1,
                StackBehavior = SystemEnum.eStackBehavior.Cumulative
            };

            object keyword = CreateKeyword(
                data,
                grant,
                Array.Empty<KeywordEffectData>());

            Assert.That(
                keyword.GetType().FullName,
                Is.EqualTo("GamePlay.Features.Scripts.Keyword.DataDrivenKeyword"));
            Assert.That(
                GetProperty<SystemEnum.eKeyword>(keyword, "KeywordType"),
                Is.EqualTo(SystemEnum.eKeyword.Smoke));
            Assert.That(GetProperty<int>(keyword, "Stacks"), Is.EqualTo(1));
            Assert.That(GetProperty<int>(keyword, "RemainingTurns"), Is.EqualTo(2));

            grant.InitStack = 3;
            grant.Duration = 1;
            Invoke<object>(keyword, "Refresh", grant);

            Assert.That(GetProperty<int>(keyword, "Stacks"), Is.EqualTo(4));
            Assert.That(GetProperty<int>(keyword, "RemainingTurns"), Is.EqualTo(2));
        }

        [Test]
        public void KeywordFactory_CreatesSpecialParryReadyKeyword()
        {
            KeywordData data = new()
            {
                index = ParryReadyKeywordID,
                KeywordName = "ParryReady",
                Stackable = false,
                MaxStack = 1,
                RemovePolicy = SystemEnum.eRemovePolicy.ByCondition,
                RefreshPolicy = SystemEnum.eRefreshPolicy.Refresh,
                keywordType = SystemEnum.eKeyword.ParryReady,
                keywordIcon = "ParryReady"
            };
            KeywordGrantData grant = new()
            {
                KeywordID = data.index,
                Duration = 1,
                InitStack = 1,
                StackBehavior = SystemEnum.eStackBehavior.Refresh
            };
            List<KeywordEffectData> effects = new()
            {
                new KeywordEffectData
                {
                    index = ParryReadyEffectID,
                    KeywordID = data.index,
                    EffectType = SystemEnum.eEffectType.TriggerSkill,
                    TriggerCondition = SystemEnum.eTriggerCondition.OnHit,
                    ActionID = CounterSkillID
                }
            };

            object keyword = CreateKeyword(data, grant, effects);

            Assert.That(
                keyword.GetType().FullName,
                Is.EqualTo("GamePlay.Features.Scripts.Keyword.ParryReadyKeyword"));
            Assert.That(
                GetProperty<SystemEnum.eKeyword>(keyword, "KeywordType"),
                Is.EqualTo(SystemEnum.eKeyword.ParryReady));
        }

        [Test]
        public void KeywordFactory_RejectsSentinelKeywordType()
        {
            KeywordData data = new()
            {
                index = 0,
                keywordType = SystemEnum.eKeyword.None
            };
            KeywordGrantData grant = new()
            {
                KeywordID = data.index,
                Duration = 1,
                InitStack = 1,
                StackBehavior = SystemEnum.eStackBehavior.Refresh
            };

            LogAssert.Expect(
                LogType.Warning,
                new Regex(@"\[KeywordFactory\] 생성할 수 없는 키워드 타입입니다: None\(0\)"));
            object keyword = CreateKeyword(
                data,
                grant,
                Array.Empty<KeywordEffectData>());

            Assert.That(keyword, Is.Null);
        }

        private Dictionary<long, SheetData> Parse<T>() where T : SheetData, new()
        {
            string csv = File.ReadAllText(Path.Combine(CsvRoot, typeof(T).Name + ".csv"));
            return new T().ParseAsync(csv, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            throw new InvalidOperationException($"Type not found: {fullName}");
        }

        private static object CreateKeyword(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
        {
            Type factoryType = FindType("GamePlay.Features.Scripts.Keyword.KeywordFactory");
            MethodInfo method = factoryType.GetMethod(
                "CreateKeyword",
                BindingFlags.Public | BindingFlags.Static);
            return method.Invoke(null, new object[] { data, grant, effects });
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            return (T)property.GetValue(instance);
        }

        private static T Invoke<T>(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(instance, methodName, arguments.Length);
            return (T)method.Invoke(instance, arguments);
        }

        private static void InvokeAsync(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(instance, methodName, arguments.Length);
            UniTask task = (UniTask)method.Invoke(instance, arguments);
            task.GetAwaiter().GetResult();
        }

        private static MethodInfo FindMethod(object instance, string methodName, int parameterCount)
        {
            MethodInfo[] methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (MethodInfo method in methods)
            {
                if (method.Name == methodName && method.GetParameters().Length == parameterCount)
                    return method;
            }

            throw new MissingMethodException(instance.GetType().FullName, methodName);
        }
    }
}
