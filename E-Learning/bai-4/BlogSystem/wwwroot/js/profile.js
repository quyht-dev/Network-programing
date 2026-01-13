document.addEventListener("DOMContentLoaded", () => {
  loadBlogs();
});

// =========================================
// GET BLOGS
// =========================================
async function loadBlogs() {
  const response = await fetch("/api/blogs/personal", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: +sessionStorage.getItem("userId") }),
  });

  const result = await response.json();

  console.log(result);

  const list = document.getElementById("blogList");
  list.innerHTML = "";

  if (!result.success || !result.data) {
    list.innerHTML = `<tr><td colspan="5" class="text-center">No blogs found</td></tr>`;
    return;
  }

  result.data.forEach((blog) => {
    list.innerHTML += `
            <tr>
                <td>${blog.id}</td>
                <td>${blog.title}</td>
                <td>
                    ${
                      blog.thumbnail
                        ? `<img src="${blog.thumbnail}" width="80" />`
                        : "No Image"
                    }
                </td>
                <td>${blog.createdAt}</td>
                <td>
                    <button class="btn btn-warning btn-sm" onclick="openUpdateModal(${
                      blog.id
                    }, '${blog.title}', \`${blog.content}\`)">Edit</button>
                    <button class="btn btn-danger btn-sm" onclick="deleteBlog(${
                      blog.id
                    })">Delete</button>
                </td>
            </tr>
        `;
  });
}

// =========================================
// CREATE BLOG
// =========================================
async function createBlog() {
  const title = document.getElementById("createTitle").value;
  const content = document.getElementById("createContent").value;
  const fileInput = document.getElementById("createImage");

  const formData = new FormData();
  formData.append("Title", title);
  formData.append("Content", content);
  if (fileInput.files.length > 0) {
    formData.append("Image", fileInput.files[0]);
  }

  const response = await fetch("/api/blogs", {
    method: "POST",
    body: formData,
  });
  const result = await response.json();

  if (result.success) {
    alert("Created!");
    loadBlogs();
    bootstrap.Modal.getInstance(document.getElementById("createModal")).hide();
  } else {
    alert(result.message);
  }
}

// =========================================
// OPEN UPDATE MODAL
// =========================================
function openUpdateModal(id, title, content) {
  document.getElementById("updateId").value = id;
  document.getElementById("updateTitle").value = title;
  document.getElementById("updateContent").value = content;

  const modal = new bootstrap.Modal(document.getElementById("updateModal"));
  modal.show();
}

// =========================================
// UPDATE BLOG
// =========================================
async function updateBlog() {
  const id = document.getElementById("updateId").value;
  const title = document.getElementById("updateTitle").value;
  const content = document.getElementById("updateContent").value;

  const response = await fetch(`/api/blogs/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, content }),
  });

  const result = await response.json();

  if (result.success) {
    alert("Updated!");
    loadBlogs();
    bootstrap.Modal.getInstance(document.getElementById("updateModal")).hide();
  } else {
    alert(result.message);
  }
}

// =========================================
// DELETE BLOG
// =========================================
async function deleteBlog(id) {
  if (!confirm("Delete this blog?")) return;

  const response = await fetch(`/api/blogs/${id}`, {
    method: "DELETE",
  });

  const result = await response.json();

  if (result.success) {
    alert("Deleted!");
    loadBlogs();
  } else {
    alert(result.message);
  }
}
