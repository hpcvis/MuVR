using UltimateXR.Core;
using UltimateXR.Extensions.Unity;
using UltimateXR.Haptics;
using UnityEngine;

namespace UltimateXR.Devices.Integrations.DeviceSimulator {
	/// <summary>
	///     Generic base class for left-right input devices that can be handled through the new
	///     generic Unity XR input interface. Before, we had to manually support each SDK individually.
	/// </summary>
	public class UxrDeviceSimulatorControllerInput : UxrControllerInput {
		public XRDeviceSimulator simulator;

		#region Public Overrides UxrControllerInput

		public override UxrControllerSetupType SetupType => UxrControllerSetupType.Dual;
		public override bool IsHandednessSupported => true;

		/// <inheritdoc />
		public override string LeftControllerName => "Device Simulator Left";

		/// <inheritdoc />
		public override string RightControllerName => "Device Simulator Right";

		/// <inheritdoc />
		public override bool IsControllerEnabled(UxrHandSide handSide) => enabled;

		/// <inheritdoc />
		public override bool HasControllerElements(UxrHandSide handSide, UxrControllerElements controllerElement) => true;

		/// <inheritdoc />
		public override float GetInput1D(UxrHandSide handSide, UxrInput1D input1D, bool getIgnoredInput = false) {
			if (ShouldIgnoreInput(handSide, getIgnoredInput)) return 0.0f;

			return input1D switch {
				UxrInput1D.Grip => handSide == UxrHandSide.Left ? simulator.LeftControllerState.grip : simulator.RightControllerState.grip,
				UxrInput1D.Trigger => handSide == UxrHandSide.Left ? simulator.LeftControllerState.trigger : simulator.RightControllerState.trigger,
				_ => 0
			};
		}

		/// <inheritdoc />
		public override Vector2 GetInput2D(UxrHandSide handSide, UxrInput2D input2D, bool getIgnoredInput = false) {
			if (ShouldIgnoreInput(handSide, getIgnoredInput)) return Vector2.zero;

			return input2D switch {
				UxrInput2D.Joystick => FilterTwoAxesDeadZone(handSide == UxrHandSide.Left ? simulator.LeftControllerState.primary2DAxis : simulator.RightControllerState.primary2DAxis, JoystickDeadZone),
				UxrInput2D.Joystick2 => FilterTwoAxesDeadZone(handSide == UxrHandSide.Left ? simulator.LeftControllerState.secondary2DAxis : simulator.RightControllerState.secondary2DAxis, JoystickDeadZone),
				_ => Vector2.zero
			};
		}

		/// <inheritdoc />
		public override UxrControllerInputCapabilities GetControllerCapabilities(UxrHandSide handSide) => 0;

		/// <inheritdoc />
		public override void SendHapticFeedback(UxrHandSide handSide, UxrHapticClip hapticClip) { }

		/// <inheritdoc />
		public override void SendHapticFeedback(UxrHandSide handSide,
			float frequency,
			float amplitude,
			float durationSeconds,
			UxrHapticMode hapticMode = UxrHapticMode.Mix) { }

		/// <inheritdoc />
		public override void StopHapticFeedback(UxrHandSide handSide) { }

		#endregion

		#region Unity

		/// <summary>
		///     Initializes variables and subscribes to events.
		///     If the controllers were already initialized, enables the component. Otherwise it begins disabled until devices are
		///     connected.
		/// </summary>
		protected override void Awake() {
			base.Awake();

			simulator ??= GetComponent<XRDeviceSimulator>();

			if (enabled) RaiseConnectOnStart = enabled;
		}

		// When UI validation is performed, add a simulator to the same object if one has not already been specified
		protected virtual void OnValidate() => simulator ??= gameObject.GetOrAddComponent<XRDeviceSimulator>();

		#endregion


		#region Protected Overrides UxrControllerInput

		/// <summary>
		///     Updates the input state. This should not be called by the user since it is called by the framework already.
		/// </summary>
		protected override void UpdateInput() {
			base.UpdateInput();

			var buttonPressTriggerLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Trigger, ButtonContact.Press);
			var buttonPressTriggerRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Trigger, ButtonContact.Press);
			var buttonPressJoystickLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Joystick, ButtonContact.Press);
			var buttonPressJoystickRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Joystick, ButtonContact.Press);
			var buttonPressButton1Left = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Button1, ButtonContact.Press);
			var buttonPressButton1Right = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Button1, ButtonContact.Press);
			var buttonPressButton2Left = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Button2, ButtonContact.Press);
			var buttonPressButton2Right = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Button2, ButtonContact.Press);
			var buttonPressMenuLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Menu, ButtonContact.Press);
			var buttonPressMenuRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Menu, ButtonContact.Press);
			var buttonPressGripLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Grip, ButtonContact.Press);
			var buttonPressGripRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Grip, ButtonContact.Press);
			var buttonPressThumbCapSenseLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.ThumbCapSense, ButtonContact.Press);
			var buttonPressThumbCapSenseRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.ThumbCapSense, ButtonContact.Press);

			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Trigger, buttonPressTriggerLeft);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Trigger, buttonPressTriggerRight);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Joystick, buttonPressJoystickLeft);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Joystick, buttonPressJoystickRight);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Button1, buttonPressButton1Left);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Button1, buttonPressButton1Right);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Button2, buttonPressButton2Left);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Button2, buttonPressButton2Right);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Menu, buttonPressMenuLeft);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Menu, buttonPressMenuRight);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.Grip, buttonPressGripLeft);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.Grip, buttonPressGripRight);
			SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.ThumbCapSense, buttonPressThumbCapSenseLeft);
			SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.ThumbCapSense, buttonPressThumbCapSenseRight);

			var buttonTouchTriggerLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Trigger, ButtonContact.Touch);
			var buttonTouchTriggerRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Trigger, ButtonContact.Touch);
			var buttonTouchJoystickLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Joystick, ButtonContact.Touch);
			var buttonTouchJoystickRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Joystick, ButtonContact.Touch);
			var buttonTouchButton1Left = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Button1, ButtonContact.Touch);
			var buttonTouchButton1Right = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Button1, ButtonContact.Touch);
			var buttonTouchButton2Left = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Button2, ButtonContact.Touch);
			var buttonTouchButton2Right = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Button2, ButtonContact.Touch);
			var buttonTouchMenuLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Menu, ButtonContact.Touch);
			var buttonTouchMenuRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Menu, ButtonContact.Touch);
			var buttonTouchGripLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.Grip, ButtonContact.Touch);
			var buttonTouchGripRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.Grip, ButtonContact.Touch);
			var buttonTouchThumbCapSenseLeft = HasButtonContact(UxrHandSide.Left, UxrInputButtons.ThumbCapSense, ButtonContact.Touch);
			var buttonTouchThumbCapSenseRight = HasButtonContact(UxrHandSide.Right, UxrInputButtons.ThumbCapSense, ButtonContact.Touch);

			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Trigger, buttonTouchTriggerLeft);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Trigger, buttonTouchTriggerRight);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Joystick, buttonTouchJoystickLeft);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Joystick, buttonTouchJoystickRight);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Button1, buttonTouchButton1Left);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Button1, buttonTouchButton1Right);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Button2, buttonTouchButton2Left);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Button2, buttonTouchButton2Right);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Menu, buttonTouchMenuLeft);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Menu, buttonTouchMenuRight);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.Grip, buttonTouchGripLeft);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.Grip, buttonTouchGripRight);
			SetButtonFlags(ButtonFlags.TouchFlagsLeft, UxrInputButtons.ThumbCapSense, buttonTouchThumbCapSenseLeft);
			SetButtonFlags(ButtonFlags.TouchFlagsRight, UxrInputButtons.ThumbCapSense, buttonTouchThumbCapSenseRight);

			var leftJoystick = GetInput2D(UxrHandSide.Left, UxrInput2D.Joystick);
			var leftDPad = leftJoystick; // Mapped to joystick by default

			if (leftJoystick != Vector2.zero && leftJoystick.magnitude > AnalogAsDPadThreshold) {
				var joystickAngle = Input2DToAngle(leftJoystick);

				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickLeft, IsInput2dDPadLeft(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickRight, IsInput2dDPadRight(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickUp, IsInput2dDPadUp(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickDown, IsInput2dDPadDown(joystickAngle));
			} else {
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickLeft, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickRight, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickUp, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.JoystickDown, false);
			}

			if (leftDPad != Vector2.zero && leftDPad.magnitude > AnalogAsDPadThreshold) {
				var dPadAngle = Input2DToAngle(leftDPad);

				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadLeft, IsInput2dDPadLeft(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadRight, IsInput2dDPadRight(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadUp, IsInput2dDPadUp(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadDown, IsInput2dDPadDown(dPadAngle));
			} else {
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadLeft, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadRight, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadUp, false);
				SetButtonFlags(ButtonFlags.PressFlagsLeft, UxrInputButtons.DPadDown, false);
			}

			var rightJoystick = GetInput2D(UxrHandSide.Right, UxrInput2D.Joystick);
			var rightDPad = rightJoystick; // Mapped to joystick by default

			if (rightJoystick != Vector2.zero && rightJoystick.magnitude > AnalogAsDPadThreshold) {
				var joystickAngle = Input2DToAngle(rightJoystick);

				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickLeft, IsInput2dDPadLeft(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickRight, IsInput2dDPadRight(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickUp, IsInput2dDPadUp(joystickAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickDown, IsInput2dDPadDown(joystickAngle));
			} else {
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickLeft, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickRight, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickUp, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.JoystickDown, false);
			}

			if (rightDPad != Vector2.zero && rightDPad.magnitude > AnalogAsDPadThreshold) {
				var dPadAngle = Input2DToAngle(rightDPad);

				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadLeft, IsInput2dDPadLeft(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadRight, IsInput2dDPadRight(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadUp, IsInput2dDPadUp(dPadAngle));
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadDown, IsInput2dDPadDown(dPadAngle));
			} else {
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadLeft, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadRight, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadUp, false);
				SetButtonFlags(ButtonFlags.PressFlagsRight, UxrInputButtons.DPadDown, false);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		///     Checks whether the given button in a controller is currently being touched or pressed.
		/// </summary>
		/// <param name="handSide">Which controller side to check</param>
		/// <param name="button">Button to check</param>
		/// <param name="buttonContact">Type of contact to check for (touch or press)</param>
		/// <returns>Boolean telling whether the specified button has contact</returns>
		private bool HasButtonContact(UxrHandSide handSide, UxrInputButtons button, ButtonContact buttonContact) {
			if (button == UxrInputButtons.Joystick) {
				var controllerButton = buttonContact == ButtonContact.Touch ? ControllerButton.Primary2DAxisTouch : ControllerButton.Primary2DAxisClick;
				return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(controllerButton) : simulator.RightControllerState.HasButton(controllerButton);
			}

			if (button == UxrInputButtons.Joystick2) {
				var controllerButton = buttonContact == ButtonContact.Touch ? ControllerButton.Secondary2DAxisTouch : ControllerButton.Secondary2DAxisClick;
				return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(controllerButton) : simulator.RightControllerState.HasButton(controllerButton);
			}

			if (button == UxrInputButtons.Trigger) return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(ControllerButton.TriggerButton) : simulator.RightControllerState.HasButton(ControllerButton.TriggerButton);

			if (button == UxrInputButtons.Grip) return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(ControllerButton.GripButton) : simulator.RightControllerState.HasButton(ControllerButton.GripButton);

			if (button == UxrInputButtons.Button1) {
				var controllerButton = buttonContact == ButtonContact.Touch ? ControllerButton.PrimaryTouch : ControllerButton.PrimaryButton;
				return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(controllerButton) : simulator.RightControllerState.HasButton(controllerButton);
			}

			if (button == UxrInputButtons.Button2) {
				var controllerButton = buttonContact == ButtonContact.Touch ? ControllerButton.SecondaryTouch : ControllerButton.SecondaryButton;
				return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(controllerButton) : simulator.RightControllerState.HasButton(controllerButton);
			}

			if (button == UxrInputButtons.Menu) return handSide == UxrHandSide.Left ? simulator.LeftControllerState.HasButton(ControllerButton.MenuButton) : simulator.RightControllerState.HasButton(ControllerButton.MenuButton);

			return false;
		}

		#endregion

		#region Private Types & Data

		private string InputClassName => GetType().Name;

		/// <summary>
		///     Types of button contact.
		/// </summary>
		private enum ButtonContact {
			/// <summary>
			///     Button press.
			/// </summary>
			Press,

			/// <summary>
			///     Button contact without pressing.
			/// </summary>
			Touch
		}

		#endregion
	}
}