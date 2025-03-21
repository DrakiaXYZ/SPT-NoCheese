using BepInEx;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Common.Utils;

namespace DrakiaXYZ.NoCheese
{
    [BepInPlugin("xyz.drakia.nocheese", "DrakiaXYZ-NoCheese", "1.0.1")]
    [BepInDependency("com.SPT.core", "3.11.0")]
    public class NoCheesePlugin : BaseUnityPlugin
    {
        private static FieldInfo _raidSettingsField;
        private static FieldInfo _dateTimeField;

        public void Awake()
        {
            _raidSettingsField = AccessTools.Field(typeof(LocalGame).BaseType, "localRaidSettings_0");
            _dateTimeField = AccessTools.Field(typeof(LocalGame).BaseType, "dateTime_0");

            Application.quitting += Quit;
        }

        private static void Quit()
        {
            // If we're not in a game, don't do anything
            if (!Singleton<AbstractGame>.Instantiated)
            {
                return;
            }

            BaseLocalGame<EftGamePlayerOwner> game = Singleton<AbstractGame>.Instance as BaseLocalGame<EftGamePlayerOwner>;
            if (game == null)
            {
                return;
            }

            // Store the player's profile before we do anything, otherwise their armband goes away for some reason
            var profile = game.Profile_0;
            var resultProfile = (new GClass1998(profile, GClass2007.Instance)).ToUnparsedData(Array.Empty<JsonConverter>());

            // Then kill the player, this is necessary to get insurance stuff working, might sync with Fika?
            game.GameWorld_0.MainPlayer.ActiveHealthController.Kill(EDamageType.Existence);

            // Note for future me:
            // Look at the `BaseLocalGame` method that calls `LocalRaidEnded`, as well as `LocalRaidEnded` itself for
            // what classes are used below

            var raidSettings = _raidSettingsField.GetValue(game) as LocalRaidSettings;
            var lostInsuredItems = game.method_12();
            var transferItems = game.method_13();
            var duration = EFTDateTimeClass.Now - (DateTime)_dateTimeField.GetValue(game);

            // Tell the server we left
            GClass1959 results = new GClass1959
            {
                profile = resultProfile,
                result = ExitStatus.Left,
                killerId = "",
                killerAid = "",
                exitName = "",
                inSession = true,
                favorite = (profile.Info.Side == EPlayerSide.Savage),
                playTime = (int)duration.TotalSeconds,
                ProfileId = profile.Id
            };

            var data = new Class77<string, GClass1959, GClass1319[], Dictionary<string, GClass1319[]>, GClass1961>(raidSettings.serverId, results, lostInsuredItems, transferItems, null);

            RequestHandler.PostJson("/client/match/local/end", Json.Serialize(data));
        }
    }
}
