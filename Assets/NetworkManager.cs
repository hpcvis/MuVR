using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks {
	/// <summary>
	///     This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking
	///     changes).
	/// </summary>
	private readonly string gameVersion = "1";

	public string playerPrefab;


	/// <summary>
	///     MonoBehaviour method called on GameObject by Unity during early initialization phase.
	/// </summary>
	private void Awake() {
		// #Critical
		// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
		PhotonNetwork.AutomaticallySyncScene = true;
	}


	/// <summary>
	///     MonoBehaviour method called on GameObject by Unity during initialization phase.
	/// </summary>
	private void Start() {
		// we check if we are connected or not, we join if we are , else we initiate the connection to the server.
		if (PhotonNetwork.IsConnected) {
			// #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
			PhotonNetwork.JoinRandomRoom();
		}
		else {
			// #Critical, we must first and foremost connect to Photon Online Server.
			PhotonNetwork.ConnectUsingSettings();
			PhotonNetwork.GameVersion = gameVersion;
		}
	}
	
	public override void OnConnectedToMaster()
	{
		Debug.Log("OnConnectedToMaster() was called by PUN");
		
		// #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
		PhotonNetwork.JoinRandomRoom();
	}


	public override void OnDisconnected(DisconnectCause cause)
	{
		Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
	}
	
	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

		// #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
		PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 100 });
	}

	public override void OnJoinedRoom()
	{
		Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");
		
#if !UNITY_SERVER
		PhotonNetwork.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
#endif
	}
}