using UnityEngine;
using UnityEngine.XR;

namespace HockeyStickhandling
{
    public sealed class StickTracker : MonoBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private XRNode trackedController = XRNode.RightHand;

        [Header("Calibration")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] private float floorHeight = 0.06f;
        [SerializeField] private bool lockBladeToFloor;
        [SerializeField] private bool useYawOnlyRotation = true;
        [SerializeField] private float bladeGroundOffset;
        [SerializeField] private float maxLiftHeight = 0.24f;
        [SerializeField] private float directLiftScale = 5.5f;
        [SerializeField] private bool projectBladeAlongShaft = true;
        [SerializeField] private bool useRotationDerivedPosition;
        [SerializeField] private float bladeDistanceFromController = 0.95f;
        [SerializeField] private float rotationSideScale = 1.2f;
        [SerializeField] private float rotationDepthScale = 2.6f;
        [SerializeField] private float rotationLiftScale = 0.8f;
        [SerializeField] private bool useShaftProjectedHeight;
        [SerializeField] private bool invertShaftDirection;
        [SerializeField] private float bladeContactHalfWidth = 0.08f;
        [SerializeField] private float bladeContactHalfLength = 0.32f;
        [SerializeField] private float puckContactRadius = 0.14f;
        [SerializeField] private float directPuckVerticalTolerance = 0.05f;
        [SerializeField] private float maxBladeHeightAbovePuck = 0.04f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothing = 36.0f;
        [SerializeField] private float downwardPositionSmoothing = 90.0f;
        [SerializeField] private float rotationSmoothing = 26.0f;
        [SerializeField] private float snapDistance = 0.65f;
        [SerializeField] private float maxTrackedSpeed = 5.5f;
        [SerializeField] private float maxRotationStepDegrees = 55.0f;
        [SerializeField] private float diagnosticRefreshSeconds = 0.5f;

        private InputDevice controller;
        private Renderer bladeRenderer;
        private PuckController targetPuck;
        private Transform playAreaRoot;
        private Color idleColor;
        private Color contactColor = new Color(0.0f, 0.95f, 1.0f);
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 smoothedPosition;
        private Quaternion smoothedRotation;
        private Vector3 bladeVelocity;
        private Vector3 previousPosition;
        private bool hasSmoothedPose;
        private bool hasTargetPose;
        private bool hasControllerBaseline;
        private bool hasRotationBaseline;
        private Vector3 rotationBaselineForward;
        private Vector3 rotationBaselineLocalPosition;
        private float controllerBaselineY;
        private float liftAmount;
        private float lowestObservedTrackedY = float.PositiveInfinity;
        private bool lastPoseTracked;
        private bool lastConnected;
        private bool lastRecalibrateHeld;
        private int goodPoseFrames;
        private int lostPoseFrames;
        private int limitedPoseFrames;
        private int reportGoodPoseFrames;
        private int reportLostPoseFrames;
        private int reportLimitedPoseFrames;
        private float nextDiagnosticRefresh;
        private float lastGoodPoseTime = -999.0f;
        private float lastBladeContactTime = -999.0f;
        private string diagnosticText = "Stick tracking: waiting";

        public string DiagnosticText => diagnosticText;
        public Vector3 BladePosition => smoothedPosition;
        public Vector3 BladeVelocity => bladeVelocity;
        public float LastBladeContactTime => lastBladeContactTime;
        public bool HasTrackedPose => hasTargetPose;

        public void SetFloorHeight(float height)
        {
            floorHeight = height;
        }

        public void SetTargetPuck(PuckController puck)
        {
            targetPuck = puck;
        }

        public void SetPlayAreaRoot(Transform root)
        {
            playAreaRoot = root;
        }

        public void SetLockBladeToFloor(bool shouldLock)
        {
            lockBladeToFloor = shouldLock;
        }

        public void RecalibrateHeight()
        {
            hasControllerBaseline = false;
            hasRotationBaseline = false;
        }

        public void RecalibrateCurrentMount()
        {
            positionOffset = Vector3.zero;
            lowestObservedTrackedY = float.PositiveInfinity;
            hasControllerBaseline = false;
            hasRotationBaseline = false;
            hasSmoothedPose = false;
            hasTargetPose = false;
            previousPosition = transform.position;
        }

        private void Awake()
        {
            bladeRenderer = GetComponent<Renderer>();
            if (bladeRenderer != null)
            {
                idleColor = bladeRenderer.material.color;
            }

            targetPosition = transform.position;
            targetRotation = transform.rotation;
            smoothedPosition = transform.position;
            smoothedRotation = transform.rotation;
            previousPosition = transform.position;
        }

        private void Update()
        {
            if (!controller.isValid)
            {
                controller = InputDevices.GetDeviceAtXRNode(trackedController);
            }

            lastConnected = controller.isValid;
            if (!controller.TryGetFeatureValue(CommonUsages.devicePosition, out var controllerPosition) ||
                !controller.TryGetFeatureValue(CommonUsages.deviceRotation, out var controllerRotation))
            {
                RecordLostPose();
                RefreshDiagnostics();
                return;
            }

            if (controller.TryGetFeatureValue(CommonUsages.trackingState, out var trackingState) &&
                (!trackingState.HasFlag(InputTrackingState.Position) ||
                 !trackingState.HasFlag(InputTrackingState.Rotation)))
            {
                RecordLostPose();
                RefreshDiagnostics();
                return;
            }

            RecordGoodPose();
            var offset = controllerRotation * positionOffset;
            var trackedPosition = controllerPosition + offset;
            lowestObservedTrackedY = Mathf.Min(lowestObservedTrackedY, trackedPosition.y);
            if (!hasControllerBaseline)
            {
                controllerBaselineY = float.IsPositiveInfinity(lowestObservedTrackedY) ? trackedPosition.y : lowestObservedTrackedY;
                hasControllerBaseline = true;
            }

            if (lockBladeToFloor)
            {
                trackedPosition.y = floorHeight;
            }
            else
            {
                liftAmount = Mathf.Clamp((trackedPosition.y - controllerBaselineY) * directLiftScale, 0.0f, maxLiftHeight);
                trackedPosition.y = floorHeight + bladeGroundOffset + liftAmount;
            }

            if (projectBladeAlongShaft)
            {
                var shaftDirection = controllerRotation * Vector3.forward;
                if (invertShaftDirection)
                {
                    shaftDirection = -shaftDirection;
                }

                if (useRotationDerivedPosition && playAreaRoot != null)
                {
                    if (!hasRotationBaseline)
                    {
                        rotationBaselineForward = shaftDirection.normalized;
                        rotationBaselineLocalPosition = playAreaRoot.InverseTransformPoint(transform.position);
                        hasRotationBaseline = true;
                    }

                    var localShaft = playAreaRoot.InverseTransformDirection(shaftDirection.normalized);
                    var localBase = playAreaRoot.InverseTransformDirection(rotationBaselineForward);
                    var localDelta = localShaft - localBase;
                    var localPosition = rotationBaselineLocalPosition;
                    localPosition.x += localDelta.x * rotationSideScale;
                    localPosition.z += localDelta.z * rotationDepthScale;
                    var minBladeY = floorHeight + bladeGroundOffset;
                    var maxBladeY = minBladeY + maxLiftHeight;
                    var lift = Mathf.Clamp(localDelta.y * rotationLiftScale, 0.0f, maxLiftHeight);
                    var worldPosition = playAreaRoot.TransformPoint(localPosition);
                    worldPosition.y = Mathf.Clamp(minBladeY + lift, minBladeY, maxBladeY);
                    trackedPosition = worldPosition;
                }
                else
                {
                    var planarShaftDirection = Vector3.ProjectOnPlane(shaftDirection, Vector3.up);
                    if (planarShaftDirection.sqrMagnitude < 0.0001f)
                    {
                        planarShaftDirection = playAreaRoot != null ? playAreaRoot.forward : Vector3.forward;
                    }

                    var projectedBladePosition = controllerPosition + planarShaftDirection.normalized * bladeDistanceFromController;
                    if (useShaftProjectedHeight)
                    {
                        var minBladeY = floorHeight + bladeGroundOffset;
                        var maxBladeY = minBladeY + maxLiftHeight;
                        projectedBladePosition.y = Mathf.Clamp(projectedBladePosition.y, minBladeY, maxBladeY);
                    }
                    else
                    {
                        projectedBladePosition.y = trackedPosition.y;
                    }

                    trackedPosition = projectedBladePosition;
                }
            }

            var desiredRotation = useYawOnlyRotation
                ? Quaternion.Euler(0.0f, controllerRotation.eulerAngles.y, 0.0f) * Quaternion.Euler(rotationOffset)
                : controllerRotation * Quaternion.Euler(rotationOffset);

            LimitTrackingJump(ref trackedPosition, ref desiredRotation);
            targetPosition = trackedPosition;
            targetRotation = desiredRotation;
            hasTargetPose = true;

            HandleRecalibrationInput();
            RefreshDiagnostics();
        }

        private void HandleRecalibrationInput()
        {
            var recalibrateHeld =
                controller.TryGetFeatureValue(CommonUsages.gripButton, out var gripPressed) && gripPressed &&
                controller.TryGetFeatureValue(CommonUsages.primaryButton, out var primaryPressed) && primaryPressed;

            if (recalibrateHeld && !lastRecalibrateHeld)
            {
                RecalibrateCurrentMount();
            }

            lastRecalibrateHeld = recalibrateHeld;
        }

        private void LateUpdate()
        {
            if (!hasTargetPose)
            {
                return;
            }

            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            if (!hasSmoothedPose || Vector3.Distance(smoothedPosition, targetPosition) > snapDistance)
            {
                smoothedPosition = targetPosition;
                smoothedRotation = targetRotation;
                hasSmoothedPose = true;
            }
            else
            {
                var smoothing = targetPosition.y < smoothedPosition.y ? downwardPositionSmoothing : positionSmoothing;
                var positionBlend = 1.0f - Mathf.Exp(-smoothing * deltaTime);
                var rotationBlend = 1.0f - Mathf.Exp(-rotationSmoothing * deltaTime);
                smoothedPosition = Vector3.Lerp(smoothedPosition, targetPosition, positionBlend);
                smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, rotationBlend);
            }

            transform.SetPositionAndRotation(smoothedPosition, smoothedRotation);
            bladeVelocity = (smoothedPosition - previousPosition) / deltaTime;
            var contactStartPosition = previousPosition;
            ApplyPuckContacts(contactStartPosition, smoothedPosition, smoothedRotation);
            previousPosition = smoothedPosition;
        }

        private void LimitTrackingJump(ref Vector3 trackedPosition, ref Quaternion trackedRotation)
        {
            if (!hasSmoothedPose)
            {
                return;
            }

            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            var maxStep = maxTrackedSpeed * deltaTime;
            var positionDelta = trackedPosition - smoothedPosition;
            if (positionDelta.magnitude > maxStep)
            {
                trackedPosition = smoothedPosition + positionDelta.normalized * maxStep;
                limitedPoseFrames += 1;
            }

            var angleDelta = Quaternion.Angle(smoothedRotation, trackedRotation);
            if (angleDelta > maxRotationStepDegrees)
            {
                trackedRotation = Quaternion.RotateTowards(smoothedRotation, trackedRotation, maxRotationStepDegrees);
                limitedPoseFrames += 1;
            }
        }

        private void RecordGoodPose()
        {
            lastPoseTracked = true;
            lastGoodPoseTime = Time.time;
            goodPoseFrames += 1;
        }

        private void RecordLostPose()
        {
            lastPoseTracked = false;
            lostPoseFrames += 1;
        }

        private void RefreshDiagnostics()
        {
            if (Time.time < nextDiagnosticRefresh)
            {
                return;
            }

            var totalFrames = Mathf.Max(1, goodPoseFrames + lostPoseFrames);
            var trackedPercent = Mathf.RoundToInt(goodPoseFrames * 100.0f / totalFrames);
            reportGoodPoseFrames = goodPoseFrames;
            reportLostPoseFrames = lostPoseFrames;
            reportLimitedPoseFrames = limitedPoseFrames;
            goodPoseFrames = 0;
            lostPoseFrames = 0;
            limitedPoseFrames = 0;
            nextDiagnosticRefresh = Time.time + diagnosticRefreshSeconds;

            var status = lastPoseTracked ? "TRACKING" : "LOST";
            var connected = lastConnected ? "connected" : "not connected";
            var sinceGoodPose = Mathf.Max(0.0f, Time.time - lastGoodPoseTime);
            diagnosticText =
                $"Stick {status} ({connected})\n" +
                $"Pose {trackedPercent}% good  lost {reportLostPoseFrames}  limited {reportLimitedPoseFrames}\n" +
                $"Last good {sinceGoodPose:0.0}s  offset {bladeDistanceFromController:0.00}m  lift {liftAmount:0.00}m x{directLiftScale:0.0}\n" +
                "Grip + A: recalibrate";
        }

        private void ApplyPuckContacts(Vector3 contactStartPosition, Vector3 contactPosition, Quaternion contactRotation)
        {
            var hitPuck = false;
            if (targetPuck != null &&
                TryGetBladePuckContact(
                    contactStartPosition,
                    contactPosition,
                    contactRotation,
                    targetPuck.transform.position,
                    out var contactPoint))
            {
                targetPuck.ApplyStickContact(bladeVelocity, contactPoint);
                lastBladeContactTime = Time.time;
                hitPuck = true;
            }

            if (bladeRenderer != null)
            {
                bladeRenderer.material.color = hitPuck ? contactColor : idleColor;
            }
        }

        private bool TryGetBladePuckContact(
            Vector3 bladeStartPosition,
            Vector3 bladeEndPosition,
            Quaternion bladeRotation,
            Vector3 puckPosition,
            out Vector3 contactPoint)
        {
            if (IsBladeOverlappingPuck(bladeEndPosition, bladeRotation, puckPosition, out contactPoint))
            {
                return true;
            }

            var midPosition = Vector3.Lerp(bladeStartPosition, bladeEndPosition, 0.5f);
            if (IsBladeOverlappingPuck(midPosition, bladeRotation, puckPosition, out contactPoint))
            {
                return true;
            }

            return IsBladeOverlappingPuck(bladeStartPosition, bladeRotation, puckPosition, out contactPoint);
        }

        private bool IsBladeOverlappingPuck(
            Vector3 bladePosition,
            Quaternion bladeRotation,
            Vector3 puckPosition,
            out Vector3 contactPoint)
        {
            contactPoint = bladePosition;
            var verticalDelta = bladePosition.y - puckPosition.y;
            if (verticalDelta < -directPuckVerticalTolerance || verticalDelta > maxBladeHeightAbovePuck)
            {
                return false;
            }

            var localPuck = Quaternion.Inverse(bladeRotation) * (puckPosition - bladePosition);
            var closestLocal = new Vector3(
                Mathf.Clamp(localPuck.x, -bladeContactHalfWidth, bladeContactHalfWidth),
                0.0f,
                Mathf.Clamp(localPuck.z, -bladeContactHalfLength, bladeContactHalfLength));
            var planarDelta = new Vector2(localPuck.x - closestLocal.x, localPuck.z - closestLocal.z);
            var planarDistance = planarDelta.magnitude;
            if (planarDistance > puckContactRadius)
            {
                return false;
            }

            contactPoint = bladePosition + bladeRotation * closestLocal;
            return true;
        }
    }
}
