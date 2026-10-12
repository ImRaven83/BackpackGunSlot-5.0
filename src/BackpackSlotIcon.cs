using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using UnityEngine;

namespace BackpackGunSlot
{
    internal enum GunClass { Pistol, Smg, Shotgun, Rifle }

    internal static class BackpackSlotIcon
    {
        private const string Pistols = "5447b5cf4bdc2d65278b4567";
        private const string Smgs = "5447b5e04bdc2d62278b4567";
        private const string Shotguns = "5447b6094bdc2dc3278b4567";

        // The largest gun the slot takes. Long guns or a custom filter count as rifles.
        internal static GunClass Of(Slot slot)
        {
            var excluded = new HashSet<string>(slot.Filters.Where(f => f.ExcludedFilter != null)
                .SelectMany(f => f.ExcludedFilter).Select(id => id.ToString()));
            var allowed = slot.Filters.SelectMany(f => f.Filter).Select(id => id.ToString())
                .Where(id => !excluded.Contains(id)).ToList();
            if (allowed.Count == 0 || allowed.Any(id => id != Pistols && id != Smgs && id != Shotguns))
                return GunClass.Rifle;
            return allowed.Contains(Shotguns) ? GunClass.Shotgun
                : allowed.Contains(Smgs) ? GunClass.Smg : GunClass.Pistol;
        }

        // Null keeps the vanilla rifle ghost.
        internal static Sprite For(GunClass gun) => gun switch
        {
            GunClass.Pistol => SlotIcons.Pistol,
            GunClass.Smg => SlotIcons.Smg,
            GunClass.Shotgun => SlotIcons.Shotgun,
            _ => null
        };
    }
}
