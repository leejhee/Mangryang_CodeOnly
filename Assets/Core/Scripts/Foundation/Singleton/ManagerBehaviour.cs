using Core.Scripts.Foundation.Define;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Scripts.Foundation.Singleton
{
    public abstract class ManagerBehaviour : MonoBehaviour
    {
        public abstract ManagerBehaviour GetInstance();

        [SerializeField] private SystemEnum.ManagerType managerType;
        
        [SerializeField] private List<SystemEnum.GameState> lifeCycles;
        
        public List<SystemEnum.GameState> LifeCycles => lifeCycles;
        
    }
}