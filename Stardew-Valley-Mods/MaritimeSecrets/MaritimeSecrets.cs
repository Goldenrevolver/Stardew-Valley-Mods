using StardewModdingAPI;
using StardewValley;
using System;

namespace MaritimeSecrets
{
    public class MaritimeSecrets : Mod
    {
        internal string talkedToMarinerTodayKey;

        public override void Entry(IModHelper helper)
        {
            talkedToMarinerTodayKey = $"{ModManifest?.UniqueID}/TalkedToMarinerToday";

            Helper.Events.GameLoop.DayEnding += delegate { ResetMarinerTalk(); };

            Patcher.PatchAll(this);
        }

        private void ResetMarinerTalk()
        {
            foreach (var farmer in Game1.getAllFarmers())
            {
                farmer?.modData?.Remove(talkedToMarinerTodayKey);
            }
        }

        /// <summary>
        /// Small helper method to log to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        /// <summary>
        /// Small helper method to log an error to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        /// <param name="e">an optional error message to log additionally</param>
        public void ErrorLog(object o, Exception e = null)
        {
            string baseMessage = o == null ? "null" : o.ToString();

            string errorMessage = e == null ? string.Empty : $"\n{e.Message}\n{e.StackTrace}";

            Monitor.Log(baseMessage + errorMessage, LogLevel.Error);
        }
    }
}