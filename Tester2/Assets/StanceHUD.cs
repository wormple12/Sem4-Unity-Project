using UnityEngine;
using UnityEngine.UI;

public class StanceHUD : MonoBehaviour {
    [Tooltip ("Image component for the stance sprites")]
    public Image stanceImage;
    [Tooltip ("Sprite to display when standing")]
    public Sprite standingSprite;
    [Tooltip ("Sprite to display when crouching")]
    public Sprite crouchingSprite;
    [Tooltip ("Sprite to display when sprinting")]
    public Sprite sprintingSprite;
    [Tooltip ("Sprite to display when climbing")]
    public Sprite climbingSprite;
    [Tooltip ("Sprite to display when in midair")]
    public Sprite jumpingSprite;
    [Tooltip ("Sprite to display when being a standing bird")]
    public Sprite birdSprite;
    [Tooltip ("Sprite to display when being a flying bird")]
    public Sprite flyingBirdSprite;

    PlayerCharacterController character;
    PlayerWalkController walkController;

    private void Start () {
        character = FindObjectOfType<PlayerCharacterController> ();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, StanceHUD> (character, this);
        walkController = FindObjectOfType<PlayerWalkController> ();

        character.onStanceChanged += OnStanceChanged;
        OnStanceChanged ();
    }

    void OnStanceChanged () {
        bool isBird = !character.gameObject.activeInHierarchy;
        PlayerMovementController movementController = FindObjectOfType<PlayerMovementController> ();
        if (isBird) {
            if (!movementController.isGrounded) {
                stanceImage.sprite = flyingBirdSprite;
            } else {
                stanceImage.sprite = birdSprite;
            }
        } else {
            if (character.isCrouching) {
                stanceImage.sprite = crouchingSprite;
            } else if (character.isClimbing) {
                stanceImage.sprite = climbingSprite;
            } else if (!movementController.isGrounded) {
                stanceImage.sprite = jumpingSprite;
            } else if (walkController.gameObject.activeInHierarchy && walkController.isSprinting) {
                stanceImage.sprite = sprintingSprite;
            } else {
                stanceImage.sprite = standingSprite;
            }
        }
    }
}