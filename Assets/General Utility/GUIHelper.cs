using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.GUIs
{
    public class GUIHelper
    {
        #region GUI Color

        public static Color transparentBlack01 = new Color(0f, 0f, 0f, 0.1f);
        public static Color transparentBlack02 = new Color(0f, 0f, 0f, 0.2f);
        public static Color transparentBlack03 = new Color(0f, 0f, 0f, 0.3f);
        public static Color transparentBlack04 = new Color(0f, 0f, 0f, 0.4f);
        public static Color transparentBlack05 = new Color(0f, 0f, 0f, 0.5f);
        public static Color transparentBlack06 = new Color(0f, 0f, 0f, 0.6f);
        public static Color transparentBlack07 = new Color(0f, 0f, 0f, 0.7f);
        public static Color transparentBlack08 = new Color(0f, 0f, 0f, 0.8f);
        public static Color transparentBlack09 = new Color(0f, 0f, 0f, 0.9f);

        public static Color transparentWhite01 = new Color(1f, 1f, 1f, 0.1f);
        public static Color transparentWhite02 = new Color(1f, 1f, 1f, 0.2f);
        public static Color transparentWhite03 = new Color(1f, 1f, 1f, 0.3f);
        public static Color transparentWhite04 = new Color(1f, 1f, 1f, 0.4f);
        public static Color transparentWhite05 = new Color(1f, 1f, 1f, 0.5f);
        public static Color transparentWhite06 = new Color(1f, 1f, 1f, 0.6f);
        public static Color transparentWhite07 = new Color(1f, 1f, 1f, 0.7f);
        public static Color transparentWhite08 = new Color(1f, 1f, 1f, 0.8f);
        public static Color transparentWhite09 = new Color(1f, 1f, 1f, 0.9f);

        public static Color grey01 = new Color(0.1f, 0.1f, 0.1f, 1f);
        public static Color grey015 = new Color(0.15f, 0.15f, 0.15f, 1f);
        public static Color grey02 = new Color(0.2f, 0.2f, 0.2f, 1f);
        public static Color grey03 = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static Color grey04 = new Color(0.4f, 0.4f, 0.4f, 1f);
        public static Color grey05 = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static Color grey06 = new Color(0.6f, 0.6f, 0.6f, 1f);
        public static Color grey07 = new Color(0.7f, 0.7f, 0.7f, 1f);
        public static Color grey08 = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static Color grey09 = new Color(0.9f, 0.9f, 0.9f, 1f);

        #endregion

        #region GUI Styles

        public static GUIStyle blackFolderStyle = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            contentOffset = new Vector2(5f, 0f)
        };

        public static GUIStyle centeredLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle centeredBoldLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle downCenteredBoldLabel = new GUIStyle()
        {
            alignment = TextAnchor.LowerCenter,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle bigTitleLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 18,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };

        public static GUIStyle simpleIconButton = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            imagePosition = ImagePosition.ImageOnly
        };

        public static GUIStyle foldoutStyle = new()
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };

        #endregion

        #region Textures

        private static Texture2D _whiteTexture;
        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                }
                return _whiteTexture;
            }
        }

        #endregion

        #region GUI Elements

        public static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color color2 = GUI.color;
                GUI.color *= color;
                GUI.DrawTexture(rect, WhiteTexture);
                GUI.color = color2;
            }
        }

        #endregion

        #region GUI Change Check

        private static bool _hadChangeBeforeChangeCheck = false;

        public static void BeginChangeCheck()
        {
            _hadChangeBeforeChangeCheck = GUI.changed;
            GUI.changed = false;
        }
        public static bool EndChangeCheck()
        {
            bool change = GUI.changed;
            GUI.changed |= _hadChangeBeforeChangeCheck;
            return change;
        }

        #endregion
    }
}
