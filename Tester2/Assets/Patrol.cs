using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour {

    public GameObject FOV = null;

    [Header ("Movement")]
    public float speed = 1.5f;
    public float rotationSpeed = 2.0f;
    public Transform[] moveSpots;
    private int toSpot;

    [Header ("Waiting")]
    public float defaultWaitTime = 5.0f;
    private float waitTime;

    public float headRotationSpeed = 1.0f;
    public float headMaxRotation = 35f;

    Quaternion headLeftTurn, headRightTurn;
    float headTurnTime;

    void Start () {
        toSpot = 0;
        waitTime = defaultWaitTime;
        ResetHeadRotation ();
    }

    void Update () {

        if (moveSpots.Length != 0) {
            transform.position = Vector3.MoveTowards (transform.position, moveSpots[toSpot].position, speed * Time.deltaTime);

            RotateTowardsMoveSpot (moveSpots[toSpot]);

            if (Vector3.Distance (transform.position, moveSpots[toSpot].position) < 0.2f) {
                if (waitTime <= 0 && IsHeadRotationAtCenter ()) {
                    if (toSpot == moveSpots.Length - 1) {
                        toSpot = 0;
                    } else {
                        toSpot = toSpot + 1;
                    }
                    waitTime = defaultWaitTime;
                    ResetHeadRotation ();
                } else {
                    waitTime -= Time.deltaTime;
                    RotateHead ();
                }
            }
        }
    }

    private void RotateTowardsMoveSpot (Transform moveSpot) {
        // Determine which direction to rotate towards
        Vector3 targetDirection = moveSpot.position - transform.position;
        // The step size is equal to speed times frame time.
        float singleStep = rotationSpeed * Time.deltaTime;
        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards (transform.forward, targetDirection, singleStep, 0.0f);
        // Calculate a rotation a step closer to the target and applies rotation to this object
        transform.rotation = Quaternion.LookRotation (newDirection);
    }

    private void ResetHeadRotation () {
        FOV.transform.localRotation = Quaternion.Euler (0, 0, 0);
        headTurnTime = 0.0f;
        headLeftTurn = Quaternion.AngleAxis (headMaxRotation, Vector3.up);
        headRightTurn = Quaternion.AngleAxis (-headMaxRotation, Vector3.up);
    }

    private void RotateHead () {
        // play head animation???

        // move volumetric light, using harmonic motion
        if (FOV) {
            headTurnTime += Time.deltaTime;
            FOV.transform.localRotation = Quaternion.Lerp (headLeftTurn, headRightTurn,
                (Mathf.Sin (headTurnTime * headRotationSpeed) + 1.0f) / 2.0f);
        }
    }

    private bool IsHeadRotationAtCenter () {
        float headAngle = FOV.transform.localEulerAngles.y;
        float ceiling = 0.5f;
        float floor = -ceiling;
        return headAngle >= Mathf.Min (floor, ceiling) && headAngle <= Mathf.Max (floor, ceiling);
    }
}