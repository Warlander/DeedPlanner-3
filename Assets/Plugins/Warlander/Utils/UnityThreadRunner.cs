using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Plugins.Warlander.Utils
{
    public class UnityThreadRunner : MonoBehaviour
    {
        private readonly ConcurrentQueue<Action> _actionsToRun = new ConcurrentQueue<Action>();

        private void Update()
        {
            while (_actionsToRun.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        public void RunOnUnityThread(Action action)
        {
            _actionsToRun.Enqueue(action);
        }
    }
}