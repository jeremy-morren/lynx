using System.Runtime.CompilerServices;
using Lynx.EfCore.Chains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Tests;

public class EfCoreEntityChainTests
{
    [Theory]
    [InlineData(typeof(Entity1))]
    [InlineData(typeof(Entity2))]
    [InlineData(typeof(Entity3))]
    [InlineData(typeof(Child))]
    [InlineData(typeof(Foreign))]
    [InlineData(typeof(Alone))]
    [InlineData(typeof(ManuallyDefinedReference))]
    public void CacheChains(Type type)
    {
        using var _ = CreateContext(out var model, $"{nameof(CacheChains)}_{type.Name}");

        model.GetReferencingNavigations(type).Should().BeSameAs(model.GetReferencingNavigations(type));
        model.GetReferencingForeignKeys(type).Should().BeSameAs(model.GetReferencingForeignKeys(type));
    }

    [Fact]
    public void CheckReferencingNavigations()
    {
        using var _ = CreateContext(out var model);

        // Entity1: referenced by Entity2 and itself
        model.GetNavigationChains(typeof(Entity1))
            .Select(e => e.Root.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1), typeof(Entity2), typeof(Entity2)]);

        // Entity2: referenced by Entity1 and itself
        model.GetNavigationChains(typeof(Entity2))
            .Select(e => e.Root.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1), typeof(Entity2), typeof(Entity1)]);

        // Entity3 and Child: referenced by Entity1 and Entity2 and itself
        model.GetNavigationChains(typeof(Entity3))
            .Select(e => e.Root.ClrType)
            .Distinct()
            .Should().BeEquivalentTo([typeof(Entity2), typeof(Entity1), typeof(Entity3)]);

        // Alone: not referenced by any entity
        model.GetNavigationChains(typeof(Alone)).ShouldBeEmpty();

        // Child: Referenced as a collection by Entity2
        model.GetNavigationChains(typeof(Child))
            .Select(e => e.Root.ClrType)
            .Distinct()
            .Should().BeEquivalentTo([typeof(ParentEntity), typeof(Entity1), typeof(Entity2)]);

        // Foreign: referenced by Owned, which is owned by Entity3
        // NB: Owned itself is not an entity, so it should not be included
        // NB: EntityStrongId.Foreign is decorated with LynxDoNotIncludeReferenced, but it should still be regarded as a reference (just not loaded)
        model.GetNavigationChains(typeof(Foreign))
            .Select(e => e.Root.ClrType)
            .Distinct()
            .Should().BeEquivalentTo(
                [typeof(Child), typeof(Entity1), typeof(Entity2), typeof(Entity3), typeof(ParentEntity), typeof(EntityStrongId)]);
    }

    [Fact]
    public void GetCollectionReferencingNavigations()
    {
        using var _ = CreateContext(out var model);

        // Child: Referenced as a collection by Entity2
        model.GetNavigationChains(typeof(Child))
            .Should()
            .Contain(c => c.Chain.Any(n => n.IsCollection));
    }

    [Fact]
    public void GetManualForeignKeyNavigationsShouldBeEmpty()
    {
        using var _ = CreateContext(out var model);

        model.GetNavigationChains(typeof(ManuallyDefinedReference))
            .Should().BeEmpty();
    }

    [Fact]
    public void OnlyForeignKeysShouldBeReturned()
    {
        using var _ = CreateContext(out var model);

        // Entity2: referenced by Child and ManuallyDefinedReference
        // There is also a reverse navigation with Entity1, but that should not be returned
        model.GetForeignKeyChains(typeof(Entity2))
            .Select(e => e.Root.ClrType)
            .Should().BeEquivalentTo([typeof(Child), typeof(ParentEntity), typeof(ManuallyDefinedReference)]);
    }

    [Fact]
    public void OwnedEntitiesShouldNotBeReturned()
    {
        using var _ = CreateContext(out var model);

        // Foreign: referenced by Owned, which is owned by Entity3
        // NB: Owned itself is not an entity, so it should not be included
        // NB: EntityStrongId.Foreign is decorated with LynxDoNotIncludeReferenced, but it should still be regarded as a reference (just not loaded)
        model.GetForeignKeyChains(typeof(Foreign))
            .Select(e => e.Root.ClrType)
            .Distinct()
            .Should().BeEquivalentTo(
                [typeof(Entity3), typeof(Entity2), typeof(Child), typeof(ParentEntity), typeof(ManuallyDefinedReference), typeof(EntityStrongId)]);
    }

    [Fact]
    public void AloneEntityShouldHaveNoReferences()
    {
        using var _ = CreateContext(out var model);

        model.GetForeignKeyChains(typeof(Alone)).ShouldBeEmpty();
    }

    [Fact]
    public void Entity1ShouldHaveSpecificReferences()
    {
        using var _ = CreateContext(out var model);

        var referencingEntities = model.GetForeignKeyChains(typeof(Entity1));

        referencingEntities.Should()
            .OnlyContain(x => x.Target.ClrType == typeof(Entity1));

        referencingEntities.Should()
            .ContainSingle(x => x.Root.ClrType == typeof(Entity2)).Which
            .Chain.Should().ContainSingle().Which
            .Properties.Should().ContainSingle().Which
            .Name.Should().Be(nameof(Entity2.ParentIdValue));

        referencingEntities
            .Where(x => x.Root.ClrType == typeof(ManuallyDefinedReference))
            .Should().HaveCount(2).And
            .ContainSingle(x => x.Chain.Count == 1).And
            .ContainSingle(x => x.Chain.Count == 2).Which
            .Chain[0].PrincipalEntityType.ClrType.Should().Be(typeof(Entity2));
    }

    private static IDisposable CreateContext(out IModel model, [CallerMemberName] string? testName = null)
    {
        testName.ShouldNotBeNullOrEmpty();
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(testName)
            .Options;

        var context = new TestContext(options);
        model = context.Model;
        return context;
    }
}