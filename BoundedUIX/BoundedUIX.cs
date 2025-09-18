using HarmonyLib;
using ResoniteModLoader;

namespace BoundedUIX
{
    public class BoundedUIX : ResoniteMod
    {
        internal const string TargetSlotNamePlaceholder = "{TargetName}";

        public static ModConfiguration Config { get; private set; } = null!;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> enableUIXSelectionKey = new(
            "EnableUIXSelection",
            "Enable selecting elements in UIX hierarchies directly with the Developer ToolTip.",
            () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> allowLayoutSelectionKey = new(
            "AllowLayoutSelection",
            "Allow selecting UIX RectTransforms that do not have visual elements (layout parents).",
            () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> ignoreAlreadySelectedKey = new(
            "IgnoreAlreadySelected",
            "Skip already selected elements in the targeting process. Helps with layered elements.",
            () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> repeatSelectionThresholdKey = new(
            "RepeatSelectionThreshold",
            "Minimum local canvas distance between hits before they count as a new selection attempt.",
            () => 15f,
            valueValidator: value => value >= 0f);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> prioritizeHierarchyDepthKey = new(
            "PrioritizeHierarchyDepth",
            "Prioritize hierarchy depth over layout order when selecting RectTransforms.",
            () => false);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> enableUIXGizmosKey = new(
            "EnableUIXGizmos",
            "Enable modifying Slot Gizmo behavior in UIX hierarchies to move the elements instead.",
            () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> gizmoOffsetKey = new(
            "GizmoOffset",
            "Distance to raise the movement gizmos from the surface of the canvas. World scale.",
            () => 0.02f,
            valueValidator: value => value >= 0f);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> moveTransformToParentKey = new(
            "MoveTransformToParent",
            "Move RectTransform values up when creating a parent (analog to Slot transforms).",
            () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> childSlotNameKey = new(
            "ChildSlotName",
            "Default name for child Slots in UIX hierarchies. Use {TargetName} to get the parent's name.",
            () => "Panel",
            valueValidator: name => !string.IsNullOrWhiteSpace(name));

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> parentSlotNameKey = new(
            "ParentSlotName",
            "Default name for parent Slots in UIX hierarchies. Use {TargetName} to get the child-to-be's name.",
            () => "{TargetName} Space",
            valueValidator: name => !string.IsNullOrWhiteSpace(name));

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> pivotSlotNameKey = new(
            "PivotSlotName",
            "Default name for pivot Slots in UIX hierarchies. Use {TargetName} to get the child-to-be's name.",
            () => "{TargetName} Space",
            valueValidator: name => !string.IsNullOrWhiteSpace(name));

        public static bool EnableUIXSelection => Config.GetValue(enableUIXSelectionKey);
        public static bool AllowLayoutSelection => Config.GetValue(allowLayoutSelectionKey);
        public static bool IgnoreAlreadySelected => Config.GetValue(ignoreAlreadySelectedKey);
        public static float RepeatSelectionThreshold => Config.GetValue(repeatSelectionThresholdKey);
        public static bool PrioritizeHierarchyDepth => Config.GetValue(prioritizeHierarchyDepthKey);
        public static bool EnableUIXGizmos => Config.GetValue(enableUIXGizmosKey);
        public static float GizmoOffset => Config.GetValue(gizmoOffsetKey);
        public static bool MoveTransformToParent => Config.GetValue(moveTransformToParentKey);
        public static string ChildSlotName => Config.GetValue(childSlotNameKey);
        public static string ParentSlotName => Config.GetValue(parentSlotNameKey);
        public static string PivotSlotName => Config.GetValue(pivotSlotNameKey);

        public override string Author => "Banane9";
        public override string Link => "https://github.com/ResoniteModdingGroup/BoundedUIX";
        public override string Name => "BoundedUIX-RML";
        public override string Version => "3.0.0";

        public override void OnEngineInit()
        {
            var harmony = new Harmony($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Save(true);
            harmony.PatchAll();
        }
    }
}
