using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

// NOTE: Component ported from Mirror
[HelpURL("https://mirror-networking.gitbook.io/docs/components/network-rigidbody")]
[RequireComponent(typeof(NetworkTransform))]
public class NetworkRigidbody : EnchancedNetworkBehaviour {
    [Header("Settings")] 
    [SerializeField] Rigidbody target;
    [Tooltip("Flag indicating weather or not the managed rigidbody should be Kinematic")]
    public bool targetIsKinematic = false;

    [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
    public bool clientAuthority = true;

    [field: Header("Velocity")] 
    [field: Tooltip("Syncs Velocity every SyncInterval")]
    [field: SerializeField] private bool syncVelocity = true;

    [field: Tooltip("Set velocity to 0 each frame (only works if syncVelocity is false")] 
    [field: SerializeField] private bool clearVelocity = false;

    [field: Tooltip("Only Syncs Value if distance between previous and current is great than sensitivity")]
    [field: SerializeField] private float velocitySensitivity = 0.1f;


    [field: Header("Angular Velocity")] 
    [field: Tooltip("Syncs AngularVelocity every SyncInterval")] 
    [field: SerializeField] private bool syncAngularVelocity = true;

    [field: Tooltip("Set angularVelocity to 0 each frame (only works if syncAngularVelocity is false")]
    [field: SerializeField] private bool clearAngularVelocity = false;

    [field: Tooltip("Only Syncs Value if distance between previous and current is great than sensitivity")]
    [field: SerializeField] private float angularVelocitySensitivity = 0.1f;

    /// <summary>
    ///     Values sent on client with authority after they are sent to the server
    /// </summary>
    private ClientSyncState previousValue = new();

    private new void OnValidate() {
        base.OnValidate();
        if (target is null) target = GetComponent<Rigidbody>();
        if (target is not null) targetIsKinematic = target.isKinematic;
    }

    #region Sync vars

    [SyncVar(Channel = Channel.Unreliable, OnChange = nameof(OnVelocityChanged))]
    private Vector3 velocity;

    [SyncVar(Channel = Channel.Unreliable, OnChange = nameof(OnAngularVelocityChanged))]
    private Vector3 angularVelocity;

    [SyncVar(OnChange = nameof(OnIsKinematicChanged))]
    private bool isKinematic;

    [SyncVar(OnChange = nameof(OnUseGravityChanged))]
    private bool useGravity;

    [SyncVar(OnChange = nameof(OnDragChanged))]
    private float drag;

    [SyncVar(OnChange = nameof(OnAngularDragChanged))]
    private float angularDrag;

    /// <summary>
    ///     Ignore value if is host or client with Authority
    /// </summary>
    /// <returns></returns>
    private bool IgnoreSync => IsServer || ClientWithAuthority;

    private bool ClientWithAuthority => clientAuthority && IsOwner;
    private bool ServerWithAuthority => IsServer && !clientAuthority;

    private void OnVelocityChanged(Vector3 _, Vector3 newValue, bool onServer) {
        if (IgnoreSync)
            return;

        target.velocity = newValue;
    }

    private void OnAngularVelocityChanged(Vector3 _, Vector3 newValue, bool onServer) {
        if (IgnoreSync)
            return;

        target.angularVelocity = newValue;
    }

    private void OnIsKinematicChanged(bool _, bool newValue, bool onServer) {
        if (IgnoreSync)
            return;

        targetIsKinematic = newValue;
        UpdateOwnershipKinematicState();
    }

    private void OnUseGravityChanged(bool _, bool newValue, bool onServer) {
        if (IgnoreSync)
            return;

        target.useGravity = newValue;
    }

    private void OnDragChanged(float _, float newValue, bool onServer) {
        if (IgnoreSync)
            return;

        target.drag = newValue;
    }

    private void OnAngularDragChanged(float _, float newValue, bool onServer) {
        if (IgnoreSync)
            return;

        target.angularDrag = newValue;
    }

    #endregion
    
    public override void OnStartBoth() {
        base.OnStartBoth();
        OnEnable();
        
        UpdateOwnershipKinematicState();
    }

    public override void OnOwnershipBoth(NetworkConnection prev) {
        base.OnOwnershipBoth(prev);

        UpdateOwnershipKinematicState();
    }
    
    // Make sure that anyone without authority isn't performing physics calculations
    public void UpdateOwnershipKinematicState() => target.isKinematic = targetIsKinematic || !(ServerWithAuthority || ClientWithAuthority);
    

    public void OnEnable() {
        if (TimeManager is not null)
            TimeManager.OnPostTick += OnPostTick;
    }

    void OnDisable() {
        if (TimeManager is not null)
            TimeManager.OnPostTick -= OnPostTick;
    }
    
    private void OnPostTick() {
        if (IsServer)
            SyncToClients();
        else if (ClientWithAuthority) SendToServer();
    }

    // TODO: Should this be switched to occuring on ticks?
    void FixedUpdate() {
        if (clearAngularVelocity && !syncAngularVelocity) target.angularVelocity = Vector3.zero;

        if (clearVelocity && !syncVelocity) target.velocity = Vector3.zero;
    }

    /// <summary>
    ///     Updates sync var values on server so that they sync to the client
    /// </summary>
    [Server]
    private void SyncToClients() {
        // only update if they have changed more than Sensitivity

        var currentVelocity = syncVelocity ? target.velocity : default;
        var currentAngularVelocity = syncAngularVelocity ? target.angularVelocity : default;

        var velocityChanged = syncVelocity && (previousValue.velocity - currentVelocity).sqrMagnitude >
            velocitySensitivity * velocitySensitivity;
        var angularVelocityChanged = syncAngularVelocity &&
                                     (previousValue.angularVelocity - currentAngularVelocity).sqrMagnitude >
                                     angularVelocitySensitivity * angularVelocitySensitivity;

        if (velocityChanged) {
            velocity = currentVelocity;
            previousValue.velocity = currentVelocity;
        }

        if (angularVelocityChanged) {
            angularVelocity = currentAngularVelocity;
            previousValue.angularVelocity = currentAngularVelocity;
        }

        // other rigidbody settings
        isKinematic = targetIsKinematic;
        useGravity = target.useGravity;
        drag = target.drag;
        angularDrag = target.angularDrag;
    }

    /// <summary>
    ///     Uses Command to send values to server
    /// </summary>
    [Client(RequireOwnership = true)]
    private void SendToServer() {
        SendVelocity();
        SendRigidBodySettings();
    }

    [Client(RequireOwnership = true)]
    private void SendVelocity() {
        var now = Time.time;
        if (now < previousValue.nextSyncTime)
            return;

        var currentVelocity = syncVelocity ? target.velocity : default;
        var currentAngularVelocity = syncAngularVelocity ? target.angularVelocity : default;

        var velocityChanged = syncVelocity && (previousValue.velocity - currentVelocity).sqrMagnitude >
            velocitySensitivity * velocitySensitivity;
        var angularVelocityChanged = syncAngularVelocity &&
                                     (previousValue.angularVelocity - currentAngularVelocity).sqrMagnitude >
                                     angularVelocitySensitivity * angularVelocitySensitivity;

        // if angularVelocity has changed it is likely that velocity has also changed so just sync both values
        // however if only velocity has changed just send velocity
        if (angularVelocityChanged) {
            SendVelocityAndAngularServerRpc(currentVelocity, currentAngularVelocity);
            previousValue.velocity = currentVelocity;
            previousValue.angularVelocity = currentAngularVelocity;
        }
        else if (velocityChanged) {
            SendVelocityServerRpc(currentVelocity);
            previousValue.velocity = currentVelocity;
        }


        // only update syncTime if either has changed
        if (angularVelocityChanged || velocityChanged) previousValue.nextSyncTime = now + (float) TimeManager.TickDelta;
    }

    [Client(RequireOwnership = true)]
    private void SendRigidBodySettings() {
        // These shouldn't change often so it is ok to send in their own Command
        if (previousValue.isKinematic != targetIsKinematic) {
            SendIsKinematicServerRpc(targetIsKinematic);
            previousValue.isKinematic = targetIsKinematic;
        }

        if (previousValue.useGravity != target.useGravity) {
            SendUseGravityServerRpc(target.useGravity);
            previousValue.useGravity = target.useGravity;
        }

        if (previousValue.drag != target.drag) {
            SendDragServerRpc(target.drag);
            previousValue.drag = target.drag;
        }

        if (previousValue.angularDrag != target.angularDrag) {
            SendAngularDragServerRpc(target.angularDrag);
            previousValue.angularDrag = target.angularDrag;
        }
    }

    /// <summary>
    ///     Called when only Velocity has changed on the client
    /// </summary>
    [ServerRpc]
    private void SendVelocityServerRpc(Vector3 velocity) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        this.velocity = velocity;
        target.velocity = velocity;
    }

    /// <summary>
    ///     Called when angularVelocity has changed on the client
    /// </summary>
    [ServerRpc]
    private void SendVelocityAndAngularServerRpc(Vector3 velocity, Vector3 angularVelocity) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        if (syncVelocity) {
            this.velocity = velocity;

            target.velocity = velocity;
        }

        this.angularVelocity = angularVelocity;
        target.angularVelocity = angularVelocity;
    }

    [ServerRpc]
    private void SendIsKinematicServerRpc(bool isKinematic) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        this.isKinematic = isKinematic;
        targetIsKinematic = isKinematic;
        UpdateOwnershipKinematicState();
    }

    [ServerRpc]
    private void SendUseGravityServerRpc(bool useGravity) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        this.useGravity = useGravity;
        target.useGravity = useGravity;
    }

    [ServerRpc]
    private void SendDragServerRpc(float drag) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        this.drag = drag;
        target.drag = drag;
    }

    [ServerRpc]
    private void SendAngularDragServerRpc(float angularDrag) {
        // Ignore messages from client if not in client authority mode
        if (!clientAuthority)
            return;

        this.angularDrag = angularDrag;
        target.angularDrag = angularDrag;
    }

    /// <summary>
    ///     holds previously synced values
    /// </summary>
    public struct ClientSyncState {
        /// <summary>
        ///     Next sync time that velocity will be synced, based on syncInterval.
        /// </summary>
        public float nextSyncTime;

        public Vector3 velocity;
        public Vector3 angularVelocity;
        public bool isKinematic;
        public bool useGravity;
        public float drag;
        public float angularDrag;
    }
}

