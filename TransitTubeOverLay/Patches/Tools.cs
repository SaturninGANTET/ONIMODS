using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;

namespace TransitTubeOverlay.Patches
{
    [HarmonyPatch(typeof(FilteredDragTool))]
    public class FilteredDragTool_Patch
    {

        /**
        * Adds Transit Tube filter toggle
        */
        [HarmonyPatch("GetDefaultFilters")]
        [HarmonyPostfix]
        public static void AddTravelTubeFilterLayerToggle(
            Dictionary<string, ToolParameterMenu.ToggleState> filters)
        {
            filters.Add(CONSTANTS.FILTERLAYERS.TRAVELTUBE, ToolParameterMenu.ToggleState.Off);
        }
        


        [HarmonyPatch("OnOverlayChanged")]

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AddTransitTubeOverlayDetection(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var result = new List<CodeInstruction>();

            var overlayIdField = typeof(TransitTubeOverlay).GetField("ID");


            for (int i = 0; i < codes.Count; i++)
            {
                result.Add(codes[i]);

                if (
                    i >= 3 &&
                    i < codes.Count - 1 &&
                    codes[i - 3].opcode == OpCodes.Call &&
                    codes[i - 3].operand is MethodInfo HashedStringEqualityFn &&
                    HashedStringEqualityFn.Name == "op_Equality" &&
                    codes[i - 2].opcode == OpCodes.Brfalse_S &&
                    codes[i - 1].opcode == OpCodes.Ldsfld &&
                    codes[i - 1].operand is FieldInfo fieldInfo &&
                    fieldInfo.Name == "LOGIC" &&
                    codes[i].opcode == OpCodes.Stloc_0
                )
                {
                    var skipLabel = generator.DefineLabel();
                    var entryPoint = new CodeInstruction(OpCodes.Ldarg_1);
                    codes[i + 1].MoveLabelsTo(entryPoint);

                    result.Add(entryPoint);
                    result.Add(new CodeInstruction(OpCodes.Ldsfld, overlayIdField));
                    result.Add(new CodeInstruction(OpCodes.Call, HashedStringEqualityFn));
                    result.Add(new CodeInstruction(OpCodes.Brfalse_S, skipLabel));
                    result.Add(new CodeInstruction(OpCodes.Ldstr, CONSTANTS.FILTERLAYERS.TRAVELTUBE));
                    result.Add(new CodeInstruction(OpCodes.Stloc_0));
                    result.Add(new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel));
                }
            }
            return result;
        }
        

        /*
         * We are checking Ids instead of ObjectLayers because transit tubes reside in ObjectLayer.Building layer
         * Changing transit tubes' objectLayer would fuck up collision logic so we can't do that
         */
        [HarmonyPatch(nameof(FilteredDragTool.GetFilterLayerFromGameObject))]
        [HarmonyPrefix]
        public static bool AddTransiteTubeComponentIdCheck(
            GameObject input,
            ref string __result
            )
        {
            BuildingComplete buildingComplete = input.GetComponent<BuildingComplete>();
            BuildingUnderConstruction buildingUnderConstruction = input.GetComponent<BuildingUnderConstruction>();
            if (buildingComplete != null &&
                buildingComplete.prefabid.HasAnyTags(TransitTubeOverlay.TargetIDs.ToList())
                )
            {
                __result = CONSTANTS.FILTERLAYERS.TRAVELTUBE;
                return false;
            }

            if (buildingUnderConstruction != null &&
                TransitTubeOverlay.TargetIDs.Contains(buildingUnderConstruction.Def.PrefabID)
                )
            {
                __result = CONSTANTS.FILTERLAYERS.TRAVELTUBE;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(DisconnectTool))]
    public static class DisconnectTool_Patch
    {
        /**
        * Adds Transit Tube filter toggle
        */
        [HarmonyPatch("GetDefaultFilters")]
        [HarmonyPostfix]
        public static void AddTravelTubeFilterLayerToggle(
            Dictionary<string, ToolParameterMenu.ToggleState> filters)
        {
            filters.Add(CONSTANTS.FILTERLAYERS.TRAVELTUBE, ToolParameterMenu.ToggleState.Off);
        }
    }
}
