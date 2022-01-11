using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIScript : MonoBehaviour
{
    public GameObject settingsCanvas;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)){
            if (settingsCanvas.activeSelf) {
                Conductor.instance.lockSpin.rotationSpeed = 100f;
                Conductor.instance.musicSource.UnPause();
            }
            settingsCanvas.SetActive(!settingsCanvas.activeSelf);
            
        }
            //if (!Conductor.instance.musicSource.isPlaying) {
            //    if (Input.GetKeyDown(KeyCode.Q)) {
            //        if (songListPosition > 0) {
            //            songListPosition--;
            //            currentSong = songList[songListPosition];
            //            SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
            //        }
            //    }
            //    if (Input.GetKeyDown(KeyCode.E)) {
            //        if (songListPosition < songList.Count) {
            //            songListPosition++;
            //            currentSong = songList[songListPosition];
            //            SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
            //        }
            //    }
            //}
    }

    
}
