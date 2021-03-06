namespace AgileObjects.AgileMapper.TypeConversion
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    internal abstract class TryParseConverterBase : IValueConverter
    {
        private readonly ToStringConverter _toStringConverter;
        private readonly Type _nonNullableTargetType;
        private readonly Type _nullableTargetType;
        private readonly MethodInfo _tryParseMethod;
        private readonly ParameterExpression _valueVariable;

        protected TryParseConverterBase(
            ToStringConverter toStringConverter,
            Type nonNullableTargetType)
        {
            _toStringConverter = toStringConverter;
            _nonNullableTargetType = nonNullableTargetType;
            _nullableTargetType = typeof(Nullable<>).MakeGenericType(nonNullableTargetType);

            _tryParseMethod = nonNullableTargetType
                .GetMethods(Constants.PublicStatic)
                .First(m => (m.Name == "TryParse") && (m.GetParameters().Length == 2));

            _valueVariable = Expression.Variable(
                nonNullableTargetType,
                nonNullableTargetType.GetVariableName(f => f.InCamelCase) + "Value");
        }

        public bool IsFor(Type nonNullableTargetType)
            => nonNullableTargetType == _nonNullableTargetType;

        public virtual bool CanConvert(Type nonNullableSourceType)
            => (nonNullableSourceType == _nonNullableTargetType) || (nonNullableSourceType == typeof(object));

        public virtual Expression GetConversion(Expression sourceValue, Type targetType)
        {
            if (sourceValue.Type == _nullableTargetType)
            {
                return sourceValue.GetToValueOrDefaultCall();
            }

            if (sourceValue.Type != typeof(string))
            {
                sourceValue = _toStringConverter.GetConversion(sourceValue);
            }

            var tryParseCall = Expression.Call(_tryParseMethod, sourceValue, _valueVariable);
            var successfulParseReturnValue = _valueVariable.GetConversionTo(targetType);
            var defaultValue = Expression.Default(targetType);
            var parsedValueOrDefault = Expression.Condition(tryParseCall, successfulParseReturnValue, defaultValue);
            var tryParseBlock = Expression.Block(new[] { _valueVariable }, parsedValueOrDefault);

            return tryParseBlock;
        }
    }
}