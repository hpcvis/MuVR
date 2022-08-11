using TMPro;
using UnityEngine;

namespace MuVR.Utility {
	// FPS Counter, tweaked from the version in Unity's standard assets
	[RequireComponent(typeof(TMP_Text))]
	public class FPSCounter : MonoBehaviour {
		private const float FPSMeasurePeriod = 0.5f;
		private const string Display = "{0} FPS";
		private int fpsAccumulator;
		private float fpsNextPeriod;
		private int currentFps;
		private TMP_Text text;
		
		private void Awake() {
			fpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
			text = GetComponent<TMP_Text>();
		}
		
		private void Update() {
			// measure average frames per second
			fpsAccumulator++;
			
			if (!(Time.realtimeSinceStartup > fpsNextPeriod)) return;
			currentFps = (int)(fpsAccumulator / FPSMeasurePeriod);
			fpsAccumulator = 0;
			fpsNextPeriod += FPSMeasurePeriod;
			text.text = string.Format(Display, currentFps);
		}
	}
}