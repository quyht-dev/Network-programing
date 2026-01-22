using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/*
    TCP Optimization Methods in this Client

    1. TCP_NODELAY (client.NoDelay = true)
       - Tắt Nagle Algorithm để giảm độ trễ khi gửi gói tin nhỏ.
       - Phù hợp cho ứng dụng real-time hoặc cần phản hồi nhanh.

    2. SendBufferSize / ReceiveBufferSize
       - Điều chỉnh kích thước bộ đệm TCP (8192 bytes).
       - Giúp giảm số lần gửi/nhận, tăng hiệu quả truyền dữ liệu.

    3. SendTimeout / ReceiveTimeout
       - Thiết lập thời gian chờ (5 giây).
       - Tránh tình trạng treo chương trình khi mạng gặp sự cố.

    4. Asynchronous I/O (async/await)
       - Sử dụng bất đồng bộ để không block thread chính.
       - Dễ mở rộng xử lý nhiều client đồng thời, tăng khả năng chịu tải.

    Đây là các kỹ thuật cơ bản nhưng quan trọng để tối ưu hiệu năng và độ tin cậy của TCP Client.
*/


class TcpOptimizedClient
{
    static async Task Main()
    {
        TcpClient client = new TcpClient();

        // 1. Tắt Nagle Algorithm
        client.NoDelay = true;

        // 2. Thiết lập Buffer
        client.SendBufferSize = 8192;
        client.ReceiveBufferSize = 8192;

        // 3. Timeout
        client.SendTimeout = 5000;    // 5s
        client.ReceiveTimeout = 5000;

        Console.WriteLine("Connecting to server...");
        await client.ConnectAsync("127.0.0.1", 9000);

        NetworkStream stream = client.GetStream();

        // 4. Asynchronous I/O
        string message = "Hello TCP Optimization from C#";
        byte[] data = Encoding.UTF8.GetBytes(message);

        await stream.WriteAsync(data, 0, data.Length);
        Console.WriteLine("Data sent.");

        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        Console.WriteLine("Received: " +
            Encoding.UTF8.GetString(buffer, 0, bytesRead));

        client.Close();
    }
}
