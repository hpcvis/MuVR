using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class DeleteOnKey : NetworkBehaviour  {
    void Update() {
        if (!Input.GetKeyDown(KeyCode.Delete)) return;
        
        if (IsServer) Destroy(gameObject);
        else DestroyServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyServerRPC() => Destroy(gameObject);
}
