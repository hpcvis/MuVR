using System.Collections.Generic;
using System.Linq;
using System.Text;
using uMuVR.Enhanced;
using TriInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace uMuVR {
	/// <summary>
	/// Base class providing boilerplate code for UserAvatar extensions that need the data post processed
	/// </summary>
	public abstract class UserAvatarPostProcessed : UserAvatar {
		
		#region Static Reference Management

		/// <summary>
		/// List of all UserAvatars currently in the scene
		/// </summary>
		protected static UserAvatarPostProcessed[] inScene;
		/// <summary>
		/// Index of this UserAvatar within the scene
		/// </summary>
		protected uint indexInScene;

		/// <summary>
		/// On dis/enable add/remove us from the list of syncs in the scene
		/// </summary>
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
		
		/// <summary>
		/// Pose slot data extension
		/// </summary>
		public struct PostProcessData {
			/// <summary>
			/// Enum indicating how we should treat the data in each PoseSlot
			/// </summary>
			public enum ProcessMode {
				Process,	// Fully process the data
				Copy,		// Just copy the data from input to storage
				Ignore,		// Do nothing
			}
			/// <summary>
			/// The process mode of this PoseSlot
			/// </summary>
			public ProcessMode processMode;
			/// <summary>
			/// Pose data for this PoseSlot
			/// </summary>
			public readonly PoseRef poseRef;
			
			public PostProcessData(ProcessMode processMode) {
				this.processMode = processMode;
				poseRef = new PoseRef();
			}
		}
		
		/// <summary>
		/// Unity job that applies post processing
		/// </summary>
		protected struct PostProcessJob : IJobParallelFor {
			/// <summary>
			/// The index of the UserAvatar in the static references
			/// </summary>
			public uint ownerID;
			/// <summary>
			/// Elapsed time
			/// </summary>
			public float deltaTime;
			
			/// <summary>
			/// Names of the slots which need to be processed
			/// </summary>
			public NativeArray<byte>.ReadOnly slotData;
			/// <summary>
			/// Index in the above array where every string starts
			/// </summary>
			public NativeArray<long>.ReadOnly slotStarts;

			/// <summary>
			/// Function run (potentially) in parallel that processes this item
			/// </summary>
			/// <param name="index">Index within the for loop of this execution</param>
			public void Execute(int index) {
				// Calculate the start and end point of the slot's name string
				var start = (int)slotStarts[index];
				var end = (int)(index < slotStarts.Length - 1 ? slotStarts[index + 1] : slotData.Length);
				
				// Extract slot's name from the buffer
				var slot = Encoding.UTF8.GetString(slotData.Skip(start).Take(end - start).ToArray());
				// Get a reference to the owning UserAvatar
				var owner = UserAvatarPostProcessed.inScene[ownerID];
				
				// If the owner is fully processing this slot, process the data
				if(owner.ShouldProcess(slot))
					owner.GetProcessedPose(slot) = owner.OnPostProcess(slot, owner.GetProcessedPose(slot), owner.GetRawPose(slot), deltaTime);
				// If the owner is simply copying data, copy data
				else if(owner.ShouldCopy(slot))
					owner.GetProcessedPose(slot) = owner.GetRawPose(slot);
			}
		} 

		#endregion

		[Title("Postprocessing")]
		[PropertyTooltip("Weather or not post processing should be performed using jobs or sequentially")] 
		public bool useJobs = true;
		
		/// <summary>
		/// Additional dictionary that contains the raw pose data before it has been processed
		/// </summary>
		protected readonly Dictionary<string, PostProcessData> rawSlotData = new();
		
		/// <summary>
		/// Slot name native array (for use with the jobs system)
		/// </summary>
		/// <remarks>Unused if not operating in jobs mode</remarks>
		protected NativeArray<byte> slotsNativeArray;
		/// <summary>
		/// Slot name start native array (for use with the jobs system)
		/// </summary>
		/// <remarks>Unused if not operating in jobs mode</remarks>
		protected NativeArray<long> startsNativeArray;
		/// <summary>
		/// Handle to the job which so it can be awaited at the end of the frame
		/// </summary>
		/// <remarks>Unused if not operating in jobs mode</remarks>
		protected JobHandle postProcessJob;
		/// <summary>
		/// When the object goes away cleanup the native arrays
		/// </summary>
		/// <remarks>Unused if not operating in jobs mode</remarks>
		public void OnDestroy() {
			if (slotsNativeArray.IsCreated) slotsNativeArray.Dispose();
			if (startsNativeArray.IsCreated) startsNativeArray.Dispose();
		}
		
		/// <summary>
		/// When the object is created "copy" (+ modifications) the pose-slot dictionary from the base class
		/// </summary>
		public void Awake() {
			foreach (var slot in slots.Keys) 
				rawSlotData[slot] = new PostProcessData(PostProcessData.ProcessMode.Process);
		}
		
		
#if UNITY_EDITOR
		/// <summary>
		/// In the editor make sure the there is valid data for UI updates
		/// </summary>
		protected override void OnValidate() {
			base.OnValidate();
			rawSlotData.Clear();
			Awake();
		}
#endif
		
		/// <summary>
		/// Reference to a pose slot for reading purposes (from the processed dictionary)
		/// </summary>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be read</returns>
		public override PoseRef GetterPoseRef(string slot) => slots[slot];
		/// <summary>
		/// Reference to a pose slot for storage purposes (from the unprocessed dictionary)
		/// </summary>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be stored</returns>
		public override PoseRef SetterPoseRef(string slot) => rawSlotData[slot].poseRef;
		
		/// <summary>
		/// Reference to a pose within a pose slot for reading purposes (from the processed dictionary)
		/// </summary>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be read</returns>
		public virtual ref Pose GetProcessedPose(string slot) => ref GetterPoseRef(slot).pose;
		/// <summary>
		/// Reference to a pose within a pose slot for storage purposes (from the unprocessed dictionary)
		/// </summary>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be stored</returns>
		public virtual ref Pose GetRawPose(string slot) => ref SetterPoseRef(slot).pose;
		
		/// <summary>
		/// Determines if the given slot should be processed
		/// </summary>
		/// <param name="slot">Name of the slot in question</param>
		/// <returns>True if it should be processed, false otherwise</returns>
		public virtual bool ShouldProcess(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Process;
		/// <summary>
		/// Determines if the given slot should be simply be copied
		/// </summary>
		/// <param name="slot">Name of the slot in question</param>
		/// <returns>True if it should be copied, false otherwise</returns>
		public virtual bool ShouldCopy(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Copy;
		/// <summary>
		/// Determines if the given slot should be ignored
		/// </summary>
		/// <param name="slot">Name of the slot in question</param>
		/// <returns>True if it should be ignored, false otherwise</returns>
		public virtual bool ShouldIgnore(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Ignore;
		
		/// <summary>
		/// Sets the process mode for a given slot
		/// </summary>
		/// <param name="slot">The slot to set the mode for</param>
		/// <param name="value">Mode to set (defaults to Process)</param>
		public void SetProcessMode(string slot, PostProcessData.ProcessMode value = PostProcessData.ProcessMode.Process) {
			var data = rawSlotData[slot];
			data.processMode = value;
			rawSlotData[slot] = data;
		}


		/// <summary>
		/// Perform the data processing every frame...
		/// </summary>
		public void Update() {
			#region Jobs

			// If we are using Unity Jobs...
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

				// Schedule the post processing
				postProcessJob = new PostProcessJob {
					ownerID = indexInScene,
					deltaTime = Time.deltaTime,
					slotData = slotsNativeArray.AsReadOnly(),
					slotStarts = startsNativeArray.AsReadOnly()
				}.Schedule(rawSlotData.Count, 1); // Job completion is awaited in LateUpdate
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
		
		/// <summary>
		/// At the end of the frame... make sure that our job is finished
		/// </summary>
		public void LateUpdate() => postProcessJob.Complete();
		
		/// <summary>
		/// Callback function that can be overridden in derived classes to process the data in some way
		/// </summary>
		/// <param name="slot">The slot being processed</param>
		/// <param name="processed">The current processed data associated with the slot</param>
		/// <param name="raw">Incoming raw data that needs to be integrated</param>
		/// <param name="dt">Elapsed time since the pose was last processed</param>
		/// <returns>The pose which should be stored in the dictionary</returns>
		public abstract Pose OnPostProcess(string slot, Pose processed, Pose raw, float dt);
	}
}