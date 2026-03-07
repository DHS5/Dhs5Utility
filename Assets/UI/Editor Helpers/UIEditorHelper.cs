using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    public class UIEditorHelper
    {
        [MenuItem("GameObject/UI/UI Button")]
        private static void CreateUIButton(MenuCommand menuCommand)
        {
            var undoIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create UI Button");

            var go = new GameObject("Button", typeof(UIButton));
            GameObjectUtility.SetParentAndAlign(go, menuCommand?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, go.name);
            Selection.activeGameObject = go;

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Undo.RecordObject(rectTransform, "Setup Button Rect Transform");
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            }

            var button = go.GetComponent<UIButton>();
            Undo.RecordObject(button, "Setup UI Button");
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation() { mode = Navigation.Mode.None };

            var image = go.AddComponent<Image>();
            Undo.RegisterCreatedObjectUndo(image, image.name);

            Undo.CollapseUndoOperations(undoIndex);
        }
    }
}
