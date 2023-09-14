using System.Security;
using System.Security.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine.Networking;

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
        public const string PluginVersion = "1.3.0";

        public static ConfigFile configFile = new ConfigFile(Paths.ConfigPath + "\\BetterCommandArtifact.cfg", true);

        public static ConfigEntry<int> itemAmount { get; set; }
        public static ConfigEntry<bool> allowBoss { get; set; }

        public static ConfigEntry<bool> enablePrinter { get; set; }

        public void OnEnable()
        {
            itemAmount = configFile.Bind("BetterCommandArtifact", "itemAmount", 3, new ConfigDescription("Set the amount of items shown when opening a command artifact drop. \n Value must be Greater Than 0."));
            allowBoss = configFile.Bind("BetterCommandArtifact", "Allow Boss", false, new ConfigDescription("Allow boss items to have multiple options?"));
            enablePrinter = configFile.Bind("BetterCommandArtifact", "Enable Printers and Scrappers", true, new ConfigDescription("Allow printers and scrappers to spawn while Command is enabled? Vanilla is false."));

            Config.SettingChanged += ConfigOnSettingChanged;
            On.RoR2.PickupPickerController.SetOptionsFromPickupForCommandArtifact += SetOptions;
            On.RoR2.Artifacts.CommandArtifactManager.OnGenerateInteractableCardSelection += CommandArtifactManager_OnGenerateInteractableCardSelection;
        }

        private void CommandArtifactManager_OnGenerateInteractableCardSelection(On.RoR2.Artifacts.CommandArtifactManager.orig_OnGenerateInteractableCardSelection orig, SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
        {
            if (!enablePrinter.Value) orig(sceneDirector, dccs);
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
                    PickupDef pd = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pd != null)
                    {
                        bool isValidEquip = pd.equipmentIndex != EquipmentIndex.None;
                        bool isValidItem = pd.itemIndex != ItemIndex.None;
                        if (isValidEquip || isValidItem)
                        {
                            list.Add(pickupIndex);
                            extraItems--;

                            if (isValidItem)
                            {
                                ItemDef id = ItemCatalog.GetItemDef(pd.itemIndex);
                                if (id != null && (id.deprecatedTier == ItemTier.Boss || id.deprecatedTier == ItemTier.VoidBoss) && !allowBoss.Value)
                                {
                                    extraItems = 0;
                                }
                            }
                        }
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
    }
}