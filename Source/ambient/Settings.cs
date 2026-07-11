using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.Noise;
using static UnityEngine.GraphicsBuffer;
using HarmonyLib;
using UnityEngine;
using Verse.AI;
using RimWorld.Planet;
using System.Collections;
using Verse.Sound;
using System.Security.Cryptography;
using System.Reflection.Emit;
using System.Net.NetworkInformation;
using static UnityEngine.TouchScreenKeyboard;
using System.Runtime.Remoting.Messaging;
using ambient;
using System.Security;
using System.Net;


namespace ambient
{
    public class AmbientRim : Mod
    {
        public static ASettings settings;
        public static bool SOUND_DEF_MODIFIED = false;

        public AmbientRim(ModContentPack content) : base(content)
        {
            settings = GetSettings<ASettings>();
        }

        public override string SettingsCategory() => "Ambient Rim";
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.FillRect(inRect);
        }

        public static bool SoundAmpChanged = false;
        private static Dictionary<SubSoundDef, float[]> origVals;

        public static void ApplySoundAmp()
        {
            RecordOrigVals();
            DefDatabase<SoundDef>.AllDefsListForReading.ForEach(def =>
            {
                if (def.defName.StartsWith("AR_"))
                {
                    def.subSounds.ForEach((s) =>
                    {
                        float old = origVals.GetValueSafe(s)[0];
                        float nval = Mathf.Clamp(old + AmbientRim.settings.SoundAmpMod, 0, 200);

                        s.volumeRange = new FloatRange(nval, nval);
                    });
                }
            });
        }

        public static void ApplySoundRangeChange()
        {
            RecordOrigVals();
            DefDatabase<SoundDef>.AllDefsListForReading.ForEach(def =>
            {
                if (def.defName.StartsWith("AR_"))
                {
                    // Verse.Log.Message("[AmbientRim]: Range change " );
                    def.subSounds.ForEach((s) =>
                    {
                        float oldl = origVals.GetValueSafe(s)[2];
                        float nvall = Mathf.Clamp(oldl + AmbientRim.settings.SoundRangeMod, 0, 200);

                        float oldr = origVals.GetValueSafe(s)[3];
                        float nvalr = Mathf.Clamp(oldr + AmbientRim.settings.SoundRangeMod, 1, 200);

                        s.distRange = new FloatRange(nvall, nvalr);
                    });
                }
            });
        }

        public static void RecordOrigVals()
        {
            if (origVals == null)
            {
                origVals = new Dictionary<SubSoundDef, float[]>();
                DefDatabase<SoundDef>.AllDefsListForReading.ForEach(def =>
                {
                    if (def.defName.StartsWith("AR_"))
                    {
                        def.subSounds.ForEach(s =>
                        {
                            origVals.Add(s, new float[] { s.volumeRange.max, s.volumeRange.max, s.distRange.min, s.distRange.max });
                        });
                    }
                });
            }

            SoundAmpChanged = true;
        }

    }

    public class ASettings : ModSettings
    {

        public int WaveType = WaveType_Default;
        const int WaveType_Default = 1;

        public int RiverType = RiverType_Default;
        const int RiverType_Default = 0;

        public bool CampFireSound = CampFireSound_Default;
        const bool CampFireSound_Default = true;

        public bool TorchSound = TorchSound_Default;
        const bool TorchSound_Default = true;

        public int BirdType = BirdType_Default;
        const int BirdType_Default = 0;
        public int MaxBirds = 18;
        public int MaxBirdNight = 5;

        public bool FlySound = FlySound_Default;
        const bool FlySound_Default = true;

        public static string[] WaveSound =
        {
            "AR_Waves",
            "AR_Waves",
        };

        public static string[] RiverSound =
        {
            "AR_River",
            "AR_River2",
        };

        public static string[][] BirdSound =
        {
            // Bad naming scheme for legacy reasons
            new string[]{ "AR_bird" },
        };

        public static string[] owls =
        {
            "AR_owl", "AR_owl2"
        };

        public static string[] seagulls =
        {
            "AR_gull", "AR_gull2"
        };

        public int SoundAmpMod = SoundAmp_Default;
        public const int SoundAmp_Default = 0;

        public int SoundRangeMod = SoundRangeMod_Default;
        public const int SoundRangeMod_Default = 0;

        public void FillRect(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard()
            {
                ColumnWidth = inRect.width/ 1.5f
            };
            list.Begin(inRect);

            list.Gap();
            list.Label("Waves sound");
            if (list.RadioButton("None", WaveType == -1))
            {
                WaveType = -1;
                ReloadSoundAllMaps();
            }
            if (list.RadioButton("Sound 1", WaveType >= 0))
            {
                WaveType = 0;
                ReloadSoundAllMaps();
            }

            list.Gap();

            list.Label("River sound");
            if (list.RadioButton("None", WaveType == -1)){
                RiverType = -1;
                ReloadSoundAllMaps();
            }
            if (list.RadioButton("Sound 1", RiverType == 0))
            {
                RiverType = 0;
                ReloadSoundAllMaps();
            }
            if (list.RadioButton("Sound 2", RiverType == 1))
            {
                RiverType = 1;
                ReloadSoundAllMaps();
            }

            list.Gap();

            list.Label("Bird sound");
            if (list.RadioButton("None", BirdType == -1))
            {
                BirdType = -1;
                ReloadSoundAllMaps();
            }
            if (list.RadioButton("Sound 1", BirdType == 0))
            {
                BirdType = 0;
                ReloadSoundAllMaps();
            }
            list.Gap();

            bool OldFly = FlySound;
            list.CheckboxLabeled("Campfire sound", ref CampFireSound);
            list.CheckboxLabeled("Rotting flies sound", ref FlySound);

            list.Gap();

            list.Label("Volume Change (" + SoundAmpMod + ")");
            int SoundAmpModNew = (int) Math.Round(list.Slider(SoundAmpMod, -50f, 50f), 1);
            if (SoundAmpModNew != SoundAmpMod)
            {
                SoundAmpMod = SoundAmpModNew;
                AmbientRim.ApplySoundAmp();
                ReloadSoundAllMaps();
            }

            list.Label("Sound Range Change (" + SoundRangeMod + ")");
            int SoundRangeModNew = (int)Math.Round(list.Slider(SoundRangeMod, -50f, 50f), 1);
            if (SoundRangeModNew != SoundRangeMod)
            {
                SoundRangeMod = SoundRangeModNew;
                AmbientRim.ApplySoundRangeChange();
                ReloadSoundAllMaps();
            }

            list.End();
            return;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WaveType, nameof(WaveType), WaveType_Default);
            Scribe_Values.Look(ref RiverType, nameof(RiverType), RiverType_Default);
            Scribe_Values.Look(ref CampFireSound, nameof(CampFireSound), CampFireSound_Default);
            Scribe_Values.Look(ref BirdType, nameof(BirdType), BirdType_Default);
            Scribe_Values.Look(ref SoundAmpMod, nameof(SoundAmpMod), SoundAmp_Default);
            Scribe_Values.Look(ref SoundRangeMod, nameof(SoundRangeMod), SoundRangeMod_Default);
            Scribe_Values.Look(ref FlySound, nameof(FlySound), FlySound_Default);
        }

        private static void ReloadSoundAllMaps()
        {
            Find.Maps.ForEach((map) =>
            {
                map.GetComponent<MapSounds>().ReloadSounds();
            });
        }
    }
}
