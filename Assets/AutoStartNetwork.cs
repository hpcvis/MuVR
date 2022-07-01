using Unity.Netcode;
using UnityEngine;

public class AutoStartNetwork : MonoBehaviour {
	private void Start() {
#if UNITY_SERVER
        NetworkManager.Singleton.StartServer();
#else
		NetworkManager.Singleton.StartClient();
#endif
	}
}