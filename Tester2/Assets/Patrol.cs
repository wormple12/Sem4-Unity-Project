using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour {

    public float speed = 1;
    public float rotationSpeed = 2;
    public float defaultWaitTime = 2;
    private float waitTime;

    public Transform[] moveSpots;
    private int toSpot;

    void Start () {
        toSpot = 0;
        waitTime = defaultWaitTime;
    }

    void Update () {

        if (moveSpots.Length != 0) {
            transform.position = Vector3.MoveTowards (transform.position, moveSpots[toSpot].position, speed * Time.deltaTime);

            rotateTowardsMoveSpot (moveSpots[toSpot]);

            if (Vector3.Distance (transform.position, moveSpots[toSpot].position) < 0.2f) {
                if (waitTime <= 0) {
                    if (toSpot == moveSpots.Length - 1) {
                        toSpot = 0;
                    } else {
                        toSpot = toSpot + 1;
                    }
                    waitTime = defaultWaitTime;
                } else {
                    waitTime -= Time.deltaTime;
                }
            }
        }
    }

    private void rotateTowardsMoveSpot (Transform moveSpot) {
        // Determine which direction to rotate towards
        Vector3 targetDirection = moveSpot.position - transform.position;
        // The step size is equal to speed times frame time.
        float singleStep = rotationSpeed * Time.deltaTime;
        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards (transform.forward, targetDirection, singleStep, 0.0f);
        // Calculate a rotation a step closer to the target and applies rotation to this object
        transform.rotation = Quaternion.LookRotation (newDirection);
    }
}