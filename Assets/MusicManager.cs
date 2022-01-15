using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource musicSource;
    // Start is called before the first frame update
    private void Awake() {
        musicSource = GetComponent<AudioSource>();

    }
    void Start()
    {
        if (PlayerPrefs.HasKey("BGMVolume")) {
            ChangeMusicVolume(PlayerPrefs.GetFloat("BGMVolume"));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMusicVolume(float vol) {
        musicSource.volume = vol;
    }
}
