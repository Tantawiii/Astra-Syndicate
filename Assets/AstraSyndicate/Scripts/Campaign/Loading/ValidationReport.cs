using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>Collects content problems so that loading never throws on bad data.</summary>
    public sealed class ValidationReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        private readonly List<string> _notes = new List<string>();

        public IReadOnlyList<string> Errors => _errors;

        public IReadOnlyList<string> Warnings => _warnings;

        public IReadOnlyList<string> Notes => _notes;

        public bool HasErrors => _errors.Count > 0;

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }

        public void AddNote(string message)
        {
            _notes.Add(message);
        }

        public void Merge(ValidationReport other)
        {
            if (other == null || other == this) return;

            _errors.AddRange(other._errors);
            _warnings.AddRange(other._warnings);
            _notes.AddRange(other._notes);
        }

        public void Log(string prefix, Object context = null)
        {
            foreach (string message in _errors) Debug.LogError($"{prefix} {message}", context);
            foreach (string message in _warnings) Debug.LogWarning($"{prefix} {message}", context);
            foreach (string message in _notes) Debug.Log($"{prefix} {message}", context);
        }
    }
}
