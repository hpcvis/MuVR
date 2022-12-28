using UnityEngine;

namespace FishyVoice {
	
	/// <summary>
	/// Component which provides positional information about the location 
	/// </summary>
	public class PlayerAudioPositionReference : MonoBehaviour {
		// Instance management
		public static PlayerAudioPositionReference instance;
		private void OnEnable() => instance = this;
		private void OnDisable() { if (instance == this) instance = null; }
		
		

		[Tooltip("Optional transform used to acquire the player's position.")]
		public new Transform transform = null;
		
		[SerializeField] private Vector3 _position;
		[Tooltip("The position of the player, can be set manually or will by automatically set to the transform's position (if set).")]
		public Vector3 position {
			get => transform?.position ?? _position;
			set => _position = value;
		}
	}
}