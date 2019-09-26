using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class CoroutineManager : MonoBehaviour
    {
        private const float MaxFrameTime = 0.05f;

        public static CoroutineManager Instance { get; private set; }
        
        [SerializeField] private CanvasGroup guiCanvasGroup = null;
        [SerializeField] private GameObject updatersRoot = null;
        private readonly Queue<IEnumerator> enumeratorsWaiting = new Queue<IEnumerator>();
        private int enumeratorsExecutingCount = 0;
        private bool interactionLocked = false;

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
            while (enumeratorsWaiting.Count > 0)
            {
                IEnumerator newExecutingEnumerator = enumeratorsWaiting.Dequeue();
                StartCoroutine(WrapCoroutine(newExecutingEnumerator));
                enumeratorsExecutingCount++;
            }

            if (interactionLocked && enumeratorsWaiting.Count == 0 && enumeratorsExecutingCount == 0)
            {
                interactionLocked = false;
                guiCanvasGroup.interactable = true;
                updatersRoot.SetActive(true);
            }
        }

        public void BlockInteractionUntilFinished()
        {
            interactionLocked = true;
            guiCanvasGroup.interactable = false;
            updatersRoot.SetActive(false);
        }
        
        public void QueueCoroutine(IEnumerator enumerator)
        {
            enumeratorsWaiting.Enqueue(enumerator);
        }

        private IEnumerator WrapCoroutine(IEnumerator enumerator)
        {
            while (true)
            {
                float frameTime = Time.realtimeSinceStartup - Time.unscaledTime;
                if (frameTime > MaxFrameTime)
                {
                    yield return null;
                }
                else
                {
                    break;
                }
            }
            
            yield return enumerator;
            enumeratorsExecutingCount--;
        }
        
    }
}