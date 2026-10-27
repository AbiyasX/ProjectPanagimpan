using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Pop-Ups")]
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject creditsUI;
    [SerializeField] private GameObject disclaimerUI;

    public void ShowDisclaimer()
    {
        disclaimerUI.SetActive(true);
    }

    public void HideDisclaimer()
    {
        disclaimerUI.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void ShowSettings()
    {
        settingsUI.SetActive(true);
    }

    public void HideSettings()
    {
        settingsUI.SetActive(false);
    }

    public void ShowCredits()
    {
        creditsUI.SetActive(true);
    }

    public void HideCredits()
    {
        creditsUI.SetActive(false);
    }
}
