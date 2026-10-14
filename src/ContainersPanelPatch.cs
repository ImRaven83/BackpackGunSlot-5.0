using System;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Insurance;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BackpackGunSlot
{
    [HarmonyPatch(typeof(ContainersPanel), nameof(ContainersPanel.Show))]
    internal static class ContainersPanelShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ContainersPanel __instance, ItemContext __0,
            InventoryEquipment __1, InventoryController __2, EFT.SkillManager __3,
            InsuranceCompany __4)
        {
            try
            {
                var binding = __instance.GetComponent<BackpackSlotBinding>()
                    ?? __instance.gameObject.AddComponent<BackpackSlotBinding>();
                binding.Initialize(__instance, __0, __1, __2, __3, __4);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Backpack slot initialization failed: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(ContainersPanel), nameof(ContainersPanel.Close))]
    internal static class ContainersPanelClosePatch
    {
        // Unbind before vanilla disposes the equipment views and their contexts.
        [HarmonyPrefix]
        private static void Prefix(ContainersPanel __instance)
        {
            __instance.GetComponent<BackpackSlotBinding>()?.Shutdown();
            BackpackSlotBinding.RemoveOrphans(__instance);
        }
    }

    internal sealed class BackpackSlotBinding : MonoBehaviour
    {
        private const string InjectedName = "BackpackGunSlot_ONPACK";
        private const float SlotGap = 8;
        private const float SmgLength = .7f;
        private ContainersPanel _panel;
        private ItemContext _parentContext;
        private ItemContext _backpackContext;
        private InventoryEquipment _equipment;
        private InventoryController _controller;
        private EFT.SkillManager _skills;
        private InsuranceCompany _insurance;
        private SlotView _view;
        private RectTransform _viewHost;
        private UnityEngine.UI.LayoutElement _rowLayout;
        private RectTransform _clip;
        private EquipmentTab _tab;
        private int _nextTabSearch;
        private GridView[] _grids;
        private int _nextGridSearch;
        private Component[] _rowElements;
        private static Il2CppStructArray<Vector3> _corners;
        // Full hierarchy searches are too slow to repeat every frame.
        private const int SearchInterval = 30;
        private Slot _boundSlot;
        private bool _running;
        private string _caption;

        public BackpackSlotBinding(IntPtr pointer) : base(pointer) { }

        [HideFromIl2Cpp]
        internal void Initialize(ContainersPanel panel, ItemContext context,
            InventoryEquipment equipment, InventoryController controller,
            EFT.SkillManager skills, InsuranceCompany insurance)
        {
            Shutdown();
            RemoveOrphans(panel);
            _panel = panel;
            _parentContext = context;
            _equipment = equipment;
            _controller = controller;
            _skills = skills;
            _insurance = insurance;
            _running = true;
            RefreshSafely();
        }

        private void LateUpdate()
        {
            if (_running) RefreshSafely();
        }

        private void RefreshSafely()
        {
            try { Refresh(); }
            catch (Exception e)
            {
                // Stop a failing binding instead of throwing and logging every frame.
                Shutdown();
                Plugin.Log.LogError("Backpack slot refresh failed; reopen inventory after correcting the problem: " + e);
            }
        }

        private void Refresh()
        {
            var backpackSlot = _equipment?.GetSlot(EquipmentSlot.Backpack);
            var pack = backpackSlot?.ContainedItem?.TryCast<CompoundItem>();
            var wanted = Plugin.SlotName.Value?.Trim();
            Slot slot = null;
            if (Plugin.ModEnabled.Value && !string.IsNullOrEmpty(wanted) && pack?.Slots != null)
                foreach (var candidate in pack.Slots)
                    if (candidate != null && candidate.Name == wanted) { slot = candidate; break; }
            SlotView backpackView = null;
            _panel._slotViews?.TryGetValue(EquipmentSlot.Backpack, out backpackView);
            // Respect the visibility of the inspected inventory (including restricted loot).
            if (backpackView == null || !backpackView.gameObject.activeInHierarchy ||
                !_controller.IsAllowedToSeeSlot(backpackSlot, EquipmentSlot.Backpack)) slot = null;

            if (slot == null)
            {
                ReleaseView();
                return;
            }
            if (!Il2CppUtil.Same(slot, _boundSlot) || _view == null)
            {
                ReleaseView();
                // Only use the weapon doll belonging to this panel's UI hierarchy.
                // Cloning a scene-global doll can copy another inventory's contents.
                var tab = FindEquipmentTab();
                var gun = BackpackSlotIcon.Of(slot);
                // Pistol slots are a plain copy of the vanilla holster. Everything else
                // is a tall weapon slot as wide as the backpack picture.
                bool holster = gun == GunClass.Pistol && tab?._holsterSlot != null;
                var template = holster ? tab._holsterSlot : tab?._seconaryWeaponSlot;
                if (template == null) return; // Retry once the sibling doll is initialized.
                var packIcon = backpackView._slotPlace;
                if (packIcon == null) return;
                var sourceSize = template.transform.Cast<RectTransform>().rect.size;
                Vector2 size = sourceSize;
                if (!holster)
                {
                    float width = packIcon.rect.width;
                    float length = width * Mathf.Max(sourceSize.x, sourceSize.y) /
                        Mathf.Max(1, Mathf.Min(sourceSize.x, sourceSize.y));
                    size = new Vector2(width, gun == GunClass.Smg ? length * SmgLength : length);
                }
                var hostObject = new GameObject("BackpackGunSlot_Host");
                _viewHost = hostObject.AddComponent<RectTransform>();
                hostObject.AddComponent<UnityEngine.UI.LayoutElement>();
                _viewHost.SetParent(backpackView.transform, false);
                _viewHost.SetAsLastSibling();
                _viewHost.anchorMin = _viewHost.anchorMax = _viewHost.pivot = new Vector2(0, 1);
                _viewHost.sizeDelta = size;
                _viewHost.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                // Grows the backpack row when the grids are shorter than the slot.
                _rowLayout = backpackView.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                _rowLayout.layoutPriority = 2;
                // Whatever clips the inventory (the scroll viewport) is the edge we must stay inside.
                var mask = backpackView.GetComponentInParent<UnityEngine.UI.RectMask2D>();
                _clip = mask != null ? mask.rectTransform
                    : backpackView.GetComponentInParent<UnityEngine.UI.Mask>()?.rectTransform;
                _view = Instantiate(template, _viewHost, false);
                _view.gameObject.name = InjectedName;
                _view.gameObject.AddComponent<BackpackSlotMarker>().Vertical = !holster;
                _view.gameObject.AddComponent<BackpackSlotTooltip>().Bind(slot);
                // A live doll may already have an item icon. It is a cloned visual, not
                // a bound ItemView: remove it without touching the source's ownership.
                foreach (var icon in _view.GetComponentsInChildren<ItemView>(true))
                    DestroyImmediate(icon.gameObject);
                _view.Start();
                _backpackContext = _parentContext.CreateChild(pack);
                // No header click: it opens the equip-item picker, which only understands
                // equipment slots and throws for a backpack slot.
                _view.Show(slot, _backpackContext, _controller, ItemUiContext.Instance,
                    _skills, _insurance, false);
                _view.gameObject.name = InjectedName;
                var rect = _view.transform.Cast<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.sizeDelta = size;
                if (!holster && _view._slotPlace != null)
                {
                    // The doll's item area is landscape. Follow the portrait bounds instead.
                    var place = _view._slotPlace;
                    place.anchorMin = Vector2.zero;
                    place.anchorMax = Vector2.one;
                    place.offsetMin = new Vector2(6, 6);
                    place.offsetMax = new Vector2(-6, -28);
                    place.localRotation = Quaternion.identity;
                    _view.ScaleItem(place.rect.size);
                }
                var layout = _view.GetComponent<UnityEngine.UI.LayoutElement>()
                    ?? _view.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.ignoreLayout = true;
                if (_view.SlotBackImage != null)
                {
                    var ghost = BackpackSlotIcon.For(gun) ?? template.SlotBackImage?.sprite;
                    if (ghost != null) _view.SetSlotBackImage(ghost);
                    if (!holster) ConfigurePortraitBackground(_view, size);
                }
                // The weapon doll comes from a different canvas/mask hierarchy.
                // Rebind its graphics to this inventory scroll viewport; a nested
                // sorting canvas otherwise renders past the raid search clipping.
                foreach (var canvas in _view.GetComponentsInChildren<Canvas>(true))
                    canvas.overrideSorting = false;
                foreach (var graphic in _view.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>(true))
                {
                    graphic.maskable = true;
                    graphic.RecalculateMasking();
                    graphic.RecalculateClipping();
                }
                _boundSlot = slot;
                _caption = null;
                if (Plugin.DebugLogging.Value)
                    Plugin.Log.LogInfo("Bound backpack weapon slot on " + pack.TemplateId);
            }
            PlaceSlot(backpackView);
            if (_caption != Plugin.Caption.Value)
            {
                _caption = Plugin.Caption.Value;
                if (_view._headerText != null) _view._headerText.text = _caption;
                if (_view._slotHeader?._headerText != null)
                    _view._slotHeader._headerText.text = _caption;
            }
        }

        // Re-run every frame: the grids and picture move when the panel relayouts.
        // Right of the grids when that fits on screen, else the column under the picture.
        [HideFromIl2Cpp]
        private void PlaceSlot(SlotView backpackView)
        {
            var parent = _viewHost.parent.Cast<RectTransform>();
            var corners = _corners ??= new Il2CppStructArray<Vector3>(4);
            // Positions below are for our header's top. The holster prefab draws its
            // header above its own rect, the weapon prefab inside it.
            float headerRise = 0;
            if (_view._slotHeader != null)
            {
                _view._slotHeader.transform.Cast<RectTransform>().GetWorldCorners(corners);
                headerRise = parent.InverseTransformPoint(corners[1]).y - _viewHost.localPosition.y;
            }
            backpackView._slotPlace.GetWorldCorners(corners);
            Vector2 iconBottomLeft = parent.InverseTransformPoint(corners[0]);
            var position = new Vector2(iconBottomLeft.x, iconBottomLeft.y - SlotGap);

            float gridsRight = float.MinValue;
            if (_grids == null || Time.frameCount >= _nextGridSearch)
            {
                _grids = backpackView.GetComponentsInChildren<GridView>(true)
                    .Where(g => !g.transform.IsChildOf(_viewHost)).ToArray();
                _nextGridSearch = Time.frameCount + SearchInterval;
            }
            foreach (var grid in _grids)
            {
                if (grid == null || !grid.gameObject.activeInHierarchy) continue;
                grid.transform.Cast<RectTransform>().GetWorldCorners(corners);
                gridsRight = Mathf.Max(gridsRight, parent.InverseTransformPoint(corners[2]).x);
            }
            if (gridsRight > float.MinValue && _clip != null)
            {
                _clip.GetWorldCorners(corners);
                float visibleRight = parent.InverseTransformPoint(corners[2]).x;
                float x = gridsRight + SlotGap;
                if (x + _viewHost.sizeDelta.x <= visibleRight)
                {
                    // Line the ON PACK header up with the BACKPACK header.
                    var header = backpackView._slotHeader != null
                        ? backpackView._slotHeader.transform.Cast<RectTransform>() : backpackView._slotPlace;
                    header.GetWorldCorners(corners);
                    position = new Vector2(x, parent.InverseTransformPoint(corners[1]).y);
                }
            }

            position.y -= headerRise;
            if (((Vector2)_viewHost.localPosition - position).sqrMagnitude > .25f)
                _viewHost.localPosition = position;

            // Our element outranks the row's own layout, so carry its height over
            // or tall grids get squashed into the next row.
            float needed = parent.rect.yMax - (position.y - _viewHost.sizeDelta.y);
            _rowElements ??= backpackView.GetComponents<Component>()
                .Where(c => !Il2CppUtil.Same(c, _rowLayout) && c.TryCast<UnityEngine.UI.ILayoutElement>() != null)
                .ToArray();
            foreach (var component in _rowElements)
            {
                if (component == null || component.TryCast<Behaviour>() is { enabled: false }) continue;
                var element = component.Cast<UnityEngine.UI.ILayoutElement>();
                needed = Mathf.Max(needed, Mathf.Max(element.minHeight, element.preferredHeight));
            }
            if (Mathf.Abs(_rowLayout.minHeight - needed) > .5f)
                _rowLayout.minHeight = needed;
        }

        [HideFromIl2Cpp]
        private static void ConfigurePortraitBackground(SlotView view, Vector2 size)
        {
            if (view.SlotBackImage == null) return;
            var image = view.SlotBackImage;
            var rect = image.rectTransform;
            // Rotate the empty silhouette only. Leave the root, item parent,
            // borders, hit targets and header at their original rotation.
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -14);
            rect.sizeDelta = new Vector2(Mathf.Max(1, size.y - 44), Mathf.Max(1, size.x - 16));
            rect.localRotation = Quaternion.Euler(0, 0, 90);
            image.preserveAspect = true;
        }

        // The doll doesn't change while the panel is open, so search once and keep it.
        [HideFromIl2Cpp]
        private EquipmentTab FindEquipmentTab()
        {
            if (_tab != null || Time.frameCount < _nextTabSearch) return _tab;
            _nextTabSearch = Time.frameCount + SearchInterval;
            for (var parent = _panel.transform.parent; parent != null; parent = parent.parent)
            {
                var tabs = parent.GetComponentsInChildren<EquipmentTab>(true);
                var activeTabs = tabs.Where(t => t.gameObject.activeInHierarchy).ToArray();
                if (activeTabs.Length == 1) return _tab = activeTabs[0];
                if (tabs.Length == 1) return _tab = tabs[0];
                if (tabs.Length > 1) return null; // An ambiguous hierarchy is unsafe to clone.
            }
            return null;
        }

        private void ReleaseView()
        {
            var view = _view;
            var context = _backpackContext;
            var host = _viewHost;
            var rowLayout = _rowLayout;
            _viewHost = null;
            _rowLayout = null;
            _clip = null;
            _grids = null;
            _rowElements = null;
            _view = null;
            _backpackContext = null;
            _boundSlot = null;
            _caption = null;
            try
            {
                if (view != null) view.Close();
            }
            finally
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    view.transform.SetParent(null, false);
                    Destroy(view.gameObject);
                }
                if (host != null)
                {
                    host.gameObject.SetActive(false);
                    host.SetParent(null, false);
                    Destroy(host.gameObject);
                }
                if (rowLayout != null) Destroy(rowLayout);
                context?.Dispose();
            }
        }

        internal static void RemoveOrphans(ContainersPanel panel)
        {
            var container = panel._slotViewsContainer;
            if (container == null) return;
            // Snapshot first: Close can change the hierarchy. Match by marker only, a name
            // match would also catch vanilla's hidden SPEC view, which vanilla disposes itself.
            var children = container.GetComponentsInChildren<BackpackSlotMarker>(true)
                .Select(m => m.transform).ToArray();
            foreach (var child in children)
            {
                var view = child.GetComponent<SlotView>();
                if (view == null) continue;
                try { view.Close(); }
                catch (Exception e) { Plugin.Log.LogError("Orphan backpack slot cleanup failed: " + e); }
                finally
                {
                    child.gameObject.SetActive(false);
                    // Detach immediately so a same-frame reopen cannot find or clone it.
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
            }
        }

        internal void Shutdown()
        {
            _running = false;
            try { ReleaseView(); }
            catch (Exception e) { Plugin.Log.LogError("Backpack slot cleanup failed: " + e); }
            _panel = null;
            _tab = null;
            _nextTabSearch = 0;
            _parentContext = null;
            _equipment = null;
            _controller = null;
            _skills = null;
            _insurance = null;
        }

        private void OnDisable() { Shutdown(); }
        private void OnDestroy() { Shutdown(); }
    }

    internal sealed class BackpackSlotMarker : MonoBehaviour
    {
        public BackpackSlotMarker(IntPtr pointer) : base(pointer) { }

        // Weapon slots show the gun standing up; the holster copy keeps it flat.
        internal bool Vertical;
    }
}


