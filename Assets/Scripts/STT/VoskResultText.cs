using UnityEngine;
using UnityEngine.UI;

public class VoskResultText : MonoBehaviour {
	public VoskSpeechToText VoskSpeechToText;
	public Text ResultText;

	private void Awake() {
		VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
	}

	private void OnTranscriptionResult(string obj) {
		var result = new RecognitionResult(obj);

		ResultText.text += result.Phrases[0].Text + " [";
		for (var i = 0; i < result.Phrases.Length; i++) {
			if (i > 0) ResultText.text += ", ";

			ResultText.text += result.Phrases[i].Text;
		}

		ResultText.text += "]\n";
	}
}