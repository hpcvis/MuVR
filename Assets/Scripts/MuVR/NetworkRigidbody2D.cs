using System;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using TriInspector;
using UnityEngine;

namespace MuVR {
	
	// NOTE: Component ported from Mirror
	[RequireComponent(typeof(NetworkTransform))]
	public class NetworkRigidbody2D : MuVR.Enchanced.NetworkBehaviour {
		[Title("Settings")] 
		[Required, SerializeField] private Rigidbody2D target;

		[PropertyTooltip("Flag indicating weather or not the managed Rigidbody2D should be Kinematic")]
		public bool targetIsKinematic;

		[PropertyTooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
		public bool clientAuthority = true;

		[field: Title("Velocity")]
		[field: PropertyTooltip("Syncs Velocity every SyncInterval")]
		[field: SerializeField] private bool syncVelocity = true;

		[field: PropertyTooltip("Set velocity to 0 each frame (only works if syncVelocity is false")]
		[field: HideIf(nameof(syncVelocity))]
		[field: SerializeField] private bool clearVelocity;


		[field: Title("Angular Velocity")]
		[field: PropertyTooltip("Syncs AngularVelocity every SyncInterval")]
		[field: SerializeField] private bool syncAngularVelocity = true;

		[field: PropertyTooltip("Set angularVelocity to 0 each frame (only works if syncAngularVelocity is false")]
		[field: HideIf(nameof(syncAngularVelocity))]
		[field: SerializeField] private bool clearAngularVelocity;

        /// <summary>
        ///     Values sent on client with authority after they are sent to the server
        /// </summary>
        private ClientSyncState previousValue;

		private new void OnValidate() {
			base.OnValidate();
			if (target is null) target = GetComponent<Rigidbody2D>();
			if (target is not null) targetIsKinematic = target.isKinematic;
		}

		private bool ClientWithAuthority => clientAuthority && IsOwner;
		private bool ServerWithAuthority => IsServer && !clientAuthority;
		private bool IsAuthority => ClientWithAuthority || ServerWithAuthority;

		#region Sync vars

		#region veclocity sync

		[ReadOnly, SerializeField] private Vector2 _velocity;

		public Vector2 velocity {
			get => _velocity;
			set {
				OnVelocityChanged(_velocity, value, false);
				_velocity = value;
				if (IsServer)
					ObserversSetVelocity(value);
				else if (IsClient)
					ServerSetVelocity(value);
			}
		}

		[ServerRpc]
		private void ServerSetVelocity(Vector2 value) {
			OnVelocityChanged(_velocity, value, false);
			ObserversSetVelocity(value);
			_velocity = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetVelocity(Vector2 value) {
			OnVelocityChanged(_velocity, value, false);
			_velocity = value;
		}

		private void OnVelocityChanged(Vector2 _, Vector2 newValue, bool onServer) {
			target.velocity = newValue;
		}

		#endregion

		#region angular velocity sync

		[ReadOnly, SerializeField] private float _angularVelocity;

		public float angularVelocity {
			get => _angularVelocity;
			set {
				OnAngularVelocityChanged(_angularVelocity, value, false);
				_angularVelocity = value;
				if (IsServer)
					ObserversSetAngularVelocity(value);
				else if (IsClient)
					ServerSetAngularVelocity(value);
			}
		}

		[ServerRpc]
		private void ServerSetAngularVelocity(float value) {
			OnAngularVelocityChanged(_angularVelocity, value, false);
			ObserversSetAngularVelocity(value);
			_angularVelocity = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetAngularVelocity(float value) {
			OnAngularVelocityChanged(_angularVelocity, value, false);
			_angularVelocity = value;
		}

		private void OnAngularVelocityChanged(float _, float newValue, bool onServer) {
			target.angularVelocity = newValue;
		}

		#endregion

		#region is kinemtaic sync

		[ReadOnly, SerializeField] private bool _isKinematic;

		public bool isKinematic {
			get => _isKinematic;
			set {
				OnIsKinematicChanged(_isKinematic, value, false);
				_isKinematic = value;
				if (IsServer)
					ObserversSetIsKinematic(value);
				else if (IsClient)
					ServerSetIsKinematic(value);
			}
		}

		[ServerRpc]
		private void ServerSetIsKinematic(bool value) {
			OnIsKinematicChanged(_isKinematic, value, false);
			ObserversSetIsKinematic(value);
			_isKinematic = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetIsKinematic(bool value) {
			OnIsKinematicChanged(_isKinematic, value, false);
			_isKinematic = value;
		}

		private void OnIsKinematicChanged(bool _, bool newValue, bool onServer) {
			targetIsKinematic = newValue;
			UpdateOwnershipKinematicState();
		}

		#endregion

		#region use gravity sync

		[ReadOnly, SerializeField] private float _gravityScale;

		public float gravityScale {
			get => _gravityScale;
			set {
				OnGravityScaleChanged(_gravityScale, value, false);
				_gravityScale = value;
				if (IsServer)
					ObserversSetGravityScale(value);
				else if (IsClient)
					ServerSetGravityScale(value);
			}
		}

		[ServerRpc]
		private void ServerSetGravityScale(float value) {
			OnGravityScaleChanged(_gravityScale, value, false);
			ObserversSetGravityScale(value);
			_gravityScale = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetGravityScale(float value) {
			OnGravityScaleChanged(_gravityScale, value, false);
			_gravityScale = value;
		}

		private void OnGravityScaleChanged(float _, float newValue, bool onServer) {
			target.gravityScale = newValue;
		}

		#endregion

		#region drag sync

		[ReadOnly, SerializeField] private float _drag;

		public float drag {
			get => _drag;
			set {
				OnDragChanged(_drag, value, false);
				_drag = value;
				if (IsServer)
					ObserversSetDrag(value);
				else if (IsClient)
					ServerSetDrag(value);
			}
		}

		[ServerRpc]
		private void ServerSetDrag(float value) {
			OnDragChanged(_drag, value, false);
			ObserversSetDrag(value);
			_drag = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetDrag(float value) {
			OnDragChanged(_drag, value, false);
			_drag = value;
		}

		private void OnDragChanged(float _, float newValue, bool onServer) {
			target.drag = newValue;
		}

		#endregion

		#region angular drag sync

		[ReadOnly, SerializeField] private float _angularDrag;

		public float angularDrag {
			get => _angularDrag;
			set {
				OnAngularDragChanged(_angularDrag, value, false);
				_angularDrag = value;
				if (IsServer)
					ObserversSetAngularDrag(value);
				else if (IsClient)
					ServerSetAngularDrag(value);
			}
		}

		[ServerRpc]
		private void ServerSetAngularDrag(float value) {
			OnAngularDragChanged(_angularDrag, value, false);
			ObserversSetAngularDrag(value);
			_angularDrag = value;
		}

		[ObserversRpc(BufferLast = true)]
		private void ObserversSetAngularDrag(float value) {
			OnAngularDragChanged(_angularDrag, value, false);
			_angularDrag = value;
		}

		private void OnAngularDragChanged(float _, float newValue, bool onServer) {
			target.angularDrag = newValue;
		}

		#endregion

		#endregion

		// Bool tracking if the "SyncVar"s have been initialized
		private bool isStarted;

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
		public void UpdateOwnershipKinematicState() {
			target.isKinematic = targetIsKinematic || !IsAuthority;
		}

		public override void Tick() {
			// Debug.Log($"Pre: {velocity} - {target.velocity}");

			SendDataIfAuthority();

			// Debug.Log($"Post: {velocity} - {target.velocity}");
		}

		// TODO: Should this be switched to occuring on ticks?
		private void FixedUpdate() {
			if (clearAngularVelocity && !syncAngularVelocity) target.angularVelocity = 0;
			if (clearVelocity && !syncVelocity) target.velocity = Vector2.zero;
		}


        /// <summary>
        ///     Uses Command to send values to server
        /// </summary>
        private void SendDataIfAuthority() {
			if (!IsAuthority) return;

			SendVelocity();
			SendRigidbody2DSettings();
		}

		private void SendVelocity() {
			// if angularVelocity has changed it is likely that velocity has also changed so just sync both values
			// however if only velocity has changed just send velocity
			if (syncVelocity && syncAngularVelocity) {
				velocity = target.velocity;
				angularVelocity = target.angularVelocity;
				previousValue.velocity = target.velocity;
				previousValue.angularVelocity = target.angularVelocity;
			}
			else if (syncVelocity) {
				velocity = target.velocity;
				previousValue.velocity = target.velocity;
			}
		}

		private void SendRigidbody2DSettings() {
			// These shouldn't change often so it is ok to send in their own Command
			if (previousValue.isKinematic != targetIsKinematic)
				previousValue.isKinematic = isKinematic = targetIsKinematic;

			if (Math.Abs(previousValue.gravityScale - target.gravityScale) > Mathf.Epsilon)
				previousValue.gravityScale = gravityScale = target.gravityScale;

			if (Math.Abs(previousValue.drag - target.drag) > Mathf.Epsilon)
				previousValue.drag = drag = target.drag;

			if (Math.Abs(previousValue.angularDrag - target.angularDrag) > Mathf.Epsilon)
				previousValue.angularDrag = angularDrag = target.angularDrag;
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
}