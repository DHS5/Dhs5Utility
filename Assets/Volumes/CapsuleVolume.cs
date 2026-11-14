using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
#endif

namespace Dhs5.Utility
{
    /// <summary>
    /// Capsule volume, edited like a <see cref="CapsuleCollider"/> but without any physics.
    /// Like the collider : the height is scaled by the scale along <see cref="Direction"/>, the radius by the largest of the two other scale components,
    /// and a height smaller than twice the radius gives a sphere.
    /// </summary>
    public class CapsuleVolume : Volume
    {
        #region Members

        [SerializeField, Min(0f)] private float _radius = 0.5f;
        [SerializeField, Min(0f)] private float _height = 2f;
        [SerializeField] private EVolumeAxis _direction = EVolumeAxis.Y;

        #endregion

        #region Properties

        /// <summary>Radius of the capsule, in local space. Clamped to 0 minimum.</summary>
        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }
        /// <summary>Total height of the capsule (hemispheres included), in local space. Clamped to 0 minimum.</summary>
        public float Height
        {
            get => _height;
            set => _height = Mathf.Max(0f, value);
        }
        /// <summary>Local axis along which the capsule's height goes</summary>
        public EVolumeAxis Direction
        {
            get => _direction;
            set => _direction = value;
        }

        #endregion

        #region World Space Accessors

        /// <summary>Scale applied to the radius : largest absolute lossy scale component orthogonal to <see cref="Direction"/></summary>
        public float RadiusScale
        {
            get
            {
                Vector3 s = Abs(transform.lossyScale);
                return _direction switch
                {
                    EVolumeAxis.X => Mathf.Max(s.y, s.z),
                    EVolumeAxis.Z => Mathf.Max(s.x, s.y),
                    _ => Mathf.Max(s.x, s.z),
                };
            }
        }
        /// <summary>Scale applied to the height : absolute lossy scale component along <see cref="Direction"/></summary>
        public float HeightScale => Mathf.Abs(transform.lossyScale[(int)_direction]);

        public float WorldRadius => _radius * RadiusScale;
        public float WorldHeight => _height * HeightScale;

        /// <summary>Normalized direction of the capsule's height, in world space</summary>
        public Vector3 WorldDirection => transform.rotation * GetLocalAxis(_direction);

        /// <summary>
        /// End points of the capsule's inner segment (centers of the hemispheres), in world space.
        /// Identical if the capsule is a sphere.
        /// </summary>
        public void GetWorldSegment(out Vector3 pointA, out Vector3 pointB)
        {
            float halfSegment = Mathf.Max(0f, WorldHeight * 0.5f - WorldRadius);
            Vector3 center = WorldCenter;
            Vector3 offset = WorldDirection * halfSegment;

            pointA = center - offset;
            pointB = center + offset;
        }

        public override Bounds WorldBounds
        {
            get
            {
                GetWorldSegment(out var a, out var b);
                float r = WorldRadius;

                Bounds bounds = new();
                bounds.SetMinMax(Vector3.Min(a, b) - r * Vector3.one, Vector3.Max(a, b) + r * Vector3.one);
                return bounds;
            }
        }

        #endregion

        #region Queries

        public override bool Contains(Vector3 worldPoint)
        {
            GetWorldSegment(out var a, out var b);
            float r = WorldRadius;

            return (worldPoint - ClosestPointOnSegment(worldPoint, a, b)).sqrMagnitude <= r * r;
        }

        public override Vector3 ClosestPoint(Vector3 worldPoint)
        {
            GetWorldSegment(out var a, out var b);
            float r = WorldRadius;

            Vector3 segmentPoint = ClosestPointOnSegment(worldPoint, a, b);
            Vector3 offset = worldPoint - segmentPoint;

            return offset.sqrMagnitude <= r * r ? worldPoint : segmentPoint + offset.normalized * r;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float sqrLength = ab.sqrMagnitude;
            if (sqrLength < Mathf.Epsilon) return a;

            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / sqrLength);
            return a + ab * t;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            _radius = Mathf.Max(0f, _radius);
            _height = Mathf.Max(0f, _height);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            float r = WorldRadius;
            float halfSegment = Mathf.Max(0f, WorldHeight * 0.5f - r);

            // Frame where the capsule's height is the local Y axis
            Quaternion axisRotation = _direction switch
            {
                EVolumeAxis.X => Quaternion.Euler(0f, 0f, -90f),
                EVolumeAxis.Z => Quaternion.Euler(90f, 0f, 0f),
                _ => Quaternion.identity,
            };
            Gizmos.matrix = Matrix4x4.TRS(WorldCenter, transform.rotation * axisRotation, Vector3.one);

            Vector3 top = Vector3.up * halfSegment;
            Vector3 bottom = -top;

            // Rings
            DrawGizmoArc(top, Vector3.right, Vector3.forward, r, 0f, 360f, 32);
            DrawGizmoArc(bottom, Vector3.right, Vector3.forward, r, 0f, 360f, 32);

            // Sides
            Gizmos.DrawLine(top + Vector3.right * r, bottom + Vector3.right * r);
            Gizmos.DrawLine(top - Vector3.right * r, bottom - Vector3.right * r);
            Gizmos.DrawLine(top + Vector3.forward * r, bottom + Vector3.forward * r);
            Gizmos.DrawLine(top - Vector3.forward * r, bottom - Vector3.forward * r);

            // Hemispheres
            DrawGizmoArc(top, Vector3.right, Vector3.up, r, 0f, 180f);
            DrawGizmoArc(top, Vector3.forward, Vector3.up, r, 0f, 180f);
            DrawGizmoArc(bottom, Vector3.right, Vector3.down, r, 0f, 180f);
            DrawGizmoArc(bottom, Vector3.forward, Vector3.down, r, 0f, 180f);
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    /// <summary>Alt : symmetrical</summary>
    [EditorTool("Edit Capsule Volume", typeof(CapsuleVolume))]
    public class CapsuleVolumeTool : VolumeTool<CapsuleVolume>
    {
        private readonly CapsuleBoundsHandle _handle = new();

        protected override void DrawHandle(CapsuleVolume volume)
        {
            Matrix4x4 matrix = GetUnscaledHandleMatrix(volume);
            float radiusScale = volume.RadiusScale;
            float heightScale = volume.HeightScale;

            using (new Handles.DrawingScope(volume.GizmoColor, matrix))
            {
                _handle.heightAxis = volume.Direction switch
                {
                    EVolumeAxis.X => CapsuleBoundsHandle.HeightAxis.X,
                    EVolumeAxis.Z => CapsuleBoundsHandle.HeightAxis.Z,
                    _ => CapsuleBoundsHandle.HeightAxis.Y,
                };
                _handle.center = Vector3.zero;
                _handle.radius = volume.Radius * radiusScale;
                _handle.height = volume.Height * heightScale;

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Edit Capsule Volume");
                    ApplyHandleCenter(volume, matrix, _handle.center);
                    volume.Radius = Unscale(_handle.radius, radiusScale, volume.Radius);
                    volume.Height = Unscale(_handle.height, heightScale, volume.Height);
                }
            }
        }
    }

#endif

    #endregion
}
