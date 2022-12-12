

namespace MuVR.Utility {
	/// <summary>
	/// Base class that all of the different types of sync classes inherit from
	/// Used by the User Avatar to find and dis/enable all of the syncs associated with an avatar
	/// </summary>
	public interface ISyncable { public bool enabled { set; get; }}
}