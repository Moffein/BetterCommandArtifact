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
        public const string PluginVersion = "1.5.1";

        public static ConfigFile configFile = new ConfigFile(Paths.ConfigPath + "\\BetterCommandArtifact.cfg", true);

        public static ConfigEntry<bool> allowTemp { get; set; }
        public static ConfigEntry<int> itemAmount { get; set; }
        public static ConfigEntry<bool> allowBoss { get; set; }

        public static ConfigEntry<bool> enablePrinter { get; set; }

        public static ConfigEntry<bool> perTierEnabled { get; set; }

        public static ConfigEntry<int> whiteAmount { get; set; }
        public static ConfigEntry<int> greenAmount { get; set; }
        public static ConfigEntry<int> redAmount { get; set; }
        public static ConfigEntry<int> yellowAmount { get; set; }
        public static ConfigEntry<int> foodAmount { get; set; }

        public static ConfigEntry<int> defaultAmount { get; set; }

        public static ConfigEntry<int> equipmentAmount { get; set; }
        public static ConfigEntry<int> equipmentLunarAmount { get; set; }

        public static ConfigEntry<int> whiteVoidAmount { get; set; }
        public static ConfigEntry<int> greenVoidAmount { get; set; }
        public static ConfigEntry<int> redVoidAmount { get; set; }
        public static ConfigEntry<int> yellowVoidAmount { get; set; }

        public static ConfigEntry<int> lunarAmount { get; set; }

        public void OnEnable()
        {
            allowTemp = configFile.Bind("BetterCommandArtifact", "Allow Temporary ITems", false, new ConfigDescription("Allow Temporary items to be selected?"));
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

            foodAmount = configFile.Bind("Tier Settings", "Food", 3, new ConfigDescription("How many options this tier has."));

            defaultAmount = configFile.Bind("Tier Settings", "Default", 3, new ConfigDescription("How many options for items that fall outside of the listed tiers."));

            equipmentAmount = configFile.Bind("Tier Settings", "Equipment", 3, new ConfigDescription("How many options this tier has."));

            lunarAmount = configFile.Bind("Tier Settings", "Lunar", 3, new ConfigDescription("How many options this tier has."));
            equipmentLunarAmount = configFile.Bind("Tier Settings", "Lunar Equipment", 3, new ConfigDescription("How many options this tier has."));

            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact_PickupIndex += PickupPickerController_SetOptionsFromPickupForCommandArtifact_PickupIndex;
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact_UniquePickup += PickupPickerController_SetOptionsFromPickupForCommandArtifact_UniquePickup;
            On.RoR2.Artifacts.CommandArtifactManager.OnGenerateInteractableCardSelection += CommandArtifactManager_OnGenerateInteractableCardSelection;
            On.RoR2.PickupDropletController.CreateCommandCube += CreateCommandCube;
        }

        private void CommandArtifactManager_OnGenerateInteractableCardSelection(On.RoR2.Artifacts.CommandArtifactManager.orig_OnGenerateInteractableCardSelection orig, SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
        {
            if (!enablePrinter.Value) orig(sceneDirector, dccs);
        }

        public void OnDisable()
        {
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact_PickupIndex -= PickupPickerController_SetOptionsFromPickupForCommandArtifact_PickupIndex;
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact_UniquePickup -= PickupPickerController_SetOptionsFromPickupForCommandArtifact_UniquePickup;
            On.RoR2.Artifacts.CommandArtifactManager.OnGenerateInteractableCardSelection -= CommandArtifactManager_OnGenerateInteractableCardSelection;
            On.RoR2.PickupDropletController.CreateCommandCube -= CreateCommandCube;
        }

        void CreateCommandCube(On.RoR2.PickupDropletController.orig_CreateCommandCube orig, PickupDropletController self)
        {
            //If tier only has 1 item to drop, dont create a command cube
            bool isTemp = self.createPickupInfo.pickup.isTempItem && !allowTemp.Value;

            int extraItems = GetExtraItemCount(self.createPickupInfo.pickup.pickupIndex);
            if (extraItems <= 0 || isTemp)
            {
                GenericPickupController.CreatePickup(self.createPickupInfo);
                return;
            }

            orig(self);
        }

        private void PickupPickerController_SetOptionsFromPickupForCommandArtifact_PickupIndex(On.RoR2.PickupPickerController.orig_SetOptionsFromPickupForCommandArtifact_PickupIndex orig, PickupPickerController self, PickupIndex pickupIndex)
        {
            self.SetOptionsFromPickupForCommandArtifact(new UniquePickup()
            {
                pickupIndex = pickupIndex
            });
        }

        private void PickupPickerController_SetOptionsFromPickupForCommandArtifact_UniquePickup(On.RoR2.PickupPickerController.orig_SetOptionsFromPickupForCommandArtifact_UniquePickup orig, PickupPickerController self, UniquePickup pickup)
        {
            if (!NetworkServer.active) return;
            var newSelection = PickupPickerController.GetOptionsFromPickupState(pickup);
            PickupPickerController.Option[] array;

            if (newSelection == null)
            {
                array = new PickupPickerController.Option[1]
                {
                    new PickupPickerController.Option
                    {
                        available = true,
                        pickup = pickup
                    }
                };
            }
            else
            {
                System.Random rnd = new System.Random();
                List<PickupPickerController.Option> list = new List<PickupPickerController.Option>();

                int extraItems = itemAmount.Value;

                if (pickup.pickupIndex != PickupIndex.none)
                {
                    extraItems = GetExtraItemCount(pickup.pickupIndex);
                    if (extraItems > 0)
                    {
                        list.Add(new PickupPickerController.Option
                        {
                            pickup = pickup
                        });
                    }
                }

                if (extraItems > 0)
                {
                    List<PickupPickerController.Option> additionalOptions = (from x in newSelection.ToList() orderby rnd.Next() select x).Where(x => (Run.instance.IsPickupAvailable(x.pickupIndex) && x.pickupIndex != pickup.pickupIndex)).Take(extraItems).ToList();
                    list.AddRange(additionalOptions);
                }

                array = new PickupPickerController.Option[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    PickupPickerController.Option pickupOption2 = list[i];
                    array[i] = new PickupPickerController.Option
                    {
                        available = Run.instance.IsPickupAvailable(pickupOption2.pickupIndex),
                        pickup = pickupOption2.pickup
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
                                if (id != null && (id.tier == ItemTier.Boss || id.tier == ItemTier.VoidBoss) && !allowBoss.Value)
                                {
                                    extraItems = 0;
                                }
                            }
                            else
                            {
                                switch (id.tier)
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
                                    case ItemTier.FoodTier:
                                        extraItems = foodAmount.Value;
                                        break;
                                    default:
                                        extraItems = defaultAmount.Value;
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