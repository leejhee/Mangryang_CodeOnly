using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace GamePlay.Common.Scripts.Novel
{
    public static class NovelDomainPlayer
    {
        public static async UniTask PlayNovelScript(string scriptTitle, CancellationToken ct = default)
        {
            InputManager.Instance.SetUIOnly(true);
            await NovelManager.PlayScriptAndWait(scriptTitle, ct);
            InputManager.Instance.SetUIOnly(false);
        } 
    }
}