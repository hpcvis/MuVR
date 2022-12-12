using System;
using UltimateXR.Core;

namespace UltimateXR.Devices.Integrations.OpenXR {
	public class UxrOpenXRControllerTracking : UxrUnityXRControllerTracking {
		// Start is called before the first frame update
		public override Type RelatedControllerInputType => typeof(UxrOpenXRControllerInput);

		public override string SDKDependency => UxrManager.SdkUnityInputSystem;
	}
}