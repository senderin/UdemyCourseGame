using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCam : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;

    }

    void Update()
    {
        Vector3 dir = transform.position - cam.transform.position;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = rot;
    }
}
