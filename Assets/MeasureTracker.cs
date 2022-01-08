using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeasureTracker : MonoBehaviour
{
    public Conductor conductor;

    public GameObject leftTracker, rightTracker;
    public Vector2 leftStart, rightStart;
    public bool direction;

    public float moveTime;
    public float moveSpeed;

    public bool moving;

    // Start is called before the first frame update
    void Start()
    {
        leftStart = leftTracker.transform.position;
        rightStart = rightTracker.transform.position;
        moving = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (moving) {
            float y = Mathf.PingPong((float)(AudioSettings.dspTime - conductor.dspSongTime) * 1000f * 16.66f / conductor.measureLength, 8.33f) - 8.33f / 2;
            leftTracker.transform.position = new Vector3(leftTracker.transform.position.x, -y, 0);
            rightTracker.transform.position = new Vector3(rightTracker.transform.position.x, y, 0);
        }
        
        //if (!moving && conductor.musicSource.isPlaying) {
        //    StopAllCoroutines();
        //    //StartCoroutine("UpAndDown");

        //}
    }

    public IEnumerator UpAndDown() {
        moving = true;
        moveTime = conductor.measureLength;
        while(moveTime > 0) {
            moveTime -= (float)(AudioSettings.dspTime - conductor.dspSongTime) * 1000f;
            if (!direction) {
                leftTracker.transform.position = new Vector3(leftTracker.transform.position.x, leftTracker.transform.position.y - moveSpeed, 0);
                rightTracker.transform.position = new Vector3(rightTracker.transform.position.x, rightTracker.transform.position.y + moveSpeed, 0);
            }
            else if (direction) {
                leftTracker.transform.position = new Vector3(leftTracker.transform.position.x, leftTracker.transform.position.y + moveSpeed, 0);
                rightTracker.transform.position = new Vector3(rightTracker.transform.position.x, rightTracker.transform.position.y - moveSpeed, 0);
            }
        }
        direction = !direction;
        yield return new WaitForEndOfFrame();
        moving = false;
    }
}
