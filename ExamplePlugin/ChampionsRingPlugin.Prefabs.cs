using ChampionsRingPlugin.Components;
using ChampionsRingPlugin.Content;
using ChampionsRingPlugin.EntityStates;
using EntityStates;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ChampionsRingPlugin.Prefabs
{
    public static class PrefabManager
    {
        public static GameObject corruptedTeleporter;
        public static InteractableSpawnCard iscCorruptedTeleporter;
        public static GameObject voidRift;
        public static InteractableSpawnCard iscVoidRift;

        public static GameObject voidSickEffect;
        public static GameObject voidSafeEffect;

        public static void Init()
        {
            Material voidCellBaseInidicatorMat = UnityEngine.Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Networkedobjects/NullSafeWard").transform.Find("Indicator").Find("IndicatorSphere").GetComponentInChildren<MeshRenderer>().material);
            Material voidCellReaverFoamMat = UnityEngine.Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Networkedobjects/NullSafeWard").transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);
            Material distortionMat = UnityEngine.Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Networkedobjects/NullSafeWard").transform.GetChild(3).GetChild(1).GetChild(3).GetComponent<ParticleSystemRenderer>().material);

            #region CorruptedTeleporter
            corruptedTeleporter = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/teleporters/Teleporter1"));
            corruptedTeleporter.SetActive(false);
            CRMissionController missionController = corruptedTeleporter.AddComponent<CRMissionController>();
            corruptedTeleporter.SetActive(true);
            missionController.target = corruptedTeleporter.GetComponent<TeleporterInteraction>();
            missionController.target.holdoutZoneController.radiusIndicator.material = UnityEngine.Object.Instantiate<Material>(voidCellBaseInidicatorMat);
            missionController.target.holdoutZoneController.radiusIndicator.material.SetVector("_TintColor", new Vector4(0.5f, 0.0f, 1.0f, 0.75f));


            var teleporterDirectors = corruptedTeleporter.GetComponents<CombatDirector>();

            foreach (CombatDirector targetTeleporterDirector in teleporterDirectors)
            {
                if (targetTeleporterDirector.customName == "Monsters")
                {
                    UnityEngine.Object.Destroy(targetTeleporterDirector);
                }
                if (targetTeleporterDirector.customName == "Boss")
                {
                    missionController.bossDirector = targetTeleporterDirector;
                }
            };


            var protectionWardGO = new GameObject();
            protectionWardGO.transform.parent = corruptedTeleporter.transform;
            protectionWardGO.transform.localPosition = Vector3.zero;

            var teamFilter1 = protectionWardGO.AddComponent<TeamFilter>();
            teamFilter1.defaultTeam = TeamIndex.Player;

            missionController.protectionWard = protectionWardGO.AddComponent<BuffWard>();
            missionController.protectionWard.buffDef = CRContentPack.protectionBuffDef;
            missionController.protectionWard.buffDuration = 0.5f;
            missionController.protectionWard.interval = 0.25f;
            missionController.protectionWard.radius = 15;
            missionController.protectionWard.floorWard = false;
            missionController.protectionWard.expires = false;
            missionController.protectionWard.invertTeamFilter = false;
            missionController.protectionWard.expireDuration = 0;
            missionController.protectionWard.removalTime = 0;
            missionController.protectionWard.removalSoundString = "";
            missionController.protectionWard.requireGrounded = false;


            var voidWardGO = new GameObject();
            voidWardGO.transform.parent = corruptedTeleporter.transform;
            voidWardGO.transform.localPosition = Vector3.zero;

            var teamFilter2 = voidWardGO.AddComponent<TeamFilter>();
            teamFilter2.defaultTeam = TeamIndex.Player;

            missionController.voidWard = voidWardGO.AddComponent<BuffWard>();
            missionController.voidWard.buffDef = CRContentPack.voidDebuffDef;
            missionController.voidWard.buffDuration = 0.5f;
            missionController.voidWard.interval = 0.25f;
            missionController.voidWard.radius = 75;
            missionController.voidWard.floorWard = false;
            missionController.voidWard.expires = false;
            missionController.voidWard.invertTeamFilter = false;
            missionController.voidWard.expireDuration = 0;
            missionController.voidWard.removalTime = 0;
            missionController.voidWard.removalSoundString = "";
            missionController.voidWard.requireGrounded = false;

            GameObject voidWardIndicator = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/NullSafeWard").transform.GetChild(1).gameObject);
            voidWardIndicator.transform.SetParent(voidWardGO.transform);
            voidWardIndicator.transform.localPosition = Vector3.zero;
            missionController.voidWard.rangeIndicator = voidWardIndicator.transform;

            voidWardIndicator.GetComponentInChildren<MeshRenderer>().material = UnityEngine.Object.Instantiate<Material>(voidCellBaseInidicatorMat);
            voidWardIndicator.GetComponentInChildren<MeshRenderer>().material.SetVector("_TintColor", new Vector4(1.0f, 0.0f, 0.25f, 0.35f));

            /*foreach (CombatDirector director in corruptedTeleporter.GetComponents<CombatDirector>())
            {
                Component.DestroyImmediate(director);
            }*/

            missionController.directors = new CombatDirector[4];
            for (int i = 0 ; i < 4 ; i++)
            {
                missionController.directors[i] = corruptedTeleporter.AddComponent<CombatDirector>();
                missionController.directors[i].enabled = false;
                missionController.directors[i].customName = "CRDirector" + i.ToString();
                missionController.directors[i].monsterCredit = 0;
                missionController.directors[i].expRewardCoefficient = 0.2f;
                missionController.directors[i].minSeriesSpawnInterval = 0.1f;
                missionController.directors[i].maxSeriesSpawnInterval = 1;
                missionController.directors[i].minRerollSpawnInterval = 2.333f;
                missionController.directors[i].maxRerollSpawnInterval = 4.333f;
                missionController.directors[i].teamIndex = TeamIndex.Monster;
                missionController.directors[i].creditMultiplier = 1f;
                missionController.directors[i].spawnDistanceMultiplier = 1;
                missionController.directors[i].shouldSpawnOneWave = false;
                missionController.directors[i].targetPlayers = true;
                missionController.directors[i].skipSpawnIfTooCheap = false;
                missionController.directors[i].resetMonsterCardIfFailed = false;
                missionController.directors[i].maximumNumberToSpawnBeforeSkipping = 6;
                missionController.directors[i].eliteBias = 1f;
                missionController.directors[i].spawnEffectPrefab = Resources.Load<GameObject>("prefabs/effects/NullifierExplosion");
            }

            missionController.inventory = corruptedTeleporter.AddComponent<Inventory>();
            corruptedTeleporter.AddComponent<EnemyInfoPanelInventoryProvider>();

            corruptedTeleporter = PrefabAPI.InstantiateClone(corruptedTeleporter, "CRTeleporter", true);
            Debug.Log("[CRCore3]: Created prefab: " + corruptedTeleporter.name);
            #endregion
            //////////
            #region VoidRifts
            voidRift = GameObject.Instantiate(Assets.voidRift);
            voidRift.transform.GetChild(0).GetComponentInChildren<MeshRenderer>().material = UnityEngine.Object.Instantiate<Material>(missionController.target.holdoutZoneController.radiusIndicator.material);

            ChildLocator childLocator = voidRift.GetComponent<ChildLocator>(); ///todo: convert all to child locator

            childLocator.FindChild("PhysicalOrb").GetComponent<MeshRenderer>().material = UnityEngine.Object.Instantiate<Material>(voidCellReaverFoamMat);
            childLocator.FindChild("DistortionParticle1").GetComponent<ParticleSystemRenderer>().material = UnityEngine.Object.Instantiate<Material>(distortionMat);
            childLocator.FindChild("DistortionParticle2").GetComponent<ParticleSystemRenderer>().material = UnityEngine.Object.Instantiate<Material>(distortionMat);
            childLocator.FindChild("DistortionParticle3").GetComponent<ParticleSystemRenderer>().material = UnityEngine.Object.Instantiate<Material>(distortionMat);
            childLocator.FindChild("DistortionParticle4").GetComponent<ParticleSystemRenderer>().material = UnityEngine.Object.Instantiate<Material>(distortionMat);

            var voidRiftHoldoutZone = voidRift.AddComponent<HoldoutZoneController>();
            voidRiftHoldoutZone.enabled = false;
            voidRiftHoldoutZone.baseRadius = 20;
            voidRiftHoldoutZone.minimumRadius = 4;
            voidRiftHoldoutZone.chargeRadiusDelta = 0;
            voidRiftHoldoutZone.baseChargeDuration = 40;
            voidRiftHoldoutZone.radiusSmoothTime = 1;
            voidRiftHoldoutZone.healingNovaRoot = voidRift.transform.GetChild(0).GetChild(1);
            voidRiftHoldoutZone.inBoundsObjectiveToken = "OBJECTIVE_RIFT_CHARGING_TOKEN";
            voidRiftHoldoutZone.outOfBoundsObjectiveToken = "OBJECTIVE_RIFT_INACTIVE_TOKEN";
            voidRiftHoldoutZone.applyFocusConvergence = true;
            voidRiftHoldoutZone.applyHealingNova = true;
            voidRiftHoldoutZone.playerCountScaling = 1;
            voidRiftHoldoutZone.radiusIndicator = voidRift.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>();
            voidRiftHoldoutZone.baseIndicatorColor = new Color(0.5f, 0.0f, 1.0f, 0.75f);

             var teamFilter = voidRift.AddComponent<TeamFilter>();
            teamFilter.defaultTeam = TeamIndex.Player;

            var riftNetworkMachine = voidRift.AddComponent<NetworkStateMachine>();
            var stateMachine = voidRift.AddComponent<EntityStateMachine>();
            stateMachine.customName = "CRRiftStateMachine";
            stateMachine.initialStateType = new SerializableEntityStateType(typeof(RiftOffState));
            stateMachine.mainStateType = new SerializableEntityStateType(typeof(RiftOnState));
            riftNetworkMachine.stateMachines = new EntityStateMachine[] { stateMachine };


            var riftInteraction = voidRift.AddComponent<PurchaseInteraction>();
            riftInteraction.displayNameToken = "CRRIFT_INTERACT_NAME";
            riftInteraction.contextToken = "CRRIFT_INTERACT_CONTEXT";
            riftInteraction.costType = CostTypeIndex.None;
            riftInteraction.available = true;
            riftInteraction.automaticallyScaleCostWithDifficulty = false;
            riftInteraction.requiredUnlockable = "";
            riftInteraction.ignoreSpherecastForInteractability = false;
            riftInteraction.setUnavailableOnTeleporterActivated = false;
            riftInteraction.isShrine = false;
            riftInteraction.isGoldShrine = false;

            var riftEntityLocator = voidRift.transform.GetChild(0).GetChild(4).GetChild(0).gameObject.AddComponent<EntityLocator>();
            riftEntityLocator.entity = voidRift;

            var voidProtectionWard = voidRift.AddComponent<BuffWard>();
            voidProtectionWard.buffDef = CRContentPack.protectionBuffDef;
            voidProtectionWard.buffDuration = 0.5f;
            voidProtectionWard.interval = 0.25f;
            voidProtectionWard.radius = 0;
            voidProtectionWard.floorWard = false;
            voidProtectionWard.expires = false;
            voidProtectionWard.invertTeamFilter = false;
            voidProtectionWard.expireDuration = 0;
            voidProtectionWard.removalTime = 0;
            voidProtectionWard.removalSoundString = "";
            voidProtectionWard.requireGrounded = false;
            voidProtectionWard.rangeIndicator = voidRift.transform.GetChild(0).GetChild(0);

            var voidRiftWard = voidRift.AddComponent<BuffWard>();
            voidRiftWard.buffDef = CRContentPack.voidDebuffDef;
            voidRiftWard.buffDuration = 0.5f;
            voidRiftWard.interval = 0.25f;
            voidRiftWard.radius = 100;
            voidRiftWard.floorWard = false;
            voidRiftWard.expires = false;
            voidRiftWard.invertTeamFilter = false;
            voidRiftWard.expireDuration = 0;
            voidRiftWard.removalTime = 0;
            voidRiftWard.removalSoundString = "";
            voidRiftWard.requireGrounded = false;
            voidRiftWard.rangeIndicator = voidRift.transform.GetChild(0).GetChild(3);
            voidRiftWard.rangeIndicator.gameObject.GetComponentInChildren<MeshRenderer>().material = UnityEngine.Object.Instantiate<Material>(voidWardIndicator.GetComponentInChildren<MeshRenderer>().material);
            voidRiftWard.rangeIndicator.gameObject.GetComponentInChildren<MeshRenderer>().material.SetVector("_TintColor", new Vector4(1.0f, 0.0f, 0.0f, 0.1f));


            VoidRiftTracker voidRiftComponentTracker = voidRift.AddComponent<VoidRiftTracker>();
            voidRiftComponentTracker.protectionWard = voidProtectionWard;
            voidRiftComponentTracker.voidWard = voidRiftWard;


            voidRift = PrefabAPI.InstantiateClone(voidRift, "CRVoidRift", true);
            Debug.Log("[CRCore3]: Created prefab: " + voidRift.name);
            #endregion
            //////////
            #region ISCs
            InteractableSpawnCard interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            interactableSpawnCard.sendOverNetwork = true;
            interactableSpawnCard.hullSize = HullClassification.Human;
            interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            interactableSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.TeleporterOK;
            interactableSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.None;
            interactableSpawnCard.directorCreditCost = 0;
            interactableSpawnCard.occupyPosition = true;
            interactableSpawnCard.eliteRules = SpawnCard.EliteRules.Default;
            interactableSpawnCard.orientToFloor = false;
            interactableSpawnCard.slightlyRandomizeOrientation = false;
            interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            interactableSpawnCard.prefab = corruptedTeleporter;
            iscCorruptedTeleporter = interactableSpawnCard;

            Debug.Log("[CRCore3]: Created isc: " + iscCorruptedTeleporter.ToString());

            InteractableSpawnCard riftSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            riftSpawnCard.sendOverNetwork = true;
            riftSpawnCard.hullSize = HullClassification.Human;
            riftSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            riftSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.TeleporterOK;
            riftSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.None;
            riftSpawnCard.directorCreditCost = 0;
            riftSpawnCard.occupyPosition = false;
            riftSpawnCard.eliteRules = SpawnCard.EliteRules.Default;
            riftSpawnCard.orientToFloor = false;
            riftSpawnCard.slightlyRandomizeOrientation = false;
            riftSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            riftSpawnCard.prefab = voidRift;
            iscVoidRift = riftSpawnCard;

            Debug.Log("[CRCore3]: Created isc: " + riftSpawnCard.ToString());
            #endregion
            /////////
            #region PostProcessing
            voidSickEffect = new GameObject();
            voidSickEffect.SetActive(false);

            var sickCameraEffectPPBase = new GameObject();
            sickCameraEffectPPBase.transform.parent = voidSickEffect.transform;
            sickCameraEffectPPBase.transform.localPosition = Vector3.zero;

            var sickCameraEffectPP = new GameObject();
            sickCameraEffectPP.transform.parent = sickCameraEffectPPBase.transform;
            sickCameraEffectPP.transform.localPosition = Vector3.zero;

            var sickVolume = sickCameraEffectPP.AddComponent<PostProcessVolume>();
            sickVolume.isGlobal = true;
            sickVolume.blendDistance = 1000;
            sickVolume.priority = 500;
            sickVolume.weight = 1;
            sickVolume.profile = Assets.voidSafePPP;
            sickVolume.sharedProfile = Assets.voidSafePPP;
            sickVolume.enabled = true;

            sickCameraEffectPP.layer = 20;
            var sickSphereCollider = sickCameraEffectPP.AddComponent<SphereCollider>();
            sickSphereCollider.center = Vector3.zero;
            sickSphereCollider.radius = 1;
            sickSphereCollider.isTrigger = false;

            var voidSickPPD = sickCameraEffectPP.AddComponent<PostProcessDuration>();
            voidSickPPD.enabled = false;
            voidSickPPD.ppVolume = sickVolume;
            voidSickPPD.maxDuration = 0.4f;
            voidSickPPD.ppWeightCurve = new AnimationCurve();
            voidSickPPD.ppWeightCurve.AddKey(0f, 0f);
            voidSickPPD.ppWeightCurve.AddKey(0.5f, 0.75f);
            voidSickPPD.ppWeightCurve.AddKey(1f, 1f);
            voidSickPPD.ppWeightCurve.preWrapMode = WrapMode.ClampForever;
            voidSickPPD.ppWeightCurve.postWrapMode = WrapMode.ClampForever;

            /*var voidSickPPD2 = voidSickEffect.AddComponent<PostProcessDuration>();
            voidSickPPD2.enabled = false;
            voidSickPPD2.ppVolume = sickVolume;
            voidSickPPD2.maxDuration = 0.4f;
            voidSickPPD2.ppWeightCurve = new AnimationCurve();
            voidSickPPD2.ppWeightCurve.AddKey(1f, 1f);
            voidSickPPD2.ppWeightCurve.AddKey(0.5f, 0.1f);
            voidSickPPD2.ppWeightCurve.AddKey(0f, 0f);
            voidSickPPD2.ppWeightCurve.preWrapMode = WrapMode.ClampForever;
            voidSickPPD2.ppWeightCurve.postWrapMode = WrapMode.ClampForever;*/

            var destroyOnTimerSick = voidSickEffect.AddComponent<DestroyOnTimer>();
            destroyOnTimerSick.duration = 0.1f;
            destroyOnTimerSick.resetAgeOnDisable = true;

            var sickCameraEffect = voidSickEffect.AddComponent<LocalCameraEffect>();
            sickCameraEffect.effectRoot = sickCameraEffectPPBase.gameObject;

            var sickTemporaryVFX = voidSickEffect.AddComponent<TemporaryVisualEffect>();
            sickTemporaryVFX.visualState = TemporaryVisualEffect.VisualState.Enter;
            sickTemporaryVFX.enterComponents = new MonoBehaviour[1];
            sickTemporaryVFX.enterComponents[0] = voidSickPPD;
            sickTemporaryVFX.exitComponents = new MonoBehaviour[1];
            sickTemporaryVFX.exitComponents[0] = destroyOnTimerSick;

            voidSickEffect.SetActive(true);
            voidSickEffect = PrefabAPI.InstantiateClone(voidSickEffect, "CRVoidSickEffect", false);
            Debug.Log("[CRCore3]: Created prefab: " + voidSickEffect.name);

            /////////////////////

            voidSafeEffect = new GameObject();
            voidSafeEffect.SetActive(false);

            var safeCameraEffectPPBase = new GameObject();
            safeCameraEffectPPBase.transform.parent = voidSafeEffect.transform;
            safeCameraEffectPPBase.transform.localPosition = Vector3.zero;

            var safeCameraEffectPP = new GameObject();
            safeCameraEffectPP.transform.parent = safeCameraEffectPPBase.transform;
            safeCameraEffectPP.transform.localPosition = Vector3.zero;

            var safeVolume = safeCameraEffectPP.AddComponent<PostProcessVolume>();
            safeVolume.isGlobal = true;
            safeVolume.blendDistance = 1000;
            safeVolume.priority = 501;
            safeVolume.weight = 1;
            safeVolume.profile = Assets.voidSickPPP;
            safeVolume.sharedProfile = Assets.voidSickPPP;
            safeVolume.enabled = true;

            safeCameraEffectPP.layer = 20;
            var safeSphereCollider = safeCameraEffectPP.AddComponent<SphereCollider>();
            safeSphereCollider.center = Vector3.zero;
            safeSphereCollider.radius = 1;
            safeSphereCollider.isTrigger = false;

            var voidSafePPD = safeCameraEffectPP.AddComponent<PostProcessDuration>();
            voidSafePPD.enabled = false;
            voidSafePPD.maxDuration = 0.4f;
            voidSafePPD.ppVolume = safeVolume;
            voidSafePPD.ppWeightCurve = new AnimationCurve();
            voidSafePPD.ppWeightCurve.AddKey(0f, 0f);
            voidSafePPD.ppWeightCurve.AddKey(0.5f, 0.75f);
            voidSafePPD.ppWeightCurve.AddKey(1f, 1f);
            voidSafePPD.ppWeightCurve.preWrapMode = WrapMode.ClampForever;
            voidSafePPD.ppWeightCurve.postWrapMode = WrapMode.ClampForever;

            /*var voidSafePPD2 = voidSafeEffect.AddComponent<PostProcessDuration>();
            voidSafePPD2.enabled = false;
            voidSafePPD2.ppVolume = safeVolume;
            voidSafePPD2.maxDuration = 0.4f;
            voidSafePPD2.ppWeightCurve = new AnimationCurve();
            voidSafePPD2.ppWeightCurve.AddKey(1f, 1f);
            voidSafePPD2.ppWeightCurve.AddKey(0.5f, 0.1f);
            voidSafePPD2.ppWeightCurve.AddKey(0f, 0f);
            voidSafePPD2.ppWeightCurve.preWrapMode = WrapMode.ClampForever;
            voidSafePPD2.ppWeightCurve.postWrapMode = WrapMode.ClampForever;*/

            var destroyOnTimerSafe = voidSafeEffect.AddComponent<DestroyOnTimer>();
            destroyOnTimerSafe.duration = 0.1f;
            destroyOnTimerSafe.resetAgeOnDisable = true;

            var safeCameraEffect = voidSafeEffect.AddComponent<LocalCameraEffect>();
            safeCameraEffect.effectRoot = safeCameraEffectPPBase.gameObject;

            var safeTemporaryVFX = voidSafeEffect.AddComponent<TemporaryVisualEffect>();
            safeTemporaryVFX.visualState = TemporaryVisualEffect.VisualState.Enter;
            safeTemporaryVFX.enterComponents = new MonoBehaviour[1];
            safeTemporaryVFX.enterComponents[0] = voidSafePPD;
            safeTemporaryVFX.exitComponents = new MonoBehaviour[1];
            safeTemporaryVFX.exitComponents[0] = destroyOnTimerSafe;

            voidSafeEffect.SetActive(true);
            voidSafeEffect = PrefabAPI.InstantiateClone(voidSafeEffect, "CRVoidSafeEffect", false);
            Debug.Log("[CRCore3]: Created prefab: " + voidSafeEffect.name);
            #endregion

        }

    }
    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static AssetBundleResourcesProvider Provider;

        public static Sprite artifactChampionOn;
        public static Sprite artifactChampionOff;

        public static PostProcessProfile voidSickPPP;
        public static PostProcessProfile voidSafePPP;

        public static GameObject voidRift;

        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CRCore3.crcore3"))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                    ResourcesAPI.AddProvider(new AssetBundleResourcesProvider("@CRC3", MainAssetBundle));
                }
            }

            // include this if you're using a custom soundbank
            /*using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleSurvivor.ExampleSurvivor.bnk"))
            {

                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }*/

            // and now we gather the assets

            artifactChampionOn = MainAssetBundle.LoadAsset<Sprite>("ArtifactChampionsOn");
            artifactChampionOff = MainAssetBundle.LoadAsset<Sprite>("ArtifactChampionsOff");

            voidRift = MainAssetBundle.LoadAsset<GameObject>("VoidRift");

            voidSickPPP = MainAssetBundle.LoadAsset<PostProcessProfile>("voidSickPPP");
            var sickFog = ScriptableObject.CreateInstance<RampFog>();
            sickFog.enabled.value = true;
            sickFog.fogIntensity.value = 1.0f;
            sickFog.fogIntensity.overrideState = true;
            sickFog.fogPower.value = 0.75f;
            sickFog.fogPower.overrideState = true;
            sickFog.fogOne.value = 0.05f;
            sickFog.fogOne.overrideState = true;
            sickFog.fogZero.value = -0.032f;
            sickFog.fogZero.overrideState = true;
            sickFog.fogColorStart.value = new Color32(130, 65, 62, 0);
            sickFog.fogColorStart.overrideState = true;
            sickFog.fogColorMid.value = new Color32(43, 51, 65, 180);
            sickFog.fogColorMid.overrideState = true;
            sickFog.fogColorEnd.value = new Color32(27, 10, 36, 240);
            sickFog.fogColorEnd.overrideState = true;
            sickFog.skyboxStrength.value = 0.15f;
            sickFog.skyboxStrength.overrideState = true;
            Assets.voidSickPPP.AddSettings(sickFog);

            voidSafePPP = MainAssetBundle.LoadAsset<PostProcessProfile>("voidSafePPP");
            var safeFog = ScriptableObject.CreateInstance<RampFog>();
            safeFog.enabled.value = true;
            safeFog.fogIntensity.value = 1.0f;
            safeFog.fogIntensity.overrideState = true;
            safeFog.fogPower.value = 0.75f;
            safeFog.fogPower.overrideState = true;
            safeFog.fogOne.value = 0.05f;
            safeFog.fogOne.overrideState = true;
            safeFog.fogZero.value = -0.032f;
            safeFog.fogZero.overrideState = true;
            safeFog.fogColorStart.value = new Color32(130, 65, 62, 20);
            safeFog.fogColorStart.overrideState = true;
            safeFog.fogColorMid.value = new Color32(43, 51, 65, 220);
            safeFog.fogColorMid.overrideState = true;
            safeFog.fogColorEnd.value = new Color32(27, 10, 36, 250);
            safeFog.fogColorEnd.overrideState = true;
            safeFog.skyboxStrength.value = 0;
            safeFog.skyboxStrength.overrideState = true;
            Assets.voidSafePPP.AddSettings(safeFog);
        }
    }
}
