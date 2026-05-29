using System;
using UnityEngine;
using UnityEngine.XR;

namespace HockeyStickhandling
{
    public sealed class PuckResetInput : MonoBehaviour
    {
        private PuckController puck;
        private Action sceneReset;
        private bool resetWasHeld;

        public void Initialize(PuckController targetPuck, Action resetCallback)
        {
            puck = targetPuck;
            sceneReset = resetCallback;
        }

        private void Update()
        {
            if (puck == null)
            {
                return;
            }

            var resetHeld = IsResetHeld(XRNode.LeftHand) ||
                            IsResetHeld(XRNode.RightHand);

            if (resetHeld && !resetWasHeld)
            {
                sceneReset?.Invoke();
                if (sceneReset == null)
                {
                    puck.ResetPuck();
                }
            }

            resetWasHeld = resetHeld;
        }

        private static bool IsResetHeld(XRNode node)
        {
            var state = XRControllerDiagnostics.GetState(node);
            return state.deviceValid &&
                   state.device.TryGetFeatureValue(CommonUsages.secondaryButton, out var pressed) &&
                   pressed;
        }
    }
}
