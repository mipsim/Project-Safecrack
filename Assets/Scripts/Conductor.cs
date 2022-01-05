using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conductor : MonoBehaviour
{
    // These first two might maybe prob should be converted to a new obj type containing both as right now you need to manually input
    //Song beats per minute
    //This is determined by the song you're trying to sync up to
    public float songBpm;

    public float timeSig; // should be a tuple, but we only use numerator anyways (ie. time sig 4/4)

    public float measureLength;

    //The number of seconds for each song beat
    public float milliSecPerBeat;

    //Current song position, in milliseconds
    public float songPosition;

    //Current song position, in beats
    //public float songPositionInBeats;

    //How many seconds have passed since the song started
    public float dspSongTime;

    //an AudioSource attached to this GameObject that will play the music.
    public AudioSource musicSource;

    // the beat position that the lock clicks at
    public float clickBeatPosition;

    // the target beat that you should hit space on (1 measure later)
    public float targetBeatPosition;

    public LockSpinWhee lockSpin;

    public bool clickPlayed = false;

    public float invokeDelay = 0f;

    // Start is called before the first frame update
    void Start() {
        //Load the AudioSource attached to the Conductor GameObject
        musicSource = GetComponent<AudioSource>();

        //Calculate the number of seconds in each beat
        milliSecPerBeat = 60000f / songBpm;

        // Measure length in milliseconds (60 seconds per minute / measures per minute) --> measures per minute is bpm / timeSig
        measureLength = 60000f / (songBpm / timeSig);

    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            // Start the song with space
            if (!musicSource.isPlaying) {
                // Record the time when the music starts
                dspSongTime = (float)AudioSettings.dspTime;

                // Start the music
                musicSource.Play();

                // Start spinning lock
                lockSpin.rotationSpeed = 100f;

                // 2 seconds later, determine click timing
                Invoke("DetermineClick", invokeDelay);
            }
            else {
                if (targetBeatPosition != 0f) {
                    // If pressed at right timing
                    if (Mathf.Abs(targetBeatPosition - songPosition) < milliSecPerBeat / 2) {
                        Debug.Log("good");
                        // spin other direction
                        lockSpin.rotationSpeed = -lockSpin.rotationSpeed;
                        SFXManager.instance.PlaySound("correct");
                    }
                    else {
                        Debug.Log("ur bad");
                        SFXManager.instance.PlaySound("wrong");

                    }
                    Invoke("DetermineClick", invokeDelay);
                    Debug.Log("target: " + targetBeatPosition + " what u got: " + songPosition);
                }
                
            }
        }

        if (musicSource.isPlaying) {
            // determine how many seconds since the song started
            songPosition = (float)(AudioSettings.dspTime - dspSongTime) * 1000f;

            // determine how many beats since the song started
            //songPositionInBeats = songPosition / secPerBeat;

            // when we get to the randomly generated beat position, play the click
            if (Mathf.Abs(clickBeatPosition - songPosition) < milliSecPerBeat / 2 && targetBeatPosition != 0 && !clickPlayed) {
                clickPlayed = true;
                SFXManager.instance.PlaySound("click");
            }
        }
        
    }

    // Chooses random beat to play a click within a time window
    public void DetermineClick() {
        // Chooses a position in the song between current pos and the next x amount of beats (in this case 24), with a delay 
        //clickBeatPosition = (int)Random.Range(songPositionInBeats + (songBpm / 8f), songPositionInBeats + 24 + (songBpm / 8f));
        clickBeatPosition = (int)(songPosition + 12f * milliSecPerBeat);

        // should be one measure after the randomBeat
        targetBeatPosition = clickBeatPosition + measureLength;

        clickPlayed = false;
    }
}
