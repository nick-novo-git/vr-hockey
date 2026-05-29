using UnityEngine;

namespace HockeyStickhandling
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private float spawnInterval = 1.6f;
        [SerializeField] private float minSpawnInterval = 0.55f;
        [SerializeField] private float spawnZ = 25.0f;
        [SerializeField] private float xRange = 2.05f;
        [SerializeField] private float obstacleSpeed = 2.8f;
        [SerializeField] private float maxObstacleSpeed = 6.25f;
        [SerializeField] private Vector3 obstacleScale = new Vector3(0.42f, 0.16f, 0.45f);

        private Material obstacleMaterial;
        private PuckController puck;
        private GameManager gameManager;
        private float nextSpawnTime;

        public void Initialize(GameObject prefab, Material material, PuckController targetPuck, GameManager manager)
        {
            obstaclePrefab = prefab;
            obstacleMaterial = material;
            puck = targetPuck;
            gameManager = manager;
        }

        public void SetLaneWidth(float halfWidth)
        {
            xRange = Mathf.Max(0.4f, halfWidth);
        }

        private void OnEnable()
        {
            nextSpawnTime = Time.time + 0.75f;
        }

        private void Update()
        {
            if (Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnWave();
            nextSpawnTime = Time.time + GetCurrentSpawnInterval();
        }

        private void SpawnWave()
        {
            var difficulty = gameManager != null ? gameManager.Difficulty : 0.0f;
            var level = gameManager != null ? gameManager.Level : 1;
            var obstacleCount = level >= 7 ? 3 : level >= 3 ? 2 : 1;
            for (var i = 0; i < obstacleCount; i += 1)
            {
                SpawnObstacle(i, obstacleCount, difficulty);
            }
        }

        private float GetCurrentSpawnInterval()
        {
            var difficulty = gameManager != null ? gameManager.Difficulty : 0.0f;
            return Mathf.Lerp(spawnInterval, minSpawnInterval, difficulty);
        }

        private float GetCurrentObstacleSpeed()
        {
            var difficulty = gameManager != null ? gameManager.Difficulty : 0.0f;
            return Mathf.Lerp(obstacleSpeed, maxObstacleSpeed, difficulty);
        }

        private void SpawnObstacle(int index, int count, float difficulty)
        {
            var obstacle = new GameObject("Obstacle");
            obstacle.transform.SetParent(transform, false);
            obstacle.transform.localPosition = new Vector3(GetSpawnX(index, count), 0.18f, spawnZ + index * 1.15f);
            obstacle.transform.localRotation = Quaternion.identity;

            var type = (ObstacleType)Random.Range(0, 4);
            var hitRadius = CreateObstacleVisual(obstacle.transform, type, difficulty);

            var body = obstacle.GetComponent<Rigidbody>() ?? obstacle.AddComponent<Rigidbody>();
            body.isKinematic = true;

            var collider = obstacle.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = GetColliderSize(type);
            collider.center = GetColliderCenter(type);

            var marker = obstacle.GetComponent<Obstacle>() ?? obstacle.AddComponent<Obstacle>();
            marker.Configure(GetCurrentObstacleSpeed() + Random.Range(-0.25f, 0.35f), puck, gameManager, hitRadius);
        }

        private float GetSpawnX(int index, int count)
        {
            if (puck != null && Random.value < 0.45f)
            {
                var localPuckPosition = transform.InverseTransformPoint(puck.transform.position);
                return Mathf.Clamp(localPuckPosition.x + Random.Range(-0.45f, 0.45f), -xRange, xRange);
            }

            if (count <= 1)
            {
                return Random.Range(-xRange, xRange);
            }

            var spacing = xRange * 2.0f / (count + 1);
            return -xRange + spacing * (index + 1) + Random.Range(-0.25f, 0.25f);
        }

        private float CreateObstacleVisual(Transform root, ObstacleType type, float difficulty)
        {
            switch (type)
            {
                case ObstacleType.Cone:
                    CreateCone(root);
                    return 0.28f;
                case ObstacleType.Chair:
                    CreateChair(root);
                    return 0.48f;
                case ObstacleType.Crate:
                    CreateCrate(root);
                    return 0.42f;
                case ObstacleType.Block:
                default:
                    CreateBlock(root, difficulty);
                    return 0.38f;
            }
        }

        private void CreateBlock(Transform root, float difficulty)
        {
            var block = CreatePrimitiveChild(root, PrimitiveType.Cube, "Foam Block", new Color(0.95f, 0.05f, 0.03f));
            block.localPosition = Vector3.zero;
            block.localScale = Vector3.Lerp(obstacleScale, obstacleScale * 1.25f, difficulty);
        }

        private void CreateCone(Transform root)
        {
            var basePart = CreatePrimitiveChild(root, PrimitiveType.Cylinder, "Traffic Cone", new Color(1.0f, 0.42f, 0.0f));
            basePart.localPosition = new Vector3(0.0f, -0.03f, 0.0f);
            basePart.localScale = new Vector3(0.26f, 0.34f, 0.26f);

            var stripe = CreatePrimitiveChild(root, PrimitiveType.Cylinder, "Cone Stripe", Color.white);
            stripe.localPosition = new Vector3(0.0f, 0.05f, 0.0f);
            stripe.localScale = new Vector3(0.275f, 0.035f, 0.275f);
        }

        private void CreateChair(Transform root)
        {
            var seat = CreatePrimitiveChild(root, PrimitiveType.Cube, "Chair Seat", new Color(0.18f, 0.18f, 0.2f));
            seat.localPosition = new Vector3(0.0f, 0.02f, 0.0f);
            seat.localScale = new Vector3(0.55f, 0.08f, 0.45f);

            var back = CreatePrimitiveChild(root, PrimitiveType.Cube, "Chair Back", new Color(0.14f, 0.14f, 0.16f));
            back.localPosition = new Vector3(0.0f, 0.34f, 0.18f);
            back.localScale = new Vector3(0.55f, 0.55f, 0.08f);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    var leg = CreatePrimitiveChild(root, PrimitiveType.Cube, "Chair Leg", new Color(0.08f, 0.08f, 0.09f));
                    leg.localPosition = new Vector3(x * 0.22f, -0.18f, z * 0.16f);
                    leg.localScale = new Vector3(0.045f, 0.34f, 0.045f);
                }
            }
        }

        private void CreateCrate(Transform root)
        {
            var crate = CreatePrimitiveChild(root, PrimitiveType.Cube, "Equipment Crate", new Color(0.16f, 0.48f, 0.82f));
            crate.localPosition = Vector3.zero;
            crate.localScale = new Vector3(0.48f, 0.28f, 0.5f);
        }

        private Transform CreatePrimitiveChild(Transform parent, PrimitiveType primitiveType, string childName, Color color)
        {
            var child = GameObject.CreatePrimitive(primitiveType);
            child.name = childName;
            child.transform.SetParent(parent, false);
            var childCollider = child.GetComponent<Collider>();
            if (childCollider != null)
            {
                Destroy(childCollider);
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = NewObstacleMaterial(childName, color);
            }

            return child.transform;
        }

        private Material NewObstacleMaterial(string materialName, Color color)
        {
            if (obstacleMaterial != null && materialName == "Foam Block")
            {
                return obstacleMaterial;
            }

            var shader = Shader.Find("Standard");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.name = $"{materialName} Material";
            material.color = color;
            return material;
        }

        private static Vector3 GetColliderSize(ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.Chair:
                    return new Vector3(0.62f, 0.72f, 0.55f);
                case ObstacleType.Cone:
                    return new Vector3(0.38f, 0.68f, 0.38f);
                case ObstacleType.Crate:
                    return new Vector3(0.52f, 0.32f, 0.54f);
                case ObstacleType.Block:
                default:
                    return new Vector3(0.48f, 0.2f, 0.5f);
            }
        }

        private static Vector3 GetColliderCenter(ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.Chair:
                    return new Vector3(0.0f, 0.12f, 0.04f);
                case ObstacleType.Cone:
                    return new Vector3(0.0f, 0.0f, 0.0f);
                case ObstacleType.Crate:
                    return Vector3.zero;
                case ObstacleType.Block:
                default:
                    return Vector3.zero;
            }
        }

        private enum ObstacleType
        {
            Block,
            Cone,
            Chair,
            Crate
        }
    }
}
