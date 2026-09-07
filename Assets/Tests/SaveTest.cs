using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using Core.Scripts.GameSave;
using Core.Scripts.GameSave.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [Category("SavePersistence")]
    public class SaveTest
    {
        private const ulong TestSlotSeed = 0x0123456789ABCDEFUL;

        private string _testRoot;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "AngelBeat.SaveTests",
                Guid.NewGuid().ToString("N"));

            SlotIO.InitUserRoot(_testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Test]
        public void GlobalSaveData_NewInstall_HasRequiredDefaults()
        {
            var global = new GlobalSaveData();

            Assert.That(global.UID, Is.Not.Null.And.Not.Empty);
            Assert.That(global.FirstInstallTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(global.GameSlots, Is.Not.Null.And.Empty);
            Assert.That(global.GameSettings, Is.Not.Null);
            Assert.That(global.LastPlayedSlotIndex, Is.EqualTo(-1));
            Assert.That(global.MasterSeed, Is.Not.EqualTo(0UL));
            Assert.That(global.SlotGeneration, Is.EqualTo(0UL));
        }

        [Test]
        public void SlotSeed_SameNameAcrossGenerations_IsDistinctAndReproducible()
        {
            const ulong masterSeed = 123456789UL;
            const string slotName = "New Game";

            ulong first = RandomUtil.DeriveSlotSeed(masterSeed, slotName, 1UL);
            ulong firstAgain = RandomUtil.DeriveSlotSeed(masterSeed, slotName, 1UL);
            ulong second = RandomUtil.DeriveSlotSeed(masterSeed, slotName, 2UL);

            Assert.That(first, Is.Not.EqualTo(0UL));
            Assert.That(firstAgain, Is.EqualTo(first));
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void GameSlotData_WriteSnapshot_ReplacesSameFeature()
        {
            GameSlotData slot = CreateSlot();
            var first = new ExploreSnapshot { currentFloor = 1 };
            var replacement = new ExploreSnapshot { currentFloor = 2 };

            slot.WriteSnapshot(first);
            slot.WriteSnapshot(replacement);

            Assert.That(slot.Features.Count, Is.EqualTo(1));
            Assert.That(slot.TryGet("Explore", out ExploreSnapshot saved), Is.True);
            Assert.That(saved, Is.Not.SameAs(replacement));
            Assert.That(saved.currentFloor, Is.EqualTo(2));
        }

        [Test]
        public void GameSlotData_SnapshotBoundary_DetachesMutableRuntimeState()
        {
            GameSlotData slot = CreateSlot();
            var runtime = new ExploreSnapshot { itemIds = new List<int> { 10 } };
            runtime.StartNewExploration(
                SystemEnum.Dungeon.MOUNTAIN_BACK,
                1,
                123UL,
                Vector2Int.zero,
                4);
            runtime.UpdatePlayerPosition(Vector2Int.zero, 0);
            runtime.AddCompletedEvent("before-save");

            slot.WriteSnapshot(runtime);

            runtime.itemIds.Add(20);
            runtime.UpdatePlayerPosition(Vector2Int.one, 1);
            runtime.AddCompletedEvent("after-save");

            Assert.That(slot.TryGet("Explore", out ExploreSnapshot firstRead), Is.True);
            Assert.That(firstRead.itemIds, Is.EqualTo(new[] { 10 }));
            Assert.That(firstRead.IsCellVisited(0), Is.True);
            Assert.That(firstRead.IsCellVisited(1), Is.False);
            Assert.That(firstRead.IsEventCompleted("before-save"), Is.True);
            Assert.That(firstRead.IsEventCompleted("after-save"), Is.False);

            firstRead.itemIds.Add(30);
            firstRead.UpdatePlayerPosition(Vector2Int.one, 1);

            Assert.That(slot.TryGet("Explore", out ExploreSnapshot secondRead), Is.True);
            Assert.That(secondRead.itemIds, Is.EqualTo(new[] { 10 }));
            Assert.That(secondRead.IsCellVisited(1), Is.False);
        }

        [Test, Timeout(8000)]
        public void SlotIO_RoundTrip_PreservesSlotAndConcreteSnapshot()
        {
            GameSlotData slot = CreateSlot();
            slot.lastGameState = SystemEnum.GameState.Explore;
            slot.PlayTime = TimeSpan.FromMinutes(17);

            ulong mapSeed = slot.RNG.DeriveAndIncrementSeed("Explore_MOUNTAIN_BACK_1");
            var snapshot = new ExploreSnapshot();
            snapshot.StartNewExploration(
                SystemEnum.Dungeon.MOUNTAIN_BACK,
                3,
                mapSeed,
                new Vector2Int(2, 4),
                25,
                generationVersion: 2,
                configSignature: 0xCAFEBABEUL);
            snapshot.UpdatePlayerPosition(new Vector2Int(3, 4), 7);
            snapshot.AddCompletedEvent("event-7");
            slot.WriteSnapshot(snapshot);

            SlotIO.SaveAsync("round-trip", slot, CancellationToken.None).GetAwaiter().GetResult();
            GameSlotData loaded = SlotIO.LoadAsync("round-trip", CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.slotName, Is.EqualTo(slot.slotName));
            Assert.That(loaded.lastGameState, Is.EqualTo(SystemEnum.GameState.Explore));
            Assert.That(loaded.PlayTime, Is.EqualTo(slot.PlayTime));
            Assert.That(loaded.slotSeed, Is.EqualTo(TestSlotSeed));
            Assert.That(loaded.RngCounters["Explore_MOUNTAIN_BACK_1"], Is.EqualTo(1UL));
            Assert.That(loaded.RNG, Is.Not.Null);
            Assert.That(loaded.slotProgress, Is.Not.Null);

            Assert.That(loaded.TryGet("Explore", out ExploreSnapshot loadedSnapshot), Is.True);
            Assert.That(loadedSnapshot.CurrentVersion, Is.EqualTo(ExploreSnapshot.V));
            Assert.That(loadedSnapshot.currentDungeon, Is.EqualTo(SystemEnum.Dungeon.MOUNTAIN_BACK));
            Assert.That(loadedSnapshot.currentFloor, Is.EqualTo(3));
            Assert.That(loadedSnapshot.mapSeed, Is.EqualTo(mapSeed));
            Assert.That(loadedSnapshot.mapGenerationVersion, Is.EqualTo(2));
            Assert.That(loadedSnapshot.mapConfigSignature, Is.EqualTo(0xCAFEBABEUL));
            Assert.That(loadedSnapshot.playerPosition, Is.EqualTo(new Vector2Int(3, 4)));
            Assert.That(loadedSnapshot.IsCellVisited(7), Is.True);
            Assert.That(loadedSnapshot.IsEventCompleted("event-7"), Is.True);
        }

        [Test, Timeout(8000)]
        public void SlotIO_RoundTrip_RngContinuesFromSavedCounter()
        {
            GameSlotData slot = CreateSlot();
            slot.RNG.NextInt("BattleReward", 0, 10_000);

            SlotIO.SaveAsync("rng", slot, CancellationToken.None).GetAwaiter().GetResult();
            GameSlotData loaded = SlotIO.LoadAsync("rng", CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(loaded.RngCounters["BattleReward"], Is.EqualTo(1UL));

            var controlCounters = new Dictionary<string, ulong>(loaded.RngCounters);
            var controlRng = new RngHubStateless(loaded.slotSeed, controlCounters);
            int expectedNext = controlRng.NextInt("BattleReward", 0, 10_000);
            int actualNext = loaded.RNG.NextInt("BattleReward", 0, 10_000);

            Assert.That(actualNext, Is.EqualTo(expectedNext));
            Assert.That(loaded.RngCounters["BattleReward"], Is.EqualTo(2UL));
        }

        [Test, Timeout(8000)]
        public void SlotIO_LoadMissingFile_ReturnsNull()
        {
            GameSlotData loaded = SlotIO.LoadAsync("missing", CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(loaded, Is.Null);
        }

        [Test, Timeout(8000)]
        public void SlotIO_LoadMalformedJson_Throws()
        {
            string path = Path.Combine(_testRoot, "malformed.json");
            File.WriteAllText(path, "{ not-valid-json", System.Text.Encoding.UTF8);

            Assert.Catch<Exception>(() =>
                SlotIO.LoadAsync("malformed", CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test, Timeout(8000)]
        public void SlotIO_LoadLegacyZeroSeed_NormalizesNonRngState()
        {
            string path = Path.Combine(_testRoot, "legacy-zero-seed.json");
            File.WriteAllText(path, "{\"slotName\":\"Legacy\",\"slotSeed\":0}", System.Text.Encoding.UTF8);
            LogAssert.Expect(LogType.Warning,
                "[GameSlotData] SlotSeed is 0 for slot 'Legacy'. Non-RNG data was restored, but RNG is unavailable.");

            GameSlotData loaded = SlotIO.LoadAsync("legacy-zero-seed", CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Features, Is.Not.Null);
            Assert.That(loaded.RngCounters, Is.Not.Null);
            Assert.That(loaded.slotProgress, Is.Not.Null);
            Assert.That(loaded.RNG, Is.Null);
        }

        [Test]
        public void ExploreSnapshot_DeepCopy_PreservesMapReproductionContract()
        {
            var snapshot = new ExploreSnapshot();
            snapshot.StartNewExploration(
                SystemEnum.Dungeon.MOUNTAIN_BACK,
                1,
                987654321UL,
                new Vector2Int(3, 5),
                400,
                generationVersion: 2,
                configSignature: 0x1234ABCDUL);

            var copy = (ExploreSnapshot)snapshot.DeepCopy();

            Assert.That(copy.mapSeed, Is.EqualTo(snapshot.mapSeed));
            Assert.That(copy.mapGenerationVersion, Is.EqualTo(2));
            Assert.That(copy.mapConfigSignature, Is.EqualTo(0x1234ABCDUL));
        }

        private static GameSlotData CreateSlot()
        {
            var slot = new GameSlotData("UT_Slot")
            {
                slotSeed = TestSlotSeed
            };
            slot.RNG = new RngHubStateless(slot.slotSeed, slot.RngCounters);
            return slot;
        }
    }
}
