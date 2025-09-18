#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace BoundedUIX
{
    [HarmonyPatch(typeof(DevTool))]
    internal static class UIXSelector
    {
        private static readonly FieldInfo GraphicField = AccessTools.Field(typeof(RectTransform), "_graphic");

        private static float2 _lastPosition = float2.MaxValue;
        private static Slot? _lastSlot;

        private static Slot CheckCanvas(RaycastHit hit)
        {
            var collider = hit.Collider ?? throw new InvalidOperationException("Raycast hit without collider");
            var bestSlot = collider.Slot;

            if (BoundedUIX.EnableUIXSelection
             && bestSlot.TryGetRectTransform(out var rectTransform)
             && rectTransform.Canvas.Slot == bestSlot)
            {
                bestSlot = FindBestRect(bestSlot.GlobalPointToLocal(hit.Point).xy, bestSlot);

                if (bestSlot.GetComponentInParents<Button>() is Button button
                 && button.Slot.HierachyDepth > rectTransform.Slot.HierachyDepth)
                {
                    bestSlot = button.Slot;
                }
            }

            return bestSlot;
        }

        private static bool HasGraphic(RectTransform rectTransform)
            => GraphicField?.GetValue(rectTransform) is Graphic;

        private static Slot FindBestRect(float2 hitPoint, Slot best)
        {
            var prioritizeDepth = BoundedUIX.PrioritizeHierarchyDepth;
            var allowLayoutSelection = BoundedUIX.AllowLayoutSelection;

            var ignoreSelected = BoundedUIX.IgnoreAlreadySelected
                && _lastSlot == best
                && MathX.Distance(_lastPosition, hitPoint) < BoundedUIX.RepeatSelectionThreshold;

            _lastPosition = hitPoint;
            _lastSlot = best;

            var traversal = new Stack<Slot>();
            traversal.Push(best);

            while (traversal.Count > 0)
            {
                var current = traversal.Pop();

                if (!current.TryGetRectTransform(out var rectTransform)
                 || (ignoreSelected && current.TryGetGizmo<SlotGizmo>() is not null))
                {
                    continue;
                }

                var isHit = rectTransform.GetCanvasBounds().Contains(hitPoint);
                var hasGraphic = HasGraphic(rectTransform);

                if (isHit
                 && (allowLayoutSelection || (hasGraphic && (!rectTransform.IsMask || rectTransform.IsMaskVisible)))
                 && (!prioritizeDepth || best.HierachyDepth <= current.HierachyDepth))
                {
                    best = current;
                }

                if (rectTransform.IsMask && (!isHit || !hasGraphic))
                {
                    continue;
                }

                foreach (var child in current.Children.Where(child => child.ActiveSelf).Reverse())
                {
                    traversal.Push(child);
                }
            }

            return best;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("TryOpenGizmo")]
        private static IEnumerable<CodeInstruction> TryOpenGizmoTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var checkCanvasHitMethod = typeof(UIXSelector).GetMethod(nameof(CheckCanvas), AccessTools.allDeclared);
            var colliderField = typeof(RaycastHit).GetField(nameof(RaycastHit.Collider), AccessTools.allDeclared);

            var instructions = codeInstructions.ToList();
            var raycastValueIndex = instructions.FindLastIndex(instruction => instruction.LoadsField(colliderField));

            if (raycastValueIndex < 0)
                return instructions;

            instructions.RemoveAt(raycastValueIndex);
            instructions[raycastValueIndex] = new CodeInstruction(OpCodes.Call, checkCanvasHitMethod);

            return instructions;
        }
    }
}
