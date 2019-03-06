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

        public void Start()
        {
            Console.WriteLine("Starting OBSInterface plugin");
            _obs = new OBSWebsocket();
            _obs.WSTimeout = new TimeSpan(0, 0, 5);
            
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
            Console.WriteLine($"{e.Source} says: {e.Data}");
            if (e.Data == "GameCore" && (!IsRecording || isStopping))
            {
                if (isStopping)
                {
                    _obs.StopRecording();
                    stopTimer.Stop();
                }
                TryStartRecording();
            }
            else
            {
                if (IsRecording)
                {
                    isStopping = true;
                    stopTimer = new Timer(3000);
                    stopTimer.AutoReset = false;
                    stopTimer.Elapsed += (source, eea) => {
                        if (IsRecording)
                            _obs.StopRecording();
                    };
                    stopTimer.Start();
                    ;
                }
            }
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
            Console.WriteLine("Creating new OBSPlugin");
        }


        private void _obs_StreamStatus(OBSWebsocket sender, StreamStatus status)
        {
            _curStatus = status;
            PrintStatus();
        }

        private void PrintStatus()
        {
            Console.WriteLine($"  FPS: {_curStatus.FPS}\n  Dropped Frames: {_curStatus.DroppedFrames}\n  Strain: {_curStatus.Strain}");
        }

        private void TryStartRecording(string fileNameFormat = "")
        {
            if (!IsRecording)
                _obs.StartRecording();
            else
            {
                _obs.StopRecording();
                Timer startTimer = new Timer(50);
                startTimer.AutoReset = true;
                startTimer.Elapsed += (source, e) => {
                    Console.WriteLine("OBS failed to start recording, retrying");
                    if (!IsRecording)
                    {
                        _obs.StartRecording();
                    }
                    if (recState == OutputState.Started || recState == OutputState.Starting)
                    {
                        Console.WriteLine("Recording started successfully");
                        ((Timer) source).AutoReset = false;
                        ((Timer) source).Stop();
                    }
                };
                startTimer.Start();

            }

        }

        public void TryConnect(int maxAttempts = 10)
        {
            conAttempts = 1;
            bool infiniteAttempts = (maxAttempts == -1) ? true : false;
            Timer timer = new Timer(1);
            timer.AutoReset = false;

            timer.Elapsed += (source, e) => {

                Console.WriteLine($"Attempting to connect to OBS ({conAttempts})...");
                if (!isConnected && ((conAttempts < maxAttempts) || infiniteAttempts))
                {
                    try
                    {
                        _obs.Connect("ws://localhost:4444", "");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("OBS Connection failed...");
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
        }

        private void onConnect(object sender, EventArgs e)
        {
            var versionInfo = _obs.GetVersion();
            var streamStatus = _obs.GetStreamingStatus();
            Console.WriteLine($"Connected to OBS version {versionInfo.OBSStudioVersion}");

            _obs.RecordingStateChanged += onRecordingStateChange;
            _obs.StreamStatus += _obs_StreamStatus;
            _obs.Disconnected += onDisconnect;

        }

        private void onDisconnect(object sender, EventArgs e)
        {
            Console.WriteLine("OBS disconnected");
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
                Console.WriteLine($"Recording state changed: {state}");
                MessageReady(new MessageData(PluginName, "OBSControl", state));
            }
            _lastRecState = state;
        }
    }
}
