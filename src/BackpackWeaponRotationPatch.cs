using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;

namespace BackpackGunSlot
{
    // Use the game's icon rotation rather than rotating the inventory container.
    // This runs for existing items, new drops and failed-move recreation alike.
    [HarmonyPatch(typeof(SlotView), nameof(SlotView.SetupItemView))]
    internal static class BackpackWeaponItemRotationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SlotView __instance)
        {
            if (__instance.GetComponent<BackpackSlotMarker>()?.Vertical != true) return;
            var itemView = __instance.ItemView;
            if (itemView == null) return;
            itemView.ItemRotation = ItemRotation.Vertical;
            __instance.ScaleItem(__instance._slotPlace.rect.size);
        }
    }

    [HarmonyPatch(typeof(SlotView), nameof(SlotView.ScaleItem))]
    internal static class BackpackWeaponItemScalePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SlotView __instance, ref Vector2 __0)
        {
            if (__instance.GetComponent<BackpackSlotMarker>()?.Vertical != true) return;
            // ItemView.UpdateScale fits the unrotated image dimensions. Transpose
            // the available bounds for a vertical icon, including the game's
            // deferred end-of-frame scaling pass.
            __0 = new Vector2(__0.y, __0.x);
        }
    }
}
