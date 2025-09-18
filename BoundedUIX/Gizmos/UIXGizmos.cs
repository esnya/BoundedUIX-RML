using FrooxEngine;
using HarmonyLib;

namespace BoundedUIX.Gizmos
{
    using Mod = global::BoundedUIX.BoundedUIX;

    [HarmonyPatch(typeof(Gizmo))]
    internal static class UIXGizmos
    {
        [HarmonyPrefix]
        [HarmonyPatch("PositionAtTarget")]
        private static bool PositionAtTargetPrefix(Gizmo __instance)
        {
            if (!Mod.EnableUIXGizmos
             || !__instance.TargetSlot.Target.TryGetMovableRectTransform(out var rectTransform))
            {
                return true;
            }

            var center = rectTransform.GetGlobalBounds().Center;
            rectTransform.GetOriginal().Center = center;

            __instance.Slot.GlobalPosition = center - (Mod.GizmoOffset * rectTransform.Canvas.Slot.Forward);
            __instance.Slot.GlobalRotation = rectTransform.Canvas.Slot.GlobalRotation;

            return false;
        }
    }
}
