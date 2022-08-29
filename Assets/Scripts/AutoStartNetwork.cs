using UnityEngine;

public class AutoStartNetwork : MonoBehaviour
{
	private void Awake() {
		GetComponent<NetworkDebugStart>().StartSharedClient();
	}
}
