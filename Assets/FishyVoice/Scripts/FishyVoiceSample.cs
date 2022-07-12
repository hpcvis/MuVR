using System;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Transporting;

namespace FishyVoice {
    public class FishyVoiceSample : FishyVoice.NetworkHudCanvases {
        
        // Indicator indicating if voice should be enabled or not
	    public Image voiceIndicator;

        [Header("Voice Settings")]
        [Tooltip("Name of the room that we should join when enabling voice")]
        // Name of the room that players join by default
        public string roomName = "<DEFAULT>";
        
        // Variable indicating the current connection state of the voice network
        protected LocalConnectionState voiceState => voiceNetwork?.connectionState ?? LocalConnectionState.Stopped;
        // Voice network reference
        protected FishyVoice.VoiceNetwork voiceNetwork;
        // Agent that participates in the voice network
        protected FishyVoice.Agent agent;

        private void Awake(){
            voiceNetwork = FindObjectOfType<FishyVoice.VoiceNetwork>();
            if(voiceNetwork is null)
                Debug.LogError("Voice Network object not found, voice connectivity will not work!");
        }
        

        public new void Start() {
            base.Start();
            
            voiceIndicator.transform.parent.gameObject.SetActive(false);

             // Listen for changes to the client state
            NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            
            UpdateColor(LocalConnectionState.Stopped, ref voiceIndicator);
            
            // Create agent and listen for its messages
            agent?.Dispose();
            agent = voiceNetwork.CreateAgent();
            agent.Network.OnJoinedChatroom += VoiceStateUpdated;
            agent.Network.OnLeftChatroom += VoiceStateUpdated;
            agent.Network.OnlosedChatroom += VoiceStateUpdated;
            
            agent.Network.OnCreatedChatroom += OnHostChatroom;
            agent.Network.OnJoinedChatroom += OnJoinedChatroom;
            agent.Network.OnLeftChatroom += OnLeftChatroom;
            agent.Network.OnlosedChatroom += OnChatroomClose;

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
            agent.Network.OnlosedChatroom -= VoiceStateUpdated;
            
            agent.Network.OnCreatedChatroom -= OnHostChatroom;
            agent.Network.OnJoinedChatroom -= OnJoinedChatroom;
            agent.Network.OnLeftChatroom -= OnLeftChatroom;
            agent.Network.OnlosedChatroom -= OnChatroomClose;
            
            agent.Network.OnChatroomCreationFailed -= OnChatroomException;
            agent.Network.OnChatroomJoinFailed -= OnChatroomException;
            agent?.Dispose();
        }
        
        public void OnClientConnectionState(ClientConnectionStateArgs args) {
            // Make sure the voice button is only visible if we are a client (or host)
            voiceIndicator.transform.parent.gameObject.SetActive(args.ConnectionState == LocalConnectionState.Started);

            if (args.ConnectionState != LocalConnectionState.Started) return;

            // When the host client starts it should leave its chatroom (it will be kept open with ID -1)
            if(NetworkManager.IsServer) agent.LeaveChatroom();
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

        public void OnServerStart(ServerConnectionStateArgs args) {
            if (args.ConnectionState != LocalConnectionState.Started) return;

            // The server needs to keep a room running, but it shouldn't participate in the room unless it is a host and the client part wishes to
            agent.HostChatroom(roomName);
        }

        public override void OnClick_Client() {
            if (NetworkManager is null)
                return;
            
            if(voiceState == LocalConnectionState.Started && clientState == LocalConnectionState.Started) OnClick_Voice();
            
            base.OnClick_Client();
        }

        public virtual void OnClick_Voice() {
            if (NetworkManager is null) return;
            if (!NetworkManager.IsClient) return;

            // Turning voice on or off equates to joining or leaving a chatroom
            if(voiceNetwork.connectionState == LocalConnectionState.Started)
                agent.LeaveChatroom();
            else 
                agent.JoinChatroom(roomName);
        }
        
        protected void VoiceStateUpdated() {
            UpdateColor(voiceNetwork.connectionState, ref voiceIndicator);
        }
        protected void VoiceStateUpdated(short _) => VoiceStateUpdated();

        protected void OnHostChatroom() => Debug.Log("<color=blue>[FishyVoice]</color> We have hosted chatroom `" + agent.Network.CurrentChatroomName + "`");
        protected void OnJoinedChatroom(short _) => Debug.Log("<color=blue>[FishyVoice]</color> We have joined chatroom `" + agent.Network.CurrentChatroomName + "`");
        protected void OnLeftChatroom() => Debug.Log("<color=blue>[FishyVoice]</color> We have left our chatroom");
        protected void OnChatroomClose() => Debug.Log("<color=blue>[FishyVoice]</color> Our chatroom has been closed");

        protected void OnChatroomException(Exception e) => throw e;
    }
}
