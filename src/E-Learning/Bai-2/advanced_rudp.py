import socket
import struct
import threading
import time
import random
import zlib
import json

# --- CẤU HÌNH ---
SERVER_IP = '127.0.0.1'
SERVER_PORT = 8888
WINDOW_SIZE = 4        # Kích thước cửa sổ trượt
TIMEOUT = 2.0          # Thời gian chờ (giây)
LOSS_RATE = 0.2        # Tỷ lệ mất gói (20%)
CORRUPT_RATE = 0.1     # Tỷ lệ hỏng dữ liệu (10%)
DELAY_RANGE = (0.1, 0.5) # Độ trễ mạng giả lập

# --- PHẦN 1: CẤU TRÚC GÓI TIN (PACKET STRUCTURE) ---
class Packet:
    def __init__(self, seq_num, data=b'', is_ack=False):
        self.seq_num = seq_num
        self.is_ack = is_ack
        self.data = data if isinstance(data, bytes) else str(data).encode()
        self.checksum = zlib.crc32(self.data)
        self.original_checksum = self.checksum

    def to_bytes(self):
        # --- SỬA LỖI TẠI ĐÂY ---
        # Đổi '!II?' thành '!iI?' (dùng 'i' thường để chấp nhận số âm)
        # i: Signed Int (có thể chứa -1)
        # I: Unsigned Int (chỉ chứa số dương)
        header = struct.pack('!iI?', self.seq_num, self.checksum, self.is_ack)
        return header + self.data

    @staticmethod
    def from_bytes(raw_bytes):
        if len(raw_bytes) < 9: return None
        try:
            # --- SỬA LỖI TẠI ĐÂY ---
            # Phải dùng '!iI?' giống hệt lúc đóng gói
            seq_num, checksum, is_ack = struct.unpack('!iI?', raw_bytes[:9])
            data = raw_bytes[9:]
            packet = Packet(seq_num, data, is_ack)
            packet.original_checksum = checksum 
            return packet
        except struct.error:
            return None

    def is_valid(self):
        return self.original_checksum == zlib.crc32(self.data)

    def __repr__(self):
        type_str = "ACK" if self.is_ack else "DATA"
        return f"[{type_str} | SEQ={self.seq_num}]"

# --- PHẦN 2: GIẢ LẬP MẠNG (LOSSY NETWORK) ---
class NetworkSimulator:
    def __init__(self, sock, target_addr):
        self.sock = sock
        self.target_addr = target_addr

    def send(self, packet_bytes):
        # 1. Mất gói
        if random.random() < LOSS_RATE:
            print(f"   >>> [MẠNG] Đã làm RƠI gói tin trên đường truyền!")
            return

        # 2. Hỏng dữ liệu
        if random.random() < CORRUPT_RATE:
            print(f"   >>> [MẠNG] Gói tin bị NHIỄU (Corruption)!")
            packet_bytes = packet_bytes[:-1] + b'\x00'

        # 3. Trễ mạng
        delay = random.uniform(*DELAY_RANGE)
        def delayed_send():
            time.sleep(delay)
            try:
                self.sock.sendto(packet_bytes, self.target_addr)
            except OSError:
                pass # Bỏ qua lỗi nếu socket đã đóng
        
        threading.Thread(target=delayed_send, daemon=True).start()

# --- PHẦN 3: SENDER (CLIENT) ---
class RUDPSender:
    def __init__(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.network = NetworkSimulator(self.sock, (SERVER_IP, SERVER_PORT))
        self.base = 0
        self.next_seq_num = 0
        self.packets = []
        self.timer = None
        self.lock = threading.Lock()
        self.running = True

    def send_data(self, data_list):
        for i, data in enumerate(data_list):
            self.packets.append(Packet(i, data))
        
        threading.Thread(target=self.receive_ack_loop, daemon=True).start()

        while self.base < len(self.packets):
            with self.lock:
                while self.next_seq_num < self.base + WINDOW_SIZE and self.next_seq_num < len(self.packets):
                    packet = self.packets[self.next_seq_num]
                    print(f"[Sender] Gửi {packet} (Window: {self.base}-{min(self.base + WINDOW_SIZE, len(self.packets))})")
                    self.network.send(packet.to_bytes())
                    
                    if self.base == self.next_seq_num:
                        self.start_timer()
                    self.next_seq_num += 1
            time.sleep(0.1)
        
        print("[Sender] === HOÀN THÀNH GỬI TẤT CẢ ===")
        self.running = False

    def start_timer(self):
        if self.timer: self.timer.cancel()
        self.timer = threading.Timer(TIMEOUT, self.handle_timeout)
        self.timer.start()

    def handle_timeout(self):
        with self.lock:
            if self.base < len(self.packets):
                print(f"[Sender] !!! TIMEOUT !!! Đang gửi lại từ SEQ={self.base}")
                self.start_timer()
                for i in range(self.base, self.next_seq_num):
                    print(f"[Sender] Re-sending {self.packets[i]}")
                    self.network.send(self.packets[i].to_bytes())

    def receive_ack_loop(self):
        while self.running:
            try:
                self.sock.settimeout(1)
                data, _ = self.sock.recvfrom(1024)
                ack_pkt = Packet.from_bytes(data)
                
                if ack_pkt and ack_pkt.is_ack and ack_pkt.is_valid():
                    ack_seq = ack_pkt.seq_num
                    print(f"[Sender] <--- Nhận được ACK cho SEQ {ack_seq}")
                    
                    with self.lock:
                        if ack_seq >= self.base:
                            self.base = ack_seq + 1
                            if self.base == self.next_seq_num:
                                if self.timer: self.timer.cancel()
                            else:
                                self.start_timer()
            except socket.timeout:
                continue
            except Exception:
                pass

# --- PHẦN 4: RECEIVER (SERVER) ---
class RUDPReceiver:
    def __init__(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind((SERVER_IP, SERVER_PORT))
        self.expected_seq_num = 0
        print(f"[Receiver] Đang lắng nghe tại {SERVER_IP}:{SERVER_PORT}...")

    def start(self):
        while True:
            data, addr = self.sock.recvfrom(1024)
            packet = Packet.from_bytes(data)

            if not packet or not packet.is_valid():
                print(f"[Receiver] Gói tin bị HỎNG hoặc sai format. Bỏ qua.")
                continue

            if packet.seq_num == self.expected_seq_num:
                print(f"[Receiver] Chấp nhận gói đúng: {packet.data.decode()}")
                ack_pkt = Packet(packet.seq_num, is_ack=True)
                time.sleep(random.uniform(0.1, 0.3)) 
                self.sock.sendto(ack_pkt.to_bytes(), addr)
                print(f"[Receiver] ---> Đã gửi ACK {packet.seq_num}")
                self.expected_seq_num += 1
            else:
                # Nếu nhận sai thứ tự, gửi lại ACK của gói cuối cùng nhận được đúng
                # Nếu chưa nhận được gì (expect=0), sẽ gửi ACK -1.
                # Code đã fix để struct chấp nhận số -1.
                print(f"[Receiver] Sai thứ tự! Chờ {self.expected_seq_num}, Nhận {packet.seq_num}. Yêu cầu gửi lại.")
                resend_ack = Packet(self.expected_seq_num - 1, is_ack=True)
                self.sock.sendto(resend_ack.to_bytes(), addr)

# --- CHẠY MAIN ---
if __name__ == "__main__":
    server = RUDPReceiver()
    server_thread = threading.Thread(target=server.start, daemon=True)
    server_thread.start()
    
    time.sleep(1)

    messages = [
        "Packet_0: Hello",
        "Packet_1: UDP",
        "Packet_2: Is",
        "Packet_3: Now",
        "Packet_4: Optimized",
        "Packet_5: With",
        "Packet_6: Sliding",
        "Packet_7: Window",
        "Packet_8: !!!"
    ]

    sender = RUDPSender()
    sender.send_data(messages)
    
    time.sleep(5)