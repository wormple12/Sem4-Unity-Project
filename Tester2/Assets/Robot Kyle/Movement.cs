using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    [SerializeField] float movespeed = 10f;

    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        transform.Translate (Input.GetAxis ("Horizontal") * movespeed * Time.deltaTime, 0, Input.GetAxis ("Vertical") * movespeed * Time.deltaTime);
    }
}