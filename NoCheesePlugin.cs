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
using EFT.Interactive;
using EFT.InventoryLogic;

namespace DrakiaXYZ.NoCheese
{
    [BepInPlugin("xyz.drakia.nocheese", "DrakiaXYZ-NoCheese", "1.0.0")]
    [BepInDependency("com.SPT.core", "3.11.0")]
    public class NoCheesePlugin : BaseUnityPlugin
    {
        private static FieldInfo _raidSettingsField;
        private static FieldInfo _dateTimeField;

        public void Awake()
        {
            _raidSettingsField = AccessTools.Field(typeof(LocalGame), "localRaidSettings_0");
            _dateTimeField = AccessTools.Field(typeof(LocalGame), "dateTime_0");

            Application.quitting += Quit;
        }

        private static void Quit()
        {
            // If we're not in a game, don't do anything
            if (!Singleton<AbstractGame>.Instantiated)
            {
                return;
            }

            // If we can't convert the game to an abstract game, don't do anything
            LocalGame game = Singleton<AbstractGame>.Instance as LocalGame;
            if (game == null)
            {
                return;
            }

            // Kill the player
            GamePlayerOwner.MyPlayer.OnDead(EDamageType.Existence);

            // Note for future me:
            // Look at the `BaseLocalGame` method that calls `LocalRaidEnded`, as well as `LocalRaidEnded` itself for
            // what classes are used below

            // Tell the server we went MIA
            var profile = GamePlayerOwner.MyPlayer.Profile;
            var raidSettings = _raidSettingsField.GetValue(game) as LocalRaidSettings;
            var lostInsuredItems = game.method_12();
            var transferItems = game.method_13();
            var duration = EFTDateTimeClass.Now - (DateTime)_dateTimeField.GetValue(game);

            GClass1959 results = new GClass1959
            {
                profile = (new GClass1998(profile, GClass2007.Instance)).ToUnparsedData(Array.Empty<JsonConverter>()),
                result = ExitStatus.MissingInAction,
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
