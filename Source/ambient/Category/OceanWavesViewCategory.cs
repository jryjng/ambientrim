using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient.Category
{
    internal class OceanWavesViewCategory : AmbientViewCategoryBase
    {
        public OceanWavesViewCategory(Map map) : base(map)
        {
            this.isCached = true;
            this.MaxSustainers = 2;

            this.cacheDistance = 40;
            this.AnchorChangeThresholdTiles = 0;
            this.ComputeCache();
        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.WaveType < 0)
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
            return terrain != null && terrain.defName.Equals("WaterOceanShallow");
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.WaveType < 0 || AmbientRim.settings.WaveType >= ASettings.WaveSound.Length)
            {
                return null;
            }

            var soundName = ASettings.WaveSound[AmbientRim.settings.WaveType];
            return DefDatabase<SoundDef>.GetNamed(soundName);
        }

        private void ComputeCache()
        {
            foreach (var cell in map.AllCells)
            {
                // Valid tile
                if (!map.terrainGrid.TerrainAt(cell).defName.Equals("WaterOceanShallow"))
                {
                    continue;
                }

                // Not too close to previous cells
                if (cachedCells.Any(prev => IntVec3Utility.DistanceTo(prev, cell) <= this.cacheDistance))
                {
                    continue;
                }


                // Not too close to the coastline
                if (GenRadial
                    .RadialCellsAround(cell, 3f, useCenter: true)
                    .Any(item =>
                    item.InBounds(map) &&
                    map.terrainGrid.TerrainAt(item).defName.ToLower().Contains("sand")))
                {
                    continue;
                }

                // Start sound
                cachedCells.Add(cell);
            }
        }
    }
}
