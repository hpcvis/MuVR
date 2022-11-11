using System;
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Core;
using UnityEngine;
using UnityEngine.XR;

namespace UltimateXR.Devices.Integrations.OpenXR
{
	public class UxrOpenXRControllerInput : UxrUnityXRControllerInput
	{
		public string[] supportedControllers;
		
		public override string SDKDependency => UxrManager.SdkUnityInputSystem;
		public override UxrControllerSetupType SetupType => UxrControllerSetupType.Dual;
		public override bool IsHandednessSupported => true;

		public override bool HasControllerElements(UxrHandSide handSide, UxrControllerElements controllerElements) => true;
		// {
		// 	InputDevice device;
		// 	try
		// 	{
		// 		device = GetInputDevice(handSide);
		// 		var s = device.name.Contains("s"); // Call a function on the name which should invoke a null reference if not there
		// 	}
		// 	catch (NullReferenceException)
		// 	{
		// 		// return true;
		// 		var inputDevices = new List<InputDevice>();
		// 		InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | (handSide == UxrHandSide.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right), inputDevices);
		// 		inputDevices = inputDevices.Where(candidate => supportedControllers.Any(supported => candidate.name.Contains(supported))).ToList();
		// 	
		// 		// If there are no matching devices then it doesn't have the controller elements
		// 		if (inputDevices.Count == 0) return false;
		//
		// 		device = inputDevices[0];
		// 	}
		// 	
		// 	if (device.name.Contains("HTC"))
		// 		return ((uint)(UxrControllerElements.Joystick | 
		// 		        UxrControllerElements.Grip |     
		// 		        UxrControllerElements.Trigger |  
		// 		        UxrControllerElements.Button1 |  
		// 		        UxrControllerElements.DPad) & (uint)controllerElements) == (uint)controllerElements;
		//
		// 	// If we haven't been "trained" to understand a controller then just return false
		// 	return false;
		// }

		public override IEnumerable<string> ControllerNames => supportedControllers;
	}
}

