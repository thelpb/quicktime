using Aki.Reflection.Utils;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

using thelpb.quicktime.Utils;

namespace TheLPB.QuickTime
{
    [BepInPlugin("com.thelpb.quicktime", "thelpb.quicktime", "1.0")]

    public class QuickTimePlugin : BaseUnityPlugin
    {
        private static ConfigEntry<KeyCode> QuickTimeKeyConfig { get; set; }

        private static GameWorld gameWorld;

        private static Type gameDateTime;
        private static MethodInfo calculateTime;
        private static MethodInfo resetTime;

        private static DateTime modifiedDateTime;
        private static DateTime currentDateTime;



        internal void Awake()
        {
            QuickTimeKeyConfig = Config.Bind("Quick Time", "KeyBind", KeyCode.Period, new ConfigDescription($"Hold this key and then use the scroll wheel to set the time. This has no impact on the raid timer."));

            gameDateTime = PatchConstants.EftTypes.Single(x => x.GetMethod("CalculateTaxonomyDate") != null);
            calculateTime = gameDateTime.GetMethod("Calculate", BindingFlags.Public | BindingFlags.Instance);
            resetTime = gameDateTime.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(x => x.Name == "Reset" && x.GetParameters().Length == 1);
        }



        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<QuickTimePlugin>();
            }
        }
        
        
        
        public void Update()
        {
            KeyCode QuickTimeKey = QuickTimeKeyConfig.Value;
            gameWorld = Singleton<GameWorld>.Instance;

            if (QuickTimeKey != KeyCode.None && Input.GetKey(QuickTimeKey))
            {
                //If GameWorld is null, it means that player currently is not in the raid
                if (gameWorld != null)
                {
                    if (GameObject.Find("Weather") == null)
                    {
                        //Notify player with bottom-right error popup
                        Notifier.DisplayWarningNotification("An error occurred attempting to change the time, seems like you are either in the hideout or factory.");
                    }
                    else
                    {
                        ChangeTime();
                    }
                }
            }
        }



        private void ChangeTime ()
        {
            double timeDelta;
            double targetTimeHours;

            float scrollDelta = Input.mouseScrollDelta.y;

            timeDelta = scrollDelta > 0 ? 1 : -1;

            if (scrollDelta != 0f)
            {
                currentDateTime = (DateTime)calculateTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(gameWorld), null);
                targetTimeHours = currentDateTime.Hour + timeDelta;
                modifiedDateTime = currentDateTime.AddHours((double)targetTimeHours - currentDateTime.Hour);
                resetTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(gameWorld), new object[] { modifiedDateTime });
                Notifier.DisplayMessageNotification("Time was set to: " + modifiedDateTime.ToString("HH:mm"));
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInspectorWindowClose);
            }
        }
    }
}
