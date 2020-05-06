using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler), typeof (AudioSource))]
public class PlayerWalkController : PlayerMovementController {

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

    float m_footstepDistanceCounter;
    public bool isSprinting { get; private set; }

    // Is called before the first frame update
    override protected void InitController () {
        m_PlayerController = GetComponent<PlayerCharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerWalkController> (m_PlayerController, this, gameObject);
    }

    override protected void HandleCharacterMovement () {
        // character movement handling
        if (m_InputHandler.GetSprintInputDown ()) {
            isSprinting = true;
            m_PlayerController.onStanceChanged.Invoke ();
        }
        if (m_InputHandler.GetSprintInputReleased ()) {
            isSprinting = false;
            m_PlayerController.onStanceChanged.Invoke ();
        }
        if (isSprinting) {
            isSprinting = m_PlayerController.SetCrouchingState (false, false);
        }

        float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldspaceMoveInput = transform.TransformVector (m_InputHandler.GetMoveInput ());

        // handle grounded movement
        if (isGrounded) {
            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;
            // reduce speed if crouching by crouch speed ratio
            if (m_PlayerController.isCrouching)
                targetVelocity *= maxSpeedCrouchedRatio;
            targetVelocity = GetDirectionReorientedOnSlope (targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            characterVelocity = Vector3.Lerp (characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

            // jumping
            if (isGrounded && m_InputHandler.GetJumpInputDown ()) {
                // force the crouch state to false
                if (m_PlayerController.SetCrouchingState (false, false)) {
                    // start by canceling out the vertical component of our velocity
                    characterVelocity = new Vector3 (characterVelocity.x, 0f, characterVelocity.z);

                    // then, add the jumpSpeed value upwards
                    characterVelocity += Vector3.up * jumpForce;

                    // play sound
                    audioSource.PlayOneShot (jumpSFX);

                    // remember last time we jumped because we need to prevent snapping to ground for a short time
                    m_LastTimeJumped = Time.time;
                    hasJumpedThisFrame = true;

                    // Force grounding to false
                    isGrounded = false;
                    m_GroundNormal = Vector3.up;
                }
            }

            // footsteps sound
            float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
            if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency) {
                m_footstepDistanceCounter = 0f;
                audioSource.PlayOneShot (footstepSFX);
            }

            // keep track of distance traveled for footsteps sound
            m_footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
        }
        // handle air movement
        else {
            if (!m_PlayerController.isClimbing) {
                // add air acceleration
                characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane (characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude (horizontalVelocity, maxSpeedInAir * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                // apply the gravity to the velocity
                characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere ();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere (m_Controller.height);
        if (!m_PlayerController.isClimbing) {
            m_Controller.Move (characterVelocity * Time.deltaTime);
        }

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast (capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore)) {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane (characterVelocity, hit.normal);
        }
    }

    override protected void HandleLanding () {
        // Fall damage
        float fallSpeed = -Mathf.Min (characterVelocity.y, m_LatestImpactSpeed.y);
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
        m_PlayerController.onStanceChanged.Invoke ();
    }

    override protected void UpdateCharacterHeight (bool force) {
        // Update height instantly
        if (force) {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
            //m_PlayerController.m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight) {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp (m_Controller.height, m_TargetCharacterHeight, m_PlayerController.crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp (playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, m_PlayerController.crouchingSharpness * Time.deltaTime);
            //m_PlayerController.m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
    }
}