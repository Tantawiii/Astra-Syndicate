using System.Collections.Generic;
using System.Linq;

namespace AstraSyndicate.Campaign
{
    public sealed class ScriptableObjectChapterSource : IChapterSource
    {
        private readonly List<ChapterConfig> _configs;

        public ScriptableObjectChapterSource(IEnumerable<ChapterConfig> configs)
        {
            _configs = configs?.ToList() ?? new List<ChapterConfig>();
        }

        public string SourceName => "ChapterConfig assets";

        public IReadOnlyList<ChapterData> LoadChapters(ValidationReport report)
        {
            var chapters = new List<ChapterData>(_configs.Count);
            foreach (ChapterConfig config in _configs)
            {
                if (config == null)
                {
                    report.AddWarning("The campaign catalog has an empty chapter slot.");
                    continue;
                }

                // Hand out a copy so gameplay code can never modify the asset on disk.
                chapters.Add(ChapterDataUtility.Normalize(ChapterDataUtility.Clone(config.Chapter)));
            }
            return chapters;
        }
    }
}
