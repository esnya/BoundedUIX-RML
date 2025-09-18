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
    }
}
