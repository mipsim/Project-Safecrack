using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public GameObject bgmSlider;
    public GameObject sfxSlider;
    public GameObject autofailToggle;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.GetFloat("AutoFail") == 1) {
            autofailToggle.GetComponent<Toggle>().isOn = true;
        }
        else {
            autofailToggle.GetComponent<Toggle>().isOn = false;
        }
        if (Conductor.instance != null) {
            Conductor.instance.autoFailToggle = autofailToggle.GetComponent<Toggle>().isOn;
        }

        if (PlayerPrefs.HasKey("BGMVolume")) {
            bgmSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("BGMVolume");
        }
        
        if (PlayerPrefs.HasKey("SFXVolume")) {
            sfxSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("SFXVolume");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleSettings() {
        if (Conductor.instance != null) {
            Conductor.instance.PauseGame();
        }
        settingsMenu.SetActive(!settingsMenu.activeSelf);
    }

    public void MainMenu() {
        SceneManager.LoadScene(0);
    }

    public void ExitGame() {
        Application.Quit();
    }

    public void AutoFailToggle(bool toggle) {
        if (Conductor.instance != null) {
            Conductor.instance.autoFailToggle = toggle;
        }
        if (toggle) {
            PlayerPrefs.SetFloat("AutoFail", 1);
        }
        else {
            PlayerPrefs.SetFloat("AutoFail", 0);
        }
    }

    public void SetBGMVolume(float vol) {
        PlayerPrefs.SetFloat("BGMVolume", vol);
    }
    
    public void SetSFXVolume(float vol) {
        PlayerPrefs.SetFloat("SFXVolume", vol);
    }
}
