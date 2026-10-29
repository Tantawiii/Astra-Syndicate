using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>A chapter authored in the editor. Wraps the same <see cref="ChapterData"/> schema as JSON chapters.</summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/Campaign/Chapter Config", fileName = "Chapter")]
    public sealed class ChapterConfig : ScriptableObject
    {
        [SerializeField] private ChapterData chapter = new ChapterData();

        /// <summary>The asset's data. Runtime code receives copies through ScriptableObjectChapterSource.</summary>
        public ChapterData Chapter => chapter;
    }
}
