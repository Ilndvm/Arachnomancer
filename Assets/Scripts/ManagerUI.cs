using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerUI : MonoBehaviour
{
    public static ManagerUI Instance { get; private set; }

    private const string PlayerPrefsKey = "TopTime";
    [SerializeField] private TMP_Text tmpText = null;
    [SerializeField] private string noRecordText = "--:--";

    private float? topTimeSeconds = null; // null if none saved
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject creditsUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadTopTime();
        UpdateDisplay();

        mainUI.SetActive(true);
        settingsUI.SetActive(false);
        creditsUI.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenSettings()
    {
        mainUI.SetActive(false);
        settingsUI.SetActive(true);
    }

    public void OpenCredits()
    {
        mainUI.SetActive(false);
        creditsUI.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsUI.SetActive(false);
        mainUI.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsUI.SetActive(false);
        mainUI.SetActive(true);
    }

    
    // top time
    public bool TrySetNewTime(float timeSeconds)
    {
        if (timeSeconds < 0f) return false;

        if (!topTimeSeconds.HasValue)
        {
            Debug.Log("Time saved first time");

            SaveTopTime(timeSeconds);
            return true;
        }

        if (timeSeconds > topTimeSeconds.Value)
        {
            Debug.Log("Time saved");
            SaveTopTime(timeSeconds);
            return true;
        }

        return false;
    }

    private void LoadTopTime()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            topTimeSeconds = PlayerPrefs.GetFloat(PlayerPrefsKey);
        }
        else
        {
            topTimeSeconds = null;
        }
    }

    private void SaveTopTime(float seconds)
    {
        topTimeSeconds = seconds;
        PlayerPrefs.SetFloat(PlayerPrefsKey, seconds);
        PlayerPrefs.Save();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string text = GetTopTimeString();
        if (tmpText != null) tmpText.text = text;
    }
    public string GetTopTimeString()
    {
        if (!topTimeSeconds.HasValue) return noRecordText;
        return FormatTime(topTimeSeconds.Value);
    }
    private static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int secs = Mathf.FloorToInt(seconds) % 60;
        int mins = Mathf.FloorToInt(seconds) / 60;

        return $"{mins}:{secs:00}";
    }

}
