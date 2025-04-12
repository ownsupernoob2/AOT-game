using UnityEngine;

public class GrapplingRope : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 currentGrapplePosition;
    public PlayerHookManager hookManager;
    public bool isLeftRope;
    public Transform waistAnchor;
    public int quality = 20;
    public float damper = 0.7f;
    public float strength = 10f;
    public float velocity = 5f;
    public float waveCount = 2f;
    public float waveHeight = 0.1f;
    public AnimationCurve affectCurve;
    private float springValue;
    private float springVelocity;
    private float springTarget = 0f;
    private float retractionTime = 0.033f;
    private float retractionTimer = 0f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            Debug.LogError("LineRenderer not found on " + gameObject.name);
        }
        if (waistAnchor == null)
        {
            Debug.LogWarning("Waist anchor not assigned on " + gameObject.name);
        }
        springValue = 0f;
        springVelocity = 0f;
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void DrawRope()
    {
        if (hookManager == null || waistAnchor == null)
        {
            Debug.LogError("HookManager or waistAnchor not assigned!");
            return;
        }

        bool isAttached = isLeftRope ? hookManager.leftHookAttached : hookManager.rightHookAttached;
        bool isRetracting = isLeftRope ? hookManager.leftHookRetracting : hookManager.rightHookRetracting;
        Vector3 grapplePoint = isLeftRope ? hookManager.leftGrapplePoint : hookManager.rightGrapplePoint;
        Vector3 anchorPosition = waistAnchor.position;

        if (isRetracting)
        {
            retractionTimer += Time.deltaTime;
            float t = retractionTimer / retractionTime;

            if (t >= 1f)
            {
                if (isLeftRope) hookManager.leftHookRetracting = false;
                else hookManager.rightHookRetracting = false;
                lr.positionCount = 0;
                retractionTimer = 0f;
                return;
            }

            currentGrapplePosition = Vector3.Lerp(grapplePoint, anchorPosition, t);
            for (int i = 0; i < quality + 1; i++)
            {
                float delta = i / (float)quality;
                Vector3 pos = Vector3.Lerp(anchorPosition, currentGrapplePosition, delta);
                lr.SetPosition(i, pos);
            }
        }
        else if (isAttached)
        {
            if (lr.positionCount == 0)
            {
                springVelocity = velocity;
                lr.positionCount = quality + 1;
            }

            UpdateSpring(Time.deltaTime);

            var up = Quaternion.LookRotation((grapplePoint - anchorPosition).normalized) * Vector3.up;
            currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * 12f);

            for (int i = 0; i < quality + 1; i++)
            {
                float delta = i / (float)quality;
                var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * springValue *
                             (affectCurve != null ? affectCurve.Evaluate(delta) : 1f);
                lr.SetPosition(i, Vector3.Lerp(anchorPosition, currentGrapplePosition, delta) + offset);
            }
        }
        else
        {
            currentGrapplePosition = anchorPosition;
            springValue = 0f;
            springVelocity = 0f;
            if (lr.positionCount > 0)
                lr.positionCount = 0;
            return;
        }

        Vector3[] positions = new Vector3[lr.positionCount];
        lr.GetPositions(positions);
        if (isLeftRope)
        {
            hookManager.leftHookLine.positionCount = positions.Length;
            hookManager.leftHookLine.SetPositions(positions);
        }
        else
        {
            hookManager.rightHookLine.positionCount = positions.Length;
            hookManager.rightHookLine.SetPositions(positions);
        }
    }

    void UpdateSpring(float deltaTime)
    {
        float force = -strength * (springValue - springTarget) - damper * springVelocity;
        springVelocity += force * deltaTime;
        springValue += springVelocity * deltaTime;
        springValue = Mathf.Clamp(springValue, -1f, 1f);
    }
}
