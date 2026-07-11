using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace ambient
{
    internal class SustainerCell
    {
        public Sustainer sustainer;
        public IntVec3 cell;

        public SustainerCell(Sustainer sustainer, IntVec3 cell)
        {
            this.sustainer = sustainer;
            this.cell = cell;
        }
    }
}
