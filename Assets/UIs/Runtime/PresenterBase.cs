using Cysharp.Threading.Tasks;
 using System;
 using System.Threading;

namespace UIs.Runtime 
{
    public abstract class PresenterBase<TView> : IPresenter where TView : class, IView
    {
        protected readonly TView View;
        protected readonly PresenterEventBag ViewEvents = new();
        protected readonly PresenterEventBag ModelEvents = new();
        protected CancellationTokenSource Cts;

        private bool _entered;

        protected PresenterBase(IView view)
        {
            View = view as TView ??
                   throw new ArgumentException(
                       $"Presenter expects view {typeof(TView).Name} but got null");
        }

        #region IPresenter Implementation

        public virtual void OnPause() { }
        public virtual void OnResume() { }

        public virtual async UniTask OnEnterAsync(CancellationToken token)
        {
            if (_entered) return;
            _entered = true;

            Cts?.Dispose();
            Cts = new CancellationTokenSource();

            await EnterAction(token);

            View.Show();
            await View.PlayEnterAsync(Cts.Token);
        }

        public virtual async UniTask OnExitAsync(CancellationToken token)
        {
            if (!_entered) return;
            _entered = false;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(Cts?.Token ?? token, token);
            try
            {
                await View.PlayExitAsync(linked.Token);
                View.Hide();

                await ExitAction(linked.Token);
            }
            finally
            {
                ViewEvents.Clear();
                ModelEvents.Clear();
                Cts?.Cancel();
                Cts?.Dispose();
                Cts = null;
            }
        }

        public virtual void Dispose()
        {
            Cts?.Cancel();
            Cts?.Dispose();
            Cts = null;
            ViewEvents.Dispose();
            ModelEvents.Dispose();
        }

        #endregion

        public virtual UniTask EnterAction(CancellationToken token) => UniTask.CompletedTask;
        public virtual UniTask ExitAction(CancellationToken token) => UniTask.CompletedTask;
    }
}
