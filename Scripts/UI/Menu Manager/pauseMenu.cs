using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class pauseMenu : MonoBehaviour
{

    MenuControl menuControl;

    float nextClick = 0f;
    private void Awake()
    {
        menuControl = GetComponent<MenuControl>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && nextClick < Time.time)
        {
            nextClick = Time.time + 0.5f;
            menuControl.ToggleMenu("Pause");
            
        }

        

        if (menuControl.GetOpenMenu().title != "...No Menu...") 
        {
            Time.timeScale = 0f;

        }
        else
        {
            Time.timeScale = 1f;

        }
        
    }

    

    public void Resume() {
        menuControl.RemoveMenu("Pause");
        Time.timeScale = 1f;
    }

    public void MainMenu(string sceneID) {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneID);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
