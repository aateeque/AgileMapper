﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using Microsoft.CSharp.RuntimeBinder;
    using TestClasses;
    using Xunit;
    using Shouldly;

    public class WhenFlatteningObjects
    {
        [Fact]
        public void ShouldIncludeASimpleTypeMember()
        {
            var source = new PublicProperty<string> { Value = "Flatten THIS" };
            var result = Mapper.Flatten(source);

            ((object)result).ShouldNotBeNull();
            ((string)result.Value).ShouldBe("Flatten THIS");
        }

        [Fact]
        public void ShouldIncludeASimpleTypeEnumerableMember()
        {
            var source = new PublicProperty<long[]> { Value = new[] { 1L, 2L, 3L } };
            var result = Mapper.Flatten(source);

            ((object)result).ShouldNotBeNull();
            ((long[])result.Value).ShouldBe(1, 2, 3);
        }

        [Fact]
        public void ShouldNotIncludeComplexTypeMembers()
        {
            var source = new PublicProperty<PublicField<int>> { Value = new PublicField<int> { Value = 9876 } };
            var result = Mapper.Flatten(source);

            Assert.Throws<RuntimeBinderException>(() => result.Value);
        }

        [Fact]
        public void ShouldIncludeANestedSimpleTypeMember()
        {
            var source = new PublicProperty<PublicField<int>> { Value = new PublicField<int> { Value = 1234 } };
            var result = Mapper.Flatten(source);

            ((int)result.Value_Value).ShouldBe(1234);
        }

        [Fact]
        public void ShouldHandleANullComplexTypeMember()
        {
            var source = new PublicProperty<PublicField<int>> { Value = null };
            var result = Mapper.Flatten(source);

            ((object)result.Value_Value).ShouldBeNull();
        }
    }
}
