using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TransitTube_Overlay_Mod
{
    public class TransitTubeOverlay : OverlayModes.Mode
    {
        private UniformGrid<SaveLoadRoot> partition;
        private HashSet<SaveLoadRoot> layerTargets = new HashSet<SaveLoadRoot>();
        private ICollection<Tag> targetIDs;
        private int overlayLayer;
        private int cameraLayerMask;

        public static readonly HashedString ID = new HashedString("TransitTube");
        public override string GetSoundName() => "SuitRequired";

        public TransitTubeOverlay()
        {
            this.targetIDs = new List<Tag>(){"TravelTube", "TravelTubeEntrance", "TravelTubeWallBridge" };
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "TravelTubesExpanded"))
            {
                //Travel Tubes Expanded Mod Installed - https://github.com/SanchozzDeponianin/ONIMods/tree/master/src/TravelTubesExpanded
                this.targetIDs.Add(new Tag("TravelTubeBunkerWallBridge"));
                this.targetIDs.Add(new Tag("TravelTubeFirePoleBridge"));
                this.targetIDs.Add(new Tag("TravelTubeInsulatedWallBridge"));
                this.targetIDs.Add(new Tag("TravelTubeLadderBridge"));
            }

            this.overlayLayer = LayerMask.NameToLayer("MaskedOverlay");
            this.cameraLayerMask = LayerMask.GetMask("MaskedOverlay");
        }

        public override void Enable()
        {
            RegisterSaveLoadListeners();
            partition = PopulatePartition<SaveLoadRoot>(targetIDs);

            Camera.main.cullingMask |= cameraLayerMask;
            SelectTool.Instance.SetLayerMask(cameraLayerMask);

            GridCompositor.Instance.ToggleMinor(false);
            base.Enable();
        }

        public override void Disable()
        {
            ResetDisplayValues(layerTargets);
            Camera.main.cullingMask &= ~cameraLayerMask;
            SelectTool.Instance.ClearLayerMask();

            UnregisterSaveLoadListeners();
            foreach (var icon in GameObject.FindObjectsOfType<TubeOverlayIcon>())
            {
                icon.SetVisible(false);
            }
            partition?.Clear();
            layerTargets.Clear();

            GridCompositor.Instance.ToggleMinor(false);
            base.Disable();
        }

        protected override void OnSaveLoadRootRegistered(SaveLoadRoot item)
        {
            if (targetIDs.Contains(item.GetComponent<KPrefabID>().GetSaveLoadTag()))
                partition.Add(item);
        }

        protected override void OnSaveLoadRootUnregistered(SaveLoadRoot item)
        {
            if (item == null || item.gameObject == null)
                return;

            layerTargets.Remove(item);
            partition.Remove(item);
        }

        public override void Update()
        {
            Vector2I min, max;
            Grid.GetVisibleExtents(out min, out max);

            RemoveOffscreenTargets(layerTargets, min, max, root =>
            {
                if (root != null)
                {
                    Vector3 pos = root.transform.GetPosition();
                    pos.z = GetDefaultDepth(root);
                    root.transform.SetPosition(pos);
                }
            });

            foreach (SaveLoadRoot root in partition.GetAllIntersecting(min, max))
            {
                if (root == null) continue;
                
                AddTargetIfVisible(root, min, max, layerTargets, overlayLayer);

                var travelTube = root.GetComponent<TravelTube>();
                if(travelTube != null)
                {
                    if (travelTube.GetIsValidExitOnly())
                    {
                        var icon = travelTube.GetComponent<TubeOverlayIcon>();
                        if (icon == null)
                        {
                            icon = travelTube.FindOrAddComponent<TubeOverlayIcon>();
                            icon.SetupOutput();
                        }

                        icon.SetVisible(true);
                    }
                }

                var travelTubeBridge = root.GetComponent<TravelTubeBridge>();
                if(travelTube != null || travelTubeBridge != null)
                {
                    root.GetComponent<KBatchedAnimController>().TintColour = new Color32(0, 255, 255, 255);
                }

                var transitTubeAcess = root.GetComponent<TravelTubeEntrance>();
                if (transitTubeAcess != null)
                {
                    var icon = transitTubeAcess.GetComponent<TubeOverlayIcon>();
                    if(icon == null)
                    {
                        icon = transitTubeAcess.FindOrAddComponent<TubeOverlayIcon>();
                        icon.SetupInput();
                    }
                    icon.SetVisible(true);
                }

            }
        }

        public override HashedString ViewMode() => ID;
    }
}
