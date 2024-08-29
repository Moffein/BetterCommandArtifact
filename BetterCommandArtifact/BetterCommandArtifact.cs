using System.Security;
using System.Security.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Artifacts;
using UnityEngine.Networking;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

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
        public const string PluginVersion = "1.4.1";

        public static ConfigFile configFile = new ConfigFile(Paths.ConfigPath + "\\BetterCommandArtifact.cfg", true);

        public static ConfigEntry<int> itemAmount { get; set; }
        public static ConfigEntry<bool> allowBoss { get; set; }

        public static ConfigEntry<bool> enablePrinter { get; set; }

        public static ConfigEntry<bool> perTierEnabled { get; set; }

        public static ConfigEntry<int> whiteAmount { get; set; }
        public static ConfigEntry<int> greenAmount { get; set; }
        public static ConfigEntry<int> redAmount { get; set; }
        public static ConfigEntry<int> yellowAmount { get; set; }

        public static ConfigEntry<int> equipmentAmount { get; set; }
        public static ConfigEntry<int> equipmentLunarAmount { get; set; }

        public static ConfigEntry<int> whiteVoidAmount { get; set; }
        public static ConfigEntry<int> greenVoidAmount { get; set; }
        public static ConfigEntry<int> redVoidAmount { get; set; }
        public static ConfigEntry<int> yellowVoidAmount { get; set; }

        public static ConfigEntry<int> lunarAmount { get; set; }

        public void OnEnable()
        {
            itemAmount = configFile.Bind("BetterCommandArtifact", "itemAmount", 3, new ConfigDescription("Set the amount of items shown when opening a command artifact drop. \n Value must be Greater Than 0."));
            allowBoss = configFile.Bind("BetterCommandArtifact", "Allow Boss", false, new ConfigDescription("Allow boss items to have multiple options? \n Ignored if Per-tier config is enabled."));
            enablePrinter = configFile.Bind("BetterCommandArtifact", "Enable Printers and Scrappers", true, new ConfigDescription("Allow printers and scrappers to spawn while Command is enabled? Vanilla is false."));
            perTierEnabled = configFile.Bind("BetterCommandArtifact", "Use Per-tier Config", false, new ConfigDescription("Enables the per-tier section of the config."));

            whiteAmount = configFile.Bind("Tier Settings", "T1 White", 3, new ConfigDescription("How many options this tier has."));
            whiteVoidAmount = configFile.Bind("Tier Settings", "T1 White (Void)", 3, new ConfigDescription("How many options this tier has."));
            
            greenAmount = configFile.Bind("Tier Settings", "T2 Green", 3, new ConfigDescription("How many options this tier has."));
            greenVoidAmount = configFile.Bind("Tier Settings", "T2 Green (Void)", 3, new ConfigDescription("How many options this tier has."));

            redAmount = configFile.Bind("Tier Settings", "T3 Red", 3, new ConfigDescription("How many options this tier has."));
            redVoidAmount = configFile.Bind("Tier Settings", "T3 Red (Void)", 3, new ConfigDescription("How many options this tier has."));

            yellowAmount = configFile.Bind("Tier Settings", "Yellow", 1, new ConfigDescription("How many options this tier has."));
            yellowVoidAmount = configFile.Bind("Tier Settings", "Yellow (Void)", 1, new ConfigDescription("How many options this tier has."));

            equipmentAmount = configFile.Bind("Tier Settings", "Equipment", 3, new ConfigDescription("How many options this tier has."));

            lunarAmount = configFile.Bind("Tier Settings", "Lunar", 3, new ConfigDescription("How many options this tier has."));
            equipmentLunarAmount = configFile.Bind("Tier Settings", "Lunar Equipment", 3, new ConfigDescription("How many options this tier has."));

            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact += SetOptions;
            On.RoR2.Artifacts.CommandArtifactManager.OnGenerateInteractableCardSelection += CommandArtifactManager_OnGenerateInteractableCardSelection;
            On.RoR2.PickupDropletController.CreateCommandCube += CreateCommandCube;
        }

        private void CommandArtifactManager_OnGenerateInteractableCardSelection(On.RoR2.Artifacts.CommandArtifactManager.orig_OnGenerateInteractableCardSelection orig, SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
        {
            if (!enablePrinter.Value) orig(sceneDirector, dccs);
        }

        public void OnDisable()
        {
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact -= SetOptions;
        }

        void CreateCommandCube(On.RoR2.PickupDropletController.orig_CreateCommandCube orig, PickupDropletController self)
        {
            //If tier only has 1 item to drop, dont create a command cube
            int extraItems = GetExtraItemCount(self.createPickupInfo.pickupIndex);
            if (extraItems <= 0)
            {
                GenericPickupController.CreatePickup(self.createPickupInfo);
                return;
            }

            orig(self);
        }
        
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
                System.Random rnd = new System.Random();
                List<PickupIndex> list = new List<PickupIndex>();

                int extraItems = itemAmount.Value;

                if (pickupIndex != PickupIndex.none)
                {
                    extraItems = GetExtraItemCount(pickupIndex);
                    if (extraItems > 0)
                    {
                        list.Add(pickupIndex);
                    }
                }

                if (extraItems > 0)
                {
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

        public static int GetExtraItemCount(PickupIndex pickupIndex)
        {
            if (pickupIndex != PickupIndex.none)
            {
                PickupDef pd = PickupCatalog.GetPickupDef(pickupIndex);
                if (pd != null)
                {
                    bool isValidEquip = pd.equipmentIndex != EquipmentIndex.None;
                    bool isValidItem = pd.itemIndex != ItemIndex.None;
                    if (isValidEquip || isValidItem)
                    {
                        int extraItems = itemAmount.Value - 1;

                        if (isValidItem)
                        {
                            ItemDef id = ItemCatalog.GetItemDef(pd.itemIndex);
                            if (!perTierEnabled.Value)
                            {
                                if (id != null && (id.deprecatedTier == ItemTier.Boss || id.deprecatedTier == ItemTier.VoidBoss) && !allowBoss.Value)
                                {
                                    extraItems = 0;
                                }
                            }
                            else
                            {
                                switch (id.deprecatedTier)
                                {
                                    case ItemTier.Tier1:
                                        extraItems = whiteAmount.Value;
                                        break;
                                    case ItemTier.Tier2:
                                        extraItems = greenAmount.Value;
                                        break;
                                    case ItemTier.Tier3:
                                        extraItems = redAmount.Value;
                                        break;
                                    case ItemTier.Boss:
                                        extraItems = yellowAmount.Value;
                                        break;
                                    case ItemTier.VoidTier1:
                                        extraItems = whiteVoidAmount.Value;
                                        break;
                                    case ItemTier.VoidTier2:
                                        extraItems = greenVoidAmount.Value;
                                        break;
                                    case ItemTier.VoidTier3:
                                        extraItems = redVoidAmount.Value;
                                        break;
                                    case ItemTier.VoidBoss:
                                        extraItems = yellowVoidAmount.Value;
                                        break;
                                    case ItemTier.Lunar:
                                        extraItems = lunarAmount.Value;
                                        break;
                                    default:
                                        //Redundant, but here so that -1 doesn't need to be added to every case.
                                        extraItems = itemAmount.Value;
                                        break;
                                }
                                extraItems--;
                            }
                        }
                        else if (isValidEquip && perTierEnabled.Value)
                        {
                            EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(pd.equipmentIndex);

                            if (ed.isLunar)
                            {
                                extraItems = equipmentLunarAmount.Value - 1;
                            }
                            else
                            {
                                extraItems = equipmentAmount.Value - 1;
                            }
                        }

                        return extraItems;
                    }
                }
            }
            return 0;
        }
    }
}