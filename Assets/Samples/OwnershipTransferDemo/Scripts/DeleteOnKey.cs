using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class DeleteOnKey : NetworkBehaviour  {
    void Update() {
        if (!Input.GetKeyDown(KeyCode.Delete)) return;
        
        if (IsServer) Despawn();
        else DespawnServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnServerRPC() => Despawn();
}
