using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishyVoice {
    public class NetworkHudCanvases : MonoBehaviour {
        #region Types.

        /// <summary>
        /// Ways the HUD will automatically start a connection.
        /// </summary>
        protected enum AutoStartType {
            Disabled,
            Host,
            Server,
            Client
        }

        #endregion

        #region Serialized.

        /// <summary>
        /// What connections to automatically start on play.
        /// </summary>
        [Tooltip("What connections to automatically start on play.")] [SerializeField]
        protected AutoStartType autoStartType = AutoStartType.Disabled;

        /// <summary>
        /// Color when socket is stopped.
        /// </summary>
        [Tooltip("Color when socket is stopped.")] [SerializeField]
        protected Color stoppedColor;

        /// <summary>
        /// Color when socket is changing.
        /// </summary>
        [Tooltip("Color when socket is changing.")] [SerializeField]
        protected Color changingColor;

        /// <summary>
        /// Color when socket is started.
        /// </summary>
        [Tooltip("Color when socket is started.")] [SerializeField]
        protected Color startedColor;

        [Header("Indicators")]
        /// <summary>
        /// Indicator for server state.
        /// </summary>
        [Tooltip("Indicator for server state.")]
        [SerializeField]
        protected Image serverIndicator;

        /// <summary>
        /// Indicator for client state.
        /// </summary>
        [Tooltip("Indicator for client state.")] [SerializeField]
        protected Image clientIndicator;

        #endregion

        #region Protected.

        /// <summary>
        /// Found NetworkManager.
        /// </summary>
        protected NetworkManager NetworkManager;

        /// <summary>
        /// Current state of client socket.
        /// </summary>
        protected LocalConnectionState clientState = LocalConnectionState.Stopped;

        /// <summary>
        /// Current state of server socket.
        /// </summary>
        protected LocalConnectionState serverState = LocalConnectionState.Stopped;

        #endregion
        

        protected void Start() {

            var systems = FindObjectOfType<EventSystem>();
            if (systems is null)
                gameObject.AddComponent<EventSystem>();

            var inputModule = FindObjectOfType<InputSystemUIInputModule>();
            if (inputModule is null)

#if ENABLE_INPUT_SYSTEM
                gameObject.AddComponent<InputSystemUIInputModule>();
#else
                gameObject.AddComponent<StandaloneInputModule>();
#endif

            NetworkManager = FindObjectOfType<NetworkManager>();
            if (NetworkManager is null) {
                Debug.LogError("NetworkManager not found, HUD will not function.");
                return;
            } else {
                UpdateColor(LocalConnectionState.Stopped, ref serverIndicator);
                UpdateColor(LocalConnectionState.Stopped, ref clientIndicator);
                NetworkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
                NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            }

            if (autoStartType == AutoStartType.Host || autoStartType == AutoStartType.Server)
                OnClick_Server();
            if (!Application.isBatchMode &&
                (autoStartType == AutoStartType.Host || autoStartType == AutoStartType.Client))
                OnClick_Client();
        }


        protected void OnDestroy() {
            if (NetworkManager is null)
                return;

            NetworkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }

        /// <summary>
        /// Updates img color baased on state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="img"></param>
        protected void UpdateColor(LocalConnectionState state, ref Image img) {
            Color c;
            if (state == LocalConnectionState.Started)
                c = startedColor;
            else if (state == LocalConnectionState.Stopped)
                c = stoppedColor;
            else
                c = changingColor;

            img.color = c;
        }


        protected void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj) {
            clientState = obj.ConnectionState;
            UpdateColor(obj.ConnectionState, ref clientIndicator);
        }


        protected void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj) {
            serverState = obj.ConnectionState;
            UpdateColor(obj.ConnectionState, ref serverIndicator);
        }


        public virtual void OnClick_Server() {
            if (NetworkManager is null)
                return;

            if (serverState != LocalConnectionState.Stopped)
                NetworkManager.ServerManager.StopConnection(true);
            else
                NetworkManager.ServerManager.StartConnection();
        }


        public virtual void OnClick_Client() {
            if (NetworkManager is null)
                return;

            if (clientState != LocalConnectionState.Stopped)
                NetworkManager.ClientManager.StopConnection();
            else
                NetworkManager.ClientManager.StartConnection();
        }
    }
}