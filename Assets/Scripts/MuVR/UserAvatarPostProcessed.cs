using System.Collections.Generic;
using System.Linq;
using System.Text;
using MuVR.Enhanced;
using TriInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MuVR {
	// Base class providing boilerplate code for UserAvatar extensions that need the data post processed
	public abstract class UserAvatarPostProcessed : UserAvatar {
		#region Static Reference Management

		protected static UserAvatarPostProcessed[] inScene;
		protected uint indexInScene;

		protected void OnEnable() {
			if (inScene is null) {
				inScene = new[] { this };
				indexInScene = 0;
				return;
			}
			
			inScene = new List<UserAvatarPostProcessed>(inScene) { this }.ToArray();
			indexInScene = (uint)(inScene.Length - 1);
		}
		protected void OnDisable() {
			var list = new List<UserAvatarPostProcessed>(inScene);
			list.Remove(this);
			inScene = list.Count > 0 ? list.ToArray() : null;
		}

		#endregion
		
		#region Types

		// Pose Reference and Process Mode stored in the dictionary 
		public struct PostProcessData {
			public enum ProcessMode {
				Process,
				Copy,
				Ignore,
			}
			public ProcessMode processMode;
			public readonly PoseRef poseRef;

			public PostProcessData(ProcessMode processMode) {
				this.processMode = processMode;
				poseRef = new PoseRef();
			}
		}

		// Unity job that applies post processing
		protected struct PostProcessJob : IJobParallelFor {
			public uint ownerID; // The ID of the UserAvatar in the static references
			public float deltaTime;
			public NativeArray<byte>.ReadOnly slotData;
			public NativeArray<long>.ReadOnly slotStarts;

			public void Execute(int index) {
				var start = (int)slotStarts[index];
				var end = (int)(index < slotStarts.Length - 1 ? slotStarts[index + 1] : slotData.Length);
				
				var slot = Encoding.UTF8.GetString(slotData.Skip(start).Take(end - start).ToArray());
				var owner = UserAvatarPostProcessed.inScene[ownerID];
				
				if(owner.ShouldProcess(slot))
					owner.GetProcessedPose(slot) = owner.OnPostProcess(slot, owner.GetProcessedPose(slot), owner.GetRawPose(slot), deltaTime);
				else if(owner.ShouldCopy(slot))
					owner.GetProcessedPose(slot) = owner.GetRawPose(slot);
			}
		} 

		#endregion

		[Title("Postprocessing")]
		[PropertyTooltip("Weather or not post processing should be performed using jobs or sequentially")] 
		public bool useJobs = true;
		
		// Additional dictionary that contains the raw pose data before it has been processed
		protected readonly Dictionary<string, PostProcessData> rawSlotData = new();
		
		// Native arrays 
		protected NativeArray<byte> slotsNativeArray;
		protected NativeArray<long> startsNativeArray;
		// Handle to the job which so it can be awaited at the end of the frame
		protected JobHandle postProcessJob;
		// When the object goes away cleanup the native arrays
		public void OnDestroy() {
			if (slotsNativeArray.IsCreated) slotsNativeArray.Dispose();
			if (startsNativeArray.IsCreated) startsNativeArray.Dispose();
		}

		// When the object is created "copy" (+ modifications) the pose-slot dictionary from the base class
		public void Awake() {
			foreach (var slot in slots.Keys) 
				rawSlotData[slot] = new PostProcessData(PostProcessData.ProcessMode.Process);
		}
		// In the editor make sure the there is valid data for UI updates
#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			rawSlotData.Clear();
			Awake();
		}
#endif
		
		public override PoseRef GetterPoseRef(string slot) => slots[slot];
		public override PoseRef SetterPoseRef(string slot) => rawSlotData[slot].poseRef;
		
		// Access to the processed and raw pose information
		public virtual ref Pose GetProcessedPose(string slot) => ref GetterPoseRef(slot).pose;
		public virtual ref Pose GetRawPose(string slot) => ref SetterPoseRef(slot).pose;
		
		// Access to data indicating how the data in a given slot should be handled
		public virtual bool ShouldProcess(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Process;
		public virtual bool ShouldCopy(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Copy;
		public virtual bool ShouldIgnore(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Ignore;
		public void SetProcessMode(string slot, PostProcessData.ProcessMode value = PostProcessData.ProcessMode.Process) {
			var data = rawSlotData[slot];
			data.processMode = value;
			rawSlotData[slot] = data;
		}

		// Perform the data processing every frame...
		public void Update() {
			#region Jobs

			if (useJobs) {
				// If the old arrays are dirty
				if (!slotsNativeArray.IsCreated || !startsNativeArray.IsCreated || startsNativeArray.Length != rawSlotData.Count) {
					// Dispose of the old arrays (if they exists)
					if (slotsNativeArray.IsCreated)
						slotsNativeArray.Dispose();
					if (startsNativeArray.IsCreated)
						startsNativeArray.Dispose();

					// Allocate new arrays
					rawSlotData.Keys.Select(s => Encoding.UTF8.GetBytes(s)).ConcatenateWithStartIndices(out var data, out var starts);
					slotsNativeArray = new NativeArray<byte>(data, Allocator.Persistent);
					startsNativeArray = new NativeArray<long>(starts, Allocator.Persistent);
				}

				postProcessJob = new PostProcessJob {
					ownerID = indexInScene,
					deltaTime = Time.deltaTime,
					slotData = slotsNativeArray.AsReadOnly(),
					slotStarts = startsNativeArray.AsReadOnly()
				}.Schedule(rawSlotData.Count, 1); // Job is completed in LateUpdate
			}

			#endregion

			else
				
			#region Serial

			{
				// Process all of the data that should be processed
				foreach (var slot in rawSlotData.Keys.Where(ShouldProcess))
					GetProcessedPose(slot) = OnPostProcess(slot, GetProcessedPose(slot), GetRawPose(slot), Time.deltaTime);

				// Copy all of the data that should be copied
				foreach (var slot in rawSlotData.Keys.Where(ShouldCopy))
					GetProcessedPose(slot) = GetRawPose(slot);
			}

			#endregion
		}

		// After everything else has updated... make sure that our job is finished
		public void LateUpdate() => postProcessJob.Complete();

		// Function that can be overridden in derived classes to process the data in some way
		public abstract Pose OnPostProcess(string slot, Pose processed, Pose raw, float dt);
	}
}