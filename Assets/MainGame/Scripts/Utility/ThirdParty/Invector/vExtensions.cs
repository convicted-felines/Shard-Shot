using System;
using System.Collections.Generic;
using UnityEngine;

namespace Invector
{
    /// <summary>
    /// 与 Invector LITE 模板 <c>vExtensions</c> 一致，供第三人称镜头避障与角度夹取使用。
    /// </summary>
    public static class vExtensions
    {
        public static T[] Append<T>(this T[] arrayInitial, T[] arrayToAppend)
        {
            if (arrayToAppend == null)
            {
                throw new ArgumentNullException("The appended object cannot be null");
            }

            if ((arrayInitial is string) || (arrayToAppend is string))
            {
                throw new ArgumentException("The argument must be an enumerable");
            }

            T[] ret = new T[arrayInitial.Length + arrayToAppend.Length];
            arrayInitial.CopyTo(ret, 0);
            arrayToAppend.CopyTo(ret, arrayInitial.Length);
            return ret;
        }

        public static T[] vToArray<T>(this List<T> list)
        {
            T[] array = new T[list.Count];
            if (list == null || list.Count == 0)
            {
                return array;
            }

            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i];
            }

            return array;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            do
            {
                if (angle < -360)
                {
                    angle += 360;
                }

                if (angle > 360)
                {
                    angle -= 360;
                }
            } while (angle < -360 || angle > 360);

            return Mathf.Clamp(angle, min, max);
        }

        public static ClipPlanePoints NearClipPlanePoints(this Camera camera, Vector3 pos, float clipPlaneMargin)
        {
            return camera.NearClipPlanePoints(pos, camera.transform.rotation, clipPlaneMargin);
        }

        public static ClipPlanePoints NearClipPlanePoints(
            this Camera camera,
            Vector3 pos,
            Quaternion rotation,
            float clipPlaneMargin)
        {
            var clipPlanePoints = new ClipPlanePoints();
            float halfFov = (camera.fieldOfView / 2f) * Mathf.Deg2Rad;
            float aspect = camera.aspect;
            float near = camera.nearClipPlane;
            float height = near * Mathf.Tan(halfFov) * (1f + clipPlaneMargin);
            float width = height * aspect;

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 rightWidth = right * width;
            Vector3 upHeight = up * height;
            Vector3 forwardNear = forward * near;

            clipPlanePoints.LowerRight = pos + rightWidth - upHeight + forwardNear;
            clipPlanePoints.LowerLeft = pos - rightWidth - upHeight + forwardNear;
            clipPlanePoints.UpperRight = pos + rightWidth + upHeight + forwardNear;
            clipPlanePoints.UpperLeft = pos - rightWidth + upHeight + forwardNear;
            return clipPlanePoints;
        }
    }

    public struct ClipPlanePoints
    {
        public Vector3 UpperLeft;
        public Vector3 UpperRight;
        public Vector3 LowerLeft;
        public Vector3 LowerRight;
    }
}
