﻿namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System.Linq.Expressions;

    internal abstract class EnumerablePopulationStrategyBase : IEnumerablePopulationStrategy
    {
        public Expression GetPopulation(ObjectMapperData data)
        {
            if (data.SourceMember.IsEnumerable)
            {
                return GetEnumerablePopulation(data.EnumerablePopulationBuilder);
            }

            return Constants.EmptyExpression;
        }

        protected abstract Expression GetEnumerablePopulation(EnumerablePopulationBuilder builder);
    }
}
