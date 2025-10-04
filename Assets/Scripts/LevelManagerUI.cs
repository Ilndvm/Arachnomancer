using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManagerUI : MonoBehaviour
{

    [SerializeField] private GameObject playerUI;
    [SerializeField] private GameObject pauseUI;


    private void Awake()
    {
        playerUI.SetActive(true);
        pauseUI.SetActive(false);
    }

    private void Update()
    {
        if (!pauseUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPauseUI();
        }
    }

    public void OpenPauseUI()
    {
        playerUI.SetActive(false);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseUI.SetActive(true);
    }

    public void ClosePauseUI()
    {
        pauseUI.SetActive(false);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerUI.SetActive(true);
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene(0);
    }

}
