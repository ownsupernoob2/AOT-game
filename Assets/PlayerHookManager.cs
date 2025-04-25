using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PlayerHookManager : MonoBehaviour, ITunnelingVignetteProvider
{
    public ActionBasedController leftController;
    public ActionBasedController rightController;
    public Transform playerCamera;
    public Transform cameraOffset;
    public Transform leftWaistAnchor;
    public Transform rightWaistAnchor;
    public GameObject leftCrosshair;
    public GameObject rightCrosshair;
    private Renderer leftCrosshairRenderer;
    private Renderer rightCrosshairRenderer;
    private Material leftCrosshairMaterial;
    private Material rightCrosshairMaterial;
    private ConfigurableJoint leftHookJoint;
    private ConfigurableJoint rightHookJoint;
    public LineRenderer leftHookLine;
    public LineRenderer rightHookLine;
    private Rigidbody playerRigidbody;
    private VRControls controls;
    public bool leftHookAttached = false;
    public bool rightHookAttached = false;
    public bool leftHookRetracting = false;
    public bool rightHookRetracting = false;
    private float boostSpeed = 0.2f;
    private float maxBoost = 200f;
    private float boostDrainRate = 5f;
    private float leftBoostMeter = 250f;
    private float rightBoostMeter = 250f;
    private float lateralOffset = 0.7f;
    private float upwardBoostFactor = 0.1f;
    private float doubleTapTime = 0.5f;
    private float doubleTapBoostCost = 50f;
    private float doubleTapBoostForce = 5f;
    private float lastLeftBoostTime = -1f;
    private float lastRightBoostTime = -1f;
    private float minLimit = 1f;
    private float pullBoostForce = 0.3f;
    private float minGrappleDistance = 2f;
    private float waistHeightOffset = -0.5f;
    private float waistSideOffset = 0.3f;
    private float baseScale = 1f;
    private float minScale = 0.5f;
    private float maxScale = 2.5f;
    private float referenceDistance = 10f;
    public Vector3 leftGrapplePoint;
    public Vector3 rightGrapplePoint;
    public Canvas leftBoostCanvas;
    public Canvas rightBoostCanvas;
    public Slider leftBoostSlider;
    public Slider rightBoostSlider;
    public LayerMask groundLayer;
    private float groundCheckDistance = 0.1f;
    public ParticleSystem leftSparks;  // Used as a prefab for instantiation
    public ParticleSystem rightSparks; // Used as a prefab for instantiation
    public ParticleSystem leftGas;    // Used as a prefab for gas emission during boost
    public ParticleSystem rightGas;   // Used as a prefab for gas emission during boost
    private float sparkDuration = 0.3f; // Maximum duration for sparks
    private float gasDuration = 0.5f;  // Maximum duration for gas emission
    private float originalLeftDistance;
    private float originalRightDistance;
    public bool crosshairEnabled = true;
    public bool tiltEnabled = true;
    public bool vignetteEnabled = true;
    // Tilt parameters
    private float maxPitchAngle = 20f;
    private float pitchTiltFactor = 2f;
    private float maxRollAngle = 15f;
    private float rollVelocityFactor = 2f;
    private float tiltSmoothSpeed = 20f;
    private float flipTorque = 2f;
    private float currentPitchAngle = 0f;
    private float currentRollAngle = 0f;
    private VignetteParameters _vignetteParameters;
    private float velocityThreshold = 0.5f;
    private bool isMoving = false;
    private TunnelingVignetteController vignetteController;
    // Aim assist parameters
    private float aimAssistAngleThreshold = 10f;
    private float aimAssistSearchRadius = 50f;
    // Store aim assist positions for grappling
    private Vector3 leftAimAssistPosition;
    private Vector3 rightAimAssistPosition;
    private bool leftUseAimAssist = false;
    private bool rightUseAimAssist = false;
    // Turn control
    private ContinuousTurnProviderBase continuousTurnProvider;
    private SnapTurnProviderBase snapTurnProvider;
    private bool snapTurnEnabled = false; // Tracks whether snap turn is enabled

    void Awake()
    {
        controls = new VRControls();
        playerRigidbody = GetComponent<Rigidbody>();
        continuousTurnProvider = GetComponent<ContinuousTurnProviderBase>();
        snapTurnProvider = GetComponent<SnapTurnProviderBase>();
        if (playerRigidbody == null) Debug.LogError("[PHM:Awake] Player Rigidbody not found! Attach this script to the XR Rig root with a Rigidbody, frame=" + Time.frameCount);
        if (leftController == null) Debug.LogWarning("[PHM:Awake] LeftController is null, frame=" + Time.frameCount);
        if (rightController == null) Debug.LogWarning("[PHM:Awake] RightController is null, frame=" + Time.frameCount);
        if (playerCamera == null) Debug.LogWarning("[PHM:Awake] PlayerCamera is null, frame=" + Time.frameCount);
        if (cameraOffset == null) Debug.LogWarning("[PHM:Awake] CameraOffset is null, frame=" + Time.frameCount);
        if (leftWaistAnchor == null || rightWaistAnchor == null) Debug.LogWarning("[PHM:Awake] Waist anchors not assigned, frame=" + Time.frameCount);
        if (leftCrosshair == null || rightCrosshair == null) Debug.LogWarning("[PHM:Awake] Crosshairs not assigned, frame=" + Time.frameCount);
        if (leftBoostCanvas == null || rightBoostCanvas == null) Debug.LogWarning("[PHM:Awake] Boost canvases not assigned, frame=" + Time.frameCount);
        if (leftBoostSlider == null || rightBoostSlider == null) Debug.LogWarning("[PHM:Awake] Boost sliders not assigned, frame=" + Time.frameCount);
        if (leftSparks == null || rightSparks == null) Debug.LogWarning("[PHM:Awake] Sparks particle systems not assigned, frame=" + Time.frameCount);
        if (leftGas == null || rightGas == null) Debug.LogWarning("[PHM:Awake] Gas particle systems not assigned, frame=" + Time.frameCount);
        if (continuousTurnProvider == null) Debug.LogWarning("[PHM:Awake] ContinuousTurnProvider not found on this GameObject, frame=" + Time.frameCount);
        else Debug.Log("[PHM:Awake] ContinuousTurnProvider found: " + continuousTurnProvider.GetType().Name + ", frame=" + Time.frameCount);
        if (snapTurnProvider == null) Debug.LogWarning("[PHM:Awake] SnapTurnProvider not found on this GameObject, frame=" + Time.frameCount);
        else Debug.Log("[PHM:Awake] SnapTurnProvider found: " + snapTurnProvider.GetType().Name + ", frame=" + Time.frameCount);
        leftCrosshairRenderer = leftCrosshair != null ? leftCrosshair.GetComponentInChildren<Renderer>() : null;
        rightCrosshairRenderer = rightCrosshair != null ? rightCrosshair.GetComponentInChildren<Renderer>() : null;
        if (leftCrosshairRenderer == null || rightCrosshairRenderer == null)
            Debug.LogWarning("[PHM:Awake] Crosshair renderers not found! Ensure crosshairs have Renderer components (MeshRenderer or SpriteRenderer), frame=" + Time.frameCount);
        else
        {
            leftCrosshairRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rightCrosshairRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leftCrosshairRenderer.receiveShadows = false;
            rightCrosshairRenderer.receiveShadows = false;
            leftCrosshairMaterial = leftCrosshairRenderer.sharedMaterial;
            rightCrosshairMaterial = rightCrosshairRenderer.sharedMaterial;
            if (leftCrosshairMaterial == null || rightCrosshairMaterial == null)
                Debug.LogWarning("[PHM:Awake] Crosshair materials not found! Ensure renderers have valid materials, frame=" + Time.frameCount);
        }
        vignetteController = FindObjectOfType<TunnelingVignetteController>();
        if (vignetteController == null) Debug.LogWarning("[PHM:Awake] TunnelingVignetteController not found, frame=" + Time.frameCount);
        _vignetteParameters = new VignetteParameters
        {
            apertureSize = 0.7f,
            featheringEffect = 0.2f,
            easeInTime = 0.3f,
            easeOutTime = 0.3f,
            easeInTimeLock = false,
            easeOutDelayTime = 0f,
            vignetteColor = Color.black,
            vignetteColorBlend = Color.black,
            apertureVerticalPosition = 0f
        };
        foreach (var stabilizer in GetComponentsInChildren<XRTransformStabilizer>())
        {
            if (stabilizer.enabled)
            {
                stabilizer.enabled = false;
                Debug.Log("[PHM:Awake] Disabled XRTransformStabilizer on " + stabilizer.gameObject.name + " to prevent potential errors, frame=" + Time.frameCount);
            }
        }
    }

    void OnEnable()
    {
        if (controls != null)
        {
            controls.Enable();
            Debug.Log("[PHM:OnEnable] VRControls enabled, frame=" + Time.frameCount);
        }
    }

    void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
            Debug.Log("[PHM:OnDisable] VRControls disabled, frame=" + Time.frameCount);
        }
    }

    void Start()
    {
        if (leftHookLine != null)
        {
            leftHookLine.positionCount = 2;
            leftHookLine.enabled = false;
            Debug.Log("[PHM:Start] Left hook line initialized: SetActive(false), frame=" + Time.frameCount);
        }
        if (rightHookLine != null)
        {
            rightHookLine.positionCount = 2;
            rightHookLine.enabled = false;
            Debug.Log("[PHM:Start] Right hook line initialized: SetActive(false), frame=" + Time.frameCount);
        }
        if (leftBoostSlider != null)
        {
            leftBoostSlider.maxValue = maxBoost;
            leftBoostSlider.value = leftBoostMeter;
            Debug.Log("[PHM:Start] Left boost slider initialized, maxValue=" + maxBoost + ", value=" + leftBoostMeter + ", frame=" + Time.frameCount);
        }
        if (rightBoostSlider != null)
        {
            rightBoostSlider.maxValue = maxBoost;
            rightBoostSlider.value = rightBoostMeter;
            Debug.Log("[PHM:Start] Right boost slider initialized, maxValue=" + maxBoost + ", value=" + rightBoostMeter + ", frame=" + Time.frameCount);
        }
        if (leftCrosshair != null)
        {
            leftCrosshair.SetActive(false);
            Debug.Log("[PHM:Start] Left crosshair initialized: SetActive(false), frame=" + Time.frameCount);
        }
        if (rightCrosshair != null)
        {
            rightCrosshair.SetActive(false);
            Debug.Log("[PHM:Start] Right crosshair initialized: SetActive(false), frame=" + Time.frameCount);
        }
        // Initialize turn providers with correct state
        if (continuousTurnProvider != null)
        {
            continuousTurnProvider.enabled = !snapTurnEnabled;
            Debug.Log("[PHM:Start] ContinuousTurnProvider initialized: enabled=" + continuousTurnProvider.enabled + ", frame=" + Time.frameCount);
        }
        if (snapTurnProvider != null)
        {
            snapTurnProvider.enabled = snapTurnEnabled;
            Debug.Log("[PHM:Start] SnapTurnProvider initialized: enabled=" + snapTurnProvider.enabled + ", frame=" + Time.frameCount);
        }
    }

    void Update()
    {
        if (playerCamera != null && leftWaistAnchor != null && rightWaistAnchor != null)
        {
            Vector3 cameraForward = playerCamera.forward;
            cameraForward.y = 0;
            Quaternion cameraYaw = cameraForward != Vector3.zero ? Quaternion.LookRotation(cameraForward) : Quaternion.identity;
            Vector3 waistBasePosition = playerCamera.position + Vector3.up * waistHeightOffset;
            Vector3 leftOffset = cameraYaw * new Vector3(-waistSideOffset, 0, 0);
            Vector3 rightOffset = cameraYaw * new Vector3(waistSideOffset, 0, 0);
            leftWaistAnchor.position = waistBasePosition + leftOffset;
            rightWaistAnchor.position = waistBasePosition + rightOffset;
            leftWaistAnchor.rotation = cameraYaw;
            rightWaistAnchor.rotation = cameraYaw;
        }
        UpdateCrosshair(leftController, leftWaistAnchor, leftCrosshair, leftCrosshairRenderer, leftCrosshairMaterial, true);
        UpdateCrosshair(rightController, rightWaistAnchor, rightCrosshair, rightCrosshairRenderer, rightCrosshairMaterial, false);
        HandleHook(leftController, controls?.VR.LeftTriggerPressed, controls?.VR.LeftGripPressed, controls?.VR.LeftButtonX,
            ref leftHookAttached, ref leftHookJoint, leftHookLine, ref leftGrapplePoint, ref leftBoostMeter, leftBoostSlider, true);
        HandleHook(rightController, controls?.VR.RightTriggerPressed, controls?.VR.RightGripPressed, controls?.VR.RightButtonA,
            ref rightHookAttached, ref rightHookJoint, rightHookLine, ref rightGrapplePoint, ref rightBoostMeter, rightBoostSlider, false);
        if (vignetteController != null && vignetteEnabled)
        {
            float velocity = playerRigidbody.velocity.magnitude;
            bool wasMoving = isMoving;
            isMoving = velocity > velocityThreshold;
            if (isMoving && !wasMoving)
            {
                vignetteController.BeginTunnelingVignette(this);
                Debug.Log("[PHM:Update] Vignette activated: velocity=" + velocity.ToString("F2") + ", frame=" + Time.frameCount);
            }
            else if (!isMoving && wasMoving)
            {
                vignetteController.EndTunnelingVignette(this);
                Debug.Log("[PHM:Update] Vignette deactivated: velocity=" + velocity.ToString("F2") + ", frame=" + Time.frameCount);
            }
        }
        // Manage turn providers with enforced mutual exclusivity
        if (continuousTurnProvider != null && snapTurnProvider != null)
        {
            // Enforce mutual exclusivity
            if (snapTurnEnabled)
            {
                if (continuousTurnProvider.enabled)
                {
                    continuousTurnProvider.enabled = false;
                    Debug.Log("[PHM:Update] ContinuousTurnProvider forcibly disabled (snap turn on), frame=" + Time.frameCount);
                }
                if (!snapTurnProvider.enabled)
                {
                    snapTurnProvider.enabled = true;
                    Debug.Log("[PHM:Update] SnapTurnProvider forcibly enabled (snap turn on), frame=" + Time.frameCount);
                }
            }
            else
            {
                if (!continuousTurnProvider.enabled)
                {
                    continuousTurnProvider.enabled = true;
                    Debug.Log("[PHM:Update] ContinuousTurnProvider forcibly enabled (snap turn off), frame=" + Time.frameCount);
                }
                if (snapTurnProvider.enabled)
                {
                    snapTurnProvider.enabled = false;
                    Debug.Log("[PHM:Update] SnapTurnProvider forcibly disabled (snap turn off), frame=" + Time.frameCount);
                }
            }
            Debug.Log("[PHM:Update] Turn state: ContinuousTurnProvider=" + (continuousTurnProvider.enabled ? "enabled" : "disabled") + ", SnapTurnProvider=" + (snapTurnProvider.enabled ? "enabled" : "disabled") + ", snapTurnEnabled=" + snapTurnEnabled + ", frame=" + Time.frameCount);
        }
        bool isGrounded = IsGrounded();
        if ((leftHookAttached || rightHookAttached) && tiltEnabled && cameraOffset != null && !isGrounded)
        {
            Transform activeWaistAnchor = leftHookAttached ? leftWaistAnchor : rightWaistAnchor;
            Vector3 hookPoint = leftHookAttached ? leftGrapplePoint : rightGrapplePoint;
            float velocity = playerRigidbody.velocity.magnitude;

            // Pitch Tilt (Head Upward)
            float heightDifference = hookPoint.y - activeWaistAnchor.position.y;
            float desiredPitchAngle = Mathf.Clamp(heightDifference * pitchTiltFactor, 0f, maxPitchAngle);
            currentPitchAngle = Mathf.Lerp(currentPitchAngle, desiredPitchAngle, Time.deltaTime * tiltSmoothSpeed);

            // Roll Tilt (Side Tilt based on hook side)
            float rollDirection = leftHookAttached ? 1f : -1f;
            float desiredRollAngle = rollDirection * Mathf.Min(velocity * rollVelocityFactor, maxRollAngle);
            currentRollAngle = Mathf.Lerp(currentRollAngle, desiredRollAngle, Time.deltaTime * tiltSmoothSpeed);

            // Apply pitch and roll to cameraOffset
            cameraOffset.localEulerAngles = new Vector3(-currentPitchAngle, 0f, currentRollAngle);
            Debug.Log("[PHM:Update] Camera tilt: pitch=" + currentPitchAngle.ToString("F2") + ", roll=" + currentRollAngle.ToString("F2") + ", velocity=" + velocity.ToString("F2") + ", frame=" + Time.frameCount);

            // Flips and Rotational Dynamics
            Vector3 controllerInput = Vector3.zero;
            if (leftController != null && leftHookAttached)
            {
                Vector3 controllerForward = leftController.transform.forward;
                controllerInput = new Vector3(controllerForward.x, 0f, controllerForward.z).normalized;
            }
            else if (rightController != null && rightHookAttached)
            {
                Vector3 controllerForward = rightController.transform.forward;
                controllerInput = new Vector3(controllerForward.x, 0f, controllerForward.z).normalized;
            }
            if (controllerInput.magnitude > 0.1f)
            {
                Vector3 torque = new Vector3(controllerInput.z, 0f, -controllerInput.x) * flipTorque;
                playerRigidbody.AddTorque(torque, ForceMode.Impulse);
                Debug.Log("[PHM:Update] Applied flip torque: " + torque + ", frame=" + Time.frameCount);
            }
        }
        else if (cameraOffset != null)
        {
            // Smoothly return to neutral when not hooked or grounded
            currentPitchAngle = Mathf.Lerp(currentPitchAngle, 0f, Time.deltaTime * tiltSmoothSpeed);
            currentRollAngle = Mathf.Lerp(currentRollAngle, 0f, Time.deltaTime * tiltSmoothSpeed);
            cameraOffset.localEulerAngles = new Vector3(-currentPitchAngle, 0f, currentRollAngle);
        }
    }

    void UpdateCrosshair(ActionBasedController controller, Transform waistAnchor, GameObject crosshair, Renderer crosshairRenderer, Material crosshairMaterial, bool isLeft)
    {
        if (controller == null || waistAnchor == null || crosshair == null || crosshairRenderer == null || crosshairMaterial == null || playerCamera == null)
        {
            Debug.LogWarning("[PHM:UpdateCrosshair] Crosshair update skipped for " + (isLeft ? "left" : "right") + ": Missing components, frame=" + Time.frameCount);
            return;
        }
        bool isAttached = isLeft ? leftHookAttached : rightHookAttached;
        if (isAttached || !crosshairEnabled)
        {
            if (crosshair.activeSelf)
            {
                crosshair.SetActive(false);
                crosshairRenderer.enabled = false;
                Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " crosshair disabled " + (isAttached ? "(hook attached)" : "(settings disabled)") + ", frame=" + Time.frameCount);
            }
            return;
        }

        Vector3 crosshairPosition = Vector3.zero;
        bool useAimAssist = false;

        // Aim assist: Check for nearby AimAssist-tagged objects
        if (crosshairEnabled)
        {
            Collider[] nearbyObjects = Physics.OverlapSphere(controller.transform.position, aimAssistSearchRadius);
            Transform closestAimAssist = null;
            float closestDistance = float.MaxValue;
            float angleThresholdCos = Mathf.Cos(aimAssistAngleThreshold * Mathf.Deg2Rad);

            foreach (var collider in nearbyObjects)
            {
                if (collider.CompareTag("AimAssist"))
                {
                    Vector3 toMarker = (collider.transform.position - controller.transform.position).normalized;
                    float cosAngle = Vector3.Dot(controller.transform.forward, toMarker);
                    if (cosAngle >= angleThresholdCos)
                    {
                        float distance = Vector3.Distance(controller.transform.position, collider.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestAimAssist = collider.transform;
                        }
                    }
                }
            }

            if (closestAimAssist != null)
            {
                crosshairPosition = closestAimAssist.position;
                useAimAssist = true;
                if (isLeft)
                {
                    leftAimAssistPosition = crosshairPosition;
                    leftUseAimAssist = true;
                }
                else
                {
                    rightAimAssistPosition = crosshairPosition;
                    rightUseAimAssist = true;
                }
                Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " crosshair snapped to AimAssist marker at: " + crosshairPosition + ", distance: " + closestDistance + ", frame=" + Time.frameCount);
            }
            else
            {
                if (isLeft) leftUseAimAssist = false;
                else rightUseAimAssist = false;
                Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " no AimAssist markers within " + aimAssistAngleThreshold + " degrees, frame=" + Time.frameCount);
            }
        }

        // Fallback to raycast if no aim assist marker is found
        if (!useAimAssist)
        {
            RaycastHit hit;
            bool raycastHit = Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 100f);
            Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " controller raycast from " + controller.transform.position + " forward " + controller.transform.forward + ", hit: " + raycastHit + ", hit object: " + (raycastHit ? hit.collider.name : "none") + ", frame=" + Time.frameCount);
            if (raycastHit)
            {
                crosshairPosition = hit.point;
            }
            else
            {
                if (crosshair.activeSelf)
                {
                    crosshair.SetActive(false);
                    crosshairRenderer.enabled = false;
                    Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " crosshair disabled (no hit), frame=" + Time.frameCount);
                }
                crosshairMaterial.color = Color.red;
                return;
            }
        }

        // Update crosshair position and properties (ensure visibility for aim assist)
        if (!crosshair.activeSelf || !crosshairRenderer.enabled)
        {
            crosshair.SetActive(true);
            crosshairRenderer.enabled = true;
            Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " crosshair enabled at: " + crosshairPosition + ", renderer: " + crosshairRenderer.enabled + ", frame=" + Time.frameCount);
        }
        crosshair.transform.position = crosshairPosition;
        Vector3 toPlayer = (playerCamera.position - crosshairPosition).normalized;
        crosshair.transform.rotation = Quaternion.LookRotation(-toPlayer);
        float distanceToPlayer = Vector3.Distance(playerCamera.position, crosshairPosition);
        float scale = baseScale * (distanceToPlayer / referenceDistance);
        scale = Mathf.Clamp(scale, minScale, maxScale);
        crosshair.transform.localScale = new Vector3(scale, scale, scale);
        float distanceToWaist = Vector3.Distance(waistAnchor.position, crosshairPosition);
        bool isUngrappleable = useAimAssist ? false : Physics.Raycast(controller.transform.position, (crosshairPosition - controller.transform.position).normalized, out RaycastHit hitCheck, distanceToPlayer) && hitCheck.collider != null && hitCheck.collider.CompareTag("ungrappleable");
        crosshairMaterial.color = isUngrappleable ? Color.red : (distanceToWaist >= minGrappleDistance ? Color.green : Color.red);
        Debug.Log("[PHM:UpdateCrosshair] " + (isLeft ? "Left" : "Right") + " crosshair at " + crosshairPosition + ", distance from waist: " + distanceToWaist + ", scale: " + scale + ", color: " + (isUngrappleable ? "Red (ungrappleable)" : (distanceToWaist >= minGrappleDistance ? "Green" : "Red")) + ", aimAssist: " + useAimAssist + ", frame=" + Time.frameCount);
    }

    void HandleHook(ActionBasedController controller, InputAction triggerAction, InputAction pullAction, InputAction boostAction,
        ref bool isAttached, ref ConfigurableJoint hookJoint, LineRenderer hookLine, ref Vector3 grapplePoint,
        ref float boostMeter, Slider boostSlider, bool isLeft)
    {
        if (controller == null || triggerAction == null || pullAction == null || boostAction == null || playerRigidbody == null || playerCamera == null)
        {
            Debug.LogWarning("[PHM:HandleHook] HandleHook skipped for " + (isLeft ? "left" : "right") + ": Missing components, frame=" + Time.frameCount);
            return;
        }
        Transform waistAnchor = isLeft ? leftWaistAnchor : rightWaistAnchor;
        if (waistAnchor == null)
        {
            Debug.LogWarning("[PHM:HandleHook] HandleHook skipped for " + (isLeft ? "left" : "right") + ": Waist anchor missing, frame=" + Time.frameCount);
            return;
        }
        if (hookLine == null)
        {
            Debug.LogWarning("[PHM:HandleHook] HandleHook skipped for " + (isLeft ? "left" : "right") + ": LineRenderer missing, frame=" + Time.frameCount);
            return;
        }
        ParticleSystem sparksPrefab = isLeft ? leftSparks : rightSparks;
        ParticleSystem gasPrefab = isLeft ? leftGas : rightGas;
        if (triggerAction.WasPerformedThisFrame())
        {
            bool useAimAssist = isLeft ? leftUseAimAssist : rightUseAimAssist;
            Vector3 targetPoint = Vector3.zero;
            float distance = 0f;
            bool isUngrappleable = false;

            if (useAimAssist)
            {
                targetPoint = isLeft ? leftAimAssistPosition : rightAimAssistPosition;
                distance = Vector3.Distance(waistAnchor.position, targetPoint);
                // Aim assist targets are always grappleable
                isUngrappleable = false;
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " using aim assist marker at: " + targetPoint + ", distance: " + distance + ", frame=" + Time.frameCount);
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 100f))
                {
                    targetPoint = hit.point;
                    distance = Vector3.Distance(waistAnchor.position, targetPoint);
                    isUngrappleable = hit.collider != null && hit.collider.CompareTag("ungrappleable");
                    Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " raycast hit at: " + targetPoint + ", distance: " + distance + ", ungrappleable: " + isUngrappleable + ", frame=" + Time.frameCount);
                }
                else
                {
                    Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " raycast missed, frame=" + Time.frameCount);
                    return;
                }
            }

            if (distance >= minGrappleDistance && !isUngrappleable)
            {
                isAttached = true;
                grapplePoint = targetPoint;
                hookJoint = playerRigidbody.gameObject.AddComponent<ConfigurableJoint>();
                hookJoint.autoConfigureConnectedAnchor = false;
                hookJoint.connectedAnchor = grapplePoint;
                hookJoint.angularXMotion = ConfigurableJointMotion.Locked;
                hookJoint.angularYMotion = ConfigurableJointMotion.Locked;
                hookJoint.angularZMotion = ConfigurableJointMotion.Locked;
                hookJoint.xMotion = ConfigurableJointMotion.Limited;
                hookJoint.yMotion = ConfigurableJointMotion.Limited;
                hookJoint.zMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit limit = new SoftJointLimit { limit = distance };
                hookJoint.linearLimit = limit;
                hookLine.enabled = true;
                if (isLeft) originalLeftDistance = distance;
                else originalRightDistance = distance;
                if (isLeft) leftHookRetracting = false;
                else rightHookRetracting = false;
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " hook attached at " + grapplePoint + ", distance: " + distance + ", frame=" + Time.frameCount);
            }
        }
        if (triggerAction.WasReleasedThisFrame() && isAttached)
        {
            isAttached = false;
            if (hookJoint != null)
            {
                Destroy(hookJoint);
                hookJoint = null;
            }
            if (isLeft) leftHookRetracting = true;
            else rightHookRetracting = true;
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
            hookLine.enabled = false;
            if (sparksPrefab != null)
            {
                ParticleSystem sparkInstance = Instantiate(sparksPrefab, waistAnchor.position, waistAnchor.rotation);
                sparkInstance.Play();
                float duration = Mathf.Min(sparkDuration, sparkInstance.main.duration + sparkInstance.main.startLifetime.constantMax);
                Destroy(sparkInstance.gameObject, duration);
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " sparks instantiated on release at: " + waistAnchor.position + ", duration: " + duration + ", frame=" + Time.frameCount);
            }
        }
        if (isAttached && pullAction.IsPressed())
        {
            float currentDistance = Vector3.Distance(playerRigidbody.position, grapplePoint);
            SoftJointLimit limit = hookJoint.linearLimit;
            limit.limit = Mathf.Max(currentDistance, minLimit);
            hookJoint.linearLimit = limit;
            Vector3 pullDirection = (grapplePoint - playerRigidbody.position).normalized;
            playerRigidbody.AddForce(pullDirection * pullBoostForce, ForceMode.VelocityChange);
            if (sparksPrefab != null)
            {
                ParticleSystem sparkInstance = Instantiate(sparksPrefab, waistAnchor.position, waistAnchor.rotation);
                sparkInstance.Play();
                float duration = Mathf.Min(sparkDuration, sparkInstance.main.duration + sparkInstance.main.startLifetime.constantMax);
                Destroy(sparkInstance.gameObject, duration);
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " sparks instantiated on pull at: " + waistAnchor.position + ", duration: " + duration + ", frame=" + Time.frameCount);
            }
            float t = (currentDistance - minLimit) / ((isLeft ? originalLeftDistance : originalRightDistance) - minLimit);
            hookLine.startColor = Color.Lerp(Color.white, Color.red, t);
            hookLine.endColor = Color.Lerp(Color.white, Color.red, t);
            hookLine.SetPosition(0, waistAnchor.position);
            hookLine.SetPosition(1, grapplePoint);
        }
        else if (isAttached)
        {
            float currentDistance = Vector3.Distance(playerRigidbody.position, grapplePoint);
            SoftJointLimit limit = hookJoint.linearLimit;
            limit.limit = Mathf.Max(currentDistance, minLimit);
            hookJoint.linearLimit = limit;
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
            hookLine.SetPosition(0, waistAnchor.position);
            hookLine.SetPosition(1, grapplePoint);
        }
        if (boostAction.WasPerformedThisFrame())
        {
            float currentTime = Time.time;
            float lastBoostTime = isLeft ? lastLeftBoostTime : lastRightBoostTime;
            if (currentTime - lastBoostTime < doubleTapTime && boostMeter >= doubleTapBoostCost)
            {
                Vector3 boostDirection = playerCamera.forward;
                boostDirection += (isLeft ? 1 : -1) * playerCamera.right * lateralOffset;
                bool isGrounded = IsGrounded();
                if (isGrounded)
                {
                    boostDirection = new Vector3(boostDirection.x, 0, boostDirection.z).normalized;
                }
                else if (isAttached && grapplePoint.y > playerRigidbody.position.y)
                {
                    boostDirection += Vector3.up * ((grapplePoint.y - playerRigidbody.position.y) * upwardBoostFactor);
                }
                playerRigidbody.AddForce(boostDirection.normalized * doubleTapBoostForce, ForceMode.VelocityChange);
                boostMeter = Mathf.Max(boostMeter - doubleTapBoostCost, 0);
                if (boostSlider != null) boostSlider.value = boostMeter;
                if (isLeft) lastLeftBoostTime = -1f;
                else lastRightBoostTime = -1f;
                if (gasPrefab != null)
                {
                    ParticleSystem gasInstance = Instantiate(gasPrefab, waistAnchor.position, waistAnchor.rotation);
                    gasInstance.Play();
                    float duration = Mathf.Min(gasDuration, gasInstance.main.duration + gasInstance.main.startLifetime.constantMax);
                    Destroy(gasInstance.gameObject, duration);
                    Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " gas instantiated on double-tap boost at: " + waistAnchor.position + ", duration: " + duration + ", frame=" + Time.frameCount);
                }
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " double-tap boost applied, meter: " + boostMeter + ", frame=" + Time.frameCount);
            }
            else
            {
                if (isLeft) lastLeftBoostTime = currentTime;
                else lastRightBoostTime = currentTime;
            }
        }
        if (boostAction.IsPressed() && boostMeter > 0)
        {
            Vector3 boostDirection = playerCamera.forward;
            boostDirection += (isLeft ? 1 : -1) * playerCamera.right * lateralOffset;
            bool isGrounded = IsGrounded();
            if (isGrounded)
            {
                boostDirection = new Vector3(boostDirection.x, 0, boostDirection.z).normalized;
                playerRigidbody.angularDrag = 5f;
            }
            else
            {
                playerRigidbody.angularDrag = isAttached ? 5f : 20f;
                if (isAttached && grapplePoint.y > playerRigidbody.position.y)
                {
                    boostDirection += Vector3.up * ((grapplePoint.y - playerRigidbody.position.y) * upwardBoostFactor);
                }
            }
            playerRigidbody.AddForce(boostDirection.normalized * boostSpeed, ForceMode.VelocityChange);
            boostMeter = Mathf.Max(boostMeter - boostDrainRate * Time.deltaTime, 0);
            if (boostSlider != null) boostSlider.value = boostMeter;
            if (gasPrefab != null)
            {
                ParticleSystem gasInstance = Instantiate(gasPrefab, waistAnchor.position, waistAnchor.rotation);
                gasInstance.Play();
                float duration = Mathf.Min(gasDuration, gasInstance.main.duration + gasInstance.main.startLifetime.constantMax);
                Destroy(gasInstance.gameObject, duration);
                Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " gas instantiated on boost at: " + waistAnchor.position + ", duration: " + duration + ", frame=" + Time.frameCount);
            }
            Debug.Log("[PHM:HandleHook] " + (isLeft ? "Left" : "Right") + " boost active, meter: " + boostMeter + ", frame=" + Time.frameCount);
        }
        else
        {
            playerRigidbody.angularDrag = 0.05f;
        }
    }

    bool IsGrounded()
    {
        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        Debug.Log("[PHM:IsGrounded] Grounded: " + grounded + ", frame=" + Time.frameCount);
        return grounded;
    }

    public VignetteParameters vignetteParameters => vignetteEnabled ? _vignetteParameters : null;

    public void DisableCrosshairs()
    {
        if (leftCrosshair != null)
        {
            leftCrosshair.SetActive(false);
            Debug.Log("[PHM:DisableCrosshairs] Left crosshair (blade) disabled by menu, frame=" + Time.frameCount);
        }
        if (rightCrosshair != null)
        {
            rightCrosshair.SetActive(false);
            Debug.Log("[PHM:DisableCrosshairs] Right crosshair (blade) disabled by menu, frame=" + Time.frameCount);
        }
    }

    public void EnableCrosshairs()
    {
        if (leftCrosshair != null && crosshairEnabled)
        {
            leftCrosshair.SetActive(true);
            Debug.Log("[PHM:EnableCrosshairs] Left crosshair (blade) enabled by menu, frame=" + Time.frameCount);
        }
        if (rightCrosshair != null && crosshairEnabled)
        {
            rightCrosshair.SetActive(true);
            Debug.Log("[PHM:EnableCrosshairs] Right crosshair (blade) enabled by menu, frame=" + Time.frameCount);
        }
    }

    public void EnableSnapTurn()
    {
        snapTurnEnabled = true;
        if (continuousTurnProvider != null && continuousTurnProvider.enabled)
        {
            continuousTurnProvider.enabled = false;
            Debug.Log("[PHM:EnableSnapTurn] ContinuousTurnProvider disabled, frame=" + Time.frameCount);
        }
        if (snapTurnProvider != null && !snapTurnProvider.enabled)
        {
            snapTurnProvider.enabled = true;
            Debug.Log("[PHM:EnableSnapTurn] SnapTurnProvider enabled, frame=" + Time.frameCount);
        }
        Debug.Log("[PHM:EnableSnapTurn] Snap turn enabled, snapTurnEnabled=" + snapTurnEnabled + ", frame=" + Time.frameCount);
    }

    public void DisableSnapTurn()
    {
        snapTurnEnabled = false;
        if (continuousTurnProvider != null && !continuousTurnProvider.enabled)
        {
            continuousTurnProvider.enabled = true;
            Debug.Log("[PHM:DisableSnapTurn] ContinuousTurnProvider enabled, frame=" + Time.frameCount);
        }
        if (snapTurnProvider != null && snapTurnProvider.enabled)
        {
            snapTurnProvider.enabled = false;
            Debug.Log("[PHM:DisableSnapTurn] SnapTurnProvider disabled, frame=" + Time.frameCount);
        }
        Debug.Log("[PHM:DisableSnapTurn] Snap turn disabled, snapTurnEnabled=" + snapTurnEnabled + ", frame=" + Time.frameCount);
    }
}