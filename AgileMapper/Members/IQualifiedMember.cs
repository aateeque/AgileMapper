namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Linq.Expressions;

    internal interface IQualifiedMember
    {
        Type Type { get; }

        bool IsEnumerable { get; }

        string Name { get; }

        string GetPath();

        IQualifiedMember Append(Member childMember);

        IQualifiedMember RelativeTo(IQualifiedMember otherMember);

        IQualifiedMember WithType(Type runtimeType);

        bool CouldMatch(QualifiedMember otherMember);

        bool Matches(QualifiedMember otherMember);

        Expression GetQualifiedAccess(Expression instance);
    }
}