using System;
using UnityEngine;

namespace iMobileDevice.Unity
{
    [DisallowMultipleComponent]
    public class GameObjectEventTrigger : MonoBehaviour
    {
        public Action updateCallback { get; set; }

        private void OnDestroy()
        {
            updateCallback = null;
        }

        private void Update()
        {
            updateCallback?.Invoke();
        }
    }
}