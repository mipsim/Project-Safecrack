using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    public AudioSource sfxSource;
    public AudioClip click, correct, wrong, button, clear;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        sfxSource = GetComponent<AudioSource>();
        ChangeSFXVolume(sfxSource.volume);
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
