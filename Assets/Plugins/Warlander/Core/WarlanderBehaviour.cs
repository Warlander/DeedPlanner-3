using System;
using System.Collections;
using UnityEngine;

namespace Warlander.Core
{
    public class WarlanderBehaviour : MonoBehaviour
    {
        protected void RunAfter(float time, Action action)
        {
            StartCoroutine(RunAfterCoroutine(time, action));
        }

        private IEnumerator RunAfterCoroutine(float time, Action action)
        {
            yield return new WaitForSeconds(time);
            action();
        }

        protected void RunNextFrame(Action action)
        {
            StartCoroutine(RunNextFrameCoroutine(action));
        }

        private IEnumerator RunNextFrameCoroutine(Action action)
        {
            yield return null;
            action();
        }
    }
}