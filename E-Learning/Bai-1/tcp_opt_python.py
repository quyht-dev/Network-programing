import socket

"""
    ĐOẠN CODE PYTHON NÀY MINH HỌA CÁC PHƯƠNG PHÁP TỐI ƯU TCP SAU:

    1. Tắt Nagle Algorithm (TCP_NODELAY)
        - Sử dụng setsockopt với tùy chọn TCP_NODELAY.
        - Giúp dữ liệu được gửi ngay lập tức mà không bị gom gói.
        - Giảm độ trễ (latency), phù hợp với các ứng dụng cần phản hồi nhanh.

    2. Tối ưu Buffer gửi và nhận (SO_SNDBUF, SO_RCVBUF)
        - Thiết lập kích thước buffer gửi và nhận là 8192 bytes.
        - Giúp cải thiện hiệu suất truyền dữ liệu TCP.
        - Giảm số lần gọi hệ thống khi truyền dữ liệu liên tục.

    3. Thiết lập Timeout cho socket
        - Sử dụng settimeout(5) để giới hạn thời gian chờ gửi/nhận.
        - Tránh chương trình bị treo vô hạn khi kết nối hoặc truyền dữ liệu gặp sự cố mạng.

    4. Sử dụng TCP Socket chuẩn (SOCK_STREAM)
        - Đảm bảo truyền dữ liệu đáng tin cậy, đúng thứ tự.
        - Có cơ chế kiểm soát lỗi và tái truyền gói tin.

    Kết luận:
        Đoạn code tập trung minh họa các kỹ thuật tối ưu TCP cơ bản thông qua socket option,
        giúp cân bằng giữa hiệu năng, độ trễ và tính ổn định trong Python.
"""


sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# TCP_NODELAY
sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

# Buffer
sock.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, 8192)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, 8192)

sock.settimeout(5)

try:
    sock.connect(("127.0.0.1", 9000))
    print("Connection success")
except socket.error as e:
    print("Error not connection with server: ", e)

sock.sendall(b"Hello from Python TCP")

# Chờ phản hồi từ server
try:
    data = sock.recv(1024)
    if data:
        print("Server response:", data.decode())
    else:
        print("No response or connection closed")
except socket.timeout:
    print("Timed out waiting for server response")

sock.close()
