using UnityEngine;

namespace WordWars.Models
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "AIWordWars/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public enum EnvironmentType { Local, Production }

        [Header("Environment Configuration")]
        [Tooltip("Switch between Local and Production to change all API URLs globally.")]
        public EnvironmentType currentEnvironment = EnvironmentType.Local;

        [Header("Local URLs")]
        public string localApiUrl = "http://127.0.0.1:8000";
        public string localWsUrl = "ws://127.0.0.1:8007";

        [Header("Production URLs (Cloud Run)")]
        public string productionApiUrl = "https://your-cloud-run-gateway-url.run.app";
        public string productionWsUrl = "wss://your-cloud-run-multiplayer-url.run.app";

        private static AppConfig _instance;
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Load the AppConfig asset from the Resources folder
                    _instance = Resources.Load<AppConfig>("AppConfig");
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning("AppConfig asset not found in a Resources folder! Creating a temporary default instance. To use the Editor dropdown, create an AppConfig asset in a Resources folder via Right Click -> Create -> AIWordWars -> AppConfig.");
                        _instance = CreateInstance<AppConfig>();
                    }
                }
                return _instance;
            }
        }

        // Helper properties to get the active URLs
        public string ApiUrl => currentEnvironment == EnvironmentType.Local ? localApiUrl : productionApiUrl;
        public string WsUrl => currentEnvironment == EnvironmentType.Local ? localWsUrl : productionWsUrl;
    }
}