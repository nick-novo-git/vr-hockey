using UnityEngine;

namespace HockeyStickhandling
{
    public sealed class Obstacle : MonoBehaviour
    {
        [SerializeField] private float speed = 1.4f;
        [SerializeField] private float despawnZ = -2.5f;
        [SerializeField] private float puckHitRadius = 0.42f;

        private PuckController targetPuck;
        private GameManager gameManager;
        private bool registeredDodge;

        public void Configure(float moveSpeed, PuckController puck, GameManager manager, float hitRadius)
        {
            speed = moveSpeed;
            targetPuck = puck;
            gameManager = manager;
            puckHitRadius = hitRadius;
            registeredDodge = false;
        }

        private void Update()
        {
            transform.localPosition += Vector3.back * (speed * Time.deltaTime);
            if (IsTouchingPuck())
            {
                gameManager?.RegisterHit();
                targetPuck.ResetPuck();
                Destroy(gameObject);
                return;
            }

            if (transform.localPosition.z < despawnZ)
            {
                if (!registeredDodge)
                {
                    gameManager?.RegisterDodge();
                    registeredDodge = true;
                }

                Destroy(gameObject);
            }
        }

        private bool IsTouchingPuck()
        {
            if (targetPuck == null)
            {
                return false;
            }

            var delta = targetPuck.transform.position - transform.position;
            var planarDistance = new Vector2(delta.x, delta.z).magnitude;
            return planarDistance <= puckHitRadius && Mathf.Abs(delta.y) < 0.35f;
        }
    }
}
