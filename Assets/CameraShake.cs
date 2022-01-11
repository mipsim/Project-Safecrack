using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.03f;
    private float dampingSpeed = 1.0f;
    Vector3 initialPosition;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (shakeDuration > 0) {
            transform.localPosition = initialPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else {
            shakeDuration = 0f;
            initialPosition = transform.position;
            transform.localPosition = initialPosition;
        }
    }

    public void TriggerShake(float duration) {
        shakeDuration = duration;
    }
}
