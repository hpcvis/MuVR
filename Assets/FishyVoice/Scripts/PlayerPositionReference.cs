using TriInspector;
using UnityEngine;

namespace FishyVoice {
	
	public class PlayerPositionReference : MonoBehaviour {
		// Instance management
		public static PlayerPositionReference instance;
		private void OnEnable() => instance = this;
		private void OnDisable() { if (instance == this) instance = null; }
		
		

		[PropertyTooltip("Optional transform used to acquire the player's position.")]
		public new Transform transform = null;
		
		[SerializeField] private Vector3 _position;
		[PropertyTooltip("The position of the player, can be set manually or will by automatically set to the transform's position (if set).")]
		public Vector3 position {
			get => transform?.position ?? _position;
			set => _position = value;
		}
	}
}