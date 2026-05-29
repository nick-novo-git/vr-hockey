using UnityEngine;

namespace HockeyStickhandling
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PuckController : MonoBehaviour
    {
        [SerializeField] private float maxSpeed = 2.35f;
        [SerializeField] private float resetBelowY = -0.5f;
        [SerializeField] private float stickVelocityMultiplier = 0.72f;
        [SerializeField] private float pushAwaySpeed = 0.62f;
        [SerializeField] private float minimumStickSpeed = 0.05f;
        [SerializeField] private float contactCooldown = 0.08f;
        [SerializeField] private float slideDeceleration = 2.75f;
        [SerializeField] private float rinkEdgePadding = 0.2f;

        private Rigidbody body;
        private GameManager gameManager;
        private Transform playAreaRoot;
        private Vector2 playAreaSize = new Vector2(4.0f, 5.0f);
        private float playAreaCenterZ = 1.6f;
        private float puckHeight = 0.04f;
        private Vector3 startPosition;
        private Vector3 startLocalPosition;
        private bool hasLocalStartPosition;
        private Vector3 slideVelocity;
        private float nextStickContactTime;

        public void Initialize(GameManager manager)
        {
            gameManager = manager;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            startPosition = transform.position;
        }

        private void FixedUpdate()
        {
            var planarVelocity = new Vector3(slideVelocity.x, 0.0f, slideVelocity.z);
            if (planarVelocity.magnitude > maxSpeed)
            {
                planarVelocity = planarVelocity.normalized * maxSpeed;
                slideVelocity = planarVelocity;
            }

            if (planarVelocity.sqrMagnitude > 0.0001f)
            {
                var nextPosition = transform.position + planarVelocity * Time.fixedDeltaTime;
                if (IsOutsidePlayArea(nextPosition))
                {
                    ResetPuck();
                    return;
                }

                transform.position = ClampToPlayArea(nextPosition);

                var nextSpeed = Mathf.Max(0.0f, planarVelocity.magnitude - slideDeceleration * Time.fixedDeltaTime);
                slideVelocity = planarVelocity.normalized * nextSpeed;
            }
            else
            {
                slideVelocity = Vector3.zero;
            }

            if (transform.position.y < startPosition.y + resetBelowY)
            {
                ResetPuck();
            }
        }

        public void SetHomePosition(Vector3 position)
        {
            startPosition = position;
            if (playAreaRoot != null)
            {
                startLocalPosition = playAreaRoot.InverseTransformPoint(position);
                hasLocalStartPosition = true;
            }

            transform.position = position;
        }

        public void SetPlayArea(Transform root, Vector2 size, float centerZ, float height)
        {
            playAreaRoot = root;
            playAreaSize = size;
            playAreaCenterZ = centerZ;
            puckHeight = height;
        }

        public void ApplyStickContact(Vector3 bladeVelocity, Vector3 bladePosition)
        {
            if (Time.time < nextStickContactTime)
            {
                return;
            }

            var awayFromBlade = new Vector3(transform.position.x - bladePosition.x, 0.0f, transform.position.z - bladePosition.z);
            if (awayFromBlade.sqrMagnitude < 0.0001f)
            {
                awayFromBlade = Vector3.forward;
            }

            awayFromBlade.Normalize();

            var planarBladeVelocity = new Vector3(bladeVelocity.x, 0.0f, bladeVelocity.z);
            var desiredVelocity = awayFromBlade * pushAwaySpeed;
            if (planarBladeVelocity.magnitude >= minimumStickSpeed)
            {
                desiredVelocity += planarBladeVelocity * stickVelocityMultiplier;
            }

            body.WakeUp();
            slideVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
            nextStickContactTime = Time.time + contactCooldown;
        }

        public void MoveLocal(Vector3 localDelta)
        {
            if (playAreaRoot == null)
            {
                transform.position += localDelta;
                return;
            }

            var localPosition = playAreaRoot.InverseTransformPoint(transform.position) + localDelta;
            localPosition.x = Mathf.Clamp(localPosition.x, -playAreaSize.x * 0.5f + 0.2f, playAreaSize.x * 0.5f - 0.2f);
            localPosition.z = Mathf.Clamp(localPosition.z, playAreaCenterZ - playAreaSize.y * 0.5f + 0.2f, playAreaCenterZ + playAreaSize.y * 0.5f - 0.2f);
            localPosition.y = puckHeight;
            transform.position = playAreaRoot.TransformPoint(localPosition);
        }

        private Vector3 ClampToPlayArea(Vector3 worldPosition)
        {
            if (playAreaRoot == null)
            {
                worldPosition.y = startPosition.y;
                return worldPosition;
            }

            var localPosition = playAreaRoot.InverseTransformPoint(worldPosition);
            localPosition.x = Mathf.Clamp(localPosition.x, -playAreaSize.x * 0.5f + 0.2f, playAreaSize.x * 0.5f - 0.2f);
            localPosition.z = Mathf.Clamp(localPosition.z, playAreaCenterZ - playAreaSize.y * 0.5f + 0.2f, playAreaCenterZ + playAreaSize.y * 0.5f - 0.2f);
            localPosition.y = puckHeight;
            return playAreaRoot.TransformPoint(localPosition);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.GetComponent<Obstacle>() != null)
            {
                gameManager?.RegisterHit();
                ResetPuck();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Obstacle>() != null)
            {
                gameManager?.RegisterHit();
                ResetPuck();
            }
        }

        private bool IsOutsidePlayArea(Vector3 worldPosition)
        {
            if (playAreaRoot == null)
            {
                return false;
            }

            var localPosition = playAreaRoot.InverseTransformPoint(worldPosition);
            return localPosition.x < -playAreaSize.x * 0.5f + rinkEdgePadding ||
                   localPosition.x > playAreaSize.x * 0.5f - rinkEdgePadding ||
                   localPosition.z < playAreaCenterZ - playAreaSize.y * 0.5f + rinkEdgePadding ||
                   localPosition.z > playAreaCenterZ + playAreaSize.y * 0.5f - rinkEdgePadding;
        }

        public void ResetPuck()
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            slideVelocity = Vector3.zero;
            transform.position = playAreaRoot != null && hasLocalStartPosition
                ? playAreaRoot.TransformPoint(startLocalPosition)
                : startPosition;
            transform.rotation = Quaternion.identity;
        }
    }
}
