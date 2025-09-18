using FrooxEngine;
using HarmonyLib;

namespace BoundedUIX.Gizmos
{
    using Mod = global::BoundedUIX.BoundedUIX;

    [HarmonyPatch(typeof(TranslationGizmo), nameof(TranslationGizmo.SetTarget))]
    internal static class TranslationGizmoPatches
    {
        [HarmonyPostfix]
        private static void Postfix(TranslationGizmo __instance, Slot slot)
        {
            var moveableRect = slot.TryGetMovableRectTransform(out _);

            foreach (var child in __instance.Slot.Children)
            {
                child.ActiveSelf = !moveableRect || !child.Name.Contains("Z") || !Mod.EnableUIXGizmos;
            }
        }
    }
}
