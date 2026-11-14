using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
#endif

namespace Dhs5.Utility
{
    /// <summary>
    /// Sphere volume, edited like a <see cref="SphereCollider"/> but without any physics.
    /// Like the collider, it stays a sphere under non-uniform scale : its world radius uses the largest scale component.
    /// </summary>
    public class SphereVolume : Volume
    {
        #region Members

        [SerializeField, Min(0f)] private float _radius = 0.5f;

        #endregion

        #region Properties

        /// <summary>Radius of the sphere, in local space. Clamped to 0 minimum.</summary>
        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        #endregion

        #region World Space Accessors

        /// <summary>Scale applied to the radius : largest absolute lossy scale component</summary>
        public float RadiusScale
        {
            get
            {
                Vector3 s = Abs(transform.lossyScale);
                return Mathf.Max(s.x, s.y, s.z);
            }
        }

        /// <summary>Radius of the sphere, in world space</summary>
        public float WorldRadius => _radius * RadiusScale;

        public override Bounds WorldBounds => new(WorldCenter, 2f * WorldRadius * Vector3.one);

        #endregion

        #region Queries

        public override bool Contains(Vector3 worldPoint)
        {
            float r = WorldRadius;
            return (worldPoint - WorldCenter).sqrMagnitude <= r * r;
        }

        public override Vector3 ClosestPoint(Vector3 worldPoint)
        {
            Vector3 center = WorldCenter;
            Vector3 offset = worldPoint - center;
            float r = WorldRadius;

            return offset.sqrMagnitude <= r * r ? worldPoint : center + offset.normalized * r;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            _radius = Mathf.Max(0f, _radius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(WorldCenter, transform.rotation, Vector3.one);
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(Vector3.zero, WorldRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(WorldCenter, transform.rotation, Vector3.one);
            Gizmos.color = GizmoFillColor;
            Gizmos.DrawSphere(Vector3.zero, WorldRadius);
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    /// <summary>Alt : symmetrical (radius only)</summary>
    [EditorTool("Edit Sphere Volume", typeof(SphereVolume))]
    public class SphereVolumeTool : VolumeTool<SphereVolume>
    {
        private readonly SphereBoundsHandle _handle = new();

        protected override void DrawHandle(SphereVolume volume)
        {
            Matrix4x4 matrix = GetUnscaledHandleMatrix(volume);
            float radiusScale = volume.RadiusScale;

            using (new Handles.DrawingScope(volume.GizmoColor, matrix))
            {
                _handle.center = Vector3.zero;
                _handle.radius = volume.Radius * radiusScale;

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Edit Sphere Volume");
                    ApplyHandleCenter(volume, matrix, _handle.center);
                    volume.Radius = Unscale(_handle.radius, radiusScale, volume.Radius);
                }
            }
        }
    }

#endif

    #endregion
}
