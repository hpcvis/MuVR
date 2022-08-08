using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProjectOnGround : MonoBehaviour {
	public static ProjectOnGround[] inScene;
	public static float averageHeightDifference;

	public CharacterTrajectoryAndAnimScript character;
	public CharacterTrajectoryAndAnimScript.JointType toeJoint, ankleJoint; //, kneeJoint;

	public float heelHeight;
	public float toeOffset;
	public float targetPhase = Mathf.PI;
	private float heightDifference;

	public void OnEnable() {
		inScene = inScene is null ? new[] { this } : new List<ProjectOnGround>(inScene) { this }.ToArray();
	}

	public void OnDisable() {
		var list = new List<ProjectOnGround>(inScene);
		list.Remove(this);
		inScene = list.Count > 0 ? list.ToArray() : null;
	}

	public void Update() {
		CalculateFoot(character.phase, character.IsStanding() ? 1 : 0, Vector3.up, out var position, out var rotation);
		transform.position = position;
		transform.rotation = rotation;
	}

	private void CalculateFoot(float phase, float standing, Vector3 up, out Vector3 position, out Quaternion rotation) {
		var comparisonHeight = heelHeight > .1f ? heelHeight : .1f;

		var toe = character.GetJoint(toeJoint).jointPoint;
		var ankle = character.GetJoint(ankleJoint).jointPoint;
		// var knee = character.GetJoint(kneeJoint).jointPoint;

		var weight = 1 - Mathf.Min(Mathf.Abs(phase - targetPhase), Mathf.Abs(phase - targetPhase - Mathf.PI * 2)) / Mathf.PI;
		weight += standing;
		weight = Mathf.Clamp(weight, 0, 1);
		// Debug.Log($"{ankleJoint.ToString()} - {weight}");

		var toeProjected = toe.transform.position;
		var ankleProjected = ankle.transform.position;
		var toeNormal = up;
		var ankleNormal = up;

		if (Physics.Raycast(new Ray(toeProjected + up, -up), out var hit, Mathf.Infinity, LayerMask.NameToLayer("Character"))) {
			toeProjected = hit.point;
			toeNormal = hit.normal;
		}

		if (Physics.Raycast(new Ray(ankleProjected + up, -up), out hit, Mathf.Infinity, LayerMask.NameToLayer("Character"))) {
			ankleProjected = hit.point;
			ankleNormal = hit.normal;
		}

		var anklePosition = ankleProjected;
		if (Mathf.Abs(ankleProjected.y + heelHeight - toeProjected.y) > comparisonHeight)
			// Big slope case!
			anklePosition.y = toeProjected.y + heelHeight;
		else
			// Gradual slope case!
			anklePosition.y += heelHeight;

		// Make sure the ankle isn't clipping through the ground
		if (anklePosition.y < ankleProjected.y) anklePosition.y = ankleProjected.y;
		toeProjected.y += toeOffset;

		// Calculate the average height distance (Used to offset the other points)
		heightDifference = ankle.transform.position.y - anklePosition.y;
		averageHeightDifference = inScene.Aggregate(0f, (total, next) => total + next.heightDifference, total => total / inScene.Length);
		// averageHeightDifference = 0;
		// foreach (var p in inScene)
		// 	averageHeightDifference += p.heightDifference;
		// averageHeightDifference /= inScene.Length * 2;

		// // Make sure the ankle isn't too far away from the knee
		// Vector3 toKnee = knee.transform.position - ankle.transform.position;
		// float hipDistance = toKnee.magnitude;
		// Debug.Log($"{averageHeightDifference} - {inScene.Length}");
		// float oldToHip = (hip.transform.position - ankle.transform.position).magnitude; 
		// if (hipDistance > oldToHip) {
		//     anklePosition += toHip.normalized * (hipDistance - oldToHip);
		// }

		rotation = Quaternion.Slerp(ankle.transform.rotation, Quaternion.LookRotation(toeProjected - anklePosition, (ankleNormal + toeNormal).normalized), weight);
		position = Vector3.Lerp(ankle.transform.position, anklePosition, weight * weight);
	}
}