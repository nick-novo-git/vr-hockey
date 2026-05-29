using UnityEngine;
using UnityEngine.XR;

namespace HockeyStickhandling
{
    public sealed class StickTracker : MonoBehaviour
    {
        private enum StickTrackingMode
        {
            BaselineControllerProjection,
            MountedControllerShaft
        }

        public enum ActiveStickController
        {
            Auto,
            Left,
            Right
        }

        public enum ControllerMountOrientation
        {
            PointingDown,
            PointingUp
        }

        public enum ControllerAxisSource
        {
            Forward,
            Back,
            Up,
            Down,
            Right,
            Left
        }

        [System.Serializable]
        public sealed class MountedControllerProfile
        {
            public float controllerToBladeDistance = 1.45f;
            public float controllerToBladeForwardOffset;
            public float controllerToBladeVerticalOffset;
            public float controllerToBladeLateralOffset;
            public float controllerToBladeDirectionSign = 1.0f;
            public ControllerMountOrientation controllerMountOrientation = ControllerMountOrientation.PointingUp;
            public bool invertForwardAxis;
            public bool invertUpAxis;
            public bool invertRightAxis;
            public ControllerAxisSource shaftForwardAxisSource = ControllerAxisSource.Forward;
            public ControllerAxisSource shaftUpAxisSource = ControllerAxisSource.Up;
            public Vector3 controllerToStickRotationOffset = Vector3.zero;
            public Vector3 bladeRotationOffset = Vector3.zero;
            public Vector3 controllerForwardAxis = Vector3.forward;
            public Vector3 controllerUpAxis = Vector3.up;
            public Vector3 controllerRightAxis = Vector3.right;
            public Vector3 shaftForwardAxis = Vector3.forward;
            public Vector3 bladeUpAxis = Vector3.up;
            public Vector3 bladeForwardAxis = Vector3.forward;
            public float bladeLiftMultiplier = 2.5f;
            public float bladeRotationMultiplier = 1.0f;
            public float smallMotionSensitivity = 1.25f;
            public float contactClearanceMultiplier = 1.25f;
        }

        [Header("Tracking")]
        [SerializeField] private XRNode trackedController = XRNode.RightHand;
        [SerializeField] private StickTrackingMode trackingMode = StickTrackingMode.BaselineControllerProjection;
        [SerializeField] private ActiveStickController activeStickController = ActiveStickController.Auto;
        [SerializeField] private bool autoDetectMountedController = true;

        [Header("Calibration")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] private float bladeHeightOffset;
        [SerializeField] private float bladeForwardBackOffset;
        [SerializeField] private float bladeLeftRightOffset;
        [SerializeField] private float floorHeight = 0.06f;
        [SerializeField] private bool lockBladeToFloor;
        [SerializeField] private bool useYawOnlyRotation = true;
        [SerializeField] private float bladeGroundOffset;
        [SerializeField] private float maxLiftOffset = 0.24f;
        [SerializeField] private float liftUpMultiplier = 72.0f;
        [SerializeField] private bool projectBladeAlongShaft = true;
        [SerializeField] private bool useYawOnlyBladeProjection = true;
        [SerializeField] private bool useShaftPitchForLift = true;
        [SerializeField] private float shaftPitchLiftUpMultiplier = 1.65f;
        [SerializeField] private bool autoDetectShaftPitchLiftDirection = true;
        [SerializeField] private bool useAbsoluteShaftPitchLift;
        [SerializeField] private bool invertShaftPitchLift;
        [SerializeField] private bool useRotationDerivedPosition;
        [SerializeField] private float bladeDistanceFromController = 0.95f;
        [SerializeField] private float rotationSideScale = 1.2f;
        [SerializeField] private float rotationDepthScale = 2.6f;
        [SerializeField] private float rotationLiftScale = 0.8f;
        [SerializeField] private bool useShaftProjectedHeight;
        [SerializeField] private bool invertShaftDirection;
        [SerializeField] private float bladeContactHalfWidth = 0.055f;
        [SerializeField] private float bladeContactHalfLength = 0.27f;
        [SerializeField] private float puckContactRadius = 0.12f;
        [SerializeField] private float directPuckVerticalTolerance = 0.05f;
        [SerializeField] private float bladeContactClearance = 0.0015f;
        [SerializeField] private float puckContactHeight = 0.024f;
        [SerializeField] private float contactIgnoreHeight = 0.001f;
        [SerializeField] private float maxPuckDepenetrationPerContact = 0.014f;
        [SerializeField] private float topDragRejectHeight;
        [SerializeField] private float liftRisingVelocityThreshold = 0.0015f;
        [SerializeField] private float arcContactLockoutSeconds = 0.34f;
        [SerializeField] private float contactReenableBelowPuckTop = 0.008f;
        [SerializeField] private float maxStickHeightAbovePuck = 0.75f;
        [SerializeField] private float maxAllowedFrameToFrameStickMovement = 0.35f;
        [SerializeField] private bool temporarilyDisablePuckStickCollision = true;

        [Header("Mounted Controller Stick")]
        [SerializeField] private float controllerToBladeDistance = 1.45f;
        [SerializeField] private float controllerToBladeForwardOffset;
        [SerializeField] private float controllerToBladeVerticalOffset;
        [SerializeField] private float controllerToBladeLateralOffset;
        [SerializeField] private float controllerToBladeDirectionSign = 1.0f;
        [SerializeField] private ControllerMountOrientation controllerMountOrientation = ControllerMountOrientation.PointingUp;
        [SerializeField] private bool invertForwardAxis;
        [SerializeField] private bool invertUpAxis;
        [SerializeField] private bool invertRightAxis;
        [SerializeField] private ControllerAxisSource shaftForwardAxisSource = ControllerAxisSource.Forward;
        [SerializeField] private ControllerAxisSource shaftUpAxisSource = ControllerAxisSource.Up;
        [SerializeField] private Vector3 controllerToStickRotationOffset = Vector3.zero;
        [SerializeField] private Vector3 bladeRotationOffset = Vector3.zero;
        [SerializeField] private Vector3 controllerForwardAxis = Vector3.forward;
        [SerializeField] private Vector3 controllerUpAxis = Vector3.up;
        [SerializeField] private Vector3 controllerRightAxis = Vector3.right;
        [SerializeField] private Vector3 shaftForwardAxis = Vector3.forward;
        [SerializeField] private Vector3 bladeUpAxis = Vector3.up;
        [SerializeField] private Vector3 bladeForwardAxis = Vector3.forward;
        [SerializeField] private float bladeLiftMultiplier = 2.5f;
        [SerializeField] private float bladeRotationMultiplier = 1.0f;
        [SerializeField] private float smallMotionSensitivity = 1.25f;
        [SerializeField] private float contactClearanceMultiplier = 1.25f;
        [SerializeField] private float automaticCalibrationSeconds = 3.0f;
        [SerializeField] private int minimumCalibrationSamples = 30;
        [SerializeField] private bool autoFlipBladeDirectionTowardPlayArea;
        [SerializeField] private bool clampMountedBladeToIce = true;
        [SerializeField] private bool keepMountedBladeFlatOnIce = true;
        [SerializeField] private bool useBladeFaceRollForFlatRotation = true;
        [SerializeField] private float mountedBladeMinimumForward = 0.15f;
        [SerializeField] private float maxCalibrationSampleMovement = 0.08f;
        [SerializeField] private float maxCalibrationSampleAngle = 12.0f;
        [SerializeField] private float requiredStableCalibrationSeconds = 1.0f;
        [SerializeField] private float maxStableCalibrationSpeed = 0.08f;
        [SerializeField] private float maxStableCalibrationAngularSpeed = 18.0f;
        [SerializeField] private MountedControllerProfile leftControllerProfile = new MountedControllerProfile();
        [SerializeField] private MountedControllerProfile rightControllerProfile = new MountedControllerProfile();

        [Header("Experimental Virtual Ice Plane")]
        [SerializeField] private bool useVirtualIcePlaneOffset;
        [SerializeField] private float virtualIcePlaneYOffset;
        [SerializeField] private float bladeContactPlaneYOffset;
        [SerializeField] private float puckContactPlaneYOffset;
        [SerializeField] private float visualStickYOffset;
        [SerializeField] private float contactOnlyYOffset;
        [SerializeField] private float maxVisualStickYOffset = 0.15f;

        [Header("Debug Virtual Blade")]
        [SerializeField] private bool showBladeDebugMarkers;
        [SerializeField] private float debugControllerToBladeDistance = 0.95f;
        [SerializeField] private float debugControllerToBladeVerticalOffset;
        [SerializeField] private float debugControllerToBladeForwardOffset;
        [SerializeField] private float debugArrowLength = 0.55f;

        [Header("Stick Shaft Visual")]
        [SerializeField] private bool showTrackedShaftVisual;
        [SerializeField] private float trackedShaftVisualThickness = 0.035f;

        [Header("Stick Model Visual")]
        [SerializeField] private bool alignStickModelVisualToController = true;
        [SerializeField] private string stickModelVisualName = "Hockey Stick Model Visual";
        [SerializeField] private float stickModelVisualScale = 0.012f;
        [SerializeField] private float stickModelVisualLength = 1.68f;
        [SerializeField] private bool alignStickModelBladeToPhysicsBlade = true;
        [SerializeField] private bool invertStickModelVisualUp;
        [SerializeField] private Vector3 stickModelVisualRotationOffset = Vector3.zero;
        [SerializeField] private Vector3 stickModelVisualPositionOffset = new Vector3(0.0f, 0.045f, 0.0f);

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothing = 36.0f;
        [SerializeField] private float downwardPositionSmoothing = 260.0f;
        [SerializeField] private float rotationSmoothing = 26.0f;
        [SerializeField] private float snapDistance = 0.65f;
        [SerializeField] private float maxTrackedSpeed = 5.5f;
        [SerializeField] private float maxDownwardSpeed = 18.0f;
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
        private Transform calculatedBladeDebugMarker;
        private Transform visualBladeDebugMarker;
        private Transform controllerMountDebugMarker;
        private Transform shaftLineDebugMarker;
        private Transform bladeContactZoneDebugMarker;
        private Transform trackedShaftVisual;
        private Transform stickModelVisual;
        private Transform controllerForwardDebugArrow;
        private Transform controllerUpDebugArrow;
        private Transform controllerRightDebugArrow;
        private Transform shaftForwardDebugArrow;
        private Transform bladeForwardDebugArrow;
        private Vector3 bladeVelocity;
        private Vector3 previousPosition;
        private bool hasSmoothedPose;
        private bool hasTargetPose;
        private bool hasControllerBaseline;
        private bool hasRotationBaseline;
        private bool hasShaftLiftBaseline;
        private bool hasShaftLiftSign;
        private Vector3 rotationBaselineForward;
        private Vector3 rotationBaselineLocalPosition;
        private float controllerBaselineY;
        private float shaftLiftBaselineY;
        private float shaftLiftSign = 1.0f;
        private float liftAmount;
        private float visualLiftAmount;
        private bool puckContactsEnabled = true;
        private bool isAutoCalibrating;
        private bool hasMountedRestCalibration;
        private int calibrationSampleCount;
        private int leftDetectionSamples;
        private int rightDetectionSamples;
        private float calibrationStartedAt;
        private float calibrationEndsAt;
        private float leftDetectionMotion;
        private float rightDetectionMotion;
        private Vector3 calibrationPositionSum;
        private Quaternion calibrationRotationAverage = Quaternion.identity;
        private float calibrationStableStartedAt;
        private float lastCalibrationSampleTime;
        private Vector3 lastLeftDetectionPosition;
        private Vector3 lastRightDetectionPosition;
        private Vector3 lastCalibrationSamplePosition;
        private Quaternion lastCalibrationSampleRotation = Quaternion.identity;
        private bool hasLastLeftDetectionPosition;
        private bool hasLastRightDetectionPosition;
        private Vector3 mountedRestBladePoint;
        private Quaternion mountedRestBladeRotation = Quaternion.identity;
        private float mountedRestYOffset;
        private float mountedBladeDirectionSign = 1.0f;
        private float lastControllerLift;
        private float lastShaftLift;
        private float lastBladeBottomY;
        private float lastBladeTipY;
        private float lastPuckCenterY;
        private float lastPuckTopY;
        private float lastBladeHeightAbovePuck;
        private Vector3 lastControllerPosition;
        private Vector3 lastCalculatedBladePoint;
        private Vector3 lastBladeBottomPoint;
        private Vector3 lastControllerForwardDirection = Vector3.forward;
        private Vector3 lastControllerUpDirection = Vector3.up;
        private Vector3 lastControllerRightDirection = Vector3.right;
        private Vector3 lastBladeForwardDirection = Vector3.forward;
        private Vector3 lastRawShaftDirection;
        private Vector3 lastPlanarShaftDirection;
        private Quaternion stickModelVisualRestCorrection = Quaternion.identity;
        private bool lastContactAllowed;
        private string lastContactReason = "none";
        private float contactIgnoredUntil;
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
        private string detectedControllerLabel = "None";
        private string controllerTrackingStatus = "not checked";

        public string DiagnosticText => diagnosticText;
        public Vector3 BladePosition => smoothedPosition;
        public Vector3 BladeVelocity => bladeVelocity;
        public Vector3 CalculatedBladePoint => lastCalculatedBladePoint;
        public Vector3 BladeBottomPoint => lastBladeBottomPoint;
        public float LastBladeContactTime => lastBladeContactTime;
        public bool HasTrackedPose => hasTargetPose;
        public bool IsAutoCalibrating => isAutoCalibrating;
        public bool HasMountedRestCalibration => hasMountedRestCalibration;
        public float AutoCalibrationRemaining => isAutoCalibrating ? Mathf.Max(0.0f, calibrationEndsAt - Time.time) : 0.0f;
        public float StableCalibrationSeconds => calibrationStableStartedAt > 0.0f ? Mathf.Max(0.0f, Time.time - calibrationStableStartedAt) : 0.0f;
        public string CalibrationStatus =>
            isAutoCalibrating
                ? $"Rest your stick naturally on the ice\nDetected: {detectedControllerLabel}\nTracking: {controllerTrackingStatus}\nHold still {StableCalibrationSeconds:0.0}/{requiredStableCalibrationSeconds:0.0}s  samples {calibrationSampleCount}\nBlade {FormatVector(lastCalculatedBladePoint)}  Shaft {FormatVector(lastRawShaftDirection)}"
                : hasMountedRestCalibration || trackingMode != StickTrackingMode.MountedControllerShaft
                    ? string.Empty
                    : "Rest your stick naturally on the ice";

        public void ApplyMountCalibration(
            Vector3 controllerPositionOffset,
            Vector3 controllerRotationOffset,
            float heightOffset,
            float forwardBackOffset,
            float leftRightOffset)
        {
            positionOffset = controllerPositionOffset;
            rotationOffset = controllerRotationOffset;
            bladeHeightOffset = heightOffset;
            bladeForwardBackOffset = forwardBackOffset;
            bladeLeftRightOffset = leftRightOffset;
        }

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

        public void SetMountedControllerMode(bool enabled)
        {
            trackingMode = enabled
                ? StickTrackingMode.MountedControllerShaft
                : StickTrackingMode.BaselineControllerProjection;
        }

        public void SetActiveStickController(ActiveStickController controllerSelection)
        {
            activeStickController = controllerSelection;
            ResolveConfiguredController();
        }

        public void SetAutoDetectMountedController(bool enabled)
        {
            autoDetectMountedController = enabled;
        }

        public void ConfigureMountedControllerProfiles(
            MountedControllerProfile leftProfile,
            MountedControllerProfile rightProfile)
        {
            if (leftProfile != null)
            {
                leftControllerProfile = leftProfile;
            }

            if (rightProfile != null)
            {
                rightControllerProfile = rightProfile;
            }

            ApplyMountedProfileForNode(trackedController);
        }

        public void ConfigureMountedStick(
            float bladeDistance,
            float forwardOffset,
            float verticalOffset,
            float lateralOffset,
            float bladeDirectionSign,
            ControllerMountOrientation mountOrientation,
            bool shouldInvertForwardAxis,
            bool shouldInvertUpAxis,
            bool shouldInvertRightAxis,
            ControllerAxisSource mountedShaftForwardAxisSource,
            ControllerAxisSource mountedShaftUpAxisSource,
            Vector3 stickRotationOffset,
            Vector3 mountedBladeRotationOffset,
            Vector3 mountedControllerForwardAxis,
            Vector3 mountedControllerUpAxis,
            Vector3 mountedControllerRightAxis,
            Vector3 mountedShaftForwardAxis,
            Vector3 mountedBladeUpAxis,
            Vector3 mountedBladeForwardAxis,
            float mountedBladeLiftMultiplier,
            float mountedBladeRotationMultiplier,
            float mountedSmallMotionSensitivity,
            float mountedContactClearanceMultiplier)
        {
            controllerToBladeDistance = bladeDistance;
            controllerToBladeForwardOffset = forwardOffset;
            controllerToBladeVerticalOffset = verticalOffset;
            controllerToBladeLateralOffset = lateralOffset;
            controllerToBladeDirectionSign = Mathf.Sign(Mathf.Approximately(bladeDirectionSign, 0.0f) ? 1.0f : bladeDirectionSign);
            controllerMountOrientation = mountOrientation;
            invertForwardAxis = shouldInvertForwardAxis;
            invertUpAxis = shouldInvertUpAxis;
            invertRightAxis = shouldInvertRightAxis;
            shaftForwardAxisSource = mountedShaftForwardAxisSource;
            shaftUpAxisSource = mountedShaftUpAxisSource;
            controllerToStickRotationOffset = stickRotationOffset;
            bladeRotationOffset = mountedBladeRotationOffset;
            controllerForwardAxis = mountedControllerForwardAxis;
            controllerUpAxis = mountedControllerUpAxis;
            controllerRightAxis = mountedControllerRightAxis;
            shaftForwardAxis = mountedShaftForwardAxis;
            bladeUpAxis = mountedBladeUpAxis;
            bladeForwardAxis = mountedBladeForwardAxis;
            bladeLiftMultiplier = mountedBladeLiftMultiplier;
            bladeRotationMultiplier = mountedBladeRotationMultiplier;
            smallMotionSensitivity = mountedSmallMotionSensitivity;
            contactClearanceMultiplier = mountedContactClearanceMultiplier;
            leftControllerProfile = CreateCurrentMountedProfile();
            rightControllerProfile = CreateCurrentMountedProfile();
        }

        public void SetPuckContactsEnabled(bool enabled)
        {
            puckContactsEnabled = enabled;
        }

        public void SetTemporaryPuckStickCollisionDisabled(bool disabled)
        {
            temporarilyDisablePuckStickCollision = disabled;
            if (disabled)
            {
                SetPuckContactsEnabled(false);
            }
        }

        public void BeginAutomaticRestCalibration(float durationSeconds = -1.0f)
        {
            calibrationStartedAt = Time.time;
            calibrationEndsAt = Time.time + (durationSeconds > 0.0f ? durationSeconds : automaticCalibrationSeconds);
            calibrationSampleCount = 0;
            leftDetectionSamples = 0;
            rightDetectionSamples = 0;
            leftDetectionMotion = 0.0f;
            rightDetectionMotion = 0.0f;
            calibrationPositionSum = Vector3.zero;
            calibrationRotationAverage = Quaternion.identity;
            calibrationStableStartedAt = 0.0f;
            lastCalibrationSampleTime = 0.0f;
            lastLeftDetectionPosition = Vector3.zero;
            lastRightDetectionPosition = Vector3.zero;
            lastCalibrationSamplePosition = Vector3.zero;
            lastCalibrationSampleRotation = Quaternion.identity;
            hasLastLeftDetectionPosition = false;
            hasLastRightDetectionPosition = false;
            stickModelVisualRestCorrection = Quaternion.identity;
            mountedBladeDirectionSign = 1.0f;
            detectedControllerLabel = "Detecting";
            controllerTrackingStatus = "waiting for controller pose";
            hasMountedRestCalibration = false;
            isAutoCalibrating = trackingMode == StickTrackingMode.MountedControllerShaft;
            SetPuckContactsEnabled(false);
            ResolveConfiguredController();
        }

        public void RecalibrateHeight()
        {
            hasControllerBaseline = false;
            hasRotationBaseline = false;
        }

        public void RecalibrateCurrentMount()
        {
            lowestObservedTrackedY = float.PositiveInfinity;
            hasControllerBaseline = false;
            hasRotationBaseline = false;
            hasShaftLiftBaseline = false;
            hasShaftLiftSign = false;
            hasMountedRestCalibration = false;
            isAutoCalibrating = false;
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
            ResolveControllerBeforePoseRead();
            controller = XRControllerDiagnostics.GetState(trackedController).device;

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
            lastControllerPosition = controllerPosition;
            lowestObservedTrackedY = Mathf.Min(lowestObservedTrackedY, trackedPosition.y);
            if (!hasControllerBaseline)
            {
                controllerBaselineY = float.IsPositiveInfinity(lowestObservedTrackedY) ? trackedPosition.y : lowestObservedTrackedY;
                hasControllerBaseline = true;
            }

            var shaftDirection = controllerRotation * Vector3.forward;
            if (invertShaftDirection)
            {
                shaftDirection = -shaftDirection;
            }

            lastRawShaftDirection = shaftDirection.normalized;
            if (!hasShaftLiftBaseline)
            {
                shaftLiftBaselineY = lastRawShaftDirection.y;
                hasShaftLiftBaseline = true;
            }

            if (trackingMode == StickTrackingMode.MountedControllerShaft)
            {
                UpdateMountedControllerStick(controllerPosition, controllerRotation, trackedPosition);
                HandleRecalibrationInput();
                RefreshDiagnostics();
                return;
            }

            lastCalculatedBladePoint = CalculateDebugVirtualBladePoint(trackedPosition, controllerRotation, lastRawShaftDirection);

            if (lockBladeToFloor)
            {
                trackedPosition.y = floorHeight;
            }
            else
            {
                var rawControllerLift = Mathf.Max(0.0f, trackedPosition.y - controllerBaselineY);
                var controllerLift = rawControllerLift * liftUpMultiplier;
                var shaftPitchDelta = lastRawShaftDirection.y - shaftLiftBaselineY;
                if (invertShaftPitchLift)
                {
                    shaftPitchDelta = -shaftPitchDelta;
                }

                if (useAbsoluteShaftPitchLift)
                {
                    shaftPitchDelta = Mathf.Abs(shaftPitchDelta);
                }
                else if (autoDetectShaftPitchLiftDirection)
                {
                    if (!hasShaftLiftSign && Mathf.Abs(shaftPitchDelta) > 0.01f)
                    {
                        shaftLiftSign = Mathf.Sign(shaftPitchDelta);
                        hasShaftLiftSign = true;
                    }

                    shaftPitchDelta *= shaftLiftSign;
                }

                lastControllerLift = controllerLift;
                var shaftLift = useShaftPitchForLift ? Mathf.Max(0.0f, shaftPitchDelta * shaftPitchLiftUpMultiplier) : 0.0f;
                lastShaftLift = shaftLift;
                liftAmount = Mathf.Clamp(Mathf.Max(controllerLift, shaftLift), 0.0f, maxLiftOffset);
                visualLiftAmount = Mathf.Clamp(rawControllerLift, 0.0f, maxLiftOffset);
                trackedPosition.y = floorHeight + bladeGroundOffset + visualLiftAmount;
            }

            if (projectBladeAlongShaft)
            {
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
                    var maxBladeY = minBladeY + maxLiftOffset;
                    var lift = Mathf.Clamp(localDelta.y * rotationLiftScale, 0.0f, maxLiftOffset);
                    var worldPosition = playAreaRoot.TransformPoint(localPosition);
                    worldPosition.y = Mathf.Clamp(minBladeY + lift, minBladeY, maxBladeY);
                    trackedPosition = ApplyBladeMountOffset(worldPosition);
                }
                else
                {
                    var planarShaftDirection = useYawOnlyBladeProjection
                        ? Quaternion.Euler(0.0f, controllerRotation.eulerAngles.y, 0.0f) * (invertShaftDirection ? Vector3.back : Vector3.forward)
                        : Vector3.ProjectOnPlane(shaftDirection, Vector3.up);
                    if (planarShaftDirection.sqrMagnitude < 0.0001f)
                    {
                        planarShaftDirection = hasTargetPose
                            ? lastPlanarShaftDirection
                            : playAreaRoot != null ? playAreaRoot.forward : Vector3.forward;
                    }

                    lastPlanarShaftDirection = planarShaftDirection.normalized;
                    var projectedBladePosition = trackedPosition + planarShaftDirection.normalized * bladeDistanceFromController;
                    if (useShaftProjectedHeight)
                    {
                        var minBladeY = floorHeight + bladeGroundOffset;
                        var maxBladeY = minBladeY + maxLiftOffset;
                        projectedBladePosition.y = Mathf.Clamp(projectedBladePosition.y, minBladeY, maxBladeY);
                    }
                    else
                    {
                        projectedBladePosition.y = trackedPosition.y;
                    }

                    trackedPosition = ApplyBladeMountOffset(projectedBladePosition);
                }
            }
            else
            {
                trackedPosition = ApplyBladeMountOffset(trackedPosition);
            }

            if (useVirtualIcePlaneOffset && Mathf.Abs(visualStickYOffset) > 0.0001f)
            {
                trackedPosition.y += Mathf.Clamp(visualStickYOffset, -maxVisualStickYOffset, maxVisualStickYOffset);
            }

            var desiredRotation = useYawOnlyRotation
                ? Quaternion.Euler(0.0f, controllerRotation.eulerAngles.y, 0.0f) * Quaternion.Euler(rotationOffset)
                : controllerRotation * Quaternion.Euler(rotationOffset);

            if (!IsValidPose(trackedPosition, desiredRotation))
            {
                RecordLostPose();
                RefreshDiagnostics();
                return;
            }

            ClampUnsafeStickHeight(ref trackedPosition);
            LimitTrackingJump(ref trackedPosition, ref desiredRotation);
            targetPosition = trackedPosition;
            targetRotation = desiredRotation;
            hasTargetPose = true;

            HandleRecalibrationInput();
            RefreshDiagnostics();
        }

        private void UpdateMountedControllerStick(
            Vector3 controllerPosition,
            Quaternion controllerRotation,
            Vector3 offsetControllerPosition)
        {
            var mappedStickRotation = GetMappedStickRotation(controllerRotation);
            lastRawShaftDirection = (mappedStickRotation * SafeAxis(shaftForwardAxis, Vector3.forward)).normalized;
            lastControllerForwardDirection = (mappedStickRotation * Vector3.forward).normalized;
            lastControllerUpDirection = (mappedStickRotation * Vector3.up).normalized;
            lastControllerRightDirection = (mappedStickRotation * Vector3.right).normalized;
            lastCalculatedBladePoint = CalculateMountedBladePoint(offsetControllerPosition, controllerRotation);

            if (isAutoCalibrating)
            {
                SampleMountedRestPose(offsetControllerPosition, controllerRotation);
                if (Time.time >= calibrationEndsAt &&
                    calibrationSampleCount >= minimumCalibrationSamples &&
                    StableCalibrationSeconds >= requiredStableCalibrationSeconds)
                {
                    FinishMountedRestCalibration();
                }

                hasTargetPose = false;
                return;
            }

            if (!hasMountedRestCalibration)
            {
                BeginAutomaticRestCalibration();
                hasTargetPose = false;
                return;
            }

            var rawBladePoint = CalculateMountedBladePoint(offsetControllerPosition, controllerRotation);
            var rawBladeRotation = GetMountedBladeGameplayRotation(CalculateMountedBladeRotation(controllerRotation));
            var restDelta = rawBladePoint - mountedRestBladePoint;
            var bladePosition = mountedRestBladePoint +
                                new Vector3(
                                    restDelta.x * smallMotionSensitivity,
                                    restDelta.y * bladeLiftMultiplier,
                                    restDelta.z * smallMotionSensitivity);
            bladePosition.y += mountedRestYOffset;
            var rotationMultiplier = Mathf.Max(0.0f, bladeRotationMultiplier);
            var bladeRotation = rotationMultiplier <= 0.0f
                ? mountedRestBladeRotation
                : Quaternion.SlerpUnclamped(mountedRestBladeRotation, rawBladeRotation, rotationMultiplier);
            lastBladeForwardDirection = (bladeRotation * Vector3.forward).normalized;

            lastControllerLift = Mathf.Max(0.0f, bladePosition.y - (floorHeight + bladeGroundOffset));
            lastShaftLift = Mathf.Max(0.0f, lastRawShaftDirection.y - shaftLiftBaselineY);
            liftAmount = Mathf.Clamp(lastControllerLift, 0.0f, maxLiftOffset);
            visualLiftAmount = liftAmount;

            if (!IsValidPose(bladePosition, bladeRotation))
            {
                RecordLostPose();
                return;
            }

            ClampMountedBladeToIce(ref bladePosition);
            ClampUnsafeStickHeight(ref bladePosition);
            KeepMountedBladeInFront(ref bladePosition);
            LimitTrackingJump(ref bladePosition, ref bladeRotation);
            targetPosition = bladePosition;
            targetRotation = bladeRotation;
            hasTargetPose = true;
            SetPuckContactsEnabled(!temporarilyDisablePuckStickCollision);
        }

        private void ResolveControllerBeforePoseRead()
        {
            if (trackingMode != StickTrackingMode.MountedControllerShaft)
            {
                ResolveConfiguredController();
                return;
            }

            if (activeStickController != ActiveStickController.Auto || !autoDetectMountedController)
            {
                ResolveConfiguredController();
                return;
            }

            if (isAutoCalibrating || !hasMountedRestCalibration)
            {
                AutoDetectControllerFromSetup();
            }
        }

        private void ResolveConfiguredController()
        {
            if (activeStickController == ActiveStickController.Left)
            {
                SetTrackedControllerNode(XRNode.LeftHand);
            }
            else if (activeStickController == ActiveStickController.Right)
            {
                SetTrackedControllerNode(XRNode.RightHand);
            }
        }

        private void AutoDetectControllerFromSetup()
        {
            XRControllerDiagnostics.LogBothStates("StickControllerDetect");
            var leftState = XRControllerDiagnostics.GetState(XRNode.LeftHand);
            var rightState = XRControllerDiagnostics.GetState(XRNode.RightHand);
            var leftValid = leftState.HasTrackedPose;
            var rightValid = rightState.HasTrackedPose;
            var leftPosition = leftState.position;
            var rightPosition = rightState.position;

            if (leftValid)
            {
                leftDetectionSamples += 1;
                if (hasLastLeftDetectionPosition)
                {
                    leftDetectionMotion += Vector3.Distance(leftPosition, lastLeftDetectionPosition);
                }

                lastLeftDetectionPosition = leftPosition;
                hasLastLeftDetectionPosition = true;
            }

            if (rightValid)
            {
                rightDetectionSamples += 1;
                if (hasLastRightDetectionPosition)
                {
                    rightDetectionMotion += Vector3.Distance(rightPosition, lastRightDetectionPosition);
                }

                lastRightDetectionPosition = rightPosition;
                hasLastRightDetectionPosition = true;
            }

            if (leftState.selectPressed && leftValid)
            {
                SetTrackedControllerNode(XRNode.LeftHand);
                controllerTrackingStatus = "left selected by input";
                return;
            }

            if (rightState.selectPressed && rightValid)
            {
                SetTrackedControllerNode(XRNode.RightHand);
                controllerTrackingStatus = "right selected by input";
                return;
            }

            if (leftValid && !rightValid)
            {
                SetTrackedControllerNode(XRNode.LeftHand);
            }
            else if (rightValid && !leftValid)
            {
                SetTrackedControllerNode(XRNode.RightHand);
            }
            else if (leftValid && rightValid)
            {
                var leftScore = GetControllerDetectionScore(leftDetectionSamples, leftDetectionMotion);
                var rightScore = GetControllerDetectionScore(rightDetectionSamples, rightDetectionMotion);
                SetTrackedControllerNode(leftScore > rightScore ? XRNode.LeftHand : XRNode.RightHand);
            }

            controllerTrackingStatus =
                $"{XRControllerDiagnostics.FormatState("L", leftState)} samples:{leftDetectionSamples} stable:{GetStabilityScore(leftDetectionSamples, leftDetectionMotion):0.00}\n" +
                $"{XRControllerDiagnostics.FormatState("R", rightState)} samples:{rightDetectionSamples} stable:{GetStabilityScore(rightDetectionSamples, rightDetectionMotion):0.00}";
        }

        private void SetTrackedControllerNode(XRNode node)
        {
            if (trackedController == node)
            {
                detectedControllerLabel = node == XRNode.LeftHand ? "Left" : "Right";
                return;
            }

            trackedController = node;
            controller = default;
            detectedControllerLabel = node == XRNode.LeftHand ? "Left" : "Right";
            ApplyMountedProfileForNode(node);
            if (isAutoCalibrating)
            {
                calibrationSampleCount = 0;
                calibrationPositionSum = Vector3.zero;
                calibrationRotationAverage = Quaternion.identity;
                calibrationStableStartedAt = 0.0f;
                lastCalibrationSampleTime = 0.0f;
                lastCalibrationSamplePosition = Vector3.zero;
                lastCalibrationSampleRotation = Quaternion.identity;
            }
        }

        private void ApplyMountedProfileForNode(XRNode node)
        {
            ApplyMountedProfile(node == XRNode.LeftHand ? leftControllerProfile : rightControllerProfile);
        }

        private void ApplyMountedProfile(MountedControllerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            controllerToBladeDistance = profile.controllerToBladeDistance;
            controllerToBladeForwardOffset = profile.controllerToBladeForwardOffset;
            controllerToBladeVerticalOffset = profile.controllerToBladeVerticalOffset;
            controllerToBladeLateralOffset = profile.controllerToBladeLateralOffset;
            controllerToBladeDirectionSign = Mathf.Sign(Mathf.Approximately(profile.controllerToBladeDirectionSign, 0.0f) ? 1.0f : profile.controllerToBladeDirectionSign);
            controllerMountOrientation = profile.controllerMountOrientation;
            invertForwardAxis = profile.invertForwardAxis;
            invertUpAxis = profile.invertUpAxis;
            invertRightAxis = profile.invertRightAxis;
            shaftForwardAxisSource = profile.shaftForwardAxisSource;
            shaftUpAxisSource = profile.shaftUpAxisSource;
            controllerToStickRotationOffset = profile.controllerToStickRotationOffset;
            bladeRotationOffset = profile.bladeRotationOffset;
            controllerForwardAxis = profile.controllerForwardAxis;
            controllerUpAxis = profile.controllerUpAxis;
            controllerRightAxis = profile.controllerRightAxis;
            shaftForwardAxis = profile.shaftForwardAxis;
            bladeUpAxis = profile.bladeUpAxis;
            bladeForwardAxis = profile.bladeForwardAxis;
            bladeLiftMultiplier = profile.bladeLiftMultiplier;
            bladeRotationMultiplier = profile.bladeRotationMultiplier;
            smallMotionSensitivity = profile.smallMotionSensitivity;
            contactClearanceMultiplier = profile.contactClearanceMultiplier;
        }

        private MountedControllerProfile CreateCurrentMountedProfile()
        {
            return new MountedControllerProfile
            {
                controllerToBladeDistance = controllerToBladeDistance,
                controllerToBladeForwardOffset = controllerToBladeForwardOffset,
                controllerToBladeVerticalOffset = controllerToBladeVerticalOffset,
                controllerToBladeLateralOffset = controllerToBladeLateralOffset,
                controllerToBladeDirectionSign = controllerToBladeDirectionSign,
                controllerMountOrientation = controllerMountOrientation,
                invertForwardAxis = invertForwardAxis,
                invertUpAxis = invertUpAxis,
                invertRightAxis = invertRightAxis,
                shaftForwardAxisSource = shaftForwardAxisSource,
                shaftUpAxisSource = shaftUpAxisSource,
                controllerToStickRotationOffset = controllerToStickRotationOffset,
                bladeRotationOffset = bladeRotationOffset,
                controllerForwardAxis = controllerForwardAxis,
                controllerUpAxis = controllerUpAxis,
                controllerRightAxis = controllerRightAxis,
                shaftForwardAxis = shaftForwardAxis,
                bladeUpAxis = bladeUpAxis,
                bladeForwardAxis = bladeForwardAxis,
                bladeLiftMultiplier = bladeLiftMultiplier,
                bladeRotationMultiplier = bladeRotationMultiplier,
                smallMotionSensitivity = smallMotionSensitivity,
                contactClearanceMultiplier = contactClearanceMultiplier
            };
        }

        private static float GetControllerDetectionScore(int samples, float motion)
        {
            return samples * 2.0f - motion * 10.0f;
        }

        private static float GetStabilityScore(int samples, float motion)
        {
            return samples <= 0 ? 0.0f : 1.0f / (1.0f + motion / samples);
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
            if (puckContactsEnabled && !isAutoCalibrating)
            {
                ApplyPuckContacts(contactStartPosition, smoothedPosition, smoothedRotation);
            }
            else if (bladeRenderer != null)
            {
                bladeRenderer.material.color = idleColor;
            }

            UpdateDebugBladeMarkers();
            UpdateTrackedShaftVisual();
            UpdateStickModelVisual();
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
            if (maxAllowedFrameToFrameStickMovement > 0.0f)
            {
                maxStep = Mathf.Min(maxStep, maxAllowedFrameToFrameStickMovement);
            }

            var positionDelta = trackedPosition - smoothedPosition;
            if (positionDelta.y < 0.0f)
            {
                var maxDownwardStep = maxDownwardSpeed * deltaTime;
                if (maxAllowedFrameToFrameStickMovement > 0.0f)
                {
                    maxDownwardStep = Mathf.Min(maxDownwardStep, maxAllowedFrameToFrameStickMovement);
                }

                var clampedY = Mathf.Max(positionDelta.y, -maxDownwardStep);
                var planarDelta = new Vector3(positionDelta.x, 0.0f, positionDelta.z);
                if (planarDelta.magnitude > maxStep)
                {
                    planarDelta = planarDelta.normalized * maxStep;
                }

                trackedPosition = smoothedPosition + new Vector3(planarDelta.x, clampedY, planarDelta.z);
                if (positionDelta.y < -maxDownwardStep || new Vector2(positionDelta.x, positionDelta.z).magnitude > maxStep)
                {
                    limitedPoseFrames += 1;
                }

                positionDelta = trackedPosition - smoothedPosition;
            }

            if (positionDelta.y >= 0.0f && positionDelta.magnitude > maxStep)
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

        private void ClampUnsafeStickHeight(ref Vector3 trackedPosition)
        {
            if (targetPuck == null || maxStickHeightAbovePuck <= 0.0f)
            {
                return;
            }

            var maxY = GetPuckContactTopY(targetPuck.transform.position) + maxStickHeightAbovePuck;
            if (trackedPosition.y > maxY)
            {
                trackedPosition.y = maxY;
                limitedPoseFrames += 1;
            }
        }

        private void ClampMountedBladeToIce(ref Vector3 bladePosition)
        {
            if (!clampMountedBladeToIce || trackingMode != StickTrackingMode.MountedControllerShaft)
            {
                return;
            }

            var minBladeY = floorHeight + bladeGroundOffset;
            if (bladePosition.y >= minBladeY)
            {
                return;
            }

            bladePosition.y = minBladeY;
            limitedPoseFrames += 1;
        }

        private void KeepMountedBladeInFront(ref Vector3 bladePosition)
        {
            if (trackingMode != StickTrackingMode.MountedControllerShaft ||
                playAreaRoot == null ||
                mountedBladeMinimumForward <= 0.0f)
            {
                return;
            }

            var localPosition = playAreaRoot.InverseTransformPoint(bladePosition);
            if (localPosition.z >= mountedBladeMinimumForward)
            {
                return;
            }

            localPosition.z = mountedBladeMinimumForward;
            bladePosition = playAreaRoot.TransformPoint(localPosition);
            limitedPoseFrames += 1;
        }

        private void SampleMountedRestPose(Vector3 controllerPosition, Quaternion controllerRotation)
        {
            if (!IsValidPose(controllerPosition, controllerRotation))
            {
                return;
            }

            var now = Time.time;
            if (calibrationSampleCount > 0)
            {
                var deltaTime = Mathf.Max(0.0001f, now - lastCalibrationSampleTime);
                var movement = Vector3.Distance(controllerPosition, lastCalibrationSamplePosition);
                var angle = Quaternion.Angle(controllerRotation, lastCalibrationSampleRotation);
                var isStable =
                    movement <= maxCalibrationSampleMovement &&
                    angle <= maxCalibrationSampleAngle &&
                    movement / deltaTime <= maxStableCalibrationSpeed &&
                    angle / deltaTime <= maxStableCalibrationAngularSpeed;
                if (!isStable)
                {
                    ResetMountedCalibrationSamples(controllerPosition, controllerRotation, now);
                    calibrationEndsAt = Mathf.Max(calibrationEndsAt, now + requiredStableCalibrationSeconds);
                    controllerTrackingStatus = "hold still";
                    return;
                }
            }
            else
            {
                calibrationStableStartedAt = now;
            }

            calibrationPositionSum += controllerPosition;
            calibrationRotationAverage = calibrationSampleCount == 0
                ? controllerRotation
                : Quaternion.Slerp(calibrationRotationAverage, controllerRotation, 1.0f / (calibrationSampleCount + 1));
            calibrationSampleCount += 1;
            lastCalibrationSamplePosition = controllerPosition;
            lastCalibrationSampleRotation = controllerRotation;
            lastCalibrationSampleTime = now;
        }

        private void ResetMountedCalibrationSamples(Vector3 controllerPosition, Quaternion controllerRotation, float sampleTime)
        {
            calibrationSampleCount = 0;
            calibrationPositionSum = Vector3.zero;
            calibrationRotationAverage = Quaternion.identity;
            calibrationStableStartedAt = sampleTime;
            lastCalibrationSamplePosition = controllerPosition;
            lastCalibrationSampleRotation = controllerRotation;
            lastCalibrationSampleTime = sampleTime;
        }

        private void FinishMountedRestCalibration()
        {
            var averagedControllerPosition = calibrationPositionSum / Mathf.Max(1, calibrationSampleCount);
            var averagedControllerRotation = calibrationRotationAverage;
            mountedBladeDirectionSign = ChooseMountedBladeDirectionSign(averagedControllerPosition, averagedControllerRotation);
            mountedRestBladePoint = CalculateMountedBladePoint(averagedControllerPosition, averagedControllerRotation);
            mountedRestBladeRotation = GetMountedBladeGameplayRotation(CalculateMountedBladeRotation(averagedControllerRotation));
            ConfigureStickModelVisualRestCorrection(averagedControllerPosition, averagedControllerRotation);
            mountedRestYOffset = floorHeight + bladeGroundOffset - mountedRestBladePoint.y;
            hasMountedRestCalibration = true;
            isAutoCalibrating = false;
            hasSmoothedPose = false;
            hasTargetPose = false;
            previousPosition = mountedRestBladePoint + Vector3.up * mountedRestYOffset;
        }

        private float ChooseMountedBladeDirectionSign(Vector3 controllerPosition, Quaternion controllerRotation)
        {
            if (!autoFlipBladeDirectionTowardPlayArea || playAreaRoot == null)
            {
                // For the mounted-controller stick, the calibrated controller-to-blade sign is the
                // source of truth. Auto-flipping can place the blade at the top/controller end.
                return 1.0f;
            }

            var positivePoint = CalculateMountedBladePoint(controllerPosition, controllerRotation, 1.0f);
            var negativePoint = CalculateMountedBladePoint(controllerPosition, controllerRotation, -1.0f);
            var positiveZ = playAreaRoot.InverseTransformPoint(positivePoint).z;
            var negativeZ = playAreaRoot.InverseTransformPoint(negativePoint).z;
            return negativeZ > positiveZ ? -1.0f : 1.0f;
        }

        private Vector3 CalculateMountedBladePoint(Vector3 controllerWorldPosition, Quaternion controllerWorldRotation)
        {
            return CalculateMountedBladePoint(controllerWorldPosition, controllerWorldRotation, mountedBladeDirectionSign);
        }

        private Vector3 CalculateMountedBladePoint(
            Vector3 controllerWorldPosition,
            Quaternion controllerWorldRotation,
            float bladeDirectionSign)
        {
            var stickRotation = GetMappedStickRotation(controllerWorldRotation);
            var shaftDirection = stickRotation * SafeAxis(shaftForwardAxis, Vector3.forward);
            var localOffset =
                Vector3.right * controllerToBladeLateralOffset +
                Vector3.forward * controllerToBladeForwardOffset +
                Vector3.up * controllerToBladeVerticalOffset;

            return controllerWorldPosition +
                   shaftDirection.normalized * controllerToBladeDistance * bladeDirectionSign * controllerToBladeDirectionSign +
                   stickRotation * localOffset;
        }

        private Quaternion CalculateMountedBladeRotation(Quaternion controllerWorldRotation)
        {
            var stickRotation = GetMappedStickRotation(controllerWorldRotation);
            var bladeForward = stickRotation * SafeAxis(bladeForwardAxis, Vector3.forward);
            var bladeUp = stickRotation * SafeAxis(bladeUpAxis, Vector3.up);
            if (Vector3.Cross(bladeForward, bladeUp).sqrMagnitude < 0.0001f)
            {
                bladeUp = Vector3.up;
            }

            return Quaternion.LookRotation(bladeForward.normalized, bladeUp.normalized) *
                   Quaternion.Euler(bladeRotationOffset);
        }

        private Quaternion GetMountedBladeGameplayRotation(Quaternion rawBladeRotation)
        {
            if (!keepMountedBladeFlatOnIce)
            {
                return rawBladeRotation;
            }

            var planarForward = Vector3.ProjectOnPlane(rawBladeRotation * Vector3.forward, Vector3.up);
            if (useBladeFaceRollForFlatRotation)
            {
                var planarRight = Vector3.ProjectOnPlane(rawBladeRotation * Vector3.right, Vector3.up);
                if (planarRight.sqrMagnitude > 0.0001f)
                {
                    planarForward = Vector3.Cross(planarRight.normalized, Vector3.up);
                }
            }

            if (planarForward.sqrMagnitude < 0.0001f)
            {
                planarForward = playAreaRoot != null ? playAreaRoot.forward : Vector3.forward;
            }

            return Quaternion.LookRotation(planarForward.normalized, Vector3.up);
        }

        private Quaternion GetMappedStickRotation(Quaternion controllerWorldRotation)
        {
            var mappedForward = controllerWorldRotation * GetMappedControllerAxis(shaftForwardAxisSource, controllerForwardAxis, Vector3.forward, invertForwardAxis, true);
            var mappedUp = controllerWorldRotation * GetMappedControllerAxis(shaftUpAxisSource, controllerUpAxis, Vector3.up, invertUpAxis, true);
            var mappedRight = controllerWorldRotation * GetMappedControllerAxis(controllerRightAxis, Vector3.right, invertRightAxis, false);
            if (Vector3.Cross(mappedForward, mappedUp).sqrMagnitude < 0.0001f)
            {
                mappedUp = Vector3.Cross(mappedRight, mappedForward);
            }

            if (mappedUp.sqrMagnitude < 0.0001f)
            {
                mappedUp = Vector3.up;
            }

            return Quaternion.LookRotation(mappedForward.normalized, mappedUp.normalized) *
                   Quaternion.Euler(controllerToStickRotationOffset);
        }

        private Vector3 GetMappedControllerAxis(
            ControllerAxisSource source,
            Vector3 fallbackAxis,
            Vector3 fallback,
            bool explicitInvert,
            bool invertWhenPointingUp)
        {
            var mappedAxis = GetControllerAxisSourceVector(source);
            if (mappedAxis.sqrMagnitude < 0.0001f)
            {
                mappedAxis = SafeAxis(fallbackAxis, fallback);
            }

            return IsAxisInverted(explicitInvert, invertWhenPointingUp) ? -mappedAxis : mappedAxis;
        }

        private Vector3 GetMappedControllerAxis(
            Vector3 axis,
            Vector3 fallback,
            bool explicitInvert,
            bool invertWhenPointingUp)
        {
            var mappedAxis = SafeAxis(axis, fallback);
            return IsAxisInverted(explicitInvert, invertWhenPointingUp) ? -mappedAxis : mappedAxis;
        }

        private static Vector3 GetControllerAxisSourceVector(ControllerAxisSource source)
        {
            switch (source)
            {
                case ControllerAxisSource.Back:
                    return Vector3.back;
                case ControllerAxisSource.Up:
                    return Vector3.up;
                case ControllerAxisSource.Down:
                    return Vector3.down;
                case ControllerAxisSource.Right:
                    return Vector3.right;
                case ControllerAxisSource.Left:
                    return Vector3.left;
                case ControllerAxisSource.Forward:
                default:
                    return Vector3.forward;
            }
        }

        private static Vector3 SafeAxis(Vector3 axis, Vector3 fallback)
        {
            return axis.sqrMagnitude > 0.0001f ? axis.normalized : fallback;
        }

        private Vector3 CalculateDebugVirtualBladePoint(
            Vector3 controllerWorldPosition,
            Quaternion controllerWorldRotation,
            Vector3 shaftWorldDirection)
        {
            var shaftDirection = shaftWorldDirection.sqrMagnitude > 0.0001f
                ? shaftWorldDirection.normalized
                : controllerWorldRotation * Vector3.forward;
            var yawRotation = Quaternion.Euler(0.0f, controllerWorldRotation.eulerAngles.y, 0.0f);
            var sideDirection = yawRotation * Vector3.right;
            var forwardDirection = yawRotation * Vector3.forward;
            if (invertShaftDirection)
            {
                forwardDirection = -forwardDirection;
            }

            return controllerWorldPosition +
                   shaftDirection * debugControllerToBladeDistance +
                   sideDirection * bladeLeftRightOffset +
                   forwardDirection * debugControllerToBladeForwardOffset +
                   Vector3.up * debugControllerToBladeVerticalOffset;
        }

        private static bool IsValidPose(Vector3 position, Quaternion rotation)
        {
            return IsFinite(position.x) &&
                   IsFinite(position.y) &&
                   IsFinite(position.z) &&
                   IsFinite(rotation.x) &&
                   IsFinite(rotation.y) &&
                   IsFinite(rotation.z) &&
                   IsFinite(rotation.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void UpdateDebugBladeMarkers()
        {
            if (!showBladeDebugMarkers)
            {
                SetDebugMarkerVisible(calculatedBladeDebugMarker, false);
                SetDebugMarkerVisible(visualBladeDebugMarker, false);
                SetDebugMarkerVisible(controllerMountDebugMarker, false);
                SetDebugMarkerVisible(shaftLineDebugMarker, false);
                SetDebugMarkerVisible(bladeContactZoneDebugMarker, false);
                SetDebugMarkerVisible(controllerForwardDebugArrow, false);
                SetDebugMarkerVisible(controllerUpDebugArrow, false);
                SetDebugMarkerVisible(controllerRightDebugArrow, false);
                SetDebugMarkerVisible(shaftForwardDebugArrow, false);
                SetDebugMarkerVisible(bladeForwardDebugArrow, false);
                return;
            }

            calculatedBladeDebugMarker = EnsureDebugMarker(
                calculatedBladeDebugMarker,
                "Calculated Blade Point",
                new Color(1.0f, 0.2f, 1.0f));
            visualBladeDebugMarker = EnsureDebugMarker(
                visualBladeDebugMarker,
                "Visual Blade Point",
                new Color(0.1f, 1.0f, 0.2f));
            controllerMountDebugMarker = EnsureDebugMarker(
                controllerMountDebugMarker,
                "Controller Mount Point",
                Color.white);
            shaftLineDebugMarker = EnsureDebugBox(
                shaftLineDebugMarker,
                "Calculated Shaft Line",
                new Color(1.0f, 0.85f, 0.05f));
            bladeContactZoneDebugMarker = EnsureDebugBox(
                bladeContactZoneDebugMarker,
                "Blade Contact Zone",
                new Color(0.0f, 0.85f, 1.0f));
            controllerForwardDebugArrow = EnsureDebugBox(
                controllerForwardDebugArrow,
                "Controller Forward Arrow",
                new Color(1.0f, 0.1f, 0.1f));
            controllerUpDebugArrow = EnsureDebugBox(
                controllerUpDebugArrow,
                "Controller Up Arrow",
                new Color(0.1f, 1.0f, 0.1f));
            controllerRightDebugArrow = EnsureDebugBox(
                controllerRightDebugArrow,
                "Controller Right Arrow",
                new Color(1.0f, 0.1f, 1.0f));
            shaftForwardDebugArrow = EnsureDebugBox(
                shaftForwardDebugArrow,
                "Calculated Shaft Forward Arrow",
                new Color(1.0f, 0.85f, 0.05f));
            bladeForwardDebugArrow = EnsureDebugBox(
                bladeForwardDebugArrow,
                "Blade Forward Arrow",
                new Color(0.1f, 0.3f, 1.0f));

            calculatedBladeDebugMarker.position = lastCalculatedBladePoint;
            visualBladeDebugMarker.position = smoothedPosition;
            controllerMountDebugMarker.position = lastControllerPosition;
            PositionDebugLine(shaftLineDebugMarker, lastControllerPosition, lastCalculatedBladePoint, 0.025f);
            PositionDebugLine(
                controllerForwardDebugArrow,
                lastControllerPosition,
                lastControllerPosition + lastControllerForwardDirection * debugArrowLength,
                0.035f);
            PositionDebugLine(
                controllerUpDebugArrow,
                lastControllerPosition,
                lastControllerPosition + lastControllerUpDirection * debugArrowLength,
                0.03f);
            PositionDebugLine(
                controllerRightDebugArrow,
                lastControllerPosition,
                lastControllerPosition + lastControllerRightDirection * debugArrowLength,
                0.03f);
            PositionDebugLine(
                shaftForwardDebugArrow,
                lastControllerPosition,
                lastControllerPosition + lastRawShaftDirection * debugArrowLength,
                0.035f);
            PositionDebugLine(
                bladeForwardDebugArrow,
                smoothedPosition,
                smoothedPosition + lastBladeForwardDirection * debugArrowLength,
                0.035f);
            bladeContactZoneDebugMarker.SetPositionAndRotation(smoothedPosition, smoothedRotation);
            bladeContactZoneDebugMarker.localScale = new Vector3(
                bladeContactHalfWidth * 2.0f,
                0.01f,
                bladeContactHalfLength * 2.0f);
            SetDebugMarkerVisible(calculatedBladeDebugMarker, true);
            SetDebugMarkerVisible(visualBladeDebugMarker, true);
            SetDebugMarkerVisible(controllerMountDebugMarker, true);
            SetDebugMarkerVisible(shaftLineDebugMarker, true);
            SetDebugMarkerVisible(bladeContactZoneDebugMarker, true);
            SetDebugMarkerVisible(controllerForwardDebugArrow, true);
            SetDebugMarkerVisible(controllerUpDebugArrow, true);
            SetDebugMarkerVisible(controllerRightDebugArrow, true);
            SetDebugMarkerVisible(shaftForwardDebugArrow, true);
            SetDebugMarkerVisible(bladeForwardDebugArrow, true);
        }

        private Transform EnsureDebugMarker(Transform marker, string markerName, Color markerColor)
        {
            if (marker != null)
            {
                return marker;
            }

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = markerName;
            markerObject.transform.localScale = Vector3.one * 0.055f;
            var markerCollider = markerObject.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            var markerRenderer = markerObject.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.material.color = markerColor;
            }

            return markerObject.transform;
        }

        private Transform EnsureDebugBox(Transform marker, string markerName, Color markerColor)
        {
            if (marker != null)
            {
                return marker;
            }

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markerObject.name = markerName;
            var markerCollider = markerObject.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            var markerRenderer = markerObject.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.material.color = markerColor;
            }

            return markerObject.transform;
        }

        private static void PositionDebugLine(Transform line, Vector3 start, Vector3 end, float thickness)
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length < 0.0001f)
            {
                line.localScale = Vector3.zero;
                return;
            }

            line.position = Vector3.Lerp(start, end, 0.5f);
            line.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            line.localScale = new Vector3(thickness, thickness, length);
        }

        private static void SetDebugMarkerVisible(Transform marker, bool isVisible)
        {
            if (marker != null && marker.gameObject.activeSelf != isVisible)
            {
                marker.gameObject.SetActive(isVisible);
            }
        }

        private void UpdateTrackedShaftVisual()
        {
            if (!showTrackedShaftVisual || trackingMode != StickTrackingMode.MountedControllerShaft || !hasTargetPose)
            {
                SetDebugMarkerVisible(trackedShaftVisual, false);
                return;
            }

            trackedShaftVisual = EnsureDebugBox(
                trackedShaftVisual,
                "Tracked Stick Shaft Visual",
                new Color(0.025f, 0.025f, 0.03f));
            PositionDebugLine(
                trackedShaftVisual,
                smoothedPosition,
                lastControllerPosition,
                trackedShaftVisualThickness);
            SetDebugMarkerVisible(trackedShaftVisual, true);
        }

        private void UpdateStickModelVisual()
        {
            if (!alignStickModelVisualToController || trackingMode != StickTrackingMode.MountedControllerShaft || !hasTargetPose)
            {
                return;
            }

            if (stickModelVisual == null)
            {
                var found = transform.Find(stickModelVisualName);
                if (found == null)
                {
                    return;
                }

                stickModelVisual = found;
                stickModelVisual.SetParent(null, true);
            }

            var shaftDirection = lastControllerPosition - smoothedPosition;
            if (shaftDirection.sqrMagnitude < 0.0001f)
            {
                shaftDirection = transform.forward;
            }

            shaftDirection.Normalize();
            var shaftLength = Mathf.Max(0.1f, stickModelVisualLength);
            var visualCenter = smoothedPosition + shaftDirection * (shaftLength * 0.5f);
            var visualRotation = GetStickModelVisualRotation(shaftDirection) *
                                 Quaternion.Euler(stickModelVisualRotationOffset);
            var offset = transform.right * stickModelVisualPositionOffset.x +
                         transform.up * stickModelVisualPositionOffset.y +
                         transform.forward * stickModelVisualPositionOffset.z;

            stickModelVisual.SetPositionAndRotation(visualCenter + offset, visualRotation);
            stickModelVisual.localScale = Vector3.one * Mathf.Max(0.01f, stickModelVisualScale);
        }

        private void ConfigureStickModelVisualRestCorrection(Vector3 controllerPosition, Quaternion controllerRotation)
        {
            var shaftDirection = controllerPosition - mountedRestBladePoint;
            if (shaftDirection.sqrMagnitude < 0.0001f)
            {
                stickModelVisualRestCorrection = Quaternion.identity;
                return;
            }

            shaftDirection.Normalize();
            stickModelVisualRestCorrection = Quaternion.identity;
        }

        private Quaternion GetStickModelVisualRotation(Vector3 shaftDirection)
        {
            if (alignStickModelBladeToPhysicsBlade)
            {
                return BuildStickModelVisualRotationFromBladeAxis(shaftDirection, transform.forward);
            }

            return BuildStickModelVisualRotation(shaftDirection, lastControllerUpDirection, lastControllerRightDirection);
        }

        private Quaternion BuildStickModelVisualRotationFromBladeAxis(Vector3 shaftDirection, Vector3 bladeLongDirection)
        {
            var visualForward = Vector3.ProjectOnPlane(bladeLongDirection, shaftDirection);
            if (visualForward.sqrMagnitude < 0.0001f)
            {
                visualForward = Vector3.ProjectOnPlane(transform.forward, shaftDirection);
            }

            if (visualForward.sqrMagnitude < 0.0001f)
            {
                visualForward = Vector3.ProjectOnPlane(lastControllerUpDirection, shaftDirection);
            }

            if (visualForward.sqrMagnitude < 0.0001f)
            {
                visualForward = Vector3.forward;
            }

            visualForward.Normalize();
            var visualUp = Vector3.Cross(visualForward, shaftDirection);
            if (invertStickModelVisualUp)
            {
                visualUp = -visualUp;
            }

            if (visualUp.sqrMagnitude < 0.0001f)
            {
                visualUp = Vector3.up;
            }

            return Quaternion.LookRotation(visualForward, visualUp.normalized);
        }

        private Quaternion BuildStickModelVisualRotation(Vector3 shaftDirection, Vector3 upReference, Vector3 fallbackReference)
        {
            var visualUp = Vector3.ProjectOnPlane(upReference, shaftDirection);
            if (visualUp.sqrMagnitude < 0.0001f)
            {
                visualUp = Vector3.ProjectOnPlane(fallbackReference, shaftDirection);
            }

            if (visualUp.sqrMagnitude < 0.0001f)
            {
                visualUp = Vector3.ProjectOnPlane(transform.up, shaftDirection);
            }

            if (visualUp.sqrMagnitude < 0.0001f)
            {
                visualUp = Vector3.up;
            }

            visualUp.Normalize();
            var visualForward = Vector3.Cross(shaftDirection, visualUp);
            if (visualForward.sqrMagnitude < 0.0001f)
            {
                visualForward = Vector3.Cross(shaftDirection, Vector3.up);
            }

            return Quaternion.LookRotation(visualForward.normalized, visualUp);
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
            var bladeBottomY = GetBladeBottomY(smoothedPosition);
            var puckTopDelta = targetPuck != null ? bladeBottomY - GetPuckContactTopY(targetPuck.transform.position) : 0.0f;
            var puckPosition = targetPuck != null ? targetPuck.transform.position : Vector3.zero;
            lastBladeBottomPoint = new Vector3(smoothedPosition.x, bladeBottomY, smoothedPosition.z);
            diagnosticText =
                $"Stick {status} ({connected})\n" +
                $"Pose {trackedPercent}% good  lost {reportLostPoseFrames}  limited {reportLimitedPoseFrames}\n" +
                $"Mode {trackingMode} cal {(isAutoCalibrating ? "CAL" : hasMountedRestCalibration ? "READY" : "NONE")} samples {calibrationSampleCount}\n" +
                $"Controller {FormatVector(lastControllerPosition)} rawY {lastControllerPosition.y:0.000} baseY {controllerBaselineY:0.000}\n" +
                $"Blade {FormatVector(smoothedPosition)} calc {FormatVector(lastCalculatedBladePoint)}  Puck {FormatVector(puckPosition)}\n" +
                $"Mount {controllerMountOrientation} offsetSign {controllerToBladeDirectionSign:0} autoSign {mountedBladeDirectionSign:0} inv F{IsAxisInverted(invertForwardAxis, true)} U{IsAxisInverted(invertUpAxis, true)} R{IsAxisInverted(invertRightAxis, false)}\n" +
                $"Axis source shaftF {shaftForwardAxisSource} shaftU {shaftUpAxisSource}\n" +
                $"Ctrl axes F{FormatVector(lastControllerForwardDirection)} U{FormatVector(lastControllerUpDirection)} R{FormatVector(lastControllerRightDirection)}\n" +
                $"Shaft {FormatVector(lastRawShaftDirection)} bladeF {FormatVector(lastBladeForwardDirection)} bladeY-ice {(smoothedPosition.y - (floorHeight + bladeGroundOffset)):0.000}\n" +
                $"Lift {liftAmount:0.000}m visual {visualLiftAmount:0.000} ctrl {lastControllerLift:0.000} shaft {lastShaftLift:0.000} upX {liftUpMultiplier:0.0} shaftX {shaftPitchLiftUpMultiplier:0.0} shaftY {lastRawShaftDirection.y:0.00}\n" +
                $"Blade tipY {lastBladeTipY:0.000} bottomY {lastBladeBottomY:0.000} puckY {lastPuckCenterY:0.000} topY {lastPuckTopY:0.000}\n" +
                $"Height above puck {lastBladeHeightAbovePuck:0.000}m cutoff {contactIgnoreHeight:0.000} contact {(lastContactAllowed ? "YES" : "NO")} {lastContactReason} lock {Mathf.Max(0.0f, contactIgnoredUntil - Time.time):0.00}s\n" +
                $"Plane {(useVirtualIcePlaneOffset ? "ON" : "OFF")} ice {virtualIcePlaneYOffset:0.000} blade {bladeContactPlaneYOffset:0.000} puck {puckContactPlaneYOffset:0.000} contact {contactOnlyYOffset:0.000}\n" +
                $"Mount LR {bladeLeftRightOffset:0.00} FB {bladeForwardBackOffset:0.00} H {bladeHeightOffset:0.00} debugDist {debugControllerToBladeDistance:0.00}\n" +
                "Grip + A: recalibrate";
        }

        private bool IsAxisInverted(bool explicitInvert, bool invertWhenPointingUp)
        {
            return explicitInvert ||
                   (controllerMountOrientation == ControllerMountOrientation.PointingUp && invertWhenPointingUp);
        }

        private Vector3 ApplyBladeMountOffset(Vector3 worldPosition)
        {
            var right = playAreaRoot != null ? playAreaRoot.right : Vector3.right;
            var forward = playAreaRoot != null ? playAreaRoot.forward : Vector3.forward;
            return worldPosition +
                   right * bladeLeftRightOffset +
                   forward * bladeForwardBackOffset +
                   Vector3.up * bladeHeightOffset;
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
                targetPuck.ResolveBladeOverlap(contactPoint, puckContactRadius, maxPuckDepenetrationPerContact);
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
            lastContactAllowed = false;
            lastContactReason = "checking";
            var bladeContactYOffset = GetBladeContactPlaneYOffset();
            var puckContactYOffset = GetPuckContactPlaneYOffset();
            var bladeBottomY = GetBladeBottomY(bladePosition) + bladeContactYOffset;
            var puckBottomY = puckPosition.y + puckContactYOffset - puckContactHeight * 0.5f;
            var puckTopY = GetPuckContactTopY(puckPosition) + puckContactYOffset;
            var heightAbovePuckTop = bladeBottomY - puckTopY;
            lastBladeBottomY = bladeBottomY;
            lastBladeTipY = GetBladeTipY(bladePosition, bladeRotation) + bladeContactYOffset;
            lastPuckCenterY = puckPosition.y + puckContactYOffset;
            lastPuckTopY = puckTopY;
            lastBladeHeightAbovePuck = heightAbovePuckTop;
            if (heightAbovePuckTop > topDragRejectHeight || bladeVelocity.y > liftRisingVelocityThreshold)
            {
                contactIgnoredUntil = Time.time + arcContactLockoutSeconds;
            }

            if (Time.time < contactIgnoredUntil && heightAbovePuckTop > -contactReenableBelowPuckTop)
            {
                lastContactReason = "arc clearing";
                return false;
            }

            if (bladeBottomY < puckBottomY - directPuckVerticalTolerance)
            {
                lastContactReason = "blade below puck zone";
                return false;
            }

            if (heightAbovePuckTop > topDragRejectHeight && bladeVelocity.y >= -0.02f)
            {
                lastContactReason = "over puck";
                return false;
            }

            if (heightAbovePuckTop > contactIgnoreHeight * Mathf.Max(0.01f, contactClearanceMultiplier))
            {
                lastContactReason = "lift ignored";
                return false;
            }

            var contactAdjustedBladePosition = bladePosition + Vector3.up * bladeContactYOffset;
            var contactAdjustedPuckPosition = puckPosition + Vector3.up * puckContactYOffset;
            var localPuck = Quaternion.Inverse(bladeRotation) * (contactAdjustedPuckPosition - contactAdjustedBladePosition);
            var closestLocal = new Vector3(
                Mathf.Clamp(localPuck.x, -bladeContactHalfWidth, bladeContactHalfWidth),
                0.0f,
                Mathf.Clamp(localPuck.z, -bladeContactHalfLength, bladeContactHalfLength));
            var planarDelta = new Vector2(localPuck.x - closestLocal.x, localPuck.z - closestLocal.z);
            var planarDistance = planarDelta.magnitude;
            if (planarDistance > puckContactRadius)
            {
                lastContactReason = "not touching side";
                return false;
            }

            contactPoint = bladePosition + bladeRotation * closestLocal;
            lastContactAllowed = true;
            lastContactReason = "valid";
            return true;
        }

        private float GetBladeContactPlaneYOffset()
        {
            if (!useVirtualIcePlaneOffset)
            {
                return 0.0f;
            }

            return virtualIcePlaneYOffset + bladeContactPlaneYOffset + contactOnlyYOffset;
        }

        private float GetPuckContactPlaneYOffset()
        {
            if (!useVirtualIcePlaneOffset)
            {
                return 0.0f;
            }

            return virtualIcePlaneYOffset + puckContactPlaneYOffset;
        }

        private float GetBladeBottomY(Vector3 bladePosition)
        {
            var bladeCollider = GetComponent<Collider>();
            if (bladeCollider != null)
            {
                var centerOffset = bladePosition.y - transform.position.y;
                return bladeCollider.bounds.min.y + centerOffset;
            }

            return bladePosition.y - transform.lossyScale.y * 0.5f;
        }

        private float GetPuckContactTopY(Vector3 puckPosition)
        {
            var puckCollider = targetPuck != null ? targetPuck.GetComponent<Collider>() : null;
            if (puckCollider != null)
            {
                return puckCollider.bounds.center.y + puckContactHeight * 0.5f + bladeContactClearance;
            }

            return puckPosition.y + puckContactHeight * 0.5f + bladeContactClearance;
        }

        private float GetBladeTipY(Vector3 bladePosition, Quaternion bladeRotation)
        {
            var frontTip = bladePosition + bladeRotation * Vector3.forward * bladeContactHalfLength;
            var backTip = bladePosition - bladeRotation * Vector3.forward * bladeContactHalfLength;
            return Mathf.Max(frontTip.y, backTip.y);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.00},{value.y:0.00},{value.z:0.00}";
        }
    }
}
