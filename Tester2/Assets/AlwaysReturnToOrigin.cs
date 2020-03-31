using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysReturnToOrigin : MonoBehaviour {

    Vector3 origin;

    // Start is called before the first frame update
    void Start () {
        origin = transform.localPosition;
    }

    const float leeway = 0.03f;

    // Update is called once per frame
    void Update () {
        if (transform.localPosition.x > origin.x + leeway || transform.localPosition.x < origin.x - leeway ||
            transform.localPosition.z > origin.z + leeway || transform.localPosition.z < origin.z - leeway) {
            transform.localPosition = Vector3.Lerp (transform.localPosition, origin, 0.1f);
        }
    }
}