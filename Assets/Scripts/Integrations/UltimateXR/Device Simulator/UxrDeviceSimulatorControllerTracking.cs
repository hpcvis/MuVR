using System;
using UltimateXR.Core;
using UltimateXR.Extensions.Unity;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace UltimateXR.Devices.Integrations.DeviceSimulator {
    /// <summary>
    ///     Base class for tracking devices based on OpenVR.
    /// </summary>
    public class UxrDeviceSimulatorControllerTracking : UxrControllerTracking {
		public XRDeviceSimulator simulator;

		public override string SDKDependency => UxrManager.SdkUnityInputSystem;

		public override Type RelatedControllerInputType => typeof(UxrDeviceSimulatorControllerInput);

		protected override void Start() {
			base.Start();

			simulator ??= GetComponent<XRDeviceSimulator>();

			if (!isActiveAndEnabled) return;

			// Update the camera's tracked pose driver to get its position from the simulator
			var poseDriver = Avatar.CameraComponent.GetComponent<TrackedPoseDriver>();
			poseDriver.poseProviderComponent = simulator;
		}

		// When UI validation is performed, add a simulator to the same object if one has not already been specified
		protected virtual void OnValidate() => simulator ??= gameObject.GetOrAddComponent<XRDeviceSimulator>();

		/// <inheritdoc />
		protected override void UpdateSensors() {
			base.UpdateSensors();

			if (Avatar.CameraComponent == null) {
				Debug.LogWarning("No camera has been setup for this avatar");
				return;
			}

			LocalAvatarLeftHandSensorRot = simulator.LeftControllerState.deviceRotation;
			LocalAvatarLeftHandSensorPos = simulator.LeftControllerState.devicePosition;

			LocalAvatarRightHandSensorRot = simulator.RightControllerState.deviceRotation;
			LocalAvatarRightHandSensorPos = simulator.RightControllerState.devicePosition;
		}
	}
}