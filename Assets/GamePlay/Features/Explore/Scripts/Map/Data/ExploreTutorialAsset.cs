using GamePlay.Features.Explore.Scripts.Map.Logic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [CreateAssetMenu(menuName = "GamePlay/Explore/Tutorial Skeleton Asset", fileName = "TutorialSkeleton")]
    public class ExploreTutorialAsset :  ScriptableObject
    {
        public ExploreMapSkeleton skeleton;
    }
}