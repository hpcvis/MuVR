using UnityEngine;

public class ProjectOnGround : MonoBehaviour {
    public CharacterTrajectoryAndAnimScript character;
    public CharacterTrajectoryAndAnimScript.JointType joint;

    public void Update() {
        var j = character.GetJoint(joint).jointPoint;
        var up = j.transform.up;

        if (Physics.Raycast(new Ray(j.transform.position + up, -up), out var hit, 20, LayerMask.NameToLayer("Character"))) {
            transform.position = hit.point;
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(j.transform.forward, hit.normal), Vector3.up);
        } else {
            transform.position = j.transform.position;
            transform.rotation = j.transform.rotation;
        }
    }
}
