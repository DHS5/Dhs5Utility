using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public class ConsoleCommand
    {
        #region ENUM Scope

        public enum EScope
        {
            RUNTIME = 0,
            EDITOR = 1,
            BOTH = 2,
        }

        #endregion

        #region ENUM Parameter

        public enum EParameterType
        {
            BOOL,
            INT,
            FLOAT,
            STRING,
            ENUM,
            VECTOR2,
            VECTOR2INT,
            VECTOR3,
            VECTOR3INT,
            COLOR,
        }

        #endregion

        #region STRUCT Parameter

        public struct Parameter
        {
            public Parameter(EParameterType type, Type underlyingType)
            {
                this.type = type;
                this.underlyingType = underlyingType;
                this.hasDefaultValue = false;
                this.defaultValue = null;
            }
            public Parameter(EParameterType type, Type underlyingType, object defaultValue)
            {
                this.type = type;
                this.underlyingType = underlyingType;
                this.hasDefaultValue = true;
                this.defaultValue = defaultValue;
            }

            public readonly EParameterType type;
            public readonly Type underlyingType;
            public readonly bool hasDefaultValue;
            public readonly object defaultValue;
        }

        #endregion

        #region ENUM MatchResult

        public enum EMatchResult
        {
            NO_MATCH = 0,
            NAME_MATCH = 1,
            PARTIAL_MATCH = 2,
            PERFECT_MATCH = 3,
        }

        #endregion

        #region EXCEPTION InvalidTypeParameter

        public class InvalidTypeParameterException : Exception
        {
            public Type type;

            public InvalidTypeParameterException(Type type) : base() { this.type = type; }
            public InvalidTypeParameterException(Type type, string message) : base(message) { this.type = type; }
            public InvalidTypeParameterException(Type type, string message, Exception inner) : base(message, inner) { this.type = type; }

            public override string ToString()
            {
                return "Can't use type " + type.Name + " as ConsoleCommand parameter.";
            }
        }

        #endregion


        #region Members

        public readonly string name;
        public readonly EScope scope;
        public readonly Parameter[] parameters;
        public readonly Action<object[]> callback;

        public readonly string optionString;
        public readonly string hintString;

        #endregion

        #region Constructors

        public ConsoleCommand(string name, EScope scope, Parameter[] parameters, Action<object[]> callback)
        {
            this.name = name;
            this.scope = scope;
            this.parameters = parameters != null ? parameters : new Parameter[0];
            this.callback = callback;

            this.optionString = GetOptionString();
            this.hintString = GetHintString();
        }

        public ConsoleCommand(string name, EScope scope, MethodInfo methodInfo)
        {
            this.name = name;
            this.scope = scope;

            var methodParameters = methodInfo.GetParameters();
            if (methodParameters.IsValid())
            {
                this.parameters = new Parameter[methodParameters.Length];
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    if (TryParseParameterInfo(methodParameters[i], out var parameter))
                    {
                        this.parameters[i] = parameter;
                    }
                    else
                    {
                        throw new InvalidTypeParameterException(methodParameters[i].ParameterType);
                    }
                }
            }
            else
            {
                this.parameters = new Parameter[0];
            }

            this.callback = (parameters) => methodInfo.Invoke(null, parameters);

            this.optionString = GetOptionString();
            this.hintString = GetHintString();
        }

        #endregion

        #region Matching

        public EMatchResult IsMatch(string[] commandLineContentAsArray)
        {
            // To see options when typing a blank space
            if (!commandLineContentAsArray.IsValid()) return EMatchResult.PARTIAL_MATCH;

            if (commandLineContentAsArray.Length > 1)
            {
                if (string.Equals(name, commandLineContentAsArray[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    if (commandLineContentAsArray.Length - 1 > parameters.Length)
                    {
                        return EMatchResult.NAME_MATCH;
                    }
                    for (int i = 1; i < commandLineContentAsArray.Length; i++)
                    {
                        var matchResult = IsParameterMatch(parameters[i - 1], commandLineContentAsArray[i]);

                        // Not a perfect match before the last element --> NAME_MATCH
                        if (matchResult != EMatchResult.PERFECT_MATCH && i < commandLineContentAsArray.Length - 1)
                        {
                            return EMatchResult.NAME_MATCH;
                        }

                        if (i == commandLineContentAsArray.Length - 1)
                        {
                            if (matchResult is EMatchResult.NO_MATCH)
                            {
                                return EMatchResult.NAME_MATCH;
                            }
                            if (commandLineContentAsArray.Length - 1 == parameters.Length)
                            {
                                return matchResult;
                            }
                            return EMatchResult.PARTIAL_MATCH;
                        }
                    }
                }
            }
            else if (string.Equals(name, commandLineContentAsArray[0], StringComparison.InvariantCultureIgnoreCase))
            {
                return parameters.Length > 0 ? EMatchResult.PARTIAL_MATCH : EMatchResult.PERFECT_MATCH;
            }
            else if (name.StartsWith(commandLineContentAsArray[0], StringComparison.InvariantCultureIgnoreCase))
            {
                return EMatchResult.PARTIAL_MATCH;
            }
            return EMatchResult.NO_MATCH;
        }

        #endregion

        #region Run

        public void Run(string[] stringParameters)
        {
            if (!stringParameters.IsValid())
            {
                Run(commandParameters: null);
                return;
            }

            object[] commandParameters = new object[stringParameters.Length];

            for (int i = 0; i < commandParameters.Length; i++)
            {
                var parsedParam = ParseParameter(parameters[i], stringParameters[i]);

                if (parsedParam == null)
                {
                    Debug.LogError(stringParameters[i] + " parsed as null (parameter " + i + ")  in command " + name);
                    return;
                }

                commandParameters[i] = parsedParam;
            }

            Run(commandParameters);
        }
        public void Run(object[] commandParameters)
        {
            try
            {
                callback.Invoke(commandParameters);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion


        #region Utility

        private string GetOptionString()
        {
            if (!parameters.IsValid()) return name;

            StringBuilder sb = new();

            sb.Append(name);
            sb.Append(' ');

            for (int i = 0; i < parameters.Length; i++)
            {
                sb.Append(parameters[i].type.ToString());
                if (i < parameters.Length - 1) sb.Append(' ');
            }

            return sb.ToString();
        }
        private string GetHintString()
        {
            if (!parameters.IsValid()) return name;

            StringBuilder sb = new();

            sb.Append(name);
            sb.Append(' ');

            for (int i = 0; i < parameters.Length; i++)
            {
                sb.Append(GetParameterDefaultValueAsString(parameters[i]));
                if (i < parameters.Length - 1) sb.Append(' ');
            }

            return sb.ToString();
        }

        #endregion

        #region Equality

        public override int GetHashCode()
        {
            return HashCode.Combine(name, parameters);
        }

        #endregion


        // --- STATIC ---

        #region Static ParameterInfo Parsing

        public static bool TryParseParameterInfo(ParameterInfo parameterInfo, out Parameter parameter)
        {
            var hasDefaultValue = parameterInfo.RawDefaultValue != DBNull.Value;

            var type = parameterInfo.ParameterType;
            EParameterType parameterType;

            if (type == typeof(bool))
            {
                parameterType = EParameterType.BOOL;
            }
            else if (type == typeof(int))
            {
                parameterType = EParameterType.INT;
            }
            else if (type == typeof(float))
            {
                parameterType = EParameterType.FLOAT;
            }
            else if (type == typeof(string))
            {
                parameterType = EParameterType.STRING;
            }
            else if (type.IsEnum)
            {
                parameterType = EParameterType.ENUM;
            }
            else if (type == typeof(Vector2))
            {
                parameterType = EParameterType.VECTOR2;
            }
            else if (type == typeof(Vector2Int))
            {
                parameterType = EParameterType.VECTOR2INT;
            }
            else if (type == typeof(Vector3))
            {
                parameterType = EParameterType.VECTOR3;
            }
            else if (type == typeof(Vector3Int))
            {
                parameterType = EParameterType.VECTOR3INT;
            }
            else if (type == typeof(Color))
            {
                parameterType = EParameterType.COLOR;
            }
            else
            {
                parameter = default;
                return false;
            }

            if (hasDefaultValue)
            {
                parameter = new Parameter(parameterType, type, parameterInfo.RawDefaultValue);
            }
            else
            {
                parameter = new Parameter(parameterType, type);
            }
            return true;
        }

        #endregion

        #region Static Parameter Parsing

        public static EMatchResult IsParameterMatch(Parameter parameter, string parameterString)
        {
            switch (parameter.type)
            {
                case EParameterType.BOOL:
                    if (string.Equals("true", parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals("false", parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || "true".StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || "false".StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || parameterString == "0" || parameterString == "1")
                    {
                        return EMatchResult.PERFECT_MATCH;
                    }
                    return EMatchResult.NO_MATCH;

                case EParameterType.INT:
                    return int.TryParse(parameterString, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) 
                        ? EMatchResult.PERFECT_MATCH : EMatchResult.NO_MATCH;
                
                case EParameterType.FLOAT:
                    return float.TryParse(parameterString, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                        ? EMatchResult.PERFECT_MATCH : EMatchResult.NO_MATCH;

                case EParameterType.STRING: return EMatchResult.PERFECT_MATCH;

                case EParameterType.ENUM:
                    bool isInt = int.TryParse(parameterString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intRes);
                    var values = Enum.GetValues(parameter.underlyingType);
                    if (values.Length > 0)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (isInt && intRes == (int)values.GetValue(i)) 
                                return EMatchResult.PERFECT_MATCH;
                            var str = values.GetValue(i).ToString();
                            if (string.Equals(str, parameterString, StringComparison.InvariantCultureIgnoreCase))
                                return EMatchResult.PERFECT_MATCH;
                            if (str.StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase))
                                return EMatchResult.PARTIAL_MATCH;
                        }

                        if (isInt)
                        {
                            for (int i = 0; i < values.Length; i++)
                            {
                                var str = ((int)values.GetValue(i)).ToString();
                                if (str.StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase))
                                    return EMatchResult.PARTIAL_MATCH;
                            }
                        }
                    }
                    return EMatchResult.NO_MATCH;

                case EParameterType.VECTOR2:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length > 2) return EMatchResult.NO_MATCH;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (!float.TryParse(arguments[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                                return EMatchResult.NO_MATCH;
                        }
                        return arguments.Length == 2 ? EMatchResult.PERFECT_MATCH : EMatchResult.PARTIAL_MATCH;
                    }
                
                case EParameterType.VECTOR2INT:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length > 2) return EMatchResult.NO_MATCH;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (!int.TryParse(arguments[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                                return EMatchResult.NO_MATCH;
                        }
                        return arguments.Length == 2 ? EMatchResult.PERFECT_MATCH : EMatchResult.PARTIAL_MATCH;
                    }

                case EParameterType.VECTOR3:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length > 3) return EMatchResult.NO_MATCH;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (!float.TryParse(arguments[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                                return EMatchResult.NO_MATCH;
                        }
                        return arguments.Length == 3 ? EMatchResult.PERFECT_MATCH : EMatchResult.PARTIAL_MATCH;
                    }

                case EParameterType.VECTOR3INT:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length > 3) return EMatchResult.NO_MATCH;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (!int.TryParse(arguments[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                                return EMatchResult.NO_MATCH;
                        }
                        return arguments.Length == 3 ? EMatchResult.PERFECT_MATCH : EMatchResult.PARTIAL_MATCH;
                    }

                case EParameterType.COLOR:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length > 4) return EMatchResult.NO_MATCH;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (!float.TryParse(arguments[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                                return EMatchResult.NO_MATCH;
                        }
                        return (arguments.Length == 3 || arguments.Length == 4) ? EMatchResult.PERFECT_MATCH : EMatchResult.PARTIAL_MATCH;
                    }

                default: throw new NotImplementedException();
            }
        }

        public static object ParseParameter(Parameter parameter, string parameterString)
        {
            switch (parameter.type)
            {
                case EParameterType.BOOL:
                    if (string.Equals("true", parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || "true".StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || parameterString == "1")
                    {
                        return true;
                    }
                    else if (string.Equals("false", parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || "false".StartsWith(parameterString, StringComparison.InvariantCultureIgnoreCase)
                        || parameterString == "0")
                    {
                        return false;
                    }
                    return null;

                case EParameterType.INT:
                    if (int.TryParse(parameterString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intRes))
                    {
                        return intRes;
                    }
                    return null;

                case EParameterType.FLOAT:
                    if (float.TryParse(parameterString, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatRes))
                    {
                        return floatRes;
                    }
                    return null;

                case EParameterType.STRING: return parameterString;

                case EParameterType.ENUM:
                    {
                        if (Enum.TryParse(parameter.underlyingType, parameterString, true, out var result))
                            return result;

                        else if (int.TryParse(parameterString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            return Enum.ToObject(parameter.underlyingType, intValue);

                        return null;
                    }

                case EParameterType.VECTOR2:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length == 2
                            && float.TryParse(arguments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                            && float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                        {
                            return new Vector2(x, y);
                        }
                        return null;
                    }

                case EParameterType.VECTOR2INT:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length == 2
                            && int.TryParse(arguments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                            && int.TryParse(arguments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                        {
                            return new Vector2Int(x, y);
                        }
                        return null;
                    }

                case EParameterType.VECTOR3:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length == 3
                            && float.TryParse(arguments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                            && float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                            && float.TryParse(arguments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                        {
                            return new Vector3(x, y, z);
                        }
                        return null;
                    }

                case EParameterType.VECTOR3INT:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length == 3
                            && int.TryParse(arguments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                            && int.TryParse(arguments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)
                            && int.TryParse(arguments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
                        {
                            return new Vector3Int(x, y, z);
                        }
                        return null;
                    }

                case EParameterType.COLOR:
                    {
                        var arguments = parameterString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (arguments.Length == 3
                            && float.TryParse(arguments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r)
                            && float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g)
                            && float.TryParse(arguments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
                        {
                            return new Color(r, g, b);
                        }
                        if (arguments.Length == 4
                            && float.TryParse(arguments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r2)
                            && float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g2)
                            && float.TryParse(arguments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b2)
                            && float.TryParse(arguments[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                        {
                            return new Color(r2, g2, b2, a);
                        }
                        return null;
                    }

                default: throw new NotImplementedException();
            }
        }

        #endregion

        #region Static Parameter Default Value

        public static string GetParameterDefaultValueAsString(Parameter parameter)
        {
            if (parameter.hasDefaultValue) return parameter.defaultValue.ToString();

            switch (parameter.type)
            {
                case EParameterType.BOOL: return "false";
                case EParameterType.INT: return "0";
                case EParameterType.FLOAT: return "0.0";
                case EParameterType.STRING: return "_";
                case EParameterType.ENUM: return Enum.GetName(parameter.underlyingType, 0);
                case EParameterType.VECTOR2: return "0,0";
                case EParameterType.VECTOR2INT: return "0,0";
                case EParameterType.VECTOR3: return "0,0,0";
                case EParameterType.VECTOR3INT: return "0,0,0";
                case EParameterType.COLOR: return "0,0,0,0";
                default: throw new NotImplementedException();
            }
        }

        #endregion
    }
}
