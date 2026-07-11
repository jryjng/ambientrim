using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient.Category
{
    internal class RiverViewCategory : AmbientViewCategoryBase
    {

        public RiverViewCategory(Map map) : base(map)
        {
            this.MaxSustainers = 2;
            this.AnchorChangeThresholdTiles = 0;
            
            this.isCached = true;
            this.cacheDistance = 35;
            this.cacheSpawnDistance = 55;
            this.ComputeCache();
        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.RiverType < 0)
            {
                return false;
            }

            return true;
        }

        public override bool TileConditionSuitable(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            var terrain = map.terrainGrid.TerrainAt(cell);
            return terrain != null && terrain.defName.Contains("WaterMoving");
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.RiverType < 0 || AmbientRim.settings.RiverType >= ASettings.RiverSound.Length)
            {
                return null;
            }

            var soundName = ASettings.RiverSound[AmbientRim.settings.RiverType];
            return DefDatabase<SoundDef>.GetNamed(soundName);
        }

        private void ComputeCache()
        {
            // Can be optimized with a sorted data struct but lets not worry about premature opt.
            foreach (var cell in map.AllCells)
            {
                // Valid tile
                if (!map.terrainGrid.TerrainAt(cell).defName.Contains("WaterMoving"))
                {
                    continue;
                }

                // Not too close to previous cells
                if (cachedCells.Any(prev => IntVec3Utility.ManhattanDistanceFlat(prev, cell) <= this.cacheDistance))
                {
                    continue;
                }

                // Not too close to the coastline
                if (GenRadial
                    .RadialCellsAround(cell, 1f, useCenter: true)
                    .Any(item =>
                    item.InBounds(map) &&
                    !map.terrainGrid.TerrainAt(item).defName.ToLower().Contains("water")))
                {
                    continue;
                }

                // Start sound
                cachedCells.Add(cell);
            }
            // Verse.Log.Message("Cache size is " + cachedCells.Count);
        }
    }
}
