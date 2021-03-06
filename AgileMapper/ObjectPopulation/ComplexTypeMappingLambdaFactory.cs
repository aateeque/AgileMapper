namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;
    using Members;

    internal class ComplexTypeMappingLambdaFactory : ObjectMappingLambdaFactoryBase
    {
        private readonly ComplexTypeConstructionFactory _constructionFactory;

        public ComplexTypeMappingLambdaFactory(MapperContext mapperContext)
        {
            _constructionFactory = new ComplexTypeConstructionFactory(mapperContext);
        }

        protected override bool IsNotConstructable(IObjectMapperCreationData data)
            => _constructionFactory.GetNewObjectCreation(data) == null;

        protected override IEnumerable<Expression> GetShortCircuitReturns(GotoExpression returnNull, ObjectMapperData data)
        {
            yield return GetStrategyShortCircuitReturns(returnNull, data);
            yield return GetExistingObjectShortCircuit(returnNull.Target, data);
        }

        private static Expression GetStrategyShortCircuitReturns(Expression returnNull, MemberMapperData data)
        {
            if (!data.SourceMember.Matches(data.TargetMember))
            {
                return Constants.EmptyExpression;
            }

            var shortCircuitConditions = data
                .RuleSet
                .ComplexTypeMappingShortCircuitStrategy
                .GetConditions(data)
                .Select(condition => (Expression)Expression.IfThen(condition, returnNull));

            var shortCircuitBlock = Expression.Block(shortCircuitConditions);

            return shortCircuitBlock;
        }

        private static Expression GetExistingObjectShortCircuit(LabelTarget returnTarget, MemberMapperData data)
        {
            var tryGetCall = Expression.Call(
                Expression.Property(data.Parameter, "MappingContext"),
                MappingContext.TryGetMethod.MakeGenericMethod(data.SourceType, data.InstanceVariable.Type),
                data.SourceObject,
                data.InstanceVariable);

            var ifTryGetReturn = Expression.IfThen(
                tryGetCall,
                Expression.Return(returnTarget, data.InstanceVariable));

            return ifTryGetReturn;
        }

        protected override IEnumerable<Expression> GetObjectPopulation(IObjectMapperCreationData data)
        {
            var mapperData = data.MapperData;

            yield return GetCreationCallback(CallbackPosition.Before, mapperData);

            var instanceVariableValue = GetObjectResolution(data);
            var instanceVariableAssignment = Expression.Assign(mapperData.InstanceVariable, instanceVariableValue);
            yield return instanceVariableAssignment;

            yield return GetCreationCallback(CallbackPosition.After, mapperData);

            yield return GetObjectRegistrationCall(mapperData);

            foreach (var population in GetPopulationsAndCallbacks(data))
            {
                yield return population;
            }
        }

        private static Expression GetCreationCallback(CallbackPosition callbackPosition, MemberMapperData data)
            => GetCallbackOrEmpty(c => c.GetCreationCallbackOrNull(callbackPosition, data), data);

        private Expression GetObjectResolution(IObjectMapperCreationData data)
        {
            var mapperData = data.MapperData;

            var objectCreation = _constructionFactory.GetNewObjectCreation(data);
            var createdObjectAssignment = Expression.Assign(mapperData.CreatedObject, objectCreation);
            var instanceDataTargetAssignment = Expression.Assign(mapperData.TargetObject, createdObjectAssignment);
            var existingOrCreatedObject = Expression.Coalesce(mapperData.TargetObject, instanceDataTargetAssignment);

            return existingOrCreatedObject;
        }

        private static Expression GetObjectRegistrationCall(MemberMapperData data)
        {
            return Expression.Call(
                Expression.Property(data.Parameter, "MappingContext"),
                MappingContext.RegisterMethod.MakeGenericMethod(data.SourceType, data.TargetType),
                data.SourceObject,
                data.InstanceVariable);
        }

        private static IEnumerable<Expression> GetPopulationsAndCallbacks(IObjectMapperCreationData data)
        {
            var sourceMemberTypeTests = new List<Expression>();

            foreach (var memberPopulation in MemberPopulationFactory.Create(data))
            {
                if (!memberPopulation.IsSuccessful)
                {
                    yield return memberPopulation.GetPopulation();
                    continue;
                }

                var prePopulationCallback = GetPopulationCallbackOrEmpty(CallbackPosition.Before, memberPopulation, data);

                if (prePopulationCallback != Constants.EmptyExpression)
                {
                    yield return prePopulationCallback;
                }

                yield return memberPopulation.GetPopulation();

                var postPopulationCallback = GetPopulationCallbackOrEmpty(CallbackPosition.After, memberPopulation, data);

                if (postPopulationCallback != Constants.EmptyExpression)
                {
                    yield return postPopulationCallback;
                }

                if (memberPopulation.SourceMemberTypeTest != null)
                {
                    sourceMemberTypeTests.Add(memberPopulation.SourceMemberTypeTest);
                }
            }

            CreateSourceMemberTypeTesterIfRequired(sourceMemberTypeTests, data);
        }

        private static Expression GetPopulationCallbackOrEmpty(
            CallbackPosition position,
            IMemberPopulation memberPopulation,
            IObjectMapperCreationData data)
        {
            return GetCallbackOrEmpty(
                c => c.GetCallbackOrNull(position, memberPopulation.MapperData, data.MapperData),
                data.MapperData);
        }

        private static void CreateSourceMemberTypeTesterIfRequired(ICollection<Expression> typeTests, IObjectMapperCreationData data)
        {
            if (typeTests.None())
            {
                return;
            }

            var typeTest = typeTests.AndTogether();
            var typeTestLambda = Expression.Lambda<Func<IMappingData, bool>>(typeTest, Parameters.MappingData);

            data.MapperData.MapperKey.AddSourceMemberTypeTester(typeTestLambda.Compile());
        }

        protected override Expression GetReturnValue(ObjectMapperData data) => data.InstanceVariable;

        public void Reset() => _constructionFactory.Reset();
    }
}