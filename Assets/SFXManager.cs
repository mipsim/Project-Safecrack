using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    public AudioSource sfxSource;
    public AudioClip click;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        sfxSource = GetComponent<AudioSource>();
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
            default:
                break;
                
        }
    }
}
