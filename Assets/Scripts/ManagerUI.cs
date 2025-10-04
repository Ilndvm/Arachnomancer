using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerUI : MonoBehaviour
{

    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject creditsUI;

    private void Awake()
    {
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



}
