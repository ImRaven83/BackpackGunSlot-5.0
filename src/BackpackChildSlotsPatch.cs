using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace BackpackGunSlot
{
    internal static class BackpackWeaponSlot
    {
        internal static bool Matches(Slot slot) => slot != null &&
            slot.Name == Plugin.SlotName.Value?.Trim();
    }

    [HarmonyPatch(typeof(SearchableSlotView), nameof(SearchableSlotView.CreateSlots))]
    internal static class BackpackSpecialSlotsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SearchableSlotView __instance, Item __0)
        {
            if (__instance._specSlotHeader != null)
                __instance._specSlotHeader.gameObject.SetActive(true);
            var pack = __0?.TryCast<CompoundItem>();
            if (pack?.Slots == null || !pack.Slots.Any(BackpackWeaponSlot.Matches))
                return true;
            // The equipped backpack binding draws this slot with the ON BACK prefab.
            // Do not also create vanilla's small SPEC slot for the same address.
            if (pack.Slots.All(BackpackWeaponSlot.Matches))
            {
                if (__instance._specSlotsPanel != null)
                    __instance._specSlotsPanel.gameObject.SetActive(false);
                if (__instance._specSlotHeader != null)
                    __instance._specSlotHeader.gameObject.SetActive(false);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(SearchableSlotView __instance, Item __0)
        {
            var pack = __0?.TryCast<CompoundItem>();
            if (pack?.Slots == null || !pack.Slots.Any(BackpackWeaponSlot.Matches) ||
                __instance._specSlotsPanel == null) return;
            foreach (var view in __instance._specSlotsPanel.GetComponentsInChildren<SlotView>(true))
            {
                if (!BackpackWeaponSlot.Matches(view.Slot) ||
                    view.GetComponent<BackpackSlotMarker>() != null) continue;
                // Keep the object for vanilla's registered disposal callback.
                view.Close();
                view.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(ContainedGridsView), nameof(ContainedGridsView.Show), typeof(CompoundItem),
        typeof(ItemContext), typeof(Il2CppReferenceArray<GridView>), typeof(Il2CppReferenceArray<SlotView>),
        typeof(ItemController), typeof(FilterPanel), typeof(ItemUiContext), typeof(bool))]
    internal static class LayoutPresetSlotsPatch
    {
        // The Terraframe layout ships a preset slot view that points at nothing. Vanilla packs
        // have no slots so it never gets used, but ours would be paired with it and throw.
        [HarmonyPrefix]
        private static void Prefix(ref Il2CppReferenceArray<SlotView> __3)
        {
            if (__3 != null && __3.Any(v => v == null))
                __3 = new Il2CppReferenceArray<SlotView>(__3.Where(v => v != null).ToArray());
        }
    }

    [HarmonyPatch(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.CreateModSlots))]
    internal static class BackpackInspectionSlotsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ItemSpecificationPanel __instance, CompoundItem __0)
        {
            if (__0?.Slots == null || !__0.Slots.Any(BackpackWeaponSlot.Matches) ||
                !Il2CppUtil.Each(__0.AllSlots).All(BackpackWeaponSlot.Matches)) return true;
            // This is storage, not a weapon attachment. The attachment renderer has
            // no icon for the custom slot name and otherwise shows a solid white tile.
            __instance._modsPanel.gameObject.SetActive(false);
            return false;
        }
    }
}
