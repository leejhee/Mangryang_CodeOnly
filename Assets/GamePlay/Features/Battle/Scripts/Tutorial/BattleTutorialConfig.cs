using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    [CreateAssetMenu(
        menuName = "GamePlay/Battle/Tutorial Config",
        fileName = "BattleTutorialConfig")]
    public class BattleTutorialConfig : ScriptableObject
    {
        public BattleTutorialStep[] steps;
    }
}