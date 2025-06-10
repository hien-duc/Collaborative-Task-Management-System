using System.Threading.Tasks;

namespace Collaborative_Task_Management_System.Services
{
    public interface IDashboardBroadcastService
    {
        Task BroadcastDashboardUpdate(string userId, int? projectId = null);
        Task BroadcastDashboardUpdateToProjectMembers(int projectId);
    }
}
