using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler), typeof (AudioSource))]
public class PlayerWalkController : MonoBehaviour {

    PlayerCharacterController m_PlayerController;
    CharacterController m_Controller;
    PlayerInputHandler m_InputHandler;

    [Header ("General")]
    [Tooltip ("Force applied downward when in the air")]
    public float gravityDownForce = 14f;
    [Tooltip ("distance from the bottom of the character controller capsule to test for grounded")]
    public float groundCheckDistance = 0.05f;
    [Tooltip ("Physic layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;

    [Tooltip ("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;

    [Header ("Movement")]
    [Tooltip ("Max movement speed when grounded (when not sprinting)")]
    public float maxSpeedOnGround = 6f;
    [Tooltip ("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;
    [Tooltip ("Max movement speed when crouching")]
    [Range (0, 1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip ("Max movement speed when not grounded")]
    public float maxSpeedInAir = 6f;
    [Tooltip ("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip ("Multiplicator for the sprint speed (based on grounded speed)")]
    public float sprintSpeedModifier = 1.5f;
    [Tooltip ("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    [Header ("Jump")]
    [Tooltip ("Force applied upward when jumping")]
    public float jumpForce = 7f;

    [Header ("Fall Damage")]
    [Tooltip ("Whether the player will recieve damage when hitting the ground at high speed")]
    public bool recievesFallDamage = true;
    [Tooltip ("Minimun fall speed for recieving fall damage")]
    public float minSpeedForFallDamage = 10f;
    [Tooltip ("Fall speed for recieving th emaximum amount of fall damage")]
    public float maxSpeedForFallDamage = 30f;
    [Tooltip ("Damage recieved when falling at the mimimum speed")]
    public float fallDamageAtMinSpeed = 10f;
    [Tooltip ("Damage recieved when falling at the maximum speed")]
    public float fallDamageAtMaxSpeed = 50f;

    [Header ("Audio")]
    [Tooltip ("Amount of footstep sounds played when moving one meter")]
    public float footstepSFXFrequency = 1f;
    [Tooltip ("Amount of footstep sounds played when moving one meter while sprinting")]
    public float footstepSFXFrequencyWhileSprinting = 1f;
    [Tooltip ("Sound played for footsteps")]
    public AudioClip footstepSFX;
    [Tooltip ("Sound played when jumping")]
    public AudioClip jumpSFX;
    [Tooltip ("Sound played when landing")]
    public AudioClip landSFX;
    [Tooltip ("Sound played when taking damage froma fall")]
    public AudioClip fallDamageSFX;

    Vector3 m_CharacterVelocity;
    float m_footstepDistanceCounter;

    const float k_GroundCheckDistanceInAir = 0.07f;
    const float k_JumpGroundingPreventionTime = 0.2f;

    // Start is called before the first frame update
    void Start () {
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerWalkController> (m_Controller, this, gameObject);

        m_PlayerController = GetComponent<PlayerCharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerWalkController> (m_PlayerController, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWalkController> (m_InputHandler, this, gameObject);
    }

    // Update is called once per frame
    void Update () {

        bool wasGrounded = m_PlayerController.isGrounded;
        GroundCheck ();

        // landing
        if (m_PlayerController.isGrounded && !wasGrounded) {
            // Fall damage
            float fallSpeed = -Mathf.Min (m_PlayerController.characterVelocity.y, m_PlayerController.m_LatestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - minSpeedForFallDamage) / (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f) {
                /* float dmgFromFall = Mathf.Lerp (fallDamageAtMinSpeed, fallDamageAtMaxSpeed, fallSpeedRatio);
                m_PlayerController.m_Health.TakeDamage (dmgFromFall, null); */

                // fall damage SFX
                audioSource.PlayOneShot (fallDamageSFX);
            } else {
                // land SFX
                audioSource.PlayOneShot (landSFX);
            }
        }

        // character movement handling
        bool isSprinting = m_InputHandler.GetSprintInputHeld (); {
            if (isSprinting) {
                isSprinting = m_PlayerController.SetCrouchingState (false, false);
            }

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on our character's transform orientation
            Vector3 worldspaceMoveInput = transform.TransformVector (m_InputHandler.GetMoveInput ());

            // handle grounded movement
            if (m_PlayerController.isGrounded) {
                // calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;
                // reduce speed if crouching by crouch speed ratio
                if (m_PlayerController.isCrouching)
                    targetVelocity *= maxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope (targetVelocity.normalized, m_PlayerController.m_GroundNormal) * targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                m_PlayerController.characterVelocity = Vector3.Lerp (m_PlayerController.characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

                // jumping
                if (m_PlayerController.isGrounded && m_InputHandler.GetJumpInputDown ()) {
                    // force the crouch state to false
                    if (m_PlayerController.SetCrouchingState (false, false)) {
                        // start by canceling out the vertical component of our velocity
                        m_PlayerController.characterVelocity = new Vector3 (m_PlayerController.characterVelocity.x, 0f, m_PlayerController.characterVelocity.z);

                        // then, add the jumpSpeed value upwards
                        m_PlayerController.characterVelocity += Vector3.up * jumpForce;

                        // play sound
                        audioSource.PlayOneShot (jumpSFX);

                        // remember last time we jumped because we need to prevent snapping to ground for a short time
                        m_PlayerController.m_LastTimeJumped = Time.time;
                        m_PlayerController.hasJumpedThisFrame = true;

                        // Force grounding to false
                        m_PlayerController.isGrounded = false;
                        m_PlayerController.m_GroundNormal = Vector3.up;
                    }
                }

                // footsteps sound
                float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
                if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency) {
                    m_footstepDistanceCounter = 0f;
                    audioSource.PlayOneShot (footstepSFX);
                }

                // keep track of distance traveled for footsteps sound
                m_footstepDistanceCounter += m_PlayerController.characterVelocity.magnitude * Time.deltaTime;
            }
            // handle air movement
            else {
                if (!m_PlayerController.isClimbing) {
                    // add air acceleration
                    m_PlayerController.characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

                    // limit air speed to a maximum, but only horizontally
                    float verticalVelocity = m_PlayerController.characterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane (m_PlayerController.characterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude (horizontalVelocity, maxSpeedInAir * speedModifier);
                    m_PlayerController.characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    // apply the gravity to the velocity
                    m_PlayerController.characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
                }
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = m_PlayerController.GetCapsuleBottomHemisphere ();
        Vector3 capsuleTopBeforeMove = m_PlayerController.GetCapsuleTopHemisphere (m_Controller.height);
        if (!m_PlayerController.isClimbing) {
            m_Controller.Move (m_PlayerController.characterVelocity * Time.deltaTime);
        }

        // detect obstructions to adjust velocity accordingly
        m_PlayerController.m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast (capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, m_PlayerController.characterVelocity.normalized, out RaycastHit hit, m_PlayerController.characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore)) {
            // We remember the last impact speed because the fall damage logic might need it
            m_PlayerController.m_LatestImpactSpeed = m_PlayerController.characterVelocity;

            m_PlayerController.characterVelocity = Vector3.ProjectOnPlane (m_PlayerController.characterVelocity, hit.normal);
        }
    }

    void GroundCheck () {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = m_PlayerController.isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        m_PlayerController.isGrounded = false;
        m_PlayerController.m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= m_PlayerController.m_LastTimeJumped + k_JumpGroundingPreventionTime) {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast (m_PlayerController.GetCapsuleBottomHemisphere (), m_PlayerController.GetCapsuleTopHemisphere (m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore)) {
                // storing the upward direction for the surface found
                m_PlayerController.m_GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot (hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit (m_PlayerController.m_GroundNormal)) {
                    m_PlayerController.isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > m_Controller.skinWidth) {
                        m_Controller.Move (Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit (Vector3 normal) {
        return Vector3.Angle (transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope (Vector3 direction, Vector3 slopeNormal) {
        Vector3 directionRight = Vector3.Cross (direction, transform.up);
        return Vector3.Cross (slopeNormal, directionRight).normalized;
    }
}