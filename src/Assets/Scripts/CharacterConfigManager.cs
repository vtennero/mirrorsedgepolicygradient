using UnityEngine;

public class CharacterConfigManager : MonoBehaviour
{
    private static CharacterConfigManager _instance;
    public static CharacterConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CharacterConfigManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CharacterConfigManager");
                    _instance = go.AddComponent<CharacterConfigManager>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Character Configuration")]
    [Tooltip("Assign a CharacterConfig asset here, or leave null to use default values")]
    public CharacterConfig config;
    
    private CharacterConfig _defaultConfig;
    
    public static CharacterConfig Config
    {
        get
        {
            if (Instance.config != null)
                return Instance.config;
            
            if (Instance._defaultConfig == null)
            {
                Instance._defaultConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            }
            return Instance._defaultConfig;
        }
    }
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
