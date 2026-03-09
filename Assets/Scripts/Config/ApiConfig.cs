using UnityEngine;

/// <summary>
/// ScriptableObject storing API configuration.
/// Create an instance via Assets > Create > Config > API Config.
/// </summary>
[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/API Config")]
public class ApiConfig : ScriptableObject
{
    [Header("Mapbox")]
    [SerializeField] private string mapboxAccessToken;

    [Header("OpenAI")]
    [SerializeField] private string openAiApiKey;
    [SerializeField] private string baseUrl = "https://api.openai.com/v1";

    /// <summary>Returns the Mapbox access token.</summary>
    public string MapboxAccessToken => mapboxAccessToken;

    /// <summary>Returns the OpenAI API key.</summary>
    public string OpenAiApiKey => openAiApiKey;

    /// <summary>Returns the base URL for OpenAI API requests.</summary>
    public string BaseUrl => baseUrl;
}
