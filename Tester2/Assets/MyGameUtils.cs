using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGameUtils {

    public static float LimitVectorAngleTo90 (float angle) {
        float remainder = angle % 90;
        if (angle >= 90) {
            angle = (-90) + remainder;
        } else if (angle <= -90) {
            angle = 90 - remainder;
        }
        return angle;
    }

}