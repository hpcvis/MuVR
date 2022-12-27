using uMuVR.Enhanced;
using UnityEngine;

namespace FishyVoice.Samples {
	public class PositionalAudioPlayerPositioner : NetworkBehaviour {
		[SerializeField] private float period = 5;
		[SerializeField] private float amplitude = 7;


		public override void Tick() {
			if(IsServer) ServerTick();
		}

		private void ServerTick() {
			var pos = transform.position;
			pos.x = Mathf.Sin(Time.unscaledTime / period) * amplitude;
			transform.position = pos;
		}
	}
}