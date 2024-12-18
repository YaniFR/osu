// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;


namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public class ColourEvaluator
    {
        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="MonoStreak"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(MonoStreak monoStreak)
        {
            return DifficultyCalculationUtils.Logistic(exponent: Math.E * monoStreak.Index - 2 * Math.E) * EvaluateDifficultyOf(monoStreak.Parent) * 0.5;
        }

        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="AlternatingMonoPattern"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(AlternatingMonoPattern alternatingMonoPattern)
        {
            return DifficultyCalculationUtils.Logistic(exponent: Math.E * alternatingMonoPattern.Index - 2 * Math.E) * EvaluateDifficultyOf(alternatingMonoPattern.Parent);
        }

        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="RepeatingHitPatterns"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(RepeatingHitPatterns repeatingHitPattern)
        {
            return 2 * (1 - DifficultyCalculationUtils.Logistic(exponent: Math.E * repeatingHitPattern.RepetitionInterval - 2 * Math.E));
        }

        /// <summary>
        /// Calculates a consistency penalty based on the number of consecutive consistent intervals, considering the delta time between each colour sequence.
        /// </summary>
        private static double consistentIntervalPenalty(TaikoDifficultyHitObject hitObject, double threshold = 0.01)
        {
            int consistentCount = 0;
            double totalDeltaTime = 0.0;

            TaikoDifficultyHitObject current = hitObject;

            while (current.Previous(1) is TaikoDifficultyHitObject previousHitObject)
            {
                double currentInterval = current.DeltaTime;

                if (previousHitObject.Previous(1) is not TaikoDifficultyHitObject)
                    break;

                double previousInterval = previousHitObject.DeltaTime;

                // If the current interval is consistent, accumulate delta time and increase count within the Margin of Error.
                if (Math.Abs(1 - currentInterval / previousInterval) <= threshold)
                {
                    consistentCount++;
                    totalDeltaTime += currentInterval;
                }

                current = previousHitObject;
            }

            double penaltyScale = Math.Min(consistentCount * 0.01, 0.10);
            double deltaPenalty = Math.Clamp(1 - totalDeltaTime / (consistentCount + 1) * 0.001, 0.85, 1.0);

            double penalty = doubletPenalty(hitObject);

            return (1.0 - Math.Min(penaltyScale, 1 - deltaPenalty)) * penalty;
        }

        private static double doubletPenalty(TaikoDifficultyHitObject hitObject)
        {
            var evenHitObjects = hitObject.Rhythm.EvenHitObjects;
            var evenPatterns = hitObject.Rhythm.EvenPatterns;
            var mono = hitObject.Colour.MonoStreak;

            double penalty = 1.0;
            double doubletCount = 0;
            if (evenHitObjects == null || evenPatterns == null || evenHitObjects.Previous == null || evenHitObjects.Previous.Previous == null || mono == null)
                return 1.0;

            //if (evenPatterns.Children.Count > 1 && hitObject.Colour.MonoStreak?.HitObjects.Count != 2 && evenHitObjects.HitObjectIntervalRatio != evenPatterns.ChildrenInterval)
            if (evenHitObjects.Previous.Children.Count == 2 && evenHitObjects.StartTime - evenHitObjects.Previous.EndTime > 100 && evenHitObjects.Previous.Duration < 55 && evenHitObjects.Children.Count > 1) // && ((evenHitObjects.HitObjectIntervalRatio == evenHitObjects.Previous.Previous.HitObjectIntervalRatio) && evenHitObjects.Previous.HitObjectIntervalRatio < evenHitObjects.HitObjectIntervalRatio))
            //if (evenHitObjects.Previous.Children.Count == 2 && ((evenHitObjects.HitObjectIntervalRatio == evenHitObjects.Previous.Previous.HitObjectIntervalRatio) && evenHitObjects.Previous.HitObjectIntervalRatio > evenHitObjects.HitObjectIntervalRatio))
            {

                for (int i = 0; i < evenHitObjects.Children.Count - 1; i++)
                {
                    double? interval = evenHitObjects.Children[i].HitObjectInterval;
                    double? nextInterval = evenHitObjects.Children[i + 1].HitObjectInterval;
                    if (interval.HasValue && nextInterval.HasValue)
                    {
                        if (Math.Abs(1 - (double)interval / (double)nextInterval) <= 0.01)
                            doubletCount++;
                    }
                }

                double doubletRatio = evenHitObjects.Children.Count > 1 ? doubletCount / (evenHitObjects.Children.Count - 1) : 0.0;
                penalty = DifficultyCalculationUtils.Logistic(doubletRatio, 0.5, 1.5, 1.0);
                return Math.Pow(penalty,  1.2);
                //return 0;
            }

            return 1.0;
        }

        /// <summary>
        /// Evaluate the difficulty of the first hitobject within a colour streak.
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject)
        {
            TaikoDifficultyHitObjectColour colour = ((TaikoDifficultyHitObject)hitObject).Colour;
            //TaikoDifficultyHitObjectRhythm rhythm = ((TaikoDifficultyHitObject)hitObject).Rhythm;
            var taikoObject = (TaikoDifficultyHitObject)hitObject;
            double difficulty = 0.0d;

            if (colour.MonoStreak?.FirstHitObject == hitObject) // Difficulty for MonoStreak
                difficulty += EvaluateDifficultyOf(colour.MonoStreak);

            if (colour.AlternatingMonoPattern?.FirstHitObject == hitObject) // Difficulty for AlternatingMonoPattern
                difficulty += EvaluateDifficultyOf(colour.AlternatingMonoPattern);

            if (colour.RepeatingHitPattern?.FirstHitObject == hitObject) // Difficulty for RepeatingHitPattern
                difficulty += EvaluateDifficultyOf(colour.RepeatingHitPattern);
            double dz = 1;
            if (taikoObject.Rhythm.EvenHitObjects?.FirstHitObject == hitObject && taikoObject.Rhythm.EvenPatterns?.FirstHitObject == hitObject)
                dz = doubletPenalty((TaikoDifficultyHitObject)hitObject);   

            double consistencyPenalty = consistentIntervalPenalty(taikoObject);
            difficulty *= consistencyPenalty;

            return difficulty;
        }
    }
}
