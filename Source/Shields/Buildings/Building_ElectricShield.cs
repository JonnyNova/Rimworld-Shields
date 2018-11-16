﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrontierDevelopments.General;
using FrontierDevelopments.General.Comps;
using FrontierDevelopments.Shields.Comps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FrontierDevelopments.Shields.Buildings
{
    public class Building_ElectricShield : Building_ShieldBase
    {
        public enum ShieldStatus
        {
            NoPowerNet,
            Unpowered,
            InternalBatteryDischarged,
            ThermalShutdown,
            BatteryPowerTooLow,
            Online
        }
        
        private CompPowerTrader _powerTrader;
        private CompFlickable _flickable;
        private Comp_ShieldRadial _shield;
        private Comp_HeatSink _heatSink;

        private bool _activeLastTick;

        private bool _thermalShutoff = true;
        private float _additionalPowerDraw;

        private float BasePowerConsumption => -_shield.ProtectedCellCount * Mod.Settings.PowerPerTile;

        public bool FlickedOn => _flickable == null || _flickable != null && _flickable.SwitchIsOn; 

        public ShieldStatus Status
        {
            get
            {
                if (!HasPowerNet()) return ShieldStatus.NoPowerNet;
                if (!_powerTrader.PowerOn) return ShieldStatus.Unpowered;
                if (_thermalShutoff && _heatSink.OverMinorThreshold) return ShieldStatus.ThermalShutdown;
                if (!IsActive()) return ShieldStatus.BatteryPowerTooLow;
                return ShieldStatus.Online;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("FD_Shields"), OpportunityType.Critical);
            _powerTrader = GetComp<CompPowerTrader>();
            _flickable = GetComp<CompFlickable>();
            _shield = GetComp<Comp_ShieldRadial>();
            _heatSink = GetComp<Comp_HeatSink>();
            _heatSink.CanBreakdown = IsActive;
            _heatSink.MinorBreakdown = () => BreakdownMessage("fd.shields.incident.minor.title".Translate(), "fd.shields.incident.minor.body".Translate(), DoMinorBreakdown());
            _heatSink.MajorBreakdown = () => BreakdownMessage("fd.shields.incident.major.title".Translate(), "fd.shields.incident.major.body".Translate(), DoMajorBreakdown());
            _heatSink.CriticalBreakdown = () => BreakdownMessage("fd.shields.incident.critical.title".Translate(), "fd.shields.incident.critical.body".Translate(), DoCriticalBreakdown());
            _activeLastTick = IsActive();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Tick()
        {
            var active = IsActive();
            if (_powerTrader?.PowerNet != null)
            {
                var availThisTick = _powerTrader.PowerNet.CurrentEnergyGainRate() +
                                    _powerTrader.PowerNet.CurrentStoredEnergy() * 60000;
                var powerWanted = BasePowerConsumption - _additionalPowerDraw;
                if (availThisTick + powerWanted < 0)
                {
                    powerWanted = -availThisTick;
                }
                _powerTrader.PowerOutput = powerWanted;
            }
            base.Tick();
            if(_activeLastTick && !active && FlickedOn)
                Messages.Message("fd.shields.incident.offline.body".Translate(), new GlobalTargetInfo(Position, Map), MessageTypeDefOf.NegativeEvent);
            _additionalPowerDraw = 0;
            _activeLastTick = active;
        }

        private bool HasPowerNet()
        {
            return _powerTrader?.PowerNet != null;
        }
        
        public override bool IsActive()
        {
            return _powerTrader.PowerOn 
                   && (_powerTrader.PowerNet?.CurrentStoredEnergy()).GetValueOrDefault(0f) > Mod.Settings.MinimumOnlinePower
                   && (!_thermalShutoff || (_thermalShutoff && !_heatSink.OverMinorThreshold));
        }

        private void RenderImpactEffect(Vector2 position)
        {
            MoteMaker.ThrowLightningGlow(Common.ToVector3(position), Map, 0.5f);
        }

        private void PlayBulletImpactSound(Vector2 position)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(Common.ToIntVec3(position), Map));
        }

        private float DrawPowerOneTick(float amount)
        {
            if (_powerTrader.PowerNet == null) return 0f;
            
            // can this be feed by instantaneous draw? (who are we kidding, no way)
            var gainPowerCovers = _powerTrader.PowerNet.CurrentEnergyGainRate() + BasePowerConsumption + amount;
            if (gainPowerCovers >= 0) return amount;
            var gainAndBatteriesCover = gainPowerCovers + _powerTrader.PowerNet.CurrentStoredEnergy() * 60000;
            
            // will batteries cover the difference?
            if (gainAndBatteriesCover >= 0) return amount;

            // uh-oh, energy shortfall
            return amount - gainAndBatteriesCover;
        }

        public override bool Block(long damage, Vector3 position)
        {
            if (!IsActive()) return false;
            // convert watts per day to watts per tick
            var charge = (damage * 60000 * Mod.Settings.PowerPerDamage);
            if (Mod.Settings.ScaleOnHeat) charge = charge * Mathf.Pow(1.01f, _heatSink.Temp);
            var drawn = -DrawPowerOneTick(-charge);
            _heatSink.PushHeat(drawn / 60000 * Mod.Settings.HeatPerPower);
            _additionalPowerDraw = charge;
            if (drawn < charge) return false;
            RenderImpactEffect(Common.ToVector2(position));
            PlayBulletImpactSound(Common.ToVector2(position));
            return true;    
        }

        public override bool Collision(Vector3 vector)
        {
            return _shield.Collision(vector);
        }

        public override Vector3? Collision(Vector3 start, Vector3 end)
        {
            return _shield.Collision(start, end);
        }

        public override Vector3? Collision(Ray ray, float limit)
        {
            return _shield.Collision(ray, limit);
        }

        public override string GetInspectString()
        {
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("FD_CoilTemperature"), OpportunityType.Important);
            var stringBuilder = new StringBuilder();
            switch (Status)
            {
                case ShieldStatus.NoPowerNet: 
                    return "shield.status.offline".Translate() + " - " + "shield.status.no_power".Translate();
                case ShieldStatus.Unpowered:
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.no_power".Translate());
                    break;
                case ShieldStatus.BatteryPowerTooLow: 
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.battery_too_low".Translate() + " " + Mod.Settings.MinimumOnlinePower  + " Wd");
                    break;
                case ShieldStatus.InternalBatteryDischarged:
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.internal_battery_discharged".Translate());
                    break;
                case ShieldStatus.ThermalShutdown:
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.thermal_safety".Translate());
                    break;
                case ShieldStatus.Online:
                    stringBuilder.AppendLine("shield.status.online".Translate());
                    break;
            }
            stringBuilder.Append(base.GetInspectString());
            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            if (Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    icon = Resources.UiThermalShutoff,
                    defaultDesc = "thermal_shutoff.description".Translate(),
                    defaultLabel = "thermal_shutoff.label".Translate(),
                    isActive = () => _thermalShutoff,
                    toggleAction = () => _thermalShutoff = !_thermalShutoff
                };
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref _thermalShutoff, "thermalShutoff", true);
            base.ExposeData();
        }

        private void BreakdownMessage(string title, string body, float drained)
        {
            if (Faction != Faction.OfPlayer) return;
            Find.LetterStack.ReceiveLetter(
                title,
                body.Replace("{0}", ((int)drained).ToString()), 
                LetterDefOf.NegativeEvent, 
                new TargetInfo(Position, Map));
        }

        private float DoMinorBreakdown()
        {
            var random = new System.Random();
            return GetComp<CompPowerTrader>().PowerNet.batteryComps
                .Aggregate(0f, (total, battery) =>
                {
                    var drain = battery.StoredEnergy * (float) random.NextDouble();
                    battery.DrawPower(drain);
                    return total + drain;
                });
        }

        private float DoMajorBreakdown()
        {
            GetComp<CompBreakdownable>().DoBreakdown();
            if (Faction != Faction.OfPlayer) return DoMinorBreakdown();
            // manually remove the default letter...
            Find.LetterStack.RemoveLetter(Find.LetterStack.LettersListForReading.First(letter => 
                letter.lookTargets.targets.Any(t => t.Thing == this)));
            return DoMinorBreakdown();
        }

        private float DoCriticalBreakdown()
        {
            GenExplosion.DoExplosion(
                Position,
                Map,
                3.5f,
                DamageDefOf.Flame,
                this);
            return DoMajorBreakdown();
        }

        public override void Draw(CellRect cameraRect)
        {
            if(IsActive()) _shield.Draw(cameraRect);
        }
    }
}
