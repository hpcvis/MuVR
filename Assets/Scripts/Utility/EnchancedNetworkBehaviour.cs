using FishNet.CodeAnalysis.Annotations;
using FishNet.Connection;
using FishNet.Object;

// Additions to NetworkBehaviour that make it easier to use
public class EnchancedNetworkBehaviour : NetworkBehaviour {

	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnStartServer() {
		base.OnStartServer();
		OnStartBoth();
	}

	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnStartClient() {
		base.OnStartClient();
		OnStartBoth();
	}
	
	/// Function called when the object this component is attached to is spawned on either the client or the server
	/// Note: Automatically begins listening to tick events
	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public virtual void OnStartBoth() {
		TimeManager.OnPreTick += PreTick;
		TimeManager.OnTick += Tick;
		TimeManager.OnPostTick += PostTick;
	}

	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnStopServer() {
		base.OnStopServer();
		OnStopBoth();
	}

	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnStopClient() {
		base.OnStopClient();
		OnStopBoth();
	}

	/// Function called when the object this component is attached to is destroyed on either the client or the server
	/// NOTE: Unregisters tick events
	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public virtual void OnStopBoth() {
		TimeManager.OnPreTick -= PreTick;
		TimeManager.OnTick -= Tick;
		TimeManager.OnPostTick -= PostTick;
	}

	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnOwnershipServer(NetworkConnection prevOwner) {
		base.OnOwnershipServer(prevOwner);
		OnOwnershipBoth(prevOwner);
	}
	
	[OverrideMustCallBase(BaseCallMustBeFirstStatement = true)]
	public override void OnOwnershipClient(NetworkConnection prevOwner) {
		base.OnOwnershipClient(prevOwner);
		OnOwnershipBoth(prevOwner);
	}

	/// Function called when the ownership of this object changes on either the client or the server
	public virtual void OnOwnershipBoth(NetworkConnection prevOwner) { }
	
	/// Called right before a tick occurs, as well before data is read.
	public virtual void PreTick() { }
	/// Called when a tick occurs.
	public virtual void Tick() { }
	/// Called after a tick occurs; physics would have simulated if using PhysicsMode.TimeManager.
	public virtual void PostTick() { }

	
	/// Function that requests that the server give the provided NetworkConnection ownership
	public void RequestGiveOwnership(NetworkConnection newOwner) {
		if(IsServer) GiveOwnership(newOwner);
		else GiveOwnershipServerRPC(newOwner);
	}

	/// Server RPC that gives the provided NetworkConnection ownership over the controlling object
	[ServerRpc(RequireOwnership = false)]
	protected void GiveOwnershipServerRPC(NetworkConnection newOwner) => GiveOwnership(newOwner);
}

