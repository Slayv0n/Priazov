// Прокрутка вниз
function scrollToBottom() {
  window.scrollTo({
    top: document.body.scrollHeight,
    behavior: "smooth",
  });
}

// Навигация к карточкам проектов
document.addEventListener("DOMContentLoaded", function () {
  const postWrapper = document.querySelector("#post-wrapper");
  if (!postWrapper) return;

  postWrapper.addEventListener("click", (e) => {
    const card = e.target.closest("[data-id]");
    if (!card) return;
    const id = card.dataset.id;
    window.location.href = `project-profile.html?id=${id}`;
  });
});

// Вывод карточек организаций (страница поиска)
document.addEventListener("DOMContentLoaded", function () {
  const postWrapper = document.querySelector("#post-wrapper");
  if (!postWrapper) return;

  postWrapper.innerHTML = "";

  fetch("data/companies.json")
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.json();
    })
    .then((data) => {
      data.organizations.forEach((org) => {
        postWrapper.innerHTML += `
          <div data-id="${org.id}" class="organization-card">
              <div class="card-image">
                  <img class="org-image" src="static/${org.image}" 
                       onerror="this.onerror=null; this.src='static/FotoLogo.jpg';" 
                       alt="${org.title}">
              </div>
              <div class="card-content">
                  <h3 class="org-name">${org.title}</h3>
                  <p class="org-description">${
                    org.description.replace(/\s+/g, " ").trim().length > 150
                      ? org.description.replace(/\s+/g, " ").trim().substring(0, 150) +
                        "..."
                      : org.description.replace(/\s+/g, " ").trim()
                  }</p>
                  <button class="details-button" onclick="location.href='project.html?id=${org.id}'">Подробнее</button>
              </div>
          </div>`;
      });
    })
    .catch((error) => {
      console.error("Error loading organizations:", error);
      postWrapper.innerHTML =
        "<p>Произошла ошибка при загрузке данных</p>";
    });
});
