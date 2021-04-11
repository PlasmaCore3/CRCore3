using ChampionsRingPlugin.Content;
using ChampionsRingPlugin.Core;
using ChampionsRingPlugin.EntityStates;
using ChampionsRingPlugin.Prefabs;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//todo: Objective marker
//      Network vent count

namespace ChampionsRingPlugin.Components
{
    public class CRRunController : ScriptableObject
    {
        public int stagesCleared = 0;
        public static CRRunController instance;
        public void Awake()
        {
            instance = this;
        }
    }
    public class CRMissionController : NetworkBehaviour //Attached to teleporter. Controls and stores rifts, and how many are completed. 
    {
        public static int roundsCount = 4;
        public int roundsStarted = 0;
        public int roundsCleared = 0;
        public static CRMissionController instance;

        public CombatDirector[] directors;//Per round directors
        public CombatDirector bossDirector;


        public TeleporterInteraction target;
        public Inventory inventory;

        public WeightedSelection<DirectorCard> monsterCards;
        public List<DirectorCard> pickedMonsterCards;

        public Xoroshiro128Plus rng;

        public List<PickupIndex> availableTier1DropList;
        public List<PickupIndex> availableTier2DropList;
        public List<PickupIndex> availableTier3DropList;

        public GameObject[] voidRifts = new GameObject[roundsCount];

        float degenTimer = 0;
        public static float degenTickFrequency = 1f;

        public BuffWard protectionWard;
        public BuffWard voidWard;
        public void Start()
        {
            if (Run.instance)
            {
                availableTier1DropList = Run.instance.availableTier1DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();
                availableTier2DropList = Run.instance.availableTier2DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();
                availableTier3DropList = Run.instance.availableTier3DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();

                monsterCards = Util.CreateReasonableDirectorCardSpawnList(50 * Run.instance.difficultyCoefficient, 6, 2);
            }

            instance = this;

            target.locked = true;

            if (NetworkServer.active)
            {
                this.rng = new Xoroshiro128Plus((ulong)Run.instance.stageRng.nextUint);
                for (int i = 0; i < this.directors.Length; i++)
                {
                    CombatDirector combatDirector = this.directors[i];
                    combatDirector.maximumNumberToSpawnBeforeSkipping = 6;
                    combatDirector.spawnDistanceMultiplier = 2;
                    combatDirector.eliteBias = 1;
                    combatDirector.onSpawnedServer.AddListener(delegate (GameObject targetGameObject) { this.ModifySpawnedMasters(targetGameObject); });
                }
                bossDirector.onSpawnedServer.AddListener(delegate (GameObject targetGameObject) { this.ModifySpawnedMasters(targetGameObject); });
            }
            if (DirectorCore.instance)
            {
                var coreCombatDirectors = DirectorCore.instance.gameObject.GetComponents<CombatDirector>();
                foreach (CombatDirector coreDirector in coreCombatDirectors)
                {
                    coreDirector.enabled = false;
                    coreDirector.creditMultiplier = 0;
                }
            }
            else
            {
                Debug.LogWarning("[CRCore3]: CRMissionController.Start - DirectorCore not instantiated!");
            }

        }
        [Server]
        public void BeginRoundOnInteract()
        {
            BeginRound(base.gameObject);
        }

        [Server]
        public void ModifySpawnedMasters(GameObject targetGameObject)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::ModifySpawnedMasters(UnityEngine.GameObject)' called on client");
                return;
            }
            CharacterMaster component = targetGameObject.GetComponent<CharacterMaster>();
            CharacterBody body = component.GetBody();
            if (body)
            {
                foreach (EntityStateMachine entityStateMachine in body.GetComponents<EntityStateMachine>())
                {
                    entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                }
            }
            component.inventory.AddItemsFrom(this.inventory);
        }

        public void FixedUpdate()
        {
            //Make protection ward match teleported bubble, and void bubble up to twice the full radius.
            protectionWard.radius = target.holdoutZoneController.currentRadius;

            if (NetworkServer.active)
            {
                NetMessageExtensions.Send(new CRMissionNetworkMessage { riftsCompleted = this.roundsCleared }, R2API.Networking.NetworkDestination.Clients);
            }

            if (!target.isCharged)
            {
                voidWard.radius = (protectionWard.radius * 2) + 100 + (15 * roundsCleared);
            }
            else if (voidWard.radius > 0.01)
            {
                voidWard.radius /= 1.1f;
            }


            this.degenTimer += Time.fixedDeltaTime;
            if (this.degenTimer > 1f / degenTickFrequency)
            {
                this.degenTimer -= 1f / degenTickFrequency;
                foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Player))
                {
                    if (!teamComponent.body.HasBuff(BuffCatalog.FindBuffIndex("CRVoidProtectionBuff")) && teamComponent.body.HasBuff(BuffCatalog.FindBuffIndex("CRVoidDebuff")))
                    {
                        float damage = (3.0f / 100f) / degenTickFrequency * teamComponent.body.healthComponent.fullCombinedHealth;
                        teamComponent.body.healthComponent.TakeDamage(new DamageInfo
                        {
                            damage = damage,
                            position = teamComponent.body.corePosition,
                            damageType = DamageType.Silent
                        });
                    }
                }
            }
        }
        public void BeginRound(GameObject centerPoint)
        {
            //Add monster card to, and activate combat director
            //^ Make directors more powerful if more events are active?

            roundsStarted++;
            if (NetworkServer.active)
            {
                if (monsterCards.Count <= 0)
                {
                    Debug.LogWarning("[CRCore3]: CRMissionController.BeginRound - No monsters left to chose from! Attempting to get new SpawnCards...");
                    WeightedSelection<DirectorCard> newCards = Util.CreateReasonableDirectorCardSpawnList(60 * Run.instance.difficultyCoefficient, 6, 2);
                    foreach (var newCardInfo in newCards.choices)
                    {
                        DirectorCard newCard = newCardInfo.value;
                        bool isUsed = false;
                        foreach (DirectorCard selectedCard in pickedMonsterCards)
                        {
                            if (selectedCard == newCard)
                            {
                                isUsed = true;
                                break;
                            }
                        }
                        if (!isUsed && newCard != null && newCard.spawnCard)
                        {
                            monsterCards.AddChoice(newCard, newCardInfo.weight);
                            Debug.Log("[CRCore3]: CRMissionController.BeginRound - Found valid SpawnCard " + newCard.spawnCard.name + ". Adding to options.");
                        }
                    }
                }
                if (monsterCards.Count > 0)
                {
                    int selectedIndex = monsterCards.EvaluteToChoiceIndex(this.rng.nextNormalizedFloat);
                    pickedMonsterCards.Add(monsterCards.choices[selectedIndex].value);
                    if (NetworkServer.active)
                    {
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#E6B3FF>[WARNING]: " + Language.GetString(monsterCards.choices[selectedIndex].value.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().baseNameToken) + "s were released from the void!</color>" });
                    }
                    monsterCards.RemoveChoice(selectedIndex);
                }
                else
                {
                    Debug.LogWarning("[CRCore3]: CRMissionController.BeginRound - No monsters left to chose from! Reverting to already selected SpawnCard.");
                    pickedMonsterCards.Add(pickedMonsterCards[this.rng.RangeInt(0, pickedMonsterCards.Count)]);
                }


                if (Run.instance)
                {
                    if (availableTier1DropList == null)
                    {
                        availableTier1DropList = Run.instance.availableTier1DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();
                    }
                    if (availableTier2DropList == null)
                    {
                        availableTier2DropList = Run.instance.availableTier2DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();
                    }
                    if (availableTier3DropList == null)
                    {
                        availableTier3DropList = Run.instance.availableTier3DropList.Where(new Func<PickupIndex, bool>(ArenaMissionController.IsPickupAllowedForMonsters)).ToList<PickupIndex>();
                    }
                    if (monsterCards == null)
                    {
                        monsterCards = Util.CreateReasonableDirectorCardSpawnList(50 * Run.instance.difficultyCoefficient, 6, 2);
                    }
                }
                else
                {
                    Debug.LogError("[CRCore]: Run.instance does not exist!");
                }

                for (int i = 0; i < pickedMonsterCards.Count; i++)
                {
                    if (i >= directors.Length) { break; }
                    directors[i].OverrideCurrentMonsterCard(pickedMonsterCards[i]);
                    directors[i].monsterCredit += ((50 + 0.15f * this.roundsStarted) * Run.instance.difficultyCoefficient) / pickedMonsterCards.Count;
                    directors[i].creditMultiplier = 0.15f * this.roundsStarted / pickedMonsterCards.Count;
                    directors[i].monsterSpawnTimer = 0;
                    directors[i].currentSpawnTarget = centerPoint;
                    directors[i].enabled = true;
                }

                //Add items to monster inventory

                List<PickupIndex> list;
                int count;
                switch (roundsStarted)
                {
                    default:
                        list = availableTier1DropList;
                        count = rng.RangeInt(3, 7);
                        break;
                    case 3:
                    case 4:
                        list = availableTier2DropList;
                        count = rng.RangeInt(2, 5);
                        break;
                    case 5:
                        list = availableTier3DropList;
                        count = 1;
                        break;
                }
                PickupIndex pickupIndex;
                PickupDef pickupDef;
                ItemIndex itemIndex = ItemIndex.None;
                for (int i = 0; i < 25; i++)
                {
                    pickupIndex = this.rng.NextElementUniform<PickupIndex>(list);
                    list.Remove(pickupIndex);
                    pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    itemIndex = pickupDef.itemIndex;
                    bool badItem = false;
                    foreach (string itemName in CRCore3.AIBlacklist)
                    {
                        if (ItemCatalog.GetItemDef(itemIndex).nameToken.ToLower().Contains(itemName.ToLower()))
                        {
                            badItem = true;
                            break;
                        }
                    }
                    if (!badItem) { break; }
                    Debug.Log("[CRCore]: Item was blacklisted! Trying again.");
                }
                int dictOut;
                if (CRCore3.itemCountOverrides.ContainsKey(ItemCatalog.GetItemDef(itemIndex).name))
                {
                    CRCore3.itemCountOverrides.TryGetValue(ItemCatalog.GetItemDef(itemIndex).name, out dictOut);
                    inventory.GiveItem(itemIndex, dictOut);
                }
                else
                {
                    inventory.GiveItem(itemIndex, count);
                }

                if (NetworkServer.active)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#E6B3FF>[WARNING]: " + Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken) + " has integrated into the rift!</color>" });
                }

                if (roundsStarted >= 5)
                {
                    if (this.rng.RangeInt(0, 100) <= 10)
                    {
                        if (this.rng.nextBool)
                        {
                            for (int i = 0; i < 25; i++)
                            {
                                pickupIndex = this.rng.NextElementUniform<PickupIndex>(Run.instance.availableBossDropList);
                                list.Remove(pickupIndex);
                                pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                                itemIndex = pickupDef.itemIndex;
                                bool badItem = false;
                                foreach (string itemName in CRCore3.AIBlacklistBoss)
                                {
                                    if (ItemCatalog.GetItemDef(itemIndex).nameToken.ToLower().Contains(itemName.ToLower()))
                                    {
                                        badItem = true;
                                        break;
                                    }
                                }
                                if (!badItem) { break; }
                                Debug.Log("[CRCore]: Boss item was blacklisted! Trying again.");
                            }
                            inventory.GiveItem(itemIndex, 1);
                            if (NetworkServer.active)
                            {
                                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#E6B3FF>[WARNING]: Rare boss item " + Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken) + " has integrated into the rift!</color>" });
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 25; i++)
                            {
                                pickupIndex = this.rng.NextElementUniform<PickupIndex>(Run.instance.availableBossDropList);
                                list.Remove(pickupIndex);
                                pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                                itemIndex = pickupDef.itemIndex;
                                bool badItem = false;
                                foreach (string itemName in CRCore3.AIBlacklistLunar)
                                {
                                    if (ItemCatalog.GetItemDef(itemIndex).nameToken.ToLower().Contains(itemName.ToLower()))
                                    {
                                        badItem = true;
                                        break;
                                    }
                                }
                                if (!badItem) { break; }
                                Debug.Log("[CRCore]: Lunar item was blacklisted! Trying again.");
                            }
                            inventory.GiveItem(itemIndex, 1);
                            if (NetworkServer.active)
                            {
                                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#E6B3FF>[WARNING]: Rare Lunar item " + Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken) + " has integrated into the rift!</color>" });
                            }
                        }
                    }
                }

                foreach (GameObject rift in voidRifts)
                {
                    if (rift.GetComponent<EntityStateMachine>().state is RiftCompleteState)
                    {
                        rift.GetComponent<PurchaseInteraction>().available = false;
                    }
                    else
                    {
                        rift.GetComponent<PurchaseInteraction>().available = roundsCleared >= roundsStarted;
                    }
                }
            }
        }
        public void EndRound()
        {
            roundsCleared++;
            foreach (GameObject rift in voidRifts)
            {
                if (rift.GetComponent<EntityStateMachine>().state is RiftCompleteState)
                {
                    rift.GetComponent<PurchaseInteraction>().available = true;
                }
                else
                {
                    rift.GetComponent<PurchaseInteraction>().available = roundsCleared >= roundsStarted;
                }
            }
            if (roundsCleared >= roundsCount)
            {
                ObjectivePanelController.collectObjectiveSources -= OnCollectObjectives;
                target.locked = false;
            }
            foreach (CombatDirector director in directors)
            {
                director.creditMultiplier /= 2;
                director.monsterCredit = 0;
            }

        }
        public void OnEnable()
        {
            ObjectivePanelController.collectObjectiveSources += OnCollectObjectives;
        }
        public void OnDisable()
        {
            ObjectivePanelController.collectObjectiveSources -= OnCollectObjectives;
        }
        public void OnCollectObjectives(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
        {
            objectiveSourcesList.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
            {
                master = master,
                objectiveType = typeof(CRObjectiveTracker),
                source = this
            });

        }
        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write(this.roundsCleared);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(this.roundsCleared);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(base.syncVarDirtyBits);
            }
            return flag;
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.roundsCleared = reader.ReadInt32();
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                this.roundsCleared = reader.ReadInt32();
            }
        }
    }
    public class CRObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        private int numChargedRifts = -1;
        public override string GenerateString()
        {
            CRMissionController missionController = (CRMissionController)this.sourceDescriptor.source;
            return string.Format(Language.GetString("OBJECTIVE_RIFT_TOTAL_TOKEN"), missionController.roundsCleared, CRMissionController.roundsCount);
        }
        public override bool IsDirty()
        {
            return ((CRMissionController)this.sourceDescriptor.source).roundsCleared != this.numChargedRifts;
        }
    }
    public class RiftPurchaseInteraction : PurchaseEvent
    {

    }
    public class CustomTempVFXManager : MonoBehaviour
    {
        public CharacterBody characterBody;

        public TemporaryVisualEffect voidSickEffect;
        public TemporaryVisualEffect voidSafeEffect;

        public void Update()
        {
            UpdateSingleTemporaryVisualEffect(ref voidSickEffect, PrefabManager.voidSickEffect, characterBody.radius, !characterBody.HasBuff(CRContentPack.protectionBuffDef) && characterBody.HasBuff(CRContentPack.voidDebuffDef), characterBody);
            UpdateSingleTemporaryVisualEffect(ref voidSafeEffect, PrefabManager.voidSafeEffect, characterBody.radius, characterBody.HasBuff(CRContentPack.protectionBuffDef), characterBody);

        }
        private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject resource, float effectRadius, bool active, CharacterBody characterBody, string childLocatorOverride = "")
        {
            bool flag = tempEffect != null;
            if (flag != active)
            {
                if (active)
                {
                    if (!flag)
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(resource, characterBody.corePosition, Quaternion.identity);
                        gameObject.SetActive(true);
                        tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
                        tempEffect.parentTransform = characterBody.coreTransform;
                        tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
                        tempEffect.healthComponent = characterBody.healthComponent;
                        tempEffect.radius = effectRadius;
                        LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
                        if (component)
                        {
                            component.targetCharacter = characterBody.gameObject;
                        }
                        if (!string.IsNullOrEmpty(childLocatorOverride))
                        {
                            ModelLocator modelLocator = characterBody.modelLocator;
                            ChildLocator childLocator;
                            if (modelLocator == null)
                            {
                                childLocator = null;
                            }
                            else
                            {
                                Transform modelTransform = modelLocator.modelTransform;
                                childLocator = ((modelTransform != null) ? modelTransform.GetComponent<ChildLocator>() : null);
                            }
                            ChildLocator childLocator2 = childLocator;
                            if (childLocator2)
                            {
                                Transform transform = childLocator2.FindChild(childLocatorOverride);
                                if (transform)
                                {
                                    tempEffect.parentTransform = transform;
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (tempEffect)
                {
                    tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
                }
            }
        }
    }

    public class VoidRiftTracker : MonoBehaviour
    {
        public BuffWard voidWard;
        public BuffWard protectionWard;
        public ChildLocator childLocator;
        public void Awake()
        {
            childLocator = base.gameObject.GetComponent<ChildLocator>();
        }
    }
    public class CRMissionNetworkMessage : INetMessage, ISerializableObject
    {
        public int riftsCompleted;
        public void Deserialize(NetworkReader reader)
        {
            riftsCompleted = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
            {
                CRMissionController.instance.roundsCleared = riftsCompleted;
            }
        }
        public void Serialize(NetworkWriter writer)
        {
            writer.Write((Int32)riftsCompleted);
        }
    }
}
