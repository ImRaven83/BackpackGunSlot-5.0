using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;

namespace BackpackGunSlot
{
    internal static class Il2CppUtil
    {
        // Wrappers are not guaranteed to be unique per native object, so compare what they point at.
        internal static bool Same(Il2CppObjectBase a, Il2CppObjectBase b) =>
            a == null ? b == null : b != null && a.Pointer == b.Pointer;

        internal static IEnumerable<T> Each<T>(Il2CppSystem.Collections.Generic.IEnumerable<T> source)
        {
            if (source == null) yield break;
            var enumerator = source.GetEnumerator();
            var untyped = enumerator.Cast<Il2CppSystem.Collections.IEnumerator>();
            while (untyped.MoveNext()) yield return enumerator.Current;
        }
    }
}
