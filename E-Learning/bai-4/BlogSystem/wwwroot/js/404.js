document.addEventListener("DOMContentLoaded", function () {
  // Search functionality
  const searchBtn = document.getElementById("searchBtn");
  const searchInput = document.getElementById("searchInput");

  searchBtn.addEventListener("click", function () {
    performSearch();
  });

  searchInput.addEventListener("keypress", function (e) {
    if (e.key === "Enter") {
      performSearch();
    }
  });

  function performSearch() {
    const query = searchInput.value.trim();
    if (query) {
      window.location.href = `/Blog?keyword=${encodeURIComponent(query)}`;
    }
  }

  // Animate error icon
  const exclamationIcon = document.querySelector(".fa-exclamation-circle");
  setInterval(() => {
    exclamationIcon.classList.toggle("text-danger");
    exclamationIcon.classList.toggle("text-warning");
  }, 1000);
});
