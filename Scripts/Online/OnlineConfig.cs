using UnityEngine;

public class OnlineConfig
{
    public string apiKey = "";
    public string authDomain = "";
    public string projectId = "ponex";
    public string bucket = "ponex.firebasestorage.app";
    public string googleClientId = "";
    public string googleClientSecret = "";
    public string relayUrl = "";

    public static OnlineConfig Load()
    {
        OnlineConfig config = new OnlineConfig();
        TextAsset file = Resources.Load<TextAsset>("OnlineConfig");
        if (file != null && !string.IsNullOrEmpty(file.text))
            JsonUtility.FromJsonOverwrite(file.text, config);
        return config;
    }

    public bool HasFirebase => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(projectId);
    public bool HasGoogle => HasFirebase && !string.IsNullOrEmpty(googleClientId);
    public bool HasRelay => !string.IsNullOrEmpty(relayUrl);
}
