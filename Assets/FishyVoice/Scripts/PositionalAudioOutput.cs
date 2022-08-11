using Adrenak.UniVoice;
using Adrenak.UniVoice.InbuiltImplementations;
using UnityEngine;

namespace FishyVoice {
	public struct PositionalAudioParameters {
		public float spatialBlend;
		public float dopplerLevel;
		public float spread;
		public float minDistance;
		public float maxDistance;
		public AudioRolloffMode rolloffMode;

		public PositionalAudioParameters(bool spatialize, float spatialBlend = 1, float dopplerLevel = 1, float spread = 0, float minDistance = 1, float maxDistance = 500, AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic) {
			this.spatialBlend = spatialize ? spatialBlend : 0;
			this.dopplerLevel = dopplerLevel;
			this.spread = spread;
			this.minDistance = minDistance;
			this.maxDistance = maxDistance;
			this.rolloffMode = rolloffMode;
		}
	}

	// Factory that creates a positional audio output
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