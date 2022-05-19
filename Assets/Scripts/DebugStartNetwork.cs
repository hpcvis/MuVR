using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugStartNetwork : MonoBehaviour {

	// Reference to the NetworkRunner
	[SerializeField] private NetworkRunner runner;
	
	// Start is called before the first frame update
	private void Start() {
		StartGame(GameMode.AutoHostOrClient);	
	}


	// TODO: Does it make any sense for this function to be located here?
	async void StartGame(GameMode mode)
	{
		// Create the Fusion runner and let it know that we will be providing user input
		runner.ProvideInput = true;

		// Start or join (depends on gamemode) a session with a specific name
		await runner.StartGame(new StartGameArgs() {
			GameMode = mode,
			SessionName = "TestRoom",
			Scene = SceneManager.GetActiveScene().buildIndex,
			SceneObjectProvider = gameObject.AddComponent<NetworkSceneManagerDefault>()
		});
	}
}