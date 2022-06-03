using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Adrenak.UniVoice;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;

public class FishNetChatroomNetwork : EnchancedNetworkBehaviour, IChatroomNetwork {
    
    struct ChatroomAudioBroadcast : IBroadcast {
        public short id;
        public int segmentIndex;
        public int frequency;
        public int channelCount;
        public float[] samples;
        
        // Name must be at the bottom so that ChatroomAudioBroadcast and ChatroomAudioDTO have overlapping memory layouts
        public string chatroomName;
    }
    
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

    public short OwnID => (short) LocalConnection.ClientId; // TODO: Is this cast problematic?
    public List<short> PeerIDs { get; } = new List<short>(); // TODO: Needed?
    public const string DefaultChatroomName = "<DEFAULT>";
    public string CurrentChatroomName { protected set; get; } = DefaultChatroomName;
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
    
    [SyncObject] private readonly SyncList<string> openRooms = new SyncList<string>();

    // Call OnEnable once we have connected to the network, since OnEnable won't be able to find the correct managers on its first run
    public override void OnStartBoth() {
        base.OnStartBoth();
        OnEnable();
    }
    
    private void OnEnable() {
        try {
            if (NetworkManager.IsServer) 
                ServerManager.RegisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedServer);
            else if (LocalConnection.IsActive) 
                ClientManager.RegisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedClient);
        } catch(NullReferenceException) {}
    }

    private void OnDisable() {
        try {
            if (NetworkManager.IsServer) 
                ServerManager.UnregisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedServer);
            else if (LocalConnection.IsActive) 
                ClientManager.UnregisterBroadcast<ChatroomAudioBroadcast>(OnAudioBroadcastReceivedClient);
        } catch(NullReferenceException) {}
    }
    
    void OnAudioBroadcastReceivedClient(ChatroomAudioBroadcast broadcastAudio) {
        // Ignore any audio not in the same chatroom
        if (broadcastAudio.chatroomName != CurrentChatroomName) return;
        
        OnAudioReceived?.Invoke(BroadcastUnion.ToDTO(broadcastAudio));
    }
    
    void OnAudioBroadcastReceivedServer(NetworkConnection sender, ChatroomAudioBroadcast audio) {
        var ownedObject = sender.FirstObject;
        if (ownedObject is null) return;
        
        // TODO: should we ignore audio belonging to an invalid chatroom?

        // Forward the received audio to every user who can observe the player this segment of audio originated from
        ServerManager.Broadcast(ownedObject, audio, false, Channel.Unreliable);
    }

    public void Dispose() { /* I don't think anything needs to be destroyed! */ }

    public void HostChatroom(string roomName) {
        if (!LocalConnection.IsActive) return;
        
        try {
            if (openRooms.Contains(roomName)) {
                OnChatroomCreationFailed?.Invoke(new ArgumentException("Room `" + roomName + "` already exists"));
                return;
            }

            HostChatroomServerRpc(roomName);
            CurrentChatroomName = roomName;
            OnCreatedChatroom?.Invoke();
        }
        catch (Exception e) {
            OnChatroomCreationFailed?.Invoke(e);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void HostChatroomServerRpc(string roomName) {
        // Add the room to the list of rooms if it isn't already there
        if(!openRooms.Contains(roomName))
            openRooms.Add(roomName);
    }

    public void CloseChatroom() {
        if (!LocalConnection.IsActive) return;
        CloseChatroomServerRpc(CurrentChatroomName);
    }

    [ServerRpc(RequireOwnership = false)]
    void CloseChatroomServerRpc(string roomName) {
        openRooms.Remove(roomName);
        ChatroomClosedObserverRpc(roomName);
    }

    [ObserversRpc]
    void ChatroomClosedObserverRpc(string roomName) {
        if (roomName != CurrentChatroomName) return;

        CurrentChatroomName = DefaultChatroomName;
        OnlosedChatroom?.Invoke();
        OnJoinedChatroom?.Invoke(OwnID);
    }

    public void JoinChatroom(string roomName) {
        if (!LocalConnection.IsActive) return;
        
        if (!openRooms.Contains(roomName)) {
            OnChatroomJoinFailed?.Invoke(new ArgumentException("Room `" + roomName + "` doesn't exist!"));
            return;
        }

        CurrentChatroomName = roomName;
        JoinChatroomServerRpc(OwnID, roomName);
        OnJoinedChatroom?.Invoke(OwnID);
    }

    [ServerRpc(RequireOwnership = false)]
    void JoinChatroomServerRpc(short id, string roomName) {
        PeerJoinedChatroomObserverRpc(id, roomName);
    }

    [ObserversRpc]
    void PeerJoinedChatroomObserverRpc(short id, string roomName) {
        if (roomName != CurrentChatroomName) return;
        
        OnPeerJoinedChatroom?.Invoke(id);
    }

    public void LeaveChatroom() {
        if (!LocalConnection.IsActive) return;
        if (CurrentChatroomName == DefaultChatroomName) return;
        
        LeaveChatroomServerRpc(OwnID, CurrentChatroomName);
        CurrentChatroomName = DefaultChatroomName;
        OnLeftChatroom?.Invoke();
    }
    
    [ServerRpc(RequireOwnership = false)]
    void LeaveChatroomServerRpc(short id, string roomName) {
        PeerLeftChatroomObserverRpc(id, roomName);
    }

    [ObserversRpc]
    void PeerLeftChatroomObserverRpc(short id, string roomName) {
        if (roomName != CurrentChatroomName) return;
        
        OnPeerLeftChatroom?.Invoke(id);
    }

    public void SendAudioSegment(ChatroomAudioDTO dtoData) {
        if (!LocalConnection.IsActive) return;
        
        dtoData.id = OwnID;
        ChatroomAudioBroadcast data = BroadcastUnion.ToBroadcast(dtoData);
        data.chatroomName = CurrentChatroomName;
        
        ClientManager.Broadcast(data, Channel.Unreliable);
        OnAudioSent?.Invoke(dtoData);
    }
}
