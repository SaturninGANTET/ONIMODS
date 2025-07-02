using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Database;
using HarmonyLib;
using KMod;
using STRINGS;
using UnityEngine;


namespace TransitTube_Overlay_Mod
{
    public class Patches
    {
        public static MethodInfo DebugLogFn = AccessTools.Method(typeof(Debug), nameof(Debug.Log), new Type[] { typeof(object) });
        public class TransiteTubeOverlayMod : UserMod2
        {

            public override void OnLoad(Harmony harmony)
            {
                Util.RegisterAllStrings();
                base.OnLoad(harmony);

                var IsAreaClearMethod = AccessTools.Method(typeof(BuildingDef), "IsAreaClear", new Type[]{
                    typeof(GameObject),
                    typeof(int),
                    typeof(Orientation),
                    typeof(ObjectLayer),
                    typeof(ObjectLayer),
                    typeof(bool),
                    typeof(bool),
                    typeof(string).MakeByRefType(),
                    typeof(bool)
                });
                harmony.Patch(IsAreaClearMethod, postfix: new HarmonyMethod(typeof(BuildingDef_IsAreaClear_Patch).GetMethod(nameof(BuildingDef_IsAreaClear_Patch.Postfix))));
            }

            public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
            {
                base.OnAllModsLoaded(harmony, mods);

                if (Util.isModLoaded(Constants.otherMods.TravelTubesExpanded))
                {
                    MethodInfo BunkerCreateFn = AccessTools.Method(
                        "TravelTubesExpanded.TravelTubeBunkerWallBridgeConfig:CreateBuildingDef",
                        parameters: Type.EmptyTypes
                    );
                    MethodInfo FirePoleCreateFn = AccessTools.Method(
                        "TravelTubesExpanded.TravelTubeFirePoleBridgeConfig:CreateBuildingDef",
                        parameters: Type.EmptyTypes
                    );
                    MethodInfo InsulatedCreateFn = AccessTools.Method(
                        "TravelTubesExpanded.TravelTubeInsulatedWallBridgeConfig:CreateBuildingDef",
                        parameters: Type.EmptyTypes
                    );
                    MethodInfo LadderCreateFn = AccessTools.Method(
                        "TravelTubesExpanded.TravelTubeLadderBridgeConfig:CreateBuildingDef",
                        parameters: Type.EmptyTypes
                    );

                    harmony.Patch(BunkerCreateFn, postfix: new HarmonyMethod(typeof(TravelTubeExpanded_CreateBuildingDef_Patch).GetMethod(nameof(TravelTubeExpanded_CreateBuildingDef_Patch.Postfix))));
                    harmony.Patch(FirePoleCreateFn, postfix: new HarmonyMethod(typeof(TravelTubeExpanded_CreateBuildingDef_Patch).GetMethod(nameof(TravelTubeExpanded_CreateBuildingDef_Patch.Postfix))));
                    harmony.Patch(InsulatedCreateFn, postfix: new HarmonyMethod(typeof(TravelTubeExpanded_CreateBuildingDef_Patch).GetMethod(nameof(TravelTubeExpanded_CreateBuildingDef_Patch.Postfix))));
                    harmony.Patch(LadderCreateFn, postfix: new HarmonyMethod(typeof(TravelTubeExpanded_CreateBuildingDef_Patch).GetMethod(nameof(TravelTubeExpanded_CreateBuildingDef_Patch.Postfix))));

                }
            }
        }


        public static class TravelTubeExpanded_CreateBuildingDef_Patch
        {
            public static void Postfix(ref BuildingDef __result)
            {
                if (__result == null) return;
                __result.ViewMode = TransitTubeOverlay.ID;
                __result.ObjectLayer = ObjectLayer.TravelTube;
                __result.BuildLocationRule = Constants.BuildLocationRules.transitTubeBridge;
            }
        }


        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        public static class TransitTubeOverlayMod_AssetsInit
        {
            public static void Postfix()
            {
                Util.RegisterEmbeddedIcon(
                    Constants.RessourceName.TransitTubeOverlayToggle,
                    Constants.AssetsName.TransitTubeOverlayToggle
                );
                Util.RegisterEmbeddedIcon(
                    Constants.RessourceName.TransitTubeInput,
                    Constants.AssetsName.TransitTubeInput
                );
                Util.RegisterEmbeddedIcon(
                    Constants.RessourceName.TransitTubeOutput,
                    Constants.AssetsName.TransitTubeOutput
                );
            }
        }

        /**
         * Adds auto overlay toggle when selecting building to build.
         */
        [HarmonyPatch(typeof(TravelTubeConfig), nameof(TravelTubeConfig.CreateBuildingDef))]
        public static class TravelTubeConfig_CreateBuildingDef_Patch
        {
            //Execute after travel tube anywhere to avoid beeing overriden
            [HarmonyPriority(Priority.Low)]
            public static void Postfix(ref BuildingDef __result)
            {
                if (__result == null) return;
                __result.ViewMode = TransitTubeOverlay.ID;
                __result.ObjectLayer = ObjectLayer.TravelTube;
                if(__result.BuildLocationRule == BuildLocationRule.NotInTiles)
                {
                    __result.BuildLocationRule = Constants.BuildLocationRules.transitTube;
                }
            }
        }
        [HarmonyPatch(typeof(TravelTubeWallBridgeConfig), nameof(TravelTubeWallBridgeConfig.CreateBuildingDef))]
        public static class TravelTubeWallBridgeConfig_CreateBuildingDef_Patch
        {
            public static void Postfix(ref BuildingDef __result)
            {
                if (__result == null) return;
                __result.ViewMode = TransitTubeOverlay.ID;
                __result.ObjectLayer = ObjectLayer.TravelTube;
                if (__result.BuildLocationRule == BuildLocationRule.Tile)
                {
                    __result.BuildLocationRule = Constants.BuildLocationRules.transitTubeBridge;
                }
            }
        }
        [HarmonyPatch(typeof(TravelTubeEntranceConfig), nameof(TravelTubeEntranceConfig.CreateBuildingDef))]
        public static class TravelTubeEntrance_CreateBuildingDef_Patch
        {
            public static void Postfix(ref BuildingDef __result)
            {
                if (__result == null) return;
                __result.ViewMode = TransitTubeOverlay.ID;
                __result.ObjectLayer = ObjectLayer.TravelTube;
                if (__result.BuildLocationRule == BuildLocationRule.OnFloor)
                {
                    __result.BuildLocationRule = Constants.BuildLocationRules.transitTubeEntrance;
                }
            }
        }


        /**
         * Adds Transit Tube overlay button in overlayMenu
         */
        [HarmonyPatch(typeof(OverlayMenu), "InitializeToggles")]
        public class InitalizeToggles_Patch
        {
            public static void Postfix(List<KIconToggleMenu.ToggleInfo> ___overlayToggleInfos)
            {
                Type overlayToggleInfoType = typeof(OverlayMenu).GetNestedType("OverlayToggleInfo", BindingFlags.NonPublic);
                if (overlayToggleInfoType == null)
                {
                    Debug.LogError("[TransitTube_Overlay_Mod] Couldn't find class OverlayToggleInfo.");
                    return;
                }

                ConstructorInfo ctor = overlayToggleInfoType.GetConstructor(new Type[]
                {
                    typeof(string),          // text
                    typeof(string),          // icon_name
                    typeof(HashedString),    // sim_view
                    typeof(string),          // required_tech_item
                    typeof(Action),          // hotKey
                    typeof(string),          // tooltip
                    typeof(string)           // tooltip_header
                });


                object newToggle = ctor.Invoke(new object[]
                {
                    "Transit Tube Overlay",             // text
                    "mod_overlay_transit_tube",             // icon_name
                    TransitTubeOverlay.ID,              // sim_view
                    "TravelTube",                       // required_tech_item
                    Action.NumActions,                  // hotKey
                    "Displays transit tube components", // tooltip
                    "Transit Tube Overlay"              // tooltip_header
                });

                if (newToggle is KIconToggleMenu.ToggleInfo toggleInfo)
                {
                    ___overlayToggleInfos.Add(toggleInfo);
                }
                else
                {
                    Debug.LogError("[TransitTube_Overlay_Mod] Couldn't cast to toggleInfo.");
                }
            }
        }

        [HarmonyPatch(typeof(OverlayScreen), "RegisterModes")]
        public static class OverlayScreen_RegisterModes_Patch
        {
            public static void Postfix(OverlayScreen __instance)
            {
                var registerModeMethod = AccessTools.Method(typeof(OverlayScreen), "RegisterMode");

                if (registerModeMethod == null)
                {
                    Debug.LogError("[TransitTubeOverlay] Failed to find RegisterMode method.");
                    return;
                }

                var overlay = new TransitTubeOverlay();

                registerModeMethod.Invoke(__instance, new object[] { overlay });
            }
        }



        /**
         * Finds the real exits (no bridge)
         */
        [HarmonyPatch(typeof(TravelTube), "OnDirtyNavCellUpdated")]
        public static class TravelTube_OnDirtyNavCellUpdated_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                LocalBuilder realExitFlag = generator.DeclareLocal(typeof(bool));

                var codes = new List<CodeInstruction>(instructions);
                var setIsValidExitOnly = AccessTools.Method(typeof(TravelTubeExt), nameof(TravelTubeExt.SetIsValidExitOnly));

                for (int i = 0; i < codes.Count; i++)
                {
                    yield return codes[i];

                    //Init realExitFlag
                    if (
                        i >= 1 &&
                        codes[i - 1].opcode == OpCodes.Ldc_I4_0 &&
                        codes[i].opcode == OpCodes.Stloc_3
                    )
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Stloc_S, realExitFlag);
                    }


                    //Set Flag
                    if (
                        i >= 2 &&
                        codes[i - 2].opcode == OpCodes.Ldfld &&
                        codes[i - 2].operand is FieldInfo field &&
                        field.Name == "endNavType" &&
                        codes[i - 1].opcode == OpCodes.Ldc_I4_8 &&
                        codes[i].opcode == OpCodes.Beq_S
                    )
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Stloc_S, realExitFlag);
                    }

                    //Update internal State
                    if (
                        codes[i].opcode == OpCodes.Call &&
                        codes[i].operand is MethodInfo method &&
                        method.Name == "UpdateExitStatus" && method.DeclaringType == typeof(TravelTube)
                    )
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, realExitFlag);
                        yield return new CodeInstruction(OpCodes.Call, setIsValidExitOnly);
                    }
                }
            }
        }


        /**
         * Updates the bitfieldMap to allow StatusItems to display on our overlay only
         */
        [HarmonyPatch(typeof(StatusItem), nameof(StatusItem.GetStatusItemOverlayBySimViewMode))]
        public static class StatusItem_GetStatusItemOverlayBySimViewMode_Patch
        {
            static bool patched = false;

            public static void Prefix()
            {
                if (patched) return;
                patched = true;

                var field = typeof(StatusItem).GetField("overlayBitfieldMap", BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                {
                    Debug.LogError("Failed to find overlayBitfieldMap field.");
                    return;
                }

                var dict = field.GetValue(null) as Dictionary<HashedString, StatusItem.StatusItemOverlays>;
                if (dict == null)
                {
                    Debug.LogError("overlayBitfieldMap is null or invalid.");
                    return;
                }

                // Inject overlay mapping
                dict[TransitTubeOverlay.ID] = (StatusItem.StatusItemOverlays)Constants.statusItemOverlayBit;
            }
        }

        [HarmonyPatch(typeof(BuildingStatusItems), "CreateStatusItems")]
        public static class BuildingStatusItem_CreateStatusItems_Patch
        {
            public static void Postfix(BuildingStatusItems __instance)
            {

                __instance.NoTubeConnected.render_overlay = TransitTubeOverlay.ID;
                __instance.NoTubeExits.render_overlay = TransitTubeOverlay.ID;
                __instance.TransitTubeEntranceWaxReady.render_overlay = TransitTubeOverlay.ID;
                __instance.NoTubeConnected.status_overlays |= Constants.statusItemOverlayBit;
                __instance.NoTubeExits.status_overlays |= Constants.statusItemOverlayBit;
                __instance.TransitTubeEntranceWaxReady.status_overlays |= Constants.statusItemOverlayBit;
                __instance.Broken.status_overlays |= Constants.statusItemOverlayBit;
                __instance.PendingDeconstruction.status_overlays |= Constants.statusItemOverlayBit;
                __instance.PendingDemolition.status_overlays |= Constants.statusItemOverlayBit;
            }
        }

        [HarmonyPatch(typeof(BuildingStatusItems), "ShowInUtilityOverlay")]
        public static class BuildingStatusItems_ShowInUtilityOverlay_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(
                HashedString mode,
                object data,
                BuildingStatusItems __instance,
                ref bool __result
                )
            {
                if (__result) return;
                if (mode == TransitTubeOverlay.ID)
                {
                    Tag prefabTag = ((Transform)data).GetComponent<KPrefabID>().PrefabTag;
                    __result = TransitTubeOverlay.TargetIDs.Contains(prefabTag);
                }
                return;
            }
        }


        //******************************************************
        //****************************************************** 
        //******************************************************
        //***********        TOOLS UPDATE        ***************
        //****************************************************** 
        //****************************************************** 
        //******************************************************


        /** 
         * 
         */
        [HarmonyPatch(typeof(FilteredDragTool), "OnOverlayChanged")]
        public static class FilteredDragTool_OnOverlayChanged_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        codes[i-1].operand is FieldInfo fieldInfo &&
                        fieldInfo.Name == "LOGIC" &&
                        codes[i].opcode == OpCodes.Stloc_0
                    )
                    {
                        var skipLabel = generator.DefineLabel();
                        var entryPoint = new CodeInstruction(OpCodes.Ldarg_1);
                        codes[i + 1].MoveLabelsTo(entryPoint);

                        result.Add(entryPoint);
                        result.Add( new CodeInstruction(OpCodes.Ldsfld, overlayIdField));
                        result.Add( new CodeInstruction(OpCodes.Call, HashedStringEqualityFn));
                        result.Add( new CodeInstruction(OpCodes.Brfalse_S, skipLabel));
                        result.Add( new CodeInstruction(OpCodes.Ldstr, Constants.FILTERLAYERS.TRAVELTUBE));
                        result.Add( new CodeInstruction(OpCodes.Stloc_0));
                        result.Add( new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel));
                    }
                }
                return result;
            }
        }

        [HarmonyPatch(typeof(FilteredDragTool), "GetFilterLayerFromObjectLayer")]
        public static class FilteredDragToolPatch_GetFilterLayerFromObjectLayer_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ObjectLayer gamer_layer, ref string __result)
            {
                if(gamer_layer == ObjectLayer.TravelTube)
                {
                    __result = Constants.FILTERLAYERS.TRAVELTUBE;
                }
            }
        }

        [HarmonyPatch(typeof(FilteredDragTool), "GetObjectLayerFromFilterLayer")]
        public static class FilteredDragToolPatch_GetObjectLayerFromFilterLayer_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(string filter_layer, ref ObjectLayer __result)
            {
                if (filter_layer == Constants.FILTERLAYERS.TRAVELTUBE)
                {
                    __result = ObjectLayer.TravelTube;
                }
            }
        }

        /**
         * Adds Transit Tube filter toggle
         */
        [HarmonyPatch(typeof(FilteredDragTool), "GetDefaultFilters")]
        public static class FilteredDragTool_GetDefaultFilters_Patch
        {
            [HarmonyPostfix]
            public static void Postfix_GetDefaultFilters(
                Dictionary<string, ToolParameterMenu.ToggleState> filters)
            {
                filters.Add(Constants.FILTERLAYERS.TRAVELTUBE, ToolParameterMenu.ToggleState.Off);
            }
        }


        [HarmonyPatch(typeof(BuildingDef), "IsValidTileLocation")]
        public static class BuildingDef_IsValidTileLocation_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(
                    GameObject source_go,
                    int cell,
                    bool replacement_tile,
                    ref string fail_reason,
                    BuildingDef __instance,
                    ref bool __result
                )
            {

                if(__result == false)
                {
                    return;
                }
                GameObject transitTube = Grid.Objects[cell, (int)ObjectLayer.TravelTube];
                if( 
                    transitTube != null && 
                    transitTube != source_go && 
                    transitTube.GetComponent<Building>().Def.BuildLocationRule is BuildLocationRule buildLocRule &&
                    (
                        buildLocRule == Constants.BuildLocationRules.transitTube ||
                        buildLocRule == Constants.BuildLocationRules.transitTubeBridge ||
                        buildLocRule == Constants.BuildLocationRules.transitTubeEntrance
                    )
                )
                {
                    //TODO MOVE STRING
                    fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                    __result = false;
                    return;
                }
                return;
            }
        }

        public static class BuildingDef_IsAreaClear_Patch
        {
            public static void Postfix(
                GameObject source_go,
                int cell,
                Orientation orientation,
                ObjectLayer layer,
                ObjectLayer tile_layer,
                bool replace_tile,
                bool restrictToActiveWorld,
                ref string fail_reason,
                BuildingDef __instance,
                ref bool __result
                )
            {
                if (__result == false)
                    return;


                MethodInfo isValidTileLocationFn = AccessTools.Method(typeof(BuildingDef), "IsValidTileLocation");
                object[] isValidTileLocationFnParameter = null;

                switch (__instance.BuildLocationRule)
                {
                    //MY BUILDING
                    case Constants.BuildLocationRules.transitTube:
                        if(Grid.Objects[cell, (int)ObjectLayer.Building] != null)
                        {
                            fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                            __result = false;
                            return;
                        }

                        GameObject transitTube = Grid.Objects[cell, (int)ObjectLayer.TravelTube];
                        if(transitTube != null && transitTube != source_go)
                        {
                            fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                            __result = false;
                            return;
                        }
                        if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null)
                        {
                            fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                            __result = false;
                            return;
                        }
                        break;

                    case Constants.BuildLocationRules.transitTubeBridge:

                        isValidTileLocationFnParameter = new object[] { source_go, cell, false, null };
                        if(!(bool)isValidTileLocationFn.Invoke(__instance, isValidTileLocationFnParameter))
                        {
                            fail_reason = (string)isValidTileLocationFnParameter[3];
                            __result = false;
                            return;
                        }
                        if (Grid.Objects[cell, (int)ObjectLayer.Building] != null)
                        {
                            fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                            __result = false;
                            return;
                        }
                        if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null)
                        {
                            fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                            __result = false;
                            return;
                        }
                        break;


                    case Constants.BuildLocationRules.transitTubeEntrance:
                        for(int index = 0; index < __instance.PlacementOffsets.Length; index++)
                        {
                            CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(__instance.PlacementOffsets[index], orientation);
                            int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                            isValidTileLocationFnParameter = new object[] { source_go, cell, true, null };
                            if (!(bool)isValidTileLocationFn.Invoke(__instance, isValidTileLocationFnParameter))
                            {
                                fail_reason = (string)isValidTileLocationFnParameter[3];
                                __result = false;
                                return;
                            }
                            if (Grid.Objects[cell1, (int)ObjectLayer.Building] != null)
                            {
                                fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                                __result = false;
                                return;
                            }
                            if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null)
                            {
                                fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                                __result = false;
                                return;
                            }
                        }
                        break;
                    default:
                        if(layer == ObjectLayer.Building)
                        {
                            for (int index = 0; index < __instance.PlacementOffsets.Length; index++)
                            {
                                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(__instance.PlacementOffsets[index], orientation);
                                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                                if (Grid.Objects[cell1, (int)ObjectLayer.TravelTube] != null)
                                {
                                    fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                                    __result = false;
                                    return;
                                }
                            }
                        }
                        break;
                }

            }
        }
    }
}
