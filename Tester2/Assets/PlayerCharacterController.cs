using UnityEngine;
using UnityEngine.Events;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler), typeof (AudioSource))]
public class PlayerCharacterController : MonoBehaviour {
    [Header ("References")]
    [Tooltip ("Reference to the main camera used for the player")]
    public Camera playerCamera;
    [Tooltip ("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;

    [Header ("Rotation")]
    [Tooltip ("Rotation speed for moving the camera")]
    public float rotationSpeed = 200f;
    [Range (0.1f, 1f)]
    [Tooltip ("Rotation speed multiplier when aiming")]
    public float aimingRotationMultiplier = 0.4f;

    [Header ("Movement")]
    [Tooltip ("Max movement speed when grounded (when not sprinting)")]
    public float maxSpeedOnGround = 10f;
    [Tooltip ("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;
    [Tooltip ("Max movement speed when crouching")]
    [Range (0, 1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip ("Max movement speed when not grounded")]
    public float maxSpeedInAir = 10f;
    [Tooltip ("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip ("Multiplicator for the sprint speed (based on grounded speed)")]
    public float sprintSpeedModifier = 2f;
    [Tooltip ("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    [Header ("Stance")]
    [Tooltip ("Ratio (0-1) of the character height where the camera will be at")]
    public float cameraHeightRatio = 0.9f;
    [Tooltip ("Height of character when standing")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip ("Height of character when crouching")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip ("Speed of crouching transitions")]
    public float crouchingSharpness = 10f;

    // ============================
    // CUSTOM CLIMBING VARIABLES
    Ray ray;
    [Header ("Climbing")]
    public float climbRayRange = 1f;
    // ============================

    public UnityAction<bool> onStanceChanged;

    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; set; }
    public bool hasJumpedThisFrame { get; set; }
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

    public Health m_Health { get; set; }
    PlayerInputHandler m_InputHandler;
    CharacterController m_Controller;
    PlayerClimbController m_ClimbController;
    PlayerWalkController m_WalkController;
    PlayerWeaponsManager m_WeaponsManager;
    Actor m_Actor;
    public Vector3 m_GroundNormal { get; set; }
    public Vector3 m_LatestImpactSpeed { get; set; }
    public float m_LastTimeJumped { get; set; } = 0f;
    float m_CameraVerticalAngle = 0f;
    float m_TargetCharacterHeight;

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

        m_Controller.enableOverlapRecovery = true;

        m_Health.onDie += OnDie;

        // force the crouch state to false when starting
        SetCrouchingState (false, true);
        UpdateCharacterHeight (true);
    }

    void Update () {
        // check for Y kill
        if (!isDead && transform.position.y < killHeight) {
            m_Health.Kill ();
        }

        hasJumpedThisFrame = false;

        // crouching
        if (m_InputHandler.GetCrouchInputDown ()) {
            SetCrouchingState (!isCrouching, false);
        }

        UpdateCharacterHeight (false);

        HandleCharacterMovement ();
    }

    void OnDie () {
        isDead = true;

        // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
        m_WeaponsManager.SwitchToWeaponIndex (-1, true);
    }

    void HandleCharacterMovement () {
        // horizontal character rotation
        {
            // rotate the transform with the input speed around its local Y axis
            transform.Rotate (new Vector3 (0f, (m_InputHandler.GetLookInputsHorizontal () * rotationSpeed * RotationMultiplier), 0f), Space.Self);
        }

        // vertical camera rotation
        {
            // add vertical inputs to the camera's vertical angle
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical () * rotationSpeed * RotationMultiplier;

            // limit the camera's vertical angle to min/max
            m_CameraVerticalAngle = Mathf.Clamp (m_CameraVerticalAngle, -89f, 89f);

            // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
            playerCamera.transform.localEulerAngles = new Vector3 (m_CameraVerticalAngle, 0, 0);
        }

        // =========================
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
            if (Input.GetKeyUp (KeyCode.Mouse1) || m_InputHandler.GetSprintInputHeld () || m_InputHandler.GetJumpInputDown () ||
                (!Physics.Raycast (transform.position, transform.forward, out raycastHit, climbRayRange) || raycastHit.transform.tag != "Climbable")) {
                isClimbing = false;
                m_ClimbController.enabled = false;
                m_WalkController.enabled = true;
                characterVelocity = Vector3.up * 5f;
            }
        }
        // =========================

    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    public Vector3 GetCapsuleBottomHemisphere () {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    public Vector3 GetCapsuleTopHemisphere (float atHeight) {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    void UpdateCharacterHeight (bool force) {
        // Update height instantly
        if (force) {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight) {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp (m_Controller.height, m_TargetCharacterHeight, crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp (playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
    }

    // returns false if there was an obstruction
    public bool SetCrouchingState (bool crouched, bool ignoreObstructions) {
        // set appropriate heights
        if (crouched) {
            m_TargetCharacterHeight = capsuleHeightCrouching;
        } else {
            // Detect obstructions
            if (!ignoreObstructions) {
                Collider[] standingOverlaps = Physics.OverlapCapsule (
                    GetCapsuleBottomHemisphere (),
                    GetCapsuleTopHemisphere (capsuleHeightStanding),
                    m_Controller.radius, -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps) {
                    if (c != m_Controller) {
                        return false;
                    }
                }
            }

            m_TargetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null) {
            onStanceChanged.Invoke (crouched);
        }

        isCrouching = crouched;
        return true;
    }
}