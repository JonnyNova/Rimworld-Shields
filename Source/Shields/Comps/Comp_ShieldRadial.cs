﻿using System.Collections.Generic;
using FrontierDevelopments.General;
using FrontierDevelopments.Shields.CompProperties;
using FrontierDevelopments.Shields.Windows;
using RimWorld;
using UnityEngine;
using Verse;

namespace FrontierDevelopments.Shields.Comps
{
    public class Comp_ShieldRadial : ThingComp
    {
        private int _fieldRadius;
        private int _cellCount;
        private bool _renderField = true;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            _cellCount = GenRadial.NumCellsInRadius(_fieldRadius);
        }

        public int ProtectedCellCount => _cellCount;

        public CompProperties_ShieldRadial Props => 
            (CompProperties_ShieldRadial)props;

        public int Radius
        {
            get => _fieldRadius;
            set
            {
                if (value < 0)
                {
                    _fieldRadius = 0;
                    return;
                }
                if (value > Props.maxRadius)
                {
                    _fieldRadius = Props.maxRadius;
                    return;
                }
                _fieldRadius = value;
                _cellCount = GenRadial.NumCellsInRadius(_fieldRadius);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var current in base.CompGetGizmosExtra())
                yield return current;
            
            yield return new Command_Toggle
            {
                icon = Resources.UiBlank,
                defaultDesc = "fd.shield.render_field.description".Translate(),
                defaultLabel = "fd.shield.render_field.label".Translate(),
                isActive = () => _renderField,
                toggleAction = () => _renderField = !_renderField
            };

            if (parent.Faction == Faction.OfPlayer)
            {
                if (Props.minRadius != Props.maxRadius)
                {
                    yield return new Command_Action
                    {
                        icon = Resources.UiSetRadius,
                        defaultDesc = "radius.description".Translate(),
                        defaultLabel = "radius.label".Translate(),
                        activateSound = SoundDef.Named("Click"),
                        action = () => Find.WindowStack.Add(new Popup_IntSlider("radius.label".Translate(), Props.minRadius, Props.maxRadius, () => Radius, size =>  Radius = size))
                    };
                }
            }
        }

        public bool Collision(Vector3 vector)
        {
            return Vector3.Distance(Common.ToVector3(parent.Position), vector) < _fieldRadius + 0.5f;
        }

        public Vector3? Collision(Ray ray, float limit)
        {
            var point = ray.GetPoint(limit);
            if (Collision(point))
            {
                return ray.origin;
            }
            return null;
        }

        public void Draw()
        {
            if (!_renderField) return;
            var position = Common.ToVector3(parent.Position);
            position.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            var scalingFactor = (float)(_fieldRadius * 2.2);
            var scaling = new Vector3(scalingFactor, 1f, scalingFactor);
            var matrix = new Matrix4x4();
            matrix.SetTRS(position, Quaternion.AngleAxis(0, Vector3.up), scaling);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Resources.ShieldMat, 0);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(parent.Position, _fieldRadius);
        }
        
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref _fieldRadius, "radius", Props.maxRadius);
            Scribe_Values.Look(ref _renderField, "renderField", true);
        }
    }
}