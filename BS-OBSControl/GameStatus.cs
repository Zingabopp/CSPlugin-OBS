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
    }
}
