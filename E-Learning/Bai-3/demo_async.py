import asyncio
import time

# Một "coroutine" mô phỏng tác vụ tốn thời gian (I/O bound)
async def handle_task(name, duration):
    print(f"  [➔] Bắt đầu: {name} (dự kiến {duration}s)")
    # Thay vì time.sleep() (làm treo cả luồng), ta dùng asyncio.sleep()
    await asyncio.sleep(duration) 
    print(f"  [✓] Hoàn thành: {name}")
    return f"Kết quả {name}"

async def run_asynchronous():
    print("\n=== ĐANG CHẠY CHẾ ĐỘ BẤT ĐỒNG BỘ (ASYNCHRONOUS) ===")
    start_time = time.perf_counter()

    # Tạo danh sách các tác vụ để chạy cùng lúc
    tasks = [
        handle_task("Kiểm tra kho", 2),
        handle_task("Xử lý thanh toán", 3),
        handle_task("Gửi Email xác nhận", 1)
    ]

    # Kỹ thuật Gather: Chạy tất cả các task đồng thời và chờ kết quả
    results = await asyncio.gather(*tasks)

    end_time = time.perf_counter()
    print(f"==> Tổng thời gian chạy bất đồng bộ: {end_time - start_time:.2f} giây")
    return results

def run_synchronous():
    print("\n=== ĐANG CHẠY CHẾ ĐỘ ĐỒNG BỘ (SYNCHRONOUS) ===")
    start_time = time.perf_counter()

    # Tác vụ chạy tuần tự, cái sau chờ cái trước
    def sync_task(name, duration):
        print(f"  [➔] Bắt đầu: {name}...")
        time.sleep(duration)
        print(f"  [✓] Hoàn thành: {name}")

    sync_task("Kiểm tra kho", 2)
    sync_task("Xử lý thanh toán", 3)
    sync_task("Gửi Email xác nhận", 1)

    end_time = time.perf_counter()
    print(f"==> Tổng thời gian chạy tuần tự: {end_time - start_time:.2f} giây")

# Hàm thực thi chính
if __name__ == "__main__":
    # 1. Chạy thử đồng bộ trước
    run_synchronous()
    
    print("-" * 50)

    # 2. Chạy thử bất đồng bộ
    asyncio.run(run_asynchronous())