using Adrenak.UniVoice;

namespace FishyVoice {
	public class Agent : ChatroomAgent {
		public Agent(IChatroomNetwork chatroomNetwork, IAudioInput audioInput, IAudioOutputFactory audioOutputFactory)
			: base(chatroomNetwork, audioInput, audioOutputFactory)
		{ }

		// Convenience wrapper functions around the Network operations
		public void HostChatroom(string roomName) => Network.HostChatroom(roomName);
		public void CloseChatroom() => Network.CloseChatroom();
		public void JoinChatroom(string roomName) => Network.JoinChatroom(roomName);
		public void LeaveChatroom() => Network.LeaveChatroom();
	}
}
