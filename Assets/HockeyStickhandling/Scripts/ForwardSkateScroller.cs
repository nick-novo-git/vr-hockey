using UnityEngine;

namespace HockeyStickhandling
{
    public sealed class ForwardSkateScroller : MonoBehaviour
    {
        [SerializeField] private float skateSpeed = 1.0f;
        [SerializeField] private float puckReachAssistSpeed = 0.8f;
        [SerializeField] private float puckReachAssistSmoothTime = 1.25f;
        [SerializeField] private float reachableCenterX;
        [SerializeField] private float reachableCenterZ = 0.95f;
        [SerializeField] private float reachableSettleDistance = 0.08f;
        [SerializeField] private float lostPuckDistance = 1.15f;
        [SerializeField] private float activeBladeDistance = 0.75f;
        [SerializeField] private float activeBladeSpeed = 0.12f;
        [SerializeField] private float recentContactDelay = 0.9f;

        private Transform movingRinkRoot;
        private Transform playerRoot;
        private PuckController puck;
        private StickTracker stickTracker;
        private Vector2 puckAssistVelocity;

        public void Initialize(Transform rinkRoot, PuckController targetPuck, Transform stablePlayerRoot, StickTracker trackedStick)
        {
            movingRinkRoot = rinkRoot;
            puck = targetPuck;
            playerRoot = stablePlayerRoot;
            stickTracker = trackedStick;
        }

        private void Update()
        {
            if (movingRinkRoot == null)
            {
                return;
            }

            movingRinkRoot.localPosition += Vector3.back * (skateSpeed * Time.deltaTime);
            MovePuckTowardReachableZone();
        }

        public void ResetScroll()
        {
            if (movingRinkRoot != null)
            {
                movingRinkRoot.localPosition = Vector3.zero;
                movingRinkRoot.localRotation = Quaternion.identity;
            }
        }

        private void MovePuckTowardReachableZone()
        {
            if (puck == null || playerRoot == null)
            {
                return;
            }

            var puckLocal = playerRoot.InverseTransformPoint(puck.transform.position);
            var current = new Vector2(puckLocal.x, puckLocal.z);
            var target = new Vector2(reachableCenterX, reachableCenterZ);
            var distanceFromHome = Vector2.Distance(current, target);
            if (distanceFromHome < lostPuckDistance || IsActivelyStickhandling(puckLocal))
            {
                puckAssistVelocity = Vector2.zero;
                return;
            }

            if ((target - current).sqrMagnitude <= reachableSettleDistance * reachableSettleDistance)
            {
                puckAssistVelocity = Vector2.zero;
                return;
            }

            var next = Vector2.SmoothDamp(
                current,
                target,
                ref puckAssistVelocity,
                puckReachAssistSmoothTime,
                puckReachAssistSpeed,
                Time.deltaTime);
            var delta = next - current;
            puck.MoveLocal(new Vector3(delta.x, 0.0f, delta.y));
        }

        private bool IsActivelyStickhandling(Vector3 puckLocal)
        {
            if (stickTracker == null || !stickTracker.HasTrackedPose)
            {
                return false;
            }

            if (Time.time - stickTracker.LastBladeContactTime <= recentContactDelay)
            {
                return true;
            }

            var bladeLocal = playerRoot.InverseTransformPoint(stickTracker.BladePosition);
            var bladeToPuck = new Vector2(bladeLocal.x - puckLocal.x, bladeLocal.z - puckLocal.z);
            if (bladeToPuck.magnitude > activeBladeDistance)
            {
                return false;
            }

            var localBladeVelocity = playerRoot.InverseTransformDirection(stickTracker.BladeVelocity);
            var planarSpeed = new Vector2(localBladeVelocity.x, localBladeVelocity.z).magnitude;
            return planarSpeed >= activeBladeSpeed;
        }
    }
}
