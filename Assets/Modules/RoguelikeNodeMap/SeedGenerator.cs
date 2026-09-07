using System.Linq;

namespace Modules.RoguelikeNodeMap
{
    /// <summary>
    /// 임시로 만들어놓은 시드생성기.
    /// 추후 시드 기획 필요 시 구체화 예정.
    /// </summary>
    public static class SeedGenerator
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // 입력이 없을 경우 랜덤시드를 여기서 만든다.
        public static string GenerateSeed()
        {
            System.Random random = new System.Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}