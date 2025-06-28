namespace TransitTube_Overlay_Mod
{
    public static class CustomStatusItem
    {
        public static StatusItem transitTubeTrueExit = new StatusItem(
            id: "ExitTubeStatus",
            name: "Travel Tube Exit",
            tooltip: "This is a valid exit.",
            icon: "status_item_exclamation",
            icon_type: StatusItem.IconType.Info,
            notification_type: NotificationType.Neutral,
            allow_multiples: false,
            render_overlay: TransitTubeOverlay.ID,
            status_overlays: 0x00080000
        );
    }
}
