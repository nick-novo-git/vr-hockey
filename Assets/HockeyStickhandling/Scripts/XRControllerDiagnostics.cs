using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

namespace HockeyStickhandling
{
    public static class XRControllerDiagnostics
    {
        private static readonly List<InputDevice> Devices = new List<InputDevice>();
        private static float nextLogTime;

        public struct ControllerState
        {
            public XRNode node;
            public InputDevice device;
            public bool deviceValid;
            public bool positionValid;
            public bool rotationValid;
            public bool trackingStateValid;
            public InputTrackingState trackingState;
            public bool triggerPressed;
            public bool selectPressed;
            public Vector3 position;
            public Quaternion rotation;

            public bool HasTrackedPose =>
                deviceValid &&
                positionValid &&
                rotationValid &&
                (!trackingStateValid ||
                 (trackingState.HasFlag(InputTrackingState.Position) &&
                  trackingState.HasFlag(InputTrackingState.Rotation)));
        }

        public static ControllerState GetState(XRNode node)
        {
            var state = new ControllerState
            {
                node = node,
                rotation = Quaternion.identity
            };

            state.device = GetBestDevice(node);
            state.deviceValid = state.device.isValid;
            if (!state.deviceValid)
            {
                return state;
            }

            state.positionValid = state.device.TryGetFeatureValue(CommonUsages.devicePosition, out state.position);
            state.rotationValid = state.device.TryGetFeatureValue(CommonUsages.deviceRotation, out state.rotation);
            state.trackingStateValid = state.device.TryGetFeatureValue(CommonUsages.trackingState, out state.trackingState);
            state.triggerPressed =
                (state.device.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerButton) && triggerButton) ||
                (state.device.TryGetFeatureValue(CommonUsages.trigger, out var triggerValue) && triggerValue > 0.55f);
            state.selectPressed =
                state.triggerPressed ||
                (state.device.TryGetFeatureValue(CommonUsages.primaryButton, out var primaryButton) && primaryButton) ||
                (state.device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton);
            return state;
        }

        public static bool TryGetPose(XRNode node, out Vector3 position, out Quaternion rotation, out InputDevice device)
        {
            var state = GetState(node);
            position = state.position;
            rotation = state.rotation;
            device = state.device;
            return state.HasTrackedPose;
        }

        public static string FormatState(string label, ControllerState state)
        {
            var tracking = state.trackingStateValid ? state.trackingState.ToString() : "n/a";
            return $"{label} valid:{state.deviceValid} pos:{state.positionValid} rot:{state.rotationValid} tracking:{tracking} trigger:{state.triggerPressed} select:{state.selectPressed}";
        }

        public static void LogBothStates(string source, float intervalSeconds = 1.0f)
        {
            if (Time.time < nextLogTime)
            {
                return;
            }

            nextLogTime = Time.time + intervalSeconds;
            var left = GetState(XRNode.LeftHand);
            var right = GetState(XRNode.RightHand);
            Debug.Log($"[{source}] backend:{GetBackendDescription()} | {FormatState("Left", left)} | {FormatState("Right", right)}");
        }

        private static string GetBackendDescription()
        {
            var loadedDevice = string.IsNullOrEmpty(XRSettings.loadedDeviceName)
                ? "none"
                : XRSettings.loadedDeviceName;
#if XR_MANAGEMENT
            var loader = XRGeneralSettings.Instance != null &&
                         XRGeneralSettings.Instance.Manager != null &&
                         XRGeneralSettings.Instance.Manager.activeLoader != null
                ? XRGeneralSettings.Instance.Manager.activeLoader.name
                : "no-loader";
            return $"XRSettings={loadedDevice}, loader={loader}";
#else
            return $"XRSettings={loadedDevice}";
#endif
        }

        private static InputDevice GetBestDevice(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
            {
                return device;
            }

            Devices.Clear();
            var characteristics = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand;
            characteristics |= node == XRNode.LeftHand
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;
            InputDevices.GetDevicesWithCharacteristics(characteristics, Devices);
            for (var i = 0; i < Devices.Count; i++)
            {
                if (Devices[i].isValid)
                {
                    return Devices[i];
                }
            }

            return device;
        }
    }
}
