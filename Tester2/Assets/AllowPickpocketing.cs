using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AllowPickpocketing : Interactible {

    public string activationLabel { get; private set; } = "Pickpocket";
    public string nameLabel = "Guard";
    void Awake () {
        base.setPublicName (activationLabel + "\n" + nameLabel);
    }

    //public GameObject normalCrosshair;
    public GameObject loadingCircle;
    private List<Image> progressImages = new List<Image> ();

    [SerializeField]
    public WealthLevel wealthLevel = WealthLevel.POOR;
    public float secondsToSteal = 3f;

    // Start is called before the first frame update
    void Start () {
        foreach (Transform child in loadingCircle.transform) {
            Image progressImage = child.gameObject.GetComponent<Image> ();
            progressImages.Add (progressImage);
        }
    }

    public override void TriggerInteraction () {
        //normalCrosshair.SetActive (false);
        loadingCircle.SetActive (true);
        isStealing = true;
        secondsPassed = 0;
    }

    private bool isStealing = false;
    private float secondsPassed = 0;

    // Update is called once per frame
    void Update () {
        if (isStealing) {
            if (secondsPassed <= secondsToSteal) {
                secondsPassed += Time.smoothDeltaTime;
                float progressPct = secondsPassed / secondsToSteal;
                progressImages.ForEach (delegate (Image image) {
                    image.fillAmount = progressPct;
                });
            } else {
                EndInteraction ();
            }
        }
    }

    // temporary count of stolen cash
    private static int stolenTotal = 0;

    public override void EndInteraction () {
        if (isStealing && secondsPassed > secondsToSteal) {
            stolenTotal += (int) wealthLevel;
            Debug.Log ("You stole $" + (int) wealthLevel + "! Your total cash: $" + stolenTotal + ".");
            enabled = false;
        }

        isStealing = false;
        progressImages.ForEach (delegate (Image image) {
            image.fillAmount = 0;
        });
        loadingCircle.SetActive (false);
    }
}

public enum WealthLevel {
    BEGGAR = 5,
    POOR = 10,
    MEDIOCRE = 20,
    WEALTHY = 50,
    EPIC = 100
}