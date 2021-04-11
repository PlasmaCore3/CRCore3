using ChampionsRingPlugin.EntityStates;
using ChampionsRingPlugin.Prefabs;
using RoR2;
using System;
using UnityEngine;

namespace ChampionsRingPlugin.Content
{
    public class CRContentPack : ContentPack
    {
        public static bool initalized = false;

        public static ArtifactDef artifactCR;
        public static UnlockableDef artifactCRUnlockable;

        public static BuffDef voidDebuffDef;
        public static BuffDef protectionBuffDef;

        public CRContentPack()
        {
            if (!initalized)
            {
                Init();
            }
            base.artifactDefs = new ArtifactDef[1];

            artifactDefs[0] = artifactCR;

            base.buffDefs = new BuffDef[2];
            buffDefs[0] = voidDebuffDef;
            buffDefs[1] = protectionBuffDef;

            base.entityStateTypes = new Type[4];
            entityStateTypes[0] = typeof(RiftBaseState);
            entityStateTypes[1] = typeof(RiftOffState);
            entityStateTypes[2] = typeof(RiftOnState);
            entityStateTypes[3] = typeof(RiftCompleteState);

        }
        public static void Init()
        {
            artifactCR = ScriptableObject.CreateInstance<ArtifactDef>();
            artifactCR.nameToken = "ARTIFACT_CRCORE_NAME";
            artifactCR.descriptionToken = "ARTIFACT_CRCORE_DESC";
            artifactCR.unlockableDef = null;
            artifactCR.smallIconSelectedSprite = Assets.artifactChampionOn;
            artifactCR.smallIconDeselectedSprite = Assets.artifactChampionOff;
            artifactCR.pickupModelPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);


            voidDebuffDef = ScriptableObject.CreateInstance<BuffDef>();
            voidDebuffDef.name = "CRVoidDebuff";
            voidDebuffDef.buffColor = new Color32(255, 100, 150, 255);
            voidDebuffDef.canStack = false;
            voidDebuffDef.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffNullifyStackIcon");
            voidDebuffDef.isDebuff = true;
            Debug.Log("Initalized Buff: " + voidDebuffDef.name);

            protectionBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            protectionBuffDef.name = "CRVoidProtectionBuff";
            protectionBuffDef.buffColor = new Color32(100, 30, 255, 255);
            protectionBuffDef.canStack = false;
            protectionBuffDef.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffNullifiedIcon");
            protectionBuffDef.isDebuff = false;
            Debug.Log("Initalized Buff: " + protectionBuffDef.name);
            initalized = true;
        }
    }
}
