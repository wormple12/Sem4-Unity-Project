using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BasicDoor : Interactible {

    public string activationLabel { get; private set; } = "Open";
<<<<<<< HEAD
    public string name = "Door";
    public string nextScene= "";
=======
    public string nameLabel = "Door";
>>>>>>> 0c4abb10f9184fa3b7e891d42061dcd9af71b07b
    void Awake () {
        base.setPublicName (activationLabel + "\n" + nameLabel);
    }

    // Smoothly open a door
    public float doorOpenAngle = -90.0f; //Set either positive or negative number to open the door inwards or outwards
    public float openSpeed = 1.2f; //Increasing this value will make the door open faster
    public bool isOpened = false;

    Transform myParent;
    float defaultRotationAngle;
    float currentRotationAngle;
    float openTime = 0;

    // Start is called before the first frame update
    void Start () {
        myParent = transform.parent;
        defaultRotationAngle = myParent.localEulerAngles.y;
        currentRotationAngle = myParent.localEulerAngles.y;
    }

    public override void TriggerInteraction () {
        isOpened = !isOpened;
        currentRotationAngle = myParent.localEulerAngles.y;
        openTime = 0;
        if(nextScene.Length >= 1)
            SceneManager.LoadScene(nextScene);
    }

    void Update () {
        if (openTime < 1) {
            openTime += Time.deltaTime * openSpeed;
        }
        myParent.localEulerAngles = new Vector3 (myParent.localEulerAngles.x, Mathf.LerpAngle (currentRotationAngle, defaultRotationAngle + (isOpened ? doorOpenAngle : 0), openTime), myParent.localEulerAngles.z);
    }
}