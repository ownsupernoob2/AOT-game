using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class SettingsMenuController : MonoBehaviour
{
    public Canvas settingsCanvas;
    public Toggle crosshairToggle;
    public Toggle boostSliderToggle;
    public Toggle snapTurnToggle;
    public Toggle tiltToggle;
    public Toggle vignetteToggle;
    public GameObject vignette;
    public Button exitButton;
    public GameObject leftBlade;
    public GameObject rightBlade;
    public ActionBasedController leftController;
    public PlayerHookManager hookManager;
    public ContinuousTurnProviderBase continuousTurnProvider;
    public SnapTurnProviderBase snapTurnProvider;
    private VRControls controls;
    private bool isMenuOpen = false;

    void Awake()
    {
        controls = new VRControls();
        if (settingsCanvas == null) Debug.LogError("Settings Canvas not assigned!");
        if (crosshairToggle == null) Debug.LogWarning("Crosshair Toggle not assigned!");
        if (boostSliderToggle == null) Debug.LogWarning("Boost Slider Toggle not assigned!");
        if (snapTurnToggle == null) Debug.LogWarning("Snap Turn Toggle not assigned!");
        if (tiltToggle == null) Debug.LogWarning("Tilt Toggle not assigned!");
        if (vignetteToggle == null) Debug.LogWarning("Vignette Toggle not assigned!");
        if (exitButton == null) Debug.LogWarning("Exit Button not assigned!");
        if (leftController == null) Debug.LogWarning("Left Controller not assigned!");
        if (hookManager == null) Debug.LogWarning("Hook Manager not assigned!");
        if (continuousTurnProvider == null) Debug.LogWarning("Continuous Turn Provider not assigned!");
        if (snapTurnProvider == null) Debug.LogWarning("Snap Turn Provider not assigned!");
    }

    void OnEnable()
    {
        if (controls != null) controls.Enable();
        controls.VR.LeftMenu.performed += _ => ToggleMenu();

        if (crosshairToggle != null) crosshairToggle.onValueChanged.AddListener(ToggleCrosshair);
        if (boostSliderToggle != null) boostSliderToggle.onValueChanged.AddListener(ToggleBoostSlider);
        if (snapTurnToggle != null) snapTurnToggle.onValueChanged.AddListener(ToggleSnapTurn);
        if (tiltToggle != null) tiltToggle.onValueChanged.AddListener(ToggleTilt);
        if (vignetteToggle != null) vignetteToggle.onValueChanged.AddListener(ToggleVignette);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
    }

    void OnDisable()
    {
        if (controls != null) controls.Disable();
        controls.VR.LeftMenu.performed -= _ => ToggleMenu();
        if (crosshairToggle != null) crosshairToggle.onValueChanged.RemoveListener(ToggleCrosshair);
        if (boostSliderToggle != null) boostSliderToggle.onValueChanged.RemoveListener(ToggleBoostSlider);
        if (snapTurnToggle != null) snapTurnToggle.onValueChanged.RemoveListener(ToggleSnapTurn);
        if (tiltToggle != null) tiltToggle.onValueChanged.RemoveListener(ToggleTilt);
        if (vignetteToggle != null) vignetteToggle.onValueChanged.RemoveListener(ToggleVignette);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
    }

    void Start()
    {
        if (settingsCanvas != null)
        {
            settingsCanvas.enabled = false;
            settingsCanvas.renderMode = RenderMode.WorldSpace;
            settingsCanvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }
        if (crosshairToggle != null) crosshairToggle.isOn = true;
        if (boostSliderToggle != null) boostSliderToggle.isOn = true;
        if (snapTurnToggle != null) snapTurnToggle.isOn = true;
        if (tiltToggle != null) tiltToggle.isOn = true;
        if (vignetteToggle != null) vignetteToggle.isOn = true;
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (leftBlade != null) leftBlade.SetActive(!isMenuOpen);
        if (rightBlade != null) rightBlade.SetActive(!isMenuOpen);
        if (settingsCanvas != null)
        {
            settingsCanvas.enabled = isMenuOpen;
            Debug.Log($"Settings menu {(isMenuOpen ? "opened" : "closed")}");
        }
        if (isMenuOpen && hookManager != null && hookManager.playerCamera != null)
        {
            settingsCanvas.transform.position = hookManager.playerCamera.position + hookManager.playerCamera.forward * 0.5f;
            settingsCanvas.transform.rotation = Quaternion.LookRotation(settingsCanvas.transform.position - hookManager.playerCamera.position);
        }
    }

    void ToggleCrosshair(bool isEnabled)
    {
        if (hookManager != null)
        {
            hookManager.crosshairEnabled = isEnabled;
            Debug.Log($"Crosshairs {(isEnabled ? "enabled" : "disabled")} in settings");
        }
    }

    void ToggleBoostSlider(bool isEnabled)
    {
        if (hookManager != null)
        {
            if (hookManager.leftBoostCanvas != null) hookManager.leftBoostCanvas.enabled = isEnabled;
            if (hookManager.rightBoostCanvas != null) hookManager.rightBoostCanvas.enabled = isEnabled;
            Debug.Log($"Boost sliders {(isEnabled ? "enabled" : "disabled")}");
        }
    }

    void ToggleSnapTurn(bool isSnapTurn)
    {
        if (continuousTurnProvider != null)
        {
            continuousTurnProvider.enabled = !isSnapTurn;
            Debug.Log($"Continuous turn {(isSnapTurn ? "disabled" : "enabled")}");
        }
        else
        {
            Debug.LogWarning("ContinuousTurnProvider not assigned in SettingsMenuController!");
        }
        if (snapTurnProvider != null)
        {
            snapTurnProvider.enabled = isSnapTurn;
            Debug.Log($"Snap turn {(isSnapTurn ? "enabled" : "disabled")}");
        }
        else
        {
            Debug.LogWarning("SnapTurnProvider not assigned in SettingsMenuController!");
        }
    }

    void ToggleTilt(bool isEnabled)
    {
        if (hookManager != null)
        {
            hookManager.tiltEnabled = isEnabled;
            Debug.Log($"Camera tilt {(isEnabled ? "enabled" : "disabled")}");
        }
    }

    void ToggleVignette(bool isEnabled)
    {
        if (hookManager != null)
        {
            if (vignette != null) vignette.SetActive(isEnabled);
            hookManager.vignetteEnabled = isEnabled;
            Debug.Log($"Tunneling vignette {(isEnabled ? "enabled" : "disabled")}");
        }
    }

    void ExitGame()
    {
        Debug.Log("Exiting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}