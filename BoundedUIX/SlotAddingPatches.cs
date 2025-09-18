using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.Undo;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace BoundedUIX
{
    internal static class SlotAddingPatches
    {
        private static IEnumerable<CodeInstruction> PostfixToAddSlot(this IEnumerable<CodeInstruction> codeInstructions, IEnumerable<CodeInstruction> targetSlotLoadInstructions, AddSlotPostfix postfix)
        {
            var addSlotMethod = typeof(Slot).GetMethod(nameof(Slot.AddSlot), new[] { typeof(string) });

            foreach (var code in codeInstructions)
            {
                yield return code;

                if (code.Calls(addSlotMethod))
                {
                    foreach (var loadInstruction in targetSlotLoadInstructions)
                        yield return loadInstruction;

                    yield return new CodeInstruction(OpCodes.Call, postfix.Method);
                }
            }
        }

        private delegate Slot AddSlotPostfix(Slot newSlot, Slot targetSlot);

        [HarmonyPatch(typeof(SceneInspector))]
        private static class SceneInspectorPatches
        {
            private static IEnumerable<CodeInstruction> LoadFromSceneInspector
            {
                get
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SceneInspector), nameof(SceneInspector.ComponentView)));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(SyncRef<Slot>), nameof(SyncRef<Slot>.Target)));
                }
            }

            private static Slot OnAddChildPostfix(Slot newSlot, Slot targetSlot)
            {
                if (targetSlot.TryGetRectTransform(out _))
                {
                    newSlot.Name = BoundedUIX.ChildSlotName.Replace(BoundedUIX.TargetSlotNamePlaceholder, targetSlot.Name);
                    newSlot.AttachComponent<RectTransform>();
                }

                return newSlot;
            }

            [HarmonyTranspiler]
            [HarmonyPatch("OnAddChildPressed")]
            private static IEnumerable<CodeInstruction> OnAddChildPressedTranspiler(IEnumerable<CodeInstruction> codeInstructions)
            {
                return codeInstructions.PostfixToAddSlot(LoadFromSceneInspector, OnAddChildPostfix);
            }

            private static Slot OnInsertParentPostfix(Slot newSlot, Slot targetSlot)
            {
                if (targetSlot.TryGetMovableRectTransform(out var originalTransform))
                {
                    newSlot.Name = BoundedUIX.ParentSlotName.Replace(BoundedUIX.TargetSlotNamePlaceholder, targetSlot.Name);
                    var newTransform = newSlot.AttachComponent<RectTransform>();

                    if (BoundedUIX.MoveTransformToParent)
                    {
                        newTransform.CopyValues(originalTransform);
                        originalTransform.ResetTransform();
                    }
                }

                return newSlot;
            }

            [HarmonyTranspiler]
            [HarmonyPatch("OnInsertParentPressed")]
            private static IEnumerable<CodeInstruction> OnInsertParentPressedTranspiler(IEnumerable<CodeInstruction> codeInstructions)
            {
                return codeInstructions.PostfixToAddSlot(LoadFromSceneInspector, OnInsertParentPostfix);
            }
        }

        [HarmonyPatch(typeof(SlotPositioning))]
        private static class SlotPositioningCreatePivotAtCenterPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("CreatePivotAtCenter", new[] { typeof(Slot), typeof(bool) })]
            private static void CreatePivotAtCenterPostfix(Slot __0, bool __1, ref Slot __result)
                => AdjustPivot(__0, ref __result);

            [HarmonyPostfix]
            [HarmonyPatch("CreatePivotAtCenter", new[] { typeof(Slot), typeof(BoundingBox), typeof(bool) }, new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
            private static void CreatePivotAtCenterWithBoxPostfix(Slot __0, ref BoundingBox __1, bool __2, ref Slot __result)
                => AdjustPivot(__0, ref __result);

            private static void AdjustPivot(Slot targetSlot, ref Slot result)
            {
                if (!targetSlot.TryGetMovableRectTransform(out var originalTransform))
                    return;

                var parentTransform = originalTransform.RectParent;
                if (parentTransform == null)
                    return;

                var pivotSlot = result;
                var configuredName = BoundedUIX.PivotSlotName.Replace(BoundedUIX.TargetSlotNamePlaceholder, targetSlot.Name);

                if (pivotSlot == targetSlot)
                {
                    var parentSlot = targetSlot.Parent;
                    if (parentSlot == null)
                        return;

                    pivotSlot = parentSlot.AddSlot(configuredName);
                    pivotSlot.AttachComponent<RectTransform>();
                    targetSlot.SetParent(pivotSlot);
                }
                else
                {
                    pivotSlot.Name = configuredName;
                }

                var pivotTransform = pivotSlot.GetComponent<RectTransform>() ?? pivotSlot.AttachComponent<RectTransform>();

                var originalArea = originalTransform.ComputeGlobalComputeRect();
                var parentArea = parentTransform.ComputeGlobalComputeRect();
                var parentSize = parentArea.size;
                if (parentSize == float2.Zero)
                    return;

                var pivotAnchor = (originalArea.Center - parentArea.ExtentMin) / parentSize;
                var pivotOffset = originalArea.size / 2f;

                pivotTransform.AnchorMin.Value = pivotAnchor;
                pivotTransform.AnchorMax.Value = pivotAnchor;
                pivotTransform.OffsetMin.Value = -pivotOffset;
                pivotTransform.OffsetMax.Value = pivotOffset;

                if (BoundedUIX.MoveTransformToParent)
                {
                    pivotTransform.CopyValues(originalTransform);
                }

                originalTransform.ResetTransform();
                result = pivotSlot;
            }
        }
    }
}
