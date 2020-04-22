using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class PlayerMovementController : MonoBehaviour {

    protected CharacterController m_Controller;
    protected PlayerInputHandler m_InputHandler;

    [Header ("References")]
    [Tooltip ("Reference to the main camera used for the player")]
    public GameObject playerCamera;

    [Header ("General")]
    [Tooltip ("Force applied downward when in the air")]
    public float gravityDownForce = 14f;
    [Tooltip ("distance from the bottom of the character controller capsule to test for grounded")]
    public float groundCheckDistance = 0.05f;
    [Tooltip ("Physic layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip ("Ratio (0-1) of the character height where the camera will be at")]
    public float cameraHeightRatio = 0.9f;

    public Vector3 characterVelocity { get; set; }
    public float m_TargetCharacterHeight { get; set; }
    public bool isGrounded { get; protected set; }
    protected Vector3 m_GroundNormal;
    protected Vector3 m_LatestImpactSpeed;
    protected float m_LastTimeJumped = 0f;
    protected bool hasJumpedThisFrame;

    protected const float k_GroundCheckDistanceInAir = 0.07f;
    protected const float k_JumpGroundingPreventionTime = 0.2f;

    // Start is called before the first frame update
    void Start () {
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerMovementController> (m_Controller, this, gameObject);
        m_InputHandler = GetComponent<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerMovementController> (m_InputHandler, this, gameObject);
        InitController ();

        m_Controller.enableOverlapRecovery = true;
        UpdateCharacterHeight (true);
    }

    virtual protected void InitController () { }

    // Update is called once per frame
    void Update () {
        HandleFirstUpdate ();
        UpdateCharacterHeight (false);

        hasJumpedThisFrame = false;
        bool wasGrounded = isGrounded;
        GroundCheck ();
        // landing
        if (isGrounded && !wasGrounded) {
            HandleLanding ();
        }

        HandleCharacterMovement ();
        HandleLastUpdate ();
    }

    virtual protected void HandleFirstUpdate () { }

    virtual protected void HandleLanding () { }

    abstract protected void HandleCharacterMovement ();

    virtual protected void HandleLastUpdate () { }

    public void GroundCheck () {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime) {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast (GetCapsuleBottomHemisphere (), GetCapsuleTopHemisphere (m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore)) {
                // storing the upward direction for the surface found
                m_GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot (hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit (m_GroundNormal)) {
                    isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > m_Controller.skinWidth) {
                        m_Controller.Move (Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    private bool IsNormalUnderSlopeLimit (Vector3 normal) {
        return Vector3.Angle (transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope (Vector3 direction, Vector3 slopeNormal) {
        Vector3 directionRight = Vector3.Cross (direction, transform.up);
        return Vector3.Cross (slopeNormal, directionRight).normalized;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    public Vector3 GetCapsuleBottomHemisphere () {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    public Vector3 GetCapsuleTopHemisphere (float atHeight) {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    virtual protected void UpdateCharacterHeight (bool force) {
        // Update height instantly
        if (force) {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight) {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp (m_Controller.height, m_TargetCharacterHeight, 10f * Time.deltaTime); // 10f: what was earlier "crouching sharpness", see below as well
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp (playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, 10f * Time.deltaTime);
        }
    }
}