using AngelBeat;
using Core.Scripts.Managers;
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// Village Scene - '마을' 파트의 씬을 초기화하는 오브젝트.
    /// </summary>
    public class VillageScene : MonoBehaviour
    {
        [SerializeField] private GameObject explorer;
        
        private void Awake()
        {
            GameManager instance = GameManager.Instance;
        }

        private void Start()
        {
            Instantiate(explorer);
        }
    }
}