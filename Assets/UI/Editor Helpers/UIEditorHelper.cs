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


        [MenuItem("GameObject/UI/UI Button", secondaryPriority = 0)]
        private static void CreateUIButton(MenuCommand menuCommand) => CreateUIButton(menuCommand, false);
        [MenuItem("GameObject/UI/UI Button + Text", secondaryPriority = 1)]
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
        
        [MenuItem("GameObject/UI/UI Toggle", secondaryPriority = 2)]
        private static void CreateUIToggle(MenuCommand menuCommand) => CreateUIToggle(menuCommand, false);
        [MenuItem("GameObject/UI/UI Toggle + Text", secondaryPriority = 3)]
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

        [MenuItem("GameObject/UI/UI Slider", secondaryPriority = 4)]
        private static void CreateUISlider(MenuCommand menuCommand)
        {
            using (var scope = new InstantiationScope("Slider", menuCommand?.context as GameObject,
                typeof(UISlider), typeof(UIGenericTransitioner), typeof(RectTransform)))
            {
                var slider = scope.go.GetComponent<UISlider>();
                Undo.RecordObject(slider, "Setup UI Slider");
                slider.transition = Selectable.Transition.None;
                slider.navigation = new Navigation() { mode = Navigation.Mode.None };
                if (slider.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25f);
                }

                // Background
                var backgroundGO = new GameObject("Background", typeof(Image));
                GameObjectUtility.SetParentAndAlign(backgroundGO, scope.go);
                Undo.RegisterCreatedObjectUndo(backgroundGO, backgroundGO.name);
                var backgroundImage = backgroundGO.GetComponent<Image>();
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                if (backgroundGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = new Vector2(0f, 0.25f);
                    rectTransform.anchorMax = new Vector2(1f, 0.75f);
                    rectTransform.offsetMin = new Vector2(2f, 0f);
                    rectTransform.offsetMax = Vector2.zero;
                }

                // Fill Container
                var fillContainerGO = new GameObject("Fill Container", typeof(RectTransform));
                GameObjectUtility.SetParentAndAlign(fillContainerGO, scope.go);
                Undo.RegisterCreatedObjectUndo(fillContainerGO, fillContainerGO.name);
                if (fillContainerGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = new Vector2(0f, 0.25f);
                    rectTransform.anchorMax = new Vector2(1f, 0.75f);
                    rectTransform.offsetMin = new Vector2(7f, 0f);
                    rectTransform.offsetMax = new Vector2(-15f, 0f);
                }
                
                // Fill
                var fillGO = new GameObject("Fill", typeof(Image));
                GameObjectUtility.SetParentAndAlign(fillGO, fillContainerGO);
                Undo.RegisterCreatedObjectUndo(fillGO, fillGO.name);
                var fillImage = fillGO.GetComponent<Image>();
                fillImage.type = Image.Type.Sliced;
                fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                if (fillGO.TryGetComponent(out RectTransform fillRectTransform))
                {
                    fillRectTransform.anchorMin = new Vector2(0f, 0f);
                    fillRectTransform.anchorMax = new Vector2(0f, 1f);
                    fillRectTransform.sizeDelta = new Vector2(10f, 0f);
                }

                // Handle Container
                var handleContainerGO = new GameObject("Handle Container", typeof(RectTransform));
                GameObjectUtility.SetParentAndAlign(handleContainerGO, scope.go);
                Undo.RegisterCreatedObjectUndo(handleContainerGO, handleContainerGO.name);
                if (handleContainerGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = new Vector2(12f, 0f);
                    rectTransform.offsetMax = new Vector2(-10f, 0f);
                }

                // Handle
                var handleGO = new GameObject("Handle", typeof(Image));
                GameObjectUtility.SetParentAndAlign(handleGO, handleContainerGO);
                Undo.RegisterCreatedObjectUndo(handleGO, handleGO.name);
                var handleImage = handleGO.GetComponent<Image>();
                handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (handleGO.TryGetComponent(out RectTransform handleRectTransform))
                {
                    handleRectTransform.anchorMin = new Vector2(0f, 0f);
                    handleRectTransform.anchorMax = new Vector2(0f, 1f);
                    handleRectTransform.sizeDelta = new Vector2(25f, 0f);
                }

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                transitioner.AddGraphic(backgroundImage);
                transitioner.AddGraphic(fillImage);
                transitioner.AddGraphic(handleImage);

                slider.AddTransitioner(transitioner);
                slider.FillRect = fillRectTransform;
                slider.HandleRect = handleRectTransform;
            }
        }

        [MenuItem("GameObject/UI/UI Scrollbar", secondaryPriority = 5)]
        private static void CreateUIScrollbar(MenuCommand menuCommand)
        {
            using (var scope = new InstantiationScope("Scrollbar", menuCommand?.context as GameObject,
                typeof(Image), typeof(UIScrollbar), typeof(UIGenericTransitioner)))
            {
                var scrollbar = scope.go.GetComponent<UIScrollbar>();
                Undo.RecordObject(scrollbar, "Setup UI Scrollbar");
                scrollbar.transition = Selectable.Transition.None;
                scrollbar.navigation = new Navigation() { mode = Navigation.Mode.None };
                if (scrollbar.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25f);
                }

                var backgroundImage = scope.go.GetComponent<Image>();
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

                // Handle Container
                var handleContainerGO = new GameObject("Handle Container", typeof(RectTransform));
                GameObjectUtility.SetParentAndAlign(handleContainerGO, scope.go);
                Undo.RegisterCreatedObjectUndo(handleContainerGO, handleContainerGO.name);
                if (handleContainerGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = new Vector2(-20f, -20f);
                }

                // Handle
                var handleGO = new GameObject("Handle", typeof(Image));
                GameObjectUtility.SetParentAndAlign(handleGO, handleContainerGO);
                Undo.RegisterCreatedObjectUndo(handleGO, handleGO.name);
                var handleImage = handleGO.GetComponent<Image>();
                handleImage.type = Image.Type.Sliced;
                handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                if (handleGO.TryGetComponent(out RectTransform handleRectTransform))
                {
                    handleRectTransform.anchorMin = new Vector2(0f, 0f);
                    handleRectTransform.anchorMax = new Vector2(0.2f, 1f);
                    handleRectTransform.sizeDelta = new Vector2(20f, 20f);
                }

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                transitioner.AddGraphic(backgroundImage);
                transitioner.AddGraphic(handleImage);

                scrollbar.AddTransitioner(transitioner);
                scrollbar.HandleRect = handleRectTransform;
            }
        }

        #endregion
    }
}
#endif
