using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    public AudioSource sfxSource;
    public AudioClip click, correct, wrong, button, clear;

    private void Awake() {
        sfxSource = GetComponent<AudioSource>();

    }
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        if (PlayerPrefs.HasKey("SFXVolume")) {
            ChangeSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySound(string name) {
        switch (name) {
            case "click":
                sfxSource.PlayOneShot(click);
                break;
            case "correct":
                sfxSource.PlayOneShot(correct);
                break;
            case "wrong":
                sfxSource.PlayOneShot(wrong);
                break;
            case "button":
                sfxSource.PlayOneShot(button);
                break;
            case "clear":
                sfxSource.PlayOneShot(clear);
                break;
            default:
                break;
                
        }
    }

    public void ChangeSFXVolume(float vol) {
        sfxSource.volume = vol / 2;
    }
}
