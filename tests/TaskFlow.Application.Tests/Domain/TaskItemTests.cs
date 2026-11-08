using TaskFlow.Domain;

namespace TaskFlow.Application.Tests.Domain;

public class TaskItemTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Due = new(2026, 11, 20);

    private static TaskItem NewTask() => TaskItem.Create(UserId, "Write report", "Q4 numbers", Due, Now);

    /// <summary>Asserts that no field changed since <see cref="NewTask"/> created the task.</summary>
    private static void AssertUnchanged(TaskItem task, Guid originalId)
    {
        Assert.Equal(originalId, task.Id);
        Assert.Equal(UserId, task.UserId);
        Assert.Equal("Write report", task.Title);
        Assert.Equal("Q4 numbers", task.Description);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(Due, task.DueDate);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now, task.UpdatedAt);
    }

    // ---- Create ----

    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var task = TaskItem.Create(UserId, "Write report", "Q4 numbers", Due, Now);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(UserId, task.UserId);
        Assert.Equal("Write report", task.Title);
        Assert.Equal("Q4 numbers", task.Description);
        Assert.Equal(Due, task.DueDate);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Fact]
    public void Create_TrimsTitle()
    {
        var task = TaskItem.Create(UserId, "  Write report  ", null, null, Now);

        Assert.Equal("Write report", task.Title);
    }

    [Fact]
    public void Create_WithoutDescriptionOrDueDate_UsesEmptyDescriptionAndNullDueDate()
    {
        var task = TaskItem.Create(UserId, "Write report", null, null, Now);

        Assert.Equal(string.Empty, task.Description);
        Assert.Null(task.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_Throws(string? title)
    {
        Assert.Throws<DomainValidationException>(() => TaskItem.Create(UserId, title, null, null, Now));
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_Succeeds()
    {
        var task = TaskItem.Create(UserId, new string('a', TaskItem.TitleMaxLength), null, null, Now);

        Assert.Equal(TaskItem.TitleMaxLength, task.Title.Length);
    }

    [Fact]
    public void Create_WithTitleOverMaxLength_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => TaskItem.Create(UserId, new string('a', TaskItem.TitleMaxLength + 1), null, null, Now));
    }

    [Fact]
    public void Create_WithDescriptionOverMaxLength_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => TaskItem.Create(UserId, "Title", new string('a', TaskItem.DescriptionMaxLength + 1), null, Now));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => TaskItem.Create(Guid.Empty, "Title", null, null, Now));
    }

    // ---- Update ----

    [Fact]
    public void Update_ChangesFieldsAndUpdatedAt_KeepsIdentityAndCreatedAt()
    {
        var task = NewTask();
        var originalId = task.Id;
        var later = Now.AddHours(3);
        var newDue = new DateOnly(2026, 12, 1);

        task.Update("New title", "New description", TaskItemStatus.InProgress, newDue, later);

        Assert.Equal(originalId, task.Id);
        Assert.Equal("New title", task.Title);
        Assert.Equal("New description", task.Description);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        Assert.Equal(newDue, task.DueDate);
        Assert.Equal(later, task.UpdatedAt);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(UserId, task.UserId);
    }

    [Fact]
    public void Update_TrimsTitle()
    {
        var task = NewTask();

        task.Update("  New title  ", "Q4 numbers", TaskItemStatus.Pending, Due, Now.AddHours(1));

        Assert.Equal("New title", task.Title);
    }

    [Fact]
    public void Update_CanClearDueDate()
    {
        var task = NewTask();

        task.Update("Write report", "Q4 numbers", TaskItemStatus.Pending, null, Now.AddHours(1));

        Assert.Null(task.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankTitle_ThrowsAndLeavesTaskUnchanged(string? title)
    {
        var task = NewTask();
        var originalId = task.Id;

        Assert.Throws<DomainValidationException>(
            () => task.Update(title, "changed", TaskItemStatus.Completed, null, Now.AddHours(1)));

        AssertUnchanged(task, originalId);
    }

    [Fact]
    public void Update_WithTitleOverMaxLength_ThrowsAndLeavesTaskUnchanged()
    {
        var task = NewTask();
        var originalId = task.Id;

        Assert.Throws<DomainValidationException>(
            () => task.Update(new string('a', TaskItem.TitleMaxLength + 1), "changed", TaskItemStatus.Completed, null, Now.AddHours(1)));

        AssertUnchanged(task, originalId);
    }

    [Fact]
    public void Update_WithDescriptionOverMaxLength_ThrowsAndLeavesTaskUnchanged()
    {
        var task = NewTask();
        var originalId = task.Id;

        Assert.Throws<DomainValidationException>(
            () => task.Update("Changed", new string('a', TaskItem.DescriptionMaxLength + 1), TaskItemStatus.Completed, null, Now.AddHours(1)));

        AssertUnchanged(task, originalId);
    }

    [Fact]
    public void Update_WithUndefinedStatus_ThrowsAndLeavesTaskUnchanged()
    {
        var task = NewTask();
        var originalId = task.Id;

        Assert.Throws<DomainValidationException>(
            () => task.Update("Changed", "changed", (TaskItemStatus)99, null, Now.AddHours(1)));

        AssertUnchanged(task, originalId);
    }
}
