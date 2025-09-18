#nullable enable
#pragma warning disable CS8602
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace BoundedUIX.Gizmos
{
    using Mod = global::BoundedUIX.BoundedUIX;

    [HarmonyPatch(typeof(SlotGizmo))]
    internal static class SlotGizmoPatches
    {
        private static readonly AccessTools.FieldRef<SlotGizmo, TransformRelayRef> TargetSlotRef = AccessTools.FieldRefAccess<SlotGizmo, TransformRelayRef>("_targetSlot");
        private static readonly AccessTools.FieldRef<SlotGizmo, SyncRef<TranslationGizmo>> TranslationGizmoRef = AccessTools.FieldRefAccess<SlotGizmo, SyncRef<TranslationGizmo>>("_translationGizmo");
        private static readonly AccessTools.FieldRef<SlotGizmo, SyncRef<RotationGizmo>> RotationGizmoRef = AccessTools.FieldRefAccess<SlotGizmo, SyncRef<RotationGizmo>>("_rotationGizmo");
        private static readonly AccessTools.FieldRef<SlotGizmo, SyncRef<ScaleGizmo>> ScaleGizmoRef = AccessTools.FieldRefAccess<SlotGizmo, SyncRef<ScaleGizmo>>("_scaleGizmo");
        private static readonly AccessTools.FieldRef<ScaleGizmo, SyncRef<Slot>> ScaleGizmoZSlotRef = AccessTools.FieldRefAccess<ScaleGizmo, SyncRef<Slot>>("_zSlot");

        private static BoundingBox BoundUIX(BoundingBox bounds, Slot target, Slot space)
        {
            if (!Mod.EnableUIXGizmos)
                return bounds;

            if (!target.TryGetMovableRectTransform(out var rectTransform))
                return bounds;

            var canvas = rectTransform.Canvas;
            var canvasSlot = canvas?.Slot;
            if (canvas == null || canvasSlot == null)
                return bounds;

            var area = rectTransform.ComputeGlobalComputeRect();
            bounds.Encapsulate(space.GlobalPointToLocal(canvasSlot.LocalPointToGlobal(area.ExtentMin / canvas.UnitScale)));
            bounds.Encapsulate(space.GlobalPointToLocal(canvasSlot.LocalPointToGlobal(area.ExtentMax / canvas.UnitScale)));

            return bounds;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnCommonUpdate")]
        private static IEnumerable<CodeInstruction> OnCommonUpdateTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var boundUIXMethod = typeof(SlotGizmoPatches).GetMethod(nameof(BoundUIX), AccessTools.allDeclared);
            var computeBoundingBoxMethod = typeof(BoundsHelper).GetMethod(nameof(BoundsHelper.ComputeBoundingBox), AccessTools.allDeclared);
            var getGlobalPositionMethod = typeof(Slot).GetProperty(nameof(Slot.GlobalPosition), AccessTools.allDeclared).GetMethod;
            var uixBoundCenterMethod = typeof(SlotGizmoPatches).GetMethod(nameof(UIXBoundCenter), AccessTools.allDeclared);

            var instructions = codeInstructions.ToList();

            var globalPositionIndex = instructions.FindIndex(instruction => instruction.Calls(getGlobalPositionMethod));

            if (globalPositionIndex < 0)
                return instructions;

            instructions[globalPositionIndex] = new CodeInstruction(OpCodes.Call, uixBoundCenterMethod);

            var computeIndex = instructions.FindIndex(globalPositionIndex, instruction => instruction.Calls(computeBoundingBoxMethod));

            if (computeIndex < 0)
                return instructions;

            instructions.Insert(computeIndex + 1, instructions[computeIndex - 5]);
            instructions.Insert(computeIndex + 2, instructions[computeIndex - 3]);
            instructions.Insert(computeIndex + 3, new CodeInstruction(OpCodes.Call, boundUIXMethod));

            return instructions;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Setup")]
        private static void SetupPostfix(SlotGizmo __instance)
        {
            __instance.IsLocalSpace.OnValueChange += field => __instance.SwitchSpace();

            var targetSlot = TargetSlotRef(__instance).Target;
            RectTransform? rectTransform = null;
            var moveableRect = targetSlot != null && targetSlot.TryGetMovableRectTransform(out rectTransform);

            if (moveableRect && rectTransform != null)
                rectTransform.GetOriginal().Local = __instance.IsLocalSpace.Value;

            if (ScaleGizmoRef(__instance).Target is ScaleGizmo scaleGizmo)
            {
                if (ScaleGizmoZSlotRef(scaleGizmo).Target is Slot zSlot)
                    zSlot.ActiveSelf = !moveableRect || !Mod.EnableUIXGizmos;

                if (scaleGizmo.Slot.GetComponent<MeshRenderer>(r => r.Materials[0] is OverlayFresnelMaterial material && material.FrontNearColor == colorX.Blue) is MeshRenderer renderer)
                    renderer.Enabled = !moveableRect || !Mod.EnableUIXGizmos;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SwitchSpace")]
        private static bool SwitchSpacePrefix(SlotGizmo __instance)
        {
            var local = __instance.IsLocalSpace.Value;
            var target = TargetSlotRef(__instance).Target;

            if (target == null)
                return true;

            if (Mod.EnableUIXGizmos && target.TryGetMovableRectTransform(out var rectTransform))
            {
                rectTransform.GetOriginal().Local = local;
                local = true;
            }

            if (TranslationGizmoRef(__instance).Target is TranslationGizmo translationGizmo)
                translationGizmo.SetTarget(target.Position_Field, target, local);

            if (RotationGizmoRef(__instance).Target is RotationGizmo rotationGizmo)
                rotationGizmo.SetTarget(target.Rotation_Field, target, local);

            return false;
        }

        private static float3 UIXBoundCenter(Slot target)
        {
            if (!Mod.EnableUIXGizmos)
                return target.GlobalPosition;

            if (!target.TryGetMovableRectTransform(out var rectTransform))
                return target.GlobalPosition;

            var canvas = rectTransform.Canvas;
            var canvasSlot = canvas?.Slot;
            if (canvas == null || canvasSlot == null)
                return target.GlobalPosition;

            return rectTransform.GetGlobalBounds().Center - (Mod.GizmoOffset * canvasSlot.Forward);
        }
    }
}
#pragma warning restore CS8602
