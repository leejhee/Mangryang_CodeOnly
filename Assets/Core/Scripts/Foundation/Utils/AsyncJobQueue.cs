using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Scripts.Foundation.Utils
{
    /// <summary>
    /// fire-and-forget 패턴의 전역 비동기 작업 큐
    /// </summary>
    public static class AsyncJobQueue
    {
        /// <summary>
        /// thread-safe한 비동기 큐. SLM이 Producer(작업 delegate 보냄), WorkerLoop 하나가 Consumer(받아서 처리함)
        /// </summary>
        private static Channel<Func<CancellationToken, Task>> queue;
        private static CancellationTokenSource globalCts;
        private static readonly Dictionary<string, CancellationTokenSource> KeyCts = new();
        private static readonly object Gate = new();
        private static readonly object WorkerGate = new();
        private static Task worker;

        private static int pendingCount;
        private static TaskCompletionSource<bool> idle = NewIdle();
        private static TaskCompletionSource<bool> NewIdle() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        static AsyncJobQueue()
        {
            Reset();
            StartWorker();
        }

        private static void Reset()
        {
            queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
            globalCts?.Dispose();
            globalCts = new CancellationTokenSource();
            pendingCount = 0;
            idle = NewIdle();
        }

        private static void StartWorker()
        {
            lock (WorkerGate)
            {
                if (worker is { IsCompleted: false })
                    return;
                if(globalCts == null || globalCts.IsCancellationRequested)
                {
                    globalCts?.Dispose();
                    globalCts = new CancellationTokenSource();
                }

                worker = Task.Run(WorkerLoop);
            }
        }
        
        /// <summary>
        /// 대길이같은 역할. 비동기 작업수행을 의미한다.
        /// </summary>
        private static async Task WorkerLoop()
        {
            try
            {
                while (await queue.Reader.WaitToReadAsync(globalCts.Token).ConfigureAwait(false))
                {
                    while (queue.Reader.TryRead(out Func<CancellationToken, Task> job))
                    {
                        try { await job(globalCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException)
                        {/*꺼@지는거지 뭐*/}
                        catch (Exception e) { Debug.LogError($"[AsyncJobQueue] job error: {e}"); }
                        finally
                        {
                            if (Interlocked.Decrement(ref pendingCount) == 0)
                                idle.TrySetResult(true);
                        }
                    }
                }
            }
            catch(OperationCanceledException){ /*꺼@지는거지 뭐*/}
        }

        public static void Enqueue(Func<CancellationToken, Task> job)
        {
            EnsureStarted();
            if (Interlocked.Increment(ref pendingCount) == 1)
                idle = NewIdle();
            if (!queue.Writer.TryWrite(job))
            {
                if (Interlocked.Decrement(ref pendingCount) == 0)
                    idle.TrySetResult(true);
                Debug.LogWarning("[AsyncJobQueue] job failed. Writer Closed");
            }
        }

        public static void EnqueueKeyed(string key, Func<CancellationToken, Task> job)
        {
            EnsureStarted();
            CancellationToken t;
            lock (Gate)
            {
                if (KeyCts.TryGetValue(key, out var prev))
                {
                    prev.Cancel();
                    prev.Dispose();
                }

                var cts = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token);
                KeyCts[key] = cts;
                t = cts.Token;
            }

            if (Interlocked.Increment(ref pendingCount) == 1)
                idle = NewIdle();
            queue.Writer.TryWrite(async _ =>
            {
                try { await job(t); }
                finally
                {
                    lock (Gate)
                    {
                        if (KeyCts.TryGetValue(key, out var prev) && prev.Token == t)
                        {
                            prev.Dispose();
                            KeyCts.Remove(key);
                        }
                    }

                    //if (Interlocked.Decrement(ref pendingCount) == 0)
                    //    idle.TrySetResult(true);
                }
            });
        }

        public static Task EnqueueKeyedWithCompletion(string key, Func<CancellationToken, Task> job)
        {
            EnsureStarted();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            EnqueueKeyed(key, async ct =>
            {
                try{ await job(ct); tcs.TrySetResult(true); }
                catch(OperationCanceledException) {tcs.TrySetCanceled(ct);}
                catch(Exception e) { tcs.TrySetException(e); }
            });
            return tcs.Task;

        }
        
        // key 작업 때려치는 함수
        public static void CancelKey(string key)
        {
            lock (Gate)
            {
                if (KeyCts.TryGetValue(key, out var cts))
                {
                    cts.Cancel(); cts.Dispose();
                    KeyCts.Remove(key);
                }
            }
        }
        
        // 작업 다 때려치는 함수
        public static void CancelAll()
        {
            lock (Gate)
            {
                foreach (var kv in KeyCts.Values) kv.Cancel();
                foreach (var kv in KeyCts.Values) kv.Dispose();
                KeyCts.Clear();
            }

            lock (WorkerGate)
            {
                try{globalCts?.Cancel();} catch{}
            }
            
            idle?.TrySetResult(true);
            Reset();
            StartWorker();
        }

        public static Task WaitIdleAsync() => idle.Task;

        public static void EnsureStarted() => StartWorker();
    }
    
}
