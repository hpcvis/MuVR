
namespace uMuVR.Utility {
	/// <summary>
	/// Base class that all of the different types of sync classes inherit from
	/// Used by the User Avatar to find and dis/enable all of the syncs associated with an avatar
	/// </summary>
	public interface ISyncable {
		/// <summary>
		/// Enum setting weather we should be sending our transform to the pose, or reading our transform from the pose
		/// </summary>
		public enum SyncMode {
			Store,
			SyncTo = Store,
			
			Load,
			SyncFrom = Load,
		}
		
		public bool enabled { set; get; }
	}
}