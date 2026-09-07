using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Scripts.Foundation.Singleton
{
    public abstract class SingletonMono<T> : ManagerBehaviour where T : SingletonMono<T>
    {
        private static T instance;
 
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }
 
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }


        public abstract UniTask InitAsync();

        public override ManagerBehaviour GetInstance() => Instance;
    }
}