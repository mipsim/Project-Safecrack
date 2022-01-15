using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuBehavior : MonoBehaviour
{
    public static MainMenuBehavior instance;

    public GameObject TutorialPage;

    private AudioSource buttonClick;

    public GameObject[] tutorialPages;
    public int pageCount;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public GameObject advanceToGame;
    public GameObject exitTutorialButton;
    public bool fromStart;

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

    public void Update() {
        if (TutorialPage.activeSelf) {
            if (Input.GetKeyDown(KeyCode.Q) && leftArrow.activeSelf) {
                DecreasePage();
                PlayButtonSound();
            }
            else if (Input.GetKeyDown(KeyCode.E) && rightArrow.activeSelf) {
                IncreasePage();
                PlayButtonSound();
            }
        }
    }

    public void GoToGameplay() {
        if (PlayerPrefs.GetFloat("Tutorial") == 1) {
            SceneManager.LoadScene(1);
        }
        else {
            fromStart = true;
            OpenTutorial();
        }
    }

    public void GoToCredits()
    {
        SceneManager.LoadScene(2);
    }

    public void OpenTutorial()
    {
        TutorialPage.SetActive(true);
        tutorialPages[pageCount].SetActive(false);
        pageCount = 0;
        tutorialPages[pageCount].SetActive(true);
        leftArrow.SetActive(false);
        rightArrow.SetActive(true);

        if (fromStart) {
            exitTutorialButton.SetActive(false);
        }
        else {
            exitTutorialButton.SetActive(true);
        }
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

    public void DecreasePage() {
        tutorialPages[pageCount].SetActive(false);
        if (pageCount == 1) {
            leftArrow.SetActive(false);
        }
        else if (pageCount == tutorialPages.Length-1) {
            rightArrow.SetActive(true);
            advanceToGame.SetActive(false);
        }
        pageCount--;
        tutorialPages[pageCount].SetActive(true);
    }

    public void IncreasePage() {
        tutorialPages[pageCount].SetActive(false);

        if (pageCount == 0) {
            leftArrow.SetActive(true);
        }
        else if (pageCount == tutorialPages.Length-2) {
            rightArrow.SetActive(false);
            if (fromStart) {
                advanceToGame.SetActive(true);

            }
        }
        pageCount++;
        tutorialPages[pageCount].SetActive(true);

    }

    public void TutorialSeen() {
        PlayerPrefs.SetFloat("Tutorial", 1);
    }
}
