using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
#endif

namespace Dhs5.Utility
{
    /// <summary>
    /// Base of the physics-free volumes (<see cref="BoxVolume"/>, <see cref="SphereVolume"/>, <see cref="CapsuleVolume"/>) :
    /// edited like their collider counterpart, but invisible to physics queries and triggers.
    /// Detection is done through <see cref="Contains"/>.
    /// </summary>
    public abstract class Volume : MonoBehaviour
    {
        #region Members

        [SerializeField] protected Vector3 _center = Vector3.zero;

        [Header("Editor")]
        [SerializeField] protected Color _gizmoColor = new(0.55f, 0.85f, 0.4f, 1f);

        #endregion

        #region Properties

        /// <summary>Center of the volume, in local space</summary>
        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }

        public Color GizmoColor => _gizmoColor;

        #endregion

        #region World Space Accessors

        /// <summary>Center of the volume, in world space</summary>
        public Vector3 WorldCenter => transform.TransformPoint(_center);

        /// <summary>World space axis-aligned bounds enclosing the volume, like <see cref="Collider.bounds"/></summary>
        public abstract Bounds WorldBounds { get; }

        #endregion

        #region Queries

        /// <summary>Whether <paramref name="worldPoint"/> is inside the volume (surface included)</summary>
        public abstract bool Contains(Vector3 worldPoint);

        /// <summary>
        /// Closest point of the volume to <paramref name="worldPoint"/>, or the point itself if it is inside, like <see cref="Collider.ClosestPoint"/>
        /// </summary>
        public abstract Vector3 ClosestPoint(Vector3 worldPoint);

        #endregion

        #region Utility

        protected static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        protected static Vector3 GetLocalAxis(EVolumeAxis axis) => axis switch
        {
            EVolumeAxis.X => Vector3.right,
            EVolumeAxis.Z => Vector3.forward,
            _ => Vector3.up,
        };

        #endregion

        #region Editor

#if UNITY_EDITOR

        protected virtual void OnValidate() { }

        protected Color GizmoFillColor => new(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, _gizmoColor.a * 0.15f);

        /// <summary>Draws an arc with <see cref="Gizmos"/> lines, in the plane (<paramref name="axisA"/>, <paramref name="axisB"/>)</summary>
        protected static void DrawGizmoArc(Vector3 center, Vector3 axisA, Vector3 axisB, float radius, float fromDeg, float toDeg, int segments = 16)
        {
            Vector3 previous = center + radius * (Mathf.Cos(fromDeg * Mathf.Deg2Rad) * axisA + Mathf.Sin(fromDeg * Mathf.Deg2Rad) * axisB);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(fromDeg, toDeg, (float)i / segments) * Mathf.Deg2Rad;
                Vector3 next = center + radius * (Mathf.Cos(angle) * axisA + Mathf.Sin(angle) * axisB);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

#endif

        #endregion
    }

    public enum EVolumeAxis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(Volume), true), CanEditMultipleObjects]
    public class VolumeEditor : Editor
    {
        private SerializedProperty p_gizmoColor;

        private void OnEnable()
        {
            p_gizmoColor = serializedObject.FindProperty("_gizmoColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            // Same "Edit" toggle as the colliders inspectors : activates the volume's VolumeTool
            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Volume"), this);

            EditorGUILayout.Space();

            // Gizmo color drawn last : base class fields are serialized before the derived shape fields
            DrawPropertiesExcluding(serializedObject, "m_Script", "_gizmoColor");
            EditorGUILayout.PropertyField(p_gizmoColor);

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Scene view tool editing a volume with the same handles as the matching collider tool
    /// </summary>
    public abstract class VolumeTool<T> : EditorTool where T : Volume
    {
        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                if (obj is T volume)
                    DrawHandle(volume);
            }
        }

        /// <summary>Draws the handle of <paramref name="volume"/>, recording an undo before applying any change</summary>
        protected abstract void DrawHandle(T volume);

        /// <summary>Space in which the round volumes handles are drawn : centered on the volume, rotated like it, not scaled</summary>
        protected static Matrix4x4 GetUnscaledHandleMatrix(Volume volume) =>
            Matrix4x4.TRS(volume.WorldCenter, volume.transform.rotation, Vector3.one);

        /// <summary>
        /// Moves the volume's center to the handle's center, <paramref name="handleCenter"/> being in <paramref name="handleMatrix"/> space
        /// </summary>
        protected static void ApplyHandleCenter(Volume volume, Matrix4x4 handleMatrix, Vector3 handleCenter)
        {
            volume.Center = volume.transform.InverseTransformPoint(handleMatrix.MultiplyPoint3x4(handleCenter));
        }

        /// <summary><paramref name="value"/> / <paramref name="scale"/>, or <paramref name="fallback"/> if the scale is 0</summary>
        protected static float Unscale(float value, float scale, float fallback) =>
            Mathf.Approximately(scale, 0f) ? fallback : value / scale;
    }

#endif

    #endregion
}
