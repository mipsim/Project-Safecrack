using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public float resetHoldTimer;
    public bool autoFailToggle;

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
    public Slider progressBar;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI endScoreText;
    public TextMeshProUGUI hitsText;
    public TextMeshProUGUI missesText;
    private Color goldColor = new Color(1, 0.7401557f, 0);

    // VFX
    public Animator burst;
    public Animator sweep;
    public Animator sweep2;
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
    public GameObject railOverlay;
    public GameObject railTwo;
    public GameObject railThree;
    public LockSpinWhee lockSpin;
    public MeasureTracker measureTracker;

    // SPACE 
    public Sprite yellowSpace;
    public Sprite normalSpace;
    public GameObject spacebar;
    public TextMeshProUGUI spacetext;

    // Start is called before the first frame update
    private void Awake() {
        musicSource = GetComponent<AudioSource>();
        instance = this;

    }
    void Start() {
        //Load the AudioSource attached to the Conductor GameObject
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

        if (PlayerPrefs.HasKey("BGMVolume")) {
            ChangeMusicVolume(PlayerPrefs.GetFloat("BGMVolume"));
        }
        
    }

    // Update is called once per frame
    void Update() {
        //if (Input.GetKeyDown(KeyCode.Y)) {
        //    PlayerPrefs.SetFloat("Lock Mock", 1);
        //    PlayerPrefs.SetFloat("Batter Up", 1);
        //    PlayerPrefs.SetFloat("Nuts and Bolts", 1);
        //}

        // Use ESC to pause/unpause the game
        if (Input.GetKeyDown(KeyCode.Escape)) {
            settingsCanvas.transform.parent.GetComponent<SettingsMenu>().ToggleSettings();
        }

        // Inputs only allowed while settings menu not open
        if (!settingsCanvas.activeSelf && !resultText.transform.parent.gameObject.activeSelf) {
            // Use Q/E to swap between songs
            if (!gameStarted) {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E)) {
                    if (Input.GetKeyDown(KeyCode.Q)) {
                        if (songListPosition > 0) {
                            songListPosition--;
                        }
                        else if (songListPosition == 0) {
                            songListPosition = 3;
                        }
                    }
                    else {
                        if (songListPosition < songList.Count - 1) {
                            songListPosition++;
                        }
                        else if (songListPosition == 3) {
                            songListPosition = 0;
                        }
                    }
                    currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                    SFXManager.instance.PlaySound("button");
                }
            }
            else {
                
                // determine how many seconds since the song started
                songPosition = (float)(AudioSettings.dspTime - dspSongTime) * 1000f;

                progressBar.value = songPosition / (loadedClip.length * 1000f);

                lockSpin.rotationSpeed = progressBar.value * 1269 * (songBpm/100);
                //if (lockSpin.rotationSpeed < 1500f && lockSpin.rotationSpeed > -1500f) {
                //    if (lockSpin.rotationSpeed > 0) {
                //        lockSpin.rotationSpeed += Time.deltaTime * lockMultiplier;
                //    }
                //    else {
                //        lockSpin.rotationSpeed -= Time.deltaTime * lockMultiplier;
                //    }

                //}

                // when we get to the beatmap click position, play the click sound and snap
                if (songPosition > beatmapClickPosition && !clickPlayed) {
                    SFXManager.instance.PlaySound("click");
                    StartCoroutine(FirstSnap());
                }

                // if the song position passes your click position and you don't click, determine a new click and then snap to middle and snap back
                if (songPosition > targetSongPosition + msPerBeat / 2 && !responded && clickPlayed) {
                    SFXManager.instance.PlaySound("wrong");
                    if (autoFailToggle) {
                        ResetSong();
                        sweep2.Play("sweep", 0, 0);
                    }
                    else {
                        DetermineClick();
                        StartCoroutine("ResponseTwitch");
                    }
                }

                if (Input.GetKey(KeyCode.Tab)) {
                    resetHoldTimer += Time.deltaTime;
                    if (resetHoldTimer >= 1f) {
                        ResetSong();
                        sweep2.Play("sweep", 0, 0);

                    }
                }
                else {
                    resetHoldTimer = 0;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                StartCoroutine("PressButton");
                // Start the song with space
                if (!gameStarted) {
                    railOverlay.SetActive(true);
                    StartCoroutine(SpawnRails());
                    progressBar.gameObject.SetActive(true);
                    spacetext.text = "SPACE TO CRACK";
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
                else if (clickPlayed && !responded) {
                    // If pressed at right timing (or an eigth of a beat b4/after the target pos)
                    if (Mathf.Abs(targetSongPosition - songPosition) < msPerBeat / 2 ||
                        Mathf.Abs(previousTargetSongPosition - songPosition) < msPerBeat / 2) {

                        // spin other direction
                        //StartCoroutine("LockSpin");
                        lockSpin.rotationSpeed = -lockSpin.rotationSpeed;

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
                        if (autoFailToggle) {
                            ResetSong();
                        }
                    }
                    // determine new click time and snap to middle then back
                    StartCoroutine("SecondSnap");
                    DetermineClick();
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
        }

        else if (!settingsCanvas.activeSelf) {
            if (gameStarted) {
                musicSource.Pause();
                measureTracker.moving = false;
                lockSpin.rotationSpeed = 0f;
            }
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

        beatmap = currentSong.GetComponent<Beatmap>();
        scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
        currentlyPlaying.text = songName;
        currentBPM.text = "" + songBpm;

        if (PlayerPrefs.GetFloat(currentlyPlaying.text) == 1) {
            currentlyPlaying.color = goldColor;
        }
        else {
            currentlyPlaying.color = Color.white;
        }

        CalculateSongInfo();
    }

    public void ResetSong() {
        

        gameStarted = false;
        gameEnded = false;
        responded = false;
        clickPlayed = false;
        measureTracker.moving = false;

        songPosition = 0;
        totalPoints = 0;
        beatmapPosition = 0;
        beatmapClickPosition = 0;
        targetSongPosition = 0;
        lockSpin.rotationSpeed = 0f;
        resetHoldTimer = 0;
        progressBar.value = 0;

        scoreText.GetComponent<TextMeshPro>().text = totalPoints + "/" + beatmap.clickMeasureList.Capacity;
        spacetext.text = "SPACE TO START";
        settingsCanvas.SetActive(false);
        progressBar.gameObject.SetActive(false);
        railOverlay.SetActive(false);
        railTwo.SetActive(false);
        railThree.SetActive(false);
        StopCoroutine("LockSpin");
        StartCoroutine("ReturnSnap");

        if (musicSource.clip != waitingSong) {
            musicSource.clip = waitingSong;
            musicSource.loop = true;
            musicSource.Play();
        }
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
                resultText.text = "You cracked the lock!";
                int finalScore = (int)System.Math.Truncate((double)((float)totalPoints / (float)beatmap.clickMeasureList.Capacity * 100));
                endScoreText.text = "" + finalScore + "%";
                hitsText.text = "" + totalPoints;
                missesText.text = "" + (beatmap.clickMeasureList.Capacity - totalPoints);
                StartCoroutine(OpenScreen(2));
                if (currentlyPlaying.color != goldColor) {
                    if (!PlayerPrefs.HasKey(currentlyPlaying.text)) {
                        PlayerPrefs.SetFloat(currentlyPlaying.text, 1);
                    }
                    currentlyPlaying.color = goldColor;
                }
            }
            else {
                resultText.text = "The lock still stands!";
                int finalScore = (int)System.Math.Truncate((double)((float)totalPoints / (float)beatmap.clickMeasureList.Capacity * 100));
                endScoreText.text = "" + finalScore + "%";
                hitsText.text = "" + totalPoints;
                missesText.text = "" + (beatmap.clickMeasureList.Capacity - totalPoints);
                resultText.gameObject.transform.parent.gameObject.SetActive(true);
                StartCoroutine(OpenScreen(0.25f));

            }
            ResetSong();

            //Debug.Log("made it to end of song, got " + totalPoints + " out of " + beatmapPosition+1);
        }
        
        else if (beatmapPosition < beatmap.clickMeasureList.Capacity) {
            StartCoroutine("ClickPlayed");

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

    public IEnumerator SpawnRails() {
        yield return new WaitForSeconds(1f);
        railTwo.SetActive(true);
        yield return new WaitForSeconds(1f);
        railThree.SetActive(true);

    }

    public IEnumerator OpenScreen(float delay) {
        yield return new WaitForSeconds(delay);
        resultText.gameObject.transform.parent.gameObject.SetActive(true);
    }

    public void ChangeMusicVolume(float vol) {
        musicSource.volume = vol;
    }

    public void CloseScreen() {
        resultText.gameObject.transform.parent.gameObject.SetActive(false);
        if (PlayerPrefs.GetFloat("Spynthesizer") == 1 && PlayerPrefs.GetFloat("Lock Mock") == 1 && PlayerPrefs.GetFloat("Batter Up") == 1 && PlayerPrefs.GetFloat("Nuts and Bolts") == 1 && PlayerPrefs.GetFloat("Credits") != 1) {
            PlayerPrefs.SetFloat("Credits", 1);
            StartCoroutine("FadeToCredits");
            
        }
    }

    public IEnumerator FadeToCredits() {
        fade.SetActive(true);
        var fadeScreen = fade.GetComponent<SpriteRenderer>();
        fadeScreen.color = new Color(0, 0, 0, 0);
        float fadeAlpha;
        while (fadeScreen.color.a < 1) {
            fadeAlpha = fadeScreen.color.a + (0.5f * Time.deltaTime);
            fadeScreen.color = new Color(0, 0, 0, fadeAlpha);
            yield return null;
        }
        //victory screen here
        SceneManager.LoadScene(2);
        yield return new WaitForEndOfFrame();
    }
}
