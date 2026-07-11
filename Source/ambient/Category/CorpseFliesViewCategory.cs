using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient.Category
{
    internal class CorpseFliesViewCategory : AmbientViewCategoryBase
    {
        public CorpseFliesViewCategory(Map map) : base(map)
        {

        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null || !AmbientRim.settings.FlySound)
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

            var things = map.thingGrid.ThingsListAt(cell);
            foreach (var thing in things)
            {
                if (thing is Corpse corpse && corpse.GetRotStage() == RotStage.Rotting)
                {
                    return true;
                }
            }

            return false;
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || !AmbientRim.settings.FlySound)
            {
                return null;
            }

            return DefDatabase<SoundDef>.GetNamed("AR_fly");
        }
    }
}
