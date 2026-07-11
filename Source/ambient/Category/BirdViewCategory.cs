using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient
{
    public class BirdViewCategory : AmbientViewCategoryBase
    {

        private HashSet<IntVec3> SandRadiusCache;

        public BirdViewCategory(Map map) : base(map)
        {
            this.MaxSustainers = 1;
            this.AnchorChangeThresholdTiles = 20;
            SandRadiusCache = CacheCellsNearOcean(45);
        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.BirdType < 0)
            {
                return false;
            }

            return GenCelestial.IsDaytime(GenCelestial.CurCelestialSunGlow(this.map)) &&
                map.mapTemperature.OutdoorTemp >= 0 &&
                map.mapTemperature.OutdoorTemp <= 50 &&
                map.weatherManager.RainRate == 0 &&
                map.gameConditionManager.ActiveConditions.All(gc =>
                    gc.def != GameConditionDefOf.Eclipse &&
                    gc.def != GameConditionDefOf.NoxiousHaze &&
                    gc.def != GameConditionDefOf.ToxicFallout &&
                    gc.def != GameConditionDefOf.UnnaturalDarkness);
        }

        public override bool TileConditionSuitable(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (SandRadiusCache.Contains(cell)){
                return false;
            }

            var plant = map.thingGrid.ThingAt(cell, ThingCategory.Plant);
            if (plant == null)
            {
                return false;
            }

            if (!plant.def.defName.ToLower().Contains("tree"))
            {
                return false;
            }

            if (plant.def.defName.Contains("Palm"))
            {
                return false;
            }

            if (map.pollutionGrid.IsPolluted(plant.InteractionCell))
            {
                return false;
            }

            return true;
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.BirdType < 0 || AmbientRim.settings.BirdType >= ASettings.BirdSound.Length)
            {
                return null;
            }

            var birdSoundNames = ASettings.BirdSound[AmbientRim.settings.BirdType];
            if (birdSoundNames == null || birdSoundNames.Length == 0)
            {
                return null;
            }

            var soundName = birdSoundNames.RandomElement();
            return DefDatabase<SoundDef>.GetNamed(soundName);
        }



        private HashSet<IntVec3> CacheCellsNearOcean(int distance)
        {
            HashSet<IntVec3> resultCache = new HashSet<IntVec3>();
            Queue<IntVec3> queue = new Queue<IntVec3>();

            // Dictionary/HashSet to track the distance of queued items
            Dictionary<IntVec3, int> distanceMap = new Dictionary<IntVec3, int>();

            TerrainGrid terrainGrid = map.terrainGrid;
            CellIndices cellIndices = map.cellIndices;

            // 1. Grab only the initial sand cells
            foreach (IntVec3 cell in map.AllCells)
            {
                TerrainDef terrain = terrainGrid.TerrainAt(cell);

                // Change "Sand" to whatever specific sand TerrainDef you are targeting
                if (terrain != null && terrain.defName.Contains("Ocean"))
                {
                    queue.Enqueue(cell);
                    distanceMap[cell] = 0;
                    resultCache.Add(cell);
                }
            }

            // 2. Flood-fill outward up to 30 tiles
            while (queue.Count > 0)
            {
                IntVec3 current = queue.Dequeue();
                int currentDist = distanceMap[current];

                if (currentDist >= distance) continue;

                // Check 4 cardinal directions (or use GenAdj.AdjacentCellsAndInside for 8-way)
                for (int i = 0; i < 4; i++)
                {
                    IntVec3 neighbor = current + GenAdj.CardinalDirections[i];

                    // Ensure the neighbor is within map boundaries and hasn't been cached yet
                    if (neighbor.InBounds(map) && !resultCache.Contains(neighbor))
                    {
                        resultCache.Add(neighbor);
                        distanceMap[neighbor] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return resultCache;
        }
    }
}
