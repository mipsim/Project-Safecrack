using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockSpinWhee : MonoBehaviour
{
    public float rotationSpeed;

    // Start is called before the first frame update
    void Start()
    {
        rotationSpeed = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, (rotationSpeed * Time.deltaTime));
    }
}
