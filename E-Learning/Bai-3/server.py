import asyncio
import websockets
import random
import json
import datetime

async def broadcast_data(websocket):
    print(f"--- [MỚI] Một Client đã kết nối: {websocket.remote_address}")
    try:
        while True:
            # 1. Tạo dữ liệu giả lập
            data = {
                "price": random.randint(45000, 46000),
                "time": datetime.datetime.now().strftime("%H:%M:%S")
            }
            
            # 2. Gửi dữ liệu dưới dạng JSON string
            await websocket.send(json.dumps(data))
            
            # 3. In ra console server để kiểm tra
            print(f"Đã gửi tới {websocket.remote_address}: {data['price']}")
            
            # 4. QUAN TRỌNG: Nghỉ 1 giây (Bất đồng bộ) để Server làm việc khác
            await asyncio.sleep(1) 
            
    except websockets.exceptions.ConnectionClosed:
        print(f"--- [NGẮT] Client {websocket.remote_address} đã rời đi.")

async def main():
    # Khởi tạo server tại localhost, port 8765
    async with websockets.serve(broadcast_data, "localhost", 8765):
        print("🚀 Server WebSocket đang chạy tại ws://localhost:8765")
        await asyncio.Future()  # Giữ server chạy vĩnh viễn

if __name__ == "__main__":
    asyncio.run(main())