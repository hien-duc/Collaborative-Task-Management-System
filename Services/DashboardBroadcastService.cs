using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Collaborative_Task_Management_System.Hubs;
using Collaborative_Task_Management_System.Services;

namespace Collaborative_Task_Management_System.Services
{
    public class DashboardBroadcastService : IDashboardBroadcastService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IProjectServiceWithUoW _projectService;
        private readonly ILogger<DashboardBroadcastService> _logger;
        private readonly IMemoryCache _cache;
        private const string DashboardCacheKey = "DashboardData_";

        public DashboardBroadcastService(
            IHubContext<NotificationHub> hubContext,
            IProjectServiceWithUoW projectService,
            ILogger<DashboardBroadcastService> logger,
            IMemoryCache cache)
        {
            _hubContext = hubContext;
            _projectService = projectService;
            _logger = logger;
            _cache = cache;
        }

        public async Task BroadcastDashboardUpdate(string userId, int? projectId = null)
        {
            try
            {
                // Clear the dashboard cache for this user
                var cacheKey = $"{DashboardCacheKey}{userId}_{projectId}";
                _cache.Remove(cacheKey);
                
                // Send the dashboard update notification
                await _hubContext.Clients.User(userId).SendAsync("DashboardDataUpdated", projectId);
                _logger.LogInformation("Dashboard update broadcast sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard update to user {UserId}", userId);
                throw;
            }
        }

        public async Task BroadcastDashboardUpdateToProjectMembers(int projectId)
        {
            try
            {
                // Get all project members
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Cannot broadcast dashboard update: Project {ProjectId} not found", projectId);
                    return;
                }
                
                // Get all project members including the creator
                var members = project.ProjectMembers.Select(pm => pm.UserId).ToList();
                if (!members.Contains(project.CreatedById))
                {
                    members.Add(project.CreatedById);
                }
                
                // Broadcast dashboard update to each member
                foreach (var userId in members)
                {
                    await BroadcastDashboardUpdate(userId, projectId);
                }
                
                _logger.LogInformation("Dashboard update broadcast sent to {MemberCount} members of project {ProjectId}", members.Count, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard update to project members for project {ProjectId}", projectId);
                throw;
            }
        }
    }
}
