using ChampionsRingPlugin.EntityStates;
using ChampionsRingPlugin.Prefabs;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using UnityEngine;

namespace ChampionsRingPlugin.Content
{
    public class CRContentPackProvider : IContentPackProvider
    {
        public string identifier => "CRContent";

        internal static ContentPack ContentPack = new ContentPack();

        public static BuffDef voidDebuffDef;
        public static BuffDef protectionBuffDef;
        public static ArtifactDef artifactCR;

        internal static void Init()
        {
            ContentManager.collectContentPackProviders += AddCustomContent;

            artifactCR = ScriptableObject.CreateInstance<ArtifactDef>();
            artifactCR.nameToken = "ARTIFACT_CRCORE_NAME";
            artifactCR.descriptionToken = "ARTIFACT_CRCORE_DESC";
            artifactCR.unlockableDef = null;
            artifactCR.smallIconSelectedSprite = Assets.artifactChampionOn;
            artifactCR.smallIconDeselectedSprite = Assets.artifactChampionOff;
            artifactCR.pickupModelPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ContentPack.artifactDefs.Add(new ArtifactDef[] { artifactCR });


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

            ContentPack.buffDefs.Add(new BuffDef[] { protectionBuffDef, voidDebuffDef });

            ContentPack.entityStateTypes.Add(new Type[] { typeof(RiftBaseState), typeof(RiftOffState), typeof(RiftOnState), typeof(RiftCompleteState) });
        }

        private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {

            addContentPackProvider(new CRContentPackProvider());
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(ContentPack, args.output);

            args.ReportProgress(1);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            args.ReportProgress(1);
            yield break;
        }
    }
}
