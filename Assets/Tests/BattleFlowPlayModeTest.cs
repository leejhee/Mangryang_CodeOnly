using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Scripts.Foundation.Define;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BattleFlowPlayModeTest
{
    private const float BootTimeoutSeconds = 30f;
    private const float BattleTimeoutSeconds = 45f;

    [UnityTest]
    [RequiresPlayMode]
    [Timeout(90000)]
    public IEnumerator DebugBattle_ReachesFirstTurnThroughProductionLoadingFlow()
    {
        yield return EnsureLobbyReady();

        InvokeStatic("GamePlay.Common.Scripts.Scene.GamePlaySceneUtil", "LoadBattleScene");

        object controller = null;
        yield return WaitUntilOrFail(
            () =>
            {
                controller = GetStaticProperty("GamePlay.Features.Battle.Scripts.BattleController", "Instance");
                return SceneManager.GetActiveScene().name == "BattleScene" &&
                       controller != null &&
                       GetInstanceProperty(controller, "StageField") != null &&
                       GetInstanceProperty(controller, "TurnController") != null &&
                       GetInstanceProperty(GetInstanceProperty(controller, "TurnController"), "CurrentTurn") != null;
            },
            BattleTimeoutSeconds,
            "BattleScene did not initialize a stage and reach its first turn.");

        Assert.That(GetInstanceProperty(controller, "StageField"), Is.Not.Null);
        Assert.That(GetInstanceProperty(controller, "FocusChar"), Is.Not.Null);
        Assert.That((bool)GetInstanceProperty(controller, "IsBattleEnding"), Is.False);

        InvokeInstance(controller, "EndBattle", SystemEnum.eCharType.None);
        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == "LobbyScene" && !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading"),
            BootTimeoutSeconds,
            "Debug battle did not return to LobbyScene during cleanup.");
    }

    [UnityTest]
    [RequiresPlayMode]
    [Timeout(120000)]
    public IEnumerator DebugBattle_EnemyAiTurnCompletesAndReturnsControl()
    {
        yield return EnsureLobbyReady();

        InvokeStatic("GamePlay.Common.Scripts.Scene.GamePlaySceneUtil", "LoadBattleScene");

        object controller = null;
        yield return WaitForReadyBattle(null, value => controller = value);

        object turnController = GetInstanceProperty(controller, "TurnController");
        object currentTurn = GetInstanceProperty(turnController, "CurrentTurn");
        object enemyTurn = GetInstanceProperty(currentTurn, "WhoseSide")?.ToString() == "Enemy"
            ? currentTurn
            : ((IEnumerable)GetInstanceProperty(turnController, "TurnCollection"))
                .Cast<object>()
                .FirstOrDefault(turn => GetInstanceProperty(turn, "WhoseSide")?.ToString() == "Enemy");

        Assert.That(enemyTurn, Is.Not.Null, "Debug battle did not contain an enemy turn.");

        if (!ReferenceEquals(currentTurn, enemyTurn))
            InvokeInstance(turnController, "ChangeTurn", enemyTurn);

        yield return null;
        Assert.That(
            ReferenceEquals(GetInstanceProperty(turnController, "CurrentTurn"), enemyTurn),
            Is.True,
            "Enemy turn did not begin.");

        yield return WaitUntilOrFail(
            () =>
            {
                if ((bool)GetInstanceProperty(controller, "IsBattleEnding"))
                    return true;

                object turn = GetInstanceProperty(turnController, "CurrentTurn");
                bool transitionCompleted = !(bool)GetPrivateField(turnController, "_isChangingTurn");
                return turn != null &&
                       GetInstanceProperty(turn, "WhoseSide")?.ToString() == "Player" &&
                       transitionCompleted;
            },
            30f,
            "Enemy AI turns did not complete and return control to the player.");

        Assert.That((bool)GetInstanceProperty(controller, "IsBattleEnding"), Is.False,
            "A single enemy turn unexpectedly ended the debug battle.");

        InvokeInstance(controller, "EndBattle", SystemEnum.eCharType.None);
        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == "LobbyScene" &&
                  !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading"),
            BootTimeoutSeconds,
            "Enemy AI smoke test did not return to LobbyScene during cleanup.");
    }

    [UnityTest]
    [RequiresPlayMode]
    public IEnumerator GameManager_ShutdownGuard_DoesNotRecreateManager()
    {
        yield return EnsureLobbyReady();

        Type managerType = FindType("Core.Scripts.Managers.GameManager");
        FieldInfo instanceField = managerType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        FieldInfo shutdownField = managerType.GetField("isShuttingDown", BindingFlags.NonPublic | BindingFlags.Static);
        object originalInstance = instanceField?.GetValue(null);
        object originalShutdown = shutdownField?.GetValue(null);

        Assert.That(originalInstance, Is.Not.Null);
        Assert.That(shutdownField, Is.Not.Null);

        try
        {
            instanceField.SetValue(null, null);
            shutdownField.SetValue(null, true);

            object resolved = GetStaticProperty("Core.Scripts.Managers.GameManager", "Instance");
            Assert.That(resolved, Is.Null,
                "GameManager.Instance must not create an object while Play Mode is shutting down.");
        }
        finally
        {
            instanceField.SetValue(null, originalInstance);
            shutdownField.SetValue(null, originalShutdown);
        }
    }

    [UnityTest]
    [RequiresPlayMode]
    [Timeout(150000)]
    public IEnumerator MultiStageWinAndDefeatRetry_CompleteTheirSceneLoops()
    {
        yield return EnsureLobbyReady();

        object session = GetStaticProperty("GamePlay.Features.Battle.Scripts.BattleSession", "Instance");
        object party = CreateDebugParty();
        ConfigureBattleSession(session, party, new List<string> { "", "" });

        InvokeStatic("GamePlay.Common.Scripts.Scene.GamePlaySceneUtil", "LoadBattleScene");
        object firstController = null;
        yield return WaitForReadyBattle(null, value => firstController = value);
        Assert.That((int)GetInstanceProperty(session, "CurrentStageIndex"), Is.EqualTo(0));

        InvokeInstance(firstController, "EndBattle", SystemEnum.eCharType.Player);
        yield return WaitUntilOrFail(
            () => GetPrivateField(firstController, "_postWinFlow")?.ToString() == "NextBattle" &&
                  (bool)GetInstanceProperty(firstController, "IsResultViewReady"),
            10f,
            "First victory result view did not become ready for the next-battle flow.");
        InvokeInstance(firstController, "OnWinRewardClosed");

        object secondController = null;
        yield return WaitForReadyBattle(firstController, value => secondController = value);
        Assert.That((int)GetInstanceProperty(session, "CurrentStageIndex"), Is.EqualTo(1));

        InvokeInstance(secondController, "EndBattle", SystemEnum.eCharType.Player);
        yield return WaitUntilOrFail(
            () => GetPrivateField(secondController, "_postWinFlow")?.ToString() == "ReturnToScene" &&
                  (bool)GetInstanceProperty(secondController, "IsResultViewReady"),
            10f,
            "Final victory result view did not become ready for the return flow.");
        InvokeInstance(secondController, "OnWinRewardClosed");

        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == "LobbyScene" && !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading"),
            BootTimeoutSeconds,
            "Final victory did not return to LobbyScene.");
        Assert.That((bool)GetInstanceProperty(session, "HasAnyStage"), Is.False);

        party = CreateDebugParty();
        ConfigureBattleSession(session, party, new List<string> { "" });
        InvokeStatic("GamePlay.Common.Scripts.Scene.GamePlaySceneUtil", "LoadBattleScene");

        object lossController = null;
        yield return WaitForReadyBattle(null, value => lossController = value);

        object stat = GetFirstPartyMemberStat(party);
        long maxHp = (long)InvokeInstance(stat, "GetStat", SystemEnum.eStats.NMHP);
        InvokeInstance(stat, "ChangeStat", SystemEnum.eStats.NHP, -1L);

        InvokeInstance(lossController, "EndBattle", SystemEnum.eCharType.Enemy);
        yield return WaitUntilOrFail(
            () => (bool)GetInstanceProperty(lossController, "IsResultViewReady"),
            10f,
            "Defeat result view did not become ready for retry.");
        InvokeInstance(lossController, "RestartCurrentBattle");

        object retryController = null;
        yield return WaitForReadyBattle(lossController, value => retryController = value);
        Assert.That((int)GetInstanceProperty(session, "CurrentStageIndex"), Is.EqualTo(0));
        Assert.That((long)InvokeInstance(stat, "GetStat", SystemEnum.eStats.NHP), Is.EqualTo(maxHp));

        InvokeInstance(retryController, "EndBattle", SystemEnum.eCharType.None);
        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == "LobbyScene" && !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading"),
            BootTimeoutSeconds,
            "Retry battle cleanup did not return to LobbyScene.");
    }

    private static IEnumerator EnsureLobbyReady()
    {
        bool hasRuntime = GetStaticProperty("Core.Scripts.Managers.GameManager", "Instance") != null;
        bool lobbyReady = hasRuntime &&
                          SceneManager.GetActiveScene().name == "LobbyScene" &&
                          !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading");
        if (lobbyReady)
            yield break;

        SceneManager.LoadScene("BootScene", LoadSceneMode.Single);
        // LoadScene 요청 직후에는 이전 씬이 한 프레임 동안 active로 남을 수 있다.
        yield return null;

        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == "LobbyScene" &&
                  !GetStaticBool("Core.Scripts.Foundation.SceneUtil.SceneLoader", "IsLoading"),
            BootTimeoutSeconds,
            "BootScene did not finish loading LobbyScene.");
    }

    private static IEnumerator WaitForReadyBattle(object previousController, Action<object> capture)
    {
        object controller = null;
        yield return WaitUntilOrFail(
            () =>
            {
                controller = GetStaticProperty("GamePlay.Features.Battle.Scripts.BattleController", "Instance");
                object turns = GetInstanceProperty(controller, "TurnController");
                return SceneManager.GetActiveScene().name == "BattleScene" &&
                       controller != null &&
                       !ReferenceEquals(controller, previousController) &&
                       GetInstanceProperty(controller, "StageField") != null &&
                       turns != null &&
                       GetInstanceProperty(turns, "CurrentTurn") != null;
            },
            BattleTimeoutSeconds,
            "BattleScene did not become ready.");
        capture(controller);
    }

    private static object CreateDebugParty()
    {
        Type partyType = FindType("GamePlay.Common.Scripts.Entities.Character.Party");
        object party = Activator.CreateInstance(partyType);
        InvokeInstance(party, "InitParty");
        return party;
    }

    private static void ConfigureBattleSession(object session, object party, List<string> stages)
    {
        InvokeInstance(
            session,
            "SetBattleData",
            party,
            SystemEnum.Dungeon.MOUNTAIN_BACK,
            1,
            stages,
            SystemEnum.eScene.LobbyScene,
            false,
            -1);
    }

    private static object GetFirstPartyMemberStat(object party)
    {
        object members = party.GetType().GetField("partyMembers", BindingFlags.Public | BindingFlags.Instance)?.GetValue(party);
        object first = ((IEnumerable)members).Cast<object>().First();
        return GetInstanceProperty(first, "BaseStat");
    }

    private static IEnumerator WaitUntilOrFail(Func<bool> predicate, float timeoutSeconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(predicate(), Is.True, message);
    }

    private static Type FindType(string fullName)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(found => found != null);
        return type ?? throw new InvalidOperationException($"Type not found: {fullName}");
    }

    private static object GetStaticProperty(string typeName, string propertyName) =>
        FindType(typeName).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);

    private static bool GetStaticBool(string typeName, string propertyName) =>
        (bool)GetStaticProperty(typeName, propertyName);

    private static object GetInstanceProperty(object instance, string propertyName)
    {
        if (instance == null)
            return null;
        return instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
    }

    private static void InvokeStatic(string typeName, string methodName)
    {
        MethodInfo method = FindType(typeName)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName && candidate.GetParameters().Length == 0);
        if (method == null)
            throw new MissingMethodException(typeName, $"{methodName}()");

        method.Invoke(null, null);
    }

    private static object InvokeInstance(object instance, string methodName, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
        return method?.Invoke(instance, arguments);
    }

    private static object GetPrivateField(object instance, string fieldName) =>
        instance?.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
}
