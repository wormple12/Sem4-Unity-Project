using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactible : MonoBehaviour {

    private string UI_name = "";

    public bool hasExternalTrigger = false;
    public float interactionDistance = 3f;

    public abstract void TriggerInteraction ();

    public virtual void EndInteraction () { }

    public string getPublicName () {
        return UI_name;
    }

    public void setPublicName (string newName) {
        UI_name = newName;
    }

    // for special case of bird transformation, where the label didn't have time 
    // to become disabled before the script that handled the disabling stopped
    protected bool forceRemoveLabel = false;
    public bool getForceRemoveLabel () {
        return forceRemoveLabel;
    }

}