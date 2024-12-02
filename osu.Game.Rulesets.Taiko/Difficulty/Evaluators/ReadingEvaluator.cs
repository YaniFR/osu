// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        private const double high_sv_multiplier = 1.0;
        private const double low_sv_multiplier = 1.0;

        /// <summary>
        /// Calculates the influence of higher slider velocities on hitobject difficulty.
        /// The bonus is determined based on the EffectiveBPM, shifting within a defined range
        /// between the upper and lower boundaries to reflect how increased slider velocity impacts difficulty.
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject)
        {
            double effectiveBPM = noteObject.EffectiveBPM;

            const double velocity_max = 640;
            const double velocity_min = 480;

            const double center = (velocity_max + velocity_min) / 2;
            const double range = velocity_max - velocity_min;

            return high_sv_multiplier * DifficultyCalculationUtils.Logistic(effectiveBPM, center, 1.0 / (range / 10));
        }

        public static double LowSV(TaikoDifficultyHitObject noteObject)
        {
            double effectiveBPM = noteObject.EffectiveBPM;
            double bpmCap = 150;
            double effectiveCapBpm = Math.Min(noteObject.EffectiveBPM, bpmCap);
            double low_sv_bonus = Math.Clamp(Math.Pow(Math.Max(0, Math.Abs(effectiveCapBpm-150) / 150),0.5),0,0.57);

            double ObjectDensity = CalculateObjectDensity(noteObject);

            double value = 200 * 1 / effectiveCapBpm - bpmCap*1.33;
            double adjustedValue = (value / effectiveCapBpm* 3) / (1.5 / ObjectDensity);

            return low_sv_multiplier * adjustedValue * (low_sv_bonus*0.9);
        }

        /// <summary>
        /// Calculates the object density based on the DeltaTime, EffectiveBPM, and CurrentSliderVelocity.
        /// </summary>
        /// <param name="noteObject">The current noteObject to evaluate.</param>
        /// <returns>The calculated object density.</returns>
        public static double CalculateObjectDensity(TaikoDifficultyHitObject noteObject)
        {
            double objectDensity = 50 * DifficultyCalculationUtils.Logistic(noteObject.DeltaTime, 200, 1.0 / 300);

            return 1 - DifficultyCalculationUtils.Logistic(noteObject.EffectiveBPM, objectDensity, 1.0 / 240);
        }

    }
}
