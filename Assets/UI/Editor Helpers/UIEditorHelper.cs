using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;


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
        private static void CreateUIButton(MenuCommand menuCommand) => CreateUIButton(menuCommand, false);
        [MenuItem("GameObject/UI/UI Button + Text")]
        private static void CreateUITextButton(MenuCommand menuCommand) => CreateUIButton(menuCommand, true);
        private static void CreateUIButton(MenuCommand menuCommand, bool withText)
        {
            using (var scope = new InstantiationScope("Button", menuCommand?.context as GameObject, 
                typeof(UIButton), typeof(Image), typeof(UIGenericTransitioner)))
            {
                var button = scope.go.GetComponent<UIButton>();
                Undo.RecordObject(button, "Setup UI Button");
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation() { mode = Navigation.Mode.None };

                var image = scope.go.GetComponent<Image>();
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 0.5f;

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                transitioner.AddGraphic(image);

                if (scope.go.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                }

                button.AddTransitioner(transitioner);

                // TEXT
                if (withText)
                {
                    var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
                    GameObjectUtility.SetParentAndAlign(textGO, scope.go);
                    Undo.RegisterCreatedObjectUndo(textGO, textGO.name);

                    var text = textGO.GetComponent<TextMeshProUGUI>();
                    text.color = Color.black;
                    text.alignment = TextAlignmentOptions.Center;
                    text.text = "Button";

                    if (textGO.TryGetComponent(out rectTransform))
                    {
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;
                    }
                }
            }
        }
        
        [MenuItem("GameObject/UI/UI Toggle")]
        private static void CreateUIToggle(MenuCommand menuCommand) => CreateUIToggle(menuCommand, false);
        [MenuItem("GameObject/UI/UI Toggle + Text")]
        private static void CreateUITextToggle(MenuCommand menuCommand) => CreateUIToggle(menuCommand, true);
        private static void CreateUIToggle(MenuCommand menuCommand, bool withText)
        {
            using (var scope = new InstantiationScope("Toggle", menuCommand?.context as GameObject, 
                typeof(UIToggle), typeof(Image), typeof(UIGenericTransitioner)))
            {
                var toggle = scope.go.GetComponent<UIToggle>();
                Undo.RecordObject(toggle, "Setup UI Toggle");
                toggle.transition = Selectable.Transition.None;
                toggle.navigation = new Navigation() { mode = Navigation.Mode.None };
                if (toggle.transform.parent != null
                    && toggle.transform.parent.TryGetComponent(out UIToggleGroup group))
                {
                    toggle.Group = group;
                }

                var image = scope.go.GetComponent<Image>();
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 0.5f;

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                transitioner.AddGraphic(image);

                if (scope.go.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 50f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                }

                toggle.AddTransitioner(transitioner);

                // Checkmark
                var checkmarkGO = new GameObject("Checkmark", typeof(Image));
                GameObjectUtility.SetParentAndAlign(checkmarkGO, scope.go);
                Undo.RegisterCreatedObjectUndo(checkmarkGO, checkmarkGO.name);

                var checkmarkImage = checkmarkGO.GetComponent<Image>();
                checkmarkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
                checkmarkImage.raycastTarget = false;

                if (checkmarkGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }

                toggle.AddCheckmark(checkmarkImage);

                // TEXT
                if (withText)
                {
                    var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
                    GameObjectUtility.SetParentAndAlign(textGO, scope.go);
                    Undo.RegisterCreatedObjectUndo(textGO, textGO.name);

                    var text = textGO.GetComponent<TextMeshProUGUI>();
                    text.color = Color.black;
                    text.alignment = TextAlignmentOptions.Converted;
                    text.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    text.verticalAlignment = VerticalAlignmentOptions.Middle;
                    text.text = "Toggle";

                    if (textGO.TryGetComponent(out rectTransform))
                    {
                        rectTransform.pivot = new Vector2(0f, 0.5f);
                        rectTransform.anchorMin = new Vector2(1f, 0f);
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = new Vector2(10f, 0f);
                        rectTransform.offsetMax = new Vector2(210f, 0f);
                    }
                }
            }
        }

        #endregion
    }
}
#endif
