using HarmonyLib;
using KMod;
using System;
using System.Collections.Generic;
using System.IO;
using TransitTubeOverlay.Integration;
using static TransitTubeOverlay.Patches.OverlayPatches;

namespace TransitTubeOverlay.Patches
{

    public class TransiteTubeOverlayMod : UserMod2
    {

        public override void OnLoad(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            LocString.CreateLocStringKeys(typeof(STRINGS), null);

            base.OnLoad(harmony);
            BuildingConfig_CreateBuildingDef_Patch.PatchAll(harmony);
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            if (Utils.isModLoaded("TravelTubesExpanded"))
            {
                TravelTubesExpanded.Patch(harmony);
            }
        }

    }
}
