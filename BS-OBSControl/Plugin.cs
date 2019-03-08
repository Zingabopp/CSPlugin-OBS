using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionPlugin;
using BS_OBSControl.Util;


namespace BS_OBSControl
{
    public class Plugin : IPlugin
    {
        public static string PluginName = "OBSControl";
        public string Name => PluginName;
        public string Version => "0.0.1";

        private bool doesCIExist;
        private static string _counterPosition = "-1,2,2.5";
        public static string StatusPosition
        {
            get { return _counterPosition; }
            set { _counterPosition = value; }
        }

        public void OnApplicationStart()
        {
            Logger.LogLevel = LogLevel.Trace;
            
            //Checks if a IPlugin with the name in quotes exists, in case you want to verify a plugin exists before trying to reference it, or change how you do things based on if a plugin is present
            doesCIExist = IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "Command-Interface");
            if(doesCIExist)
            {
                SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                var ctrlObj = new GameObject(PluginName).AddComponent<OBSControl>();
                GameObject.DontDestroyOnLoad(ctrlObj);
            }
            

        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {

            if (newScene.name == "Menu")
            {
                //Code to execute when entering The Menu


            }

            if (newScene.name == "GameCore")
            {
                //Code to execute when entering actual gameplay


            }


        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            //Create GameplayOptions/SettingsUI if using either
            if (scene.name == "Menu")
                UI.BasicUI.CreateUI();

        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
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
    }
}
