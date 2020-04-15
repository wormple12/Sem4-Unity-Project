using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIgnoreCollision : MonoBehaviour {

    public Collider bird;
    public Collider camera;

    // Start is called before the first frame update
    void Start () {
        Physics.IgnoreCollision (bird, camera);
    }

    // Update is called once per frame
    void Update () {

    }
}