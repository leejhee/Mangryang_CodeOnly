using Core.Scripts.Managers;
using System;
using UnityEngine;

namespace GamePlay.Features.Scripts.Explore
{
    public class ExploreScene : MonoBehaviour
    {
        [SerializeField] private GameObject exploreController;

        private void Awake()
        {
#if UNITY_EDITOR
            GameManager instance = GameManager.Instance;
#endif
            Debug.Log("[Scene Initialized] : ExploreScene");
            // Controller은 이동과 상호작용 용도의 포트 역할 객체이므로 여기서 인스턴스화함. 
            Instantiate(exploreController);
        }

    }
}

