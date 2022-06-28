using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
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
        protected LocalConnectionStates clientState = LocalConnectionStates.Stopped;

        /// <summary>
        /// Current state of server socket.
        /// </summary>
        protected LocalConnectionStates serverState = LocalConnectionStates.Stopped;

        #endregion

        void OnGUI() {
#if ENABLE_INPUT_SYSTEM
            string GetNextStateText(LocalConnectionStates state) {
                if (state == LocalConnectionStates.Stopped)
                    return "Start";
                else if (state == LocalConnectionStates.Starting)
                    return "Starting";
                else if (state == LocalConnectionStates.Stopping)
                    return "Stopping";
                else if (state == LocalConnectionStates.Started)
                    return "Stop";
                else
                    return "Invalid";
            }

            GUILayout.BeginArea(new Rect(16, 16, 256, 9000));
            Vector2 defaultResolution = new Vector2(1920f, 1080f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / defaultResolution.x, Screen.height / defaultResolution.y, 1));

            GUIStyle style = GUI.skin.GetStyle("button");
            int originalFontSize = style.fontSize;

            Vector2 buttonSize = new Vector2(256f, 64f);
            style.fontSize = 28;
            //Server button.
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                if (GUILayout.Button($"{GetNextStateText(serverState)} Server", GUILayout.Width(buttonSize.x),
                        GUILayout.Height(buttonSize.y)))
                    OnClick_Server();
                GUILayout.Space(10f);
            }

            //Client button.
            if (GUILayout.Button($"{GetNextStateText(clientState)} Client", GUILayout.Width(buttonSize.x),
                    GUILayout.Height(buttonSize.y)))
                OnClick_Client();

            style.fontSize = originalFontSize;

            GUILayout.EndArea();
#endif
        }

        protected void Start() {
#if !ENABLE_INPUT_SYSTEM
            EventSystem systems = FindObjectOfType<EventSystem>();
            if (systems is null)
                gameObject.AddComponent<EventSystem>();
            BaseInputModule inputModule = FindObjectOfType<BaseInputModule>();
            if (inputModule is null)
                gameObject.AddComponent<StandaloneInputModule>();
#else
            serverIndicator.transform.parent.gameObject.SetActive(false);
            clientIndicator.transform.parent.gameObject.SetActive(false);
#endif

            NetworkManager = FindObjectOfType<NetworkManager>();
            if (NetworkManager is null) {
                Debug.LogError("NetworkManager not found, HUD will not function.");
                return;
            }
            else {
                UpdateColor(LocalConnectionStates.Stopped, ref serverIndicator);
                UpdateColor(LocalConnectionStates.Stopped, ref clientIndicator);
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
        protected void UpdateColor(LocalConnectionStates state, ref Image img) {
            Color c;
            if (state == LocalConnectionStates.Started)
                c = startedColor;
            else if (state == LocalConnectionStates.Stopped)
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

            if (serverState != LocalConnectionStates.Stopped)
                NetworkManager.ServerManager.StopConnection(true);
            else
                NetworkManager.ServerManager.StartConnection();
        }


        public virtual void OnClick_Client() {
            if (NetworkManager is null)
                return;

            if (clientState != LocalConnectionStates.Stopped)
                NetworkManager.ClientManager.StopConnection();
            else
                NetworkManager.ClientManager.StartConnection();
        }
    }
}