﻿using System;
using System.Collections.Generic;
using FrontierDevelopments.General;
using FrontierDevelopments.General.UI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FrontierDevelopments.Shields
{
    public interface IShieldField
    {
        Map Map { get; }
        bool PresentOnMap(Map map);
        bool IsActive();
        int ProtectedCellCount { get; }
        float CellProtectionFactor { get; }
        bool Collision(Vector3 point);
        Vector3? Collision(Ray ray, float limit);
        Vector3? Collision(Vector3 start, Vector3 end);
        float Block(float damage, Vector3 position);
        float Block(ShieldDamages damages, Vector3 position);
        void FieldPreDraw();
        void FieldDraw(CellRect cameraRect);
        void FieldPostDraw();
        Faction Faction { get; }
        IEnumerable<IShield> Emitters { get; }
    }

    public interface IShield : IShieldWithStatus, IShieldUserInterface, ILabeled, IAttackTarget
    {
        bool PresentOnMap(Map map);
        IShieldParent Parent { get; }
        void SetParent(IShieldParent shieldParent);
        bool IsActive();
        IEnumerable<IShieldField> Fields { get; }
        float DeploymentSize { get; }
    }

    public interface IShieldParent : IShieldWithStatus, IShieldUserInterface, IAttackTarget
    {
        bool ParentActive { get; }
        float SinkDamage(float damage);
    }

    public interface IShieldWithStatus
    {
        IEnumerable<IShieldStatus> Status { get; }
        String TextStats { get; }
    }
    
    public interface IShieldSettable
    {
        IEnumerable<ShieldSetting> ShieldSettings { get; set; }
        bool HasWantSettings { get; }
        void ClearWantSettings();
        void NotifyWantSettings();
    }

    public interface IShieldUserInterface : IShieldSettable
    {
        IEnumerable<Gizmo> ShieldGizmos { get; }
        IEnumerable<UiComponent> UiComponents { get; }
    }
}
