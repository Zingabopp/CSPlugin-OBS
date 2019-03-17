using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using CommandPluginLib;
using System.Collections;

namespace BS_OBSControl
{
    class OBSControl : MonoBehaviour, ICommandPlugin
    {
        public string PluginName => Plugin.PluginName; // Name that identifies this plugin as a source/destination
        public const string Counterpart = "OBSControl"; // Destination plugin on Command-Server
        private string appendText = "";
        private Dictionary<string, Action<object, string>> _commands;

        /// <summary>
        /// Dictionary of commands this plugin can receive.
        /// </summary>
        public Dictionary<string, Action<object, string>> Commands
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
        }

        private void BuildCommands()
        {
            Logger.Trace("BuildCommands()");
            Commands.AddSafe(CommandKeys.Key_RecordStatus, OnRecordStatusChange);
            Commands.AddSafe(CommandKeys.Key_GetRecFileFormat, RecRecFileFormat);
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

            if (newScene.name == "MenuCore")
            {
                //Code to execute when entering The Menu
                Logger.Debug("In menu");

            }

            if (newScene.name == "GameCore")
            {
                //Code to execute when entering actual gameplay
                Logger.Debug("In GameCore");
                appendText = "";
                StartCoroutine(GetFileFormat());
                StartCoroutine(GameStatusSetup());
            }
        }

        /*
        public async Task TaskAsyncCountDown(int count, string flag = "")
        {
            for(int i = count; i >=0; i--)
            {
                Logger.Info($"{i}, {flag}");
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
        public async Task TaskAwaitACoroutine()
        {
            await TaskAsyncCountDown(10, "precoro");
            var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
            StartCoroutine(
                tempCoroutine(
                    CoroutineCountDown(10, "coro"),
                    () => tcs.TrySetResult(null)));

            await tcs.Task;
            await TaskAsyncCountDown(10, "postcoro");
        }

        public IEnumerator tempCoroutine(IEnumerator coro, System.Action afterExecutionCallback)
        {
            yield return coro;
            afterExecutionCallback();
        }

        public IEnumerator CoroutineCountDown(int count, string flag = "")
        {
            for (int i = count; i >= 0; i--)
            {
                Logger.Info($"{i}, {flag}");
                yield return new WaitForSeconds(1);
            }
        }
        */
        private static float PlayerHeight;
        private PlayerSpecificSettings _playerSettings;
        private PlayerSpecificSettings PlayerSettings
        {
            get
            {
                if (_playerSettings == null)
                {
                    _playerSettings = GameStatus.gameSetupData?.playerSpecificSettings;
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

        public IEnumerator<WaitUntil> GetFileFormat()
        {
            yield return new WaitUntil(() => {
                Logger.Debug("GetFileFormat: LevelInfo is null");
                return (GameStatus.LevelInfo != null);
            });
            var level = GameStatus.LevelInfo;
            string fileFormat = "";
            fileFormat = $"{level.songName}-{level.songAuthorName}";
            Logger.Debug($"Starting recording, file format: {fileFormat}");
            TryStartRecording(fileFormat);
        }

        public IEnumerator<WaitUntil> GameStatusSetup()
        {
            // TODO: Limit wait by tries/current scene so it doesn't go forever.
            yield return new WaitUntil(() => {
                return !(!BS_Utils.Plugin.LevelData.IsSet || GameStatus.GpModSO == null) || (SceneManager.GetActiveScene().name == "MenuCore");
            });
            GameStatus.Setup();
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }

        public void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            Logger.Trace("In OnDidFinish");
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            try
            {
                if (levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
                    appendText = "-failed";
                else
                {
                    float scorePercent = ((float) levelCompletionResults.score / GameStatus.MaxModifiedScore) * 100f;
                    string scoreStr = scorePercent.ToString("F3");
                    appendText = $"-{scoreStr.Substring(0, scoreStr.Length - 1)}{(levelCompletionResults.fullCombo ? "-FC" : "")}";
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Error appending file name", ex);
            }
            TryStopRecording();
        }

        public void AppendLastRecordingName(string suffix)
        {
            var msg = new MessageData(PluginName, Counterpart, suffix, CommandKeys.Key_AppendRecName);
            Logger.Info($"Attempting to append {suffix} to file name of last recording");
            MessageReady(this, msg);
        }

        public void TryStartRecording(string fileFormat = "")
        {
            var message = new MessageData(PluginName,
                Counterpart,
                fileFormat,
                CommandKeys.Key_StartRecord);
            Logger.Trace($"TryStartRecording, MessageReady:\n{message.ToString(3)}");
            MessageReady(this, message);
        }

        public void TryStopRecording(string renameTo = "")
        {
            var message = new MessageData(PluginName,
                Counterpart,
                renameTo,
                CommandKeys.Key_StopRecord);
            Logger.Trace($"TryStopRecording, MessageReady:\n{message.ToString(3)}");
            MessageReady(this, message);
        }

        #region Receivable Commands
        public void OnRecordStatusChange(object sender, string status)
        {
            Logger.Debug($"Record status change: {status}");
            if (status.ToLower().Contains("stopped") && !(appendText == ""))
            {
                AppendLastRecordingName(appendText);
                appendText = "";
            }
            StatusText = status;
        }

        public void RecRecFileFormat(object sender, string fmt)
        {
            Logger.Debug($"Received file format: {fmt}");
        }
        #endregion

        /// <summary>
        /// Generate a message ID
        /// </summary>
        /// <param name="length">(optional) message ID length</param>
        /// <returns>A random string of alphanumerical characters</returns>
        public static string NewMessageID(int length = 8)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //var random = new System.Random();

            string result = "";
            for (int i = 0; i < length; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Length - 1);
                //int index = random.Next(0, pool.Length - 1);
                result += pool[index];
            }

            return result;
        }


        #region Status TMPro
        public static float statusTextFontSize = 20f;
        private static TextMeshProUGUI _statusTextObj;
        public static TextMeshProUGUI StatusTextObj
        {
            get
            {
                if (_statusTextObj == null)
                    _statusTextObj = CreateText(_statusText);
                return _statusTextObj;
            }
            set { _statusTextObj = value; }
        }
        private static bool creatingText = false;
        private static string _statusText = "";
        public static string StatusText
        {
            set
            {
                _statusText = value;
                if (_statusTextObj == null && creatingText == false)
                {
                    creatingText = true;
                    StatusTextObj.text = _statusText;
                    creatingText = false;
                }
                if (!creatingText)
                    StatusTextObj.text = _statusText;
            }
        }


        public static void FacePosition(Transform obj, Vector3 targetPos)
        {
            var rotAngle = Quaternion.LookRotation(obj.position - targetPos);
            obj.rotation = rotAngle;
        }
        /*
        public static TextMeshProUGUI CreateStatusText(string text, float yOffset = 0)
        {
            Logger.Debug("CreateStatusText()");
            var textGO = new GameObject();
            GameObject.DontDestroyOnLoad(textGO);
            textGO.transform.position = StringToVector3(Plugin.StatusPosition);
            textGO.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            textGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Logger.Debug("---Making canvas()");
            var textCanvas = textGO.AddComponent<Canvas>();
            textCanvas.renderMode = RenderMode.WorldSpace;
            (textCanvas.transform as RectTransform).sizeDelta = new Vector2(200f, 50f);
            Logger.Debug("CreateStatusText 2");
            FacePosition(textCanvas.transform, new Vector3(0, PlayerHeight, 0));
            GameObject gameObj = new GameObject("OBSStatusText");
            gameObj.SetActive(false);
            TextMeshProUGUI textMeshProUGUI = gameObj.AddComponent<TextMeshProUGUI>();
            Logger.Debug("CreateStatusText 3");
            RectTransform rectTransform = textMeshProUGUI.transform as RectTransform;

            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.sizeDelta = new Vector2(400f, 20f);
            rectTransform.Translate(new Vector3(0, yOffset, 0));
            textMeshProUGUI.text = text;
            Logger.Debug("CreateStatusText 4");
            try
            {
                textMeshProUGUI.font = Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
                Logger.Debug("---Got font");
                textMeshProUGUI.fontSize = statusTextFontSize;
                textMeshProUGUI.alignment = TextAlignmentOptions.Center;
                textMeshProUGUI.ForceMeshUpdate();
                Logger.Debug("Forced mesh update");
                textMeshProUGUI.rectTransform.SetParent(textCanvas.transform, false);
                Logger.Debug("Set parent");
                //gameObj.SetActive(true);
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception creating OBSStatusText:\n", ex);
            }
            Logger.Debug("Finished CreateStatusText");
            return textMeshProUGUI;
        }
        */
        public static TextMeshProUGUI CreateText(string text)
        {
            Canvas _canvas = new GameObject("OBSStatusCanvas").AddComponent<Canvas>();
            _canvas.gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            _canvas.renderMode = RenderMode.WorldSpace;
            (_canvas.transform as RectTransform).sizeDelta = new Vector2(200f, 50f);
            //FacePosition(_canvas.gameObject.transform, new Vector3(0, PlayerHeight, 0));
            //_canvas.transform.position = StringToVector3(Plugin.StatusPosition);
            return CreateText(_canvas, text, new Vector2(0f, 0f), (_canvas.transform as RectTransform).sizeDelta);
        }

        public static TextMeshProUGUI CreateText(Canvas parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObj = parent.gameObject; //new GameObject("OBSStatusText");
            gameObj.SetActive(false);
            GameObject.DontDestroyOnLoad(gameObj);
            TextMeshProUGUI textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            /*
            Teko-Medium SDF No Glow
            Teko-Medium SDF
            Teko-Medium SDF No Glow Fading
            */
            var font = Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            if (font == null)
            {
                Logger.Error("Could not locate font asset, unable to display text");
                return null;
            }
            textMesh.font = font;
            textMesh.rectTransform.SetParent(parent.transform as RectTransform, false);
            textMesh.text = text;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;
            textMesh.alignment = TextAlignmentOptions.Left;
            gameObj.transform.position = StringToVector3(Plugin.StatusPosition);
            FacePosition(textMesh.gameObject.transform, new Vector3(0, PlayerHeight, 0));
            gameObj.SetActive(true);
            return textMesh;
        }


        public static Vector3 StringToVector3(string vStr)
        {
            Logger.Debug("StringToVector3: " + vStr);
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
        public const string Key_AppendRecName = "APPENDREC";
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
