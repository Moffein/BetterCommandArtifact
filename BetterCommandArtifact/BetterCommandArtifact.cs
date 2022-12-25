using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine.Networking;
using PickupIndex = RoR2.PickupIndex;
using PickupTransmutationManager = RoR2.PickupTransmutationManager;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace BetterCommandArtifact
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class BetterCommandArtifact : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Boooooop";
        public const string PluginName = "BetterCommandArtifact";
        public const string PluginVersion = "1.0.1";

        public static ConfigFile configFile = new ConfigFile(Paths.ConfigPath + "\\BetterCommandArtifact.cfg", true);

        public static ConfigEntry<int> itemAmount { get; set; }
        
        public void OnEnable()
        {
            itemAmount = configFile.Bind("BetterCommandArtifact", "itemAmount", 5, new ConfigDescription("Set the amount of items shown when opening a command artifact drop. \n Value must be Greater Than 0."));
            Config.SettingChanged += ConfigOnSettingChanged;
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact += SetOptions;
        }

        void ConfigOnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (itemAmount.Value <= 0)
                itemAmount.Value = 1;
        }

        public void OnDisable()
        {
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact -= SetOptions;
        }
        
        [Server]
        void SetOptions(On.RoR2.PickupPickerController.orig_SetOptionsFromPickupForCommandArtifact orig, RoR2.PickupPickerController self, PickupIndex pickupIndex)
        {
            if (!NetworkServer.active) return;
            
            PickupIndex[] newSelection = PickupTransmutationManager.GetGroupFromPickupIndex(pickupIndex);
            PickupPickerController.Option[] array;

            if (newSelection == null)
            {
                array = new PickupPickerController.Option[1]
                {
                    new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = pickupIndex
                    }
                };
            }
            else
            {
                Random rnd = new Random();
                List<PickupIndex> list = new List<PickupIndex>();

                int extraItems = itemAmount.Value;
                if (pickupIndex != PickupIndex.none)
                {
                    list.Add(pickupIndex);
                    extraItems--;
                }

                if (extraItems > 0)
                {
                    var add = (from x in newSelection.ToList() orderby rnd.Next() select x).Where(x => Run.instance.IsPickupAvailable(x));

                    List<PickupIndex> additionalOptions = (from x in newSelection.ToList() orderby rnd.Next() select x).Where(x => (Run.instance.IsPickupAvailable(x) && x != pickupIndex)).Take(extraItems).ToList();
                    list.AddRange(additionalOptions);
                }

                array = new PickupPickerController.Option[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    PickupIndex pickupIndex2 = list[i];
                    array[i] = new PickupPickerController.Option
                    {
                        available = Run.instance.IsPickupAvailable(pickupIndex2),
                        pickupIndex = pickupIndex2
                    };
                }
            }
            self.SetOptionsServer(array);
        }
    }
}