﻿namespace AgileObjects.AgileMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    internal static class Constants
    {
        public static readonly Type[] NoTypeArguments = { };

        public static readonly Expression EmptyExpression = Expression.Empty();

        public static readonly BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        public static readonly BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        public static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        public static readonly BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public const string CreateNew = "CreateNew";

        public const string Merge = "Merge";

        public const string Overwrite = "Overwrite";

        #region Numeric Types

        public static readonly IEnumerable<Type> WholeNumberNumericTypes = new[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        public static readonly IEnumerable<Type> NumericTypes =
            WholeNumberNumericTypes
                .Concat(typeof(float), typeof(decimal), typeof(double))
                .ToArray();

        public static readonly IDictionary<Type, double> NumericTypeMaxValuesByType = GetValuesByType("MaxValue");
        public static readonly IDictionary<Type, double> NumericTypeMinValuesByType = GetValuesByType("MinValue");

        private static Dictionary<Type, double> GetValuesByType(string fieldName)
        {
            return NumericTypes
                .ToDictionary(t => t, t => Convert.ToDouble(t.GetField(fieldName).GetValue(null)));
        }

        #endregion
    }
}
