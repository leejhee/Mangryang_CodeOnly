using System;
// ReSharper disable InconsistentNaming

namespace Core.Scripts.Foundation.Define
{
    public static class SystemEnum
    {
        #region System Domain
        public enum GameState
        {
            None,
            Lobby,
            Loading,
            Village,
            Explore,
            Battle,
            EMax
        }
        
        public enum ManagerType
        {
            Party,
            Inventory,
            Explore,
            Battle,
        }
        
        public enum UIEvent
        {
            Click,
            Drag,
        }

        public enum MouseEvent
        {
            Press,
            Click,
        }

        public enum Sound
        {
            Bgm,
            Effect,
            MaxCount
        }

        public enum eScene
        {
            None,
        
            LobbyScene,
            LoadingScene,
            ExploreScene,
            BattleScene,
            VillageScene,
            ViewTestScene,
            NovelTestScene,
            MaxCount
        }
        
        #endregion
        
        #region SO 관련 데이터 enum
        /// <summary>
        /// 엑셀 데이터로 받아올 예정
        /// </summary>
        /// 
        public enum eMapNode
        {
            None,
            Location,
            Event
        }
   
        public enum eNodeType
        {
            None,
        
            Floor,
            Event,
            Item,
            Combat,
            Boss,
            Bound,

            MaxValue
        }

        public enum eMystery
        {
            None,
            //추가 예정
        }

        public enum eEdgeEvents
        {
            None,
            //추가 예정.
        }

        #endregion
        
        #region Gameplay Domain

        public enum eRandomName
        {
            
        }
        
        public enum eCharType
        {
            None, 

            Player,
            Enemy,
            Neutral,

            eMax
        }

        public enum eObang
        {
            Red,
            Blue,
            Yellow,
            Black,
            White
        }
        
        public enum eStats
        {
            None,

            BLUE,
            N_BLUE,
            RED,
            N_RED,
            YELLOW,
            N_YELLOW,
            WHITE,
            N_WHITE,
            BLACK,
            N_BLACK,
        
            HP,
            NHP,
            NMHP,
        
            DEFENSE,
            NDEFENSE,
            MAGIC_RESIST,
            NMAGIC_RESIST,
        
            PHYSICAL_ATTACK,
            NPHYSICAL_ATTACK,
            MAGIC_ATTACK,
            NMAGIC_ATTACK,
            
            AILMENT_INFLICT,
            NAILMENT_INFLICT,
            ACCURACY,
            NACCURACY,
        
            CRIT_RATE,
            NCRIT_RATE,
        
            SPEED, 
            NSPEED,
            
            MOVEMENT,
            NMOVEMENT,
            ACTION_POINT,
            NACTION_POINT,
            NMACTION_POINT,
            
            EVATION,
            NEVATION,
            
            AILMENT_RESISTANCE,
            NAILMENT_RESISTANCE,
        
            RANGE_INCREASE,
            DAMAGE_INCREASE,
            ACCURACY_INCREASE,
        
            eMax
        }

        public enum EConditionCheckType
        {
            STACK_화상,
        
            STAT_NMHP,
            STAT_NHP,
        
        }

        public enum NarrativeStatType
        {
            Charming,
            Luck,
            Wisdom,
            Empathy,
            
            Length
        }
        
        public enum EConditionOpcode
        {
            None,
        
            BIGGER,
            SMALLER,
            EQUAL,
            NOT_EQUAL,
            GREATER_EQUAL,
            LESS_EQUAL,
        
            eMax
        }
    
        public enum eSkillType
        {
            None,

            PhysicalAttack,
            MagicAttack,
            Buff,
            Debuff,
            Heal,
            Etc,
            
            eMax

        }

        public enum ePivot
        {
            None,
        
            TARGET_SELF,
            TARGET_ENEMY,
            TARGET_ALLY,
        }

        public enum eUpgradeLevel
        {
            None,
            
        }

        public enum eSkillOwner
        {
            Dokkaebi,
            Normal,
        }

        public enum eRound
        {
            None,
            ceil,
            round,
            floor
        }
        
        public enum eRefreshPolicy
        {
            None,
            Refresh,
            Stack,
            Extend
        }

        public enum eRemovePolicy
        {
            ByDuration,
            ByCondition
        }

        public enum eEffectType
        {
            ControlMove,
            TriggerSkill,
            Debuff,
            Dot
        }

        public enum eTriggerCondition
        {
            OnUse,
            OnHit,
            EndOfTurn
        }
        
        
        
        public enum eSourceType
        {
            None,
            Skill,
        }

        public enum eApplyTarget
        {
            None,
            TARGET_SELF,
            TARGET_ENEMY,
        }

        public enum eStackBehavior
        {
            Refresh,
            Cumulative,
            
        }
        
        public enum Dungeon
        {
            None,
            
            TUTORIAL,
            MOUNTAIN_BACK,
        
            eMax
        }
        
        #region Explore 
        
        [Serializable]
        public enum MapCellType
        {
            None,
            Wall,
            Floor,
        }
        
        [Serializable]
        public enum MapSymbolType
        {
            None,
            StartPoint,
            EndPoint,
            Battle,
            EliteBattle,
            BossBattle,
            Gather,
            Event,
            Item,
        }
        
        [Serializable]
        public enum CellEventType
        {
            DUMMY_1,
            DUMMY_2,
            DUMMY_3,
            DUMMY_4,
            DUMMY_5,
        
            eMax,
        }
        
        #endregion
        
        #region Execution & Keyword
        public enum eExecutionType
        {
            None,
            STACK_CHANGE,
            EXCHANGE_STACK,

            eMax
        }

        public enum eKeyword
        {
            None,
        
            ParryReady,
            Smoke,
            SmokeBind,
            Twister,
            BanMove,
            DefenseDecrease,
            
            eMax
        }

        public enum eExecutionPhase
        {
            None,
            SoR,
            EoR,
            SoT,
            EoT,
            Instant,
            Always,
            eMax
        }
    
        public enum eKeywordTargetType
        {
            None,
            Self,
            AllyAll,
            EnemyAll,
            EnemyNearest,
            EnemyRandom,
            // 추가 가능
        }

        public enum eInfluenceType
        {
            None,
        
            Negative,
            Positive,
            Neutral,
        
            eMax
        }
    
        #endregion
    
        public enum eIsAttack
        {
            Player,
            Monster,

            eMax
        }
    
        [Flags]
        public enum eUnlockCondition
        {
            Default = 0,

        }

        public enum eSkillUnlock
        {
            None,
            Default,
            EUINYEO_SKILL_1,
            EUINYEO_SKILL_2,
            EUINYEO_SKILL_3,
        }
        #endregion
        
        
        
    }
}
