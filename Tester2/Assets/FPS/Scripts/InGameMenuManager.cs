﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenuManager : MonoBehaviour {
    [Tooltip ("Root GameObject of the menu used to toggle its activation")]
    public GameObject menuRoot;
    [Tooltip ("Master volume when menu is open")]
    [Range (0.001f, 1f)]
    public float volumeWhenMenuOpen = 0.5f;
    [Tooltip ("Slider component for look sensitivity")]
    public Slider lookSensitivitySlider;
    [Tooltip ("Toggle component for shadows")]
    public Toggle shadowsToggle;
    [Tooltip ("Toggle component for invincibility")]
    public Toggle invincibilityToggle;
    [Tooltip ("Button component for restarting level")]
    public Button restartButton;

    PlayerInputHandler m_PlayerInputsHandler;
    Health m_PlayerHealth;

    void Start () {
        m_PlayerInputsHandler = FindObjectOfType<PlayerInputHandler> ();
        DebugUtility.HandleErrorIfNullFindObject<PlayerInputHandler, InGameMenuManager> (m_PlayerInputsHandler, this);

        m_PlayerHealth = m_PlayerInputsHandler.GetComponent<Health> ();
        DebugUtility.HandleErrorIfNullGetComponent<Health, InGameMenuManager> (m_PlayerHealth, this, gameObject);

        menuRoot.SetActive (false);

        lookSensitivitySlider.value = m_PlayerInputsHandler.lookSensitivity;
        lookSensitivitySlider.onValueChanged.AddListener (OnMouseSensitivityChanged);

        shadowsToggle.isOn = QualitySettings.shadows != ShadowQuality.Disable;
        shadowsToggle.onValueChanged.AddListener (OnShadowsChanged);

        invincibilityToggle.isOn = m_PlayerHealth.invincible;
        invincibilityToggle.onValueChanged.AddListener (OnInvincibilityChanged);

        restartButton.onClick.AddListener (OnLevelRestart);
    }

    private void Update () {
        // Lock cursor when clicking outside of menu
        if (!menuRoot.activeSelf && Input.GetMouseButtonDown (0)) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetKeyDown (KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetButtonDown (GameConstants.k_ButtonNamePauseMenu) ||
            (menuRoot.activeSelf && Input.GetButtonDown (GameConstants.k_ButtonNameCancel))) {
            SetPauseMenuActivation (!menuRoot.activeSelf);
        }

        if (Input.GetAxisRaw (GameConstants.k_AxisNameVertical) != 0) {
            if (EventSystem.current.currentSelectedGameObject == null) {
                EventSystem.current.SetSelectedGameObject (null);
                lookSensitivitySlider.Select ();
            }
        }
    }

    public void ClosePauseMenu () {
        SetPauseMenuActivation (false);
    }

    void SetPauseMenuActivation (bool active) {
        menuRoot.SetActive (active);

        if (menuRoot.activeSelf) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            AudioUtility.SetMasterVolume (volumeWhenMenuOpen);

            EventSystem.current.SetSelectedGameObject (null);
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            AudioUtility.SetMasterVolume (1);
        }

    }

    void OnMouseSensitivityChanged (float newValue) {
        m_PlayerInputsHandler.lookSensitivity = newValue;
    }

    void OnShadowsChanged (bool newValue) {
        QualitySettings.shadows = newValue ? ShadowQuality.All : ShadowQuality.Disable;
    }

    void OnInvincibilityChanged (bool newValue) {
        m_PlayerHealth.invincible = newValue;
    }

    void OnLevelRestart () {
        ClosePauseMenu ();
        SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
    }
}