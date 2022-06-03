using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Adrenak.UniVoice;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

// Component that provides a UniVoice ChatroomNetwork backed by the existing FishNetworking enviornment 
public class FishNetChatroomNetwork : EnchancedNetworkBehaviour, IChatroomNetwork {
    
    // Struct that we broadcast across the network, contains all of the audio data
    public struct ChatroomAudioBroadcast : IBroadcast {
        public short id;            // ClientId of the player who sent this audio
        public int segmentIndex;
        public int frequency;
        public int channelCount;
        public float[] samples;     
        
        // Extra fields must be at the bottom so that ChatroomAudioBroadcast and ChatroomAudioDTO have overlapping memory layouts
        public string roomName;     // Name of the room this audio is being sent to
        public uint tick;           // The tick this audio was sent on
    }
    
    // Dictionary mapping open room names to the list of players currently in the room
    [SyncObject] private readonly SyncDictionary<string, List<short>> openRooms = new SyncDictionary<string, List<short>>();
    
    // Name of the room that players join by default
    public const string DefaultRoomName = "<DEFAULT>";
    
    [Tooltip("When true data is only sent to people who can observer you, when false data is sent to everyone in the same room")]
    public bool enableProximityAudio = true;
    
    
    #region UniVoice Compatability/Callbacks
    
    // "Union" used to equate ChatroomAudioBroadcasts to UniVoice's own broadcast struct 
    [StructLayout(LayoutKind.Explicit)]
    private struct BroadcastUnion {
        [FieldOffset(0)]
        ChatroomAudioBroadcast broadcast;
        [FieldOffset(0)]
        ChatroomAudioDTO dto;

        static BroadcastUnion StaticRef = new BroadcastUnion();

        public static ChatroomAudioBroadcast ToBroadcast(ChatroomAudioDTO dto)
        {
            StaticRef.dto = dto;
            return StaticRef.broadcast;
        }
        public static ChatroomAudioDTO ToDTO(ChatroomAudioBroadcast broadcast)
        {
            StaticRef.broadcast = broadcast;
            return StaticRef.dto;
        }
    }
    
    [field: SerializeField, ReadOnly] public string CurrentChatroomName { protected set; get; } = DefaultRoomName;
    public short OwnID => (short) LocalConnection.ClientId; // TODO: Is this cast problematic?
    public List<short> PeerIDs => openRooms.ContainsKey(CurrentChatroomName) ? openRooms[CurrentChatroomName] : new List<short>(); // TODO: Needed?
    public event Action OnCreatedChatroom;
    public event Action<Exception> OnChatroomCreationFailed;
    public event Action OnlosedChatroom;
    public event Action<short> OnJoinedChatroom;
    public event Action<Exception> OnChatroomJoinFailed;
    public event Action OnLeftChatroom;
    public event Action<short> OnPeerJoinedChatroom;
    public event Action<short> OnPeerLeftChatroom;
    public event Action<ChatroomAudioDTO> OnAudioReceived;
    public event Action<ChatroomAudioDTO> OnAudioSent;
    
    #endregion
    

    // When the server starts, it begins hosting the default room
    public override void OnStartServer() {
        base.OnStartServer();
        HostChatroom(DefaultRoomName);
    }
    
    // When a client starts, it joins the default room
    public override void OnStartClient() {
        base.OnStartClient();
        JoinChatroom(DefaultRoomName);
    }

    // Call OnEnable once we have connected to the network, since OnEnable won't be able to find the correct managers on its first run
    public override void OnStartBoth() {
        base.OnStartBoth();
        OnEnable();
    }
    
    // OnEnable beings listening for audio
    private void OnEnable() {
        try {
            if (NetworkManager.IsServer) 
                ServerManager.RegisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedServer);
            if (NetworkManager.IsClient) 
                ClientManager.RegisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedClient);
        } catch(NullReferenceException) {}
    }

    // OnDisable stops listening for audio
    private void OnDisable() {
        try {
            if (NetworkManager.IsServer) 
                ServerManager.UnregisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedServer);
            if (NetworkManager.IsClient) 
                ClientManager.UnregisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedClient);
        } catch(NullReferenceException) {}
    }
    
    // Function called when audio is received on a client, simply forwards it to UniVoice
    void OnAudioBroadcastReceivedClient(ChatroomAudioBroadcast broadcastAudio) {
        // Ignore any audio not in the same chatroom
        if (broadcastAudio.roomName != CurrentChatroomName) return;
        
        // Debug.Log($"Received data from {broadcastAudio.id}");
        
        OnAudioReceived?.Invoke(BroadcastUnion.ToDTO(broadcastAudio));
    }
    
    // Function called when audio is received on the server, it figures out where the audio needs to be forwarded and then does so.
    void OnAudioBroadcastReceivedServer(NetworkConnection sender, ChatroomAudioBroadcast audio) {
        // Find all of the connections in the same room as the sender
        HashSet<NetworkConnection> recipients = new HashSet<NetworkConnection>();
        foreach (var ID in openRooms[audio.roomName])
            recipients.Add(NetworkManager.ServerManager.Clients[ID]);
        // Don't send data back to the sender
        recipients.Remove(sender);

        
        // If proximity audio is enabled, remove any people from the room who can't observe this player
        if (enableProximityAudio) {
            var ownedObject = sender.FirstObject;
            if (ownedObject is not null) {
                HashSet<NetworkConnection> observerRecipients = new HashSet<NetworkConnection>();
                
                foreach (var observer in ownedObject.Observers)
                    if (recipients.Contains(observer))
                        observerRecipients.Add(observer);
                
                recipients = observerRecipients;
            }
        }
        
        // Debug.Log($"[SERVER] Received data from {audio.id}");
        // foreach(var con in recipients)
        //     Debug.Log($"[SERVER] Forwarded data to {con.ClientId}");

        // Forward the received audio to every user we discovered
        ServerManager.Broadcast(recipients, audio, false, Channel.Unreliable);
    }

    public void Dispose() { /* I don't think anything needs to be destroyed! */ }

    // Function called when a new chatroom is to be connected to
    public void HostChatroom(string roomName) {
        if (!LocalConnection.IsActive) return;
        
        try {
            // Make sure the room doesn't already exist
            if (openRooms.ContainsKey(roomName)) {
                OnChatroomCreationFailed?.Invoke(new ArgumentException("Room `" + roomName + "` already exists"));
                return;
            }

            // Notify the server (and event listeners) that we have joined a new room
            HostChatroomServerRpc(OwnID, roomName);
            CurrentChatroomName = roomName;
            OnCreatedChatroom?.Invoke();
        } catch (Exception e) {
            OnChatroomCreationFailed?.Invoke(e);
        }
    }

    // RPC that notifies the server when a player joins a new room.
    [ServerRpc(RequireOwnership = false)]
    void HostChatroomServerRpc(short id, string roomName) {
        // Add the room to the list of rooms if it isn't already there
        if(!openRooms.ContainsKey(roomName))
            openRooms.Add(roomName, new List<short>());

        // NOTE: We must reassign the list for FishNet to notice that the list of players has changed
        var roomIDs = openRooms[roomName];
        roomIDs.Add(id);
        openRooms[roomName] = roomIDs;
    }

    // Function called when the room host closes a room
    public void CloseChatroom() {
        if (!LocalConnection.IsActive) return;
        CloseChatroomServerRpc(CurrentChatroomName);
    }

    // RPC that notifies the server when a room has been closed, the sever then notifies everyone in the room
    [ServerRpc(RequireOwnership = false)]
    void CloseChatroomServerRpc(string roomName) {
        openRooms.Remove(roomName);
        ChatroomClosedObserverRpc(roomName);
    }

    // RPC that notifies everyone in a room when the room is closed
    [ObserversRpc]
    void ChatroomClosedObserverRpc(string roomName) {
        if (roomName != CurrentChatroomName) return;

        CurrentChatroomName = DefaultRoomName;
        OnlosedChatroom?.Invoke();
        OnJoinedChatroom?.Invoke(OwnID);
    }

    // Function called to join a chatroom
    public void JoinChatroom(string roomName) {
        if (!LocalConnection.IsActive) return;
        
        // Make sure the room exists
        if (!openRooms.ContainsKey(roomName)) {
            OnChatroomJoinFailed?.Invoke(new ArgumentException("Room `" + roomName + "` doesn't exist!"));
            return;
        }

        // Notify the server that we have joined the room
        CurrentChatroomName = roomName;
        JoinChatroomServerRpc(OwnID, roomName);
        OnJoinedChatroom?.Invoke(OwnID);
    }

    // RPC that notifies the server (and all clients in the room) that we have joined a room
    [ServerRpc(RequireOwnership = false)]
    void JoinChatroomServerRpc(short id, string roomName) {
        var roomIDs = openRooms[roomName];
        roomIDs.Add(id);
        openRooms[roomName] = roomIDs;
        
        PeerJoinedChatroomObserverRpc(id, roomName);
    }

    // RPC that notifies a client that another client has joined a chat room
    [ObserversRpc]
    void PeerJoinedChatroomObserverRpc(short id, string roomName) {
        if (roomName != CurrentChatroomName) return;
        
        OnPeerJoinedChatroom?.Invoke(id);
    }

    // Function called to leave a room you are currently apart of
    // NOTE: It is a good idea to leave your current chatroom before joining or hosting another one
    public void LeaveChatroom() {
        if (!LocalConnection.IsActive) return;
        if (CurrentChatroomName == DefaultRoomName) return;
        
        LeaveChatroomServerRpc(OwnID, CurrentChatroomName);
        CurrentChatroomName = DefaultRoomName;
        OnLeftChatroom?.Invoke();
    }
    
    // RPC that notifies the server (and other peers in the room) that you have left the chatroom
    [ServerRpc(RequireOwnership = false)]
    void LeaveChatroomServerRpc(short id, string roomName) {
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
    void PeerLeftChatroomObserverRpc(short id, string roomName) {
        if (roomName != CurrentChatroomName) return;
        
        OnPeerLeftChatroom?.Invoke(id);
    }

    // Function called to send audio to your chatroom
    public void SendAudioSegment(ChatroomAudioDTO dtoData) {
        if (!LocalConnection.IsActive) return;
        
        // Add additional book keeping information to the data
        dtoData.id = OwnID;
        ChatroomAudioBroadcast data = BroadcastUnion.ToBroadcast(dtoData);
        data.roomName = CurrentChatroomName;
        data.tick = TimeManager.Tick;
        
        // Unreliably send it to the server
        ClientManager.Broadcast(data, Channel.Unreliable);
        OnAudioSent?.Invoke(dtoData);
    }
}
