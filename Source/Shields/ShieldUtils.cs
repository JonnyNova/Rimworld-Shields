using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FrontierDevelopments.Shields
{
    public static class ShieldUtils
    {
        public static IEnumerable<IShield> AllShields(Pawn pawn)
        {
            foreach (var shield in InventoryShields(pawn))
            {
                yield return shield;
            }

            foreach (var shield in EquipmentShields(pawn))
            {
                yield return shield;
            }

            foreach (var shield in HediffShields(pawn))
            {
                yield return shield;
            }
        }

        public static IEnumerable<IShield> InventoryShields(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null) yield break;
            foreach (var thing in pawn.inventory.innerContainer)
            {
                switch (thing)
                {
                    case MinifiedShield shield:
                        yield return shield;
                        break;
                    case MinifiedThing minified:
                        switch (minified.InnerThing)
                        {
                            case ThingWithComps thingWithComps:
                                foreach (var comp in thingWithComps.AllComps)
                                {
                                    switch (comp)
                                    {
                                        case IShield shield:
                                            yield return shield;
                                            break;
                                    }
                                }
                                break;
                        }
                        break;
                }
            }
        }
        
        public static IEnumerable<IShield> EquipmentShields(Pawn pawn)
        {
            if (pawn.equipment == null) yield break;
            foreach (var equipment in pawn.equipment.AllEquipmentListForReading)
            {
                foreach (var comp in equipment.AllComps)
                {
                    switch (comp)
                    {
                        case IShield shield: 
                            yield return shield;
                            break;
                    }
                }
            }
        }

        public static IEnumerable<IShield> HediffShields(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null) yield break;
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                switch (hediff)
                {
                    case IShield shield:
                        yield return shield;
                        break;
                }
            }
        }
    }
}