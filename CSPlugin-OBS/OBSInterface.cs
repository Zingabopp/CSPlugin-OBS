using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CommandPluginLib;
using OBSWebsocketDotNet;

namespace CSPluginOBS
{
    public class OBSInterface : ICommandPlugin
    {
        private OBSWebsocket _obs;
        private int conAttempts = 10;
        private bool isStopping = false;
        private static Timer stopTimer;
        private StreamStatus _curStatus;
        private OutputState recState;
        private bool isConnected
        {
            get
            {
                if (_obs != null)
                    return _obs.IsConnected;
                else
                    return false;
            }
        }
        private Dictionary<string, Action<string>> _commands;
        #region Command Keys
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
        
        #endregion Command Keys
        #region ICommandPlugin
        public string PluginName { get { return "OBSControl"; } }

        public List<string> Commands { get { return new List<string>(); } }

        private void BuildCommands()
        {
            _commands = new Dictionary<string, Action<string>> {
                {Key_StartRecord, TryStartRecording },
                {Key_StopRecord, TryStopRecording }
            };
        }

        public void Start()
        {
            Logger.LogLevel = LogLevel.Debug;
            Logger.ShortenSourceName = true;
            Logger.ShowTime = false;
            Logger.Info("Starting OBSInterface plugin");
            _obs = new OBSWebsocket();
            _obs.Connected += onConnect;
            _obs.RecordingStateChanged += onRecordingStateChange;
            TryConnect(-1);
            /*
            _commands = new Dictionary<string, Action<string>> { { "STARTREC", TryStartRecording } };
            _commands["STARTREC"]("");
            _commands["STARTREC"]("test");
            */
        }

        public void OnMessage(object sender, MessageData e)
        {
            Logger.Trace($"{e.Source} says: {e.Data}");

            if (_commands.ContainsKey(e.Flag))
                _commands[e.Flag](e.Data);
        }

        public event Action<MessageData> MessageReady;

        #endregion

        public bool IsRecording
        {
            get
            {
                if (isConnected)
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

        private void TryStartRecording(string fileNameFormat = "")
        {
            if (isStopping)
            {
                _obs.StopRecording();
                stopTimer.Stop();
            }
            if (!IsRecording)
                _obs.StartRecording();
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

        private void TryStopRecording(string stopDelay = "3000")
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
                if (!isConnected && ((conAttempts < maxAttempts) || infiniteAttempts))
                {
                    try
                    {
                        _obs.WSTimeout = new TimeSpan(0, 0, 5);
                        _obs.WSDisableLog = true;
                        _obs.Connect("ws://localhost:4444", "");
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("OBS Connection failed...");
                    }
                    if (!isConnected && ((conAttempts <= maxAttempts) || infiniteAttempts))
                    {
                        //Console.WriteLine($"Failed to connect to OBS ({conAttempts})...");
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

        private static string _lastRecState = "";


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
                Logger.Debug($"Recording state changed: {state}");
                MessageReady(new MessageData(PluginName, "OBSControl", state));
            }
            _lastRecState = state;
        }
    }
}
