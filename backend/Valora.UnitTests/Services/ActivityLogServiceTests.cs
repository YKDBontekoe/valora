using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Application.Services;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Services;

public class ActivityLogServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly ActivityLogService _service;
    private readonly WorkspaceRepository _repository;

    public ActivityLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _repository = new WorkspaceRepository(_context);
        _service = new ActivityLogService(_repository);
    }

    [Fact]
    public async Task GetActivityLogsAsync_ShouldReturnLogs()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer });
        _context.Workspaces.Add(workspace);

        var log = new ActivityLog { Workspace = workspace, ActorId = "owner", Type = ActivityLogType.WorkspaceCreated, Summary = "Created", CreatedAt = DateTime.UtcNow };
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        var result = await _service.GetActivityLogsAsync(userId, workspace.Id);

        Assert.Single(result);
        Assert.Equal("Created", result.First().Summary);
    }
}
