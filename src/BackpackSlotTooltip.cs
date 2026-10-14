using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace BackpackGunSlot
{
    // Hovering the empty slot lists every weapon class it takes. The ghost only shows the biggest.
    // Hover is polled rather than received through the pointer interfaces: Unity cannot
    // dispatch those to an injected IL2CPP type and throws on every enter and exit.
    internal sealed class BackpackSlotTooltip : MonoBehaviour
    {
        private static readonly (string Id, string Name)[] Classes =
        {
            ("5447b5cf4bdc2d65278b4567", "pistols"),
            ("5447b5e04bdc2d62278b4567", "SMGs"),
            ("5447b6094bdc2dc3278b4567", "shotguns"),
            ("5447b5fc4bdc2d87278b4567", "carbines"),
            ("5447b5f14bdc2d61278b4567", "assault rifles"),
            ("5447b6194bdc2d67278b4567", "marksman rifles"),
            ("5447b6254bdc2dc3278b4568", "sniper rifles"),
            ("5447bed64bdc2d97278b4568", "machine guns"),
            ("5447bedf4bdc2d87278b4568", "grenade launchers"),
            ("5447bee84bdc2dc3278b4569", "special weapons"),
        };

        private Slot _slot;
        private string _text;
        private bool _shown;
        private bool _hovered;
        private Canvas _canvas;

        public BackpackSlotTooltip(IntPtr pointer) : base(pointer) { }

        [HideFromIl2Cpp]
        internal void Bind(Slot slot)
        {
            _slot = slot;
            var excluded = new HashSet<string>(slot.Filters.Where(f => f.ExcludedFilter != null)
                .SelectMany(f => f.ExcludedFilter).Select(id => id.ToString()));
            var allowed = new HashSet<string>(slot.Filters.SelectMany(f => f.Filter)
                .Select(id => id.ToString()).Where(id => !excluded.Contains(id)));
            var names = Classes.Where(c => allowed.Contains(c.Id)).Select(c => c.Name).ToList();
            _text = names.Count == 0 ? null : "Fits: " + string.Join(", ", names);
        }

        private void Update()
        {
            bool hovered = IsPointerOver();
            if (hovered == _hovered) return;
            _hovered = hovered;
            if (hovered) Show();
            else Hide();
        }

        private bool IsPointerOver()
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_canvas == null) return false;
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(
                transform.Cast<RectTransform>(), Input.mousePosition, camera);
        }

        private void Show()
        {
            // A filled slot shows the item's own tooltip instead.
            if (_text == null || _slot?.ContainedItem != null) return;
            var tooltip = ItemUiContext.Instance?.Tooltip;
            if (tooltip == null) return;
            tooltip.Show(_text, new Il2CppSystem.Nullable<Vector2>(), .5f, new Il2CppSystem.Nullable<float>());
            _shown = true;
        }

        private void OnDisable()
        {
            _hovered = false;
            Hide();
        }

        private void Hide()
        {
            if (!_shown) return;
            _shown = false;
            var tooltip = ItemUiContext.Instance?.Tooltip;
            if (tooltip != null && tooltip.isActiveAndEnabled) tooltip.Close();
        }
    }
}
