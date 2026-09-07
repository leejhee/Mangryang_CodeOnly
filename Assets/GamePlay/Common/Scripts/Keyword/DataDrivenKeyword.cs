using Core.Scripts.Data;
using System.Collections.Generic;

namespace GamePlay.Features.Scripts.Keyword
{
    /// <summary>
    /// KeywordData, KeywordGrantData, KeywordEffectData만으로 동작하는 공통 키워드입니다.
    /// 별도의 런타임 상태나 생명주기가 필요한 키워드만 전용 구현체를 사용합니다.
    /// </summary>
    public sealed class DataDrivenKeyword : KeywordBase
    {
        public DataDrivenKeyword(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
            : base(data, grant, effects)
        { }
    }
}
