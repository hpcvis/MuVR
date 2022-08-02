using Generated;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public static class GamepadMap {
	private static readonly InputActions InputActions = new InputActions();
	public static void Enable() => InputActions.Enable();
	public static void Disable() => InputActions.Disable();
	
	public static float RightStickAxisX => InputActions.Default.Look.ReadValue<Vector2>().x;
	public static float RightStickAxisY => InputActions.Default.Look.ReadValue<Vector2>().y;
	public static float LeftStickAxisX => InputActions.Default.Move.ReadValue<Vector2>().x;
	public static float LeftStickAxisY => InputActions.Default.Move.ReadValue<Vector2>().y;
	public static float LeftTrigger => InputActions.Default.Strafe.ReadValue<float>();
	public static float RightTrigger => InputActions.Default.Sprint.ReadValue<float>();
	// public static float DPadAxisX => Input.GetAxis("D-Pad X Axis");
	// public static float DPadAxisY => Input.GetAxis("D-Pad Y Axis");

	// public static bool ButtonA => Input.GetButton("Submit");
	public static bool ButtonB => InputActions.Default.ToggleCrouch.ReadValue<float>() > .2f;
	// public static bool ButtonX => Input.GetButton("Fire3");
	// public static bool ButtonY => Input.GetButton("Jump");
	public static bool RightBumper => Mathf.Abs(InputActions.Default.ZoomIn.ReadValue<float>()) > .2f;
	public static bool LeftBumper => Mathf.Abs(InputActions.Default.ZoomOut.ReadValue<float>()) > .2f;
	public static bool ButtonBack => InputActions.Default.Reset.ReadValue<float>() > .2f;
	// public static bool ButtonStart => Input.GetButton("Start Button");
}