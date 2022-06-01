
// Class that provides singleton access to the generated XRInputActions class
public class XRInputActions : Generated.XRInputActions {
	// Backing variable
	private static Generated.XRInputActions _actions = null;

	// Singleton access
	public static Generated.XRInputActions instance {
		get {
			if (_actions is null)
				_actions = new Generated.XRInputActions();
			return _actions;
		}
	}
}