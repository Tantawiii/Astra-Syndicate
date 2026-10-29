using System.IO;
using AstraSyndicate.Campaign;
using AstraSyndicate.Save;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>Project-wide settings read by GameBootstrapper from Resources/AstraSyndicate/GameConfig.</summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        /// <summary>Path passed to Resources.Load.</summary>
        public const string ResourcesPath = "AstraSyndicate/GameConfig";

        [SerializeField] private CampaignCatalog campaignCatalog;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField, Min(0f)] private float transitionFadeSeconds = 0.35f;

        [Tooltip("Encrypts and verifies save files. Leave empty during development to store readable JSON.")]
        [SerializeField] private SaveProtectorProvider saveProtector;

        [Tooltip("Folder inside Application.persistentDataPath that holds the save files.")]
        [SerializeField] private string saveFolderName = "saves";

        public CampaignCatalog Catalog => campaignCatalog;

        public string MainMenuScene => mainMenuScene;

        public float TransitionFadeSeconds => transitionFadeSeconds;

        public SaveProtectorProvider SaveProtector => saveProtector;

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, saveFolderName);
    }
}
