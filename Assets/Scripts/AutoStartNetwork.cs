using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AutoStartNetwork : MonoBehaviour
{
	private void Awake() {
#if UNITY_SERVER
		GetComponent<NetworkDebugStart>().StartServer();
#else
		GetComponent<NetworkDebugStart>().StartClient();
#endif
	}
}
