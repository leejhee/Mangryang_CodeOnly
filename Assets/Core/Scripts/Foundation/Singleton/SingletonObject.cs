using System;
using System.Reflection;
using UnityEngine;

namespace Core.Scripts.Foundation.Singleton
{
    /// <summary>
    /// Singleton 객체를 위한 베이스 클래스 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingletonObject<T> where T : class
    {
        /// <summary>
        /// Singleton 객체 호출
        /// </summary>
        public static T Instance
        {
            get
            {
                return SingletonAllocator._Instance;
            }
        }

        public virtual void Init()
        {
            Debug.Log($"Static Singleton Initializing : {typeof(T)}");
        }
        #region 생성자
        /// <summary>
        /// Singleton 생성자
        /// </summary>
        protected SingletonObject() { }
        #endregion 생성자

        #region Singleton용 접근제한 클래스
        /// <summary>
        /// Singleton용 접근제한 클래스
        /// </summary>
        internal static class SingletonAllocator
        {
            internal static T _Instance = null; // Singleton 객체

            /// <summary>
            /// SingletonAllocator 생성자 
            /// </summary>
            static SingletonAllocator()
            {
                CreateInstance(typeof(T));
                Initialize(typeof(T));
            }

            /// <summary>
            /// _Instance 생성 
            /// </summary>
            /// <param name="type"> 생성 타입 </param>
            static void CreateInstance(Type type)
            {
                ConstructorInfo constructorNonPublic = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);

                ConstructorInfo[] constructorPublic = type.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public);

                if (constructorPublic.Length > 0)
                {
                    Debug.LogError($"{type.FullName}의 생성자를 확인해주세요. Singleton 은 Public 생성자를 허용하지 않습니다. ");
                    return;
                }

                // 생성자에서 인스턴스를 생성합니다.
                if (constructorNonPublic != null)
                {
                    _Instance = (T)constructorNonPublic.Invoke(null);
                }
                else
                {
                    Debug.LogError("Singleton 객체를 생성할 수 없습니다. 비공개 생성자가 필요합니다.");
                }
            }

            // Singleton 오브젝트 공통 로직
            static void Initialize(Type type)
            {

            }

        }
        #endregion Singleton용 접근제한 클래스

    }
}