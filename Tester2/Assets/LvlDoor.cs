using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class LvlDoor : Interactible {

    public string activationLabel { get; private set; } = "Proceed";
    public string nameLabel = "Next";
    public string nextScene = "";
    void Awake () {
        base.setPublicName (activationLabel + "\n" + nameLabel);
    }

    private TextMeshProUGUI cashCounter;
    private int cash;

    void Start () {
        cashCounter = GameObject.Find ("CashCounter").GetComponent<TextMeshProUGUI> ();
        cash= int.Parse(cashCounter.text.Replace(@"$", ""));
    }

    public override void TriggerInteraction () {
        if(cash == 20)
            SceneManager.LoadScene ("Level2");
        else if(nextScene.Length >= 1)
            SceneManager.LoadScene (nextScene);
    }

    void Update () {
     
    }
}
