document.getElementById("loginForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const email = document.getElementById("typeEmailX").value;
  const password = document.getElementById("typePasswordX").value;

  try {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });

    const result = await response.json();

    if (result.success) {
      // Lấy Access Token từ response
      const accessToken = result.data.accessToken;
      const userId = result.data.userId;
      const email = result.data.email;
      const role = result.data.role;

      // Lưu Access Token vào Session Storage
      sessionStorage.setItem("accessToken", accessToken);
      sessionStorage.setItem("userId", userId);
      sessionStorage.setItem("email", email);
      sessionStorage.setItem("role", role);

      // Hiển thị thông báo thành công
      document.getElementById(
        "message"
      ).innerHTML = `<div class="bg-success p-3 text-white">${result.message}</div>`;

      // Chuyển hướng sau 2 giây
      setTimeout(() => {
        window.location.href = "/profile";
      }, 2000);
    } else {
      document.getElementById(
        "message"
      ).innerHTML = `<div class="bg-danger p-3 text-white">${result.message}</div>`;
    }
  } catch (error) {
    console.error("Login error:", error);
    document.getElementById(
      "message"
    ).innerHTML = `<div class="bg-danger p-3 text-white">Đã xảy ra lỗi khi đăng nhập</div>`;
  }
});
