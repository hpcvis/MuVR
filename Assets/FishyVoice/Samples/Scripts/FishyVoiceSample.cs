using System;
using FishNet.Transporting;
using TriInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FishyVoice.Samples {
	/// <summary>
	/// Extension to FishNetworking's auto start sample script which manages as voice network as well
	/// </summary>
	public class FishyVoiceSample : NetworkHudCanvases {
		// Indicator indicating if voice should be enabled or not
		public Image voiceIndicator;

		[Header("Voice Settings")]
		[SerializeField]
		private bool spacer;
		[PropertyTooltip("Name of the room that we should join by default")]
		public string roomName = "<DEFAULT>";

		// Variable indicating the current connection state of the voice network
		protected LocalConnectionState voiceState => voiceNetwork?.connectionState ?? LocalConnectionState.Stopped;
		// Voice network reference
		protected VoiceNetwork voiceNetwork;
		// Agent that participates in the voice network
		protected Agent agent;

		protected void Awake() {
			voiceNetwork = FindObjectOfType<VoiceNetwork>();
			if (voiceNetwork is null)
				Debug.LogError("Voice Network object not found, voice connectivity will not work!");
		}

		protected virtual void InitAgent() {
			agent?.Dispose();
			agent = voiceNetwork.CreateAgent();
		}
		
		protected new void Start() {
			base.Start();

			voiceIndicator.transform.parent.gameObject.SetActive(false);

			// Listen for changes to the client state
			NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;

			UpdateColor(LocalConnectionState.Stopped, ref voiceIndicator);

			// Create agent and listen for its messages
			InitAgent();
			agent.Network.OnJoinedChatroom += VoiceStateUpdated;
			agent.Network.OnLeftChatroom += VoiceStateUpdated;
			agent.Network.OnClosedChatroom += VoiceStateUpdated;

			agent.Network.OnCreatedChatroom += OnHostChatroom;
			agent.Network.OnJoinedChatroom += OnJoinedChatroom;
			agent.Network.OnLeftChatroom += OnLeftChatroom;
			agent.Network.OnClosedChatroom += OnChatroomClose;

			agent.Network.OnChatroomCreationFailed += OnChatroomException;
			agent.Network.OnChatroomJoinFailed += OnChatroomException;
		}

		protected new void OnDestroy() {
			base.OnDestroy();
			if (NetworkManager is null) return;

			// Stop listening for changes to the client state
			NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;

			// Stop listening for agent messages
			agent.Network.OnJoinedChatroom -= VoiceStateUpdated;
			agent.Network.OnLeftChatroom -= VoiceStateUpdated;
			agent.Network.OnClosedChatroom -= VoiceStateUpdated;

			agent.Network.OnCreatedChatroom -= OnHostChatroom;
			agent.Network.OnJoinedChatroom -= OnJoinedChatroom;
			agent.Network.OnLeftChatroom -= OnLeftChatroom;
			agent.Network.OnClosedChatroom -= OnChatroomClose;

			agent.Network.OnChatroomCreationFailed -= OnChatroomException;
			agent.Network.OnChatroomJoinFailed -= OnChatroomException;
			agent?.Dispose();
		}

		protected void OnClientConnectionState(ClientConnectionStateArgs args) {
			// Make sure the voice button is only visible if we are a client (or host)
			voiceIndicator.transform.parent.gameObject.SetActive(args.ConnectionState == LocalConnectionState.Started);

			if (args.ConnectionState != LocalConnectionState.Started) return;

			// When the host client starts it should leave its chatroom (it will be kept open with ID -1)
			if (NetworkManager.IsServer) agent.LeaveChatroom();
		}

		public override void OnClick_Server() {
			if (NetworkManager is null)
				return;

			if (serverState != LocalConnectionState.Stopped) {
				NetworkManager.ServerManager.OnServerConnectionState -= OnServerStart;
				NetworkManager.ServerManager.StopConnection(true);
			} else {
				NetworkManager.ServerManager.StartConnection();
				NetworkManager.ServerManager.OnServerConnectionState += OnServerStart;
			}
		}

		protected void OnServerStart(ServerConnectionStateArgs args) {
			if (args.ConnectionState != LocalConnectionState.Started) return;

			// The server needs to keep a room running, but it shouldn't participate in the room unless it is a host and the client part wishes to
			agent.HostChatroom(roomName);
		}

		public override void OnClick_Client() {
			if (NetworkManager is null)
				return;

			if (voiceState == LocalConnectionState.Started && clientState == LocalConnectionState.Started)
				agent.LeaveChatroom();

			base.OnClick_Client();
		}

		public virtual void OnClick_Voice() {
			if (NetworkManager is null) return;
			if (!NetworkManager.IsClient) return;

			// Turning voice on or off equates to joining or leaving a chatroom
			if (voiceNetwork.connectionState == LocalConnectionState.Started)
				agent.LeaveChatroom();
			else
				agent.JoinChatroom(roomName);
		}

		protected void VoiceStateUpdated() => UpdateColor(voiceNetwork.connectionState, ref voiceIndicator);
		protected void VoiceStateUpdated(short _) => VoiceStateUpdated();
		protected void OnHostChatroom() => Debug.Log($"<color=blue>[FishyVoice]</color> We have hosted chatroom `{agent.Network.CurrentChatroomName}`");
		protected void OnJoinedChatroom(short _) => Debug.Log($"<color=blue>[FishyVoice]</color> We have joined chatroom `{agent.Network.CurrentChatroomName}`");
		protected void OnLeftChatroom() => Debug.Log("<color=blue>[FishyVoice]</color> We have left our chatroom");
		protected void OnChatroomClose() => Debug.Log("<color=blue>[FishyVoice]</color> Our chatroom has been closed");
		protected void OnChatroomException(Exception e) =>throw e;
	}
}