using GamePlay.Common.Scripts.Novel;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Symbol.Encounter
{
    /// <summary>
    /// 고정된 맵에서만 우선 사용 가능
    /// </summary>
    public class NovelEncounter : MonoBehaviour
    {
        [SerializeField] private string novelTitle;
        private async void PlayNovel()
        {
            await NovelDomainPlayer.PlayNovelScript(novelTitle);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ExploreController player = other.GetComponent<ExploreController>();
            if (!player) return;
            
            PlayNovel();
        }
    }
}
