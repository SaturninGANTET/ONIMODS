using HarmonyLib;
using TransitTubeOverlay.Patches;

namespace TransitTubeOverlay.Integration
{
    public static class TravelTubesExpanded
    {
        public static void Patch(Harmony harmony)
        {
            BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, "TravelTubesExpanded.TravelTubeBunkerWallBridgeConfig");
            BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, "TravelTubesExpanded.TravelTubeFirePoleBridgeConfig");
            BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, "TravelTubesExpanded.TravelTubeInsulatedWallBridgeConfig");
            BuildingDef_CreateBuildingDef_PatchHelper.PatchViewMode(harmony, "TravelTubesExpanded.TravelTubeLadderBridgeConfig");

            TransitTubeOverlay.TargetIDs.Add(new Tag("TravelTubeBunkerWallBridge"));
            TransitTubeOverlay.TargetIDs.Add(new Tag("TravelTubeFirePoleBridge"));
            TransitTubeOverlay.TargetIDs.Add(new Tag("TravelTubeInsulatedWallBridge"));
            TransitTubeOverlay.TargetIDs.Add(new Tag("TravelTubeLadderBridge"));

            Debug.Log("[TransitTubeOverlay] TravelTubesExpanded detected -> custom buildings patched");
        }
    }
}
