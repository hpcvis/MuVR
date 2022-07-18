using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.InbuiltImplementations;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using TriInspector;

namespace FishyVoice {
    // Component that provides a UniVoice ChatroomNetwork backed by the existing FishNetworking environment 
    [DisallowMultipleComponent]
    public class VoiceNetwork : MuVR.Enchanced.NetworkBehaviour, IChatroomNetwork {

        // Struct that we broadcast across the network, contains all of the audio data
        public struct AudioBroadcast : IBroadcast {
            public short id; // ClientId of the player who sent this audio
            public int segmentIndex;
            public int frequency;
            public int channelCount;
            public float[] samples;

            // Extra fields must be at the bottom so that AudioBroadcast and ChatroomAudioDTO have overlapping memory layouts
            public string roomName; // Name of the room this audio is being sent to
            public uint tick; // The tick this audio was sent on
            public int senderID; // The client ID of the packet's sender
        }


        // Bool property checking if the network is currently active
        protected bool networkActive => (IsServer && ServerManager.Started) || LocalConnection.IsActive;

        // Variable tracking the current connection state of the network (connected? disconnected?)
        [HideInInspector] public LocalConnectionState connectionState = LocalConnectionState.Stopped;

        // Dictionary mapping open room names to the list of players currently in the room
        [SyncObject] protected readonly SyncDictionary<string, List<short>> openRooms = new();


        #region UniVoice Compatability/Callbacks

        // "Union" used to equate AudioBroadcasts to UniVoice's own broadcast struct 
        [StructLayout(LayoutKind.Explicit)]
        protected struct BroadcastUnion {
            [FieldOffset(0)] private AudioBroadcast broadcast;
            [FieldOffset(0)] private ChatroomAudioDTO dto;

            static BroadcastUnion StaticRef = new BroadcastUnion();

            public static AudioBroadcast ToBroadcast(ChatroomAudioDTO dto) {
                StaticRef.dto = dto;
                return StaticRef.broadcast;
            }

            public static ChatroomAudioDTO ToDTO(AudioBroadcast broadcast) {
                StaticRef.broadcast = broadcast;
                return StaticRef.dto;
            }
        }

        [ShowInInspector, ReadOnly] public string CurrentChatroomName { protected set; get; } = string.Empty;
        public short OwnID => (short)LocalConnection.ClientId; // TODO: Is this cast problematic?

        [ShowInInspector, ListDrawerSettings(HideAddButton = true, HideRemoveButton = true)]
        public List<short> PeerIDs => openRooms.ContainsKey(CurrentChatroomName)
            ? openRooms[CurrentChatroomName]
            : new List<short>();

        public event Action OnCreatedChatroom;
        public event Action<Exception> OnChatroomCreationFailed;
        public event Action OnClosedChatroom;
        public event Action<short> OnJoinedChatroom;
        public event Action<Exception> OnChatroomJoinFailed;
        public event Action OnLeftChatroom;
        public event Action<short> OnPeerJoinedChatroom;
        public event Action<short> OnPeerLeftChatroom;
        public event Action<ChatroomAudioDTO> OnAudioReceived;
        public event Action<ChatroomAudioDTO> OnAudioSent;

        #endregion

        // If we are a child of the NetworkManager then remove our parent before anything starts
        public void Awake() {
            if (GetComponentInParent<NetworkManager>() is null) return;

            // This is a hacky way of undoing DontDestroyOnLoad
            var newGO = new GameObject();
            transform.parent = newGO.transform;
            transform.parent = null;
            Destroy(newGO);
        }

        // Call OnEnable once we have connected to the network, since OnEnable won't be able to find the correct managers on its first run
        public override void OnStartBoth() {
            base.OnStartBoth();
            OnEnable();
        }

        // OnEnable beings listening for audio
        protected void OnEnable() {
            try {
                if (IsServer)
                    ServerManager.RegisterBroadcast<AudioBroadcast>(OnAudioBroadcastReceivedServer);
                if (IsClient)
                    ClientManager.RegisterBroadcast<AudioBroadcast>(OnAudioBroadcastReceivedClient);
            } catch (NullReferenceException) { }
        }

        // OnDisable stops listening for audio
        protected void OnDisable() {
            try {
                if (IsServer)
                    ServerManager.UnregisterBroadcast<AudioBroadcast>(OnAudioBroadcastReceivedServer);
                if (IsClient)
                    ClientManager.UnregisterBroadcast<AudioBroadcast>(OnAudioBroadcastReceivedClient);
            } catch (NullReferenceException) { }
            
            // If we are in a room be sure to leave
            if(connectionState == LocalConnectionState.Started)
                LeaveChatroom();
        }

        // void Update() {
        //     foreach(var id in PeerIDs)
        //         Debug.Log($"id = {id}");
        // }

        // Function called when audio is received on a client, simply forwards it to UniVoice
        protected void OnAudioBroadcastReceivedClient(AudioBroadcast broadcastAudio) {
            // Ignore any audio not in the same chatroom
            if (broadcastAudio.roomName != CurrentChatroomName) return;

            // Debug.Log($"Received data from {broadcastAudio.senderID}");

            OnAudioReceived?.Invoke(BroadcastUnion.ToDTO(broadcastAudio));
        }

        // Function called when audio is received on the server, it figures out where the audio needs to be forwarded and then does so.
        void OnAudioBroadcastReceivedServer(NetworkConnection sender, AudioBroadcast audio) {
            // Make sure the targeted user is in the room (and data isn't forwarded back to the sender)
            if (audio.id == sender.ClientId) return;
            if (!ServerManager.Clients.ContainsKey(audio.id)) return;
            if (!openRooms.ContainsKey(audio.roomName)) return;
            if (!openRooms[audio.roomName].Contains(audio.id)) return;

            audio.senderID = sender.ClientId;
            // Debug.Log($"Forwarding data from {audio.senderID} to {audio.id}");

            // Forward the received audio to the targeted user
            ServerManager.Broadcast(ServerManager.Clients[audio.id], audio, false, Channel.Reliable);
        }

        public void Dispose() { /* I don't think anything needs to be destroyed! */ }

        // Function called when a new chatroom is to be connected to
        public void HostChatroom(string roomName) {
            if (!networkActive) throw new Exception("The network is not active!");

            try {
                // Make sure the room doesn't already exist
                if (openRooms.ContainsKey(roomName)) {
                    OnChatroomCreationFailed?.Invoke(new ArgumentException("Room `" + roomName + "` already exists"));
                    return;
                }

                // Notify the server (and event listeners) that we have created & joined a new room
                if (IsServer) HostChatroomServer(OwnID, roomName);
                else HostChatroomServerRpc(OwnID, roomName);
                CurrentChatroomName = roomName;
                connectionState = LocalConnectionState.Started;
                OnCreatedChatroom?.Invoke();
                OnJoinedChatroom?.Invoke(OwnID);
            } catch (Exception e) {
                OnChatroomCreationFailed?.Invoke(e);
            }
        }

        // RPC that notifies the server when a player joins a new room.
        [ServerRpc(RequireOwnership = false)]
        protected void HostChatroomServerRpc(short id, string roomName) => HostChatroomServer(id, roomName);
        [Server] protected void HostChatroomServer(short id, string roomName) {
            // Add the room to the list of rooms if it isn't already there
            if (!openRooms.ContainsKey(roomName))
                openRooms.Add(roomName, new List<short>());

            // NOTE: We must reassign the list for FishNet to notice that the list of players has changed
            var roomIDs = openRooms[roomName];
            roomIDs.Add(id);
            openRooms[roomName] = roomIDs;
        }

        // Function called when the room host closes a room
        public void CloseChatroom() {
            if (!networkActive) throw new Exception("The network is not active!");
            
            if(IsServer) CloseChatroomServer(CurrentChatroomName);
            else CloseChatroomServerRpc(CurrentChatroomName);
        }

        // RPC that notifies the server when a room has been closed, the sever then notifies everyone in the room
        [ServerRpc(RequireOwnership = false)]
        protected void CloseChatroomServerRpc(string roomName) => CloseChatroomServer(roomName);
        [Server] protected void CloseChatroomServer(string roomName) {
            openRooms.Remove(roomName);
            ChatroomClosedObserverRpc(roomName);
        }

        // RPC that notifies everyone in a room when the room is closed
        [ObserversRpc]
        protected void ChatroomClosedObserverRpc(string roomName) {
            if (roomName != CurrentChatroomName) return;

            connectionState = LocalConnectionState.Stopped;
            OnClosedChatroom?.Invoke();
            CurrentChatroomName = string.Empty;
        }

        // Function called to join a chatroom
        public void JoinChatroom(string roomName) {
            if (!networkActive) throw new Exception("The network is not active!");

            // Make sure the room exists
            if (!openRooms.ContainsKey(roomName)) {
                OnChatroomJoinFailed?.Invoke(new ArgumentException("Room `" + roomName + "` doesn't exist!"));
                return;
            }

            // Notify the server that we have joined the room
            CurrentChatroomName = roomName;
            connectionState = LocalConnectionState.Started;
            if(IsServer) JoinChatroomServer(OwnID, roomName); 
            else JoinChatroomServerRpc(OwnID, roomName);
            OnJoinedChatroom?.Invoke(OwnID);
        }

        // RPC that notifies the server (and all clients in the room) that we have joined a room
        [ServerRpc(RequireOwnership = false)]
        protected void JoinChatroomServerRpc(short id, string roomName) => JoinChatroomServer(id, roomName);
        [Server] protected void JoinChatroomServer(short id, string roomName){
            var roomIDs = openRooms[roomName];
            roomIDs.Add(id);
            openRooms[roomName] = roomIDs;
            
            PeerJoinedChatroomObserverRpc(id, roomName);
        }

        // RPC that notifies a client that another client has joined a chat room
        [ObserversRpc]
        protected void PeerJoinedChatroomObserverRpc(short id, string roomName) {
            if (roomName != CurrentChatroomName) return;
            
            OnPeerJoinedChatroom?.Invoke(id);
        }

        // Function called to leave a room you are currently apart of
        // NOTE: It is a good idea to leave your current chatroom before joining or hosting another one
        public void LeaveChatroom() {
            if (!networkActive) throw new Exception("The network is not active!");
            if (CurrentChatroomName == string.Empty) return;
            
            LeaveChatroomServerRpc(OwnID, CurrentChatroomName);
            connectionState = LocalConnectionState.Stopped;
            OnLeftChatroom?.Invoke();
            CurrentChatroomName = string.Empty;
        }
        
        // RPC that notifies the server (and other peers in the room) that you have left the chatroom
        [ServerRpc(RequireOwnership = false)]
        protected void LeaveChatroomServerRpc(short id, string roomName) => LeaveChatroomServer(id, roomName);
        [Server] protected void LeaveChatroomServer(short id, string roomName){
            var roomIDs = openRooms[roomName];
            roomIDs.Remove(id);
            openRooms[roomName] = roomIDs;

            PeerLeftChatroomObserverRpc(id, roomName);
            
            // If the room no longer has any players, close it
            if (roomIDs.Count == 0)
                openRooms.Remove(roomName);
        }

        // RPC that notifies another player that you have left their chatroom
        [ObserversRpc]
        protected void PeerLeftChatroomObserverRpc(short id, string roomName) {
            if (roomName != CurrentChatroomName) return;
            
            OnPeerLeftChatroom?.Invoke(id);
        }

        // Function called to send audio to your chatroom
        public void SendAudioSegment(ChatroomAudioDTO dtoData) {
            if (!networkActive) throw new Exception("The network is not active!");
            
            // Debug.Log($"Sending from {LocalConnection.ClientId} to {dtoData.id}");
            
            // Add additional book keeping information to the data
            AudioBroadcast data = BroadcastUnion.ToBroadcast(dtoData);
            data.roomName = CurrentChatroomName;
            data.tick = TimeManager.Tick;
            
            // Unreliably send it to the server
            ClientManager.Broadcast(data, Channel.Reliable);
            OnAudioSent?.Invoke(dtoData);
        }
        
        
        
        #region CreateAgent

        // Creates a new ChatroomAgent using this network, given an audio input and an audio output factory
        public FishyVoice.Agent CreateAgent(IAudioInput audioInput, IAudioOutputFactory audioOutputFactory) =>
            new FishyVoice.Agent(this, audioInput, audioOutputFactory) {
                MuteSelf = false
            };

        // Creates a new ChatroomAgent using this network, given an audio input (default audio output)
        public FishyVoice.Agent CreateAgent(IAudioInput audioInput) => CreateAgent(audioInput, new InbuiltAudioOutputFactory());
        // Creates a new ChatroomAgent using this network, given an audio output factory (default audio input)
        public FishyVoice.Agent CreateAgent(IAudioOutputFactory audioOutputFactory) {
            var input = new UniMicAudioInput(Mic.Instantiate());
            if (!Mic.Instance.IsRecording)
                Mic.Instance.StartRecording(16000, 100);

            return CreateAgent(input, audioOutputFactory);
        }
        // Creates a new ChatroomAgent using this network (default audio input and output)
        public FishyVoice.Agent CreateAgent() => CreateAgent(new InbuiltAudioOutputFactory());

        #endregion
    }
}

