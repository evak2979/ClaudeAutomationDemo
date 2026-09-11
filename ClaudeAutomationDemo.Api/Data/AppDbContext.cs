using ClaudeAutomationDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeAutomationDemo.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
