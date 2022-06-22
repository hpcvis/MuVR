using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

// NOTE: Component ported from Mirror
[RequireComponent(typeof(NetworkTransform))]
public class NetworkRigidbody2D : EnchancedNetworkBehaviour {
    [Header("Settings")] 
    [SerializeField] Rigidbody2D target;
    [Tooltip("Flag indicating weather or not the managed Rigidbody2D should be Kinematic")]
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
    
    private bool ClientWithAuthority => clientAuthority && IsOwner;
    private bool ServerWithAuthority => IsServer && !clientAuthority;
    private bool IsAuthority => ClientWithAuthority || ServerWithAuthority;

    private new void OnValidate() {
        base.OnValidate();
        if (target is null) target = GetComponent<Rigidbody2D>();
        if (target is not null) targetIsKinematic = target.isKinematic;
    }

    #region Sync vars

    #region veclocity sync
    
    [SerializeField, ReadOnly] private Vector2 _velocity;

    public Vector2 velocity {
        get => _velocity;
        set {
            OnVelocityChanged(_velocity, value, false);
            if (IsServer)
                ObserversSetVelocity(value);
            else if (IsClient)
                ServerSetVelocity(value);
        }
    }

    [ServerRpc]
    private void ServerSetVelocity(Vector2 value, Channel channel = Channel.Unreliable) {
        OnVelocityChanged(_velocity, value, false);
        ObserversSetVelocity(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetVelocity(Vector2 value, Channel channel = Channel.Unreliable) {
        OnVelocityChanged(_velocity, value, false);
        _velocity = value;
    }
    
    private void OnVelocityChanged(Vector2 _, Vector2 newValue, bool onServer) => target.velocity = newValue;
    
    #endregion

    #region angular velocity sync

    [SerializeField, ReadOnly] private float _angularVelocity;

    public float angularVelocity {
        get => _angularVelocity;
        set {
            OnAngularVelocityChanged(_angularVelocity, value, false);
            if (IsServer)
                ObserversSetAngularVelocity(value);
            else if (IsClient)
                ServerSetAngularVelocity(value);
        }
    }

    [ServerRpc]
    private void ServerSetAngularVelocity(float value, Channel channel = Channel.Unreliable) {
        OnAngularVelocityChanged(_angularVelocity, value, false);
        ObserversSetAngularVelocity(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetAngularVelocity(float value, Channel channel = Channel.Unreliable) {
        OnAngularVelocityChanged(_angularVelocity, value, false);
        _angularVelocity = value;
    }
    
    private void OnAngularVelocityChanged(float _, float newValue, bool onServer) => target.angularVelocity = newValue;
    
    #endregion
    
    #region is kinemtaic sync

    [SerializeField, ReadOnly] private bool _isKinematic;

    public bool isKinematic {
        get => _isKinematic;
        set {
            OnIsKinematicChanged(_isKinematic, value, false);
            if (IsServer)
                ObserversSetIsKinematic(value);
            else if (IsClient)
                ServerSetIsKinematic(value);
        }
    }

    [ServerRpc]
    private void ServerSetIsKinematic(bool value, Channel channel = Channel.Reliable) {
        OnIsKinematicChanged(_isKinematic, value, false);
        ObserversSetIsKinematic(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetIsKinematic(bool value, Channel channel = Channel.Reliable) {
        OnIsKinematicChanged(_isKinematic, value, false);
        _isKinematic = value;
    }
    
    private void OnIsKinematicChanged(bool _, bool newValue, bool onServer) {
        targetIsKinematic = newValue;
        UpdateOwnershipKinematicState();
    }
    
    #endregion
    
    #region gravity scale sync

    [SerializeField, ReadOnly] private float _gravityScale;

    public float gravityScale {
        get => _gravityScale;
        set {
            OnGravityScaleChanged(_gravityScale, value, false);
            if (IsServer)
                ObserversSetGravityScale(value);
            else if (IsClient)
                ServerSetGravityScale(value);
        }
    }

    [ServerRpc]
    private void ServerSetGravityScale(float value, Channel channel = Channel.Reliable) {
        OnGravityScaleChanged(_gravityScale, value, false);
        ObserversSetGravityScale(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetGravityScale(float value, Channel channel = Channel.Reliable) {
        OnGravityScaleChanged(_gravityScale, value, false);
        _gravityScale = value;
    }
    
    private void OnGravityScaleChanged(float _, float newValue, bool onServer) => target.gravityScale = newValue;
    
    #endregion
    
    #region drag sync

    [SerializeField, ReadOnly] private float _drag;

    public float drag {
        get => _drag;
        set {
            OnDragChanged(_drag, value, false);
            if (IsServer)
                ObserversSetDrag(value);
            else if (IsClient)
                ServerSetDrag(value);
        }
    }

    [ServerRpc]
    private void ServerSetDrag(float value, Channel channel = Channel.Reliable) {
        OnDragChanged(_drag, value, false);
        ObserversSetDrag(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetDrag(float value, Channel channel = Channel.Reliable) {
        OnDragChanged(_drag, value, false);
        _drag = value;
    }
    
    private void OnDragChanged(float _, float newValue, bool onServer) => target.drag = newValue;
    
    #endregion
    
    #region angular drag sync

    [SerializeField, ReadOnly] private float _angularDrag;

    public float angularDrag {
        get => _angularDrag;
        set {
            OnAngularDragChanged(_angularDrag, value, false);
            if (IsServer)
                ObserversSetAngularDrag(value);
            else if (IsClient)
                ServerSetAngularDrag(value);
        }
    }

    [ServerRpc]
    private void ServerSetAngularDrag(float value, Channel channel = Channel.Reliable) {
        OnAngularDragChanged(_angularDrag, value, false);
        ObserversSetAngularDrag(value);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetAngularDrag(float value, Channel channel = Channel.Reliable) {
        OnAngularDragChanged(_angularDrag, value, false);
        _angularDrag = value;
    }
    
    private void OnAngularDragChanged(float _, float newValue, bool onServer) => target.angularDrag = newValue;
    
    #endregion

    #endregion

    // Bool tracking if the "SyncVar"s have been initialized
    private bool isStarted = false;
    
    public override void OnStartBoth() {
        base.OnStartBoth();

        UpdateOwnershipKinematicState();
        
        // Make sure that rarely updated properties have their initial values synced
        if (IsAuthority) {
            isKinematic = targetIsKinematic;
            gravityScale = target.gravityScale;
            drag = target.drag;
            angularDrag = target.angularDrag;
        }

        isStarted = true;
    }
    

    public override void OnOwnershipBoth(NetworkConnection prev) {
        base.OnOwnershipBoth(prev);

        if (!isStarted) return;
        
        // If your the owner, make sure your local Rigidbody2D has the same settings as the previous owner
        if (IsAuthority) {
            target.velocity = velocity;
            target.angularVelocity = angularVelocity;
        }
        
        UpdateOwnershipKinematicState();
    }

    // Make sure that anyone without authority isn't performing physics calculations
    public void UpdateOwnershipKinematicState() => target.isKinematic = targetIsKinematic || !IsAuthority;

    public override void Tick() {
        // Debug.Log($"Pre: {velocity} - {target.velocity}");
        
        SendDataIfAuthority();

        // Debug.Log($"Post: {velocity} - {target.velocity}");
    }

    // TODO: Should this be switched to occuring on ticks?
    void FixedUpdate() {
        if (clearAngularVelocity && !syncAngularVelocity) target.angularVelocity = 0;
        if (clearVelocity && !syncVelocity) target.velocity = Vector2.zero;
    }
    

    /// <summary>
    ///     Uses Command to send values to server
    /// </summary>
    private void SendDataIfAuthority() {
        if (!IsAuthority) return;
        
        SendVelocity();
        SendRigidbodySettings();
    }
    
    private void SendVelocity() {
        var currentVelocity = syncVelocity ? target.velocity : default;
        var currentAngularVelocity = syncAngularVelocity ? target.angularVelocity : default;

        var velocityChanged = syncVelocity && (previousValue.velocity - currentVelocity).sqrMagnitude >
            velocitySensitivity * velocitySensitivity;
        var angularVelocityChanged = syncAngularVelocity &&
                                     Mathf.Abs(previousValue.angularVelocity - currentAngularVelocity) > angularVelocitySensitivity;

        // if angularVelocity has changed it is likely that velocity has also changed so just sync both values
        // however if only velocity has changed just send velocity
        if (angularVelocityChanged) {
            velocity = currentVelocity;
            angularVelocity = currentAngularVelocity;
            previousValue.velocity = currentVelocity;
            previousValue.angularVelocity = currentAngularVelocity;
        }
        else if (velocityChanged) {
            velocity = currentVelocity;
            previousValue.velocity = currentVelocity;
        }
    }

    [Client(RequireOwnership = true)]
    private void SendRigidbodySettings() {
        // These shouldn't change often so it is ok to send in their own Command
        if (previousValue.isKinematic != targetIsKinematic) {
            isKinematic = targetIsKinematic;
            previousValue.isKinematic = targetIsKinematic;
        }

        if (previousValue.gravityScale != target.gravityScale) {
            gravityScale = target.gravityScale;
            previousValue.gravityScale = target.gravityScale;
        }

        if (previousValue.drag != target.drag) {
            drag = target.drag;
            previousValue.drag = target.drag;
        }

        if (previousValue.angularDrag != target.angularDrag) {
            angularDrag = target.angularDrag;
            previousValue.angularDrag = target.angularDrag;
        }
    }

    /// <summary>
    ///     holds previously synced values
    /// </summary>
    public struct ClientSyncState {
        public Vector2 velocity;
        public float angularVelocity;
        public bool isKinematic;
        public float gravityScale;
        public float drag;
        public float angularDrag;
    }
}

