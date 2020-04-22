using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactible : MonoBehaviour {

    private string UI_name = "";
    protected bool forceRemoveLabel = false;

    public float interactionDistance = 3f;

    public abstract void TriggerInteraction ();

    public virtual void EndInteraction () { }

    public string getPublicName () {
        return UI_name;
    }

    public void setPublicName (string newName) {
        UI_name = newName;
    }

    public bool getForceRemoveLabel () {
        return forceRemoveLabel;
    }

}