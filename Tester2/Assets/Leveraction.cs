using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leveraction : Interactible {

    public string activationLabel = "Pull";
    public string nameLabel = "Lever";
    void Awake () {
        base.setPublicName (activationLabel + "\n" + nameLabel);
    }

    public Interactible targetToActivate;

    // Start is called before the first frame update
    void Start () { }

    public override void TriggerInteraction () {
        targetToActivate.TriggerInteraction ();
    }

    void Update () { }
}