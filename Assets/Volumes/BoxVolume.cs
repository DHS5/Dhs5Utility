using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
#endif

namespace Dhs5.Utility
{
    /// <summary>
    /// Oriented box volume, edited like a <see cref="BoxCollider"/> but without any physics.
    /// The box follows the transform (position, rotation and scale).
    /// </summary>
    public class BoxVolume : Volume
    {
        #region Members

        [SerializeField] private Vector3 _size = Vector3.one;

        #endregion

        #region Properties

        /// <summary>Size of the box, in local space. Negative components are made positive.</summary>
        public Vector3 Size
        {
            get => _size;
            set => _size = Abs(value);
        }
        /// <summary>Half the size, in local space</summary>
        public Vector3 Extents => _size * 0.5f;

        #endregion

        #region World Space Accessors

        /// <summary>Maps the unit cube (centered on 0, size 1) to the box in world space</summary>
        public Matrix4x4 BoxToWorldMatrix => transform.localToWorldMatrix * Matrix4x4.TRS(_center, Quaternion.identity, _size);

        public override Bounds WorldBounds
        {
            get
            {
                Matrix4x4 m = transform.localToWorldMatrix;
                Vector3 e = Extents;

                // Projection of the oriented extents on each world axis
                Vector3 worldExtents = new(
                    Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                    Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                    Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);

                return new Bounds(m.MultiplyPoint3x4(_center), worldExtents * 2f);
            }
        }

        /// <summary>Fills <paramref name="corners"/> (length 8 minimum) with the 8 corners of the box in world space</summary>
        public void GetWorldCorners(Vector3[] corners)
        {
            if (corners == null || corners.Length < 8)
                throw new ArgumentException("Corners array must have a length of at least 8", nameof(corners));

            Matrix4x4 m = transform.localToWorldMatrix;
            Vector3 e = Extents;

            int i = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        corners[i++] = m.MultiplyPoint3x4(_center + Vector3.Scale(e, new Vector3(x, y, z)));
        }
        /// <summary>The 8 corners of the box in world space (allocates, see <see cref="GetWorldCorners(Vector3[])"/>)</summary>
        public Vector3[] GetWorldCorners()
        {
            Vector3[] corners = new Vector3[8];
            GetWorldCorners(corners);
            return corners;
        }

        #endregion

        #region Queries

        public override bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - _center;
            Vector3 e = Extents;

            return Mathf.Abs(local.x) <= e.x
                && Mathf.Abs(local.y) <= e.y
                && Mathf.Abs(local.z) <= e.z;
        }

        /// <remarks>Exact for any rotation and scale, except a skewed transform (rotated child of a non-uniformly scaled parent)</remarks>
        public override Vector3 ClosestPoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - _center;
            Vector3 e = Extents;

            local.x = Mathf.Clamp(local.x, -e.x, e.x);
            local.y = Mathf.Clamp(local.y, -e.y, e.y);
            local.z = Mathf.Clamp(local.z, -e.z, e.z);

            return transform.TransformPoint(local + _center);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            _size = Abs(_size);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireCube(_center, _size);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoFillColor;
            Gizmos.DrawCube(_center, _size);
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    /// <summary>Alt : symmetrical, Shift : keep proportions</summary>
    [EditorTool("Edit Box Volume", typeof(BoxVolume))]
    public class BoxVolumeTool : VolumeTool<BoxVolume>
    {
        private readonly BoxBoundsHandle _handle = new();

        protected override void DrawHandle(BoxVolume volume)
        {
            using (new Handles.DrawingScope(volume.GizmoColor, volume.transform.localToWorldMatrix))
            {
                _handle.center = volume.Center;
                _handle.size = volume.Size;

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Edit Box Volume");
                    volume.Center = _handle.center;
                    volume.Size = _handle.size;
                }
            }
        }
    }

#endif

    #endregion
}
