using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject gameMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Canvas upgradeMenu;
    [SerializeField] private TextMeshProUGUI bloodText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI waveTextP;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI timerTextP;
    [SerializeField] private TextMeshProUGUI stacksText;
    [SerializeField] private TextMeshProUGUI uniqueText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    private void Awake()
    {
        // defensive
        gameMenu.SetActive(true);
        pauseMenu.SetActive(false);
        upgradeMenu.enabled = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (upgradeMenu.enabled)
            {
                CloseUpgradeMenu();
            }
            else if (pauseMenu.activeSelf)
            {
                ClosePauseMenu();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (upgradeMenu.enabled)
            {
                CloseUpgradeMenu();
            }
            else
            {
                OpenpUpgradeMenu();
            }
        }
    }

    public void OpenpUpgradeMenu()
    {
        gameMenu.SetActive(false);
        pauseMenu.SetActive(false);
        upgradeMenu.enabled = true;
        WebDrawCoordinator.Instance.bloodDropsAmountText.text = GameManager.Instance.blood.ToString();


        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenPauseMenu()
    {
        gameMenu.SetActive(false);
        upgradeMenu.enabled = false;
        pauseMenu.SetActive(true);
        UpdateUpgrades();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePauseMenu()
    {
        pauseMenu.SetActive(false);
        upgradeMenu.enabled = false;
        gameMenu.SetActive(true);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // new close for upgrade menu (mirrors ClosePauseMenu behavior)
    public void CloseUpgradeMenu()
    {
        upgradeMenu.enabled = false;
        pauseMenu.SetActive(false);
        gameMenu.SetActive(true);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoBackToMenu()
    {
        AudioManager.Instance.PlaySound(AudioManager.Sound.ButtonClick);

        ManagerUI.Instance.TrySetNewTime(GameManager.Instance.timer);

        // ensure timeScale is restored when leaving scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //AudioManager.Instance.ChangeMusic(0, 0.5f);
    }

    public void UpdateUpgrades()
    {
        stacksText.text = UpgradeManager.Instance.GetStackableUpgradesString();
        uniqueText.text = UpgradeManager.Instance.GetUniqueUpgradesString();
    }

    public void UpdateBloodText()
    {
        bloodText.text = "" + GameManager.Instance.blood;
    }
    public void UpdateWaveText(int currentWave)
    {
        waveText.text = "Wave: " + currentWave;
        waveTextP.text = "Wave: " + currentWave;
    }
    public void UpdateTimerText(int seconds)
    {
        if (seconds < 0) seconds = 0;

        int minutes = seconds / 60;
        int secs = seconds % 60;

        timerText.text = $"{minutes}:{secs:00}";
        timerTextP.text = $"{minutes}:{secs:00}";
    }
    public void UpdateHPSlider()
    {
        if (hpSlider == null) return;

        // Guard against invalid max
        if (GameManager.Instance.player.maxHP <= 0f)
        {
            hpSlider.maxValue = 1f;
            hpSlider.value = 0f;
            return;
        }

        // Ensure slider max matches maxHP
        if (!Mathf.Approximately(hpSlider.maxValue, GameManager.Instance.player.maxHP))
            hpSlider.maxValue = GameManager.Instance.player.maxHP;

        // Clamp current and set value
        float clamped = Mathf.Clamp(GameManager.Instance.player.currentHP, 0f, GameManager.Instance.player.maxHP);
        hpSlider.value = clamped;
        hpText.text = (int)GameManager.Instance.player.currentHP + "/" + GameManager.Instance.player.maxHP;
    }
}