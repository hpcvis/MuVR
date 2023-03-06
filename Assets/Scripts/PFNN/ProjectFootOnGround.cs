using System.Collections.Generic;
using System.Linq;
using uMuVR.Enhanced;
using UnityEngine;

/// <summary>
///     Component which performs foot IK
/// </summary>
public class ProjectFootOnGround : MonoBehaviour {
	/// <summary>
	///     List of all the component's currently in the scene
	/// </summary>
	public static ProjectFootOnGround[] inScene;
	/// <summary>
	///     The average distance the IK moves all the feet in the scene
	/// </summary>
	public static float averageHeightDifference;

	/// <summary>
	///     Reference to the PFNN controller
	/// </summary>
	public PFNN.Controller character;
	/// <summary>
	///     Locations of the toe and ankle joints
	/// </summary>
	public PFNN.Controller.JointType toeJoint, ankleJoint;

	/// <summary>
	///     The height of the heal above the toes
	/// </summary>
	public float heelHeight;
	/// <summary>
	///     The height the toes should be above the ground (half the thickness of the toes)
	/// </summary>
	public float toeOffset;
	/// <summary>
	///     The point in the phase when the foot should be lifted off the ground
	/// </summary>
	public float targetPhase = UnityEngine.Mathf.PI;

	/// <summary>
	///     The average distance this IK instance moves its associated foot
	/// </summary>
	private float heightDifference;

	/// <summary>
	///     When we are dis/enabled remove/add us to the list of Projections
	/// </summary>
	public void OnEnable() {
		inScene = inScene is null ? new[] { this } : new List<ProjectFootOnGround>(inScene) { this }.ToArray();
	}
	public void OnDisable() {
		var list = new List<ProjectFootOnGround>(inScene);
		list.Remove(this);
		inScene = list.Count > 0 ? list.ToArray() : null;
	}


	/// <summary>
	///     Every frame update the foot IK
	/// </summary>
	public void Update() {
		transform.CopyFrom(CalculateFoot(character.phase, character.IsStanding() ? 1 : 0, Vector3.up));
	}

	/// <summary>
	///     Calculates the foot placement
	/// </summary>
	/// <param name="phase">Where in the in the walk cycle we currently area</param>
	/// <param name="standing">How fast the character is moving</param>
	/// <param name="up">The world space vector which defines up</param>
	/// <returns>The new pose of the foot</returns>
	private Pose CalculateFoot(float phase, float standing, Vector3 up) {
		// Calculate the IK weight based on the face and standing
		var weight = 1 - UnityEngine.Mathf.Min(UnityEngine.Mathf.Abs(phase - targetPhase), UnityEngine.Mathf.Abs(phase - targetPhase - UnityEngine.Mathf.PI)) / UnityEngine.Mathf.PI;
		weight += standing;
		weight = UnityEngine.Mathf.Clamp(weight, 0, 1);

		// Set a minimum on heelHeight
		var comparisonHeight = heelHeight > .1f ? heelHeight : .1f;

		// Label external state
		var toe = character.GetJoint(toeJoint).jointPoint;
		var ankle = character.GetJoint(ankleJoint).jointPoint;
		var toeProjected = toe.transform.position;
		var ankleProjected = ankle.transform.position;
		var toeNormal = up;
		var ankleNormal = up;

		// Perform a toe and ankle projection
		if (Physics.Raycast(new Ray(toeProjected + up, -up), out var hit, UnityEngine.Mathf.Infinity, LayerMask.NameToLayer("Character"))) {
			toeProjected = hit.point;
			toeNormal = hit.normal;
		}
		if (Physics.Raycast(new Ray(ankleProjected + up, -up), out hit, UnityEngine.Mathf.Infinity, LayerMask.NameToLayer("Character"))) {
			ankleProjected = hit.point;
			ankleNormal = hit.normal;
		}

		// Unique angle formula
		var anklePosition = ankleProjected;
		if (UnityEngine.Mathf.Abs(ankleProjected.y + heelHeight - toeProjected.y) > comparisonHeight)
			// Big slope case!
			anklePosition.y = toeProjected.y + heelHeight;
		else
			// Gradual slope case!
			anklePosition.y += heelHeight;

		// Make sure the ankle isn't clipping through the ground
		if (anklePosition.y < ankleProjected.y) anklePosition.y = ankleProjected.y;
		toeProjected.y += toeOffset;
		var baseAnklePosition = ankle.transform.position;
		baseAnklePosition.y = UnityEngine.Mathf.Max(baseAnklePosition.y, ankleProjected.y);

		// Blend the position and rotation with the original ones based on the animation phase so that he can pick his feet up off the ground!
		Pose output;
		output.rotation = Quaternion.Slerp(ankle.transform.rotation, Quaternion.LookRotation(toeProjected - anklePosition, (ankleNormal + toeNormal).normalized), weight);
		output.position = Vector3.Lerp(baseAnklePosition, anklePosition, weight * weight);

		// Calculate the average height distance (Used to offset the other points)
		heightDifference = ankle.transform.position.y - output.position.y;
		const float alpha = .9f;
		averageHeightDifference = alpha * averageHeightDifference + (1 - alpha) * /*new*/inScene.Aggregate(0f, (total, next) => total + next.heightDifference, total => total / inScene.Length);

		// TODO: Need to make sure switching to a pose here didn't cause a regression!
		return output;
	}
}