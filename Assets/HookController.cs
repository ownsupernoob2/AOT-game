using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class HookController : MonoBehaviour
{
    public ActionBasedController leftController;
    public ActionBasedController rightController;
    public LineRenderer leftHookLine;
    public LineRenderer rightHookLine;
    public ParticleSystem leftSparks;
    public ParticleSystem rightSparks;
    public GameObject leftBoostCube;
    public GameObject rightBoostCube;

    private Rigidbody playerRigidbody;
    private ConfigurableJoint leftHookJoint;
    private ConfigurableJoint rightHookJoint;
    private VRControls controls;
    public bool leftHookAttached = false;
    public bool rightHookAttached = false;
    public bool leftHookRetracting = false;
    public bool rightHookRetracting = false;
    public Vector3 leftGrapplePoint;
    public Vector3 rightGrapplePoint;

    private float retractSpeed = 10f;
    private float minLimit = 1f;
    private float originalLeftDistance;
    private float originalRightDistance;

    void Awake()
    {
        controls = new VRControls();
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null) Debug.LogError("Player Rigidbody not found!");
        if (leftController == null || rightController == null) Debug.LogWarning("Controller(s) not assigned!");
        if (leftHookLine == null || rightHookLine == null) Debug.LogWarning("LineRenderer(s) not assigned!");
        if (leftSparks == null || rightSparks == null) Debug.LogWarning("Sparks not assigned!");
        if (leftBoostCube == null || rightBoostCube == null) Debug.LogWarning("Boost cubes not assigned!");
    }

    void OnEnable() => controls?.Enable();
    void OnDisable() => controls?.Disable();

    void Start()
    {
        leftHookLine.positionCount = 2;
        rightHookLine.positionCount = 2;
        leftHookLine.enabled = false;
        rightHookLine.enabled = false;

        if (leftSparks != null) leftSparks.Stop();
        if (rightSparks != null) rightSparks.Stop();
    }

    void Update()
    {
        HandleHook(leftController, controls?.VR.LeftTriggerPressed, controls?.VR.LeftButtonX,
            ref leftHookAttached, ref leftHookJoint, leftHookLine, ref leftGrapplePoint, true);
        HandleHook(rightController, controls?.VR.RightTriggerPressed, controls?.VR.RightButtonA,
            ref rightHookAttached, ref rightHookJoint, rightHookLine, ref rightGrapplePoint, false);

        if (leftSparks != null && leftSparks.isPlaying) leftSparks.transform.position = leftBoostCube.transform.position;
        if (rightSparks != null && rightSparks.isPlaying) rightSparks.transform.position = rightBoostCube.transform.position;
    }

    void HandleHook(ActionBasedController controller, InputAction triggerAction, InputAction pullAction,
        ref bool isAttached, ref ConfigurableJoint hookJoint, LineRenderer hookLine, ref Vector3 grapplePoint, bool isLeft)
    {
        if (controller == null || triggerAction == null || pullAction == null || playerRigidbody == null) return;

        // **Hook Attachment**
        if (triggerAction.WasPerformedThisFrame())
        {
            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out RaycastHit hit, 100f))
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

                float distance = Vector3.Distance(playerRigidbody.position, grapplePoint);
                SoftJointLimit limit = new SoftJointLimit { limit = distance };
                hookJoint.linearLimit = limit;

                hookLine.enabled = true;
                hookLine.SetPosition(0, controller.transform.position);
                hookLine.SetPosition(1, grapplePoint);

                if (isLeft) originalLeftDistance = distance;
                else originalRightDistance = distance;

                if (isLeft) leftHookRetracting = false;
                else rightHookRetracting = false;
            }
        }

        // **Hook Release**
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
                leftSparks.Play();
                Invoke("StopLeftSparks", 0.5f);
            }
            else
            {
                rightSparks.Play();
                Invoke("StopRightSparks", 0.5f);
            }
        }

        // **Update LineRenderer**
        if (isAttached)
        {
            hookLine.SetPosition(0, controller.transform.position);
            hookLine.SetPosition(1, grapplePoint);
        }

        // **Hook Retraction**
        if (isAttached && pullAction.IsPressed())
        {
            SoftJointLimit limit = hookJoint.linearLimit;
            limit.limit -= retractSpeed * Time.deltaTime;
            if (limit.limit < minLimit) limit.limit = minLimit;
            hookJoint.linearLimit = limit;

            if (isLeft)
            {
                leftSparks.transform.position = leftBoostCube.transform.position;
                leftSparks.Play();
            }
            else
            {
                rightSparks.transform.position = rightBoostCube.transform.position;
                rightSparks.Play();
            }

            float t = (hookJoint.linearLimit.limit - minLimit) / ((isLeft ? originalLeftDistance : originalRightDistance) - minLimit);
            hookLine.startColor = Color.Lerp(Color.white, Color.red, t);
            hookLine.endColor = Color.Lerp(Color.white, Color.red, t);
        }
        else if (isAttached)
        {
            if (isLeft) leftSparks.Stop();
            else rightSparks.Stop();
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
        }
    }

    private void StopLeftSparks() => leftSparks.Stop();
    private void StopRightSparks() => rightSparks.Stop();
}