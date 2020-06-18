using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerTeleporter : MonoBehaviour {

    private CharacterController m_Controller;
    private PlayerCharacterController m_PlayerController;
    private PlayerWalkController m_WalkController;
    private TextMeshProUGUI useCounter;

    public AudioClip teleportSound;
    public AudioClip failSound;
    public int teleportUses = 0;
    public float teleportRange = 50f;
    public float teleportSpeed = 60f;

    private RaycastHit lastRaycastHit;
    private bool isTeleporting = false;

    void Start () {
        m_Controller = GetComponent<CharacterController> ();
        m_PlayerController = GetComponent<PlayerCharacterController> ();
        m_WalkController = GetComponent<PlayerWalkController> ();
        useCounter = GameObject.Find ("TeleportCounter").GetComponent<TextMeshProUGUI> ();
        useCounter.SetText ("»» " + teleportUses.ToString ());
    }

    void Update () {
        if (!isTeleporting) {
            if (Input.GetKeyDown (KeyCode.E)) {
                if (teleportUses > 0 && GetLookedAtObject () != null) {
                    StartTeleport ();
                } else if (failSound != null) {
                    AudioSource.PlayClipAtPoint (failSound, transform.position);
                }
            }
        } else {
            MoveStep ();
        }
    }

    private GameObject GetLookedAtObject () {
        Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        if (Physics.Raycast (ray, out lastRaycastHit, teleportRange))
            return lastRaycastHit.collider.gameObject;
        else
            return null;
    }

    private void StartTeleport () {
        m_Controller.enabled = false;
        m_PlayerController.enabled = false;
        m_WalkController.enabled = false;
        isTeleporting = true;
        teleportUses--;
        useCounter.SetText ("»» " + teleportUses.ToString ());
        if (teleportSound != null)
            AudioSource.PlayClipAtPoint (teleportSound, transform.position);
    }

    private void MoveStep () {
        Vector3 target = lastRaycastHit.point + lastRaycastHit.normal;

        if (Vector3.Distance (transform.position, target) < .05f) {
            EndTeleport ();
        } else {
            transform.position = Vector3.MoveTowards (transform.position, target, teleportSpeed * Time.deltaTime);
        }
    }

    private void EndTeleport () {
        m_Controller.enabled = true;
        m_PlayerController.enabled = true;
        m_WalkController.enabled = true;
        isTeleporting = false;
        m_WalkController.characterVelocity = Camera.main.transform.forward;
    }
}