using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Lists every chapter of the offline campaign. Chapters can be ChapterConfig assets or JSON files;
    /// a JSON chapter replaces an asset chapter with the same id, so content can be tweaked as text
    /// without touching the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/Campaign/Campaign Catalog", fileName = "CampaignCatalog")]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [Tooltip("Chapters authored as ChapterConfig assets.")]
        [SerializeField] private List<ChapterConfig> chapterAssets = new List<ChapterConfig>();

        [Tooltip("Chapters authored as JSON (same schema as ChapterConfig). JSON wins over an asset with the same chapter id.")]
        [SerializeField] private List<TextAsset> jsonChapters = new List<TextAsset>();

        /// <summary>Creates the chapter sources in priority order: later sources override earlier ones.</summary>
        public IReadOnlyList<IChapterSource> CreateSources()
        {
            var documents = new List<JsonChapterSource.Document>();
            foreach (TextAsset asset in jsonChapters)
            {
                if (asset != null) documents.Add(new JsonChapterSource.Document(asset.name, asset.text));
            }

            return new IChapterSource[]
            {
                new ScriptableObjectChapterSource(chapterAssets),
                new JsonChapterSource(documents),
            };
        }
    }
}
