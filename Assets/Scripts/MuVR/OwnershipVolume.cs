using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using Gma.DataStructures;
using TriInspector;

namespace MuVR {
    /// <summary>
    /// Component that represents a volume of ownership
    /// </summary>
    /// <remarks>NOTE: The system is not designed around overlapping ownership volumes, try to prevent this scenario if possible!</remarks>
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
    public class OwnershipVolume : MuVR.Enhanced.NetworkBehaviour {
        /// <summary>
        /// "Stack" of unique users who are currently within the volume
        /// NOTE: Ordered set selected so that the stack ordering is preserved, while still only allowing unique network connections in the list
        /// </summary>
        private readonly OrderedSet<NetworkConnection> potentialOwners = new();
        
        /// <summary>
        /// Set of OwnershipManagers that are currently within the volume (and should thus be notified of ownership changes)
        /// </summary>
        private readonly HashSet<OwnershipManager> containedOwnershipManagers = new();
        
        /// <summary>
        /// The connection this volume currently considers to be its owner 
        /// </summary>
        [SyncVar(OnChange = nameof(OnVolumeOwnerChanged))]
        public NetworkConnection volumeOwner = null;
        
        /// <summary>
        /// Variable which displays who the current owner of the volume is
        /// </summary>
        [SerializeField, ReadOnly]
        private int volumeOwnerDebug = -2; // Inspector display of the current volume owner (-2 = unset, -1 = scene)

        /// <summary>
        /// Set of valid ownership modes
        /// </summary>
        public enum OwnershipMode {
            Manual,
            LocalUser, // Sets the owner to the user who spawned this volume (only works for non-scene objects.)
            NewestUser, // Sets the owner to the last user who touched the volume
            OldestUser // Sets the owner to the first user who touched the volume
        }
        /// <summary>
        /// The ownership mode utilized by the device
        /// </summary>
        [SerializeField] private OwnershipMode mode;
        
        /// <summary>
        /// If we are in LocalUser mode, assign the user who spawned this object as the volumeOwner
        /// </summary>
        public override void OnStartClient() {
            base.OnStartClient();
            if (mode != OwnershipMode.LocalUser) return;

            UpdateOwnerServerRpc(Owner);
        }

        
        /// <summary>
        /// Un/Register the event listen which removes dead connections from the list of potential owners
        /// </summary>
        public override void OnStartServer() {
            base.OnStartServer();
            ServerManager.Objects.OnPreDestroyClientObjects += OnPreDestroyClientObjects;
        }
        public override void OnStopServer() {
            base.OnStopServer();
            ServerManager.Objects.OnPreDestroyClientObjects -= OnPreDestroyClientObjects;
        }
        
        /// <summary>
        /// When a client leaves the game, remove them from the list of potential owners
        /// </summary>
        public void OnPreDestroyClientObjects(NetworkConnection leaving) {
            potentialOwners.Remove(leaving);
            UpdateOwner(GetFirstPotentialOwner());
        }
        
        /// <summary>
        /// When another object overlaps with us, update volumeOwner
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            if (IsServer) OnTriggerEnterServer(other.gameObject);
            else OnTriggerEnterServerRpc(other.gameObject);
        }
        [Server]
        private void OnTriggerEnterServer(GameObject other) {
            if (other is null) return;

            // Objects that we assign ownership to don't control who owns the volume
            var m = other.GetComponentInParent<OwnershipManager>();
            if (m is not null) return;

            var no = other.GetComponentInParent<NetworkObject>();
            if (no is null) return;
            // If the object doesn't have an owner, then we can't update our owner to match...
            if (no.Owner.ClientId < 0) return;
            
            
            switch (mode) {
                // In oldest user mode, add the interacting user to the back of the potential user list
                case OwnershipMode.OldestUser:
                    potentialOwners.Add(no.Owner);
                    // In newest user mode, add the interacting user to the front of the potential user list
                break; case OwnershipMode.NewestUser: {
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
            if (potentialOwners.Count > 0) UpdateOwner(GetFirstPotentialOwner());
        }
        [ServerRpc(RequireOwnership = false)]
        private void OnTriggerEnterServerRpc(GameObject other) => OnTriggerEnterServer(other);
        
        /// <summary>
        /// When another object stops overlapping with us, update volumeOwner
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other) {
            if (IsServer) OnTriggerExitServer(other.gameObject);
            else OnTriggerExitServerRpc(other.gameObject);
        }
        [Server]
        private void OnTriggerExitServer(GameObject other) {
            if (other is null) return;

            // Objects that we assign ownership to don't control who owns the volume
            var m = other.GetComponentInParent<OwnershipManager>();
            if (m is not null) return;

            var no = other.GetComponentInParent<NetworkObject>();
            if (no is null) return;
            // Only update ownership if we are in oldest or newest user mode
            if (mode is not (OwnershipMode.OldestUser or OwnershipMode.NewestUser)) return;

            // Remove the owner that is no longer overlapping from the list of potential owners
            potentialOwners.Remove(no.Owner);
            UpdateOwner(GetFirstPotentialOwner());
        }
        [ServerRpc(RequireOwnership = false)]
        private void OnTriggerExitServerRpc(GameObject other) => OnTriggerEnterServer(other);
        
        /// <summary>
        /// On validate gives warnings if settings on connected components aren't properly set
        /// </summary>
        protected override void OnValidate() {
            base.OnValidate();

            var rigidbody = GetComponent<Rigidbody>();
            var collider = GetComponent<Collider>();

            if (!rigidbody.isKinematic)
                Debug.LogWarning( "If the rigidbody isn't kinematic the volume might move around! And then everyone will be very confused!");
            if (!collider.isTrigger)
                Debug.LogError("The collider must be a trigger or the volume will not function correctly.");
        }
        
        /// <summary>
        /// Function that updates the debug display when the volume's owner changes
        /// </summary>
        private void OnVolumeOwnerChanged(NetworkConnection prev, NetworkConnection @new, bool asServer) {
            volumeOwnerDebug = volumeOwner?.ClientId ?? -1;
        }

        
        /// <summary>
        /// Server only function called by an OwnershipManager to register it as listening for changes in ownership
        /// </summary>
        /// <param name="m"></param>
        public void RegisterAsListener(OwnershipManager m) {
            if (IsServer) RegisterAsListenerServer(m);
            else RegisterAsListenerServerRPC(m.NetworkObject);
        }
        [Server]
        private void RegisterAsListenerServer(OwnershipManager m) {
            if (m is null) return;
            containedOwnershipManagers.Add(m);
        }
        [ServerRpc(RequireOwnership = false)]
        private void RegisterAsListenerServerRPC(NetworkObject m) =>
            RegisterAsListenerServer(m?.GetComponentInChildren<OwnershipManager>());
        
        
        /// <summary>
        /// Server only function called by an OwnershipManager to indicate it is no longer interested in ownership changes
        /// </summary>
        /// <param name="m"></param>
        public void UnregisterAsListener(OwnershipManager m) {
            if (IsServer) UnregisterAsListenerServer(m);
            else UnregisterAsListenerServerRPC(m.NetworkObject);
        }
        [Server]
        private void UnregisterAsListenerServer(OwnershipManager m) {
            if (m is null) return;
            containedOwnershipManagers.Remove(m);
        }
        [ServerRpc(RequireOwnership = false)]
        private void UnregisterAsListenerServerRPC(NetworkObject m) =>
            UnregisterAsListenerServer(m?.GetComponentInChildren<OwnershipManager>());


        
        /// <summary>
        /// Server only function that updates the current owner
        /// </summary>
        /// <param name="newOwner">The connection which should become the new owner</param>
        [Server]
        protected void UpdateOwner(NetworkConnection newOwner) {
            volumeOwner = newOwner;

            // Notify all of the contained OwnershipManagers that the owner has changed
            foreach (var m in containedOwnershipManagers)
                m.GiveOwnership(volumeOwner);
        }
        
        /// <summary>
        /// RPC that tells the server to update the current owner
        /// </summary>
        /// <param name="client">The connection which should become the new owner</param>
        [ServerRpc]
        protected void UpdateOwnerServerRpc(NetworkConnection client) => UpdateOwner(client);

        
        /// <summary>
        /// Helper function that extracts the first connection from the set of potential owners
        /// </summary>
        /// <returns>The first owner in the set of potential owners</returns>
        private NetworkConnection GetFirstPotentialOwner() {
            NetworkConnection ret = null;
            foreach (var owner in potentialOwners) {
                if (ret is not null) break;
                ret = owner;
            }

            return ret;
        }
    }
}
