using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>Reads chapters from JSON text using the <see cref="ChapterData"/> schema.</summary>
    public sealed class JsonChapterSource : IChapterSource
    {
        private readonly List<Document> _documents;

        public JsonChapterSource(IEnumerable<Document> documents)
        {
            _documents = documents?.ToList() ?? new List<Document>();
        }

        public string SourceName => "JSON chapters";

        public IReadOnlyList<ChapterData> LoadChapters(ValidationReport report)
        {
            var chapters = new List<ChapterData>(_documents.Count);
            foreach (Document document in _documents)
            {
                if (TryParse(document.Json, document.Name, out ChapterData chapter, out string error)) chapters.Add(chapter);
                else report.AddError(error);
            }
            return chapters;
        }

        public static bool TryParse(string json, string documentName, out ChapterData chapter, out string error)
        {
            chapter = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"{documentName}: the file is empty.";
                return false;
            }

            try
            {
                chapter = JsonUtility.FromJson<ChapterData>(json);
            }
            catch (Exception e)
            {
                error = $"{documentName}: invalid JSON ({e.Message}).";
                return false;
            }

            if (chapter == null)
            {
                error = $"{documentName}: no chapter could be read.";
                return false;
            }

            ChapterDataUtility.Normalize(chapter);
            if (chapter.id.Length == 0)
            {
                chapter = null;
                error = $"{documentName}: the chapter has no \"id\".";
                return false;
            }

            error = null;
            return true;
        }

        public readonly struct Document
        {
            public readonly string Name;
            public readonly string Json;

            public Document(string name, string json)
            {
                Name = name;
                Json = json;
            }
        }
    }
}
