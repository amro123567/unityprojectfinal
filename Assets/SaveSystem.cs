using UnityEngine;

// Attach this to a persistent GameObject in your first scene
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    [System.Serializable]
    public class GameData
    {
        public float playerHealth;
        public int currentLevel;
        public int score;
        public bool hasDoubleJump;      // example unlocked ability
        public float playerX;
        public float playerY;
    }

    private GameData gameData;
    private const string SAVE_KEY = "SaveData";

    void Awake()
    {
        // Singleton — persists across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadGame();
    }

    public void SaveGame(Transform playerTransform, float health, int level, int score, bool doubleJump)
    {
        gameData.playerHealth   = health;
        gameData.currentLevel   = level;
        gameData.score          = score;
        gameData.hasDoubleJump  = doubleJump;
        gameData.playerX        = playerTransform.position.x;
        gameData.playerY        = playerTransform.position.y;

        string json = JsonUtility.ToJson(gameData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("Game Saved: " + json);
    }

    public GameData LoadGame()
    {
        gameData = new GameData();

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            gameData = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Game Loaded: " + json);
        }
        else
        {
            // Default values for new game
            gameData.playerHealth  = 100f;
            gameData.currentLevel  = 1;
            gameData.score         = 0;
            gameData.hasDoubleJump = false;
        }

        return gameData;
    }

    public GameData GetData() => gameData;

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("Save deleted.");
    }
}
