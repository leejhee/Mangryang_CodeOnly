using System;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    /// <summary>
    /// 절차적 탐사 맵의 형태와 필수 품질 기준을 정의한다.
    /// 필드 순서와 의미는 생성 설정 서명에 포함되므로 변경 시 생성기 버전도 올려야 한다.
    /// </summary>
    [Serializable]
    public sealed class ExploreMapGenerationRules
    {
        [Header("Main Route")]
        [Min(2)] public int minSpineNodes = 5;
        [Min(2)] public int maxSpineNodes = 8;
        [Min(1)] public int minNodeSpacing = 2;
        [Min(1)] public int maxNodeSpacing = 4;
        [Min(1)] public int minStartBossPathLength = 8;

        [Header("Branches")]
        [Range(0f, 1f)] public float branchProbability = 0.25f;
        [Min(1)] public int minBranchLength = 1;
        [Min(1)] public int maxBranchLength = 3;

        [Header("Boundaries & Symbols")]
        [Min(1)] public int borderThickness = 1;
        [Min(1)] public int minSymbolSpacing = 2;
        [Min(0)] public int eliteSpacingBonus = 1;

        [Header("Generation Attempts")]
        [Min(1)] public int maxAttempts = 24;

        public static ExploreMapGenerationRules CreateDefault() => new();
    }
}
