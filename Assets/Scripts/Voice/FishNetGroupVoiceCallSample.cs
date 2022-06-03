using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using UnityEngine.UI;
using Adrenak.UniVoice.InbuiltImplementations;
using Adrenak.UniVoice.Samples;
using FishNet.Object;

[RequireComponent(typeof(FishNetChatroomNetwork))]
public class FishNetGroupVoiceCallSample : NetworkBehaviour {
    public GameObject audioPanel;
    
    [Header("Menu")]
    public GameObject menuGO;
    public InputField inputField;
    public Button hostButton;
    public Button joinButton;
    public Button exitButton;
    public Text menuMessage;

    [Header("Chatroom")]
    public GameObject chatroomGO;
    public Transform peerViewContainer;
    public PeerView peerViewTemplate;
    public Text chatroomMessage;
    public Toggle muteSelfToggle;
    public Toggle muteOthersToggle;

    FishNetChatroomNetwork network;
    ChatroomAgent agent;
    Dictionary<short, PeerView> peerViews = new Dictionary<short, PeerView>();

    private void Awake() {
        network = GetComponent<FishNetChatroomNetwork>();
        audioPanel.SetActive(false);
    }

    public override void OnStartClient() {
        base.OnStartClient();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        InitializeInput();
        InitializeAgent();

        audioPanel.SetActive(true);
        menuGO.SetActive(true);
        chatroomGO.SetActive(false);
        muteSelfToggle.SetIsOnWithoutNotify(!agent.MuteSelf);
        muteSelfToggle.onValueChanged.AddListener(value =>
            agent.MuteSelf = !value);

        muteOthersToggle.SetIsOnWithoutNotify(!agent.MuteOthers);
        muteOthersToggle.onValueChanged.AddListener(value =>
            agent.MuteOthers = !value);
    }

    void InitializeInput() {
        hostButton.onClick.AddListener(HostChatroom);
        joinButton.onClick.AddListener(JoinChatroom);
        exitButton.onClick.AddListener(ExitChatroom);
    }

    void InitializeAgent() {
        var input = new UniMicAudioInput(Mic.Instance);
        if (!Mic.Instance.IsRecording)
            Mic.Instance.StartRecording(16000, 100);

        agent = new ChatroomAgent(network, input, new InbuiltAudioOutputFactory()) {
            MuteSelf = false
        };

        // HOSTING
        agent.Network.OnCreatedChatroom += () => {
            var chatroomName = agent.Network.CurrentChatroomName;
            ShowMessage($"Chatroom \"{chatroomName}\" created!\n" +
            $" You are Peer ID 0");
            menuGO.SetActive(false);
            chatroomGO.SetActive(true);
        };

        agent.Network.OnChatroomCreationFailed += ex => {
            ShowMessage("Chatroom creation failed");
        };

        agent.Network.OnlosedChatroom += () => {
            ShowMessage("You closed the chatroom! All peers have been kicked");
            menuGO.SetActive(true);
            chatroomGO.SetActive(false);
        };

        // JOINING
        agent.Network.OnJoinedChatroom += id => {
            if (agent.Network.CurrentChatroomName == FishNetChatroomNetwork.DefaultChatroomName)
                return;
            
            var chatroomName = agent.Network.CurrentChatroomName;
            ShowMessage("Joined chatroom " + chatroomName);
            ShowMessage("You are Peer ID " + id);

            menuGO.SetActive(false);
            chatroomGO.SetActive(true);
        };

        agent.Network.OnChatroomJoinFailed += ex => {
            ShowMessage(ex);
        };

        agent.Network.OnLeftChatroom += () => {
            ShowMessage("You left the chatroom");

            menuGO.SetActive(true);
            chatroomGO.SetActive(false);
        };

        // PEERS
        agent.Network.OnPeerJoinedChatroom += id => {
            var view = Instantiate(peerViewTemplate, peerViewContainer);
            view.IncomingAudio = !agent.PeerSettings[id].muteThem;
            view.OutgoingAudio = !agent.PeerSettings[id].muteSelf;

            view.OnIncomingModified += value =>
                agent.PeerSettings[id].muteThem = !value;

            view.OnOutgoingModified += value =>
                agent.PeerSettings[id].muteSelf = !value;

            peerViews.Add(id, view);
            view.SetPeerID(id);
        };

        agent.Network.OnPeerLeftChatroom += id => {
            var peerViewInstance = peerViews[id];
            Destroy(peerViewInstance.gameObject);
            peerViews.Remove(id);
        };
    }

    void Update() {
        if (agent is null) return;
        
        foreach (var output in agent.PeerOutputs) {
            if (peerViews.ContainsKey(output.Key)) {
                /*
                 * This is an inefficient way of showing a part of the 
                 * audio source spectrum. AudioSource.GetSpectrumData returns
                 * frequency values up to 24000 Hz in some cases. Most human
                 * speech is no more than 5000 Hz. Showing the entire spectrum
                 * will therefore lead to a spectrum where much of it doesn't
                 * change. So we take only the spectrum frequencies between
                 * the average human vocal range.
                 * 
                 * Great source of information here: 
                 * http://answers.unity.com/answers/158800/view.html
                 */
                var size = 512;
                var minVocalFrequency = 50;
                var maxVocalFrequency = 8000;
                var sampleRate = AudioSettings.outputSampleRate;
                var frequencyResolution = sampleRate / 2 / size;

                var audioSource = (output.Value as InbuiltAudioOutput).AudioSource;
                var spectrumData = new float[size];                    
                audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

                var indices = Enumerable.Range(0, size - 1).ToList();
                var minVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - minVocalFrequency), x)).x;
                var maxVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - maxVocalFrequency), x)).x;
                var indexRange = maxVocalFrequencyIndex - minVocalFrequency;

                spectrumData = spectrumData.Select(x => 1000 * x)
                    .ToList()
                    .GetRange(minVocalFrequency, indexRange)
                    .ToArray();
                peerViews[output.Key].DisplaySpectrum(spectrumData);
            }
        }
    }

    void HostChatroom() {
        var roomName = inputField.text;
        Debug.Log("Hosting: " + roomName);
        agent.Network.HostChatroom(roomName);
    }

    void JoinChatroom() {
        var roomName = inputField.text;
        agent.Network.JoinChatroom(roomName);
    }

    void ExitChatroom() {
        Debug.Log(agent.CurrentMode);
        if (agent.CurrentMode == ChatroomAgentMode.Host)
            agent.Network.CloseChatroom();
        else if (agent.CurrentMode == ChatroomAgentMode.Guest)
            agent.Network.LeaveChatroom();
    }

    void ShowMessage(object obj) {
        Debug.Log("<color=blue>" + obj + "</color>");
        menuMessage.text = obj.ToString();
        if (agent.CurrentMode != ChatroomAgentMode.Unconnected)
            chatroomMessage.text = obj.ToString();
    }
}
