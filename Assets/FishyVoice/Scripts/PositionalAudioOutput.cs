using Adrenak.UniVoice;
using Adrenak.UniVoice.InbuiltImplementations;
using UnityEngine;

namespace FishyVoice {
	/// <summary>
	/// Parameters which control the positional audio output
	/// </summary>
	/// <remarks>These all just configure the Unity audio source associated with the player</remarks>
	public struct PositionalAudioParameters {
		/// <summary>
		/// Sets how much the associated AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.
		/// </summary>
		public float spatialBlend;
		/// <summary>
		/// Sets the Doppler scale for the associated AudioSource.
		/// </summary>
		public float dopplerLevel;
		/// <summary>
		/// Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space.
		/// </summary>
		public float spread;
		/// <summary>
		/// Within the Min distance the associated AudioSource will cease to grow louder in volume.
		/// </summary>
		public float minDistance;
		/// <summary>
		/// (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
		/// </summary>
		public float maxDistance;
		/// <summary>
		/// Sets how the associated AudioSource attenuates over distance.
		/// </summary>
		public AudioRolloffMode rolloffMode;

		public PositionalAudioParameters(bool spatialize, float spatialBlend = 1, float dopplerLevel = 1, float spread = 0, float minDistance = 1, float maxDistance = 500, AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic) {
			this.spatialBlend = spatialize ? spatialBlend : 0;
			this.dopplerLevel = dopplerLevel;
			this.spread = spread;
			this.minDistance = minDistance;
			this.maxDistance = maxDistance;
			this.rolloffMode = rolloffMode;
		}
		public PositionalAudioParameters(float spatialBlend, float dopplerLevel = 1, float spread = 0, float minDistance = 1, float maxDistance = 500, AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic) {
			this.spatialBlend = spatialBlend;
			this.dopplerLevel = dopplerLevel;
			this.spread = spread;
			this.minDistance = minDistance;
			this.maxDistance = maxDistance;
			this.rolloffMode = rolloffMode;
		}
	}
	
	/// <summary>
	/// Factory that creates a positional audio output
	/// </summary>
	public class PositionalAudioOutputFactory : IAudioOutputFactory {

		public int BufferSegCount { get; protected set; }
		public int MinSegCount { get; protected set; }
		public PositionalAudioParameters Parameters { get; protected set; } = default;

		public PositionalAudioOutputFactory() : this(10, 5) { }

		public PositionalAudioOutputFactory(int bufferSegCount, int minSegCount, PositionalAudioParameters? parameters = null) {
			BufferSegCount = bufferSegCount;
			MinSegCount = minSegCount;
			Parameters = parameters ?? new PositionalAudioParameters(true);
		}

		public IAudioOutput Create(int samplingRate, int channelCount, int segmentLength) {
			var source = new GameObject($"UniVoice Peer").AddComponent<AudioSource>();
			source.spatialize = true;
			source.spatialBlend = Parameters.spatialBlend;
			source.dopplerLevel = Parameters.dopplerLevel;
			source.spread = Parameters.spread;
			source.minDistance = Parameters.minDistance;
			source.maxDistance = Parameters.maxDistance;
			source.rolloffMode = Parameters.rolloffMode;
			
			return InbuiltAudioOutput.New(
				new InbuiltAudioBuffer(
					samplingRate, channelCount, segmentLength, BufferSegCount
				),
				source,
				MinSegCount
			);
		}
	}
}