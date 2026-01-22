// Global variables
let currentPage = 1;
let currentKeyword = "";
let currentSort = "";
let isLoading = false;
let hasMoreBlogs = true;

// DOM Elements
const blogListContainer = document.getElementById("blogListContainer");
const featuredBlogContainer = document.getElementById("featuredBlogContainer");
const loadingSpinner = document.getElementById("loadingSpinner");
const noBlogsMessage = document.getElementById("noBlogsMessage");
const viewMoreContainer = document.getElementById("viewMoreContainer");
const loadMoreBtn = document.getElementById("loadMoreBtn");
const searchInput = document.getElementById("searchInput");
const searchBtn = document.getElementById("searchBtn");

// Format date function
function formatDate(dateString) {
  const date = new Date(dateString);
  return date.toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

// Truncate text function
function truncateText(text, maxLength = 100) {
  if (text.length <= maxLength) return text;
  return text.substr(0, maxLength) + "...";
}

// Cập nhật hàm createFeaturedBlogHTML
function createFeaturedBlogHTML(blog) {
  if (!blog) return "";

  return `
        <div class="featured-blog card shadow-lg border-0 mb-5">
            <div class="row g-0">
                <div class="col-md-5">
                    <div class="img-container position-relative">
                        <img src="${
                          blog.thumbnail || "/images/cat_beautiful.jpg"
                        }" class="img-fluid w-100" alt="${blog.title}" />
                        <span class="category-badge position-absolute" style="top: 20px; left: 20px;">
                            NỔI BẬT
                        </span>
                        <div class="position-absolute bottom-0 start-0 p-4">
                            <div class="d-flex align-items-center text-white">
                                <img src="/images/avatar.jpg" class="rounded-circle me-2" width="40" height="40" alt="Author">
                                <div>
                                    <small class="d-block">Tác giả ID: ${
                                      blog.authorId
                                    }</small>
                                    <small class="opacity-75">${formatDate(
                                      blog.createdAt
                                    )}</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-7">
                    <div class="card-body p-4">
                        <h2 class="card-title fw-bold mb-3">${blog.title}</h2>
                        <p class="card-text text-muted mb-4">
                            ${truncateText(blog.content || "", 200)}
                        </p>
                        <div class="d-flex align-items-center justify-content-between">
                            <div class="d-flex gap-3">
                                <span class="badge bg-light text-dark">
                                    <i class="fas fa-eye me-1"></i> ${
                                      blog.viewCount || 0
                                    }
                                </span>
                                <span class="badge bg-light text-dark">
                                    <i class="fas fa-heart me-1"></i> ${
                                      blog.likeCount || 0
                                    }
                                </span>
                                <span class="badge bg-light text-dark">
                                    <i class="fas fa-comment me-1"></i> ${
                                      blog.commentCount || 0
                                    }
                                </span>
                            </div>
                            <a href="/Blog/Detail/${
                              blog.id
                            }" class="btn btn-gradient px-4 py-2">
                                <i class="fas fa-book-reader me-2"></i> Đọc ngay
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Cập nhật hàm createBlogCardHTML
function createBlogCardHTML(blog) {
  return `
        <div class="col-lg-4 col-md-6">
            <div class="card blog-card shadow-sm border-0 h-100">
                <div class="position-relative overflow-hidden">
                    <img src="${
                      blog.thumbnail || "/images/cat_beautiful.jpg"
                    }" class="card-img-top" alt="${blog.title}" />
                    <span class="badge bg-dark position-absolute" style="top: 10px; right: 10px;">
                        <i class="fas fa-clock me-1"></i> 7 phút
                    </span>
                </div>
                <div class="card-body">
                    <div class="meta-info mb-3">
                        <span><i class="fas fa-user"></i> ID: ${
                          blog.authorId
                        }</span>
                        <span><i class="fas fa-calendar"></i> ${formatDate(
                          blog.createdAt
                        )}</span>
                    </div>
                    <h5 class="fw-bold mb-3">${blog.title}</h5>
                    <p class="text-muted mb-4">
                        ${truncateText(blog.content || "", 100)}
                    </p>
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="read-time">📖 5 phút đọc</span>
                        <a href="/Blog/Detail/${
                          blog.id
                        }" class="btn btn-outline-gradient rounded-pill px-4">
                            Đọc tiếp
                        </a>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Fetch blogs from API
async function fetchBlogs(page = 1, keyword = "", sort = "") {
  try {
    isLoading = true;
    loadingSpinner.style.display = "block";

    // Build query parameters
    const params = new URLSearchParams();
    if (keyword) params.append("keyword", keyword);
    if (sort) params.append("sort", sort);

    const response = await fetch(`/api/blogs?${params.toString()}`);

    if (!response.ok) {
      throw new Error("Network response was not ok");
    }

    const result = await response.json();

    if (result.success && result.data) {
      return result.data;
    } else {
      throw new Error(result.message || "Failed to fetch blogs");
    }
  } catch (error) {
    console.error("Error fetching blogs:", error);
    showError("Không thể tải dữ liệu. Vui lòng thử lại sau.");
    return [];
  } finally {
    isLoading = false;
    loadingSpinner.style.display = "none";
  }
}

// Display blogs
async function displayBlogs() {
  const blogs = await fetchBlogs(currentPage, currentKeyword, currentSort);

  if (blogs.length === 0) {
    if (currentPage === 1) {
      noBlogsMessage.style.display = "block";
      viewMoreContainer.style.display = "none";
    }
    hasMoreBlogs = false;
    return;
  }

  noBlogsMessage.style.display = "none";

  if (currentPage === 1) {
    // Clear containers for first page
    blogListContainer.innerHTML = "";

    // Set featured blog (first blog)
    if (blogs.length > 0) {
      featuredBlogContainer.innerHTML = createFeaturedBlogHTML(blogs[0]);

      // Display remaining blogs
      const remainingBlogs = blogs.slice(1);
      remainingBlogs.forEach((blog) => {
        blogListContainer.innerHTML += createBlogCardHTML(blog);
      });
    }
  } else {
    // Append blogs for pagination
    blogs.forEach((blog) => {
      blogListContainer.innerHTML += createBlogCardHTML(blog);
    });
  }

  // Show/hide load more button
  if (blogs.length >= 5) {
    // Assuming 5 blogs per page
    viewMoreContainer.style.display = "block";
    hasMoreBlogs = true;
  } else {
    viewMoreContainer.style.display = "none";
    hasMoreBlogs = false;
  }
}

// Show error message
function showError(message) {
  const errorDiv = document.createElement("div");
  errorDiv.className = "alert alert-danger alert-dismissible fade show";
  errorDiv.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

  const container = document.querySelector(".container.py-5");
  container.insertBefore(errorDiv, container.firstChild);

  setTimeout(() => {
    errorDiv.remove();
  }, 5000);
}

// Search function
function handleSearch() {
  currentKeyword = searchInput.value.trim();
  currentPage = 1;
  displayBlogs();
}

// Sort function
function handleSort(sortValue) {
  currentSort = sortValue;
  currentPage = 1;
  displayBlogs();
}

// Load more blogs
function loadMoreBlogs() {
  if (!isLoading && hasMoreBlogs) {
    currentPage++;
    displayBlogs();
  }
}

// Event Listeners
document.addEventListener("DOMContentLoaded", () => {
  // Initial load
  displayBlogs();

  // Search button click
  searchBtn.addEventListener("click", handleSearch);

  // Search input enter key
  searchInput.addEventListener("keypress", (e) => {
    if (e.key === "Enter") {
      handleSearch();
    }
  });

  // Sort dropdown items
  document.querySelectorAll(".dropdown-item[data-sort]").forEach((item) => {
    item.addEventListener("click", (e) => {
      e.preventDefault();
      const sortValue = e.target.getAttribute("data-sort");

      // Update dropdown button text
      document.getElementById("sortDropdown").innerHTML = `
                        <i class="fas fa-sort me-2"></i> ${e.target.textContent}
                    `;

      handleSort(sortValue);
    });
  });

  // Category filter buttons
  document.querySelectorAll(".category-filter .btn").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      // Remove active class from all buttons
      document.querySelectorAll(".category-filter .btn").forEach((b) => {
        b.classList.remove("active");
      });

      // Add active class to clicked button
      e.target.classList.add("active");

      const sortValue = e.target.getAttribute("data-sort") || "";
      handleSort(sortValue);
    });
  });

  // Load more button
  loadMoreBtn.addEventListener("click", loadMoreBlogs);

  // Infinite scroll (optional)
  window.addEventListener("scroll", () => {
    if (
      window.innerHeight + window.scrollY >=
      document.body.offsetHeight - 500
    ) {
      loadMoreBlogs();
    }
  });
});
