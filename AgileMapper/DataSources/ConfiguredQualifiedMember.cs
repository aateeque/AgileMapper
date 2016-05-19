namespace AgileObjects.AgileMapper.DataSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;
    using Members;
    using ReadableExpressions;

    internal class ConfiguredQualifiedMember : IQualifiedMember
    {
        private readonly Expression _value;
        private readonly int _sourceObjectDepth;
        private readonly IQualifiedMember _matchedTargetMember;
        private readonly IEnumerable<Member> _childMembers;

        public ConfiguredQualifiedMember(Expression value, IMemberMappingContext context)
            : this(value, context.SourceObjectDepth, context.TargetMember)
        {
        }

        private ConfiguredQualifiedMember(Expression value, int sourceObjectDepth, IQualifiedMember matchedTargetMember)
            : this(
                  value.Type,
                  value.ToReadableString(),
                  value,
                  sourceObjectDepth + 1,
                  matchedTargetMember,
                  Enumerable.Empty<Member>())
        {
            IsSimple = value.Type.IsSimple();
            IsEnumerable = !IsSimple && value.Type.IsEnumerable();
            IsComplex = !(IsSimple || IsEnumerable);
        }

        private ConfiguredQualifiedMember(ConfiguredQualifiedMember parent, Member childMember)
            : this(
                  childMember.Type,
                  parent.Name + childMember.MemberName.JoiningName,
                  parent._value,
                  parent._sourceObjectDepth,
                  parent._matchedTargetMember.Append(childMember),
                  parent._childMembers.Concat(childMember).ToArray())
        {
            IsSimple = childMember.IsSimple;
            IsEnumerable = childMember.IsEnumerable;
            IsComplex = childMember.IsComplex;
        }

        private ConfiguredQualifiedMember(
            Type type,
            string name,
            Expression value,
            int sourceObjectDepth,
            IQualifiedMember matchedTargetMember,
            IEnumerable<Member> childMembers)
        {
            Type = type;
            Name = name;
            _value = value;
            _sourceObjectDepth = sourceObjectDepth;
            _matchedTargetMember = matchedTargetMember;
            _childMembers = childMembers;
        }

        public Type DeclaringType => _value.Type.DeclaringType;

        public Type Type { get; }

        public string Name { get; }

        public bool IsComplex { get; }

        public bool IsEnumerable { get; }

        public bool IsSimple { get; }

        public bool ExistingValueCanBeChecked => true;

        public IQualifiedMember Append(Member childMember)
            => new ConfiguredQualifiedMember(this, childMember);

        public IQualifiedMember RelativeTo(int depth)
            => (depth != _sourceObjectDepth) ? new ConfiguredQualifiedMember(_value, depth, _matchedTargetMember) : this;

        public bool IsSameAs(IQualifiedMember otherMember)
            => (otherMember as ConfiguredQualifiedMember)?._value == _value;

        public bool Matches(IQualifiedMember otherMember) => _matchedTargetMember.Matches(otherMember);

        public Expression GetAccess(Expression instance) => _value;

        public Expression GetQualifiedAccess(Expression instance) => _childMembers.GetQualifiedAccess(instance);

        public Expression GetPopulation(Expression instance, Expression value) => Constants.EmptyExpression;

        public IQualifiedMember WithType(Type runtimeType)
        {
            return runtimeType != Type
                ? new ConfiguredQualifiedMember(_value.GetConversionTo(runtimeType), _sourceObjectDepth, _matchedTargetMember)
                : this;
        }
    }
}