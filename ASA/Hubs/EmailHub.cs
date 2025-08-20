using Microsoft.AspNetCore.SignalR;

namespace IndustrialSolutions.Hubs
{
    public class EmailHub : Hub
    {
        private readonly ILogger<EmailHub> _logger;

        public EmailHub(ILogger<EmailHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, "EmailUpdates");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "EmailUpdates");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinEmailGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "EmailUpdates");
        }

        public async Task LeaveEmailGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "EmailUpdates");
        }
    }
}