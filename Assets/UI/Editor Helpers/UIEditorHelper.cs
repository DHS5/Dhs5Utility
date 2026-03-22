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


        [MenuItem("GameObject/UI (Canvas)/UI Button", secondaryPriority = 0)]
        private static void CreateUIButton(MenuCommand menuCommand) => CreateUIButton(menuCommand, false);
        [MenuItem("GameObject/UI (Canvas)/UI Button + Text", secondaryPriority = 1)]
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
        
        [MenuItem("GameObject/UI (Canvas)/UI Toggle", secondaryPriority = 2)]
        private static void CreateUIToggle(MenuCommand menuCommand) => CreateUIToggle(menuCommand, false);
        [MenuItem("GameObject/UI (Canvas)/UI Toggle + Text", secondaryPriority = 3)]
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

        [MenuItem("GameObject/UI (Canvas)/UI Slider", secondaryPriority = 4)]
        private static void CreateUISlider(MenuCommand menuCommand)
        {
            using (var scope = new InstantiationScope("Slider", menuCommand?.context as GameObject,
                typeof(UISlider), typeof(UIGenericTransitioner)))
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
                transitioner.AddGraphic(handleImage);

                slider.AddTransitioner(transitioner);
                slider.FillRect = fillRectTransform;
                slider.HandleRect = handleRectTransform;
            }
        }

        [MenuItem("GameObject/UI (Canvas)/UI Scrollbar", secondaryPriority = 5)]
        private static void CreateUIScrollbar(MenuCommand menuCommand)
        {
            CreateUIScrollbar("Scrollbar", menuCommand?.context as GameObject);
        }
        public static GameObject CreateUIScrollbar(string name, GameObject parent, Action<RectTransform> rectTransformSetup = null)
        {
            GameObject result = null;

            using (var scope = new InstantiationScope(name, parent,
                typeof(Image), typeof(UIScrollbar), typeof(UIGenericTransitioner)))
            {
                result = scope.go;
                var scrollbar = scope.go.GetComponent<UIScrollbar>();
                Undo.RecordObject(scrollbar, "Setup UI Scrollbar");
                scrollbar.transition = Selectable.Transition.None;
                scrollbar.navigation = new Navigation() { mode = Navigation.Mode.None };
                if (scrollbar.TryGetComponent(out RectTransform rectTransform))
                {
                    if (rectTransformSetup != null) rectTransformSetup.Invoke(rectTransform);
                    else
                    {
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25f);
                    }
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
                transitioner.AddGraphic(handleImage);

                scrollbar.AddTransitioner(transitioner);
                scrollbar.HandleRect = handleRectTransform;
            }

            return result;
        }

        [MenuItem("GameObject/UI (Canvas)/UI ScrollView/Vertical", secondaryPriority = 6)]
        private static void CreateUIVerticalScrollView(MenuCommand menuCommand) => CreateUIScrollView(menuCommand?.context as GameObject, true, false);
        [MenuItem("GameObject/UI (Canvas)/UI ScrollView/Horizontal", secondaryPriority = 7)]
        private static void CreateUIHorizontalScrollView(MenuCommand menuCommand) => CreateUIScrollView(menuCommand?.context as GameObject, false, true);
        [MenuItem("GameObject/UI (Canvas)/UI ScrollView/Both", secondaryPriority = 8)]
        private static void CreateUIBothScrollView(MenuCommand menuCommand) => CreateUIScrollView(menuCommand?.context as GameObject, true, true);
        public static GameObject CreateUIScrollView(GameObject parent, bool vertical, bool horizontal, string name = "ScrollView", Action<RectTransform> rectTransformSetup = null)
        {
            GameObject result = null;

            using (var scope = new InstantiationScope(name, parent,
                typeof(UIScrollRect), typeof(Image)))
            {
                result = scope.go;

                var scrollRect = scope.go.GetComponent<UIScrollRect>();
                Undo.RecordObject(scrollRect, "Setup UI ScrollRect");
                scrollRect.Vertical = vertical;
                scrollRect.Horizontal = horizontal;

                var image = scope.go.GetComponent<Image>();
                image.type = Image.Type.Sliced;
                image.color = new Color(1f, 1f, 1f, 0.2f);
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                Vector2 contentSize = new Vector2(500f, 500f);
                if (scrollRect.TryGetComponent(out RectTransform rectTransform))
                {
                    if (rectTransformSetup != null)
                    {
                        rectTransformSetup.Invoke(rectTransform);
                    }
                    else if (rectTransform.parent is RectTransform p)
                    {
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        contentSize = p.rect.size;
                    }
                    else
                    {
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500f);
                    }
                }

                // Viewport
                var viewportGO = new GameObject("Viewport", typeof(Image), typeof(Mask));
                GameObjectUtility.SetParentAndAlign(viewportGO, scope.go);
                Undo.RegisterCreatedObjectUndo(viewportGO, viewportGO.name);
                var viewportImage = viewportGO.GetComponent<Image>();
                viewportImage.type = Image.Type.Sliced;
                viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
                if (viewportGO.TryGetComponent(out rectTransform))
                {
                    scrollRect.ViewportRect = rectTransform;

                    rectTransform.pivot = Vector2.up;
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                }
                
                // Content
                var contentGO = new GameObject("Content", typeof(RectTransform));
                GameObjectUtility.SetParentAndAlign(contentGO, viewportGO);
                Undo.RegisterCreatedObjectUndo(contentGO, contentGO.name);
                if (contentGO.TryGetComponent(out rectTransform))
                {
                    scrollRect.ContentRect = rectTransform;

                    if (vertical && horizontal)
                    {
                        rectTransform.pivot = Vector2.up;
                        rectTransform.anchorMin = Vector2.up;
                        rectTransform.anchorMax = Vector2.up;
                        rectTransform.sizeDelta = contentSize;
                    }
                    else if (vertical)
                    {
                        rectTransform.pivot = new Vector2(0.5f, 1f);
                        rectTransform.anchorMin = Vector2.up;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = new Vector2(0f, contentSize.y);
                    }
                    else if (horizontal)
                    {
                        rectTransform.pivot = new Vector2(0f, 0.5f);
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.up;
                        rectTransform.sizeDelta = new Vector2(contentSize.x, 0f);
                    }
                }

                // Vertical Scrollbar
                if (vertical)
                {
                    var verticalScrollbarGO = CreateUIScrollbar("Vertical Scrollbar", scope.go, (rectTransform) =>
                    {
                        rectTransform.pivot = new Vector2(1f, 1f);
                        rectTransform.anchorMin = Vector2.right;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = new Vector2(20f, horizontal ? -20f : 0f);
                    });

                    var verticalScrollbar = verticalScrollbarGO.GetComponent<UIScrollbar>();
                    verticalScrollbar.Direction = UIScrollbar.EDirection.BottomToTop;
                    scrollRect.VerticalScrollbar = verticalScrollbar;
                    scrollRect.VerticalScrollbarVisibility = UIScrollRect.EScrollbarVisibility.AutoHideAndExpandViewport;
                }
                
                // Horizontal Scrollbar
                if (horizontal)
                {
                    var horizontalScrollbarGO = CreateUIScrollbar("Horizontal Scrollbar", scope.go, (rectTransform) =>
                    {
                        rectTransform.pivot = new Vector2(0f, 0f);
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.right;
                        rectTransform.sizeDelta = new Vector2(vertical ? -20f : 0f, 20f);
                    });

                    var horizontalScrollbar = horizontalScrollbarGO.GetComponent<UIScrollbar>();
                    scrollRect.HorizontalScrollbar = horizontalScrollbar;
                    scrollRect.HorizontalScrollbarVisibility = UIScrollRect.EScrollbarVisibility.AutoHideAndExpandViewport;
                }

                Selection.activeGameObject = scope.go;
            }

            return result;
        }

        [MenuItem("GameObject/UI (Canvas)/UI Dropdown", secondaryPriority = 9)]
        private static void CreateUIDropdown(MenuCommand menuCommand)
        {
            using (var scope = new InstantiationScope("Dropdown", menuCommand?.context as GameObject,
                typeof(UIDropdown), typeof(Image), typeof(UIGenericTransitioner)))
            {
                var dropdown = scope.go.GetComponent<UIDropdown>();
                Undo.RecordObject(dropdown, "Setup UI Dropdown");
                dropdown.transition = Selectable.Transition.None;
                dropdown.navigation = new Navigation() { mode = Navigation.Mode.None };
                dropdown.AddOption("Option A");
                dropdown.AddOption("Option B");

                var image = scope.go.GetComponent<Image>();
                image.type = Image.Type.Sliced;
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

                if (dropdown.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                }

                var transitioner = scope.go.GetComponent<UIGenericTransitioner>();
                dropdown.AddTransitioner(transitioner);
                transitioner.AddGraphic(image);

                // Caption
                var captionGO = new GameObject("Caption Text", typeof(TextMeshProUGUI));
                GameObjectUtility.SetParentAndAlign(captionGO, scope.go);
                Undo.RegisterCreatedObjectUndo(captionGO, captionGO.name);
                var captionText = captionGO.GetComponent<TextMeshProUGUI>();
                captionText.text = "Caption";
                captionText.fontSize = 22f;
                captionText.color = Color.black;
                captionText.horizontalAlignment = HorizontalAlignmentOptions.Left;
                captionText.verticalAlignment = VerticalAlignmentOptions.Middle;
                if (captionGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = new Vector2(10f, 6f);
                    rectTransform.offsetMax = new Vector2(-40f, -7f);
                }
                dropdown.CaptionText = captionText;

                // Arrow
                var arrowGO = new GameObject("Arrow", typeof(Image));
                GameObjectUtility.SetParentAndAlign(arrowGO, scope.go);
                Undo.RegisterCreatedObjectUndo(arrowGO, arrowGO.name);
                var arrowImage = arrowGO.GetComponent<Image>();
                arrowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
                if (arrowGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.pivot = new Vector2(1f, 0.5f);
                    rectTransform.anchorMin = rectTransform.pivot;
                    rectTransform.anchorMax = rectTransform.pivot;
                    rectTransform.anchoredPosition = new Vector2(-10f, 0f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);
                }

                // Scroll View (Template)
                var scrollViewGO = CreateUIScrollView(scope.go, true, false, "Template", (rectTransform) =>
                {
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.right;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = new Vector2(0f, 2f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 150f);
                    dropdown.Template = rectTransform;
                });

                // Content
                var contentGO = scrollViewGO.transform.GetChild(0).GetChild(0).gameObject;
                if (contentGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 35f);
                }

                // Item
                var itemGO = new GameObject("Item", typeof(UIDefaultDropdownItem), typeof(UIToggle), typeof(Image));
                GameObjectUtility.SetParentAndAlign(itemGO, contentGO);
                Undo.RegisterCreatedObjectUndo(itemGO, itemGO.name);
                var item = itemGO.GetComponent<UIDefaultDropdownItem>();

                var itemToggle = itemGO.GetComponent<UIToggle>();
                itemToggle.navigation = new Navigation() { mode = Navigation.Mode.None };
                itemToggle.transition = Selectable.Transition.ColorTint;
                itemToggle.colors = new ColorBlock()
                {
                    fadeDuration = 0f,
                    colorMultiplier = 1f,
                    normalColor = Color.white,
                    highlightedColor = Color.moccasin,
                    pressedColor = Color.burlywood,
                    selectedColor = Color.moccasin,
                    disabledColor = Color.gray7,
                };
                itemToggle.IsOn = false;
                if (itemGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(0f, 35f);
                }

                var itemCheckmarkGO = new GameObject("Item Checkmark", typeof(Image));
                GameObjectUtility.SetParentAndAlign(itemCheckmarkGO, itemGO);
                Undo.RegisterCreatedObjectUndo(itemCheckmarkGO, itemCheckmarkGO.name);
                var itemCheckmark = itemCheckmarkGO.GetComponent<Image>();
                itemCheckmark.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
                itemToggle.AddCheckmark(itemCheckmark);
                if (itemCheckmarkGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.pivot = new Vector2(0f, 0.5f);
                    rectTransform.anchorMin = rectTransform.pivot;
                    rectTransform.anchorMax = rectTransform.pivot;
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30f);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);
                }

                var itemTextGO = new GameObject("Item Text", typeof(TextMeshProUGUI));
                GameObjectUtility.SetParentAndAlign(itemTextGO, itemGO);
                Undo.RegisterCreatedObjectUndo(itemTextGO, itemTextGO.name);
                var itemText = itemTextGO.GetComponent<TextMeshProUGUI>();
                itemText.text = "Option";
                itemText.fontSize = 18f;
                itemText.color = Color.black;
                itemText.horizontalAlignment = HorizontalAlignmentOptions.Left;
                itemText.verticalAlignment = VerticalAlignmentOptions.Middle;
                if (itemTextGO.TryGetComponent(out rectTransform))
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = new Vector2(30f, 1f);
                    rectTransform.offsetMax = new Vector2(-10f, -2f);
                }

                item.OnValidate();

                scrollViewGO.SetActive(false);

                Selection.activeGameObject = scope.go;
            }
        }

        #endregion
    }
}
#endif
