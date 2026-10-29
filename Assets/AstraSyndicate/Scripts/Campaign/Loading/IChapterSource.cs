using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>Provides chapter data from one kind of local content (assets, JSON files...).</summary>
    public interface IChapterSource
    {
        /// <summary>Human-readable name used in validation messages.</summary>
        string SourceName { get; }

        /// <summary>Loads every chapter this source provides. Bad content is reported, never thrown.</summary>
        IReadOnlyList<ChapterData> LoadChapters(ValidationReport report);
    }
}
