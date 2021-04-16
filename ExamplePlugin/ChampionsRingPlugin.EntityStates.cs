using ChampionsRingPlugin.Components;
using ChampionsRingPlugin.Content;
using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using EntityStates.NullifierMonster;
using R2API.Networking.Interfaces;
using ChampionsRingPlugin.Core;

namespace ChampionsRingPlugin.EntityStates
{
    class RiftBaseState : EntityState
    {
        public CRMissionController missionController;
        public ChildLocator childLocator;

        public Transform rangeIndicator;
        public Transform buffIndicator;

        public BuffWard protectionBuffWard;
        public BuffWard voidBuffWard;

        public override void OnEnter()
        {
            VoidRiftTracker tracker = base.gameObject.GetComponent<VoidRiftTracker>();
            missionController = CRMissionController.instance;
            childLocator = tracker.childLocator;

            rangeIndicator = childLocator.FindChild("RangeIndicator");
            buffIndicator = childLocator.FindChild("BuffIndicator");

            voidBuffWard = tracker.voidWard;
            protectionBuffWard = tracker.protectionWard;
        }
    }
    class RiftOffState : RiftBaseState
    {
        public GameObject teleportTarget;
        public override void OnEnter()
        {
            base.OnEnter();
            protectionBuffWard.radius = 0;
            voidBuffWard.radius = 125;
            base.gameObject.GetComponent<PurchaseInteraction>().onPurchase.AddListener(delegate (Interactor interactor)
            {
                OnInteract(interactor);
            });
        }
        public override void OnExit()
        {
            base.OnExit();
            base.gameObject.GetComponent<PurchaseInteraction>().enabled = false;
        }

        public void OnInteract(Interactor interactor)
        {
            if (teleportTarget && interactor.gameObject.GetComponent<CharacterBody>())
            {
                EffectManager.SpawnEffect(GenericCharacterDeath.voidDeathEffect, new EffectData
                {
                    origin = interactor.gameObject.transform.position,
                    scale = 5f
                }, true);
                if (Util.HasEffectiveAuthority(interactor.gameObject))
                {
                    TeleportHelper.TeleportBody(interactor.gameObject.GetComponent<CharacterBody>(), teleportTarget.transform.position);
                }
                else
                {
                    if (NetworkServer.active)
                    {
                        NetMessageExtensions.Send(new CRTeleportNetworkMessage { target = interactor.gameObject, position = teleportTarget.transform.position, fromServer = true }, R2API.Networking.NetworkDestination.Clients);
                    }
                    else
                    {
                        NetMessageExtensions.Send(new CRTeleportNetworkMessage { target = interactor.gameObject, position = teleportTarget.transform.position, fromServer = false }, R2API.Networking.NetworkDestination.Server);
                    }
                }
                EffectManager.SpawnEffect(DeathState.deathExplosionEffect, new EffectData
                {
                    origin = teleportTarget.transform.position + new Vector3(0, 3, 0),
                    scale = 5f
                }, true);
            }
            else
            {
                this.outer.SetNextStateToMain();
                base.gameObject.GetComponent<PurchaseInteraction>().available = false;
            }
        }
    }

    class RiftOnState : RiftBaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            base.missionController.BeginRound(base.outer.gameObject);
            base.GetComponent<HoldoutZoneController>().onCharged.AddListener(new UnityAction<HoldoutZoneController>(this.OnFinish));
            base.GetComponent<HoldoutZoneController>().enabled = true;

            var particleSystem2 = childLocator.FindChild("ParticleSystemStart").gameObject;
            particleSystem2.SetActive(true);
            childLocator.FindChild("Beacon").gameObject.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Util.PlaySound("Play_ui_obj_nullWard_activate", base.gameObject);
            Util.PlaySound("Play_ui_obj_nullWard_charge_loop", base.gameObject);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            protectionBuffWard.radius = base.GetComponent<HoldoutZoneController>().currentRadius;
            voidBuffWard.radius = base.GetComponent<HoldoutZoneController>().currentRadius + 75;
            if (!NetworkServer.active && base.GetComponent<HoldoutZoneController>().charge >= 0.99)
            {
                this.outer.SetNextState(new RiftCompleteState());
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            base.GetComponent<HoldoutZoneController>().onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(this.OnFinish));
        }
        public void OnFinish(HoldoutZoneController holdoutZone)
        {
            this.outer.SetNextState(new RiftCompleteState());
        }
    }

    class RiftCompleteState : RiftBaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            base.missionController.EndRound();
            base.outer.gameObject.GetComponent<PurchaseInteraction>().available = false;
            base.GetComponent<HoldoutZoneController>().enabled = false;
            var particleSystem = childLocator.FindChild("ParticleSystemBase").gameObject.GetComponent<ParticleSystem>();
            particleSystem.Stop( true, ParticleSystemStopBehavior.StopEmitting );
            var particleSystem2 = childLocator.FindChild("ParticleSystemFinish").gameObject;
            particleSystem2.SetActive(true);
            Util.PlaySound("Stop_ui_obj_nullWard_charge_loop", base.gameObject);

            if (CRCore3.dropRewards.Value && NetworkServer.active)
            {
                List<PickupIndex> list = Run.instance.availableTier1DropList;
                if (CRMissionController.instance.roundsStarted > 2)
                {
                    list = Run.instance.availableTier2DropList;
                }
                PickupDropletController.CreatePickupDroplet(CRMissionController.instance.rng.NextElementUniform<PickupIndex>(list), this.gameObject.transform.position, Vector3.up * 30);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            protectionBuffWard.radius /= 1.0025f;
            voidBuffWard.radius /= 1.01f;
            childLocator.FindChild("PhysicalOrb").localScale /= 1.05f;
            //childLocator.FindChild("PointLight").GetComponent<Light>().intensity /= 1.1f;
        }
    }
}
