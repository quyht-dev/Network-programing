# 🎮 Tic Tac Toe – WPF Application (Giữa kỳ)

Ứng dụng **Tic Tac Toe (Cờ ca rô)** được xây dựng bằng **WPF (Windows Presentation Foundation)** với **C# (.NET)**.  
Dự án nhằm mục đích học tập, thực hành:
- Xây dựng giao diện bằng **XAML**
- Xử lý sự kiện trong **Code-behind**
- Hiểu cấu trúc cơ bản của một dự án **WPF**

---

## 🧩 Công nghệ sử dụng
- Ngôn ngữ: **C#**
- Framework: **.NET (WPF)**
- IDE khuyến nghị: **Visual Studio 2022+**
- Hệ điều hành: **Windows**

---

## 📁 Cấu trúc thư mục

<pre>
tic-tac-toe/
|
├── App.xaml
├── App.xaml.cs
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── AssemblyInfo.cs
├── tic-tac-toe.csproj
│
├── bin/
├── obj/
│
└── README.md
</pre>

> Còn cập nhật

## ▶️ Cách chạy chương trình

### Cách 1: Chạy bằng Visual Studio (khuyến nghị)
1. Mở **Visual Studio**
2. Chọn **Open a project or solution**
3. Mở file `tic-tac-toe.csproj`
4. Nhấn **F5** hoặc **Start**

---

### Cách 2: Chạy bằng dòng lệnh (CLI)

> Đảm bảo bạn đã cài đặt **.NET SDK** trên máy tính.

```bash
cd path/to/tic-tac-toe // Thay đổi đường dẫn đến thư mục dự án
dotnet build // Xây dựng dự án
dotnet run // Chạy ứng dụng
```