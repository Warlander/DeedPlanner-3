using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugins.Warlander.Utils
{
    public class UnityThreadRunner : MonoBehaviour
    {
        private Queue<Action> _actionsToRun = new Queue<Action>();

        private void Update()
        {
            while (_actionsToRun.Count > 0)
            {
                _actionsToRun.Dequeue()?.Invoke();
            }
        }
        
        public void RunOnUnityThread(Action action)
        {
            _actionsToRun.Enqueue(action);
        }
    }
}