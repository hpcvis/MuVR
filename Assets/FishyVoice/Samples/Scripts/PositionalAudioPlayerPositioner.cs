using uMuVR.Enhanced;
using UnityEngine;

namespace FishyVoice.Samples {
	/// <summary>
	/// Component which moves the "player" around in a rhythmic pattern so that the positional nature of its audio can be demonstrated
	/// </summary>
	public class PositionalAudioPlayerPositioner : NetworkBehaviour {
		/// <summary>
		/// How long in seconds it should take for the "player" to complete one cycle
		/// </summary>
		[SerializeField] private float period = 5;
		/// <summary>
		/// The furthest distance the "player" will travel doing one second
		/// </summary>
		[SerializeField] private float amplitude = 7;
		
		/// <summary>
		/// Every tick, move the "player" if we are the server
		/// </summary>
		public override void Tick() {
			if(IsServer) ServerTick();
		}

		/// <summary>
		/// Every tick, move the "player" if we are the server
		/// </summary>
		private void ServerTick() {
			var pos = transform.position;
			pos.x = Mathf.Sin(Time.unscaledTime / period) * amplitude;
			transform.position = pos;
		}
	}
}