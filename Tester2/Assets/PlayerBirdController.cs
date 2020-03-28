using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler), typeof (AudioSource))]
public class PlayerBirdController : PlayerMovementController {

    [Header ("References")]
    [Tooltip ("Audio source for them hot wings...")]
    public AudioSource audioSource;

    [Header ("Rotation")]
    public float verticalRotationSpeed = 70f;
    public float horisontalRotationSpeed = 100f;

    float m_CameraVerticalAngle = 0f;
    float m_CameraHorisontalAngle = 0f;

    [Header ("Flying")]
    [Tooltip ("Force applied upward when \"jumping\"")]
    public float jumpForce = 7f;
    [Tooltip ("Max movement speed when not grounded")]
    public float maxSpeedInAir = 6f;
    [Tooltip ("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip ("Minimum time between \"jumps\"")]
    public int timeBetweenJumps = 10;

    [Header ("Ground Movement")]
    [Tooltip ("Height of character when on ground")]
    public float capsuleHeight = 1.0f;
    [Tooltip ("Max movement speed when grounded")]
    public float maxSpeedOnGround = 6f;
    [Tooltip ("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;

    [Header ("Audio")]
    [Tooltip ("Sound played when flapping wings")]
    public AudioClip wingSFX;
    /* [Tooltip ("Sound played when landing")]
    public AudioClip landSFX;
    [Tooltip ("Sound played when hitting a surface")]
    public AudioClip damageSFX; */

    // cleanup! E.g. import file with Important GameObjects instead of many of the Find calls
    private GameObject player;
    private GameObject birdCamMaster;

    public void InitTransform (Transform bird, Vector3 velocityAtTransformation) {
        transform.position = new Vector3 (bird.position.x, bird.position.y + 0.3f, bird.position.z);
        Vector3 crowRotation = bird.localEulerAngles;

        m_CameraHorisontalAngle = crowRotation.y;
        transform.localEulerAngles = new Vector3 (0, m_CameraHorisontalAngle, 0);
        // fixing weird issue with vertical rotation where it went above 90 and the camera clamp would break the desired rotation:
        // see UpdateCameraRotation for further explanation
        m_CameraVerticalAngle = MyGameUtils.LimitVectorAngleTo90 (crowRotation.x);

        birdCamMaster = transform.parent.gameObject;
        player = birdCamMaster.transform.parent.transform.Find ("Player").gameObject;
        birdCamMaster.SetActive (true);
        player.SetActive (false);

        characterVelocity = velocityAtTransformation;

        UpdateSpawnPoints ();
    }

    // Is called before the first frame update
    override protected void InitController () {
        m_TargetCharacterHeight = capsuleHeight;
    }

    // Update is called once per frame
    override protected void HandleFirstUpdate () {
        UpdateCameraRotation ();
    }

    override protected void HandleLastUpdate () {
        if (Input.GetKeyDown (KeyCode.E)) {
            RevertToHuman ();
        }
    }

    float camGroundingIntensity = 0.01f;

    private void UpdateCameraRotation () {
        // vertical rotation
        m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical () * verticalRotationSpeed;
        // limit the vertical angle to min/max
        m_CameraVerticalAngle = isGrounded ?
            Mathf.Lerp (m_CameraVerticalAngle, 10f, camGroundingIntensity) :
            Mathf.Clamp (m_CameraVerticalAngle, -89f, 89f);
        camGroundingIntensity = isGrounded ? Mathf.Lerp (camGroundingIntensity, 1f, 0.03f) : 0.01f;

        // horizontal rotation
        m_CameraHorisontalAngle += m_InputHandler.GetLookInputsHorizontal () * horisontalRotationSpeed;
        transform.localEulerAngles = new Vector3 (m_CameraVerticalAngle, m_CameraHorisontalAngle, 0);
    }

    override protected void HandleCharacterMovement () {
        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldspaceMoveInput = transform.TransformVector (m_InputHandler.GetMoveInput ());

        if (isGrounded) {
            HandleGroundMovement (worldspaceMoveInput);
        } else {
            HandleAirMovement (worldspaceMoveInput);
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere ();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere (m_Controller.height);
        m_Controller.Move (characterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast (capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore)) {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane (characterVelocity, hit.normal);
        }
    }

    private void HandleGroundMovement (Vector3 worldspaceMoveInput) {
        // calculate the desired velocity from inputs, max speed, and current slope
        Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround;

        targetVelocity = GetDirectionReorientedOnSlope (targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

        // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
        characterVelocity = Vector3.Lerp (characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

        // jumping
        if (isGrounded && m_InputHandler.GetJumpInputDown ()) {
            m_TargetCharacterHeight = capsuleHeight;

            // start by canceling out the vertical component of our velocity
            characterVelocity = new Vector3 (characterVelocity.x, 0f, characterVelocity.z);

            // then, add the jumpSpeed value upwards
            characterVelocity += Vector3.up * jumpForce;

            // play sound
            audioSource.PlayOneShot (wingSFX);

            // remember last time we jumped because we need to prevent snapping to ground for a short time
            m_LastTimeJumped = Time.time;
            hasJumpedThisFrame = true;

            // Force grounding to false
            isGrounded = false;
            m_GroundNormal = Vector3.up;
        }
    }

    long timeSinceLastJump = 0;

    private void HandleAirMovement (Vector3 worldspaceMoveInput) {
        // add air acceleration
        characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

        // limit air speed to a maximum, but only horizontally
        float verticalVelocity = characterVelocity.y;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane (characterVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude (horizontalVelocity, maxSpeedInAir);
        characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

        // flying
        if (timeSinceLastJump >= timeBetweenJumps && m_InputHandler.GetJumpInputDown ()) {

            // then, add the jumpSpeed value upwards
            characterVelocity += Vector3.up * jumpForce * 1.5f;

            // play sound
            audioSource.PlayOneShot (wingSFX);

            // remember last time we jumped because we need to prevent snapping to ground for a short time
            m_LastTimeJumped = Time.time;
            hasJumpedThisFrame = true;
            timeSinceLastJump = 0;
        } else {
            timeSinceLastJump += 1;

            // apply the gravity to the velocity
            if (Input.GetKey (KeyCode.Space)) {
                characterVelocity += Vector3.down * gravityDownForce * 0.5f * Time.deltaTime;
            } else {
                characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
            }
        }
    }

    private void RevertToHuman () {
        player.SetActive (true);
        birdCamMaster.SetActive (false);

        player.GetComponent<PlayerCharacterController> ()
            .TransformTo (transform.position, new Vector3 (playerCamera.transform.localEulerAngles.x, transform.localEulerAngles.y, 0));

        UpdateSpawnPoints ();
    }

    override protected void UpdateCharacterHeight (bool force) {
        // Update height instantly
        if (force) {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.33333f;
            //transform.localPosition += Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight) {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp (m_Controller.height, m_TargetCharacterHeight, 10f * Time.deltaTime); // 10f: what was earlier "crouching sharpness", see below as well
            m_Controller.center = Vector3.up * m_Controller.height * 0.33333f;
            transform.localPosition = Vector3.Lerp (transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, 10f * Time.deltaTime);
        }
    }

    private void UpdateSpawnPoints () {
        GameObject[] critterSpawners = GameObject.FindGameObjectsWithTag ("CritterSpawner");
        Camera newCamera = GameObject.FindWithTag ("MainCamera").GetComponent<Camera> ();
        foreach (GameObject spawner in critterSpawners) {
            spawner.GetComponent<lb_BirdController> ().ChangeCamera (newCamera);
        }
    }
}