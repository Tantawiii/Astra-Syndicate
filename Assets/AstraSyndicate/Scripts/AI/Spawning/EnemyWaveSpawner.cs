using System;
using System.Collections.Generic;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AstraSyndicate.AI
{
    /// <summary>
    /// Spawns enemy waves at spawn points; the next wave starts a while after the previous one is wiped out.
    /// Set Total Waves to 1 and First Wave Size to N for a fixed encounter of N ships.
    /// </summary>
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [SerializeField] private Ship enemyPrefab;
        [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();

        [Tooltip("Optional routes, handed out to spawned ships in turn.")]
        [SerializeField] private PatrolRoute[] patrolRoutes = Array.Empty<PatrolRoute>();

        [SerializeField, Min(1)] private int firstWaveSize = 3;
        [SerializeField, Min(0)] private int extraShipsPerWave = 1;
        [SerializeField, Min(1)] private int maxWaveSize = 8;
        [Tooltip("0 = endless.")]
        [SerializeField, Min(0)] private int totalWaves;
        [SerializeField, Min(0f)] private float firstWaveDelay = 2f;
        [SerializeField, Min(0f)] private float timeBetweenWaves = 6f;
        [Tooltip("Ships appear within this radius of their spawn point.")]
        [SerializeField, Min(0f)] private float spawnScatter = 40f;

        private readonly List<Ship> _alive = new List<Ship>();
        private IEventBus _events;
        private float _countdown;

        public event Action<int, int> WaveStarted;

        public int Wave { get; private set; }

        public int AliveCount => _alive.Count;

        public bool Finished => totalWaves > 0 && Wave >= totalWaves && _alive.Count == 0;

        /// <summary>Seconds until the next wave, or 0 while a wave is in progress.</summary>
        public float NextWaveIn => _alive.Count == 0 && !Finished ? Mathf.Max(0f, _countdown) : 0f;

        public void Configure(Ship prefab, Transform[] points, PatrolRoute[] routes)
        {
            enemyPrefab = prefab;
            spawnPoints = points ?? Array.Empty<Transform>();
            patrolRoutes = routes ?? Array.Empty<PatrolRoute>();
        }

        private void Awake()
        {
            Services.TryGet(out _events);
            _countdown = firstWaveDelay;
        }

        private void Update()
        {
            _alive.RemoveAll(ship => ship == null || !ship.IsAlive);
            if (_alive.Count > 0 || Finished || enemyPrefab == null || spawnPoints.Length == 0) return;

            _countdown -= Time.deltaTime;
            if (_countdown <= 0f) SpawnWave();
        }

        private void SpawnWave()
        {
            Wave++;
            int count = Mathf.Min(maxWaveSize, firstWaveSize + extraShipsPerWave * (Wave - 1));
            for (int i = 0; i < count; i++)
            {
                Transform point = spawnPoints[i % spawnPoints.Length];
                Ship ship = Instantiate(enemyPrefab, point.position + Random.insideUnitSphere * spawnScatter, point.rotation);
                ship.name = $"{enemyPrefab.name} {Wave}-{i + 1}";
                if (patrolRoutes.Length > 0 && ship.TryGetComponent(out EnemyBrain brain))
                {
                    brain.SetPatrolRoute(patrolRoutes[i % patrolRoutes.Length]);
                }
                _alive.Add(ship);
            }

            _countdown = timeBetweenWaves;
            WaveStarted?.Invoke(Wave, count);
            _events?.Publish(new WaveStartedEvent(Wave, count));
        }
    }
}
