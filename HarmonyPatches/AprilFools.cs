using HarmonyLib;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(GamePause))]
    [HarmonyPatch("Pause")]
    class AprilFoolsPatch
    {
        static void Prefix()
        {
            System.DateTime time;
            bool bunbundai = false;
            try
            {
                var userID = 0;//BS_Utils.Gameplay.GetUserInfo.GetUserID();
                if (userID == 76561198182060577)
                    bunbundai = true;
            }
            catch (System.Exception ex)
            {
                Logging.logger.Error("Error in Bunbundai Contingency");
            }
            if (IPA.Utilities.Utils.CanUseDateTimeNowSafely)
                time = System.DateTime.Now;
            else
                time = System.DateTime.UtcNow;

         //   if (bunbundai)
         //       BS_Utils.Gameplay.ScoreSubmission.DisableSubmission("Nice Pause bunbundai"); else 
            if (time.Month == 4 && time.Day == 1)
                BS_Utils.Gameplay.ScoreSubmission.DisableSubmission("Nice Pause Idjot");

        }
    }
}
