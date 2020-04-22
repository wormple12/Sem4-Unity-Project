using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController), typeof (PlayerInputHandler), typeof (AudioSource))]
public class PlayerClimbController : MonoBehaviour {

    CharacterController m_Controller;
    PlayerCharacterController m_PlayerController;
    PlayerInputHandler m_InputHandler;

    [Header ("Climbing")]
    public float climbRayRange = 1f;
    public float climbSpeed = 5f;
    public float stickToWallForce = 5f;

    Ray ray;
    public bool isStable { get; private set; } = false;

    // Start is called before the first frame update
    void Start () {
        enabled = false;

        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerClimbController> (m_Controller, this, gameObject);

        m_PlayerController = GetComponent<PlayerCharacterController> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerClimbController> (m_PlayerController, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerClimbController> (m_InputHandler, this, gameObject);
    }

    // Update is called once per frame
    void Update () {
        float verticalInput = Input.GetAxis ("Vertical");
        isStable = verticalInput < 0.1;
        Vector3 movement = transform.up * verticalInput;
        m_Controller.Move (movement * climbSpeed * Time.deltaTime);
    }
}