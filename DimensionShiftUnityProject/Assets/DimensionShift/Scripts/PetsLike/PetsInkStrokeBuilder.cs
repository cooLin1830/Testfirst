using UnityEngine;
using UnityEngine.Rendering;

namespace DimensionShift.PetsLike
{
    public static class PetsInkStrokeBuilder
    {
        public static GameObject CreateStroke(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            Vector3 planeNormal,
            Material material,
            float width,
            float wobble,
            int seed,
            int segments = 9,
            Color? color = null)
        {
            GameObject strokeObject = new GameObject(name);
            strokeObject.transform.SetParent(parent);

            LineRenderer line = strokeObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = material;
            Color strokeColor = color ?? Color.black;
            line.startColor = strokeColor;
            line.endColor = strokeColor;
            line.widthMultiplier = width;

            int pointCount = Mathf.Max(2, segments + 1);
            line.positionCount = pointCount;

            Vector3 direction = end - start;
            Vector3 wobbleAxis = Vector3.Cross(planeNormal.normalized, direction.normalized);
            if (wobbleAxis.sqrMagnitude < 0.001f)
            {
                wobbleAxis = Vector3.up;
            }

            wobbleAxis.Normalize();
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount == 1 ? 0f : (float)i / (pointCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                if (i > 0 && i < pointCount - 1)
                {
                    point += wobbleAxis * (SignedNoise(seed, i) * wobble);
                }

                line.SetPosition(i, point);
            }

            return strokeObject;
        }

        private static float SignedNoise(int seed, int index)
        {
            float value = Mathf.Sin(seed * 12.9898f + index * 78.233f) * 43758.5453f;
            return (value - Mathf.Floor(value)) * 2f - 1f;
        }
    }
}
