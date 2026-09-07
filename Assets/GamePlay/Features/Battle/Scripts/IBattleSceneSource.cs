using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using GamePlay.Common.Scripts.Entities.Character;
using System.Collections.Generic;

namespace GamePlay.Features.Battle.Scripts
{
    public interface IBattleSceneSource : ISceneArgs
    {
        public SystemEnum.Dungeon Dungeon { get; }
        public Party PlayerParty { get; }
        public string StageName { get; }
        public int StageIndex { get; }
        public SystemEnum.eScene ReturningScene { get; }
        
    }

    public class BattlePayloadSource : IBattleSceneSource
    {
        public SystemEnum.Dungeon Dungeon => BattleSession.Instance.DungeonName;
        public Party PlayerParty => BattleSession.Instance.PlayerParty;
        
        public string StageName => BattleSession.Instance.CurrentStageName;
        public int StageIndex => BattleSession.Instance.CurrentStageIndex;
        public SystemEnum.eScene ReturningScene => BattleSession.Instance.ReturningScene;
        public void ClearPayload() => BattleSession.Instance.Clear();
    }

    public class DebugMockSource : IBattleSceneSource
    {
        private readonly SystemEnum.Dungeon _dungeon;
        private readonly Party _playerParty;
        private readonly string _stageName;
        
        public SystemEnum.Dungeon Dungeon => _dungeon;
        public Party PlayerParty => _playerParty;
        public string StageName => _stageName;
        public int StageIndex => 0;
        public SystemEnum.eScene ReturningScene => SystemEnum.eScene.LobbyScene;
        
        public DebugMockSource(SystemEnum.Dungeon dungeon, Party playerParty, string stageName)
        {
            _dungeon = dungeon;
            _playerParty = playerParty;
            _stageName = stageName;
        }

        public static DebugMockSource Default()
        {
            Party p = new();
            p.InitParty();
            return new DebugMockSource(SystemEnum.Dungeon.MOUNTAIN_BACK, p, "0101");
        }
    }
}
