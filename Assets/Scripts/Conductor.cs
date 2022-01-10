using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Conductor : MonoBehaviour {
    // These first two might maybe prob should be converted to a new obj type containing both as right now you need to manually input
    //Song beats per minute
    //This is determined by the song you're trying to sync up to
    public float songBpm;

    public float timeSig; // should be a tuple, but we only use numerator anyways (ie. time sig 4/4)

    public float measureLength;

    public string songName;

    //The number of seconds for each song beat
    public float msPerBeat;

    //Current song position, in milliseconds
    public float songPosition;
    public int beatmapPosition;
    //Current song position, in beats
    //public float songPositionInBeats;

    //How many seconds have passed since the song started
    public float dspSongTime;

    //an AudioSource attached to this GameObject that will play the music.
    public AudioSource musicSource;

    // the beat position that the lock clicks at
    public float clickBeatPosition;
    public float beatmapClickPosition;

    // the target beat that you should hit space on (1 measure later)
    public float targetBeatPosition;
    public float targetSongPosition;
    public float previousTargetSongPosition;

    public GameObject leftBar;
    public GameObject rightBar;
    private float leftStartX;
    private float leftMidpointX;
    private float rightStartX;
    private float rightMidpointX;

    public GameObject locke;
    private LockSpinWhee lockSpin;
    public MeasureTracker measureTracker;

    public bool clickPlayed = false;
    public bool responded = false;

    public Dictionary<int, int> lockNumbers = new Dictionary<int, int>();
    public GameObject[] numberSlots;

    public TextMeshPro currentlyPlaying;
    public TextMeshPro currentBPM;
    public List<SongData> songList;
    public SongData currentSong;
    public int songListPosition;

    public TextMeshProUGUI comboText;
    public TextMeshProUGUI highscoreText;
    public GameObject songTime;
    public int score;
    public int highscore;
    public int measureMultiplier = 1;

    // Start is called before the first frame update
    void Start() {
        //Load the AudioSource attached to the Conductor GameObject
        musicSource = GetComponent<AudioSource>();
        lockSpin = locke.GetComponent<LockSpinWhee>();

        // record bar starting positions/midpoints
        leftStartX = leftBar.transform.position.x;
        rightStartX = rightBar.transform.position.x;
        leftMidpointX = (leftStartX + locke.transform.position.x) / 2;
        rightMidpointX = (rightStartX + locke.transform.position.x) / 2;


        // Initialize dictionary with 16 0's
        for (int i = 0; i < 16; i++) {
            lockNumbers.Add(i, 0);
        }

        // Set current song to song 0 and assign/calculate song data
        currentSong = songList[0];
        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
    }

    // Update is called once per frame
    void Update() {
        // Update score and highscore text
        comboText.text = "COMBO: " + score;
        highscoreText.text = "HIGHSCORE: " + highscore;

        // Use Q/E to swap between songs
        if (!musicSource.isPlaying) {
            if (Input.GetKeyDown(KeyCode.Q)) {
                if (songListPosition > 0) {
                    songListPosition--;
                    currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                }
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                if (songListPosition < songList.Count) {
                    songListPosition++;
                    currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                }
            }
        }

        // Use ESC to stop the song
        if (Input.GetKeyDown(KeyCode.Escape) && musicSource.isPlaying) {
            musicSource.Stop();
            lockSpin.rotationSpeed = 0f;
            StopAllCoroutines();
            responded = true;
            StartCoroutine("ReturnSnap");
            measureTracker.moving = false;
            beatmapPosition = 0;
            foreach (GameObject slot in numberSlots) {
                slot.SetActive(false);
            }
            SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
            songTime.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            // Start the song with space
            if (!musicSource.isPlaying) {
                songTime.SetActive(false);

                // Record the time when the music starts
                dspSongTime = (float)AudioSettings.dspTime;

                // Start the music
                musicSource.Play();

                // Start spinning lock
                lockSpin.rotationSpeed = 100f;
                responded = false;
                clickPlayed = false;
                measureTracker.moving = true;
                DetermineClick();

            }
            else {
                // If target beat determined and not yet clicked
                if (clickPlayed && !responded) {
                    //Debug.Log("Target: " + targetSongPosition + " Clicked at: " + songPosition);
                    

                    // If pressed at right timing (or earlier)
                    if (Mathf.Abs(targetSongPosition - songPosition) < msPerBeat){// / 2) {
                        // spin other direction
                        lockSpin.rotationSpeed = -lockSpin.rotationSpeed;
                        SFXManager.instance.PlaySound("correct");
                        int hitNote = (int)(songPosition % measureLength);

                        for (int i = 0; i < 15; i++) {
                            if (hitNote > lockNumbers[i] && hitNote < (int)lockNumbers[i + 1]) {
                                DisplayNumber(i);
                                break;
                            }
                        }
                        score++;
                        if (highscore < score) {
                            highscore = score;
                        }
                    }
                    else if (songPosition > previousTargetSongPosition && Mathf.Abs(previousTargetSongPosition - songPosition) < msPerBeat/2){
                        // spin other direction
                        lockSpin.rotationSpeed = -lockSpin.rotationSpeed;
                        SFXManager.instance.PlaySound("correct");
                        int hitNote = (int)(songPosition % measureLength);

                        for (int i = 0; i < 15; i++) {
                            if (hitNote > lockNumbers[i] && hitNote < (int)lockNumbers[i + 1]) {
                                DisplayNumber(i);
                                break;
                            }
                        }
                        score++;
                        if (highscore < score) {
                            highscore = score;
                        }
                    }
                    else {
                        SFXManager.instance.PlaySound("wrong");
                        score = 0;
                    }
                    // determine new click time and snap to middle then back
                    StartCoroutine("SecondSnap");
                    DetermineClick();
                }
            }
        }

        if (musicSource.isPlaying) {
            // determine how many seconds since the song started
            songPosition = (float)(AudioSettings.dspTime - dspSongTime) * 1000f;

            // when we get to the randomly generated beat position, play the click
            if (Mathf.Abs(targetSongPosition - songPosition - measureMultiplier * measureLength) < msPerBeat / 2 && targetBeatPosition != 0 && !clickPlayed) {
                StartCoroutine(FirstSnap());
            }

            // if the song position passes your click position and you don't click, determine a new click and then snap to middle and snap back
            if (songPosition > targetSongPosition && !responded && clickPlayed) {
                DetermineClick();
                StartCoroutine("ResponseTwitch");
                //SFXManager.instance.PlaySound("wrong");
                //score = 0;
            }
        }

    }

    public void SwitchSongs(AudioClip clip, string _name, int bpm, int sig) {
        musicSource.clip = clip;
        songName = _name;
        songBpm = bpm;
        timeSig = sig;

        currentlyPlaying.text = "Currently playing: " + songName;
        currentBPM.text = "BPM: " + songBpm;

        score = 0;
        clickBeatPosition = 0;
        beatmapPosition = 0;
        beatmapClickPosition = 0;
        targetBeatPosition = 0;
        targetSongPosition = 0;

        if (currentSong.GetComponent<Beatmap>()) {
            measureMultiplier = 2;
        }
        else {
            measureMultiplier = 1;
        }
        CalculateSongInfo();
    }

    // Chooses random beat to play a click within a time window
    public void DetermineClick() {
        StartCoroutine("ClickPlayed");
        if (songPosition >= clickBeatPosition || targetSongPosition == 0) {
            // Detects if there's a beatmap component and then proceeeds to click on those only
            if (songList[songListPosition].GetComponent<Beatmap>()) {
                var beatmap = songList[songListPosition].GetComponent<Beatmap>();
                if (beatmapPosition >= 10) {
                    measureMultiplier = 1;
                }
                beatmapClickPosition = (beatmap.measureList[beatmapPosition] - 1) * measureLength + beatmap.beatList[beatmapPosition] * msPerBeat/2;
                targetBeatPosition = beatmapClickPosition + measureMultiplier * measureLength; // beatmap specific multiplier
                beatmapPosition++;
                previousTargetSongPosition = targetSongPosition;
                targetSongPosition = targetBeatPosition;

            }
            else {
                clickBeatPosition = (int)(((songPosition % msPerBeat) + measureLength) + (int)Random.Range(0, timeSig) * msPerBeat);
                targetBeatPosition = clickBeatPosition + measureMultiplier * measureLength; // beatmap specific multiplier
                previousTargetSongPosition = targetSongPosition;
                targetSongPosition = songPosition + targetBeatPosition;
            }


        }

    }

    public IEnumerator ClickPlayed() {
        yield return new WaitForSeconds(msPerBeat/1000);
        clickPlayed = false;
        responded = false;

    }

    // bpm: 100
    // 1 measure is 4 beats
    // ms/beat: 600
    // 1 measure: 2400 ms

    public void CalculateSongInfo() {
        //Calculate the number of seconds in each beat
        msPerBeat = 60000f / songBpm;

        // Measure length in milliseconds (60 seconds per minute / measures per minute) --> measures per minute is bpm / timeSig
        measureLength = 60000f / (songBpm / timeSig);

        for (int i = 0; i < 16; i++) {
            lockNumbers[i] =  (int)((i / 16f) * measureLength);
        }
    }

    public void DisplayNumber(int hit) {
        foreach (GameObject slot in numberSlots) {
            if (!slot.activeSelf) {
                slot.SetActive(true);
                slot.GetComponent<TextMeshProUGUI>().text = "" + hit;
                break;
            }
        }
    }

    public IEnumerator FirstSnap() {
        clickPlayed = true;
        SFXManager.instance.PlaySound("click");
        while (leftBar.transform.position.x != leftMidpointX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftMidpointX, 0), 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightMidpointX, 0), 0.5f);
        }
        yield return null;
    }

    public IEnumerator SecondSnap() {
        responded = true;
        while (leftBar.transform.position.x != locke.transform.position.x) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, locke.transform.position, 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, locke.transform.position, 0.5f);
        }
        StartCoroutine("ReturnSnap");
        yield return null;
    }

    public IEnumerator ResponseTwitch() {
        while (leftBar.transform.position.x != locke.transform.position.x) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, locke.transform.position, 0.05f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, locke.transform.position, 0.05f);
        }
        StartCoroutine("ReturnSnap");
        yield return null;
    }

    public IEnumerator ReturnSnap() {
        yield return new WaitForSeconds(0.5f);
        while (leftBar.transform.position.x != leftStartX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftStartX, 0), 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightStartX, 0), 0.5f);
        }
        measureTracker.leftTracker.transform.position = new Vector2(measureTracker.leftStart.x, measureTracker.leftStart.y);
        measureTracker.rightTracker.transform.position = new Vector2(measureTracker.rightStart.x, measureTracker.rightStart.y);
        yield return null;
    }

    

    public void ResetHighscore() {
        highscore = 0;
    }

    public void ChangeMusicVolume(float vol) {
        musicSource.volume = vol;
    }
}
