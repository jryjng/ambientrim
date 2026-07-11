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
using Verse.Sound;
using Verse.Noise;
using RimWorld.Planet;
using System.Net.NetworkInformation;

namespace ambient
{
    public abstract class AmbientViewCategoryBase
    {

        protected readonly Map map;

        // This provides specifity on when to switch to another 'closer' cell
        public int AnchorChangeThresholdTiles = 20;

        // Number of allowed sustainers
        protected int MaxSustainers = 1;

        // Cache
        public bool isCached;
        protected HashSet<IntVec3> cachedCells = new HashSet<IntVec3>();
        protected int cacheDistance;
        protected int cacheSpawnDistance = 40;

        // List of sustainers in use
        internal LinkedList<SustainerCell> activeSustainers = new LinkedList<SustainerCell>();

        protected AmbientViewCategoryBase(Map map)
        {
            this.map = map;
        }


        /** Abstract **/
        public abstract bool MapConditionSuitable();
        public abstract bool TileConditionSuitable(IntVec3 cell);
        public abstract SoundDef GetSoundDef();


        /**
         * 
         * Move the single audio channel to a particular cell, ending the previous sustainer if present
         * 
         */
        public void UpdateAnchor(IntVec3 cell)
        {
            // Verse.Log.Message($"Method called by subclass: {this.GetType().Name}");
            AddSustainer(cell);
        }

        protected void AddSustainer(IntVec3 cell)
        {
            SoundDef soundDef = GetSoundDef();
            activeSustainers.AddFirst(new SustainerCell(SoundStarter.TrySpawnSustainer(soundDef, new TargetInfo(cell, map)), cell));
            if (activeSustainers.Count > MaxSustainers)
            {
                RemoveSustainer();
            }
        }


        protected bool RemoveSustainer()
        {
            if (activeSustainers.Count > 0)
            {
                activeSustainers.Last().sustainer.End();
                activeSustainers.RemoveLast();
                return true;
            }
            return false;
        }


        public void StopAll()
        {
            while (RemoveSustainer()) ;
        }


        public bool IsActive()
        {
            return activeSustainers.Count > 0;
        }


        public void RemoveInaudibleSustainer(IntVec3 listenerCell)
        {
            for (int i = activeSustainers.Count - 1; i >= 0; --i)
            {
                SustainerCell sc = activeSustainers.ElementAt(i);
                if (listenerCell.DistanceTo(sc.cell) > sc.sustainer.def.subSounds.ElementAt(0).distRange.max)
                {
                    // Verse.Log.Message("Removing inaudible element " + sc.cell);
                    sc.sustainer.End();
                    activeSustainers.Remove(sc);
                }
            }
        }


        // This provides specifity on when to switch to another 'closer' cell
        // Use the instance variable AnchorChangeThresholdTiles, which should be set by a subclass
        public bool IsCellOutsideAnchors(IntVec3 listenerCell)
        {
            foreach (SustainerCell sc in activeSustainers)
            {
                if (sc.cell.DistanceTo(listenerCell) < AnchorChangeThresholdTiles)
                {
                    return false;
                }
            }
            return true;
        }

        public void UseCache(IntVec3 center)
        {
            List<IntVec3> minCells = GetMinValidCells(cachedCells, center, MaxSustainers, 60);
            HashSet<IntVec3> targetCells = new HashSet<IntVec3>(minCells);
            var currentNode = activeSustainers.First;
            while (currentNode != null)
            {
                var nextNode = currentNode.Next; 
                if (!targetCells.Contains(currentNode.Value.cell))
                {
                    // Verse.Log.Message("Removing cache " + currentNode.Value.cell);
                    currentNode.Value.sustainer.End();
                    activeSustainers.Remove(currentNode);
                } else
                {
                    targetCells.Remove(currentNode.Value.cell);
                }
                currentNode = nextNode;
            }

            foreach (var cell in targetCells)
            {
                // Verse.Log.Message("Adding cache " + cell);
                AddSustainer(cell);
            }
            // activeSustainers.ToList().ForEach((a) => Verse.Log.Message(a.cell));
        }


        /* AI Generated code to get cells closest to target, while also adding directional bias such that the same direction gets penalized */
        // Simple helper class to bundle data together during the process
        private class CellScoreData
        {
            public IntVec3 Cell;
            public int RawDistSq;
            public Vector3 Direction;
        }

        protected List<IntVec3> GetMinValidCells(HashSet<IntVec3> cells, IntVec3 center, int size, int maxDist, float biasStrength = 100f)
        {
            // 1. Handle base edge cases immediately
            if (cells == null || cells.Count == 0 || size <= 0)
            {
                return new List<IntVec3>();
            }

            // 2. Filter out invalid cells and pre-calculate raw distances to avoid doing it repeatedly
            List<CellScoreData> candidates = new List<CellScoreData>();
            int maxDistSq = maxDist * maxDist;

            foreach (var cell in cells)
            {
                if (TileConditionSuitable(cell))
                {
                    int distSq = (cell.x - center.x) * (cell.x - center.x) +
                                 (cell.z - center.z) * (cell.z - center.z);

                    if (distSq < maxDistSq)
                    {
                        // Calculate its normalized direction from center right away
                        Vector3 dir = new Vector3(cell.x - center.x, cell.y - center.y, cell.z - center.z);
                        if (dir.sqrMagnitude > 0) dir.Normalize();

                        candidates.Add(new CellScoreData { Cell = cell, RawDistSq = distSq, Direction = dir });
                    }
                }
            }

            if (candidates.Count == 0) return new List<IntVec3>();

            List<IntVec3> chosenCells = new List<IntVec3>();
            List<Vector3> forbiddenDirections = new List<Vector3>();

            // 3. Iteratively pick the best cell 'size' times
            int countToTake = Math.Min(size, candidates.Count);
            for (int i = 0; i < countToTake; i++)
            {
                CellScoreData bestCandidate = null;
                float bestScore = float.MaxValue;
                int bestIndex = -1;

                for (int j = 0; j < candidates.Count; j++)
                {
                    var candidate = candidates[j];

                    // Calculate direction penalty based on ALREADY chosen cells
                    float directionalPenalty = 0f;
                    foreach (var forbiddenDir in forbiddenDirections)
                    {
                        float dot = Vector3.Dot(candidate.Direction, forbiddenDir);
                        if (dot > 0) // Only penalize if it's heading in a similar direction (0 to 1)
                        {
                            directionalPenalty += dot * biasStrength;
                        }
                    }

                    // Total Score = Distance + Penalty
                    float currentScore = candidate.RawDistSq + directionalPenalty;

                    if (currentScore < bestScore)
                    {
                        bestScore = currentScore;
                        bestCandidate = candidate;
                        bestIndex = j;
                    }
                }

                // Add the winner to our final list
                chosenCells.Add(bestCandidate.Cell);

                // Remember this direction so the NEXT iterations penalize cells near it
                forbiddenDirections.Add(bestCandidate.Direction);

                // Remove the chosen cell from candidates so we don't pick it twice
                candidates.RemoveAt(bestIndex);
            }

            return chosenCells;
        }




        // Cached local map condition
        bool isCoastal = false;
        bool isCoastalComputed = false;
        public bool IsMapCoastal()
        {
            if (isCoastalComputed)
            {
                return isCoastal;
            }
            isCoastalComputed = true;

            // Check landmarks instead
            foreach (var cell in map.AllCells)
            {
                if (map.terrainGrid.TerrainAt(cell).defName.Equals("WaterOceanShallow"))
                {
                    isCoastal = true;
                    return isCoastal;
                }
            }

            isCoastal = false;
            return isCoastal;
        }
    }
}
