using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using Gma.DataStructures;

// Component that represents a volume of ownership
// NOTE: The system is not designed around overlapping ownership volumes, try to prevent this scenario if possible!
[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
public class OwnershipVolume : EnchancedNetworkBehaviour {

    // "Stack" of unique players who are currently within the volume
    // NOTE: Ordered set selected so that the stack ordering is preserved, while still only allowing unique network connections in the list
    private readonly OrderedSet<NetworkConnection> potentialOwners = new();
    // Set of OwnershipManagers that are currently within the volume (and should thus be notified of ownership changes)
    private readonly HashSet<OwnershipManager> containedOwnershipManagers = new();

    // The connection this volume currently considers to be its owner 
    [SyncVar(OnChange = nameof(OnVolumeOwnerChanged))] public NetworkConnection volumeOwner = null;
    [SerializeField, ReadOnly] private int volumeOwnerDebug = -2; // Inspector display of the current volume owner (-2 = unset, -1 = scene)

    public enum OwnershipMode {
        Manual,
        LocalPlayer, // Sets the owner to the player who spawned this volume (only works for non-scene objects.)
        NewestPlayer, // Sets the owner to the last player who touched the volume
        OldestPlayer // Sets the owner to the first player who touched the volume
    }

    [SerializeField] private OwnershipMode mode;

    // If we are in LocalPlayer mode, assign the player who spawned this object as the volumeOwner
    public override void OnStartClient(){
        base.OnStartClient();
        if (mode != OwnershipMode.LocalPlayer) return;

        UpdateOwnerServerRpc(Owner);
    }

    // Un/Register the event listen which removes dead connections from the list of potential owners
    public override void OnStartServer() {
        base.OnStartServer();
        ServerManager.Objects.OnPreDestroyClientObjects += OnPreDestroyClientObjects;
    }
    public override void OnStopServer() {
        base.OnStopServer();
        ServerManager.Objects.OnPreDestroyClientObjects -= OnPreDestroyClientObjects;
    }
    
    // When a client leaves the game, remove them from the list of potential owners
    public void OnPreDestroyClientObjects(NetworkConnection leaving) {
        potentialOwners.Remove(leaving);
        UpdateOwner(GetFirstPotentialOwner());
    }

    // When another object overlaps with us, update volumeOwner
    private void OnTriggerEnter(Collider other) {
        if(IsServer) OnTriggerEnterServer(other.gameObject);
        else OnTriggerEnterServerRpc(other.gameObject);
    }
    [Server] private void OnTriggerEnterServer(GameObject other) {
        if (other is null) return;
        
        // Objects that we assign ownership to don't control who owns the volume
        var m = other.GetComponentInParent<OwnershipManager>();
        if (m is not null) return;
        
        var no = other.GetComponentInParent<NetworkObject>();
        if (no is null) return;
        // If the object doesn't have an owner, then we can't update our owner to match...
        if (no.Owner.ClientId < 0) return;


        switch (mode) {
        // In oldest player mode, add the interacting player to the back of the potential player list
        case OwnershipMode.OldestPlayer:
            potentialOwners.Add(no.Owner); 
        // In newest player mode, add the interacting player to the front of the potential player list
        break; case OwnershipMode.NewestPlayer: {
            var oldOwners = new List<NetworkConnection>(potentialOwners);
            potentialOwners.Clear();
            potentialOwners.Add(no.Owner);
            foreach (var owner in oldOwners)
                potentialOwners.Add(owner);
        }
        // In any other mode don't update ownership
        break; default:
            return;
        }

        // Update the volumeOwner to reference the front of the list
        if(potentialOwners.Count > 0) UpdateOwner(GetFirstPotentialOwner());
    }
    [ServerRpc(RequireOwnership = false)]
    private void OnTriggerEnterServerRpc(GameObject other) => OnTriggerEnterServer(other);

    // When another object stops overlapping with us, update volumeOwner
    private void OnTriggerExit(Collider other) {
        if(IsServer) OnTriggerExitServer(other.gameObject);
        else OnTriggerExitServerRpc(other.gameObject);
    }
    [Server] private void OnTriggerExitServer(GameObject other) {
        if (other is null) return;
        
        // Objects that we assign ownership to don't control who owns the volume
        var m = other.GetComponentInParent<OwnershipManager>();
        if (m is not null) return;
        
        var no = other.GetComponentInParent<NetworkObject>();
        if (no is null) return;
        // Only update ownership if we are in oldest or newest player mode
        if ( mode is not (OwnershipMode.OldestPlayer or OwnershipMode.NewestPlayer) ) return;

        // Remove the owner that is no longer overlapping from the list of potential owners
        potentialOwners.Remove(no.Owner);
        UpdateOwner(GetFirstPotentialOwner());
    }
    [ServerRpc(RequireOwnership = false)]
    private void OnTriggerExitServerRpc(GameObject other) => OnTriggerEnterServer(other);

    // On validate gives warnings if settings on connected components aren't properly set
    protected override void OnValidate() {
        base.OnValidate();
        
        var rigidbody = GetComponent<Rigidbody>();
        var collider = GetComponent<Collider>();
        
        if(!rigidbody.isKinematic)
            Debug.LogWarning("If the rigidbody isn't kinematic the volume might move around! And then everyone will be very confused!");
        
        if(!collider.isTrigger)
            Debug.LogError("The collider must be a trigger or the volume will not function correctly.");
    }

    // Function that updates the debug display when the volume's owner changes
    private void OnVolumeOwnerChanged(NetworkConnection prev, NetworkConnection @new, bool asServer){
        volumeOwnerDebug = volumeOwner?.ClientId ?? -1;
    }
    
    // Server only function called by an OwnershipManager to register it as listening for changes in ownership
    public void RegisterAsListener(OwnershipManager m) {
        if(IsServer) RegisterAsListenerServer(m);
        else RegisterAsListenerServerRPC(m.gameObject);
    }
    [Server] private void RegisterAsListenerServer(OwnershipManager m) {
        if (m is null) return;
        containedOwnershipManagers.Add(m);
    }
    [ServerRpc(RequireOwnership = false)]
    private void RegisterAsListenerServerRPC(GameObject m) => RegisterAsListenerServer(m.GetComponent<OwnershipManager>());
    
    // Server only function called by an OwnershipManager to indicate it is no longer interested in ownership changes
    public void UnregisterAsListener(OwnershipManager m) {
        if(IsServer) UnregisterAsListenerServer(m);
        else UnregisterAsListenerServerRPC(m.gameObject);
    }
    [Server] private void UnregisterAsListenerServer(OwnershipManager m) {
        if (m is null) return;
        containedOwnershipManagers.Remove(m);
    }
    [ServerRpc(RequireOwnership = false)]
    private void UnregisterAsListenerServerRPC(GameObject m) => UnregisterAsListenerServer(m.GetComponent<OwnershipManager>());



    // Server only function that updates the current owner 
    [Server] protected void UpdateOwner(NetworkConnection newOwner) {
        volumeOwner = newOwner;

        // Notify all of the contained OwnershipManagers that the owner has changed
        foreach(var m in containedOwnershipManagers)
            m.GiveOwnership(volumeOwner);
    }

    // RPC that tells the server to update the current owner
    [ServerRpc]
    protected void UpdateOwnerServerRpc(NetworkConnection client) => UpdateOwner(client);


    // Helper function that extracts the first connection from the set of potential owners
    private NetworkConnection GetFirstPotentialOwner() {
        NetworkConnection ret = null;
        foreach (var owner in potentialOwners) {
            if (ret is not null) break;
            ret = owner;
        }
        return ret;
    }
}
