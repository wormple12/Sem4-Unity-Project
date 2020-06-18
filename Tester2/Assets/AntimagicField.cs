using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntimagicField : MonoBehaviour {

    private PlayerBirdController birdController;

    void Start () {
        // cleanup! E.g. import file with Important GameObjects instead of many of the Find calls
        GameObject player = GameObject.Find ("Player");
        birdController = player.transform.parent.transform.Find ("BirdCamMaster").Find ("BirdPlayer").gameObject.GetComponent<PlayerBirdController> ();
    }

    private void OnTriggerEnter (Collider other) {
        if (other.tag == "Player") {
            if (other.gameObject.name == "BirdPlayer") {
                birdController.timeRemaining = 0;
            }
        } else if (other.tag == "lb_bird") {
            other.gameObject.GetComponent<lb_Bird> ().KillBird ();
        }
    }

    private void OnTriggerExit (Collider other) {

    }
}