using HarmonyLib;
using System;
using System.Reflection;

namespace TransitTubeOverlay.Patches
{
    public static class BuildingDef_CreateBuildingDef_PatchHelper
    {
        /// <summary>
        /// Set ViewMode of targeted Building to TransitTuveOverlay.ID
        /// </summary>
        public static void PatchViewMode(Harmony harmony, Type targetType)
        {
            var method = AccessTools.Method(targetType, "CreateBuildingDef");
            PatchViewMode(harmony, method);
        }

        /// <summary>
        /// Set ViewMode of targeted Building to TransitTuveOverlay.ID
        /// </summary>
        public static void PatchViewMode(Harmony harmony, string typeColonName)
        {
            var method = AccessTools.Method(typeColonName + ":CreateBuildingDef", parameters: Type.EmptyTypes);
            PatchViewMode(harmony, method);
        }

        private static void PatchViewMode(Harmony harmony, MethodInfo methodInfo)
        {
            
            HarmonyMethod patchMethod = new HarmonyMethod(typeof(BuildingDef_CreateBuildingDef_PatchHelper).GetMethod(nameof(UpdateViewMode), BindingFlags.NonPublic | BindingFlags.Static));
            harmony.Patch(methodInfo, postfix: patchMethod);
        }

        private static void UpdateViewMode(ref BuildingDef __result)
        {
            if (__result == null) return;
            __result.ViewMode = TransitTubeOverlay.ID;
        }
    }
}
