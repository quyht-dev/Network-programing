/*
  ĐOẠN CODE NODE.JS NÀY MINH HỌA CÁC PHƯƠNG PHÁP TỐI ƯU TCP SAU:

  1. Tắt Nagle Algorithm (TCP_NODELAY)
    - Sử dụng phương thức setNoDelay(true).
    - Giúp gửi dữ liệu ngay lập tức, không bị gom gói.
    - Giảm độ trễ (latency), phù hợp với ứng dụng real-time.

  2. Thiết lập Timeout cho kết nối TCP
    - Sử dụng setTimeout(5000) để giới hạn thời gian chờ.
    - Tránh kết nối bị treo khi server không phản hồi.

  3. Mô hình Event-driven (Non-blocking I/O)
    - Node.js sử dụng cơ chế xử lý sự kiện (event loop).
    - Không chặn luồng khi chờ dữ liệu, giúp xử lý nhiều kết nối hiệu quả.

  4. Quản lý vòng đời kết nối rõ ràng
    - Sử dụng các sự kiện 'connect', 'data', và 'timeout'.
    - Đóng socket bằng destroy() sau khi hoàn thành truyền dữ liệu.

  Kết luận:
    Đoạn code minh họa cách Node.js tối ưu TCP theo hướng event-driven,
    tập trung vào giảm độ trễ và khả năng xử lý đồng thời thay vì can thiệp sâu vào socket option.
*/

const net = require("net");

const client = new net.Socket();

client.setNoDelay(true);
client.setTimeout(5000);

client.connect(9000, "127.0.0.1", () => {
  console.log("Connected");
  client.write("Hello from Node.js TCP");
});

client.on("data", (data) => {
  console.log("Received: " + data);
  client.destroy();
});

client.on("timeout", () => {
  console.log("Timeout");
  client.destroy();
});
