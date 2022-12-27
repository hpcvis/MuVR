using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

namespace uMuVR.Utility {
	/// <summary>
	/// Script used to log console messages to the screen and a file in a build of the game.
	/// </summary>
	public class PrintConsole : MonoBehaviour {
		/// <summary>
		/// String representing the current log
		/// </summary>
		private string myLog = "*begin log";
		/// <summary>
		/// Name of the log file
		/// </summary>
		private string filename = "";
		/// <summary>
		/// Maximum length of the log (in characters)
		/// </summary>
		private readonly int kChars = 900;

		/// <summary>
		/// Variable which indicates if the log is visible or not
		/// </summary>
		[SerializeField] private bool doShow;

		/// <summary>
		/// On dis/enable subscribe to log events
		/// </summary>
		private void OnEnable() {
			Application.logMessageReceived += Log;
		}
		private void OnDisable() {
			Application.logMessageReceived -= Log;
		}

		/// <summary>
		/// Every frame check if we should toggle display of the log!
		/// </summary>
		private void Update() {
			if (Input.GetKeyDown(KeyCode.BackQuote))
				doShow = !doShow;
		}

		/// <summary>
		/// Callback called when a new message is added to the log
		/// </summary>
		/// <param name="logString">The string to add to the log</param>
		/// <param name="stackTrace">Path back to where the issue occurred</param>
		/// <param name="type">The type of log message</param>
		public void Log(string logString, string stackTrace, LogType type) {
			// for onscreen...
			myLog = myLog + "\n" + logString;
			if (myLog.Length > kChars) myLog = myLog[^kChars..];

			// for the file ...
			if (filename == "") {
				var d = Environment.GetFolderPath(
					Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
				Directory.CreateDirectory(d);
				var r = Random.Range(1000, 9999).ToString();
				filename = d + "/log-" + r + ".txt";
			}

			try {
				File.AppendAllText(filename, logString + "\n");
			} catch {
				// ignored
			}
		}

		/// <summary>
		/// When GUIs are rendered, display the console on screen
		/// </summary>
		private void OnGUI() {
			if (!doShow) return;
			GUI.matrix = Matrix4x4.TRS(
				Vector3.zero,
				Quaternion.identity,
				new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
			GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
		}
	}
}