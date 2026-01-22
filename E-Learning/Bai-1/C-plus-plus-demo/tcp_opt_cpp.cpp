#include <iostream>
#include <cstring>
#include <winsock2.h>
#include <ws2tcpip.h>

/*
    ĐOẠN CODE NÀY MINH HỌA CÁC PHƯƠNG PHÁP TỐI ƯU TCP SAU:

    1. Tắt Nagle Algorithm (TCP_NODELAY)
        - Sử dụng setsockopt với tùy chọn TCP_NODELAY.
        - Giúp gửi các gói dữ liệu nhỏ ngay lập tức, không chờ gom gói.
        - Giảm độ trễ (latency), phù hợp với ứng dụng cần phản hồi nhanh.

    2. Tối ưu Buffer gửi và nhận (SO_SNDBUF, SO_RCVBUF)
        - Thiết lập kích thước buffer gửi và nhận lên 8192 bytes.
        - Giúp giảm số lần gửi/nhận hệ thống gọi, cải thiện hiệu năng truyền dữ liệu.
        - Phù hợp với các ứng dụng truyền dữ liệu liên tục qua TCP.

    3. Sử dụng TCP Socket chuẩn (AF_INET, SOCK_STREAM)
        - Đảm bảo giao thức TCP đáng tin cậy, có cơ chế kiểm soát lỗi và đảm bảo thứ tự gói tin.
        - Phù hợp cho các ứng dụng client–server ổn định.

    4. Quản lý kết nối rõ ràng (Explicit Connection Handling)
        - Thực hiện kết nối bằng connect và đóng socket bằng closesocket.
        - Đảm bảo giải phóng tài nguyên đúng cách, tránh rò rỉ socket.

    Kết luận:
        Đoạn code tập trung minh họa các kỹ thuật tối ưu TCP ở mức socket option,
        giúp cải thiện độ trễ và hiệu năng truyền dữ liệu trong môi trường Windows (Winsock).
*/


#pragma comment(lib, "Ws2_32.lib")  // Chỉ cần khi dùng MSVC

int main() {
    // 1. Khởi tạo Winsock
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cerr << "WSAStartup failed\n";
        return 1;
    }

    // 2. Tạo socket TCP
    SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == INVALID_SOCKET) {
        std::cerr << "Socket creation failed\n";
        WSACleanup();
        return 1;
    }

    // 3. Tắt Nagle Algorithm (TCP_NODELAY)
    int flag = 1;
    setsockopt(sock, IPPROTO_TCP, TCP_NODELAY,
               (char*)&flag, sizeof(flag));

    // 4. Thiết lập Buffer Size
    int bufSize = 8192;
    setsockopt(sock, SOL_SOCKET, SO_SNDBUF,
               (char*)&bufSize, sizeof(bufSize));
    setsockopt(sock, SOL_SOCKET, SO_RCVBUF,
               (char*)&bufSize, sizeof(bufSize));

    // 5. Cấu hình địa chỉ server
    sockaddr_in server{};
    server.sin_family = AF_INET;
    server.sin_port = htons(9000);
    server.sin_addr.s_addr = inet_addr("127.0.0.1");
    
    // 6. Kết nối
    if (connect(sock, (sockaddr*)&server, sizeof(server)) == SOCKET_ERROR) {
        std::cerr << "Connect failed\n";
        closesocket(sock);
        WSACleanup();
        return 1;
    }

    // 7. Gửi dữ liệu
    const char* msg = "Hello from C++ TCP (Winsock)";
    send(sock, msg, (int)strlen(msg), 0);

    std::cout << "Data sent successfully\n";

    // 8. Chờ phản hồi từ server
    char buffer[1024];
    int bytesReceived = recv(sock, buffer, sizeof(buffer) - 1, 0);
    if (bytesReceived > 0) {
        buffer[bytesReceived] = '\0'; // thêm ký tự kết thúc chuỗi
        std::cout << "Server response: " << buffer << std::endl;
        // Gửi lại cho client (echo)
        send(sock, buffer, bytesReceived, 0);
    } else {
        std::cerr << "No response or connection closed\n";
    }

    // 9. Đóng socket
    closesocket(sock);
    WSACleanup();
    return 0;
}
