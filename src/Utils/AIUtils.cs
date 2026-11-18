using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;

using BDArmory.Extensions;
using BDArmory.Settings;
using BDArmory.UI;

namespace BDArmory.Utils
{
    public static class AIUtils
    {
        public enum UseAccel {None, Immediate, Smoothed};
        /// <summary>
        /// Predict a future position of a vessel given its current position, velocity and acceleration
        /// </summary>
        /// <param name="v">vessel to be extrapolated</param>
        /// <param name="time">after this time</param>
        /// <returns>Vector3 extrapolated position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PredictPosition(this Vessel v, float time, UseAccel immediate=UseAccel.Immediate)
        {
            var vel = v.Velocity();
            var acc = immediate switch
            {
                UseAccel.None => Vector3d.zero,
                UseAccel.Smoothed => v.acceleration,
                _ => v.acceleration_immediate
            };
            var time2 = 0.5f * time * time;
            return new Vector3(
                (float)(v.CoM.x + time * vel.x + time2 * acc.x),
                (float)(v.CoM.y + time * vel.y + time2 * acc.y),
                (float)(v.CoM.z + time * vel.z + time2 * acc.z)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PredictPosition(Vector3 position, Vector3 velocity, Vector3 acceleration, float time)
        {
            float time2 = 0.5f * time * time;
            return new Vector3(
                position.x + time * velocity.x + time2 * acceleration.x,
                position.y + time * velocity.y + time2 * acceleration.y,
                position.z + time * velocity.z + time2 * acceleration.z);
            //return position + time * velocity + 0.5f * time * time * acceleration;
        }

        public enum CPAType
        {
            Earliest, // The earliest future CPA solution.
            Latest, // The latest future CPA solution (even if beyond the max time).
            Closest // The closest CPA solution within the range 0 — max time.
        };
        /// <summary>
        /// Predict the next time to the closest point of approach within the next maxTime seconds using the same kinematics as PredictPosition (i.e, position, velocity and acceleration).
        /// </summary>
        /// <param name="vessel">The first vessel.</param>
        /// <param name="v">The second vessel.</param>
        /// <param name="maxTime">The maximum time to look ahead.</param>
        /// <param name="cpaType">When multiple valid solutions exist, return the one of the given type.</param>
        /// <returns>float The time to the closest point of approach within the next maxTime seconds.</returns>
        public static float TimeToCPA(this Vessel vessel, Vessel v, float maxTime = float.MaxValue, CPAType cpaType = CPAType.Earliest)
        { // Find the closest/furthest future time to closest point of approach considering accelerations in addition to velocities. This uses the generalisation of Cardano's solution to finding roots of cubics to find where the derivative of the separation is a minimum.
            if (vessel == null) return 0f; // We don't have a vessel.
            if (v == null) return 0f; // We don't have a target.
            return vessel.TimeToCPA(v.transform.position, v.Velocity(), v.acceleration, maxTime, cpaType);
        }

        /// <summary>
        /// Predict the next time to the closest point of approach within the next maxTime seconds using the same kinematics as PredictPosition (i.e, position, velocity and acceleration).
        /// </summary>
        /// <param name="vessel">The first vessel.</param>
        /// <param name="targetPosition">The second vessel position.</param>
        /// <param name="targetVelocity">The second vessel velocity.</param>
        /// <param name="targetAcceleration">The second vessel acceleration.</param>
        /// <param name="maxTime">The maximum time to look ahead.</param>
        /// <param name="cpaType">When multiple valid solutions exist, return the one of the given type.</param>
        /// <returns>
        public static float TimeToCPA(this Vessel vessel, Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetAcceleration, float maxTime = float.MaxValue, CPAType cpaType = CPAType.Earliest)
        {
            if (vessel == null) return 0f; // We don't have a vessel.
            Vector3 relPosition = targetPosition - vessel.CoM;
            Vector3 relVelocity = targetVelocity - vessel.Velocity();
            Vector3 relAcceleration = targetAcceleration - vessel.acceleration;
            return TimeToCPA(relPosition, relVelocity, relAcceleration, maxTime, cpaType);
        }

        /// <summary>
        /// Predict the time to the closest point of approach within the next maxTime seconds using the relative position, velocity and acceleration.
        /// </summary>
        /// <param name="relPosition">The relative separation.</param>
        /// <param name="relVelocity">The relative velocity.</param>
        /// <param name="relAcceleration">The relative acceleration.</param>
        /// <param name="maxTime">The maximum time to look ahead.</param>
        /// <param name="cpaType">When multiple valid solutions exist, return the one of the given type.</param>
        /// <returns></returns>
        public static float TimeToCPA(Vector3 relPosition, Vector3 relVelocity, Vector3 relAcceleration, float maxTime = float.MaxValue, CPAType cpaType = CPAType.Earliest)
        {
            float a = Vector3.Dot(relAcceleration, relAcceleration);
            float c = Vector3.Dot(relVelocity, relVelocity);
            if (a == 0 || a * maxTime < 1e-3f * c) // Not actually a cubic. Relative acceleration is zero or insignificant within the time limit, so return the much simpler linear timeToCPA.
            {
                if (c > 0)
                    return Mathf.Clamp(-Vector3.Dot(relPosition, relVelocity) / relVelocity.sqrMagnitude, 0f, maxTime);
                else
                    return 0; // The objects are static, so they're not going to get any closer.
            }

            float A = a / 2f;
            float B = Vector3.Dot(relVelocity, relAcceleration) * 3f / 2f;
            float C = c + Vector3.Dot(relPosition, relAcceleration);
            float D = Vector3.Dot(relPosition, relVelocity);
            float D0 = B * B - 3f * A * C;
            float D1 = 2f * B * B * B - 9f * A * B * C + 27f * A * A * D;
            float E = D1 * D1 - 4f * D0 * D0 * D0; // = -27*A^2*discriminant
            // float discriminant = 18f * A * B * C * D - 4f * Mathf.Pow(B, 3f) * D + Mathf.Pow(B, 2f) * Mathf.Pow(C, 2f) - 4f * A * Mathf.Pow(C, 3f) - 27f * Mathf.Pow(A, 2f) * Mathf.Pow(D, 2f);
            if (E > 0)
            { // Single solution (E is positive)
                float F = (D1 + Mathf.Sign(D1) * BDAMath.Sqrt(E)) / 2f;
                float G = Mathf.Sign(F) * Mathf.Pow(Mathf.Abs(F), 1f / 3f);
                float time = -1f / 3f / A * (B + G + D0 / G);
                return Mathf.Clamp(time, 0f, maxTime);
            }
            else if (E < 0)
            { // Triple solution (E is negative)
                float F_real = D1 / 2f;
                float F_imag = Mathf.Sign(D1) * BDAMath.Sqrt(-E) / 2f;
                float F_abs = BDAMath.Sqrt(F_real * F_real + F_imag * F_imag);
                float F_ang = Mathf.Atan2(F_imag, F_real);
                float G_abs = Mathf.Pow(F_abs, 1f / 3f);
                float G_ang = F_ang / 3f;
                float time = -1f;
                float distanceSqr = float.MaxValue;
                for (int i = 0; i < 3; ++i)
                {
                    float G = G_abs * Mathf.Cos(G_ang + 2f * (float)i * Mathf.PI / 3f);
                    float t = -1f / 3f / A * (B + G + D0 * G / G_abs / G_abs);
                    if (Mathf.Sign(C + 2f * t * B + 3f * t * t * A) > 0) // It's a minimum. There can be at most 2 minima and 1 maxima.
                    {
                        switch (cpaType)
                        {
                            case CPAType.Earliest:
                                if (t > 0 && (time < 0 || t < time)) time = t;
                                break;
                            case CPAType.Latest:
                                if (t > time) time = t;
                                break;
                            case CPAType.Closest:
                                t = Mathf.Clamp(t, 0, maxTime);
                                var distSqr = (relPosition + t * relVelocity + t * t / 2f * relAcceleration).sqrMagnitude;
                                if (distSqr < distanceSqr)
                                {
                                    distanceSqr = distSqr;
                                    time = t;
                                }
                                break;
                        }
                    }
                }
                return Mathf.Clamp(time, 0f, maxTime);
            }
            else
            { // Repeated root
                if (Mathf.Abs(D0) == 0f)
                { // A triple-root.
                    return Mathf.Clamp(-B / 3f / A, 0f, maxTime);
                }
                else
                { // Double root and simple root.
                    float time = -1f;
                    float t0 = (9f * A * D - B * C) / 2f / D0;
                    float t1 = (4f * A * B * C - 9f * A * A * D - B * B * B) / A / D0;
                    switch (cpaType)
                    {
                        case CPAType.Earliest:
                            if (t0 > 0 && (time < 0 || t0 < time)) time = t0;
                            if (t1 > 0 && (time < 0 || t1 < time)) time = t1;
                            break;
                        case CPAType.Latest:
                            if (t0 > time) time = t0;
                            if (t1 > time) time = t1;
                            break;
                        case CPAType.Closest:
                            t0 = Mathf.Clamp(t0, 0, maxTime);
                            t1 = Mathf.Clamp(t1, 0, maxTime);
                            time = ((relPosition + t0 * relVelocity + t0 * t0 / 2f * relAcceleration).sqrMagnitude < (relPosition + t1 * relVelocity + t1 * t1 / 2f * relAcceleration).sqrMagnitude) ? t0 : t1;
                            break;
                    }
                    return Mathf.Clamp(time, 0, maxTime);
                }
            }
        }

        public static float PredictClosestApproachSqrSeparation(this Vessel vessel, Vessel otherVessel, float maxTime)
        {
            var timeToCPA = vessel.TimeToCPA(otherVessel, maxTime);
            if (timeToCPA > 0 && timeToCPA < maxTime)
                return (vessel.PredictPosition(timeToCPA) - otherVessel.PredictPosition(timeToCPA)).sqrMagnitude;
            else
                return float.MaxValue;
        }

        /// <summary>
        /// Get the altitude of terrain below/above a point.
        /// </summary>
        /// <param name="position">World position, not geo position (use VectorUtils.GetWorldSurfacePostion to convert lat,long,alt to world position)</param>
        /// <param name="body">usually vessel.MainBody</param>
        /// <returns>terrain height</returns>
        public static float GetTerrainAltitude(Vector3 position, CelestialBody body, bool underwater = true)
        {
            return (float)body.TerrainAltitude(body.GetLatitude(position), body.GetLongitude(position), underwater);
        }

        public static Vessel VesselClosestTo(Vector3 position, bool useGeoCoords = false)
        {
            Vessel closestV = null;
            float closestSqrDist = float.MaxValue;
            if (FlightGlobals.Vessels == null) return null;
            if (useGeoCoords)
            {
                if (FlightGlobals.currentMainBody is null) return null;
                position = (Vector3)FlightGlobals.currentMainBody.GetWorldSurfacePosition(position.x, position.y, position.z);
            }
            using (var v = FlightGlobals.Vessels.GetEnumerator())
                while (v.MoveNext())
                {
                    if (v.Current == null || !v.Current.loaded || v.Current.packed) continue;
                    if (VesselModuleRegistry.IgnoredVesselTypes.Contains(v.Current.vesselType)) continue;
                    var wm = v.Current.ActiveController().WM;
                    if (wm != null)
                    {
                        if (Vector3.SqrMagnitude(v.Current.vesselTransform.position - position) < closestSqrDist)
                        {
                            closestSqrDist = Vector3.SqrMagnitude(v.Current.vesselTransform.position - position);
                            closestV = v.Current;
                        }
                    }
                }
            return closestV;
        }
    }
}
