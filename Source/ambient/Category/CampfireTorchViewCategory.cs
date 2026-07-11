using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ambient.Category
{
    internal class CampfireTorchViewCategory : AmbientViewCategoryBase
    {
        public CampfireTorchViewCategory(Map map) : base(map)
        {

        }

        public override bool MapConditionSuitable()
        {
            if (AmbientRim.settings == null)
            {
                return false;
            }

            return AmbientRim.settings.CampFireSound || AmbientRim.settings.TorchSound;
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
                if (thing is Building building)
                {
                    if ((building.def.defName.Equals("Campfire") && AmbientRim.settings.CampFireSound) ||
                        (building.def.defName.Equals("Brazier") && AmbientRim.settings.CampFireSound))
                    {
                        var compRefuelable = building.GetComp<CompRefuelable>();
                        if (compRefuelable != null && compRefuelable.HasFuel)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override SoundDef GetSoundDef()
        {
            if (AmbientRim.settings == null || !AmbientRim.settings.CampFireSound)
            {
                return null;
            }
            return DefDatabase<SoundDef>.GetNamed("AR_FireBurning");
        }
    }
}
