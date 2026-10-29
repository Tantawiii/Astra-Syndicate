using System;
using System.Collections.Generic;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Runs one mission at a time: briefing, objective tracking, pause, time limit and the final result.
    /// Plain C# driven by <see cref="Tick"/>, so it is fully unit-testable; MissionRunner ticks it in play.
    /// </summary>
    public sealed class MissionStateManager : IMissionStateManager
    {
        private readonly IEventBus _events;
        private readonly ObjectiveFactory _objectiveFactory;
        private readonly List<IObjective> _objectives = new List<IObjective>();
        private bool _tracking;

        public MissionStateManager(IEventBus events, ObjectiveFactory objectiveFactory)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _objectiveFactory = objectiveFactory ?? throw new ArgumentNullException(nameof(objectiveFactory));
        }

        public MissionState State { get; private set; } = MissionState.Idle;

        public MissionData CurrentMission { get; private set; }

        public IReadOnlyList<IObjective> Objectives => _objectives;

        public float ElapsedSeconds { get; private set; }

        public float? TimeRemaining
        {
            get
            {
                if (CurrentMission == null || CurrentMission.timeLimitSeconds <= 0f) return null;
                return Mathf.Max(0f, CurrentMission.timeLimitSeconds - ElapsedSeconds);
            }
        }

        public MissionResult? LastResult { get; private set; }

        public bool BeginMission(MissionData mission)
        {
            if (mission == null) throw new ArgumentNullException(nameof(mission));
            if (State != MissionState.Idle)
            {
                Debug.LogWarning($"[Mission] Cannot begin '{mission.id}' while '{CurrentMission?.id}' is {State}; call Exit() first.");
                return false;
            }

            _objectives.Clear();
            foreach (ObjectiveData data in mission.objectives)
            {
                if (data == null) continue;
                if (_objectiveFactory.TryCreate(data, out IObjective objective)) _objectives.Add(objective);
                else Debug.LogError($"[Mission] '{mission.id}': unknown objective type '{data.type}' on objective '{data.id}'; it was skipped.");
            }

            CurrentMission = mission;
            ElapsedSeconds = 0f;
            LastResult = null;
            return TryTransition(MissionState.Briefing);
        }

        public bool Launch()
        {
            if (State != MissionState.Briefing) return false;

            TryTransition(MissionState.Running);
            StartTracking();
            EvaluateObjectives();
            return true;
        }

        public bool Pause()
        {
            return State == MissionState.Running && TryTransition(MissionState.Paused);
        }

        public bool Resume()
        {
            if (State != MissionState.Paused) return false;

            TryTransition(MissionState.Running);
            EvaluateObjectives();
            return true;
        }

        public bool Complete()
        {
            if (State != MissionState.Running && State != MissionState.Paused) return false;

            End(succeeded: true, failureReason: null);
            return true;
        }

        public bool Fail(string reason)
        {
            if (State != MissionState.Running && State != MissionState.Paused) return false;

            End(succeeded: false, failureReason: string.IsNullOrEmpty(reason) ? "Mission failed" : reason);
            return true;
        }

        public void Exit()
        {
            if (State == MissionState.Idle) return;

            StopTracking();
            TryTransition(MissionState.Idle);
            CurrentMission = null;
            _objectives.Clear();
            ElapsedSeconds = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (State != MissionState.Running || deltaTime <= 0f) return;

            ElapsedSeconds += deltaTime;
            for (int i = 0; i < _objectives.Count && State == MissionState.Running; i++)
            {
                _objectives[i].Tick(deltaTime);
            }

            if (State == MissionState.Running && CurrentMission.timeLimitSeconds > 0f && ElapsedSeconds >= CurrentMission.timeLimitSeconds)
            {
                Fail("Time limit reached");
            }
        }

        private void End(bool succeeded, string failureReason)
        {
            StopTracking();

            int optionalTotal = 0;
            int optionalCompleted = 0;
            foreach (IObjective objective in _objectives)
            {
                if (!objective.IsOptional) continue;
                optionalTotal++;
                if (objective.Status == ObjectiveStatus.Completed) optionalCompleted++;
            }

            var result = new MissionResult(CurrentMission.id, succeeded, ElapsedSeconds, optionalCompleted, optionalTotal, failureReason);
            LastResult = result;
            TryTransition(succeeded ? MissionState.Completed : MissionState.Failed);
            _events.Publish(new MissionEndedEvent(result));
        }

        private void StartTracking()
        {
            if (_tracking) return;

            _tracking = true;
            _events.Subscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
            foreach (IObjective objective in _objectives)
            {
                objective.Changed += OnObjectiveChanged;
                objective.Activate(_events);
            }
        }

        private void StopTracking()
        {
            if (!_tracking) return;

            _tracking = false;
            _events.Unsubscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
            foreach (IObjective objective in _objectives)
            {
                objective.Changed -= OnObjectiveChanged;
                objective.Deactivate();
            }
        }

        private void OnPlayerDefeated(PlayerDefeatedEvent evt)
        {
            Fail(string.IsNullOrEmpty(evt.Reason) ? "Fleet destroyed" : evt.Reason);
        }

        private void OnObjectiveChanged(IObjective objective)
        {
            _events.Publish(new ObjectiveUpdatedEvent(CurrentMission.id, objective.Id, objective.Status, objective.Progress01));
            EvaluateObjectives();
        }

        private void EvaluateObjectives()
        {
            if (State != MissionState.Running) return;

            int required = 0;
            int completed = 0;
            foreach (IObjective objective in _objectives)
            {
                if (objective.IsOptional) continue;

                required++;
                if (objective.Status == ObjectiveStatus.Failed)
                {
                    Fail($"Objective failed: {objective.Description}");
                    return;
                }
                if (objective.Status == ObjectiveStatus.Completed) completed++;
            }

            if (required > 0 && completed == required) Complete();
        }

        private bool TryTransition(MissionState next)
        {
            if (!IsAllowed(State, next))
            {
                Debug.LogWarning($"[Mission] Ignored invalid transition {State} -> {next}.");
                return false;
            }

            MissionState previous = State;
            State = next;
            _events.Publish(new MissionStateChangedEvent(CurrentMission?.id, previous, next));
            return true;
        }

        private static bool IsAllowed(MissionState from, MissionState to)
        {
            switch (to)
            {
                case MissionState.Idle:
                    return from != MissionState.Idle;
                case MissionState.Briefing:
                    return from == MissionState.Idle;
                case MissionState.Running:
                    return from == MissionState.Briefing || from == MissionState.Paused;
                case MissionState.Paused:
                    return from == MissionState.Running;
                case MissionState.Completed:
                case MissionState.Failed:
                    return from == MissionState.Running || from == MissionState.Paused;
                default:
                    return false;
            }
        }
    }
}
