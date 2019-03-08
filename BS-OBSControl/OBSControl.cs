using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using CommandPluginLib;

namespace BS_OBSControl
{
    class OBSControl : MonoBehaviour , ICommandPlugin
    {
        public string PluginName => Plugin.PluginName; // Name that identifies this plugin as a source/destination
        public const string Counterpart = "OBSControl"; // Destination plugin on Command-Server
        private Dictionary<string, Action<object, string>> _commands;

        /// <summary>
        /// Dictionary of commands this plugin can receive.
        /// </summary>
        public Dictionary<string, Action<object,string>> Commands
        {
            get
            {
                if (_commands == null)
                    _commands = new Dictionary<string, Action<object, string>>();
                return _commands;
            }
        }

        /// <summary>
        /// Messages to be sent to the server.
        /// </summary>
        public event Action<object, MessageData> MessageReady;

        public void Initialize()
        {
            Logger.Trace("OBSControl Initialize()");
            BuildCommands();
        }

        private void BuildCommands()
        {
            Logger.Trace("BuildCommands()");
            //Commands.AddSafe(CommandKeys.Key_StartRecord, TryStartRecording);
            //Commands.AddSafe(CommandKeys.Key_StopRecord, TryStopRecording);
            Commands.AddSafe(CommandKeys.Key_RecordStatus, OnRecordStatusChange);
        }

        public void OnMessage(object sender, MessageData e)
        {
            Logger.Trace($"Received message:\n{e.ToString()}");
            if (e.Destination == PluginName)
                Commands[e.Flag](sender, e.Data);
            else
                Logger.Debug($"Discarding message, we are not the destination");
        }

        public void Awake()
        {
            Logger.Trace("OBSControl GameObject Awake()");
            BuildCommands();
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

        public void Start()
        {
            if (PlayerSettings != null)
                PlayerHeight = PlayerSettings.playerHeight;
            else
                PlayerHeight = 1.8f;
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {

            if (newScene.name == "Menu")
            {
                //Code to execute when entering The Menu
                Logger.Debug("In menu");
                TryStopRecording();

            }

            if (newScene.name == "GameCore")
            {
                //Code to execute when entering actual gameplay
                Logger.Debug("In GameCore");
                TryStartRecording();

            }
        }

        
        public void TryStartRecording(string fileFormat = "")
        {
            
            var message = new MessageData(PluginName,
                Counterpart,
                fileFormat,
                CommandKeys.Key_StartRecord);
            Logger.Debug($"TryStartRecording, MessageReady:\n{message.ToString(3)}");
            MessageReady(this, message);
        }

        public void TryStopRecording(string renameTo = "")
        {
            var message = new MessageData(PluginName,
                Counterpart,
                renameTo,
                CommandKeys.Key_StopRecord);
            Logger.Debug($"TryStopRecording, MessageReady:\n{message.ToString(3)}");
            MessageReady(this, message);
        }

        #region Receivable Commands
        public void OnRecordStatusChange(object sender, string status)
        {
            Logger.Debug($"Record status change: {status}");
            StatusText.text = status;
        }
        #endregion

        #region Status TMPro
        public static int numFails = 0;
        public static float statusTextFontSize = 20f;
        private float PlayerHeight;
        private PlayerSpecificSettings _playerSettings;
        private StandardLevelSceneSetup _standardLevelSceneSetup;
        private TextMeshProUGUI _statusText;
        public TextMeshProUGUI StatusText
        {
            get
            {
                if (_statusText == null)
                    _statusText = CreateStatusText("");
                return _statusText;
            }
            set { _statusText = value; }
        }

        private StandardLevelSceneSetup standardLevelSceneSetup
        {
            get
            {
                if (_standardLevelSceneSetup == null)
                    _standardLevelSceneSetup = GameObject.FindObjectsOfType<StandardLevelSceneSetup>().FirstOrDefault();
                return _standardLevelSceneSetup;
            }
        }

        private PlayerSpecificSettings PlayerSettings
        {
            get
            {
                if (_playerSettings == null)
                {
                    _playerSettings = standardLevelSceneSetup?.standardLevelSceneSetupData?.gameplayCoreSetupData?.playerSpecificSettings;
                    if (_playerSettings != null)
                    {
                        Logger.Debug("Found PlayerSettings");
                    }
                    else
                        Logger.Warning($"Unable to find PlayerSettings");
                }
                else
                    Logger.Trace("PlayerSettings already exists, don't need to find it");
                return _playerSettings;
            }
        }

        public static void FacePosition(Transform obj, Vector3 targetPos)
        {
            var rotAngle = Quaternion.LookRotation(obj.position - targetPos);
            obj.rotation = rotAngle;
        }

        public TextMeshProUGUI CreateStatusText(string text, float yOffset = 0)
        {
            Logger.Debug("OBSStatusText doesn't exist, creating it");
            var textGO = new GameObject();
            GameObject.DontDestroyOnLoad(textGO);
            textGO.transform.position = StringToVector3(Plugin.StatusPosition);
            textGO.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            textGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            var textCanvas = textGO.AddComponent<Canvas>();
            textCanvas.renderMode = RenderMode.WorldSpace;
            (textCanvas.transform as RectTransform).sizeDelta = new Vector2(200f, 50f);

            FacePosition(textCanvas.transform, new Vector3(0, PlayerHeight, 0));
            TextMeshProUGUI textMeshProUGUI = new GameObject("OBSStatusText").AddComponent<TextMeshProUGUI>();

            RectTransform rectTransform = textMeshProUGUI.transform as RectTransform;
            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.sizeDelta = new Vector2(400f, 20f);
            rectTransform.Translate(new Vector3(0, yOffset, 0));
            textMeshProUGUI.text = text;
            textMeshProUGUI.fontSize = statusTextFontSize;
            textMeshProUGUI.alignment = TextAlignmentOptions.Center;
            textMeshProUGUI.ForceMeshUpdate();
            textMeshProUGUI.rectTransform.SetParent(textCanvas.transform, false);
            return textMeshProUGUI;
        }

        public static Vector3 StringToVector3(string vStr)
        {
            string[] sAry = vStr.Split(',');
            try
            {
                Vector3 retVal = new Vector3(
                    float.Parse(sAry[0]),
                    float.Parse(sAry[1]),
                    float.Parse(sAry[2]));
                Logger.Debug($"StringToVector3: {vStr}={retVal.ToString()}");
                return retVal;
            }
            catch (Exception ex)
            {
                Logger.Exception($"Cannot convert value of {vStr} to a Vector. Needs to be in the format #,#,#", ex);
                return new Vector3(0f, .3f, 2.5f);
            }
        }
        #endregion

    }

    struct CommandKeys
    {
        public const string Key_ = "";
        #region Actions
        public const string Key_StartRecord = "STARTREC";
        public const string Key_StopRecord = "STOPREC";
        #endregion

        #region Gets
        public const string Key_RecordStatus = "RECSTATUS";
        public const string Key_GetStreamStatus = "GETSTREAMSTATUS";
        public const string Key_OBSStatus = "OBSSTATUS";
        public const string Key_GetRecFileFormat = "GETRECFILEFMT";
        #endregion

        #region Sets
        public const string Key_SetRecFileFormat = "SETRECFILEFMT";
        #endregion
    }
}
