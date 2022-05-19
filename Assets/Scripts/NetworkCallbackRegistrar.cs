using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Component that allows network callbacks to be registered from within the Unity editor, takes a list of objects to scan for callback handlers
[RequireComponent(typeof(NetworkRunner))]
public class NetworkCallbackRegistrar : MonoBehaviour {
    [Tooltip("List of GameObjects that should be scanned for callback listeners"), TypeConstraint(typeof(INetworkRunnerCallbacks))]
    public GameObject[] networkRunnerCallbacks;
    
    // Reference to the NetworkRunner that will listen to callbacks (automatically set)
    [SerializeField, ReadOnly] private NetworkRunner runner;

    public void Awake() {
        // Find the NetworkRunner on the same object
        runner = GetComponent<NetworkRunner>();

        // Register all of the NetworkRunnerCallbacks on each of the objects in the network runner list
        foreach (var callbackObject in networkRunnerCallbacks)
            foreach (var callback in callbackObject.GetComponents<INetworkRunnerCallbacks>())
                runner.AddCallbacks(callback);
    }
}
