using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects.Reward
{
    // 임시 클래스
    [System.Serializable]
    public class SkillReward
    {
        public long id;
        public Sprite selected;
        public Sprite deSelected;
        
        public SkillReward() {}
        public SkillReward(long id, Sprite selected = null, Sprite deSelected = null)
        {
            this.id = id;
            this.selected = selected;
            this.deSelected = deSelected;
        }
    }


// 얘도 나중에 바꿔줄거임
    [System.Serializable]
    public class RewardTable
    {
        public string name;
        public List<SkillReward> rewardRange;
    }
}