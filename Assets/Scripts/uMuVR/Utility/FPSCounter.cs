using TMPro;
using UnityEngine;

namespace uMuVR.Utility {
	/// <summary>
	/// Component which calculates the game's FPS
	/// </summary>
	/// <remarks>FPS Counter, tweaked from the version in Unity's standard assets</remarks>
	[RequireComponent(typeof(TMP_Text))]
	public class FPSCounter : MonoBehaviour {
		/// <summary>
		/// How often FPS is sampled in seconds
		/// </summary>
		private const float FPSMeasurePeriod = 0.5f;
		/// <summary>
		/// Counter tracking how many frames have elapsed in the measurement period
		/// </summary>
		private int fpsAccumulator;
		/// <summary>
		/// Timestamp when the next measurement will be taken
		/// </summary>
		private float fpsNextPeriod;
		/// <summary>
		/// Variable tracking the current FPS
		/// </summary>
		public int currentFps { private set; get; }
		/// <summary>
		/// Reference to the text component
		/// </summary>
		private TMP_Text text;
		
		/// <summary>
		/// When the game starts link up the component with the text and find the first measurement period
		/// </summary>
		private void Awake() {
			fpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
			text = GetComponent<TMP_Text>();
		}
		
		/// <summary>
		/// Every frame update the accumulator and if we are past the measurement period update the displayed framerate
		/// </summary>
		private void Update() {
			// measure average frames per second
			fpsAccumulator++;
			
			if (!(Time.realtimeSinceStartup > fpsNextPeriod)) return;
			currentFps = (int)(fpsAccumulator / FPSMeasurePeriod);
			fpsAccumulator = 0;
			fpsNextPeriod += FPSMeasurePeriod;
			text.text = $"{currentFps} FPS";
		}
	}
}