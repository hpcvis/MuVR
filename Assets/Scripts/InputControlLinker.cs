using System;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

// Class that links transforms found in the avatar to child SyncTransforms
public class InputControlLinker : MonoBehaviour {
	// Dictionary mapping SyncTransforms to their associated transform in the avatar
	// TODO: Instead of using an enum could we instead use strings and a custom editor to make entering an invalid string difficult?
	[Serializable] public class LinkageDictionary : SerializableDictionaryBase<PlayerAvatar.Linkage, SyncTransform> { }
	[SerializeField] private LinkageDictionary links = new LinkageDictionary();
	
	private void Start() {
		// Get a reference to the avatar (should be attached to the parent object)
		var avatar = transform.parent.GetComponent<PlayerAvatar>();
		
		// For each linkage in the dictionary, create the connection
		foreach (var (linkage, sync) in links)
			switch (linkage) {
					   case PlayerAvatar.Linkage.head: sync.target = avatar.head;
				break; case PlayerAvatar.Linkage.leftShoulder: sync.target = avatar.leftShoulder;
				break; case PlayerAvatar.Linkage.rightShoulder: sync.target = avatar.rightShoulder;
				break; case PlayerAvatar.Linkage.leftElbow: sync.target = avatar.leftElbow;
				break; case PlayerAvatar.Linkage.rightElbow: sync.target = avatar.rightElbow;
				break; case PlayerAvatar.Linkage.leftHand: sync.target = avatar.leftHand;
				break; case PlayerAvatar.Linkage.rightHand: sync.target = avatar.rightHand;
				break; case PlayerAvatar.Linkage.pelvis: sync.target = avatar.pelvis;
				break; case PlayerAvatar.Linkage.leftKnee: sync.target = avatar.leftKnee;
				break; case PlayerAvatar.Linkage.rightKnee: sync.target = avatar.rightKnee;
				break; case PlayerAvatar.Linkage.leftFoot: sync.target = avatar.leftFoot;
				break; case PlayerAvatar.Linkage.rightFoot: sync.target = avatar.rightFoot; 
				break;
			}
	}
}