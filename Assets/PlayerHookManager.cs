using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class PlayerHookManager : MonoBehaviour
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
    public ParticleSystem leftSparks;
    public ParticleSystem rightSparks;
    private float originalLeftDistance;
    private float originalRightDistance;
    private Vector3 lastLeftControllerForward;
    private Vector3 lastRightControllerForward;
    private float stabilizationThreshold = 0.01f;
    public bool crosshairEnabled = true;
    public bool tiltEnabled = true;
    private float tiltFactor = 5f;
    private float maxTiltAngle = 15f;
    private float tiltSmoothSpeed = 20f; // Increased for faster, more realistic response
    private float currentRollAngle = 0f;
    private float speed;

    void Awake()
    {
        controls = new VRControls();
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null) Debug.LogError("Player Rigidbody not found! Attach this script to the XR Rig root with a Rigidbody.");
        if (leftController == null) Debug.LogWarning("LeftController is null!");
        if (rightController == null) Debug.LogWarning("RightController is null!");
        if (playerCamera == null) Debug.LogWarning("PlayerCamera is null!");
        if (cameraOffset == null) Debug.LogWarning("CameraOffset is null!");
        if (leftWaistAnchor == null || rightWaistAnchor == null) Debug.LogWarning("Waist anchors not assigned!");
        if (leftCrosshair == null || rightCrosshair == null) Debug.LogWarning("Crosshairs not assigned!");
        if (leftBoostCanvas == null || rightBoostCanvas == null) Debug.LogWarning("Boost canvases not assigned!");
        if (leftBoostSlider == null || rightBoostSlider == null) Debug.LogWarning("Boost sliders not assigned!");
        if (leftSparks == null || rightSparks == null) Debug.LogWarning("Sparks particle systems not assigned!");
        leftCrosshairRenderer = leftCrosshair != null ? leftCrosshair.GetComponentInChildren<Renderer>() : null;
        rightCrosshairRenderer = rightCrosshair != null ? rightCrosshair.GetComponentInChildren<Renderer>() : null;
        if (leftCrosshairRenderer == null || rightCrosshairRenderer == null)
            Debug.LogWarning("Crosshair renderers not found! Ensure crosshairs have Renderer components (MeshRenderer or SpriteRenderer).");
        else
        {
            leftCrosshairRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rightCrosshairRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leftCrosshairRenderer.receiveShadows = false;
            rightCrosshairRenderer.receiveShadows = false;
            leftCrosshairMaterial = leftCrosshairRenderer.sharedMaterial;
            rightCrosshairMaterial = rightCrosshairRenderer.sharedMaterial;
            if (leftCrosshairMaterial == null || rightCrosshairMaterial == null)
                Debug.LogWarning("Crosshair materials not found! Ensure renderers have valid materials.");
        }
        lastLeftControllerForward = Vector3.forward;
        lastRightControllerForward = Vector3.forward;
    }

    void OnEnable()
    {
        if (controls != null) controls.Enable();
    }

    void OnDisable()
    {
        if (controls != null) controls.Disable();
    }

    void Start()
    {
        if (leftHookLine != null)
        {
            leftHookLine.positionCount = 2;
            leftHookLine.enabled = false;
        }
        if (rightHookLine != null)
        {
            rightHookLine.positionCount = 2;
            rightHookLine.enabled = false;
        }
        if (leftBoostSlider != null)
        {
            leftBoostSlider.maxValue = maxBoost;
            leftBoostSlider.value = leftBoostMeter;
        }
        if (rightBoostSlider != null)
        {
            rightBoostSlider.maxValue = maxBoost;
            rightBoostSlider.value = rightBoostMeter;
        }
        if (leftSparks != null) leftSparks.Stop();
        if (rightSparks != null) rightSparks.Stop();
        if (leftCrosshair != null)
        {
            leftCrosshair.SetActive(false);
            Debug.Log("Left crosshair initialized: SetActive(false)");
        }
        if (rightCrosshair != null)
        {
            rightCrosshair.SetActive(false);
            Debug.Log("Right crosshair initialized: SetActive(false)");
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
        UpdateCrosshair(leftController, leftWaistAnchor, leftCrosshair, leftCrosshairRenderer, leftCrosshairMaterial, true, ref lastLeftControllerForward);
        UpdateCrosshair(rightController, rightWaistAnchor, rightCrosshair, rightCrosshairRenderer, rightCrosshairMaterial, false, ref lastRightControllerForward);
        HandleHook(leftController, controls?.VR.LeftTriggerPressed, controls?.VR.LeftGripPressed, controls?.VR.LeftButtonX,
            ref leftHookAttached, ref leftHookJoint, leftHookLine, ref leftGrapplePoint, ref leftBoostMeter, leftBoostSlider, true);
        HandleHook(rightController, controls?.VR.RightTriggerPressed, controls?.VR.RightGripPressed, controls?.VR.RightButtonA,
            ref rightHookAttached, ref rightHookJoint, rightHookLine, ref rightGrapplePoint, ref rightBoostMeter, rightBoostSlider, false);
        if (leftSparks != null && leftSparks.isPlaying)
        {
            leftSparks.transform.position = leftWaistAnchor.position;
            Debug.Log("Left sparks active at: " + leftWaistAnchor.position);
        }
        if (rightSparks != null && rightSparks.isPlaying)
        {
            rightSparks.transform.position = rightWaistAnchor.position;
            Debug.Log("Right sparks active at: " + rightWaistAnchor.position);
        }

        // Camera tilt logic
        if ((leftHookAttached || rightHookAttached) && tiltEnabled && cameraOffset != null)
        {
            Vector3 hookPoint = leftHookAttached ? leftGrapplePoint : rightGrapplePoint;
            Vector3 toHook = (hookPoint - leftWaistAnchor.position).normalized;
            Vector3 velocity = playerRigidbody.velocity;
            Vector3 lateralVelocity = velocity - Vector3.Dot(velocity, toHook) * toHook;
            float lateralSpeed = lateralVelocity.magnitude;
            if (lateralSpeed > 0.01f)
            {
                Vector3 lateralDir = lateralVelocity.normalized;
                Vector3 cameraRight = playerCamera.transform.right;
                float rollDirection = Vector3.Dot(lateralDir, cameraRight);
                float desiredRollAngle = -rollDirection * lateralSpeed * tiltFactor;
                desiredRollAngle = Mathf.Clamp(desiredRollAngle, -maxTiltAngle, maxTiltAngle);
                // Tighter Lerp for faster response
                currentRollAngle = Mathf.Lerp(currentRollAngle, desiredRollAngle, Time.deltaTime * tiltSmoothSpeed);
                cameraOffset.localEulerAngles = new Vector3(0, 0, currentRollAngle);
                Debug.Log($"Camera tilt: speed={lateralSpeed:F2}, angle={currentRollAngle:F2}, frame={Time.frameCount}");
            }
            else
            {
                currentRollAngle = Mathf.Lerp(currentRollAngle, 0f, Time.deltaTime * tiltSmoothSpeed);
                cameraOffset.localEulerAngles = new Vector3(0, 0, currentRollAngle);
            }
        }
        else if (cameraOffset != null)
        {
            currentRollAngle = Mathf.Lerp(currentRollAngle, 0f, Time.deltaTime * tiltSmoothSpeed);
            cameraOffset.localEulerAngles = new Vector3(0, 0, currentRollAngle);
        }
    }

    void UpdateCrosshair(ActionBasedController controller, Transform waistAnchor, GameObject crosshair, Renderer crosshairRenderer, Material crosshairMaterial, bool isLeft, ref Vector3 lastControllerForward)
    {
        if (controller == null || waistAnchor == null || crosshair == null || crosshairRenderer == null || crosshairMaterial == null || playerCamera == null)
        {
            Debug.LogWarning($"Crosshair update skipped for {(isLeft ? "left" : "right")}: Missing components.");
            return;
        }
        bool isAttached = isLeft ? leftHookAttached : rightHookAttached;
        if (isAttached || !crosshairEnabled)
        {
            if (crosshair.activeSelf)
            {
                crosshair.SetActive(false);
                crosshairRenderer.enabled = false;
                Debug.Log($"{(isLeft ? "Left" : "Right")} crosshair disabled {(isAttached ? "(hook attached)" : "(settings disabled)")}");
            }
            return;
        }
        Vector3 currentForward = controller.transform.forward;
        float angleChange = Vector3.Angle(lastControllerForward, currentForward);
        if (angleChange > stabilizationThreshold)
        {
            lastControllerForward = currentForward;
        }
        else
        {
            currentForward = lastControllerForward;
            Debug.Log($"{(isLeft ? "Left" : "Right")} crosshair using stabilized forward: {currentForward}");
        }
        RaycastHit hit;
        bool raycastHit = Physics.Raycast(controller.transform.position, currentForward, out hit, 100f);
        Debug.Log($"{(isLeft ? "Left" : "Right")} controller raycast from {controller.transform.position} forward {currentForward}, hit: {raycastHit}");
        if (raycastHit)
        {
            if (!crosshair.activeSelf)
            {
                crosshair.SetActive(true);
                crosshairRenderer.enabled = true;
                Debug.Log($"{(isLeft ? "Left" : "Right")} crosshair enabled at: {hit.point}, renderer: {crosshairRenderer.enabled}");
            }
            crosshair.transform.position = hit.point;
            Vector3 toPlayer = (playerCamera.position - hit.point).normalized;
            crosshair.transform.rotation = Quaternion.LookRotation(-toPlayer);
            float distanceToPlayer = Vector3.Distance(playerCamera.position, hit.point);
            float scale = baseScale * (distanceToPlayer / referenceDistance);
            scale = Mathf.Clamp(scale, minScale, maxScale);
            crosshair.transform.localScale = new Vector3(scale, scale, scale);
            float distanceToWaist = Vector3.Distance(waistAnchor.position, hit.point);
            bool isUngrappleable = hit.collider != null && hit.collider.CompareTag("ungrappleable");
            crosshairMaterial.color = isUngrappleable ? Color.red : (distanceToWaist >= minGrappleDistance ? Color.green : Color.red);
            Debug.Log($"{(isLeft ? "Left" : "Right")} crosshair at {hit.point}, distance from waist: {distanceToWaist}, scale: {scale}, color: {(isUngrappleable ? "Red (ungrappleable)" : (distanceToWaist >= minGrappleDistance ? "Green" : "Red"))}");
        }
        else
        {
            if (crosshair.activeSelf)
            {
                crosshair.SetActive(false);
                crosshairRenderer.enabled = false;
                Debug.Log($"{(isLeft ? "Left" : "Right")} crosshair disabled (no hit)");
            }
            crosshairMaterial.color = Color.red;
        }
    }

    void HandleHook(ActionBasedController controller, InputAction triggerAction, InputAction pullAction, InputAction boostAction,
        ref bool isAttached, ref ConfigurableJoint hookJoint, LineRenderer hookLine, ref Vector3 grapplePoint,
        ref float boostMeter, Slider boostSlider, bool isLeft)
    {
        if (controller == null || triggerAction == null || pullAction == null || boostAction == null || playerRigidbody == null || playerCamera == null)
        {
            Debug.LogWarning($"HandleHook skipped for {(isLeft ? "left" : "right")}: Missing components.");
            return;
        }
        Transform waistAnchor = isLeft ? leftWaistAnchor : rightWaistAnchor;
        if (waistAnchor == null)
        {
            Debug.LogWarning($"HandleHook skipped for {(isLeft ? "left" : "right")}: Waist anchor missing.");
            return;
        }
        if (hookLine == null)
        {
            Debug.LogWarning($"HandleHook skipped for {(isLeft ? "left" : "right")}: LineRenderer missing.");
            return;
        }
        if (triggerAction.WasPerformedThisFrame())
        {
            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out RaycastHit hit, 100f))
            {
                float distance = Vector3.Distance(waistAnchor.position, hit.point);
                bool isUngrappleable = hit.collider != null && hit.collider.CompareTag("ungrappleable");
                if (distance >= minGrappleDistance && !isUngrappleable)
                {
                    isAttached = true;
                    grapplePoint = hit.point;
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
                    if (isLeft) originalLeftDistance = speed;
                    else originalRightDistance = distance;
                    if (isLeft) leftHookRetracting = false;
                    else rightHookRetracting = false;
                }
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
            if (isLeft)
            {
                leftSparks.transform.position = leftWaistAnchor.position;
                leftSparks.Play();
                Invoke("StopLeftSparks", 0.5f);
                Debug.Log("Left sparks played on release at: " + leftWaistAnchor.position);
            }
            else
            {
                rightSparks.transform.position = rightWaistAnchor.position;
                rightSparks.Play();
                Invoke("StopRightSparks", 0.5f);
                Debug.Log("Right sparks played on release at: " + rightWaistAnchor.position);
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
            if (isLeft)
            {
                leftSparks.transform.position = leftWaistAnchor.position;
                leftSparks.Play();
                Debug.Log("Left sparks played on pull at: " + leftWaistAnchor.position);
            }
            else
            {
                rightSparks.transform.position = rightWaistAnchor.position;
                rightSparks.Play();
                Debug.Log("Right sparks played on pull at: " + rightWaistAnchor.position);
            }
            float t = (currentDistance - minLimit) / ((isLeft ? originalLeftDistance : originalRightDistance) - minLimit);
            hookLine.startColor = Color.Lerp(Color.white, Color.red, t);
            hookLine.endColor = Color.Lerp(Color.white, Color.red, t);
        }
        else if (isAttached)
        {
            float currentDistance = Vector3.Distance(playerRigidbody.position, grapplePoint);
            SoftJointLimit limit = hookJoint.linearLimit;
            limit.limit = Mathf.Max(currentDistance, minLimit);
            hookJoint.linearLimit = limit;
            if (isLeft) leftSparks.Stop();
            else rightSparks.Stop();
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
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
        }
        else
        {
            playerRigidbody.angularDrag = 0.05f;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void StopLeftSparks() => leftSparks.Stop();
    private void StopRightSparks() => rightSparks.Stop();
}