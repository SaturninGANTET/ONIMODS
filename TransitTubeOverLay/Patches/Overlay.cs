using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TransitTubeOverlay.Patches;
using UnityEngine;

namespace TransitTubeOverlay.Patches
{
    public class OverlayPatches
    {
        public static class BuildingConfig_CreateBuildingDef_Patch
        {
            /// <summary>
            /// Applies the ViewMode patch to all travel tube building configs.
            /// </summary>
            public static void PatchAll(Harmony harmony)
            {
                BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, typeof(TravelTubeConfig));
                BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, typeof(TravelTubeWallBridgeConfig));
                BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, typeof(TravelTubeEntranceConfig));
            }
        }

        /*
         * Registers assets used for the overlay
         */
        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        public static class TransitTubeOverlayMod_AssetsInit
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                Utils.RegisterEmbeddedIcon(
                    CONSTANTS.RESSOURCESNAME.TransitTubeOverlayToggle,
                    CONSTANTS.ASSETSNAME.TransitTubeOverlayToggle
                );
                Utils.RegisterEmbeddedIcon(
                    CONSTANTS.RESSOURCESNAME.TransitTubeInput,
                    CONSTANTS.ASSETSNAME.TransitTubeInput
                );
                Utils.RegisterEmbeddedIcon(
                    CONSTANTS.RESSOURCESNAME.TransitTubeOutput,
                    CONSTANTS.ASSETSNAME.TransitTubeOutput
                );
            }
        }


        [HarmonyPatch(typeof(OverlayMenu), "InitializeToggles")]
        public class InitalizeToggles_Patch
        {
            [HarmonyPostfix]
            public static void AddTransitTubeOverlayToggleButton(List<KIconToggleMenu.ToggleInfo> ___overlayToggleInfos)
            {
                Type overlayToggleInfoType = typeof(OverlayMenu).GetNestedType("OverlayToggleInfo", BindingFlags.NonPublic);
                if (overlayToggleInfoType == null)
                {
                    Debug.LogError("[TransitTubeOverlay] Couldn't find class OverlayToggleInfo.");
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
                    Strings.Get("STRINGS.UI.OVERLAYS.TRANSITTUBE.NAME").ToString(),     // text -- unused as far as i know
                    CONSTANTS.ASSETSNAME.TransitTubeOverlayToggle,                      // icon_name
                    TransitTubeOverlay.ID,                                              // sim_view
                    "TravelTube",                                                       // required_tech_item
                    Action.NumActions,                                                  // hotKey
                    Strings.Get("STRINGS.UI.OVERLAYS.TRANSITTUBE.TOOLTIP").ToString(),  // tooltip
                    Strings.Get("STRINGS.UI.OVERLAYS.TRANSITTUBE.NAME").ToString(),     // tooltip_header
                });

                if (newToggle is KIconToggleMenu.ToggleInfo toggleInfo)
                {
                    ___overlayToggleInfos.Add(toggleInfo);
                }
                else
                {
                    Debug.LogError("[TransitTubeOverlay] Couldn't cast to toggleInfo.");
                }
            }
        }


        [HarmonyPatch(typeof(OverlayScreen), "RegisterModes")]
        public static class OverlayScreen_RegisterModes_Patch
        {
            [HarmonyPostfix]
            public static void RegisterTransitTubeOverlay(OverlayScreen __instance)
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
         * Finds the real exits (no bridge) to mark them in overlay
         */
        [HarmonyPatch(typeof(TravelTube), "OnDirtyNavCellUpdated")]
        public static class TravelTube_OnDirtyNavCellUpdated_Patch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> isRealExitUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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


                    //Set Flag when Transition Tube -> NoTube is available
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

                    //Update custom field
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
         * Register which bit enable StatusItem display in our overlay
         */
        [HarmonyPatch(typeof(StatusItem), nameof(StatusItem.GetStatusItemOverlayBySimViewMode))]
        public static class StatusItem_GetStatusItemOverlayBySimViewMode_Patch
        {
            static bool patched = false;

            [HarmonyPrefix]
            public static void RegisterBit()
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
                dict[TransitTubeOverlay.ID] = (StatusItem.StatusItemOverlays)CONSTANTS.statusItemOverlayBit;
            }
        }

        [HarmonyPatch(typeof(BuildingStatusItems), "CreateStatusItems")]
        public static class BuildingStatusItem_CreateStatusItems_Patch
        {
            [HarmonyPostfix]
            public static void EnableDisplayInTransitTubeOverlay(BuildingStatusItems __instance)
            {
                __instance.NoTubeConnected.render_overlay = TransitTubeOverlay.ID;
                __instance.NoTubeExits.render_overlay = TransitTubeOverlay.ID;
                __instance.TransitTubeEntranceWaxReady.render_overlay = TransitTubeOverlay.ID;
                __instance.NoTubeConnected.status_overlays |= CONSTANTS.statusItemOverlayBit;
                __instance.NoTubeExits.status_overlays |= CONSTANTS.statusItemOverlayBit;
                __instance.TransitTubeEntranceWaxReady.status_overlays |= CONSTANTS.statusItemOverlayBit;
                __instance.Broken.status_overlays |= CONSTANTS.statusItemOverlayBit;
                __instance.PendingDeconstruction.status_overlays |= CONSTANTS.statusItemOverlayBit;
                __instance.PendingDemolition.status_overlays |= CONSTANTS.statusItemOverlayBit;
            }
        }

        [HarmonyPatch(typeof(BuildingStatusItems), "ShowInUtilityOverlay")]
        public static class BuildingStatusItems_ShowInUtilityOverlay_Patch
        {
            [HarmonyPostfix]
            public static void AddTransitTubeOverlayCheck(
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
    }
}
