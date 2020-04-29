using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIgnoreCollision : MonoBehaviour {

    public Collider birdCollider;
    public Collider cameraCollider;

    // Start is called before the first frame update
    void Start () {
        Physics.IgnoreCollision (birdCollider, cameraCollider);
    }

    // Update is called once per frame
    void Update () {

    }
}