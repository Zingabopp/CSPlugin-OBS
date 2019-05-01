using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using BS_Utils;
using static BS_OBSControl.Util.ReflectionUtil;

namespace BS_OBSControl.Harmony
{
    [HarmonyPatch(typeof(SoloFreePlayFlowCoordinator), "HandleLevelDetailViewControllerDidPressPlayButton",
        new Type[] {
        typeof(StandardLevelDetailViewController)
        })]
    class SoloFreePlayFlowCoordinateorDidPressPlay
    {
        static bool Prefix(SoloFreePlayFlowCoordinator __instance, ref StandardLevelDetailViewController viewController)
        {
            Logger.Trace("In SoloFreePlayFlowCoordinator.HandleLevelDetailViewControllerDidPressPlayButton()");
            var levelView = viewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
            if (levelView != null)
                levelView.playButton.interactable = false;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, viewController.selectedDifficultyBeatmap.level, levelView?.playButton));
            return false;
        }

        private static IEnumerator DelayedLevelStart(SoloFreePlayFlowCoordinator coordinator, IBeatmapLevel levelInfo, UnityEngine.UI.Button playButton)
        {
            Logger.Trace("Delaying level start by 2 seconds...");
            if (levelInfo != null)
                Logger.Debug($"levelInfo is not null: {levelInfo.songName} by {levelInfo.levelAuthorName}");
            SharedCoroutineStarter.instance.StartCoroutine(OBSControl.Instance.GetFileFormat(levelInfo));
            OBSControl.Instance.recordingCurrentLevel = true;
            yield return new WaitForSeconds(2f);
            //playButton.interactable = true;
            coordinator.StartLevel(null, false);


        }
    }




}
