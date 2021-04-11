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
            this.outer.SetNextStateToMain();
            base.gameObject.GetComponent<PurchaseInteraction>().available = false;
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

            foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Player))
            {
                if (teamComponent.body && teamComponent.body.healthComponent)
                {
                    teamComponent.body.healthComponent.HealFraction(1.0f, new ProcChainMask());
                    teamComponent.body.healthComponent.RechargeShieldFull();
                }
            }

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
        }
        public override void OnExit()
        {
            base.OnExit();
            base.GetComponent<HoldoutZoneController>().onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(this.OnFinish));
            base.GetComponent<HoldoutZoneController>().enabled = false;
        }
        public void OnFinish(HoldoutZoneController holdoutZone)
        {
            base.missionController.EndRound();
            this.outer.SetNextState(new RiftCompleteState());
        }
    }

    class RiftCompleteState : RiftBaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            var particleSystem = childLocator.FindChild("ParticleSystemBase").gameObject.GetComponent<ParticleSystem>();
            particleSystem.Stop( true, ParticleSystemStopBehavior.StopEmitting );
            var particleSystem2 = childLocator.FindChild("ParticleSystemFinish").gameObject;
            particleSystem2.SetActive(true);
            Util.PlaySound("Stop_ui_obj_nullWard_charge_loop", base.gameObject);
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
