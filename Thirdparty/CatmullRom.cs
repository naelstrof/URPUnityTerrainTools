using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JPBotelho {
    /*  
        Catmull-Rom splines are Hermite curves with special tangent values.
        Hermite curve formula:
        (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
        For points p0 and p1 passing through points m0 and m1 interpolated over t = [0, 1]
        Tangent M[k] = (P[k+1] - P[k-1]) / 2
    */
    public static class CatmullRom {
        [System.Serializable]
        public class CatmullFactors {
            public CatmullFactors(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
                this.p1 = p1; this.p2 = p2; this.p3 = p3; this.p4 = p4;
            }
            public Vector3 p1,p2,p3,p4;
            public int Size() {
                return sizeof(float)*3*4;
            }
        }

        //Math stuff to generate the spline points
        public static List<CatmullFactors> GenerateCatmullFactors(List<Transform> controlPoints, bool closedLoop) {
            List<CatmullFactors> factors = new List<CatmullFactors>();

            Vector3 p0, p1; //Start point, end point
            Vector3 m0, m1; //Tangents

            factors = new List<CatmullFactors>();
            // First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
            int closedAdjustment = closedLoop ? 0 : 1;
            for (int currentPoint = 0; currentPoint < controlPoints.Count - closedAdjustment; currentPoint++) {
                bool closedLoopFinalPoint = (closedLoop && currentPoint == controlPoints.Count - 1);

                p0 = controlPoints[currentPoint].position;
                
                if(closedLoopFinalPoint) {
                    p1 = controlPoints[0].position;
                } else {
                    p1 = controlPoints[currentPoint + 1].position;
                }

                // m0
                // Tangent M[k] = (P[k+1] - P[k-1]) / 2
                if (currentPoint == 0) {
                    if(closedLoop) {
                        m0 = p1 - controlPoints[controlPoints.Count - 1].position;
                    } else {
                        m0 = p1 - p0;
                    }
                } else {
                    m0 = p1 - controlPoints[currentPoint - 1].position;
                }

                // m1
                if (closedLoop) {
                    //Last point case
                    if (currentPoint == controlPoints.Count - 1) {
                        m1 = controlPoints[(currentPoint + 2) % controlPoints.Count].position - p0;
                    //First point case
                    } else if (currentPoint == 0) {
                        m1 = controlPoints[currentPoint + 2].position - p0;
                    } else {
                        m1 = controlPoints[(currentPoint + 2) % controlPoints.Count].position - p0;
                    }
                } else {
                    if (currentPoint < controlPoints.Count - 2) {
                        m1 = controlPoints[(currentPoint + 2) % controlPoints.Count].position - p0;
                    } else {
                        m1 = p1 - p0;
                    }
                }

                m0 *= 0.5f; //Doing this here instead of in every single above statement
                m1 *= 0.5f;

                factors.Add(new CatmullFactors(p0,p1,m0,m1));
            }
            return factors;
        }

        //Calculates curve position at t[0, 1]
        public static Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t) {
            // Hermite curve formula:
            // (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
            Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * start
                + (t * t * t - 2.0f * t * t + t) * tanPoint1
                + (-2.0f * t * t * t + 3.0f * t * t) * end
                + (t * t * t - t * t) * tanPoint2;

            return position;
        }

        //Calculates tangent at t[0, 1]
        public static Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t) {
            // Calculate tangents
            // p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
            Vector3 tangent = (6 * t * t - 6 * t) * start
                + (3 * t * t - 4 * t + 1) * tanPoint1
                + (-6 * t * t + 6 * t) * end
                + (3 * t * t - 2 * t) * tanPoint2;

            return tangent.normalized;
        }
    }
}