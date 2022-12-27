using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.InbuiltImplementations;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using K4os.Compression.LZ4;
using TriInspector;
using UnityEngine;
using NetworkBehaviour = uMuVR.Enhanced.NetworkBehaviour;

namespace FishyVoice {

	// Component that provides a UniVoice ChatroomNetwork backed by the existing FishNetworking environment 
	[DisallowMultipleComponent]
	public class VoiceNetwork : NetworkBehaviour, IChatroomNetwork {
		// Static reference to the currently active VoiceNetwork
		public static VoiceNetwork instance;

		// Struct that we broadcast across the network, contains all of the audio data
		public struct AudioBroadcast : IBroadcast {
			public short id; // ClientId of the audio's receiver
			public int segmentIndex;
			public int frequency;
			public int channelCount;
			public float[] samples;

			// Extra fields must be at the bottom so that AudioBroadcast and ChatroomAudioDTO have overlapping memory layouts
			public string roomName; // Name of the room this audio is being sent to
			public uint tick; // The tick this audio was sent on
			public short senderID; // The client ID of the packet's sender
			public Vector3 senderPosition; // The position of the sender in the world
		}

		
		// Reference to the agent that is managing this network (set automatically by the create agent functions)
		// NOTE: If this is not set, positional audio will be automatically disabled
		public Agent agent = null;
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

			private static BroadcastUnion StaticRef;

			public static AudioBroadcast ToBroadcast(ChatroomAudioDTO dto) {
				StaticRef.dto = dto;
				return StaticRef.broadcast;
			}

			public static ChatroomAudioDTO ToDTO(AudioBroadcast broadcast) {
				StaticRef.broadcast = broadcast;
				return StaticRef.dto;
			}
		}

		[ShowInInspector] [ReadOnly] public string CurrentChatroomName { protected set; get; } = string.Empty;
		public short OwnID => (short)LocalConnection.ClientId; // TODO: Is this cast problematic?

		[ShowInInspector]
		[ListDrawerSettings(HideAddButton = true, HideRemoveButton = true)]
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
		public event Action<AudioBroadcast> OnAudioReceivedEnhanced;
		public event Action<ChatroomAudioDTO> OnAudioSent;

		#endregion


		public void Awake() {
			// Mark us as the new instance if there isn't already one, or delete us if there is
			if (instance is not null) {
				Destroy(gameObject);
				return;
			}

			instance = this;

			// If we are a child of the NetworkManager then remove our parent before anything starts
			if (GetComponentInParent<NetworkManager>() is null) return;

			// This is a hacky way of unparenting from NetworkManager and undoing DontDestroyOnLoad
			var newGO = new GameObject();
			transform.parent = newGO.transform;
			transform.parent = null;
			Destroy(newGO);
		}
		// Unmark the instance when the instance is destroyed
		public void OnDestroy() { if (instance == this) instance = null; }

		
		// OnDisable leaves a room when the voice network is disabled
		protected void OnDisable() {
			// If we are in a room be sure to leave
			if (connectionState == LocalConnectionState.Started)
				LeaveChatroom();
		}

		// void Update() {
		//     foreach(var id in PeerIDs)
		//         Debug.Log($"id = {id}");
		// }

		// Function called when audio is received on a client, simply forwards it to UniVoice
		[TargetRpc]
		protected void OnAudioBroadcastReceiveTargetRPC(NetworkConnection target, AudioBroadcast broadcastAudio) {
			// Ignore any audio not in the same chatroom
			if (broadcastAudio.roomName != CurrentChatroomName) return;

			// Debug.Log($"Received data from {broadcastAudio.senderID}");

			// If everything is in place for positional audio, update the position of the audio source
			if (!(float.IsNaN(broadcastAudio.senderPosition.x) || float.IsNaN(broadcastAudio.senderPosition.y) || float.IsNaN(broadcastAudio.senderPosition.z)))
				if ((agent?.PeerOutputs.ContainsKey(broadcastAudio.id) ?? false) && agent.PeerOutputs[broadcastAudio.id] is InbuiltAudioOutput iao) iao.AudioSource.transform.position = broadcastAudio.senderPosition;

			OnAudioReceivedEnhanced?.Invoke(broadcastAudio);
			OnAudioReceived?.Invoke(BroadcastUnion.ToDTO(broadcastAudio));
		}

		// Function called when audio is received on the server, it figures out where the audio needs to be forwarded and then does so.
		private static readonly Dictionary<NetworkConnection, uint> lastTickReceived = new(); // Dictionary used to track the timestamp of the latest received packet and then discard any packets that are earlier 
		[ServerRpc(RequireOwnership = false)]
		protected void OnAudioBroadcastReceivedServerRPC(NetworkConnection sender, AudioBroadcast audio) {
			// Make sure the targeted user is in the room (and data isn't forwarded back to the sender)
			if (audio.id == sender.ClientId) return;
			if (!ServerManager.Clients.ContainsKey(audio.id)) return;
			if (!openRooms.ContainsKey(audio.roomName)) return;
			if (!openRooms[audio.roomName].Contains(audio.id)) return;
			if (lastTickReceived.TryGetValue(sender, out var tick) && tick > audio.tick) return; // Ignore any old audio data

			audio.senderID = (short)sender.ClientId;
			// Debug.Log($"Forwarding data from {audio.senderID} to {audio.id}");

			// Update the last received tick
			lastTickReceived[sender] = audio.tick;
			// Forward the received audio to the targeted user
			OnAudioBroadcastReceiveTargetRPC(ServerManager.Clients[audio.id], audio);
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
		protected void HostChatroomServerRpc(short id, string roomName) {
			HostChatroomServer(id, roomName);
		}

		[Server]
		protected void HostChatroomServer(short id, string roomName) {
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

			if (IsServer) CloseChatroomServer(CurrentChatroomName);
			else CloseChatroomServerRpc(CurrentChatroomName);
		}

		// RPC that notifies the server when a room has been closed, the sever then notifies everyone in the room
		[ServerRpc(RequireOwnership = false)]
		protected void CloseChatroomServerRpc(string roomName) {
			CloseChatroomServer(roomName);
		}

		[Server]
		protected void CloseChatroomServer(string roomName) {
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
			if (IsServer) JoinChatroomServer(OwnID, roomName);
			else JoinChatroomServerRpc(OwnID, roomName);
			OnJoinedChatroom?.Invoke(OwnID);
		}

		// RPC that notifies the server (and all clients in the room) that we have joined a room
		[ServerRpc(RequireOwnership = false)]
		protected void JoinChatroomServerRpc(short id, string roomName) {
			JoinChatroomServer(id, roomName);
		}

		[Server]
		protected void JoinChatroomServer(short id, string roomName) {
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
		protected void LeaveChatroomServerRpc(short id, string roomName) {
			LeaveChatroomServer(id, roomName);
		}

		[Server]
		protected void LeaveChatroomServer(short id, string roomName) {
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
			var data = BroadcastUnion.ToBroadcast(dtoData);
			data.roomName = CurrentChatroomName;
			data.tick = TimeManager.Tick;
			data.senderID = (short)LocalConnection.ClientId;
			data.senderPosition = !(agent is null || PlayerPositionReference.instance is null) ? PlayerPositionReference.instance.position : new Vector3(float.NaN, float.NaN, float.NaN);

			// Unreliably send it to the server
			OnAudioBroadcastReceivedServerRPC(LocalConnection, data);
			OnAudioSent?.Invoke(dtoData);
		}


		#region CreateAgent

		// Creates a new ChatroomAgent using this network, given an audio input and an audio output factory
		public Agent CreateAgent(IAudioInput audioInput, IAudioOutputFactory audioOutputFactory) {
			var agent = new Agent(this, audioInput, audioOutputFactory) {
				MuteSelf = false
			};
			this.agent = agent; // Register this new agent as our agent
			return agent;
		}

		// Creates a new ChatroomAgent using this network, given an audio input (default audio output)
		public Agent CreateAgent(IAudioInput audioInput) => CreateAgent(audioInput, new InbuiltAudioOutputFactory());

		// Creates a new ChatroomAgent using this network, given an audio output factory (default audio input)
		public Agent CreateAgent(IAudioOutputFactory audioOutputFactory) {
			var input = new UniMicAudioInput(Mic.Instantiate());
			if (!Mic.Instance.IsRecording)
				Mic.Instance.StartRecording(16000, 100);

			return CreateAgent(input, audioOutputFactory);
		}

		// Creates a new ChatroomAgent using this network (default audio input and output)
		public Agent CreateAgent() => CreateAgent(new InbuiltAudioOutputFactory());

		#endregion
	}

	// Custom audio broadcast serializer that compresses the data
	public static class AudioBroadcastSerializer {
		private static readonly Vector3 Vec3NaN = new (float.NaN, float.NaN, float.NaN);
		private const LZ4Level CompressionLevel = LZ4Level.L00_FAST; // Is level 3 compression too slow?


		public static void WriteAudioBroadcast(this Writer writer, VoiceNetwork.AudioBroadcast data) {
#if !FISHYVOICE_DISABLE_AUDIO_COMPRESSION
			var byteWriter = new Writer();
#else
			var byteWriter = writer;
#endif
			var shouldSendPosition = !(float.IsNaN(data.senderPosition.x) || float.IsNaN(data.senderPosition.y) || float.IsNaN(data.senderPosition.z));
			byteWriter.WriteBoolean(shouldSendPosition);
			byteWriter.WriteInt16(data.id);
			byteWriter.WriteInt32(data.segmentIndex);
			byteWriter.WriteInt32(data.frequency);
			byteWriter.WriteInt32(data.channelCount);
			byteWriter.WriteArray(data.samples);
			byteWriter.WriteString(data.roomName);
			byteWriter.WriteUInt32(data.tick);
			byteWriter.WriteInt16(data.senderID);
			if(shouldSendPosition) byteWriter.WriteVector3(data.senderPosition);

			// Compress the raw data
#if !FISHYVOICE_DISABLE_AUDIO_COMPRESSION
			var compressed = new byte[LZ4Codec.MaximumOutputSize(byteWriter.Position)];
			var compressedLength = LZ4Codec.Encode(byteWriter.GetBuffer(), 0, byteWriter.Position, compressed, 0, compressed.Length, CompressionLevel);
			writer.WriteInt32(byteWriter.Position); // Start by sending the uncompressed size
			writer.WriteInt32(compressedLength);
			writer.WriteBytes(compressed, 0, compressedLength);
#endif
		}
		
		public static VoiceNetwork.AudioBroadcast ReadAudioBroadcast(this Reader reader) {
			// Decompress the raw data
#if !FISHYVOICE_DISABLE_AUDIO_COMPRESSION
			var uncompressedLength = reader.ReadInt32();
			var buffer = new byte[uncompressedLength];
			var compressed = readCompressedData(reader);
			if (LZ4Codec.Decode(compressed, buffer) < 0) throw new DecoderFallbackException("Failed to decode the packet");
			var byteReader = new Reader(buffer, null);
#else
			var byteReader = reader;
#endif

			var sentPosition = byteReader.ReadBoolean();
			return new VoiceNetwork.AudioBroadcast() {
				id = byteReader.ReadInt16(),
				segmentIndex = byteReader.ReadInt32(),
				frequency = byteReader.ReadInt32(),
				channelCount = byteReader.ReadInt32(),
				samples = byteReader.ReadArrayAllocated<float>(),
				roomName = byteReader.ReadString(),
				tick = byteReader.ReadUInt32(),
				senderID = byteReader.ReadInt16(),
				senderPosition = sentPosition ? byteReader.ReadVector3() : Vec3NaN
			};
		}

		private static byte[] readCompressedData(Reader reader) {
			var length = reader.ReadInt32();
			var compressed = new byte[length];
			reader.ReadBytes(ref compressed, length);
			return compressed;
		}
	}
}