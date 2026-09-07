using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts
{
    /// <summary>
    /// 에디터의 Battle Test Setup에서 만든 유닛 한 명의 런타임 배치 정보.
    /// 실제 스테이지 프리팹의 스폰 데이터는 변경하지 않는다.
    /// </summary>
    public sealed class BattleDebugUnitSetup
    {
        public CharacterModel Character { get; }
        public Vector2Int Cell { get; }

        public BattleDebugUnitSetup(CharacterModel character, Vector2Int cell)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            Cell = cell;
        }
    }

    /// <summary>
    /// 설정형 전투 테스트에만 사용하는 Scene Source.
    /// </summary>
    public sealed class ConfiguredBattleDebugSource : IBattleSceneSource
    {
        private readonly List<BattleDebugUnitSetup> _units;

        public SystemEnum.Dungeon Dungeon { get; }
        public Party PlayerParty { get; }
        public string StageName { get; }
        public int StageIndex => 0;
        public SystemEnum.eScene ReturningScene => SystemEnum.eScene.LobbyScene;
        public IReadOnlyList<BattleDebugUnitSetup> Units => _units;

        public ConfiguredBattleDebugSource(
            SystemEnum.Dungeon dungeon,
            string stageName,
            Party playerParty,
            IEnumerable<BattleDebugUnitSetup> units)
        {
            if (string.IsNullOrWhiteSpace(stageName))
                throw new ArgumentException("Stage name is required.", nameof(stageName));

            Dungeon = dungeon;
            StageName = stageName;
            PlayerParty = playerParty ?? throw new ArgumentNullException(nameof(playerParty));
            _units = units == null
                ? throw new ArgumentNullException(nameof(units))
                : new List<BattleDebugUnitSetup>(units);
        }
    }
}
