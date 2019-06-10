using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CommandPluginLib;
using OBSWebsocketDotNet;
using static CSPluginOBS.Util;

namespace CSPluginOBS
{
    public class OBSInterface : ICommandPlugin
    {
        private OBSWebsocket _obs;
        private int conAttempts = 10;
        private bool isStopping = false;
        private static Timer stopTimer;
        private StreamStatus _curStatus;
        private string _lastRecFile;
        private string _lastRecFmt;
        private const string vidExt = ".mkv";
        private static string _lastRecState = "";
        private OutputState recState;
        private bool isConnectedBeatSaber
        {
            get
            {
                return true;
            }
        }
        private bool isConnectedOBS
        {
            get
            {
                if (_obs != null)
                    return _obs.IsConnected;
                else
                    return false;
            }
        }
        #region Command Keys
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

        #endregion Command Keys

        #region ICommandPlugin
        public string PluginName { get { return "OBSControl"; } }

        private Dictionary<string, Action<object, string>> _commands;

        public Dictionary<string, Action<object, string>> Commands
        {
            get
            {
                if (_commands == null)
                    _commands = new Dictionary<string, Action<object, string>>();
                return _commands;
            }
        }

        private void BuildCommands()
        {

            Commands.Add(Key_StartRecord, TryStartRecording);
            Commands.Add(Key_StopRecord, TryStopRecording);
            Commands.Add(Key_SetRecFileFormat, SetRecFileFormat);
            Commands.Add(Key_GetRecFileFormat, ReqRecFileFormat);
            Commands.Add(Key_AppendRecName, AppendLastRecFile);

        }

        /// <summary>
        /// Initialize the plugin and start trying to connect to OBS
        /// </summary>
        public void Initialize()
        {
            Logger.LogLevel = LogLevel.Trace;
            Logger.ShortenSourceName = true;
            Logger.ShowTime = false;
            Logger.Info($"Starting OBSInterface plugin, LogLevel set to {Logger.LogLevel.ToString()}");

            BuildCommands();
            _obs = new OBSWebsocket();
            _obs.Connected += onConnect;
            _obs.RecordingStateChanged += onRecordingStateChange;
            TryConnect(-1);
        }

        /// <summary>
        /// Received message from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnMessage(object sender, MessageData e)
        {
            Logger.Trace($"{e.Source} says: {e.Data}");

            if (_commands.ContainsKey(e.Flag))
                _commands[e.Flag](sender, e.Data);
        }

        /// <summary>
        /// Message ready to send to Beat Saber.
        /// </summary>
        public event Action<object, MessageData> MessageReady;

        #endregion

        /// <summary>
        /// Check if recording.
        /// </summary>
        public bool IsRecording
        {
            get
            {
                if (isConnectedOBS)
                    return _obs.GetStreamingStatus().IsRecording;
                else
                    return false;
            }
        }

        public OBSInterface()
        {
            Logger.Debug("Creating new OBSPlugin");
        }


        private void _obs_StreamStatus(OBSWebsocket sender, StreamStatus status)
        {
            _curStatus = status;
            PrintStatus();
        }

        private void PrintStatus()
        {
            Logger.Trace($"  FPS: {_curStatus.FPS}\n  Dropped Frames: {_curStatus.DroppedFrames}\n  Strain: {_curStatus.Strain}");
        }

        private void TryStartRecording(object sender, string fileNameFormat = "")
        {
            //fileNameFormat = SafeFileName(_obs.GetRecordingFolder(), fileNameFormat, vidExt);
            SetRecFileFormat(this, fileNameFormat);
            if (isStopping)
            {
                _obs.StopRecording();
                stopTimer.Stop();
            }
            if (!IsRecording)
            {
                _obs.StartRecording();
                string msg = "Starting to record";
                if (fileNameFormat == "")
                    msg = msg + "...";
                else
                    msg = $"{msg} using filename {fileNameFormat}...";
                Logger.Info(msg);
            }
            else
            {
                _obs.StopRecording();
                Timer startTimer = new Timer(50);
                startTimer.AutoReset = true;
                startTimer.Elapsed += (source, e) => {
                    Logger.Debug("OBS failed to start recording, retrying");
                    if (!IsRecording)
                    {
                        _obs.StartRecording();
                    }
                    if (recState == OutputState.Started || recState == OutputState.Starting)
                    {
                        Logger.Info("Recording started successfully");
                        ((Timer) source).AutoReset = false;
                        ((Timer) source).Stop();
                    }
                };
                startTimer.Start();

            }

        }

        private void TryStopRecording(object sender, string stopDelay = "3000")
        {
            int tryDelay;
            int delay = 3000;
            if (int.TryParse(stopDelay, out tryDelay))
            {
                Logger.Debug($"TryStopRecording passed delay of {stopDelay}");
                delay = tryDelay;
            }

            if (IsRecording)
            {
                isStopping = true;
                stopTimer = new Timer(delay);
                stopTimer.AutoReset = false;
                stopTimer.Elapsed += (source, eea) => {
                    if (IsRecording)
                        _obs.StopRecording();
                };
                stopTimer.Start();
                ;
            }
        }

        public void TryConnect(int maxAttempts = -1)
        {
            conAttempts = 1;
            bool infiniteAttempts = (maxAttempts == -1) ? true : false;
            Timer timer = new Timer(10);
            timer.AutoReset = false;
            Logger.Trace("OBSInterface: In TryConnect");
            timer.Elapsed += (source, e) => {

                Logger.Info($"Attempting to connect to OBS ({conAttempts})...");
                if (!isConnectedOBS && ((conAttempts < maxAttempts) || infiniteAttempts))
                {
                    try
                    {
                        _obs.WSTimeout = new TimeSpan(0, 0, 5);
                        _obs.WSDisableLog = true;
                        string password = $"test";
                        Logger.Debug($"Attempting to connect to OBS with password {password}");
                        _obs.Connect("ws://localhost:4444", password);
                    }
                    catch (AuthFailureException)
                    {
                        Logger.Error("OBS connection failed, invalid password");
                        _obs.Disconnect(); // Needs this to disconnect when authentication fails
                    }
                    catch (WebSocketSharp.WebSocketException ex)
                    {
                        Logger.Exception("OBS Connection failed...", ex);
                    }
                    if (!isConnectedOBS && ((conAttempts <= maxAttempts) || infiniteAttempts))
                    {
                        Logger.Trace($"Failed to connect to OBS ({conAttempts})...");
                        conAttempts++;
                        ((Timer) source).Interval = 3000;
                        ((Timer) source).Start();
                    }
                }

            };
            timer.Start();
            Logger.Trace("OBSInterface: In TryConnect, after timer.Start()");
        }

        private void onConnect(object sender, EventArgs e)
        {
            Logger.Trace("OnConnect()");
            var versionInfo = _obs.GetVersion();
            var streamStatus = _obs.GetStreamingStatus();
            Logger.Info($"Connected to OBS version {versionInfo.OBSStudioVersion}");

            _obs.RecordingStateChanged += onRecordingStateChange;
            _obs.StreamStatus += _obs_StreamStatus;
            _obs.Disconnected += onDisconnect;
        }

        private void onDisconnect(object sender, EventArgs e)
        {
            Logger.Info("OBS disconnected");
            _obs.RecordingStateChanged -= onRecordingStateChange;
            _obs.StreamStatus -= _obs_StreamStatus;
            _obs.Disconnected -= onDisconnect;
            TryConnect();
        }

        private void SetRecFileFormat(object sender, string fmt = "%CCYY-%MM-%DD %hh-%mm-%ss")
        {
            Logger.Trace($"Setting filename format to {fmt}");
            if (fmt == "")
                fmt = "%CCYY-%MM-%DD %hh-%mm-%ss";
            else
            {

                fmt = MakeValidFilename(fmt);
                _lastRecFmt = fmt;
                /*
                var recFolder = _obs.GetRecordingFolder();
                var fileString = fmt;
                
                var file = new FileInfo(JoinPaths(recFolder, fileString) + vidExt);
                int index = 2;
                while (file.Exists)
                {
                    fileString = MakeValidFilename(fmt + $" ({index})");
                    string newString = $"{JoinPaths(recFolder, fileString)}";
                    file = new FileInfo(newString + vidExt);
                    index++;
                }
                */
                fmt = SafeFileName(_obs.GetRecordingFolder(), fmt, vidExt);
                _lastRecFile = fmt;
            }
            _obs.SetFilenameFormatting(MakeValidFilename(fmt));

        }

        private void AppendLastRecFile(object sender, string suffix)
        {
            string fileBase = JoinPaths(_obs.GetRecordingFolder().Replace(@"/", @"\"), _lastRecFile);
            string newFullName = "";
            var file = new FileInfo(fileBase + vidExt);
            if (file.Exists)
            {
                try
                {
                    string newName = _lastRecFmt + MakeValidFilename(suffix);
                    string finalName = newName + vidExt;
                    string recFolder = _obs.GetRecordingFolder().Replace(@"/", @"\");
                    Logger.Trace($"Recording folder is: {recFolder}");
                    //newFullName = JoinPaths(_obs.GetRecordingFolder().Replace(@"/", @"\"), finalName);
                    newFullName = JoinPaths(recFolder, SafeFileName(recFolder, newName, vidExt) + vidExt);
                    //int index = 2;

                    //var newFile = new FileInfo(newFullName);
                    /*
                    while (newFile.Exists)
                    {
                        finalName = $"{newName}_{index}{vidExt}";
                        newFullName = JoinPaths(recFolder, finalName);
                        Logger.Trace($"{newFile.FullName} exists, trying {newFullName}");
                        newFile = new FileInfo(newFullName);
                        index++;
                    }
                    */
                    
                    Logger.Trace($"Moving {file.FullName} to\n      {newFullName}");
                    file.MoveTo(newFullName);
                }
                catch (Exception ex)
                {
                    Logger.Exception("Exception in appendfile\n", ex);
                }

            }
        }





        private string GetRecFileFormat()
        {
            return _obs.GetFilenameFormatting();
        }

        private void ReqRecFileFormat(object sender, string _)
        {
            var msg = new MessageData(PluginName, "OBSControl", GetRecFileFormat(), Key_GetRecFileFormat);
            MessageReady(this, msg);
        }

        private void onRecordingStateChange(OBSWebsocket sender, OutputState newState)
        {
            string state = "";
            recState = newState;
            switch (newState)
            {
                case OutputState.Starting:
                    state = "Recording starting...";
                    break;

                case OutputState.Started:
                    state = "Recording started";
                    break;

                case OutputState.Stopping:
                    state = "Recording stopping...";
                    break;

                case OutputState.Stopped:
                    state = "Recording stopped";
                    stopTimer?.Stop();
                    isStopping = false;
                    break;

                default:
                    state = "State unknown";
                    break;
            }
            if (state != _lastRecState)
            {
                if (newState == OutputState.Started || newState == OutputState.Stopped)
                    Logger.Info($"Recording state changed: {state}");
                else
                    Logger.Debug($"Recording state changed: {state}");
                MessageReady(this, new MessageData(PluginName, "OBSControl", newState.ToString(), Key_RecordStatus));
            }
            _lastRecState = state;
        }
    }
}
