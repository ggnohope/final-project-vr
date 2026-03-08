using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that provides global access to ApiConfig.
/// Attach this to a persistent GameObject in your main scene.
/// </summary>
public class ApiConfigProvider : MonoBehaviour
{
    private const string ResourcePath = "Config/ApiConfig";

    [SerializeField] private ApiConfig apiConfig;

    public static ApiConfigProvider Instance { get; private set; }

    /// <summary>Returns the loaded ApiConfig asset.</summary>
    public ApiConfig Config => apiConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (apiConfig == null)
        {
            // Fallback: load from Resources/Config/ApiConfig.asset
            apiConfig = Resources.Load<ApiConfig>(ResourcePath);

            if (apiConfig == null)
            {
                Debug.LogError($"[ApiConfigProvider] ApiConfig not found at Resources/{ResourcePath}. " +
                               "Please create and assign one.");
            }
        }
    }
}
