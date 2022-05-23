using UnityEngine;

// Component that copies the transform from the object it is attached to, to a pose slot on a PlayerAvatar
public class SyncPose : MonoBehaviour {
	// Enum setting weather we should be sending our transform to the pose, or reading our transform from the pose
	public enum SyncMode {
		SyncTo,
		SyncFrom,
	}
	
	[Tooltip("Avatar we are syncing with")]
	public PlayerAvatar targetAvatar;
	[Tooltip("Which pose on the avatar we are syncing with")]
	public PlayerAvatar.Slot slot;
	[Tooltip("Should we send our transform to the pose, or update our transform to match the pose?")]
	public SyncMode mode;
	
	[Tooltip("Offset applied while syncing")]
	public Pose offset;

	[Tooltip("Whether or not we should sync transforms or rotations")]
	private bool syncTransforms, syncRotations;
	
	[SerializeField, ReadOnly] private PlayerAvatar.PoseRef target;

	// When the object is created make sure to update the target
	void Start() => UpdateTarget();

	// Function that finds the target from the target avatar and slot
	public void UpdateTarget() {
		switch (slot) {
				   case PlayerAvatar.Slot.head: target = targetAvatar.head;
			break; case PlayerAvatar.Slot.leftShoulder: target = targetAvatar.leftShoulder;
			break; case PlayerAvatar.Slot.rightShoulder: target = targetAvatar.rightShoulder;
			break; case PlayerAvatar.Slot.leftElbow: target = targetAvatar.leftElbow;
			break; case PlayerAvatar.Slot.rightElbow: target = targetAvatar.rightElbow;
			break; case PlayerAvatar.Slot.leftHand: target = targetAvatar.leftHand;
			break; case PlayerAvatar.Slot.rightHand: target = targetAvatar.rightHand;
			break; case PlayerAvatar.Slot.pelvis: target = targetAvatar.pelvis;
			break; case PlayerAvatar.Slot.leftKnee: target = targetAvatar.leftKnee;
			break; case PlayerAvatar.Slot.rightKnee: target = targetAvatar.rightKnee;
			break; case PlayerAvatar.Slot.leftFoot: target = targetAvatar.leftFoot;
			break; case PlayerAvatar.Slot.rightFoot: target = targetAvatar.rightFoot; 
			break;
		}
	}

	// Update is called once per frame, and make sure that our transform is properly synced with the pose according to the pose mode
	private void Update() {
		if (mode == SyncMode.SyncTo) {
			target.pose.position = transform.position + offset.position;
			target.pose.rotation = transform.rotation * offset.rotation;
		} else {
			transform.position = target.pose.position + offset.position;
			transform.rotation = target.pose.rotation * offset.rotation;
		}
	}
}