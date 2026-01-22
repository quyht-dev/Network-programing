import java.net.Socket;
import java.io.InputStream;
import java.io.OutputStream;

/*
    ĐOẠN CODE JAVA NÀY MINH HỌA CÁC PHƯƠNG PHÁP TỐI ƯU TCP SAU:

    1. Tắt Nagle Algorithm (TCP_NODELAY)
        - Sử dụng phương thức setTcpNoDelay(true).
        - Giúp dữ liệu được gửi ngay lập tức, không bị gom gói.
        - Giảm độ trễ (latency), phù hợp với các ứng dụng cần phản hồi nhanh.

    2. Tối ưu Buffer gửi và nhận (Send/Receive Buffer)
        - Thiết lập kích thước buffer gửi và nhận là 8192 bytes.
        - Giúp cải thiện hiệu năng truyền dữ liệu TCP.
        - Giảm số lần gửi/nhận ở tầng hệ điều hành.

    3. Thiết lập Timeout cho socket
        - Sử dụng setSoTimeout(5000) để giới hạn thời gian chờ đọc dữ liệu.
        - Tránh chương trình bị treo khi server không phản hồi hoặc mạng gặp sự cố.

    4. Sử dụng API Socket cấp cao của Java
        - Java cung cấp API trừu tượng hóa, dễ sử dụng nhưng vẫn cho phép cấu hình các tùy chọn TCP quan trọng.
        - Đảm bảo tính ổn định và khả năng tương thích đa nền tảng.

    Kết luận:
        Đoạn code Java minh họa việc tối ưu TCP thông qua các socket option phổ biến,
        giúp cân bằng giữa hiệu năng, độ trễ và độ ổn định trong ứng dụng client–server.
*/


public class TcpOptimizedClient {
    public static void main(String[] args) throws Exception {
        Socket socket = new Socket("127.0.0.1", 9000);

        socket.setTcpNoDelay(true);
        socket.setSendBufferSize(8192);
        socket.setReceiveBufferSize(8192);
        socket.setSoTimeout(5000);

        // Gửi dữ liệu
        OutputStream out = socket.getOutputStream();
        out.write("Hello from Java TCP".getBytes());

        // Chờ phản hồi từ server
        InputStream in = socket.getInputStream();
        byte[] buffer = new byte[1024];
        int bytesRead = in.read(buffer);
        if (bytesRead > 0) {
            String response = new String(buffer, 0, bytesRead);
            System.out.println("Server response: " + response);
        } else {
            System.out.println("No response or connection closed");
        }

        socket.close();
    }
}
