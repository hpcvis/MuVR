namespace FishyVoice.Samples {
	
	/// <summary>
	/// Extension of the audio sample which creates a positional audio sample as opposed to the basic audio sample
	/// </summary>
	public class FishyVoicePositionalAudioSample : FishyVoiceSample {
		protected override void InitAgent() {
			agent?.Dispose();
			agent = voiceNetwork.CreateAgent(new PositionalAudioOutputFactory(10, 5, new PositionalAudioParameters(true, 1, 1, 0, 3)));
		}
	}
}