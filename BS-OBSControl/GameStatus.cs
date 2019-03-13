using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BS_OBSControl
{
    public static class GameStatus
    {
        private static StandardLevelSceneSetupDataSO _levelSetupDataSO;
        private static PartyFreePlayFlowCoordinator _playFlowCoordinator;
        private static GameplayModifiersModelSO _gpModSO;

        public static StandardLevelSceneSetupDataSO LevelSetupDataSO
        {
            get
            {
                if (_levelSetupDataSO == null)
                    _levelSetupDataSO = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
                return _levelSetupDataSO;
            }
        }

        public static IBeatmapLevel LevelInfo
        {
            get
            {
                return LevelSetupDataSO?.difficultyBeatmap?.level;
            }
        }

        public static PartyFreePlayFlowCoordinator PlayFlowCoordinator
        {
            get
            {
                if (_playFlowCoordinator == null)
                {
                    Logger.Debug("PlayFlowCoordinator is null, getting new one");
                    _playFlowCoordinator = GameObject.FindObjectsOfType<PartyFreePlayFlowCoordinator>().FirstOrDefault();
                }
                return _playFlowCoordinator;
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
                else
                    Logger.Debug("Found GameplayModifersModelSO");
                return _gpModSO;
            }
        }

        public static int MaxScore;
        public static int MaxModifiedScore;

        public static void Setup()
        {
            try
            {
                MaxScore = ScoreController.MaxScoreForNumberOfNotes(GameStatus.LevelSetupDataSO.difficultyBeatmap.beatmapData.notesCount);
                MaxModifiedScore = ScoreController.GetScoreForGameplayModifiers(GameStatus.MaxScore, LevelSetupDataSO.gameplayCoreSetupData.gameplayModifiers, GameStatus.GpModSO);
            } catch(Exception ex)
            {
                Logger.Exception("Error getting max scores", ex);
            }
        }
    }
}
