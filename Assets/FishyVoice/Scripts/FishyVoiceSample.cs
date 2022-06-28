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
        public string roomName = FishyVoice.VoiceNetwork.DefaultRoomName;
        
        // Variable indicating the current connection state of the voice network
        protected LocalConnectionStates voiceState => voiceNetwork?.connectionState ?? LocalConnectionStates.Stopped;
        // Voice network reference
        protected FishyVoice.VoiceNetwork voiceNetwork;
        // Agent that participates in the voice network
        protected FishyVoice.Agent agent;

        private void Awake(){
            voiceNetwork = FindObjectOfType<FishyVoice.VoiceNetwork>();
            if(voiceNetwork is null)
                Debug.LogError("Voice Network object not found, voice connectivity will not work!");
        }

        private static string GetNextStateText(LocalConnectionStates state) {
            return state switch {
                LocalConnectionStates.Stopped => "Start",
                LocalConnectionStates.Starting => "Starting",
                LocalConnectionStates.Stopping => "Stopping",
                LocalConnectionStates.Started => "Stop",
                _ => "Invalid"
            };
        }

        private void OnGUI() {
#if ENABLE_INPUT_SYSTEM
            GUILayout.BeginArea(new Rect(16, 16, 256, 9000));
            var defaultResolution = new Vector2(1920f, 1080f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / defaultResolution.x, Screen.height / defaultResolution.y, 1));
    
            var style = GUI.skin.GetStyle("button");
            var originalFontSize = style.fontSize;
    
            var buttonSize = new Vector2(256f, 64f);
            style.fontSize = 28;
            //Server button.
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                if (GUILayout.Button($"{GetNextStateText(serverState)} Server", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                    OnClick_Server();
                GUILayout.Space(10f);
            }
    
            //Client button.
            if (GUILayout.Button($"{GetNextStateText(clientState)} Client", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                OnClick_Client();
            GUILayout.Space(10f);
            
            //Voice button.
            if(voiceNetwork is not null && NetworkManager is not null && NetworkManager.IsClient)
                if (GUILayout.Button($"{GetNextStateText(voiceState)} Voice", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                    OnClick_Voice();
    
            style.fontSize = originalFontSize;
    
            GUILayout.EndArea();
#endif
        }

        public new void Start() {
            base.Start();
            
#if ENABLE_INPUT_SYSTEM
            voiceIndicator.transform.parent.gameObject.SetActive(false);
#endif
            
            // Listen for changes to the client state
            NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            
            UpdateColor(LocalConnectionStates.Stopped, ref voiceIndicator);
            
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
#if !ENABLE_INPUT_SYSTEM
            voiceIndicator.transform.parent.gameObject.SetActive(args.ConnectionState == LocalConnectionStates.Started);
#endif
        }

        public override void OnClick_Server() {
            if (NetworkManager is null)
                return;

            if (serverState != LocalConnectionStates.Stopped) {
                NetworkManager.ServerManager.OnServerConnectionState -= OnServerStart;
                NetworkManager.ServerManager.StopConnection(true);
            } else {
                NetworkManager.ServerManager.StartConnection();
                NetworkManager.ServerManager.OnServerConnectionState += OnServerStart;
            }
        }

        public void OnServerStart(ServerConnectionStateArgs args) {
            if (args.ConnectionState != LocalConnectionStates.Started) return;

            // The server needs to keep a room running, but it shouldn't participate in the room unless it is a host and the client part wishes to
            agent.HostChatroom(roomName);
            // agent.MuteOthers = true;
            // agent.MuteSelf = true;
            voiceNetwork.connectionState = LocalConnectionStates.Stopped;
        }

        public override void OnClick_Client() {
            if (NetworkManager is null)
                return;
            
            if(voiceState == LocalConnectionStates.Started && clientState == LocalConnectionStates.Started) OnClick_Voice();
            
            base.OnClick_Client();
        }

        public virtual void OnClick_Voice() {
            if (NetworkManager is null) return;
            if (!NetworkManager.IsClient) return;

            // Turning voice on or off equates to joining or leaving a chatroom
            if(voiceNetwork.connectionState == LocalConnectionStates.Started)
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
