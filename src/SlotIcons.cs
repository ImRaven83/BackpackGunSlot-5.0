using System.IO;
using EFT.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BackpackGunSlot
{
    internal static class SlotIcons
    {
        private static Sprite _smg;
        private static Sprite _shotgun;

        // Vanilla's own empty holster ghost, so pistols match the rest of the doll.
        internal static Sprite Pistol => ResourcesCache.Pop<Sprite>("Slots/Equipment/Holster");

        // The game ships no SMG or shotgun ghost. assets/make_ghost.py builds these in the vanilla style.
        internal static Sprite Smg => _smg != null ? _smg : _smg = Load("BackpackGunSlot.smg.png");
        internal static Sprite Shotgun => _shotgun != null ? _shotgun : _shotgun = Load("BackpackGunSlot.shotgun.png");

        private static Sprite Load(string resource)
        {
            using var stream = typeof(SlotIcons).Assembly.GetManifestResourceStream(resource);
            if (stream == null)
            {
                Plugin.Log.LogError("Missing embedded icon " + resource);
                return null;
            }
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = resource,
                wrapMode = TextureWrapMode.Clamp,
                // Only managed code references these, which the game's asset sweep cannot see.
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            texture.LoadImage(new Il2CppStructArray<byte>(memory.ToArray()), true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        internal static void Clear()
        {
            Destroy(ref _smg);
            Destroy(ref _shotgun);
        }

        private static void Destroy(ref Sprite sprite)
        {
            if (sprite == null) return;
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
            sprite = null;
        }
    }
}
