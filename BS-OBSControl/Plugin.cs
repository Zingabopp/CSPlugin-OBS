using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPA;
using IllusionPlugin;
using BS_OBSControl.Util;
using CommandPluginLib;
using Harmony;
using System.Reflection;


namespace BS_OBSControl
{
    public class Plugin : IBeatSaberPlugin
    {
        public static string PluginName = "OBSControl";
        public string Name => PluginName;
        public string Version => "0.1.0";
        public static Plugin Instance;

        private bool doesCIExist;
        private static string _statusPosition = "-1,3.5,2"; //"-1,2,2.5";
        public static string StatusPosition
        {
            get { return _statusPosition; }
            set { _statusPosition = value; }
        }

        public void OnApplicationStart()
        {
            Logger.LogLevel = LogLevel.Info;
            Logger.Debug($"Starting...");
            Instance = this;
            doesCIExist = IPA.Loader.PluginManager.AllPlugins.FirstOrDefault(c => c.Metadata.Name == "Command-Interface") != null;
            try
            {
                var harmony = HarmonyInstance.Create("com.github.zingabopp.bailoutmode");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Logger.Exception("This plugin requires Harmony. Make sure you " +
                    "installed the plugin properly, as the Harmony DLL should have been installed with it.", ex);
            }
            SharedCoroutineStarter.instance.StartCoroutine(DelayedStartup());
        }

        private IEnumerator DelayedStartup()
        {
            yield return new WaitForSeconds(0.5f);
            if (doesCIExist)
            {
                Logger.Debug("Command-Interface exists, starting loader");
                var loader = new GameObject("OBSC_Loader").AddComponent<Loader>();
                GameObject.DontDestroyOnLoad(loader.gameObject);
                //loader.LoadSuccess += OnLoadSuccess;
            }
            else
                Logger.Error("Command-Interface not found, unable to start");
        }


        public void OnLoadSuccess(GameObject loader, ICommandPlugin server, ICommandPlugin OBSC)
        {
            var initMsg = new MessageData(PluginName, "Command-Interface", Plugin.PluginName, "REGISTER");
            server.OnMessage(OBSC, initMsg);
            OBSControl.StatusText = "";
            GameObject.Destroy(loader);
            Logger.Info("OBSControl loaded successfully");
        }


        public void OnApplicationQuit()
        {

        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {


        }

        public void OnFixedUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            //Create GameplayOptions/SettingsUI if using either
            //if (scene.name == "MenuCore")
            //    UI.BasicUI.CreateUI();
        }

        public void OnSceneUnloaded(Scene scene)
        {
            
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            if (nextScene.name == "MenuCore")
            {
                //Code to execute when entering The Menu


            }

            if (nextScene.name == "GameCore")
            {
                //Code to execute when entering actual gameplay


            }
        }
    }
}
