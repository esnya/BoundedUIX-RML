using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.Undo;
using HarmonyLib;

namespace BoundedUIX.Gizmos
{
    using Mod = global::BoundedUIX.BoundedUIX;

    internal static class PlaneTranslationGizmoPatches
    {
        private static readonly AccessTools.FieldRef<PlaneTranslationGizmo, float3> PointOffsetRef = AccessTools.FieldRefAccess<PlaneTranslationGizmo, float3>("pointOffset");
        private static readonly AccessTools.FieldRef<PlaneTranslationGizmo, SyncRef<SegmentMesh>> Line0Ref = AccessTools.FieldRefAccess<PlaneTranslationGizmo, SyncRef<SegmentMesh>>("line0");
        private static readonly AccessTools.FieldRef<PlaneTranslationGizmo, SyncRef<SegmentMesh>> Line1Ref = AccessTools.FieldRefAccess<PlaneTranslationGizmo, SyncRef<SegmentMesh>>("line1");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlaneTranslationGizmo), "OnInteractionBegin", new[] { typeof(Slot), typeof(float3), typeof(float3), typeof(float3?), typeof(bool) })]
        private static void OnInteractionBeginPostfix(PlaneTranslationGizmo __instance)
        {
            if (!Mod.EnableUIXGizmos || !__instance.TargetSlot.Target.TryGetMovableRectTransform(out var rectTransform))
                return;

            var originalTransform = rectTransform.GetOriginal();
            originalTransform.Update(rectTransform);

            __instance.World.BeginUndoBatch("Undo.TranslateAlongAxis".AsLocaleKey());

            if (!originalTransform.Local)
            {
                rectTransform.OffsetMin.CreateUndoPoint(true);
                rectTransform.OffsetMax.CreateUndoPoint(true);
            }
            else
            {
                rectTransform.AnchorMin.CreateUndoPoint(true);
                rectTransform.AnchorMax.CreateUndoPoint(true);
            }

            __instance.World.EndUndoBatch();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlaneTranslationGizmo), "UpdatePoint", new[] { typeof(float3) })]
        private static bool UpdatePointPrefix(PlaneTranslationGizmo __instance, float3 localPoint)
        {
            var targetSlot = __instance.TargetSlot.Target;
            if (!Mod.EnableUIXGizmos || !targetSlot.TryGetMovableRectTransform(out var rectTransform))
                return true;

            var offsetPoint = localPoint - PointOffsetRef(__instance);
            var projectedPoint = MathX.Reject(offsetPoint, __instance.LocalNormal);
            projectedPoint = __instance.Slot.LocalPointToGlobal(projectedPoint);
            projectedPoint = __instance.PointSpace.Space.GlobalPointToLocal(projectedPoint);
            var originalRect = rectTransform.GetOriginal();
            var translationOffset = (projectedPoint - __instance.PointSpace.Space.GlobalPointToLocal(originalRect.Center)).xy;

            var pxOffset = rectTransform.Canvas.UnitScale.Value * translationOffset;
            if (!originalRect.Local)
            {
                if (rectTransform.OffsetMin.CanSet())
                    rectTransform.OffsetMin.Value += pxOffset;

                if (rectTransform.OffsetMax.CanSet())
                    rectTransform.OffsetMax.Value += pxOffset;
            }
            else
            {
                var anchorOffset = pxOffset / rectTransform.RectParent.ComputeGlobalComputeRect().size;

                if (rectTransform.AnchorMin.CanSet())
                    rectTransform.AnchorMin.Value += anchorOffset;

                if (rectTransform.AnchorMax.CanSet())
                    rectTransform.AnchorMax.Value += anchorOffset;
            }

            var line = MathX.Project(localPoint, __instance.LocalNormal);
            if (Line0Ref(__instance).Target is SegmentMesh line0)
                line0.PointB.Value = line;

            if (Line1Ref(__instance).Target is SegmentMesh line1)
            {
                line1.PointA.Value = line;
                line1.PointB.Value = float3.Zero;
            }

            return false;
        }
    }
}
