using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Database;
using HarmonyLib;

namespace TransitTube_Overlay_Mod
{
    public class Patches
    {
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
                if(
                    i >= 1 &&
                    codes[i-1].opcode == OpCodes.Ldc_I4_0 &&
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
            dict[TransitTubeOverlay.ID] = (StatusItem.StatusItemOverlays) Constants.statusItemOverlayBit;
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
        }
    }
}
