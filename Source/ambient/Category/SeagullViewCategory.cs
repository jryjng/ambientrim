using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient.Category
{
    internal class SeagullViewCategory : AmbientViewCategoryBase
    {
        public SeagullViewCategory(Map map) : base(map)
        {
            this.isCached = true;
            this.cacheDistance = 30;
            ComputeCache();
        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.BirdType < 0)
            {
                return false;
            }


            return GenCelestial.IsDaytime(GenCelestial.CurCelestialSunGlow(this.map)) &&
                this.IsMapCoastal() && 
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

            var terrain = map.terrainGrid.TerrainAt(cell);
            if (terrain == null || !terrain.defName.ToLower().Contains("sand"))
            {
                return false;
            }

            if (cell.Roofed(map))
            {
                return false;
            }

            if (map.pollutionGrid.IsPolluted(cell))
            {
                return false;
            }

            return true;
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || AmbientRim.settings.BirdType < 0)
            {
                return null;
            }

            var birdSoundNames = ASettings.seagulls;
            var soundName = birdSoundNames.RandomElement();
            return DefDatabase<SoundDef>.GetNamed(soundName);
        }

        private void ComputeCache()
        {
            foreach (var item in map.AllCells.InRandomOrder())
            {
                var floor = map.terrainGrid.TerrainAt(item);
                if (floor == TerrainDefOf.WaterOceanShallow)
                {
                    // Find adjacent sand tile
                    foreach (var cell in GenRadial.RadialCellsAround(item, 8f, useCenter: true).InRandomOrder())
                    {
                        // Not too close to previous cells
                        if (cachedCells.Any(prev => IntVec3Utility.DistanceTo(prev, item) <= cacheDistance))
                        {
                            continue;
                        }

                        if (cell.InBounds(map) && map.terrainGrid.TerrainAt(cell).Equals(TerrainDefOf.Sand))
                        {
                            cachedCells.Add(cell);
                        }
                    }
                }
            }
        }
    }
}
