using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GrappleHook : MonoBehaviour
{
    public Transform handTransform; // The VR controller position
    public LayerMask grappleLayer;  // Set this to "Building" or similar in Unity
    public float maxDistance = 30f; // Maximum grapple range
    public float pullForce = 15f;   // Strength of the pull
    public float swingForce = 10f;  // Swing force for motion

    private Rigidbody playerRb;
    private SpringJoint joint;
    private Vector3 grapplePoint;
    private LineRenderer lineRenderer;
    private bool isSwinging = false;

    private XRController xrController; // Automatically assigned in Start()

    void Start()
    {
        playerRb = GetComponentInParent<Rigidbody>(); // Ensure the player has a Rigidbody
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;

        // Automatically get the XRController from this GameObject
        xrController = GetComponent<XRController>();
    }

    void Update()
    {
        if (xrController != null)
        {
            if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
            {
                ShootGrapple();
            }

            if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool grabPressed))
            {
                if (grabPressed && joint != null)
                {
                    isSwinging = true;
                }
                else if (!grabPressed)
                {
                    ReleaseGrapple();
                }
            }

            if (isSwinging && joint != null)
            {
                ApplySwingForce();
            }
        }

        DrawRope();
    }

    void ShootGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(handTransform.position, handTransform.forward, out hit, maxDistance, grappleLayer))
        {
            grapplePoint = hit.point;
            joint = playerRb.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, grapplePoint);
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.3f;

            joint.spring = pullForce;
            joint.damper = 5f;
            joint.massScale = 4.5f;
        }
    }

    void ApplySwingForce()
    {
        if (joint == null) return;

        Vector3 toGrapplePoint = grapplePoint - playerRb.position;
        Vector3 swingDirection = Vector3.Cross(toGrapplePoint, Vector3.up).normalized;

        playerRb.AddForce(swingDirection * swingForce, ForceMode.Acceleration);
    }

    void ReleaseGrapple()
    {
        if (joint != null)
        {
            Destroy(joint);
            isSwinging = false;
        }
    }

    void DrawRope()
    {
        if (joint != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, handTransform.position);
            lineRenderer.SetPosition(1, grapplePoint);
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }
}
