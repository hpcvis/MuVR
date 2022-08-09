using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace MuVR {
	public abstract class UserAvatarPostProcessed : UserAvatar {
		protected readonly Dictionary<string, PoseRef> rawSlotData = new();

		[Preserve]
		public void Awake() {
			foreach (var slot in slots.Keys) 
				rawSlotData[slot] = new PoseRef();
		}

		public override PoseRef GetterPoseRef(string slot) => slots[slot];
		public override PoseRef SetterPoseRef(string slot) => rawSlotData[slot];
		
		public ref Pose GetProcessedPose(string slot) => ref GetterPoseRef(slot).pose;
		public ref Pose GetRawPose(string slot) => ref SetterPoseRef(slot).pose;

		public void Update() {
			foreach (var slot in rawSlotData.Keys) 
				GetProcessedPose(slot) = OnPostProcess(slot, GetProcessedPose(slot), GetRawPose(slot));
		}

		// Function that can be overridden in derived classes to process the data in some way
		public abstract Pose OnPostProcess(string slot, Pose processed, Pose raw);
	}
}