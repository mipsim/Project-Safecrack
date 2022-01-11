using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBehavior : MonoBehaviour
{
    public static MainMenuBehavior instance;

    public GameObject TutorialPage;

    private AudioSource buttonClick;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != this) {
            instance = this;
        }

        buttonClick = GetComponent<AudioSource>();
    }

    public void GoToGameplay() {
        SceneManager.LoadScene(0);
    }

    public void GoToCredits()
    {
        SceneManager.LoadScene(2);
    }

    public void OpenTutorial()
    {
        TutorialPage.SetActive(true);
    }

    public void CloseTutorial()
    {
        TutorialPage.SetActive(false);
    }

    public void ExitGame() { 
        Application.Quit();
    }

    public void PrintName(GameObject buttonObj) {
        Debug.Log(buttonObj.name);
    }

    public void PlayButtonSound() { 
        buttonClick.Play();
    }
}
