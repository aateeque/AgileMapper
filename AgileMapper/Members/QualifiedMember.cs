namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using Caching;
    using Extensions;

    [DebuggerDisplay("{GetPath()}")]
    internal class QualifiedMember : IQualifiedMember
    {
        public static readonly QualifiedMember All = new QualifiedMember(new Member[0], new string[0], MapperContext.WithDefaultNamingSettings);
        public static readonly QualifiedMember None = new QualifiedMember(new Member[0], new string[0], MapperContext.WithDefaultNamingSettings);

        private readonly Member[] _memberChain;
        private readonly string[] _memberMatchingNames;
        private readonly MapperContext _mapperContext;
        private readonly Func<string> _pathFactory;
        private readonly ICache<Type, QualifiedMember> _runtimeTypedMemberCache;
        private readonly ICache<Member, QualifiedMember> _childMemberCache;

        private QualifiedMember(Member[] memberChain, string[] memberMatchingNames, MapperContext mapperContext)
            : this(memberChain.LastOrDefault(), mapperContext)
        {
            _memberChain = memberChain;
            _memberMatchingNames = memberMatchingNames;
            JoinedNames = mapperContext.NamingSettings.GetJoinedNamesFor(memberMatchingNames);
            _pathFactory = () => _memberChain.GetFullName();
        }

        private QualifiedMember(Member member, QualifiedMember parent, MapperContext mapperContext)
            : this(member, mapperContext)
        {
            var matchingName = mapperContext.NamingSettings.GetMatchingNameFor(member);

            if (parent == null)
            {
                _memberChain = new[] { member };
                _memberMatchingNames = new[] { matchingName };
                JoinedNames = _memberMatchingNames;
                _pathFactory = () => _memberChain[0].JoiningName;
                return;
            }

            _memberChain = parent._memberChain.Append(member);
            _memberMatchingNames = parent._memberMatchingNames.Append(matchingName);
            JoinedNames = mapperContext.NamingSettings.GetJoinedNamesFor(_memberMatchingNames);

            _pathFactory = () => parent.GetPath() + member.JoiningName;
        }

        private QualifiedMember(Member leafMember, MapperContext mapperContext)
        {
            LeafMember = leafMember;
            _mapperContext = mapperContext;
            _runtimeTypedMemberCache = mapperContext.Cache.CreateNew<Type, QualifiedMember>();
            _childMemberCache = mapperContext.Cache.CreateNew<Member, QualifiedMember>();
        }

        #region Factory Method

        public static QualifiedMember From(Member member, MapperContext mapperContext)
            => new QualifiedMember(member, null, mapperContext);

        public static QualifiedMember From(Member[] memberChain, MapperContext mapperContext)
        {
            var matchingNames = memberChain.Select(mapperContext.NamingSettings.GetMatchingNameFor).ToArray();

            return new QualifiedMember(memberChain, matchingNames, mapperContext);
        }

        #endregion

        public IEnumerable<Member> MemberChain => _memberChain;

        public Member LeafMember { get; }

        public Type Type => LeafMember?.Type;

        public Type ElementType => LeafMember?.ElementType;

        public string Name => LeafMember.Name;

        public IEnumerable<string> JoinedNames { get; }

        public string GetPath() => _pathFactory.Invoke();

        public bool IsComplex => LeafMember.IsComplex;

        public bool IsEnumerable => LeafMember.IsEnumerable;

        public bool IsSimple => LeafMember.IsSimple;

        public bool IsReadable => LeafMember.IsReadable;

        IQualifiedMember IQualifiedMember.Append(Member childMember) => Append(childMember);

        public QualifiedMember Append(Member childMember)
            => _childMemberCache.GetOrAdd(childMember, cm => new QualifiedMember(cm, this, _mapperContext));

        public IQualifiedMember RelativeTo(IQualifiedMember otherMember)
        {
            var otherQualifiedMember = (QualifiedMember)otherMember;

            if (otherQualifiedMember.LeafMember == _memberChain[0])
            {
                return this;
            }

            var relativeMemberChain = _memberChain.RelativeTo(otherQualifiedMember._memberChain);

            return new QualifiedMember(relativeMemberChain, _memberMatchingNames, _mapperContext);
        }

        IQualifiedMember IQualifiedMember.WithType(Type runtimeType) => WithType(runtimeType);

        public QualifiedMember WithType(Type runtimeType)
        {
            if (runtimeType == Type)
            {
                return this;
            }

            var runtimeTypedMember = _runtimeTypedMemberCache.GetOrAdd(runtimeType, rt =>
            {
                _memberChain[_memberChain.Length - 1] = LeafMember.WithType(rt);

                return new QualifiedMember(_memberChain, _memberMatchingNames, _mapperContext);
            });

            return runtimeTypedMember;
        }

        public bool IsSameAs(QualifiedMember otherMember)
        {
            if (this == otherMember)
            {
                return true;
            }

            if ((this == All) || (otherMember == All))
            {
                return true;
            }

            if ((this == None) || (otherMember == None))
            {
                return false;
            }

            return (otherMember.Type == Type) &&
                   (otherMember.Name == Name) &&
                   otherMember.LeafMember.DeclaringType.IsAssignableFrom(LeafMember.DeclaringType);
        }

        public bool CouldMatch(QualifiedMember otherMember) => JoinedNames.CouldMatch(otherMember.JoinedNames);

        public bool Matches(QualifiedMember otherMember) => JoinedNames.Match(otherMember.JoinedNames);

        public Expression GetAccess(Expression instance) => LeafMember.GetAccess(instance);

        public Expression GetQualifiedAccess(Expression instance)
            => _memberChain.GetQualifiedAccess(instance);
    }
}