using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace HockeyStickhandling
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [Header("Play Area")]
        [SerializeField] private Vector2 rinkSize = new Vector2(10.0f, 120.0f);
        [SerializeField] private float floorY = 0.0f;
        [SerializeField] private float rinkCenterZ = 58.0f;
        [SerializeField] private bool testLeftHanded = true;
        [SerializeField] private float stickStartSideOffset = 0.35f;

        private Transform gameplayRoot;
        private Transform rinkContentRoot;
        private Camera mainCamera;
        private StickTracker stickTracker;
        private PuckController puckController;
        private ForwardSkateScroller skateScroller;
        private Vector3 puckStartLocalPosition;
        private float startupRecenterUntil;
        private float calibratedFloorY;
        private bool hasCalibratedSceneHeight;
        private float lowestObservedControllerY = float.PositiveInfinity;

        private void Awake()
        {
            EnsureCameraRig();
            mainCamera = Camera.main;

            var materials = CreateMaterials();
            CreateGameplay(materials);
            CreateLight();
        }

        private void Start()
        {
            StartGame();
            CreateMenu();
        }

        private void StartGame()
        {
            gameplayRoot.gameObject.SetActive(true);
            CalibrateAndResetSceneToHeadset();
            startupRecenterUntil = Time.time + 2.0f;
        }

        private void Update()
        {
            UpdateLowestObservedControllerY();
            if (Time.time > startupRecenterUntil)
            {
                return;
            }

            PlaceGameplayInFrontOfHeadset(false);
            RefreshGameplayAnchors(false);
        }

        public void ResetSceneToHeadset()
        {
            PlaceGameplayInFrontOfHeadset(false);
            ClearObstacles();
            RefreshGameplayAnchors(true);
        }

        private void CalibrateAndResetSceneToHeadset()
        {
            PlaceGameplayInFrontOfHeadset(!hasCalibratedSceneHeight);
            hasCalibratedSceneHeight = true;
            ClearObstacles();
            RefreshGameplayAnchors(true);
        }

        private void RefreshGameplayAnchors(bool resetPuck)
        {
            if (puckController != null)
            {
                if (resetPuck && skateScroller != null)
                {
                    skateScroller.ResetScroll();
                }

                if (resetPuck)
                {
                    puckController.transform.localPosition = puckStartLocalPosition;
                }

                puckController.SetPlayArea(gameplayRoot, rinkSize, rinkCenterZ, floorY + 0.04f);
                puckController.SetHomePosition(puckController.transform.position);
                if (resetPuck)
                {
                    puckController.ResetPuck();
                }
            }

            if (stickTracker != null)
            {
                stickTracker.SetFloorHeight(gameplayRoot.position.y + floorY + 0.018f);
                stickTracker.SetTargetPuck(puckController);
                stickTracker.SetPlayAreaRoot(gameplayRoot);
                stickTracker.SetLockBladeToFloor(false);
            }
        }

        private void EnsureCameraRig()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.58f, 0.76f, 0.95f);
                return;
            }

            var rig = new GameObject("XR Camera Rig");
            rig.transform.position = Vector3.zero;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(rig.transform, false);

            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.58f, 0.76f, 0.95f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 250.0f;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            cameraObject.AddComponent<AudioListener>();

            var trackedPoseDriver = cameraObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            trackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.positionInput = new InputActionProperty(CreatePoseAction("HMD Position", "Vector3", "<XRHMD>/centerEyePosition"));
            trackedPoseDriver.rotationInput = new InputActionProperty(CreatePoseAction("HMD Rotation", "Quaternion", "<XRHMD>/centerEyeRotation"));
            trackedPoseDriver.trackingStateInput = new InputActionProperty(CreatePoseAction("HMD Tracking State", "Integer", "<XRHMD>/trackingState"));
        }

        private static InputAction CreatePoseAction(string actionName, string expectedControlType, string binding)
        {
            var action = new InputAction(actionName, expectedControlType: expectedControlType);
            action.AddBinding(binding);
            action.Enable();
            return action;
        }

        private void CreateGameplay(PrototypeMaterials materials)
        {
            gameplayRoot = new GameObject("Gameplay Root").transform;
            gameplayRoot.gameObject.SetActive(false);

            rinkContentRoot = new GameObject("Rink Content").transform;
            rinkContentRoot.SetParent(gameplayRoot, false);

            CreateFloor(materials.floor);
            var gameManager = CreateGameManager();
            CreatePuck(materials.puck, gameManager);
            CreateStickBlade(materials.stick);
            gameManager.AttachStickTracker(stickTracker);
            CreateObstacleSpawner(materials.obstacle, gameManager);
            CreateSkateScroller();
            gameManager.AttachWorldStatus(gameplayRoot);
        }

        private void CreateMenu()
        {
            var menuObject = new GameObject("Prototype Menu");
            var menu = menuObject.AddComponent<PrototypeMenu>();
            menu.Initialize();
        }

        private void PlaceGameplayInFrontOfHeadset(bool useControllerFloor)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null || gameplayRoot == null)
            {
                return;
            }

            if (useControllerFloor)
            {
                calibratedFloorY = float.IsPositiveInfinity(lowestObservedControllerY)
                    ? mainCamera.transform.position.y - 1.65f
                    : lowestObservedControllerY;
                if (TryGetControllerFloorY(out var controllerFloorY) && controllerFloorY < calibratedFloorY)
                {
                    calibratedFloorY = controllerFloorY;
                }
            }

            var forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            var yaw = Quaternion.LookRotation(forward, Vector3.up);
            gameplayRoot.SetPositionAndRotation(
                new Vector3(mainCamera.transform.position.x, calibratedFloorY, mainCamera.transform.position.z),
                yaw);
        }

        private void UpdateLowestObservedControllerY()
        {
            if (TryGetControllerFloorY(out var controllerY))
            {
                lowestObservedControllerY = Mathf.Min(lowestObservedControllerY, controllerY);
            }
        }

        private static bool TryGetControllerFloorY(out float controllerFloorY)
        {
            controllerFloorY = 0.0f;
            var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var rightPosition))
            {
                controllerFloorY = rightPosition.y;
                return true;
            }

            var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var leftPosition))
            {
                controllerFloorY = leftPosition.y;
                return true;
            }

            return false;
        }

        private PrototypeMaterials CreateMaterials()
        {
            return new PrototypeMaterials
            {
                floor = NewMaterial("Prototype Floor", Color.white),
                puck = NewMaterial("Prototype Puck", Color.black),
                obstacle = NewMaterial("Prototype Obstacle", new Color(0.95f, 0.05f, 0.03f)),
                stick = NewMaterial("Prototype Stick Blade", new Color(0.08f, 0.18f, 0.95f))
            };
        }

        private static Material NewMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Standard");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.name = materialName;
            material.color = color;
            return material;
        }

        private void CreateFloor(Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Ice Surface";
            floor.transform.SetParent(rinkContentRoot, false);
            floor.transform.localPosition = new Vector3(0.0f, floorY - 0.025f, rinkCenterZ);
            floor.transform.localScale = new Vector3(rinkSize.x, 0.05f, rinkSize.y);
            floor.GetComponent<Renderer>().sharedMaterial = material;

            CreateWall("Left Wall", new Vector3(-rinkSize.x * 0.5f, 0.2f, rinkCenterZ), new Vector3(0.05f, 0.4f, rinkSize.y));
            CreateWall("Right Wall", new Vector3(rinkSize.x * 0.5f, 0.2f, rinkCenterZ), new Vector3(0.05f, 0.4f, rinkSize.y));
            CreateWall("Back Wall", new Vector3(0.0f, 0.2f, rinkCenterZ - rinkSize.y * 0.5f), new Vector3(rinkSize.x, 0.4f, 0.05f));
        }

        private void CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            var wall = new GameObject(wallName);
            wall.transform.SetParent(rinkContentRoot, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.AddComponent<BoxCollider>();
        }

        private GameManager CreateGameManager()
        {
            var gameManagerObject = new GameObject("Game Manager");
            gameManagerObject.transform.SetParent(gameplayRoot, false);
            return gameManagerObject.AddComponent<GameManager>();
        }

        private GameObject CreatePuck(Material material, GameManager gameManager)
        {
            var puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "Hockey Puck";
            puck.transform.SetParent(gameplayRoot, false);
            puck.transform.localPosition = new Vector3(0.0f, floorY + 0.04f, 0.95f);
            puckStartLocalPosition = puck.transform.localPosition;
            puck.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);
            puck.GetComponent<Renderer>().sharedMaterial = material;

            var body = puck.AddComponent<Rigidbody>();
            body.mass = 0.17f;
            body.drag = 0.28f;
            body.angularDrag = 0.45f;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var puckController = puck.AddComponent<PuckController>();
            puckController.Initialize(gameManager);
            puck.AddComponent<PuckResetInput>().Initialize(puckController, ResetSceneToHeadset);
            this.puckController = puckController;
            return puck;
        }

        private void CreateStickBlade(Material material)
        {
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Tracked Stick Blade";
            blade.transform.SetParent(gameplayRoot, false);
            var stickStartX = testLeftHanded ? -stickStartSideOffset : stickStartSideOffset;
            blade.transform.localPosition = new Vector3(stickStartX, floorY + 0.018f, 0.95f);
            blade.transform.localScale = new Vector3(0.08f, 0.035f, 0.55f);
            blade.GetComponent<Renderer>().sharedMaterial = material;

            stickTracker = blade.AddComponent<StickTracker>();
        }

        private void CreateObstacleSpawner(Material material, GameManager gameManager)
        {
            var spawnerObject = new GameObject("Obstacle Spawner");
            spawnerObject.transform.SetParent(rinkContentRoot, false);
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.localRotation = Quaternion.identity;
            var spawner = spawnerObject.AddComponent<ObstacleSpawner>();
            spawner.Initialize(null, material, puckController, gameManager);
        }

        private void CreateSkateScroller()
        {
            var scrollerObject = new GameObject("Forward Skate Scroller");
            scrollerObject.transform.SetParent(gameplayRoot, false);
            skateScroller = scrollerObject.AddComponent<ForwardSkateScroller>();
            skateScroller.Initialize(rinkContentRoot, puckController, gameplayRoot, stickTracker);
        }

        private void ClearObstacles()
        {
            if (gameplayRoot == null)
            {
                return;
            }

            foreach (var obstacle in gameplayRoot.GetComponentsInChildren<Obstacle>())
            {
                Destroy(obstacle.gameObject);
            }
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
        }

        private sealed class PrototypeMaterials
        {
            public Material floor;
            public Material puck;
            public Material obstacle;
            public Material stick;
        }
    }
}
