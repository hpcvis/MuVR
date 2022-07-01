using Mirror;
using UnityEngine;

public class AutoStartClient : MonoBehaviour {
    void Start() {
#if !UNITY_SERVER
        NetworkManager manager = GetComponent<NetworkManager>();
	    manager.StartClient();
#endif        
    }
}
