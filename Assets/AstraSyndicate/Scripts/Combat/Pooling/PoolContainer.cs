using System;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Scene object that parents pooled instances and tells its pool when the scene goes away.</summary>
    internal sealed class PoolContainer : MonoBehaviour
    {
        public event Action Destroyed;

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }
    }
}
