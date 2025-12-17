using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;

    void Start()
    {
        menuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnStart()
    {
        menuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}