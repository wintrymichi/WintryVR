using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>
    /// Marshals work back onto the Unity main thread. All AI/voice/network work is async;
    /// anything touching UnityEngine objects goes through here.
    /// </summary>
    public sealed class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static int _mainThreadId;
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("WintryMainThreadDispatcher");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MainThreadDispatcher>();
                    _mainThreadId = Thread.CurrentThread.ManagedThreadId;
                }
                return _instance;
            }
        }

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            Instance._queue.Enqueue(action);
        }

        public static Task RunAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(() =>
            {
                try { action(); tcs.SetResult(true); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        public static Task<T> RunAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Enqueue(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        private void Update()
        {
            int budget = 256; // never let a burst of callbacks stall a frame
            while (budget-- > 0 && _queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { WintryLog.E("Dispatcher", "Queued action failed", ex); }
            }
        }
    }
}
