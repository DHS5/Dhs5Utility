using System;
using System.Text;
using UnityEngine;

namespace Dhs5.Utility.Editors
{
    public static class EditorDataUtility
    {
        #region SCRIPT Writer

        public class ScriptWriter
        {

        }

        #region ENUM Writer

        public class EnumWriter : ScriptWriter
        {
            #region Members

            public readonly string enumName;
            public readonly Type enumType;
            public readonly string[] enumContent;

            public readonly StringBuilder usingStringBuilder;

            #endregion


            // --- STATIC ---

            #region Static Utility

            public static string EnsureCorrectEnumName(string inputName)
            {
                var forbiddenCharacters = new char[] { ' ', '/', '\\', '<', '>', ':', ';', '*', '|', '"', '?', '!', '=', '+', '-', '.', ',', '\'', '{', '}', '(', ')', '[', ']',
                '#', '&', '~', '¨', '^', '`', '°', '€', '$', '£', '¤', '%', 'é', 'è', 'ç', 'à', 'ù', '@', '§', 'µ' };
                var result = inputName.Trim(forbiddenCharacters);
                foreach (var c in forbiddenCharacters)
                {
                    result = result.Replace(c, '_');
                }
                return result.ToUpper();
            }

            #endregion
        }

        #endregion

        #endregion
    }
}
