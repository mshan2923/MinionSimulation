using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Custom
{
    public static class Math
    {
        #region Convert
        public static Vector3 Direction2Rotation(Vector3 Dir)
        {
            /*
            Vector3 Lrot = Vector3.zero;

            Lrot.y = Mathf.Acos(Vector3.Normalize(new Vector3(Dir.x, 0, Dir.z)).x) * Mathf.Rad2Deg;
            if (Dir.z < 0)
            {
                Lrot.y = 360 - Lrot.y;
            }

            float XZLength = new Vector3(Dir.x, 0, Dir.z).magnitude;
            float YOffset = Mathf.Abs(1 / Mathf.Max(Mathf.Sin(Lrot.y * Mathf.Deg2Rad), Mathf.Cos(Lrot.y * Mathf.Deg2Rad)));

            //Lrot.z = Mathf.Atan(Dir.y) * Mathf.Rad2Deg;//이건 45 이상으로 안됨
            Lrot.z = Mathf.Atan((Dir.y * YOffset) / XZLength) * Mathf.Rad2Deg;

            return Lrot;*/

            return Quaternion.LookRotation(Dir).eulerAngles;
        }
        public static Vector3 Rotation2Direction(Vector3 Rot)
        {
            /*
            Vector3 temp = new Vector3(Cos2(Rot.y), (Sin2(Rot.z) / Cos2(Rot.z)), Sin2(Rot.y));
            float YawRate = Mathf.Min(Mathf.Abs(temp.x), Mathf.Abs(temp.z)) / Mathf.Max(Mathf.Abs(temp.x), Mathf.Abs(temp.z));

            if (Mathf.Abs(temp.x) > Mathf.Abs(temp.z))
            {
                return new Vector3((temp.x > 0 ? 1 : -1), temp.y, (temp.z > 0 ? YawRate : YawRate * -1));
            }
            else
            {
                return new Vector3((temp.x > 0 ? YawRate : YawRate * -1), temp.y, (temp.z > 0 ? 1 : -1));
            }*/

            return Quaternion.Euler(Rot) * Vector3.forward;
        }

        public static float Sin2(float Degreese)
        {
            return Mathf.Sin(Degreese * Mathf.Deg2Rad);
        }
        public static float Cos2(float Degreese)
        {
            return Mathf.Cos(Degreese * Mathf.Deg2Rad);
        }

        public static float Pow2(float vaule)
        {
            return vaule * vaule;
        }
        /// <summary>
        /// LookDirection is Local / Get LookSize
        /// </summary>
        /// <param name="bound"></param>
        /// <param name="LookDirection"></param>
        /// <returns></returns>
        public static Rect Bound2Rect(Bounds bound, Quaternion LookDirection)
        {
            Rect result = new Rect();
            Vector3 Look = LookDirection.eulerAngles;
            Vector3 Size = bound.extents * 2;

            result.width = Size.x * Mathf.Abs(Cos2(Look.y)) + Size.z * Mathf.Abs(Sin2(Look.y));

            float Depth = Size.x * Mathf.Abs(Sin2(Look.y)) + Size.z * Mathf.Abs(Cos2(Look.y));
            //Debug.Log("X side Depth : " + Mathf.Abs(Sin2(Look.y)) + "Z side Depth : " + Mathf.Abs(Cos2(Look.y)));//정면인상태에서 Yaw 할때 Bound의 깊이

            result.height = Size.y * Mathf.Abs(Cos2(Look.x)) + Depth * Mathf.Abs(Sin2(Look.x));

            return result;
        }

        /// <summary>
        /// Return To Degrees
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static float Dot(Vector3 A, Vector3 B)
        {
            return Mathf.Acos(Vector3.Dot(A, B)) * Mathf.Rad2Deg;
        }
        public static float AngleNormalize(float Angle)
        {
            var NorAngle = Mathf.Abs(Mathf.Abs(Angle) < 360 ? Angle : Angle % 360);

            if (NorAngle > 180)
            {
                NorAngle -= 360;
            }//Normalize Angle (-180 ~ 180)

            return NorAngle;
        }
        #endregion

        #region overlap
        /// <summary>
        /// Closest On infinity Length Line
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        public static Vector3 ClosePointOnDirection(Vector3 origin, Vector3 direction, Vector3 Target)
        {
            return origin + Vector3.Project(Target - origin, direction);
            // line_start + Vector3.Project(point - line_start, line_end - line_start);

            /*
            Vector3 lhs = Target - origin;
            float dotP = Vector3.Dot(lhs, direction);
            return origin + direction * dotP;
            *///=====Other Way
        }

        // For finite lines:
        public static Vector3 ClosePointOnLine(Vector3 origin, Vector3 end, Vector3 target)
        {
            Vector3 direction = end - origin;
            float length = direction.magnitude;
            direction.Normalize();
            float project_length = Mathf.Clamp(Vector3.Dot(target - origin, direction), 0f, length);
            return origin + direction * project_length;
        }

        public static Vector2[] ContactTwoCircle(Vector2 Origin, float OriginRadius, Vector2 Target, float TargetRadius)
        {
            //https://blog.daum.net/chamtech/8891791

            Vector2[] result = new Vector2[2];

            float Dis = (Origin - Target).magnitude;

            if (Dis <= (OriginRadius + TargetRadius))
            {
                float gaping = Mathf.Acos((Pow2(OriginRadius) - Pow2(TargetRadius) + Pow2(Dis)) / (2 * OriginRadius * Dis)) * Mathf.Rad2Deg;
                //T1

                float Dot = Mathf.Atan2((Target.y - Origin.y), (Target.x - Origin.x)) * Mathf.Rad2Deg;//T2

                //Debug.Log("Angle : (" + gaping + " + " + Dot + ")");
                result[0].x = Origin.x + OriginRadius * Cos2(gaping + Dot);
                result[0].y = Origin.y + OriginRadius * Sin2(gaping + Dot);
                //Debug.Log(Origin + " + " + OriginRadius + " * (" + Cos2(gaping + Dot) + " , " + Sin2(gaping + Dot) + " )");
                //T3 , T4

                result[1].x = Origin.x + OriginRadius * Cos2(Dot - gaping);
                result[1].y = Origin.x + OriginRadius * Sin2(Dot - gaping);
                //T5 , T6

                return result;
            }
            else
            {
                Debug.Log("Not Contect / " + (OriginRadius + TargetRadius) + " / " + Dis);
                return null;
            }
        }
        public static Vector3[] GetCloestLines(Vector3 Point1, Vector3 Vec1, Vector3 Point2, Vector3 Vec2, float intersectLength = 0.01f)
        {
            Vector3 Cloest1 = Vector3.zero;
            Vector3 Cloest2 = Vector3.zero;

            {
                Vector3 Vec3 = Point2 - Point1;
                Vector3 crossVec1and2 = Vector3.Cross(Vec1, Vec2);
                Vector3 crossVec3and2 = Vector3.Cross(Vec3, Vec2);

                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                Cloest1 = Point1 + (Vec1 * s);
            }
            {
                Vector3 vec3 = Point1 - Point2;
                Vector3 cross1N2 = Vector3.Cross(Vec2, Vec1);
                Vector3 cross3N1 = Vector3.Cross(vec3, Vec1);

                float t = Vector3.Dot(cross3N1, cross1N2) / cross1N2.sqrMagnitude;
                Cloest2 = Point2 + Vec2 * t;
            }

            if ((Cloest1 - Cloest2).sqrMagnitude < (intersectLength * intersectLength))
            {
                return new Vector3[] { Cloest1 };
            }
            else
            {
                return new Vector3[] { Cloest1, Cloest2 };
            }
        }
        public static float GetCloestLinesDistance(Vector3 Point1, Vector3 Vec1, Vector3 Point2, Vector3 Vec2, float intersectLength = 0.01f, bool sqr = true)
        {
            Vector3 Cloest1 = Vector3.zero;
            Vector3 Cloest2 = Vector3.zero;

            {
                Vector3 Vec3 = Point2 - Point1;
                Vector3 crossVec1and2 = Vector3.Cross(Vec1, Vec2);
                Vector3 crossVec3and2 = Vector3.Cross(Vec3, Vec2);

                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                Cloest1 = Point1 + (Vec1 * s);
            }
            {
                Vector3 vec3 = Point1 - Point2;
                Vector3 cross1N2 = Vector3.Cross(Vec2, Vec1);
                Vector3 cross3N1 = Vector3.Cross(vec3, Vec1);

                float t = Vector3.Dot(cross3N1, cross1N2) / cross1N2.sqrMagnitude;
                Cloest2 = Point2 + Vec2 * t;
            }

            if ((Cloest1 - Cloest2).sqrMagnitude < (intersectLength * intersectLength))
            {
                return 0;
            }
            else
            {
                return sqr ? (Cloest1 - Cloest2).sqrMagnitude : (Cloest1 - Cloest2).magnitude;
            }
        }
        #endregion

        #region AreaCheck
        public static bool InRange(float vaule, float min, float max, bool equalPass = true)
        {
            if (equalPass)
            {
                return vaule >= min && vaule <= max;
            }
            else
            {
                return vaule > min && vaule < max;
            }
        }
        public static bool InRange(Vector3 vaule, Vector3 min, Vector3 max, bool equalPass = true)
        {
            return InRange(vaule.x, min.x, max.x, equalPass)
                && InRange(vaule.y, min.y, max.y, equalPass)
                && InRange(vaule.z, min.z, max.z, equalPass);
        }
        public static bool InArea(Bounds area, Vector3 Pos)
        {
            return InArea(area.center, area.extents, Pos);
        }
        public static bool InArea(Vector3 areaCenter, Vector3 areaExtent, Vector3 Pos)
        {
            return Math.InRange(Pos, areaCenter - (areaExtent), areaCenter + (areaExtent));
        }
        public static bool InCircle(Bounds area, Vector3 Pos)
        {
            // (x - c)^2 / a^2 + (y - d)^2 / b^2 + (z - e)^2 / c^2 = 1
            // a : area.Extents.x  * 0.5f / b : area.Extents.y  * 0.5f / c : area.Extents.z  * 0.5f

            //(Pos.x * Pos.x) / (area.extents.x * 0.5f * area.extents.x * 0.5f) + (Pos.z * Pos.z) / (area.extents.z * 0.5f * area.extents.z * 0.5f)

            return Math.Pow2(Pos.x - area.center.x) / Math.Pow2(area.extents.x) + Math.Pow2(Pos.z - area.center.z) / Math.Pow2(area.extents.z) <= 1;
        }
        /// <summary>
        /// Apply Origin Rotation
        /// </summary>
        public static bool InCircle(Bounds bounds, float Yaw, Vector3 Target)
        {
            return InCircle(bounds.center, bounds.extents, Yaw, Target);
        }
        /// <summary>
        /// Apply Origin Rotation
        /// </summary>
        public static bool InCircle(Vector3 Origin, Vector3 HalfSize, float Yaw, Vector3 Target)
        {
            var forwardDir = Quaternion.Euler(new Vector3(0, Yaw, 0)) * Vector3.forward;

            var BorderDis = Mathf.Lerp(HalfSize.x, HalfSize.z,
                Mathf.Abs(Vector3.Dot(forwardDir, (Target - Origin).normalized)));

            return (Target - Origin).sqrMagnitude <= Math.Pow2(BorderDis);
        }
        #endregion
        public static float CircleRadiusRate(Bounds area, Vector3 Pos)
        {
            return Math.Pow2(Pos.x - area.center.x) / Math.Pow2(area.extents.x) + Math.Pow2(Pos.z - area.center.z) / Math.Pow2(area.extents.z);
        }

        static float SideToDistance(float Angle, float MaxSizeRot, float HalfLength)
        {
            var temp = Mathf.Abs(Mathf.Abs(Angle) < 360 ? Angle : Angle % 360);

            if (temp > 180)
            {
                temp -= 360;
            }//Normalize Angle (-180 ~ 180)

            temp = Mathf.Abs(temp);//좌우 미러

            //float Rate = 0;

            if (temp < 90)
            {
                if (temp < MaxSizeRot)
                {
                    //Rate = temp / MaxSizeRot;
                    //return Mathf.Lerp(0, MaxSizeRot, Rate);
                    return HalfLength / Math.Cos2(Mathf.Lerp(0, MaxSizeRot, (temp / MaxSizeRot)));
                }
                else
                {
                    //Rate = 1 - ((temp - MaxSizeRot) / (90 - MaxSizeRot));
                    //return Mathf.Lerp(0, 90 - MaxSizeRot, Rate);
                    return HalfLength / Math.Cos2(Mathf.Lerp(0, 90 - MaxSizeRot,
                        (1 - ((temp - MaxSizeRot) / (90 - MaxSizeRot)))));
                }
            }
            else
            {
                if (temp < (180 - MaxSizeRot))
                {
                    //Rate = ((temp - 90) / (90 - MaxSizeRot));
                    //return Mathf.Lerp(0, MaxSizeRot, Rate);
                    return HalfLength / Math.Cos2(Mathf.Lerp(0, 90 - MaxSizeRot, ((temp - 90) / (90 - MaxSizeRot))));
                }
                else
                {
                    //Rate = (Mathf.Abs(temp - 180) / MaxSizeRot);
                    //return Mathf.Lerp(0, 90 - MaxSizeRot, Rate);
                    return HalfLength / Math.Cos2(Mathf.Lerp(0, MaxSizeRot, (Mathf.Abs(temp - 180) / MaxSizeRot)));
                }
            }
        }
        #region Quad
        /// <summary>
        /// Distance For (Origin ~ ClosestPoint In Quad)
        /// </summary>
        public static float ClosestQuadToDistance(float Wide, float Height, float Rotation)
        {
            float MaxSizeRot = Mathf.Atan2(Wide * 0.5f, Height * 0.5f) * Mathf.Rad2Deg;

            var NorAngle = Mathf.Abs(Mathf.Abs(Rotation) < 360 ? Rotation : Rotation % 360);

            if (NorAngle > 180)
            {
                NorAngle -= 360;
            }//Normalize Angle (-180 ~ 180)

            NorAngle = Mathf.Abs(NorAngle);//좌우 미러

            if (NorAngle < MaxSizeRot || (180 - MaxSizeRot) < NorAngle)
            {
                return SideToDistance(NorAngle, MaxSizeRot, Height * 0.5f);
            }
            else
            {
                return SideToDistance(NorAngle, MaxSizeRot, Wide * 0.5f);
            }
        }
        /// <summary>
        /// Distance For (Origin ~ ClosestPoint In Quad)
        /// </summary>
        public static float ClosestQuadToDistance(Vector3 Size, Vector3 Origin, float Yaw, Vector3 Target)
        {
            float LookAngle = Math.Dot(YawToDirection(Yaw), (Target - Origin).normalized);
            return ClosestQuadToDistance(Size.x, Size.z, LookAngle);
        }
        public static Vector3 ClosestQuad(Vector3 Size, Vector3 Origin, float Yaw, Vector3 Target)
        {
            var CPoint = (Target - Origin).normalized * ClosestQuadToDistance(Size, Origin, Yaw, Target);

            return Origin + CPoint;
        }
        /// <summary>
        /// Apply Origin Rotation
        /// </summary>
        public static bool InQuad(Vector3 Size, Transform Origin, Vector3 Target)
        {
            //================================================ Rotation 은 Yaw만
            return Vector3.SqrMagnitude(Target - Origin.position) <=
                Math.Pow2(ClosestQuadToDistance(Size, Origin.position, Origin.rotation.eulerAngles.y, Target));
        }
        /// <summary>
        /// Apply Origin Rotation
        /// </summary>
        public static bool InQuad(Vector3 Size, Vector3 Origin, Quaternion Rotation, Vector3 Target)
        {
            return Vector3.SqrMagnitude(Target - Origin) <=
                Math.Pow2(ClosestQuadToDistance(Size, Origin, Rotation.eulerAngles.y, Target));
        }
        #endregion

        public static Vector3 Snap(Vector3 pos, float gridSize)
        {
            float multiplied = 1 / gridSize;
            return new Vector3(Mathf.Round(pos.x * multiplied),
                Mathf.Round(pos.y * multiplied), Mathf.Round(pos.z * multiplied)) * gridSize;
        }

        #region Useless
        [System.Obsolete("Recommand use RotatedQuadSize")]
        public static Vector3 RotatedBound2D(Bounds bounds, float diagonal, float Yaw)
        {
            Vector3 RotatedBound = Vector3.zero;
            var YAngle = Quaternion.Euler(0, Yaw, 0);

            Vector3 MaxPos = (Quaternion.LookRotation(bounds.extents) * YAngle) * Vector3.forward * diagonal;

            //Debug.DrawLine(bounds.center - MaxPos, bounds.center + MaxPos, Color.green, Time.deltaTime);
            RotatedBound = new Vector3(Mathf.Abs(MaxPos.x),
                0,//Mathf.Abs(MaxPos.y),
                Mathf.Abs(MaxPos.z));

            MaxPos = (Quaternion.LookRotation(new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z * -1)) * YAngle) * Vector3.forward * diagonal;
            //Debug.DrawLine(bounds.center - MaxPos, bounds.center + MaxPos, Color.red, Time.deltaTime);

            RotatedBound = new Vector3(Mathf.Max(RotatedBound.x, Mathf.Abs(MaxPos.x)),
                0,//Mathf.Max(RotatedBound.y, Mathf.Abs(MaxPos.y)),
                Mathf.Max(RotatedBound.z, Mathf.Abs(MaxPos.z)));

            return RotatedBound;
        }
        public static Vector2 RotatedQuadSize(Vector2 Extents, float Yaw)
        {
            Quaternion rotate = Quaternion.AngleAxis(Yaw, Vector3.up);

            Vector3 FR = rotate * (Vector3.right * Extents.x) + rotate * (Vector3.forward * Extents.y);
            Vector3 BL = rotate * (Vector3.left * Extents.x) + rotate * (Vector3.back * Extents.y);

            Vector3 FL = rotate * (Vector3.left * Extents.x) + rotate * (Vector3.forward * Extents.y);
            Vector3 BR = rotate * (Vector3.right * Extents.x) + rotate * (Vector3.back * Extents.y);

            return new Vector2(Mathf.Max(Mathf.Abs(FR.x - BL.x), Mathf.Abs(FL.x - BR.x)),
                                Mathf.Max(Mathf.Abs(FR.z - BL.z), Mathf.Abs(FL.z - BR.z))) * 0.5f;
        }
        public static Vector3 RotatedQuadSize(Vector3 Extents, float Yaw)
        {
            Quaternion rotate = Quaternion.AngleAxis(Yaw, Vector3.up);

            Vector3 FR = rotate * (Vector3.right * Extents.x) + rotate * (Vector3.forward * Extents.z);
            Vector3 BL = rotate * (Vector3.left * Extents.x) + rotate * (Vector3.back * Extents.z);

            Vector3 FL = rotate * (Vector3.left * Extents.x) + rotate * (Vector3.forward * Extents.z);
            Vector3 BR = rotate * (Vector3.right * Extents.x) + rotate * (Vector3.back * Extents.z);

            return new Vector3(Mathf.Max(Mathf.Abs(FR.x - BL.x), Mathf.Abs(FL.x - BR.x)), 0,
                                Mathf.Max(Mathf.Abs(FR.z - BL.z), Mathf.Abs(FL.z - BR.z))) * 0.5f;
        }
        public static Vector3 YawToDirection(float Yaw)
        {
            return new Vector3(Math.Sin2(Yaw), 0, Math.Cos2(Yaw));
        }
        #endregion

        /// <summary>
        /// 두백터의 충돌 반사각
        /// </summary>
        /// <returns></returns>
        [System.Obsolete] public static Vector3 GetCollisionReflect(Quaternion Target, Quaternion Other, float TargetMass, float OtherMass)
        {
            var AddVecForward = (Target * Vector3.forward * TargetMass + Other * Vector3.forward * OtherMass).normalized;
            var AddVecUp = (Target * Vector3.up * TargetMass + Other * Vector3.up * OtherMass).normalized;

            var AddVecRot = Quaternion.LookRotation(AddVecForward, AddVecUp);

            return Quaternion.LookRotation(Vector3.Reflect(Target * Vector3.forward, AddVecRot * Vector3.right)) * Vector3.forward * (TargetMass / (TargetMass + OtherMass));
        }// 충돌을 했을때 기준 방향
        [System.Obsolete] public static Vector3 GetCollisionReflect(Vector3 TargetDir, Vector3 OtherDir, float TargetMass, float OtherMass)
        {
            var AddVecForward = (TargetDir.normalized * TargetMass + OtherDir.normalized * OtherMass).normalized;
            var AddVecRot = Quaternion.LookRotation(AddVecForward);

            return Quaternion.LookRotation(Vector3.Reflect(TargetDir, AddVecRot * Vector3.forward)) * Vector3.forward * (TargetMass / (TargetMass + OtherMass));
        }
        public static bool GetSphereNormal(Vector3 TargetPos, Vector3 ProjectPos, Vector3 ProjectDir, float targetRadius, out Quaternion Normal)
        {
            var dis = (TargetPos - ProjectPos).magnitude;
            //var ProjectDot = Vector3.Dot(ProjectDir, (ProjectPos - TargetPos).normalized);
            var ProjectRad = Mathf.Acos(Vector3.Dot(ProjectDir, (ProjectPos - TargetPos).normalized));

            var OthoLength = dis * Mathf.Sin(ProjectRad);
            if (OthoLength > targetRadius)
            {
                Normal = Quaternion.identity;
                return false;
            }

            var ProjectLength = dis * Mathf.Cos(ProjectRad);
            var ProjectForwardPos = TargetPos + ProjectDir * ProjectLength;
            var ProjectUp = (ProjectForwardPos + (ProjectPos - ProjectForwardPos).normalized * OthoLength) - ProjectForwardPos;

            var RotationAxis = Quaternion.LookRotation(ProjectDir * -1, ProjectUp);
            var CollisionRad = Mathf.Asin(OthoLength / targetRadius);

            Normal = RotationAxis * Quaternion.AngleAxis(CollisionRad * Mathf.Rad2Deg * -1, Vector3.right);
            return true;
        }

        /// <summary>
        /// 구체 반사각
        /// </summary>
        /// <param name="v0">Direction * Speed</param>
        /// <param name="v1">Direction * Speed</param>
        public static void CollisionSphereReflect(Vector3 v0, Vector3 v1, float mass0, float mass1, out Vector3 vec0, out Vector3 vec1)
        {
            var normal = Vector3.Normalize(v0 + v1);

            var postV0 = (mass1 * v1 + mass0 * v0) / (mass0 + mass1) - v1;
            var postV1 = (mass1 * v1 + mass0 * v0) / (mass0 + mass1) - v0;
            var v0r = postV0 - v0;
            var v1r = postV1 - v1;

            var dot0 = Vector3.Dot(v0r, normal);
            var dot1 = Vector3.Dot(v1r, normal);

            v0r -= 2 * dot0 * normal;
            v1r -= 2 * dot1 * normal;

            vec0 = v0r.normalized * postV0.magnitude;
            vec1 = v1r.normalized * postV1.magnitude;
        }
        /// <summary>
        /// 구체 반사각 , Equal Mass
        /// </summary>
        /// <param name="v0">Direction * Speed</param>
        /// <param name="v1">Direction * Speed</param>
        public static void CollisionSphereReflect(Vector3 v0, Vector3 v1, out Vector3 vec0, out Vector3 vec1)
        {
            var normal = Vector3.Normalize(v0 + v1);

            var postV0 = (v0 + v1) * 0.5f - v1;
            var postV1 = (v0 + v1) * 0.5f - v0;
            var v0r = postV0 - v0;
            var v1r = postV1 - v1;

            var dot0 = Vector3.Dot(v0r, normal);
            var dot1 = Vector3.Dot(v1r, normal);

            v0r -= 2 * dot0 * normal;
            v1r -= 2 * dot1 * normal;

            vec0 = v0r.normalized * postV0.magnitude;
            vec1 = v1r.normalized * postV1.magnitude;
        }

        public static float GetCurveEvaluate(float t, Keyframe keyframe0, Keyframe keyframe1)
        {
            float dt = keyframe1.time - keyframe0.time;

            float m0 = keyframe0.outTangent * dt;
            float m1 = keyframe1.inTangent * dt;

            float t2 = t * t;
            float t3 = t2 * t;

            float a = 2 * t3 - 3 * t2 + 1;
            float b = t3 - 2 * t2 + t;
            float c = t3 - t2;
            float d = -2 * t3 + 3 * t2;

            return a * keyframe0.value + b * m0 + c * m1 + d * keyframe1.value;
        }


        #region Camera

        public static float GetCameraConeAngle(Quaternion cameraRot, float fov, float horizonFov)
        {
            return Math.Dot(cameraRot * Vector3.forward,
                (
                    cameraRot * Vector3.forward
                    + cameraRot * Vector3.up * math.tan(math.radians(fov * 0.5f))
                    + cameraRot * Vector3.right * math.tan(math.radians(horizonFov * 0.5f))
                ).normalized);
        }
        public static float GetCameraConeAngle(Camera camera)
        {
            return GetCameraConeAngle(camera.transform.rotation, camera.fieldOfView, Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect));
        }

        public static Vector2 GetRenderAreaAngle(Vector3 cameraPos, Quaternion cameraRot, Vector3 target)
        {
            return new Vector2
            {
                x = Math.Dot((cameraRot * Vector3.forward), Vector3.ProjectOnPlane(target - cameraPos, cameraRot * Vector3.up)),
                y = Math.Dot((cameraRot * Vector3.forward), Vector3.ProjectOnPlane(target - cameraPos, cameraRot * Vector3.right))
            };
        }
        public static Vector2 GetRenderAreaAngle(Camera camera, Vector3 target)
        {
            return GetRenderAreaAngle(camera.transform.position, camera.transform.rotation, target);
        }

        public static bool IsRenderArea(Vector3 cameraPos, Quaternion cameraRot, float fov, float horizonFov, Vector3 target)
        {
            var angles = GetRenderAreaAngle(cameraPos, cameraRot, target);
            return (angles.x <= horizonFov * 0.5f) && (angles.y <= fov * 0.5f);
        }
        public static bool IsRenderArea(Camera camera, Vector3 target)
        {
            return IsRenderArea(camera.transform.position, camera.transform.rotation, camera.fieldOfView,
                Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect), target);
        }

        public static bool IsRenderAreaLite(Vector3 cameraPos, Quaternion cameraRot, float CameraConeAngle, float horizonFov, Vector3 target)
        {
            if (Math.Dot(cameraRot * Vector3.forward, (target - cameraPos).normalized) > CameraConeAngle)
            {
                return false;
            }
            else
            {
                return (horizonFov * 0.5 <= (Math.Dot((cameraRot * Vector3.forward), Vector3.ProjectOnPlane(target - cameraPos, cameraRot * Vector3.right))));
            }
        }
        public static bool IsRenderAreaLite(Camera camera, Vector3 target)
        {
            return IsRenderAreaLite(camera.transform.position, camera.transform.rotation, camera.fieldOfView,
                Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect), target);
        }

        public static Vector2 IsRenderAreaRate(Vector3 cameraPos, Quaternion cameraRot, float fov, float horizonFov, Vector3 target)
        {
            var angles = GetRenderAreaAngle(cameraPos, cameraRot, target);
            return new Vector2
            {
                x = angles.x / horizonFov,
                y = angles.y / fov
            };
        }
        public static Vector2 IsRenderAreaRate(Camera camera, Vector3 target)
        {
            return IsRenderAreaRate(camera.transform.position, camera.transform.rotation, camera.fieldOfView,
                Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect), target);
        }

        #endregion

        #region Normal
        public static Vector3 GetRotatedBoxSize(LocalTransform transform, Vector3 extents)
        {
            Vector3[] points = new Vector3[]
            {
                transform.Right() * extents.x + transform.Up() * extents.y + transform.Forward() * extents.z,//ppp
                transform.Right() * extents.x + transform.Up() * extents.y - transform.Forward() * extents.z,//ppm
                transform.Right() * extents.x - transform.Up() * extents.y + transform.Forward() * extents.z,//pmp
                transform.Right() * extents.x - transform.Up() * extents.y - transform.Forward() * extents.z,//pmm
                Vector3.zero, Vector3.zero ,Vector3.zero, Vector3.zero
            };
            points[4] = -points[0];
            points[5] = -points[1];
            points[6] = -points[2];
            points[7] = -points[3];

            return new Vector3
            {
                x = points.Max(t => t.x) - points.Min(t => t.x),
                y = points.Max(t => t.y) - points.Min(t => t.y),
                z = points.Max(t => t.z) - points.Min(t => t.z)
            };
        }

        /// <summary>
        /// return  -1 -> Sphere orign into Box , 0 -> Collision , 1 -> Not Collision
        /// </summary>
        /// <param name="boxTransform"></param>
        /// <param name="boxExtent"></param>
        /// <param name="pointer"></param>
        /// <param name="pointerRadius"></param>
        /// <param name="Normal"></param>
        /// <returns></returns>
        public static int CalculationBoxOffsetOnhitSphere(LocalTransform boxTransform, float3 boxExtent, float3 pointer, float pointerRadius, out Vector3 Normal)
        {
            bool isCollision = true;

            var RotatedExtent = (boxTransform.Right() * boxExtent.x + boxTransform.Up() * boxExtent.y + boxTransform.Forward() * boxExtent.z);
            var BoxMaxPos = boxTransform.Position + RotatedExtent;

            var ProjectPointerX = math.project(pointer, boxTransform.Right());
            var ProjectBoxX = math.project(boxTransform.Position, boxTransform.Right());
            var ProjectBoxMaxX = math.project(BoxMaxPos, boxTransform.Right());
            var disX = Vector3.Magnitude(ProjectPointerX - ProjectBoxX) - Vector3.Magnitude(ProjectBoxMaxX - ProjectBoxX);
            if (disX > pointerRadius) { isCollision = false; }

            var ProjectPointerY = math.project(pointer, boxTransform.Up());
            var ProjectBoxY = math.project(boxTransform.Position, boxTransform.Up());
            var ProjectBoxMaxY = math.project(BoxMaxPos, boxTransform.Up());
            var disY = Vector3.Magnitude(ProjectPointerY - ProjectBoxY) - Vector3.Magnitude(ProjectBoxMaxY - ProjectBoxY);
            if (disY > pointerRadius) { isCollision = false; }

            var ProjectPointerZ = math.project(pointer, boxTransform.Forward());
            var ProjectBoxZ = math.project(boxTransform.Position, boxTransform.Forward());
            var ProjectBoxMaxZ = math.project(BoxMaxPos, boxTransform.Forward());
            var disZ = Vector3.Magnitude(ProjectPointerZ - ProjectBoxZ) - Vector3.Magnitude(ProjectBoxMaxZ - ProjectBoxZ);
            if (disZ > pointerRadius) { isCollision = false; }


            float3 ClosePoint = float3.zero;
            {
                var BoxMinPos = boxTransform.Position - RotatedExtent;
                var ProjectBoxMinX = math.project(BoxMinPos, boxTransform.Right());
                var ProjectBoxMinY = math.project(BoxMinPos, boxTransform.Up());
                var ProjectBoxMinZ = math.project(BoxMinPos, boxTransform.Forward());

                if (disX > 0)
                    ClosePoint += (math.distance(ProjectPointerX, ProjectBoxMinX) < math.distance(ProjectPointerX, ProjectBoxMaxX))
                        ? ProjectBoxMinX
                        : ProjectBoxMaxX;
                else
                    ClosePoint += ProjectPointerX;

                if (disY > 0)
                    ClosePoint += (math.distance(ProjectPointerY, ProjectBoxMinY) < math.distance(ProjectPointerY, ProjectBoxMaxY))
                        ? ProjectBoxMinY
                        : ProjectBoxMaxY;
                else
                    ClosePoint += ProjectPointerY;

                if (disZ > 0)
                    ClosePoint += (math.distance(ProjectPointerZ, ProjectBoxMinZ) < math.distance(ProjectPointerZ, ProjectBoxMaxZ))
                        ? ProjectBoxMinZ
                        : ProjectBoxMaxZ;
                else
                    ClosePoint += ProjectPointerZ;
            }


            if (disX < 0 && disY < 0 && disZ < 0)
            {
                //중점이 box 안으로 들어 갔을때
                Normal = math.normalize(-(boxTransform.Position - pointer));
                return -1;
            }
            else
            {
                Normal = pointer - ClosePoint;
                //ClosePointer = pointer - Normal
                return isCollision ? 0 : 1;
            }
        }
        public static bool CalculationBoxNormalOnhitSphere(LocalTransform boxTransform, float3 boxExtent, float3 pointer, float pointerRadius, out Vector3 Normal)
        {
            var RotatedExtent = (boxTransform.Right() * boxExtent.x + boxTransform.Up() * boxExtent.y + boxTransform.Forward() * boxExtent.z);
            var BoxMaxPos = boxTransform.Position + RotatedExtent;

            var ProjectPointerX = math.project(pointer, boxTransform.Right());
            var ProjectBoxX = math.project(boxTransform.Position, boxTransform.Right());
            var ProjectBoxMaxX = math.project(BoxMaxPos, boxTransform.Right());
            var disX = Vector3.Magnitude(ProjectPointerX - ProjectBoxX) - Vector3.Magnitude(ProjectBoxMaxX - ProjectBoxX);
            if (disX > pointerRadius) { Normal = Vector3.zero; return false; }

            var ProjectPointerY = math.project(pointer, boxTransform.Up());
            var ProjectBoxY = math.project(boxTransform.Position, boxTransform.Up());
            var ProjectBoxMaxY = math.project(BoxMaxPos, boxTransform.Up());
            var disY = Vector3.Magnitude(ProjectPointerY - ProjectBoxY) - Vector3.Magnitude(ProjectBoxMaxY - ProjectBoxY);
            if (disY > pointerRadius) { Normal = Vector3.zero; return false; }

            var ProjectPointerZ = math.project(pointer, boxTransform.Forward());
            var ProjectBoxZ = math.project(boxTransform.Position, boxTransform.Forward());
            var ProjectBoxMaxZ = math.project(BoxMaxPos, boxTransform.Forward());
            var disZ = Vector3.Magnitude(ProjectPointerZ - ProjectBoxZ) - Vector3.Magnitude(ProjectBoxMaxZ - ProjectBoxZ);
            if (disZ > pointerRadius) { Normal = Vector3.zero; return false; }

            float3 ClosePoint = float3.zero;
            {
                var BoxMinPos = boxTransform.Position - RotatedExtent;
                var ProjectBoxMinX = math.project(BoxMinPos, boxTransform.Right());
                var ProjectBoxMinY = math.project(BoxMinPos, boxTransform.Up());
                var ProjectBoxMinZ = math.project(BoxMinPos, boxTransform.Forward());

                if (disX > 0)
                    ClosePoint += (math.distance(ProjectPointerX, ProjectBoxMinX) < math.distance(ProjectPointerX, ProjectBoxMaxX))
                        ? ProjectBoxMinX
                        : ProjectBoxMaxX;
                else
                    ClosePoint += ProjectPointerX;

                if (disY > 0)
                    ClosePoint += (math.distance(ProjectPointerY, ProjectBoxMinY) < math.distance(ProjectPointerY, ProjectBoxMaxY))
                        ? ProjectBoxMinY
                        : ProjectBoxMaxY;
                else
                    ClosePoint += ProjectPointerY;

                if (disZ > 0)
                    ClosePoint += (math.distance(ProjectPointerZ, ProjectBoxMinZ) < math.distance(ProjectPointerZ, ProjectBoxMaxZ))
                        ? ProjectBoxMinZ
                        : ProjectBoxMaxZ;
                else
                    ClosePoint += ProjectPointerZ;
            }//Set ClosePoint

            if (disX < 0 && disY < 0 && disZ < 0)
            {
                //중점이 box 안으로 들어 갔을때
                Normal = math.normalize(-(boxTransform.Position - pointer));
            }
            else
            {
                Normal = pointer - ClosePoint;
                //ClosePointer = pointer - Normal
            }

            return true;
        }
        #endregion
    }
}
