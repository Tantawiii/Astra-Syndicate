using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>A pooled one-shot particle effect (impacts, explosions) that returns itself to the pool when done.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledEffect : MonoBehaviour
    {
        private ParticleSystem _particles;

        public static void Spawn(PooledEffect prefab, Vector3 position, Quaternion rotation, float scale = 1f)
        {
            if (prefab == null) return;

            PooledEffect effect = PrefabPool<PooledEffect>.Get(prefab);
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.transform.localScale = Vector3.one * scale;
            effect.Play();
        }

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _particles.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void Play()
        {
            _particles.Clear(true);
            _particles.Play(true);
        }

        // Called by Unity when the system finishes, because stopAction is Callback.
        private void OnParticleSystemStopped()
        {
            PrefabPool<PooledEffect>.Release(this);
        }
    }
}
