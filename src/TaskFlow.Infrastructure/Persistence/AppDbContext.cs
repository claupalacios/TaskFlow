using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<User> Users => Set<User>();
}
