using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    public float targetSongPosition;
    public float previousTargetSongPosition;

    public GameObject leftBar;
    public GameObject rightBar;
    private float leftStartX;
    private float leftMidpointX;
    private float leftStopX;
    private float rightStartX;
    private float rightMidpointX;
    private float rightStopX;

    public GameObject locke;
    public LockSpinWhee lockSpin;
    public MeasureTracker measureTracker;

    public bool clickPlayed = false;
    public bool responded = false;
    public bool gameStarted;
    public bool gameEnded;
    //public Dictionary<int, int> lockNumbers = new Dictionary<int, int>();
    //public GameObject[] numberSlots;

    public TextMeshPro currentlyPlaying;
    public TextMeshPro currentBPM;
    public List<SongData> songList;
    public SongData currentSong;
    public int songListPosition;

    public static Conductor instance;
    public int measureMultiplier = 1;
    public GameObject settingsCanvas;
    public GameObject[] deadbolts;
    public int deadboltCount;
    public int totalPoints;
    public Animator burst;
    public Animator sweep;
    public Animator stageclear;

    public GameObject scoreText;
    public int score;
    public AudioClip loadedClip;
    public AudioClip waitingSong;
    public Beatmap beatmap;
    public GameObject fade;
    public GameObject spacebar;
    public Sprite yellowSpace;
    public Sprite normalSpace;

    public GameObject leftbar, lefttri, rightbar, righttri;
    public Sprite whitebar, whitetri, redbar, redtri;
    public TextMeshProUGUI spacetext;
    public float lockMultiplier = 1;

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

        //// Initialize dictionary with 16 0's
        //for (int i = 0; i < 16; i++) {
        //    lockNumbers.Add(i, 0);
        //}

        // Set current song to song 0 and assign/calculate song data
        currentSong = songList[0]; 
        beatmap = currentSong.GetComponent<Beatmap>();

        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
        musicSource.clip = waitingSong;
        musicSource.loop = true;
        musicSource.Play();
        deadbolts = GameObject.FindGameObjectsWithTag("Deadbolt");
    }

    // Update is called once per frame
    void Update() {
        if (settingsCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
           
            if (gameStarted) {
                lockSpin.rotationSpeed = 100f;
                musicSource.UnPause();
                measureTracker.moving = true;
            }
            
            settingsCanvas.SetActive(false);
        }

        else if (!settingsCanvas.activeSelf) {
            // Use Q/E to swap between songs
            if (musicSource.clip == waitingSong) {
                if (Input.GetKeyDown(KeyCode.Q)) {
                    if (songListPosition > 0) {
                        songListPosition--;
                        currentSong = songList[songListPosition];
                        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                        SFXManager.instance.PlaySound("button");
                    }
                    else if (songListPosition == 0) {
                        songListPosition = 2;
                        currentSong = songList[songListPosition];
                        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                        SFXManager.instance.PlaySound("button");
                    }
                }
                if (Input.GetKeyDown(KeyCode.E)) {
                    if (songListPosition < songList.Count-1) {
                        songListPosition++;
                        currentSong = songList[songListPosition];
                        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                        SFXManager.instance.PlaySound("button");

                    }
                    else if (songListPosition == 2) {
                        songListPosition = 0;
                        currentSong = songList[songListPosition];
                        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                        SFXManager.instance.PlaySound("button");
                    }
                }
            }

            // Use ESC to stop the song
            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (gameStarted) {
                    musicSource.Pause();
                    measureTracker.moving = false;
                    lockSpin.rotationSpeed = 0f;
                    spacetext.text = "SPACE TO START";
                }
                settingsCanvas.SetActive(true);
                
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                Debug.Log("Target: " + targetSongPosition + " Clicked at: " + songPosition);
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
                        //Debug.Log("Target: " + targetSongPosition + " Clicked at: " + songPosition);


                        // If pressed at right timing (or earlier)
                        if (Mathf.Abs(targetSongPosition - songPosition) < msPerBeat || (songPosition > previousTargetSongPosition && Mathf.Abs(previousTargetSongPosition - songPosition) < msPerBeat / 2)) {// / 2) {
                            StartCoroutine("LockSpin");                                                          
                            // spin other direction
                            SFXManager.instance.PlaySound("correct");
                            int hitNote = (int)(songPosition % measureLength);
                            sweep.Play("sweep", 0, 0);
                            totalPoints++;
                            StartCoroutine("FlashWhite");
                            scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
                            //deadbolts[deadboltCount].SetActive(false);
                            //deadboltCount++;
                            Camera.main.GetComponent<CameraShake>().TriggerShake(0.2f);
                            //for (int i = 0; i < 15; i++) {
                            //    if (hitNote > lockNumbers[i] && hitNote < (int)lockNumbers[i + 1]) {
                            //        DisplayNumber(i);
                            //        break;
                            //    }
                            //}

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

                // when we get to the randomly generated beat position, play the click
                if (songPosition > beatmapClickPosition && !clickPlayed){ //Mathf.Abs(songPosition - beatmapClickPosition) < 2f && !clickPlayed) {
                    Debug.Log(songPosition);
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
                lockSpin.rotationSpeed = 100f;
                musicSource.UnPause();
                measureTracker.moving = true;
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

        gameEnded = false;
        totalPoints = 0;
        currentlyPlaying.text = songName;
        currentBPM.text = "" + songBpm;
        beatmap = currentSong.GetComponent<Beatmap>();
        scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
        clickBeatPosition = 0;
        beatmapPosition = 0;
        beatmapClickPosition = 0;
        targetSongPosition = 0;

        deadboltCount = 0;
        foreach (GameObject deadbolt in deadbolts) {
            deadbolt.SetActive(true);
        }
        CalculateSongInfo();
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

        //for (int i = 0; i < 16; i++) {
        //    lockNumbers[i] =  (int)((i / 16f) * measureLength);
        //}
    }

    //public void DisplayNumber(int hit) {
    //    foreach (GameObject slot in numberSlots) {
    //        if (!slot.activeSelf) {
    //            slot.SetActive(true);
    //            slot.GetComponent<TextMeshProUGUI>().text = "" + hit;
    //            break;
    //        }
    //    }
    //}

    public IEnumerator PressButton() {
        spacebar.GetComponent<Image>().sprite = yellowSpace;
        yield return new WaitForSeconds(0.1f);
        spacebar.GetComponent<Image>().sprite = normalSpace;
    }
    
    public void ResetSong() {
        gameStarted = false;
        gameEnded = false;
        settingsCanvas.SetActive(false);
        musicSource.Stop();
        measureTracker.moving = false;
        lockSpin.rotationSpeed = 0f;
        responded = false;
        StartCoroutine("ReturnSnap");
        beatmapPosition = 0;
        totalPoints = 0;
        //foreach (GameObject slot in numberSlots) {
        //    slot.SetActive(false);
        //}
        SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
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
