using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBehavior : MonoBehaviour
{
    public static MainMenuBehavior instance;

    public GameObject TutorialPage;

    private AudioSource buttonClick;

    private void Awake() {
        buttonClick = GetComponent<AudioSource>();

    }
    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow, 60);

        if (instance != this) {
            instance = this;
        }


        if (PlayerPrefs.HasKey("SFXVolume")) {
            ChangeSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        }
    }

    public void GoToGameplay() {
        SceneManager.LoadScene(1);
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

    public void ChangeSFXVolume(float vol) {
        buttonClick.volume = vol;
    }
}
