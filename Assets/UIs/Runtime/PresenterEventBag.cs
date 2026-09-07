using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UIs.Runtime
{
    /// <summary>
    /// Model - Presenter, Presenter - View의 구독을 저장.
    /// Presenter의 Dispose 시 dangling을 방지하기 위해서, 구독한 이벤트들도 전부 IDisposable로 래핑하여 저장.
    /// </summary>
    public sealed class PresenterEventBag : IDisposable
    {
        private readonly List<IDisposable> _bus = new();
        public void Add(IDisposable d) { if (d != null) _bus.Add(d); }
        public void Clear() { foreach (var d in _bus) d.Dispose(); _bus.Clear(); }
        public void Dispose() => Clear();
    }
    
    /// <summary>
    /// 람다 식 전용 Disposable Event
    /// </summary>
    internal sealed class DisposableEvent : IDisposable
    {
        private Action _dispose;
        private DisposableEvent(Action dispose) { _dispose = dispose; }
        public static IDisposable Create(Action dispose) => new DisposableEvent(dispose);
        public void Dispose() {_dispose?.Invoke(); _dispose = null;}
    }

    public sealed class Subscription : IDisposable
    {
        private Action<Action> _remove;
        private Action _handler;

        public Subscription(Action<Action> remove, Action handler)
        {
            _remove = remove;
            _handler = handler;
        }

        public void Dispose()
        {
            if(_remove != null && _handler != null) _remove(_handler);
            _handler = null; _remove = null;
        }
    }

    public sealed class Subscription<T> : IDisposable
    {
        private Action<Action<T>> _remove;
        private Action<T> _handler;

        public Subscription(Action<Action<T>> remove, Action<T> handler)
        {
            _remove = remove;
            _handler = handler;
        }

        public void Dispose()
        {
            if(_remove != null && _handler != null) _remove(_handler);
            _handler = null; _remove = null;
        }
    }
    
    public sealed class Subscription<T1, T2> : IDisposable
    {
        private Action<Action<T1, T2>> _remove;
        private Action<T1, T2> _handler;

        public Subscription(Action<Action<T1, T2>> remove, Action<T1, T2> handler)
        {
            _remove = remove; _handler = handler;
        }

        public void Dispose()
        {
            if (_remove != null && _handler != null) _remove(_handler);
            _handler = null; _remove = null;
        }
    }
    
    public sealed class Subscription<T1, T2, T3> : IDisposable
    {
        private Action<Action<T1, T2, T3>> _remove;
        private Action<T1, T2, T3> _handler;

        public Subscription(Action<Action<T1, T2, T3>> remove, Action<T1, T2, T3> handler)
        {
            _remove = remove; _handler = handler;
        }

        public void Dispose()
        {
            if (_remove != null && _handler != null) _remove(_handler);
            _handler = null; _remove = null;
        }
    }
    
    public sealed class AsyncSubscription : IDisposable
    {
        private Action<Func<UniTask>> _remove;
        private Func<UniTask> _handler;

        public AsyncSubscription(Action<Func<UniTask>> remove, Func<UniTask> handler)
        {
            _remove = remove;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_remove != null && _handler != null) _remove(_handler);
            _handler = null; _remove = null;
        }
    }

    public sealed class AsyncSubscription<T> : IDisposable
    {
        private Action<Func<T,UniTask>> _remove;
        private Func<T,UniTask> _handler;

        public AsyncSubscription(Action<Func<T,UniTask>> remove, Func<T,UniTask> handler)
        {
            _remove = remove;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_remove != null && _handler != null)
                _remove(_handler);

            _handler = null;
            _remove = null;
        }
    }

    public sealed class AsyncSubscription<TValue, TResult> : IDisposable
    {
        private Action<Func<TValue,UniTask<TResult>>> _remove;
        private Func<TValue,UniTask<TResult>> _handler;

        public AsyncSubscription(Action<Func<TValue,UniTask<TResult>>> remove, Func<TValue,UniTask<TResult>> handler)
        {
            _remove = remove;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_remove != null && _handler != null)
                _remove(_handler);

            _handler = null;
            _remove = null;
        }
    }
    /// <summary>
    /// 구독할 핸들러에 대해서 편리하게 등록하게 하기 위한 익스텐션. Model, View에 대한 bag에다가 등록해주면 된다.
    /// Model은 Presenter 구현에서 알아서 사용할 수 있도록 제약을 안걸었기 때문에 자유롭게 사용하자.
    /// </summary>
    public static class UIEventExtensions
    {
        public static T AddTo<T>(this T d, PresenterEventBag bag) where T : IDisposable
        {
            bag.Add(d);
            return d;
        }

        public static IDisposable Subscribe(this PresenterEventBag bag, 
            Action<Action> add, Action<Action> remove, Action handler)
        {
            add(handler);
            return new Subscription(remove, handler).AddTo(bag);
        }
        public static IDisposable SubscribeAsync(this PresenterEventBag bag, Action<Func<UniTask>> add,
            Action<Func<UniTask>> remove, Func<UniTask> handler)
        {
            add(handler);
            return new AsyncSubscription(remove, handler).AddTo(bag);
        }
        public static IDisposable Subscribe<T>(this PresenterEventBag bag,
            Action<Action<T>> add, Action<Action<T>> remove, Action<T> handler)
        {
            add(handler);
            return new Subscription<T>(remove, handler).AddTo(bag);
        }
        
        public static IDisposable Subscribe<T1, T2>(this PresenterEventBag bag,
            Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove, Action<T1, T2> handler)
        {
            add(handler);
            return new Subscription<T1, T2>(remove, handler).AddTo(bag);
        }
        
        public static IDisposable Subscribe<T1, T2, T3>(this PresenterEventBag bag,
            Action<Action<T1, T2, T3>> add, Action<Action<T1, T2, T3>> remove, Action<T1, T2, T3> handler)
        {
            add(handler);
            return new Subscription<T1, T2, T3>(remove, handler).AddTo(bag);
        }
        
        // T는 이벤트로 뿌리는 자료형
        // handler의 자료형은 UniTask
        public static IDisposable SubscribeAsync<T>(this PresenterEventBag bag,
            Action<Func<T,UniTask>> add, Action<Func<T,UniTask>> remove, Func<T, UniTask> handler)
        {
            add(handler);
            return new AsyncSubscription<T>(remove, handler).AddTo(bag);
        }

        public static IDisposable SubscribeAsync<TValue, TResult>(this PresenterEventBag bag,
            Action<Func<TValue, UniTask<TResult>>> add, Action<Func<TValue, UniTask<TResult>>> remove,
            Func<TValue, UniTask<TResult>> handler)
        {
            add(handler);
            return new AsyncSubscription<TValue, TResult>(remove, handler).AddTo(bag);
        }
        public static IDisposable Subscribe(this PresenterEventBag bag, UnityEvent evt, UnityAction ua)
        {
            evt.AddListener(ua);
            return DisposableEvent.Create(() => evt.RemoveListener(ua)).AddTo(bag);
        }
        
        /// <summary>
        /// UnityEvent 전용
        /// </summary>
        public static IDisposable Subscribe<T>(this PresenterEventBag bag, UnityEvent<T> evt, UnityAction<T> ua)
        {
            evt.AddListener(ua);
            return DisposableEvent.Create(() => evt.RemoveListener(ua)).AddTo(bag);
        }

    }
}