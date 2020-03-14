using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBirdController : MonoBehaviour {

    PlayerInputHandler m_InputHandler;
    float m_CameraVerticalAngle = 0f;
    float m_CameraHorisontalAngle = 0f;

    [Header ("References")]
    [Tooltip ("Reference to the main camera used for the player")]
    public Camera birdCamera;
    [Tooltip ("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;

    [Header ("Rotation")]
    [Tooltip ("Rotation speed for moving the camera")]
    public float verticalRotationSpeed = 70f;
    public float horisontalRotationSpeed = 100f;

    // cleanup! E.g. import file with Important GameObjects instead of many of the Find calls
    private GameObject player;
    private GameObject birdPlayer;

    // Start is called before the first frame update
    void Awake () {
        birdPlayer = GameObject.Find ("BirdPlayer");
        player = birdPlayer.transform.parent.transform.Find ("Player").gameObject;
    }

    void Start () {
        m_InputHandler = GetComponent<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerBirdController> (m_InputHandler, this, gameObject);
    }

    public void InitTransform (Transform bird) {
        transform.position = new Vector3 (bird.position.x, bird.position.y + 0.5f, bird.position.z);
        Vector3 crowRotation = bird.localEulerAngles;
        //transform.localEulerAngles = new Vector3 (crowRotation.x, crowRotation.y, 0);
        m_CameraHorisontalAngle = crowRotation.y;
        m_CameraVerticalAngle = crowRotation.x;
    }

    // Update is called once per frame
    void Update () {
        updateCameraRotation ();
        handleCharacterMovement ();

        // SHIFTING BACK TO HUMAN FORM
        if (Input.GetKeyDown (KeyCode.E)) {
            revertToHuman ();
        }
    }

    private void updateCameraRotation () {
        // horizontal character rotation
        // rotate the transform with the input speed around its local Y axis
        m_CameraHorisontalAngle += m_InputHandler.GetLookInputsHorizontal () * horisontalRotationSpeed;
        // vertical camera rotation
        // add vertical inputs to the camera's vertical angle
        m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical () * verticalRotationSpeed;
        // limit the camera's vertical angle to min/max
        m_CameraVerticalAngle = Mathf.Clamp (m_CameraVerticalAngle, -89f, 89f);
        // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
        transform.localEulerAngles = new Vector3 (m_CameraVerticalAngle, m_CameraHorisontalAngle, 0);
    }

    private void handleCharacterMovement () {
        // to do
    }

    private void revertToHuman () {
        player.SetActive (true);
        birdPlayer.SetActive (false);

        player.GetComponent<PlayerCharacterController> ().TransformTo (transform);

        GameObject[] critterSpawners = GameObject.FindGameObjectsWithTag ("CritterSpawner");
        Camera normalCamera = GameObject.FindWithTag ("MainCamera").GetComponent<Camera> ();
        foreach (GameObject spawner in critterSpawners) {
            spawner.GetComponent<lb_BirdController> ().ChangeCamera (normalCamera);
        }
    }
}