using UnityEngine;

namespace uMuVR.Utility.Constraints {

    /// <summary>
    /// Constraint which updates a joint to match the position of a PFNN joint
    /// </summary>
    public class CopyFromJoint : MonoBehaviour {
        /// <summary>
        /// The PFNN character to reference
        /// </summary>
        public PFNN.Controller character;
        /// <summary>
        /// The Joint within <see cref="character"/> to reference 
        /// </summary>
        public PFNN.Controller.JointType joint;

        /// <summary>
        /// Every frame copy the pose of the joint to the object
        /// </summary>
        public void Update() {
            var j = character.GetJoint(joint).jointPoint;

            transform.position = j.transform.position;
            transform.rotation = j.transform.rotation;
        }
    }
}
