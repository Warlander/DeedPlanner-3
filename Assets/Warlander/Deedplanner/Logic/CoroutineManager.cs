using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class CoroutineManager : MonoBehaviour
    {
        private const int SimultaneousCoroutinesAllowed = 1000;
        
        public static CoroutineManager Instance { get; private set; }
        
        [SerializeField] private CanvasGroup guiCanvasGroup = null;
        [SerializeField] private GameObject updatersRoot = null;
        private readonly Queue<IEnumerator> enumeratorsWaiting = new Queue<IEnumerator>();
        private readonly LinkedList<IEnumerator> enumeratorsExecuting = new LinkedList<IEnumerator>();
        
        public CoroutineManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            int enumeratorsToQueue = Math.Max(0, SimultaneousCoroutinesAllowed - enumeratorsExecuting.Count);
            for (int i = 0; i < enumeratorsToQueue; i++)
            {
                if (enumeratorsWaiting.Count == 0)
                {
                    break;
                }

                IEnumerator newExecutingEnumerator = enumeratorsWaiting.Dequeue();
                enumeratorsExecuting.AddLast(newExecutingEnumerator);
                StartCoroutine(WrapCoroutine(newExecutingEnumerator));
            }
        }

        public void QueueBlockingCoroutine(IEnumerator enumerator)
        {
            enumeratorsWaiting.Enqueue(enumerator);
            guiCanvasGroup.interactable = false;
            updatersRoot.SetActive(false);
        }

        private IEnumerator WrapCoroutine(IEnumerator enumerator)
        {
            yield return enumerator;
            enumeratorsExecuting.Remove(enumerator);
            if (enumeratorsWaiting.Count == 0 && enumeratorsExecuting.Count == 0)
            {
                guiCanvasGroup.interactable = true;
                updatersRoot.SetActive(true);
            }
        }
        
    }
}