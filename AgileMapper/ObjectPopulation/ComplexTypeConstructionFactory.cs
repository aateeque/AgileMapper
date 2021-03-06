namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Caching;
    using DataSources;
    using Extensions;
    using Members;

    internal class ComplexTypeConstructionFactory
    {
        private readonly ICache<ConstructionKey, Expression> _constructorsCache;

        public ComplexTypeConstructionFactory(MapperContext mapperContext)
        {
            _constructorsCache = mapperContext.Cache.CreateScoped<ConstructionKey, Expression>();
        }

        public Expression GetNewObjectCreation(IObjectMapperCreationData data)
        {
            return _constructorsCache.GetOrAdd(new ConstructionKey(data), key =>
            {
                var mapperData = key.Data.MapperData;

                var constructions = new List<Construction>();
                var newingConstructorRequired = true;

                var configuredFactories = mapperData
                    .MapperContext
                    .UserConfigurations
                    .GetObjectFactories(mapperData);

                foreach (var configuredFactory in configuredFactories)
                {
                    var configuredConstruction = new Construction(
                        configuredFactory.Create(mapperData),
                        configuredFactory.GetConditionOrNull(mapperData));

                    constructions.Insert(0, configuredConstruction);

                    if (!configuredFactory.HasConfiguredCondition)
                    {
                        newingConstructorRequired = false;
                        break;
                    }
                }

                if (newingConstructorRequired)
                {
                    var greediestAvailableConstructor = mapperData.InstanceVariable.Type
                        .GetConstructors(Constants.PublicInstance)
                        .Select(ctor => new ConstructorData(
                            ctor,
                            ctor.GetParameters()
                                .Select(p =>
                                {
                                    var parameterMapperData = new MemberMapperData(
                                        key.Data.TargetMember.Append(Member.ConstructorParameter(p)),
                                        mapperData);

                                    return key.Data.GetChildCreationData(parameterMapperData);
                                })
                                .Select(memberData => mapperData
                                    .MapperContext
                                    .DataSources
                                    .FindFor(memberData))
                                .ToArray()))
                        .Where(ctor => ctor.CanBeConstructed)
                        .OrderByDescending(ctor => ctor.NumberOfParameters)
                        .FirstOrDefault();

                    if (greediestAvailableConstructor != null)
                    {
                        constructions.Insert(0, greediestAvailableConstructor.Construction);
                    }
                }

                key.RemoveCreationData();

                if (constructions.None())
                {
                    return null;
                }

                return constructions
                    .Skip(1)
                    .Aggregate(
                        constructions.First().Expression,
                        (constructionSoFar, construction) =>
                            Expression.Condition(construction.Condition, construction.Expression, constructionSoFar));
            });
        }

        public void Reset() => _constructorsCache.Empty();

        #region Helper Classes

        private class ConstructionKey
        {
            private readonly MappingRuleSet _ruleSet;
            private readonly IQualifiedMember _sourceMember;
            private readonly QualifiedMember _targetMember;

            public ConstructionKey(IObjectMapperCreationData data)
            {
                _ruleSet = data.RuleSet;
                _sourceMember = data.SourceMember;
                _targetMember = data.TargetMember;
                Data = data;
            }

            public IObjectMapperCreationData Data { get; private set; }

            public override bool Equals(object obj)
            {
                var otherKey = (ConstructionKey)obj;

                return (otherKey._ruleSet == _ruleSet) &&
                    (otherKey._sourceMember == _sourceMember) &&
                    (otherKey._targetMember == _targetMember);
            }

            public override int GetHashCode() => 0;

            public void RemoveCreationData() => Data = null;
        }

        private class ConstructorData
        {
            public ConstructorData(ConstructorInfo constructor, ICollection<DataSourceSet> argumentDataSources)
            {
                CanBeConstructed = argumentDataSources.All(ds => ds.HasValue);
                NumberOfParameters = argumentDataSources.Count;

                if (!CanBeConstructed)
                {
                    return;
                }

                var variables = new List<ParameterExpression>();
                var argumentValues = new List<Expression>(NumberOfParameters);

                foreach (var argumentDataSource in argumentDataSources)
                {
                    variables.AddRange(argumentDataSource.Variables);
                    argumentValues.Add(argumentDataSource.Value);
                }

                var objectConstruction = Expression.New(constructor, argumentValues);

                Construction = variables.None()
                    ? new Construction(objectConstruction)
                    : new Construction(Expression.Block(variables, objectConstruction));
            }

            public bool CanBeConstructed { get; }

            public int NumberOfParameters { get; }

            public Construction Construction { get; }
        }

        private class Construction
        {
            public Construction(Expression construction, Expression condition = null)
            {
                Expression = construction;
                Condition = condition;
            }

            public Expression Expression { get; }

            public Expression Condition { get; }
        }

        #endregion
    }
}