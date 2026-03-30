using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace C2Server.Hubs
{
    public class AgentInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
    }

    public class RemoteHub : Hub
    {
        private static readonly ConcurrentDictionary<string, AgentInfo> OnlineAgents = new();

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"[+] KẾT NỐI MỚI: {Context.ConnectionId}");

            // TEST: Gửi một máy ảo giả lập xuống Web ngay khi Web vừa kết nối
            var fakeAgent = new AgentInfo
            {
                MachineId = "VM-TEST-001",
                MachineName = "Dinh-Van-Hao-PC",
                IPAddress = "192.168.1.100",
                OSVersion = "Windows 11 Pro"
            };

            // Gửi cho chính người vừa kết nối (Web)
            await Clients.Caller.SendAsync("AgentConnected", fakeAgent);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (OnlineAgents.TryRemove(Context.ConnectionId, out var agent))
            {
                Console.WriteLine($"[-] RỚT MẠNG: {agent.MachineName}");
                await Clients.All.SendAsync("AgentDisconnected", agent.MachineId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Agent gọi lên để báo danh
        public async Task RegisterAgent(AgentInfo info)
        {
            OnlineAgents.TryAdd(Context.ConnectionId, info);
            Console.WriteLine($"[*] AGENT ĐĂNG KÝ: {info.MachineName} - OS: {info.OSVersion}");
            await Clients.All.SendAsync("AgentConnected", info);
        }

        // Web Client gọi để lấy danh sách máy lúc F5
        public List<AgentInfo> GetOnlineAgents()
        {
            return OnlineAgents.Values.ToList();
        }
    }
}