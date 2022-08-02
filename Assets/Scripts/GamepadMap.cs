using UnityEngine;

public static class GamepadMap {
	public static float RightStickAxisX => Input.GetAxis("Right Stick X");
	public static float RightStickAxisY => Input.GetAxis("Right Stick Y");
	public static float LeftStickAxisX => Input.GetAxis("Horizontal");
	public static float LeftStickAxisY => Input.GetAxis("Vertical");
	public static float LeftTrigger => Input.GetAxis("Left Trigger");
	public static float RightTrigger => Input.GetAxis("Right Trigger");
	public static float DPadAxisX => Input.GetAxis("D-Pad X Axis");
	public static float DPadAxisY => Input.GetAxis("D-Pad Y Axis");

	public static bool ButtonA => Input.GetButton("Submit");
	public static bool ButtonB => Input.GetButton("Cancel");
	public static bool ButtonX => Input.GetButton("Fire3");
	public static bool ButtonY => Input.GetButton("Jump");
	public static bool RightBumper => Input.GetButton("Right Bumper");
	public static bool LeftBumper => Input.GetButton("Left Bumper");
	public static bool ButtonBack => Input.GetButton("Back Button");
	public static bool ButtonStart => Input.GetButton("Start Button");
}