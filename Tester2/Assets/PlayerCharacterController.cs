using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler))]
public class PlayerCharacterController : MonoBehaviour {
    [Header ("References")]
    [Tooltip ("Reference to the camera parent used for the player")]
    public GameObject playerCamera;
    private Camera mainCamera;

    [Header ("Rotation")]
    [Tooltip ("Rotation speed for moving the camera")]
    public float rotationSpeed = 200f;
    [Range (0.1f, 1f)]
    [Tooltip ("Rotation speed multiplier when aiming")]
    public float aimingRotationMultiplier = 0.4f;

    [Header ("Stance")]
    [Tooltip ("Height of character when standing")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip ("Height of character when crouching")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip ("Speed of crouching transitions")]
    public float crouchingSharpness = 10f;

    [Header ("Other")]
    [Tooltip ("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    // ============================
    // CUSTOM CLIMBING VARIABLES
    [Header ("Climbing")]
    public float climbRayRange = 1f;
    // ============================

    // ============================
    // CUSTOM INTERACTION VARIABLES
    [Header ("Interaction")]
    public float interactibleDetectionDistance = 3.0f;
    public LayerMask layerMask;
    // ============================

    public Health m_Health { get; set; }
    PlayerInputHandler m_InputHandler;
    CharacterController m_Controller;
    PlayerClimbController m_ClimbController;
    PlayerWalkController m_WalkController;
    PlayerWeaponsManager m_WeaponsManager;
    public Actor m_Actor { get; private set; }
    Text interactionText;

    Ray ray;
    public UnityAction<bool> onStanceChanged;

    public bool isDead { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isClimbing { get; private set; }
    public float RotationMultiplier {
        get {
            if (m_WeaponsManager.isAiming) {
                return aimingRotationMultiplier;
            }

            return 1f;
        }
    }

    float m_CameraVerticalAngle = 0f;

    void Start () {
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController> (m_Controller, this, gameObject);

        m_ClimbController = GetComponent<PlayerClimbController> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerClimbController, PlayerCharacterController> (m_ClimbController, this, gameObject);

        m_WalkController = GetComponent<PlayerWalkController> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerWalkController, PlayerCharacterController> (m_WalkController, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController> (m_InputHandler, this, gameObject);

        m_WeaponsManager = GetComponent<PlayerWeaponsManager> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController> (m_WeaponsManager, this, gameObject);

        m_Health = GetComponent<Health> ();
        DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController> (m_Health, this, gameObject);

        m_Actor = GetComponent<Actor> ();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController> (m_Actor, this, gameObject);

        mainCamera = Camera.main;
        interactionText = GameObject.Find ("InteractionCanvas/InteractionText").GetComponent<Text> ();

        m_Health.onDie += OnDie;

        // force the crouch state to false when starting
        SetCrouchingState (false, true);
    }

    public void TransformTo (Vector3 targetPos, Vector3 targetRotation, Vector3 targetVelocity) {
        // convert to bird position
        transform.position = new Vector3 (targetPos.x, targetPos.y - capsuleHeightCrouching, targetPos.z);
        StartCoroutine (AttemptToStandForXSeconds (1f));

        // convert to bird rotation
        transform.localEulerAngles = new Vector3 (0, targetRotation.y, 0);
        // fixing weird issue with vertical rotation where it went above 90 and the camera clamp would break the desired rotation:
        m_CameraVerticalAngle = MyGameUtils.LimitVectorAngleTo90 (targetRotation.x);
        playerCamera.transform.localEulerAngles = new Vector3 (m_CameraVerticalAngle, 0, 0);

        // convert to bird velocity
        m_WalkController.characterVelocity += targetVelocity * 1.1f;
    }

    void Update () {
        // check for Y kill
        if (!isDead && transform.position.y < killHeight) {
            m_Health.Kill ();
        }

        // crouching
        if (m_InputHandler.GetCrouchInputDown ()) {
            SetCrouchingState (!isCrouching, false);
        }

        UpdateCameraRotation ();
        HandleControllerSwitch ();
        CheckInteraction ();
    }

    private Interactible viewedItem;
    private Interactible activatedItem;

    private void CheckInteraction () {
        RaycastHit hit;
        ray = mainCamera.ViewportPointToRay (Vector3.one / 2f);

        bool hasFoundValidItem = (Physics.Raycast (ray, out hit, interactibleDetectionDistance, layerMask));
        if (hasFoundValidItem) {
            viewedItem = hit.transform.gameObject.GetComponent<Interactible> ();
            hasFoundValidItem = (viewedItem && Vector3.Distance (hit.transform.position, playerCamera.transform.position) <= viewedItem.interactionDistance);

            if (hasFoundValidItem && Input.GetKeyDown (KeyCode.E)) {
                activatedItem = viewedItem;
                activatedItem.TriggerInteraction ();
            }
        }

        interactionText.text = (hasFoundValidItem && !viewedItem.getForceRemoveLabel ()) ? "(E) " + viewedItem.getPublicName () : "";

        if (activatedItem && (!hasFoundValidItem || Input.GetKeyUp (KeyCode.E))) {
            activatedItem.EndInteraction ();
            activatedItem = null;
        }
    }

    private void OnDie () {
        isDead = true;

        // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
        m_WeaponsManager.SwitchToWeaponIndex (-1, true);
    }

    private void UpdateCameraRotation () {
        // horizontal character rotation
        // rotate the transform with the input speed around its local Y axis
        transform.Rotate (new Vector3 (0f, (m_InputHandler.GetLookInputsHorizontal () * rotationSpeed * RotationMultiplier), 0f), Space.Self);
        // vertical camera rotation
        // add vertical inputs to the camera's vertical angle
        m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical () * rotationSpeed * RotationMultiplier;
        // limit the camera's vertical angle to min/max
        m_CameraVerticalAngle = Mathf.Clamp (m_CameraVerticalAngle, -89f, 89f);
        // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
        playerCamera.transform.localEulerAngles = new Vector3 (m_CameraVerticalAngle, 0, 0);
    }

    private void HandleControllerSwitch () {
        // CLIMBING HANDLING
        if (!isClimbing) {
            if (Input.GetKeyDown (KeyCode.Mouse1)) {
                RaycastHit raycastHit;
                if (Physics.Raycast (transform.position, transform.forward, out raycastHit, climbRayRange) && raycastHit.transform.tag == "Climbable") {
                    isClimbing = true;
                    m_WalkController.enabled = false;
                    m_ClimbController.enabled = true;
                }
            }
        } else {
            RaycastHit raycastHit;
            if (Input.GetKeyUp (KeyCode.Mouse1) || m_InputHandler.GetSprintInputHeld () || m_InputHandler.GetJumpInputDown () || (!m_ClimbController.isStable &&
                    (!Physics.Raycast (transform.position, transform.forward, out raycastHit, climbRayRange) || raycastHit.transform.tag != "Climbable")
                )) {
                isClimbing = false;
                m_ClimbController.enabled = false;
                m_WalkController.enabled = true;
                m_WalkController.characterVelocity = Vector3.up * 5f;
            }
        }
    }

    // returns false if there was an obstruction
    public bool SetCrouchingState (bool crouched, bool ignoreObstructions) {
        // set appropriate heights
        if (crouched) {
            m_WalkController.m_TargetCharacterHeight = capsuleHeightCrouching;
        } else {
            // Detect obstructions
            if (!ignoreObstructions) {
                Collider[] standingOverlaps = Physics.OverlapCapsule (
                    m_WalkController.GetCapsuleBottomHemisphere (),
                    m_WalkController.GetCapsuleTopHemisphere (capsuleHeightStanding),
                    m_Controller.radius, -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps) {
                    if (c != m_Controller) {
                        return false;
                    }
                }
            }

            m_WalkController.m_TargetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null) {
            onStanceChanged.Invoke (crouched);
        }

        isCrouching = crouched;
        return true;
    }

    // rise to standing position if possible, otherwise stay crouching
    private IEnumerator AttemptToStandForXSeconds (float x) {
        float countDown = x;
        for (int i = 0; i < 10000; i++) {
            while (countDown >= 0) {
                SetCrouchingState (false, false);
                countDown -= Time.smoothDeltaTime;
                yield return null;
            }
        }
    }
}