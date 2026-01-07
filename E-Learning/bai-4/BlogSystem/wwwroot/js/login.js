document.getElementById("loginForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const email = document.getElementById("typeEmailX").value;
  const password = document.getElementById("typePasswordX").value;

  // console.log(email, password);

  const response = await fetch("/Api/Auth/Login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  const result = await response.json();

  console.log(result);

  if (result.success) {
    document.getElementById(
      "message"
    ).innerHTML = `<div class="bg-success p-3">${result.message}</div>`;
    setTimeout(() => (window.location.href = "/Home/Index"), 2000);
  } else {
    document.getElementById(
      "message"
    ).innerHTML = `<div class="bg-danger p-3">${result.message}</div>`;
  }
});
