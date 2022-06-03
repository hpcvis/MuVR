using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using Gma.DataStructures;
using Unity.VisualScripting;

// Component that represents a volume of ownership
[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
public class OwnershipVolume : EnchancedNetworkBehaviour {

    // "Stack" of unique players who are currently within the volume
    private readonly OrderedSet<NetworkConnection> potentialOwners = new();

    // The connection this volume currently considers to be its owner 
    // TODO: Maintain a list of ownership managers that are within this volume and notify them when the volume owner changes
    [SyncVar] public NetworkConnection volumeOwner;
    [SerializeField, ReadOnly] private int volumeOwnerDebug = -1;

    public enum OwnershipMode {
        Manual,
        LocalPlayer, // Sets the owner to the player who spawned this volume (only works for non-scene objects.)
        NewestPlayer, // Sets the owner to the last player who touched the volume
        OldestPlayer // Sets the owner to the first player who touched the volume
    }

    [SerializeField] private OwnershipMode mode;

    // If we are in LocalPlayer mode, assign the player who spawned this object as the volumeOwner
    public override void OnStartClient() {
        base.OnStartClient();
        if (mode != OwnershipMode.LocalPlayer) return;

        UpdateOwnerServerRpc(Owner);
    }

    // If we are in LocalPlayer mode, update the volumeOwner when ownership of this object changes
    public override void OnOwnershipServer(NetworkConnection _) {
        base.OnOwnershipServer(_);
        if (mode != OwnershipMode.LocalPlayer) return;

        UpdateOwner(Owner);
    }
    
    // When another object overlaps with us, update volumeOwner
    [Server]
    private void OnTriggerEnter(Collider other) {
        var no = other.GetComponentInParent<NetworkObject>();
        if (no is null) return;

        // In oldest player mode, add the interacting player to the back of the potential player list
        if (mode == OwnershipMode.OldestPlayer)
            potentialOwners.Add(no.Owner);
        // In newest player mode, add the interacting player to the front of the potential player list
        else if (mode == OwnershipMode.NewestPlayer) {
            var oldOwners = new List<NetworkConnection>(potentialOwners);
            potentialOwners.Clear();
            potentialOwners.Add(no.Owner);
            foreach (var owner in oldOwners)
                potentialOwners.Add(owner);
        // In any other mode don't update ownership
        } else
            return;

        // Update the volumeOwner to reference the front of the list
        UpdateOwner(potentialOwners.GetEnumerator().Current);
    }

    // When another object stops overlapping with us, update volumeOwner
    [Server]
    private void OnTriggerExit(Collider other) {
        var no = other.GetComponent<NetworkObject>();
        if (no is null) return;
        // Only update ownership if we are in oldest or newest player mode
        if ( !(mode == OwnershipMode.OldestPlayer || mode == OwnershipMode.NewestPlayer) ) return;

        // Remove the owner that is no longer overlapping from the list of potential owners
        potentialOwners.Remove(no.Owner);
        UpdateOwner(potentialOwners.GetEnumerator().Current);
    }

    // On validate gives warnings if settings on connected components aren't properly set
    protected override void OnValidate() {
        base.OnValidate();
        
        var rigidbody = GetComponent<Rigidbody>();
        var collider = GetComponent<Collider>();
        
        if(!rigidbody.isKinematic)
            Debug.LogWarning("If the rigidbody isn't kinematic the volume might move around! And then everyone will be very confused!");
        
        if(!collider.isTrigger)
            Debug.LogError("The collider must be a trigger or the " + nameof(OwnershipVolume) + " will not function correctly.");
    }
    
    // Server only function that updates the current owner 
    [Server]
    void UpdateOwner(NetworkConnection newOwner) {
        volumeOwner = newOwner;
        volumeOwnerDebug = volumeOwner.ClientId;
        Debug.Log("Owner: " + volumeOwner.ClientId);
    }
    
    // RPC that tells the server to update the current owner
    [ServerRpc]
    void UpdateOwnerServerRpc(NetworkConnection client) {
        UpdateOwner(client);
    }
}
