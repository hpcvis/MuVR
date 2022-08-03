using UnityEngine;

public class CopyFromJoint : MonoBehaviour {
    public CharacterTrajectoryAndAnimScript character;
    public CharacterTrajectoryAndAnimScript.JointType joint;

    public void Update() {
        var j = character.GetJoint(joint).jointPoint;
        
        transform.position = j.transform.position;
        transform.rotation = j.transform.rotation;
    }
}
