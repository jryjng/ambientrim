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
using Verse.AI.Group;


namespace ambient
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony inst = new Harmony("rimworld.ambientrim");
            inst.PatchAll();
        }
    }
}
