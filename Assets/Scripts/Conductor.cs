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

    public GameObject leftBar;
    public GameObject rightBar;
    private float leftStartX;
    private float leftMidpointX;
    private float rightStartX;
    private float rightMidpointX;

    public GameObject locke;
    private LockSpinWhee lockSpin;
    public MeasureTracker measureTracker;

    private bool clickPlayed = false;
    private bool responded = false;

    public float invokeDelay = 0f;

    public Dictionary<int, int> lockNumbers = new Dictionary<int, int>();
    public GameObject[] numberSlots;

    public TextMeshPro currentlyPlaying;
    public TextMeshPro currentBPM;
    public List<SongData> songList;
    public int songListPosition;

    // Start is called before the first frame update
    void Start() {
        //Load the AudioSource attached to the Conductor GameObject
        musicSource = GetComponent<AudioSource>();
        songBpm = songList[0].GetComponent<SongData>().bpm;
        timeSig = songList[0].GetComponent<SongData>().sig;
        musicSource.clip = songList[0].GetComponent<SongData>().clip;
        songName = songList[0].GetComponent<SongData>().songName;
        currentlyPlaying.text = "Currently playing: " + songName;
        currentBPM.text = "BPM: " + songBpm;

        // record bar starting positions/midpoints
        leftStartX = leftBar.transform.position.x;
        rightStartX = rightBar.transform.position.x;
        leftMidpointX = (leftStartX + locke.transform.position.x) / 2;
        rightMidpointX = (rightStartX + locke.transform.position.x) / 2;

        lockSpin = locke.GetComponent<LockSpinWhee>();



        //Calculate the number of seconds in each beat
        milliSecPerBeat = 60000f / songBpm;

        // Measure length in milliseconds (60 seconds per minute / measures per minute) --> measures per minute is bpm / timeSig
        measureLength = 60000f / (songBpm / timeSig);

        for (int i = 0; i < 16; i++) {
            lockNumbers.Add(i, 0);
        }

        CalculateSixteenths();
    }

    // Update is called once per frame
    void Update() {
        //Debug.Log(songPosition + ", " + targetBeatPosition);
        if (!musicSource.isPlaying) {
            if (Input.GetKeyDown(KeyCode.Q)) {
                if (songListPosition > 0) {
                    songListPosition--;
                    var currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                }
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                if (songListPosition < songList.Count) {
                    songListPosition++;
                    var currentSong = songList[songListPosition];
                    SwitchSongs(currentSong.clip, currentSong.songName, currentSong.bpm, currentSong.sig);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && musicSource.isPlaying) {
            musicSource.Stop();
            lockSpin.rotationSpeed = 0f;
            StopAllCoroutines();
            responded = true;
            StartCoroutine("ReturnSnap");
            measureTracker.moving = false;
            measureTracker.leftTracker.transform.position = measureTracker.leftStart;
            measureTracker.rightTracker.transform.position = measureTracker.rightStart;
            foreach (GameObject slot in numberSlots) {
                slot.SetActive(false);
            }
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            // Start the song with space
            if (!musicSource.isPlaying) {
                // Record the time when the music starts
                dspSongTime = (float)AudioSettings.dspTime;

                // Start the music
                musicSource.Play();

                // Start spinning lock
                lockSpin.rotationSpeed = 100f;
                responded = false;
                measureTracker.moving = true;
                // 2 seconds later, determine click timing
                Invoke("DetermineClick", invokeDelay);
            }
            else {
                if (targetBeatPosition != 0f) {
                    StartCoroutine(SecondSnap());

                    // If pressed at right timing
                    if (Mathf.Abs(targetBeatPosition - songPosition) < milliSecPerBeat / 2) {
                        Debug.Log("good");
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
                        //if (hitNote > (int)lockNumbers[14] && hitNote < (int)lockNumbers[15]) {
                        //    DisplayNumber(hitNote);
                        //    break;
                        //}
                    }
                    else {
                        Debug.Log("ur bad");
                        SFXManager.instance.PlaySound("wrong");

                    }
                    Invoke("DetermineClick", invokeDelay);
                    StopAllCoroutines();
                    StartCoroutine("ReturnSnap");
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
                StartCoroutine(FirstSnap());
                Debug.Log("click");
                SFXManager.instance.PlaySound("click");
            }
            if (songPosition > targetBeatPosition && !responded && targetBeatPosition != 0) {
                StartCoroutine("ResponseTwitch");

                StopAllCoroutines();
                StartCoroutine("ReturnSnap");
                DetermineClick();
            }
        }

    }

    public void SwitchSongs(AudioClip clip, string _name, int bpm, int sig) {
        musicSource.clip = clip;
        songName = _name;
        songBpm = bpm;
        timeSig = sig;
        //Calculate the number of seconds in each beat
        milliSecPerBeat = 60000f / songBpm;

        // Measure length in milliseconds (60 seconds per minute / measures per minute) --> measures per minute is bpm / timeSig
        measureLength = 60000f / (songBpm / timeSig);

        currentlyPlaying.text = "Currently playing: " + songName;
        currentBPM.text = "BPM: " + songBpm;
        CalculateSixteenths();
    }

    // Chooses random beat to play a click within a time window
    public void DetermineClick() {
        // Chooses a position in the song between current pos and the next x amount of beats (in this case 24), with a delay 
        //clickBeatPosition = (int)Random.Range(songPositionInBeats + (songBpm / 8f), songPositionInBeats + 24 + (songBpm / 8f));
        clickBeatPosition = (int)(songPosition + 10.7f * milliSecPerBeat);

        // should be one measure after the randomBeat
        targetBeatPosition = clickBeatPosition + measureLength;

        clickPlayed = false;
        responded = false;
    }

    public void CalculateSixteenths() {
        for (int i = 0; i < 16; i++) {
            //Debug.Log(measureLength);
            lockNumbers[i] =  (int)((i / 16f) * measureLength);
            //Debug.Log(lockNumbers[i]);
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
        while (leftBar.transform.position.x != leftMidpointX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftMidpointX, 0), 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightMidpointX, 0), 0.5f);
        }
        yield return null;
    }

    public IEnumerator SecondSnap() {
        while (leftBar.transform.position.x != locke.transform.position.x) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, locke.transform.position, 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, locke.transform.position, 0.5f);
        }
        responded = true;
        yield return null;
    }

    public IEnumerator ReturnSnap() {
        if (!responded) {
            SFXManager.instance.PlaySound("wrong");

            yield return new WaitForSeconds(0.5f);

        }
        while (leftBar.transform.position.x != leftStartX) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, new Vector2(leftStartX, 0), 0.5f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, new Vector2(rightStartX, 0), 0.5f);
        }
        yield return null;
    }

    public IEnumerator ResponseTwitch() {
        while (leftBar.transform.position.x != locke.transform.position.x) {
            leftBar.transform.position = Vector2.MoveTowards(leftBar.transform.position, locke.transform.position, 0.05f);
            rightBar.transform.position = Vector2.MoveTowards(rightBar.transform.position, locke.transform.position, 0.05f);
        }
        yield return null;
    }
}
