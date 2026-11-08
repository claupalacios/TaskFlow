using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Tests.Persistence;

/// <summary>
/// Schema rules that SQLite does not enforce at runtime (column lengths) or that should
/// be explicit rather than left to conventions (keys, indexes, cascade behavior).
/// </summary>
public sealed class AppDbContextModelTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private IEntityType EntityType<T>()
    {
        using var context = _db.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(T));
        Assert.NotNull(entityType);
        return entityType;
    }

    [Fact]
    public void TaskTitle_MaxLengthMatchesDomainRule()
    {
        var property = EntityType<TaskItem>().FindProperty(nameof(TaskItem.Title));

        Assert.Equal(TaskItem.TitleMaxLength, property?.GetMaxLength());
    }

    [Fact]
    public void TaskDescription_MaxLengthMatchesDomainRule()
    {
        var property = EntityType<TaskItem>().FindProperty(nameof(TaskItem.Description));

        Assert.Equal(TaskItem.DescriptionMaxLength, property?.GetMaxLength());
    }

    [Fact]
    public void TaskStatus_IsStoredAsReadableText()
    {
        var property = EntityType<TaskItem>().FindProperty(nameof(TaskItem.Status));

        Assert.Equal(typeof(string), property?.GetProviderClrType());
    }

    [Fact]
    public void Task_HasForeignKeyToUserWithCascadeDelete()
    {
        var foreignKey = Assert.Single(EntityType<TaskItem>().GetForeignKeys());

        Assert.Equal(typeof(User), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(TaskItem.UserId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Task_HasIndexOnUserId()
    {
        var indexes = EntityType<TaskItem>().GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(TaskItem.UserId)]));
    }

    [Fact]
    public void UserEmail_MaxLengthMatchesDomainRule()
    {
        var property = EntityType<User>().FindProperty(nameof(User.Email));

        Assert.Equal(User.EmailMaxLength, property?.GetMaxLength());
    }

    [Fact]
    public void UserEmail_HasUniqueIndex()
    {
        var index = Assert.Single(
            EntityType<User>().GetIndexes(),
            i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(User.Email)]));

        Assert.True(index.IsUnique);
    }
}
