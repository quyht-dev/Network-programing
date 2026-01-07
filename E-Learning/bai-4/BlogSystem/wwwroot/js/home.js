const api = async () => {
  const res = await fetch("/api/blogs", {
    method: "GET",
  });

  return res;
};

const res = await api();
const data = await res.json();
console.log(data);
