namespace TransitTubeOverlay
{
    using HarmonyLib;
    using System.Runtime.CompilerServices;
    public static class TravelTubeExt
    {
        private static readonly ConditionalWeakTable<TravelTube, TravelTubeData> data =
            new ConditionalWeakTable<TravelTube, TravelTubeData>();

        public class TravelTubeData
        {
            public bool isValidExitOnly = false;
        }

        public static bool GetIsValidExitOnly(this TravelTube tube)
        {
            return data.TryGetValue(tube, out var d) && d.isValidExitOnly;
        }

        public static void SetIsValidExitOnly(this TravelTube tube, bool value)
        {
            var d = data.GetOrCreateValue(tube);
            d.isValidExitOnly = value;
        }
    }
}
