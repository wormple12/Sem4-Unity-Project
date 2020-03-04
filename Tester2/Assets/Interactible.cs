using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactible : MonoBehaviour {

    private string UI_name = "";

    public float interactionDistance = 1.5f;

    public abstract void TriggerInteraction ();

    public string getPublicName () {
        return UI_name;
    }

    public void setPublicName (string newName) {
        UI_name = newName;
    }

}