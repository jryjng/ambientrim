using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;
using ambient;
using ambient.Category;
using Verse.Sound;
using Verse.Noise;
using RimWorld.Planet;
using System.Net.NetworkInformation;

namespace ambient
{

    // a lot can be cleaned up in this codebase but its decently extensible and readable
    // remove distanceto calculations and use manhattan distance to optimize perf a little bit
    // we are using the camera to create and despawn sustainers because of engine audio limitations

    public class MapSounds : MapComponent
    {

        // Constants
        private readonly int CameraMoveThresholdTiles = 10;
        private readonly int CandidateCellRadius = 35;
        private readonly float UpdateCameraSeconds = 0.2f;


        // Use Camera + Time to determine when to update candidates
        private IntVec3 lastCameraCenterCell = IntVec3.Invalid;
        private float lastCameraUpdateTime = -999f;
        public IntVec3 CameraCenterCell { get; private set; } = IntVec3.Invalid;


        // List of all possible candidate categories
        private readonly List<AmbientViewCategoryBase> viewCategories = new List<AmbientViewCategoryBase>();


        public MapSounds(Map map) : base(map)
        {
            viewCategories.Add(new BirdViewCategory(map));
            viewCategories.Add(new SeagullViewCategory(map));
            viewCategories.Add(new OceanWavesViewCategory(map));
            viewCategories.Add(new RiverViewCategory(map));
            viewCategories.Add(new CorpseFliesViewCategory(map));
            viewCategories.Add(new CampfireTorchViewCategory(map));

            if (!AmbientRim.SOUND_DEF_MODIFIED)
            {
                AmbientRim.ApplySoundAmp();
                AmbientRim.ApplySoundRangeChange();
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Skip update if not on map
            if (Find.CameraDriver == null || Find.CurrentMap != map)
            {
                return;
            }


            var currentCenterCell = Find.CameraDriver.MapPosition;

            // Compute cell distance and time
            var movedTiles = !lastCameraCenterCell.IsValid ? 255 : 
                Math.Abs(currentCenterCell.x - lastCameraCenterCell.x) + Math.Abs(currentCenterCell.z - lastCameraCenterCell.z);
            var elapsedSinceLastUpdate = Time.realtimeSinceStartup - lastCameraUpdateTime;

            // Regardless of factor, clear map condition factors
            if (elapsedSinceLastUpdate >= 10f)
            {
                UpdateMapCondition();
            }

            // Update candidates status
            if (movedTiles >= CameraMoveThresholdTiles && elapsedSinceLastUpdate >= UpdateCameraSeconds)
            {
                lastCameraCenterCell = currentCenterCell;
                lastCameraUpdateTime = Time.realtimeSinceStartup;
                UpdateVisibleView(currentCenterCell);
            }
        }



        /**
         * Stop category if map not suitable anymore
         */
        private void UpdateMapCondition()
        {
            foreach (var sust in viewCategories)
            {
                if (sust.IsActive() && !sust.MapConditionSuitable())
                {
                    sust.StopAll();   
                }
            }
        }



        private void UpdateVisibleView(IntVec3 position)
        {
            CameraCenterCell = position;
            Verse.Log.Message("Center is at " + CameraCenterCell);

            foreach (var category in viewCategories)
            {
                // Always remove if map is not suitable
                if (!category.MapConditionSuitable())
                {
                    category.StopAll();
                    continue;
                }


                // Despawn inaudible anchors
                category.RemoveInaudibleSustainer(CameraCenterCell);

                // This is used as a heuristic to avoid recomputation of existing cells
                if (!category.IsCellOutsideAnchors(CameraCenterCell))
                {
                    continue;
                }

                // If cached, use its cached values
                if (category.isCached)
                {
                    category.UseCache(CameraCenterCell);
                    continue;
                }

                // Not cached - have to scan its own radius
                // Find the first cell that satisfies the tile condition and apply the anchor
                int maxIndex = GenRadial.NumCellsInRadius(CandidateCellRadius);
                for (int i = 0; i < maxIndex; i++)
                {
                    IntVec3 cell = CameraCenterCell + GenRadial.RadialPattern[i];
                    if (!cell.InBounds(map) || !category.TileConditionSuitable(cell))
                    {
                        continue;
                    }

                    category.UpdateAnchor(cell);
                    break;
                }
            }
        }

        public void ReloadSounds()
        {
            foreach (var category in viewCategories)
            {
                category.StopAll();
            }

            lastCameraCenterCell = IntVec3.Invalid;
            lastCameraUpdateTime = -999f;
        }
    }
}
