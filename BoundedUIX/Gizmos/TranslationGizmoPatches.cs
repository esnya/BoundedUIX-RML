using FrooxEngine;
using HarmonyLib;

namespace BoundedUIX.Gizmos
{
    using Mod = global::BoundedUIX.BoundedUIX;

    [HarmonyPatch(typeof(TranslationGizmo))]
    internal static class TranslationGizmoPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetTarget")]
        private static void SetTargetPostfix(TranslationGizmo __instance, Slot slot)
        {
            var moveableRect = slot.TryGetMovableRectTransform(out _);

            foreach (var child in __instance.Slot.Children)
            {
                child.ActiveSelf = !moveableRect || !child.Name.Contains("Z") || !Mod.EnableUIXGizmos;
            }
        }
    }
}
