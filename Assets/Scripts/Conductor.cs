using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Conductor : MonoBehaviour {
    [Header("Song Information")]
    // These are determined by current song
    public string songName;
    public float songBpm;
    public float timeSig; // should be a tuple, but we only use numerator anyways (ie. time sig 4/4)
    public float measureLength;

    // The number of seconds for each song quarter beat
    public float msPerBeat;

    [Header("Game Information")]
    // Current song position, in milliseconds
    public float songPosition;

    // Current beatmap position
    public int beatmapPosition;

    // the beat position that the lock clicks at
    public float beatmapClickPosition;

    // the target beat that you should hit space on (1 measure later)
    public float previousTargetSongPosition = 0;
    public float targetSongPosition = 0;
    public int hitNotePosition = 0;

    [Header("Extra settings and bools")]
    public int measureMultiplier = 1;
    public int totalPoints = 0;
    public float lockMultiplier = 1;
    public float dspSongTime;

    public bool clickPlayed = false;
    public bool responded = false;
    public bool gameStarted = false;
    public bool gameEnded = false;

    public static Conductor instance;

    // Privates
    private AudioSource musicSource;
    private SongData currentSong;
    private int songListPosition;
    private AudioClip loadedClip;
    private Beatmap beatmap;

    // Song list + waiting song 
    public List<SongData> songList;
    public AudioClip waitingSong;

    // UI
    public GameObject settingsCanvas;
    public GameObject scoreText;
    public TextMeshPro currentlyPlaying;
    public TextMeshPro currentBPM;

    // VFX
    public Animator burst;
    public Animator sweep;
    public Animator stageclear;

    public GameObject leftbar, lefttri, rightbar, righttri;
    public Sprite whitebar, whitetri, redbar, redtri;

    public GameObject leftBar;
    public GameObject rightBar;
    private float leftStartX;
    private float leftMidpointX;
    private float leftStopX;
    private float rightStartX;
    private float rightMidpointX;
    private float rightStopX;

    public GameObject fade;

    // Lock and measure stuff
    public GameObject locke;
    public LockSpinWhee lockSpin;
    public MeasureTracker measureTracker;

    // SPACE 
    public Sprite yellowSpace;
    public Sprite normalSpace;
    public GameObject spacebar;
    public TextMeshProUGUI spacetext;

    // Start is called before the first frame update
    void Start() {
        instance = this;
        //Load the AudioSource attached to the Conductor GameObject
        musicSource = GetComponent<AudioSource>();
        lockSpin = locke.GetComponent<LockSpinWhee>();

        // record bar starting positions/midpoints
        leftStartX = leftBar.transform.position.x;
        rightStartX = rightBar.transform.position.x;
        leftStopX = -2.75f;
        rightStopX = 2.75f;
        leftMidpointX = (leftStartX + leftStopX) / 2;
        rightMidpointX = (rightStartX + rightStopX) / 2;

        // Set current song to song 0 and assign/calculate song data
        currentSong = songList[0]; 
        beatmap = currentSong.GetComponent<Beatmap>();

        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
        musicSource.clip = waitingSong;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Update is called once per frame
    void Update() {
        // Use ESC to pause/unpause the game
        if (Input.GetKeyDown(KeyCode.Escape)) {
            PauseGame();
        }

        // Inputs only allowed while settings menu not open
        if (!settingsCanvas.activeSelf) {
            // Use Q/E to swap between songs
            if (musicSource.clip == waitingSong) {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E)) {
                    if (Input.GetKeyDown(KeyCode.Q)) {
                        if (songListPosition > 0) {
                            songListPosition--;
                        }
                        else if (songListPosition == 0) {
                            songListPosition = 2;
                        }
                    }
                    else {
                        if (songListPosition < songList.Count - 1) {
                            songListPosition++;
                        }
                        else if (songListPosition == 2) {
                            songListPosition = 0;
                        }
                    }
                    currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                    SFXManager.instance.PlaySound("button");
                }
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                //Debug.Log("Target: " + targetSongPosition + " Clicked at: " + songPosition);
                StartCoroutine("PressButton");
                // Start the song with space
                if (!gameStarted && !gameEnded) {
                    spacetext.text = "SPACE";
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                    musicSource.clip = loadedClip;
                    musicSource.loop = false;
                    gameStarted = true;
                    SFXManager.instance.PlaySound("button");

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
                    scoreText.SetActive(true);
                    scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
                }
                else {
                    // If target beat determined and not yet clicked
                    if (clickPlayed && !responded) {

                        // If pressed at right timing (or an eigth of a beat b4/after the target pos)
                        if (Mathf.Abs(targetSongPosition - songPosition) < msPerBeat / 2 || 
                            Mathf.Abs(previousTargetSongPosition - songPosition) < msPerBeat / 2) {
                            // spin other direction
                            StartCoroutine("LockSpin");                                                          

                            SFXManager.instance.PlaySound("correct");
                            hitNotePosition = (int)(songPosition);
                            
                            // update score
                            totalPoints++;
                            scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;

                            // play vfx
                            StartCoroutine("FlashWhite");
                            sweep.Play("sweep", 0, 0);
                            Camera.main.GetComponent<CameraShake>().TriggerShake(0.2f);
                        }
                        else {
                            SFXManager.instance.PlaySound("wrong");
                        }
                        // determine new click time and snap to middle then back
                        StartCoroutine("SecondSnap");
                        DetermineClick();
                    }
                }
            }

            if (musicSource.isPlaying && gameStarted && !gameEnded) {
                // determine how many seconds since the song started
                songPosition = (float)(AudioSettings.dspTime - dspSongTime) * 1000f;

                if (lockSpin.rotationSpeed < 1500f && lockSpin.rotationSpeed > -1500f) {
                    if (lockSpin.rotationSpeed > 0) {
                        lockSpin.rotationSpeed += Time.deltaTime * lockMultiplier;
                    }
                    else {
                        lockSpin.rotationSpeed -= Time.deltaTime * lockMultiplier;
                    }

                }

                // when we get to the beatmap click position, play the click sound and snap
                if (songPosition > beatmapClickPosition && !clickPlayed){
                    SFXManager.instance.PlaySound("click");
                    StartCoroutine(FirstSnap());
                }

                // if the song position passes your click position and you don't click, determine a new click and then snap to middle and snap back
                if (songPosition > targetSongPosition + msPerBeat/2 && !responded && clickPlayed) {
                    DetermineClick();
                    StartCoroutine("ResponseTwitch");
                }
            }
        }
        

    }

    public IEnumerator LockSpin() {
        var newDirection = -lockSpin.rotationSpeed / lockSpin.rotationSpeed;
        lockSpin.rotationSpeed = 0;
        yield return new WaitForSeconds(1f);
        lockSpin.rotationSpeed = newDirection * 100f;

    }

    public void PauseGame() {
        if (settingsCanvas.activeSelf) {
            if (gameStarted) {
                musicSource.UnPause();
                measureTracker.moving = true;
                lockSpin.rotationSpeed = 100f;
            }
            settingsCanvas.SetActive(false);
        }

        else if (!settingsCanvas.activeSelf) {
            if (gameStarted) {
                musicSource.Pause();
                measureTracker.moving = false;
                lockSpin.rotationSpeed = 0f;
            }
            settingsCanvas.SetActive(true);
        }
    }

    public void SwitchSongs(AudioClip clip, string _name, int bpm, int sig) {
        if (musicSource.clip != waitingSong) {
            musicSource.clip = waitingSong;
            musicSource.loop = true;
            musicSource.Play();
        }     

        loadedClip = clip;
        songName = _name;
        songBpm = bpm;
        timeSig = sig;

        gameStarted = false;
        gameEnded = false;
        responded = false;
        clickPlayed = false;
        measureTracker.moving = false;

        totalPoints = 0;
        beatmapPosition = 0;
        beatmapClickPosition = 0;
        targetSongPosition = 0;
        lockSpin.rotationSpeed = 0f;

        beatmap = currentSong.GetComponent<Beatmap>();
        scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
        currentlyPlaying.text = songName;
        currentBPM.text = "" + songBpm;  

        CalculateSongInfo();
    }

    public void ResetSong() {
        settingsCanvas.SetActive(false);
        musicSource.Stop();
        StopCoroutine("LockSpin");
        StartCoroutine("ReturnSnap");
        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
    }

    // Chooses random beat to play a click within a time window
    public void DetermineClick() {
        beatmap = songList[songListPosition].GetComponent<Beatmap>();

        if (beatmapPosition == beatmap.clickMeasureList.Capacity) {
            gameEnded = true;
            if (totalPoints == beatmap.clickMeasureList.Capacity) {
                StartCoroutine("ShowEnding");
                SFXManager.instance.PlaySound("clear");
                stageclear.Play("stage clear", 0, 0);
            }
            ResetSong();
            
            //Debug.Log("made it to end of song, got " + totalPoints + " out of " + beatmapPosition+1);
        }
        StartCoroutine("ClickPlayed");
        if (beatmapPosition < beatmap.clickMeasureList.Capacity) {
            var beatmapMultiplier = 2f;
            if (currentSong.name == "Nuts and Bolts") {
                beatmapMultiplier = 1f;
            }
            beatmapClickPosition = ((beatmap.clickMeasureList[beatmapPosition] - 1) * measureLength + (beatmap.clickBeatList[beatmapPosition] - 1) * msPerBeat / beatmapMultiplier);
            previousTargetSongPosition = targetSongPosition;
            targetSongPosition = ((beatmap.playerMeasureList[beatmapPosition] - 1) * measureLength + (beatmap.playerBeatList[beatmapPosition] - 1) * msPerBeat / beatmapMultiplier);
            beatmapPosition++;
        }
        
        //else {
        //    if (songPosition >= clickBeatPosition) {
        //            clickBeatPosition = (int)(((songPosition % msPerBeat) + measureLength) + (int)Random.Range(0, timeSig) * msPerBeat);
        //            targetBeatPosition = clickBeatPosition + measureMultiplier * measureLength; // beatmap specific multiplier
        //            previousTargetSongPosition = targetSongPosition;
        //            targetSongPosition = songPosition + targetBeatPosition;
        //        }
        //    }
        //}
    }

    public IEnumerator ClickPlayed() {
        yield return new WaitForSeconds(0);
        clickPlayed = false;
        responded = false;

    }

    public IEnumerator ShowEnding() {
        fade.SetActive(true);
        yield return new WaitForSeconds(2f);
        fade.SetActive(false);
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
    }



    public IEnumerator PressButton() {
        spacebar.GetComponent<Image>().sprite = yellowSpace;
        yield return new WaitForSeconds(0.1f);
        spacebar.GetComponent<Image>().sprite = normalSpace;
    }
    
    
    public IEnumerator FirstSnap() {
        clickPlayed = true;
        burst.Play("burst", 0, 0);
        while (leftBar.transform.position.x != leftMidpointX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftMidpointX, 0), 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightMidpointX, 0), 0.5f);
        }
        yield return null;
    }

    public IEnumerator SecondSnap() {
        responded = true;
        while (leftBar.transform.position.x != leftStopX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftStopX, leftBar.transform.position.y), 0.5f);
        }
        while (rightBar.transform.position.x != rightStopX) {
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightStopX, rightBar.transform.position.y), 0.5f);
        }
        StartCoroutine("ReturnSnap");
        yield return null;
    }

    public IEnumerator ResponseTwitch() {
        while (leftBar.transform.position.x != leftStopX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftStopX, leftBar.transform.position.y), 0.5f);
        }
        while (rightBar.transform.position.x != rightStopX) {
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightStopX, rightBar.transform.position.y), 0.5f);
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

    public IEnumerator FlashWhite() {
        int blinkNum = 1;
        for (int i = 0; i < blinkNum; i++) {
            leftbar.GetComponent<SpriteRenderer>().sprite = whitebar;
            rightbar.GetComponent<SpriteRenderer>().sprite = whitebar;
            lefttri.GetComponent<SpriteRenderer>().sprite = whitetri;
            righttri.GetComponent<SpriteRenderer>().sprite = whitetri;
            yield return new WaitForSeconds(0.33f);
            leftbar.GetComponent<SpriteRenderer>().sprite = redbar;
            rightbar.GetComponent<SpriteRenderer>().sprite = redbar;
            lefttri.GetComponent<SpriteRenderer>().sprite = redtri;
            righttri.GetComponent<SpriteRenderer>().sprite = redtri;
        }
    }

    public void ChangeMusicVolume(float vol) {
        musicSource.volume = vol;
    }
}
