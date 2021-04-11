using System;
using System.Reflection;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;

using System.Reflection;

using UnityEngine;
using UnityEngine.Networking;

using ChampionsRingPlugin.Content;
using ChampionsRingPlugin.Components;
using ChampionsRingPlugin.Prefabs;
using RoR2.UI.LogBook;
using R2API.Networking;

namespace ChampionsRingPlugin.Core
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("PlasmaCore.CRCore3", "ChampionsRingPlugin", "0.0.0")]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(ResourcesAPI), nameof(NetworkingAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class CRCore3 : BaseUnityPlugin
    {
        /*public static ConfigFile CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\PlasmaCore.BaseMod.cfg", true);
        public static ConfigEntry<bool> exampleConfig = CustomConfigFile.Bind<bool>("BaseMod Config", "Enable", true, "Enable the only function of this mod.");*/

        public static string[] AIBlacklist = new string[]
        {
            "Bandolier",
            "BonusGoldPackOnKill",
            "Dagger",
            "EnergizedOnEquipmentUse",
            "EquipmentMagazine",
            "ExecuteLowHealthElite",
            "Firework",
            "GhostOnKill",
            "HeadHunter",
            "IceRing",
            "KillEliteFrenzy",
            "LaserTurbine",
            "NovaOnHeal",
            "ShockNearby",
            "SprintArmor",
            "SprintBonus",
            "Squid",
            "TPHealingNova",
            "Talisman",
            "Thorns",
            "TreasureCache",
            "WarCryOnMultiKill",
            "WardOnLevel",
            "BarrierOnKill",
            "BarrierOnOverHeal",
            "BossDamageBonus"
        };
        public static string[] AIBlacklistLunar = new string[]
        {
            "AutoCastEquipment",
            "FocusConvergence",
            "GoldOnHit",
            "LunarPrimaryReplacement",
            "LunarSecondaryReplacement",
            "LunarSpecialReplacement",
            "LunarTrinket",
            "LunarUtilityReplacement",
            "MonstersOnShrineUse",
            "RandomDamageZone"
        };
        public static string[] AIBlacklistBoss = new string[]
        {
            "BeetleGland",
            "NovaOnLowHealth",
            "RoboBallBuddy",
            "SprintWisp"
        };
        public static IDictionary<string, int> itemCountOverrides = new Dictionary<string, int>();

        public static CRRunController runController;
        public CRCore3()
        {
            NetworkingAPI.RegisterMessageType<CRMissionNetworkMessage>();
            itemCountOverrides.Add("ArmorPlate", 1);
            itemCountOverrides.Add("ArmorReductionOnHit", 3);
            itemCountOverrides.Add("Bear", 1);
            itemCountOverrides.Add("Behemoth", 3);
            itemCountOverrides.Add("BounceNearby", 2);
            itemCountOverrides.Add("Clover", 2);
            itemCountOverrides.Add("FireRing", 1);
            itemCountOverrides.Add("FlatHealth", 10);
            itemCountOverrides.Add("HealOnCrit", 10);
            itemCountOverrides.Add("Icicle", 3);
            itemCountOverrides.Add("Knurl", 5);
            itemCountOverrides.Add("Medkit", 2);
            itemCountOverrides.Add("Plant", 10);
            itemCountOverrides.Add("SecondarySkillMagazine", 10);
            itemCountOverrides.Add("Seed", 20);
            itemCountOverrides.Add("SlowOnHit", 2);
            itemCountOverrides.Add("UtilitySkillMagazine", 2);
            itemCountOverrides.Add("BleedOnHit", 11);
        }
        public void Start()
        {
            foreach (string name in UserProfile.GetAvailableProfileNames())
            {
                Debug.LogWarning("Located profile: " + name);
                //UserProfile.DeleteUserProfile(name);
                //UserProfile.defaultProfile = null;
            }
        }
        public void Awake() //runs when the mod is loaded
        {
            Assets.PopulateAssets();

            On.RoR2.ContentManager.SetContentPacks += (orig, packs) =>
            {
                packs.Add(new CRContentPack());
                orig(packs);
            };

            LanguageAPI.Add("ARTIFACT_CRCORE_NAME", "Artifact of Void");
            LanguageAPI.Add("ARTIFACT_CRCORE_DESC", "Enables Champion's Ring 3 gamemode");

            LanguageAPI.Add("OBJECTIVE_RIFT_CHARGING_TOKEN", "Close The Rift ({0}%)");
            LanguageAPI.Add("OBJECTIVE_RIFT_INACTIVE_TOKEN", "Enter The Rift ({0}%)");
            LanguageAPI.Add("OBJECTIVE_RIFT_TOTAL_TOKEN", "Destroy Void Rifts ({0}/{1})");

            LanguageAPI.Add("CRRIFT_INTERACT_NAME", "Void Rift");
            LanguageAPI.Add("CRRIFT_INTERACT_CONTEXT", "Open");

            LanguageAPI.Add("CRRIFT_INTEGRATE_ENEMY", "[WARNING]: {0} was released from the void!");
            LanguageAPI.Add("CRRIFT_INTEGRATE_ITEM", "[WARNING]: {0} was integrated from the void!");
            if (!CRContentPack.initalized)
            {
                CRContentPack.Init();
            }
            PrefabManager.Init();


            On.RoR2.Run.Start += (orig, self) =>
            {
                orig(self);
                if (RunArtifactManager.instance)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(CRContentPack.artifactCR.artifactIndex))
                    {
                        runController = ScriptableObject.CreateInstance<CRRunController>();

                        On.RoR2.SceneDirector.PlaceTeleporter += TeleporterPlaceHook;
                        On.RoR2.SceneDirector.Start += OnSceneStart;
                        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += OnUpdateVisualEffects;
                        On.RoR2.TeleporterInteraction.OnInteractionBegin += OnTeleporterInteract;
                        Debug.Log("[CRCore3]: Hooks added.");
                    }
                    else
                    {
                        On.RoR2.SceneDirector.PlaceTeleporter -= TeleporterPlaceHook;
                        On.RoR2.SceneDirector.Start -= OnSceneStart;
                        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects -= OnUpdateVisualEffects;
                        On.RoR2.TeleporterInteraction.OnInteractionBegin -= OnTeleporterInteract;
                        Debug.Log("[CRCore3]: Hooks removed.");
                    }
                }

            };
            On.RoR2.Run.OnDestroy += (orig, self) =>
            {
                orig(self);
                if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(CRContentPack.artifactCR.artifactIndex))
                {
                    Destroy(runController);

                    On.RoR2.SceneDirector.PlaceTeleporter -= TeleporterPlaceHook;
                    On.RoR2.SceneDirector.Start -= OnSceneStart;
                    On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects -= OnUpdateVisualEffects;
                    On.RoR2.TeleporterInteraction.OnInteractionBegin -= OnTeleporterInteract;
                    Debug.Log("[CRCore3]: Hooks removed.");
                }
            };
        }
        public void OnTeleporterInteract(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor interactor)
        {
            orig(self, interactor);
            if (CRMissionController.instance)
            {
                CRMissionController.instance.BeginRoundOnInteract();
            }
            else
            {
                Debug.LogWarning("[CRCore3]: OnTeleporterInteract - No CRMissionController instance found!");
            }
        }
        public void OnUpdateVisualEffects(On.RoR2.CharacterBody.orig_UpdateAllTemporaryVisualEffects orig, CharacterBody self)
        {
            orig(self);
            CustomTempVFXManager vfxmanager = self.gameObject.GetComponent<CustomTempVFXManager>();
            if (!vfxmanager)
            {
                vfxmanager = self.gameObject.AddComponent<CustomTempVFXManager>();
                vfxmanager.characterBody = self;
            }
        }
        public void TeleporterPlaceHook(On.RoR2.SceneDirector.orig_PlaceTeleporter orig, SceneDirector self) //NOTE: DEAD END HOOK!!
        {
            if (!self.teleporterInstance && self.teleporterSpawnCard)
            {
                if (PrefabManager.iscCorruptedTeleporter)
                {
                    self.teleporterInstance = self.directorCore.TrySpawnObject(new DirectorSpawnRequest(PrefabManager.iscCorruptedTeleporter, new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Random
                    }, self.rng));
                    Run.instance.OnServerTeleporterPlaced(self, self.teleporterInstance);
                }

                GameObject[] vents = new GameObject[4];

                for (int i = 0; i < vents.Length; i++)
                {
                    bool validPlacement;
                    for (int j = 0; j < 25; j++)
                    {
                        validPlacement = true;
                        vents[i] = self.directorCore.TrySpawnObject(new DirectorSpawnRequest(PrefabManager.iscVoidRift, new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Random
                        }, self.rng));
                        if (Vector3.Distance(self.teleporterInstance.transform.position, vents[i].transform.position) <= 75) { validPlacement = false; }
                        if (validPlacement)
                        {
                            for (int k = i - 1; k >= 0; k--)
                            {
                                if (Vector3.Distance(vents[k].transform.position, vents[i].transform.position) <= 75)
                                {
                                    validPlacement = false;
                                    break;
                                }
                            }
                        }
                        if (validPlacement) { break; }
                        if (j == 24)
                        {
                            Debug.LogWarning("[CRCore3]: To many attempts to spawn VoidRift. Defaulting to sub-optimal placement.");
                            break;
                        }
                        Debug.LogWarning("[CRCore3]: VoidRift placement to close to other objective. Retrying.");
                        GameObject.Destroy(vents[i]);
                    }
                }

                self.teleporterInstance.GetComponent<CRMissionController>().voidRifts = vents;
                self.teleporterInstance.GetComponent<CRMissionController>().Start();
            }
        }
        public void OnSceneStart(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "skymeadow")
            {
                Destroy(GameObject.Find("SMSkyboxPrefab"));
            }
        }
        //public void OnGenerateInteractableCardSelection(SceneDirector director, DirectorCardCategorySelection cardSelection)
        //{
        //    cardSelection.RemoveCardsThatFailFilter(new Predicate<DirectorCard>(RemoveChestCards));
        //}
        //public static bool RemoveChestCards(DirectorCard card)
        //{
        //    return !(card.spawnCard as InteractableSpawnCard).skipSpawnWhenSacrificeArtifactEnabled;
        //}
    }
}