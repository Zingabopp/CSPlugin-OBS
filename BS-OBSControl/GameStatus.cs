using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BS_OBSControl.Util;

namespace BS_OBSControl
{
    public static class GameStatus
    {
        private static GameplayModifiersModelSO _gpModSO;
        private static GameplayCoreSceneSetupData _gameSetupData;
        public static int MaxScore;
        public static int MaxModifiedScore;

        /*
        private static GameplayCoreSceneSetup gameplayCoreSceneSetup
        {
            get
            {
                if (_gameplayCoreSceneSetup == null)
                    _gameplayCoreSceneSetup = GameObject.FindObjectsOfType<GameplayCoreSceneSetup>().FirstOrDefault();
                return _gameplayCoreSceneSetup;
            }
        }
        */

        public static GameplayCoreSceneSetupData gameSetupData
        {
            get
            {
                if (BS_Utils.Plugin.LevelData.IsSet)
                    _gameSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                return _gameSetupData;
            }
        }

        public static IDifficultyBeatmap difficultyBeatmap
        {
            get { return gameSetupData?.difficultyBeatmap; }
        }

        public static IBeatmapLevel LevelInfo
        {
            get
            {
                return difficultyBeatmap?.level;
            }
        }

        public static GameplayModifiersModelSO GpModSO
        {
            get
            {
                if (_gpModSO == null)
                {
                    Logger.Debug("GameplayModifersModelSO is null, getting new one");
                    _gpModSO = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().FirstOrDefault();
                }
                if (_gpModSO == null)
                {
                    Logger.Warning("GameplayModifersModelSO is still null");
                }
                //else
                //    Logger.Debug("Found GameplayModifersModelSO");
                return _gpModSO;
            }
        }

        public static void Setup()
        {
            try
            {
                MaxScore = ScoreController.MaxScoreForNumberOfNotes(difficultyBeatmap.beatmapData.notesCount);
                Logger.Debug($"MaxScore: {MaxScore}");
                MaxModifiedScore = ScoreController.GetScoreForGameplayModifiers(GameStatus.MaxScore, gameSetupData.gameplayModifiers, GameStatus.GpModSO);
                Logger.Debug($"MaxModifiedScore: {MaxModifiedScore}");
            } catch(Exception ex)
            {
                Logger.Exception("Error getting max scores", ex);
            }
        }
    }
}
