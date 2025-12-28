using System;
using System.Reflection;
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

        public enum EParameter
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
        public readonly EParameter[] parameters;
        public readonly Action<object[]> callback;

        #endregion

        #region Constructors

        public ConsoleCommand(string name, EScope scope, EParameter[] parameters, Action<object[]> callback)
        {
            this.name = name;
            this.scope = scope;
            this.parameters = parameters;
            this.callback = callback;
        }

        public ConsoleCommand(string name, EScope scope, MethodInfo methodInfo)
        {
            this.name = name;
            this.scope = scope;

            var methodParameters = methodInfo.GetParameters();
            if (methodParameters.IsValid())
            {
                this.parameters = new EParameter[methodParameters.Length];
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

            this.callback = (parameters) => methodInfo.Invoke(null, parameters);
        }

        #endregion


        // --- STATIC ---

        #region Static Utility

        public static bool TryParseParameterInfo(ParameterInfo parameterInfo, out EParameter parameter)
        {
            var type = parameterInfo.ParameterType;

            if (type == typeof(bool))
            {
                parameter = EParameter.BOOL;
                return true;
            }
            if (type == typeof(int))
            {
                parameter = EParameter.INT;
                return true;
            }
            if (type == typeof(float))
            {
                parameter = EParameter.FLOAT;
                return true;
            }
            if (type == typeof(string))
            {
                parameter = EParameter.STRING;
                return true;
            }
            if (type.IsEnum)
            {
                parameter = EParameter.ENUM;
                return true;
            }
            if (type == typeof(Vector2))
            {
                parameter = EParameter.VECTOR2;
                return true;
            }
            if (type == typeof(Vector2Int))
            {
                parameter = EParameter.VECTOR2INT;
                return true;
            }
            if (type == typeof(Vector3))
            {
                parameter = EParameter.VECTOR3;
                return true;
            }
            if (type == typeof(Vector3Int))
            {
                parameter = EParameter.VECTOR3INT;
                return true;
            }
            if (type == typeof(Color))
            {
                parameter = EParameter.COLOR;
                return true;
            }

            parameter = 0;
            return false;
        }

        #endregion
    }
}
