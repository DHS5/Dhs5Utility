using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.UI
{
    public static class UIEditorHelper
    {
        #region Instantiations

        public class InstantiationScope : IDisposable
        {
            public readonly GameObject go;
            public readonly int undoIndex;

            public InstantiationScope(string name, GameObject parent, params System.Type[] types)
            {
                undoIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Create " + name);

                go = new GameObject(name, types);
                GameObjectUtility.SetParentAndAlign(go, parent);
                Undo.RegisterCreatedObjectUndo(go, go.name);
                Selection.activeGameObject = go;
            }

            public void Dispose()
            {
                Undo.CollapseUndoOperations(undoIndex);
            }
        }

        [MenuItem("GameObject/UI/UI Button")]
        private static void CreateUIButton(MenuCommand menuCommand)
        {
            using (var scope = new InstantiationScope("Button", menuCommand?.context as GameObject, 
                typeof(UIButton), typeof(Image), typeof(UIGenericTransitioner)))
            {
                var button = scope.go.GetComponent<UIButton>();
                Undo.RecordObject(button, "Setup UI Button");
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation() { mode = Navigation.Mode.None };

                var image = scope.go.GetComponent<Image>();
                Undo.RegisterCreatedObjectUndo(image, image.name);

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                transitioner.AddGraphic(image);

                if (scope.go.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                }
            }
        }

        #endregion
    }
}
#endif
