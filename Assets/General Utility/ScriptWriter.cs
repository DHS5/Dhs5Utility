using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR

namespace Dhs5.Utility.Editors
{
    public class ScriptWriter
    {
        #region ENUM ScriptType

        public enum EScriptType : byte
        {
            NONE = 0,
            CLASS = 1,
            STATIC_CLASS = 2,
            STRUCT = 3,
            ENUM = 4,
            INTERFACE = 5,
        }

        public static string GetScriptTypeString(EScriptType type)
        {
            switch (type)
            {
                case EScriptType.CLASS: return "class";
                case EScriptType.STATIC_CLASS: return "static class";
                case EScriptType.STRUCT: return "struct";
                case EScriptType.ENUM: return "enum";
                case EScriptType.INTERFACE: return "interface";

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region ENUM Protection

        public enum EProtection : byte
        {
            PUBLIC = 0,
            PROTECTED = 1,
            PRIVATE = 2,
            INTERNAL = 3,
        }

        public static string GetProtectionString(EProtection protection)
        {
            switch (protection)
            {
                case EProtection.PUBLIC: return "public";
                case EProtection.PROTECTED: return "protected";
                case EProtection.PRIVATE: return "private";
                case EProtection.INTERNAL: return "internal";

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region INTERFACE Attribute

        public interface IAttribute
        {
            public string GetAttributeAsString();
        }

        public struct SerializeFieldAttribute : IAttribute { public string GetAttributeAsString() => "SerializeField"; }
        public struct HideInInspectorAttribute : IAttribute { public string GetAttributeAsString() => "HideInInspector"; }
        public struct EnumFlagsAttribute : IAttribute { public string GetAttributeAsString() => "Flags"; }
        public struct TooltipAttribute : IAttribute 
        {
            public TooltipAttribute(string tooltipContent) => this.tooltipContent = tooltipContent;

            public readonly string tooltipContent;
            public string GetAttributeAsString() => $"Tooltip(\"{tooltipContent}\")"; 
        }
        public struct RequireComponentAttribute : IAttribute 
        {
            public RequireComponentAttribute(Component component) => this.componentTypeName = component.GetType().Name;

            public readonly string componentTypeName;
            public string GetAttributeAsString() => $"RequireComponent(typeof({componentTypeName}))"; 
        }

        #endregion

        #region STRUCT MethodParameter

        public struct MethodParameter
        {
            public MethodParameter(Type type, string name)
            {
                this.typeName = type.Name;
                this.name = name;
            }
            public MethodParameter(string typeName, string name)
            {
                this.typeName = typeName;
                this.name = name;
            }

            public readonly string typeName;
            public readonly string name;
        }

        #endregion


        #region Members

        protected readonly StringBuilder baseStringBuilder;
        protected readonly StringBuilder usingStringBuilder;
        protected int indentLevel;

        public readonly string scriptNamespace;
        public readonly EProtection scriptProtection;
        public readonly EScriptType scriptType;
        public readonly string scriptName;
        public readonly Type[] parentTypes;
        public readonly IAttribute[] attributes;

        #endregion

        #region Constructor

        public ScriptWriter(string scriptNamespace, EProtection scriptProtection, EScriptType scriptType, string scriptName, 
            Type[] parentTypes = null, IAttribute[] attributes = null, int indentLevel = 0)
        {
            baseStringBuilder = new StringBuilder();
            usingStringBuilder = new StringBuilder();

            this.indentLevel = indentLevel;
            this.scriptProtection = scriptProtection;
            this.scriptType = scriptType;
            this.scriptName = scriptName;
            this.scriptNamespace = scriptNamespace;
            this.parentTypes = parentTypes;
            this.attributes = attributes;

            InitScript();
        }

        #endregion

        #region Initialization

        protected virtual void InitScript()
        {
            // --- NAMESPACE ---
            if (!string.IsNullOrWhiteSpace(scriptNamespace))
            {
                baseStringBuilder.Append("namespace ");
                baseStringBuilder.AppendLine(scriptNamespace);
                OpenBracket();
            }

            // --- ATTRIBUTES ---
            if (TryGetAttributesAsString(out var result, attributes))
            {
                ApplyIndent();
                baseStringBuilder.AppendLine(result);
            }

            // --- DECLARATION ---
            ApplyIndent();
            baseStringBuilder.Append(GetProtectionString(scriptProtection));
            baseStringBuilder.Append(' ');
            // --> public
            baseStringBuilder.Append(GetScriptTypeString(scriptType));
            baseStringBuilder.Append(' ');
            // --> public class
            baseStringBuilder.Append(scriptName);
            // --> public class TestClass
            if (parentTypes.IsValid()
                && scriptType is not EScriptType.STATIC_CLASS)
            {
                baseStringBuilder.Append(" : ");

                for (int i = 0; i < parentTypes.Length; i++)
                {
                    baseStringBuilder.Append(parentTypes[i].Name);
                    if (i < parentTypes.Length - 1)
                    {
                        baseStringBuilder.Append(", "); 
                    }
                }
            }

            baseStringBuilder.AppendLine();
            OpenBracket();
        }

        #endregion

        #region Completion

        protected void CompleteScript(StringBuilder sb)
        {
            // Close script bracket
            CloseBracket(sb);

            if (!string.IsNullOrWhiteSpace(scriptNamespace))
            {
                CloseBracket(sb);
            }
        }

        #endregion

        #region Utility Methods

        protected void ApplyIndent() => ApplyIndent(baseStringBuilder);
        protected void ApplyIndent(StringBuilder sb)
        {
            for (int i = 0; i < indentLevel; i++)
            {
                sb.Append("    ");
            }
        }
        protected void OpenBracket() => OpenBracket(baseStringBuilder);
        protected void OpenBracket(StringBuilder sb)
        {
            ApplyIndent(sb);
            sb.AppendLine("{");
            indentLevel++;
        }
        protected void CloseBracket() => CloseBracket(baseStringBuilder);
        protected void CloseBracket(StringBuilder sb)
        {
            indentLevel--;
            ApplyIndent(sb);
            sb.AppendLine("}");
        }

        protected bool TryGetAttributesAsString(out string result, params IAttribute[] attributes)
        {
            result = null;
            if (!attributes.IsValid()) return false;

            StringBuilder sb = new();

            sb.Append('[');

            for (int i = 0; i < attributes.Length; i++)
            {
                sb.Append(attributes[i].GetAttributeAsString());
                if (i < attributes.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(']');

            result = sb.ToString();
            return true;
        }

        #endregion

        #region Public Set Methods

        public void IncreaseIndentLevel() => indentLevel++;
        public void DecreaseIndentLevel() => indentLevel--;

        /// <remarks>No need to put "using" at the beginning or ";" at the end</remarks>
        public virtual void AppendUsing(string usingName)
        {
            if (!usingName.StartsWith("using")) usingStringBuilder.Append("using ");
            usingStringBuilder.Append(usingName);
            if (!usingName.EndsWith(';')) usingStringBuilder.AppendLine(";");
        }

        /// <summary>
        /// Append a field with optional attributes
        /// </summary>
        /// <remarks><paramref name="type"/> can't be null</remarks>
        public virtual void AppendField(EProtection protection, bool isStatic, Type type, string fieldName, params IAttribute[] attributes)
        {
            ApplyIndent();
            if (TryGetAttributesAsString(out var result, attributes))
            {
                baseStringBuilder.Append(result);
                baseStringBuilder.Append(' ');
            }

            baseStringBuilder.Append(GetProtectionString(protection));
            baseStringBuilder.Append(' ');

            if (isStatic || scriptType == EScriptType.STATIC_CLASS)
            {
                baseStringBuilder.Append("static ");
            }
            
            baseStringBuilder.Append(type.Name);
            baseStringBuilder.Append(' ');

            baseStringBuilder.Append(fieldName);
            baseStringBuilder.AppendLine(";");
        }
        
        /// <summary>
        /// Append a method with the content as an IEnumerable of strings without "\n" at the end, and optional attributes
        /// </summary>
        /// <remarks>If <paramref name="returnType"/> is null, void is used</remarks>
        public virtual void AppendMethod(EProtection protection, bool isStatic, Type returnType, string methodName, 
            bool isExtension, MethodParameter[] parameters, string[] methodContent, params IAttribute[] attributes)
        {
            ApplyIndent();
            if (TryGetAttributesAsString(out var result, attributes))
            {
                baseStringBuilder.Append(result);
                baseStringBuilder.Append(' ');
            }

            baseStringBuilder.Append(GetProtectionString(protection));
            baseStringBuilder.Append(' ');

            if (isStatic || scriptType == EScriptType.STATIC_CLASS)
            {
                baseStringBuilder.Append("static ");
            }
            
            if (returnType != null) baseStringBuilder.Append(returnType.Name);
            else baseStringBuilder.Append("void");
            baseStringBuilder.Append(' ');

            baseStringBuilder.Append(methodName);

            baseStringBuilder.Append('(');
            for (int i = 0; i < parameters.Length; i++)
            {
                if (isExtension && i == 0)
                {
                    baseStringBuilder.Append("this ");
                }
                baseStringBuilder.Append(parameters[i].typeName);
                baseStringBuilder.Append(' ');
                baseStringBuilder.Append(parameters[i].name);
                if (i < parameters.Length - 1)
                {
                    baseStringBuilder.Append(", ");
                }
            }
            baseStringBuilder.AppendLine(")");

            OpenBracket();

            foreach (var contentLine in methodContent)
            {
                ApplyIndent();
                baseStringBuilder.AppendLine(contentLine);
            }

            CloseBracket();
        }

        #endregion

        #region Writing Process

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.Append(usingStringBuilder.ToString());
            sb.AppendLine();
            sb.Append(baseStringBuilder.ToString());
            CompleteScript(sb);

            return sb.ToString();
        }
        public virtual string ToStringWithoutUsings()
        {
            StringBuilder sb = new();

            sb.Append(baseStringBuilder.ToString());
            CompleteScript(sb);

            return sb.ToString();
        }

        #endregion
    }

    #region ENUM Writer

    public class EnumWriter : ScriptWriter
    {
        #region ENUM EnumType

        public enum EEnumType : byte
        {
            BYTE = 0,
            SHORT = 1,
            INT = 2,
            LONG = 3,
            SBYTE = 4,
            USHORT = 5,
            UINT = 6,
            ULONG = 7,
        }

        public static Type GetEnumSystemType(EEnumType enumType)
        {
            switch (enumType) 
            {
                case EEnumType.BYTE: return typeof(byte);
                case EEnumType.SHORT: return typeof(short);
                case EEnumType.INT: return typeof(int);
                case EEnumType.LONG: return typeof(long);
                case EEnumType.SBYTE: return typeof(sbyte);
                case EEnumType.USHORT: return typeof(ushort);
                case EEnumType.UINT: return typeof(uint);
                case EEnumType.ULONG: return typeof(ulong);
            }

            return null;
        }

        #endregion

        #region Members

        public readonly EEnumType enumType;
        public readonly string[] enumContent;
        public readonly bool flags;

        #endregion

        #region Constructors

        public EnumWriter(string enumNamespace, EProtection enumProtection, string enumName, string[] enumContent, EEnumType enumType,
            IAttribute[] attributes = null, int indentLevel = 0)
            : base(enumNamespace, enumProtection, EScriptType.ENUM, enumName, new Type[] { GetEnumSystemType(enumType) }, attributes, indentLevel)
        {
            this.enumType = enumType;
            this.enumContent = enumContent;

            if (attributes.IsValid())
            {
                foreach (var attribute in attributes)
                {
                    if (attribute is EnumFlagsAttribute)
                    {
                        this.flags = true;
                        break;
                    }
                }
            }

            FillEnum();
            usingStringBuilder.AppendLine("using System;");
        }

        #endregion

        #region Enum Content

        private void FillEnum()
        {
            for (int i = 0; i < enumContent.Length; i++)
            {
                ApplyIndent();
                baseStringBuilder.Append(enumContent[i]);
                baseStringBuilder.Append(" = ");
                if (flags)
                {
                    baseStringBuilder.Append("1 << ");
                }
                baseStringBuilder.Append(i);
                baseStringBuilder.AppendLine(",");
            }
        }

        #endregion

        #region Overrides

        public override void AppendUsing(string usingName)
        {
            if (usingName == "System"
                || usingName == "using System"
                || usingName == "using System;"
                || usingName == "System;")
                return;

            base.AppendUsing(usingName);
        }
        public override void AppendMethod(EProtection protection, bool isStatic, Type returnType, string methodName, bool isExtension, MethodParameter[] parameters, string[] methodContent, params IAttribute[] attributes)
        {
            throw new Exception("Can't implement method in ENUM");
        }
        public override void AppendField(EProtection protection, bool isStatic, Type type, string fieldName, params IAttribute[] attributes)
        {
            throw new Exception("Can't implement field in ENUM");
        }

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
}

#endif