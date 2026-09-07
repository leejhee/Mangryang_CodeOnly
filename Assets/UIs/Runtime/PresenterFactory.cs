using UnityEngine;

namespace UIs.Runtime
{
    public abstract class PresenterFactory : ScriptableObject
    {
        public abstract IPresenter Create(ViewID id, IView view, object param=null);

    }
}