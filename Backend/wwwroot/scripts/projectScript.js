document.addEventListener("DOMContentLoaded", function() {
  // --- загрузка данных проекта ---
  const urlParams = new URLSearchParams(window.location.search);
  const projectId = urlParams.get("id");
  
  if (!projectId) {
    document.querySelector(".text-fields").innerHTML = "<h1>Проект не найден</h1>";
    return;
  }
  
  fetch("data/companies.json")
    .then(response => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.json();
    })
    .then(data => {
      const project = data.organizations.find(org => org.id == projectId);
      
      if (!project) {
        document.querySelector(".text-fields").innerHTML = "<h1>Проект не найден</h1>";
        return;
      }
      
      document.title = project.title;
      
      const textFields = document.querySelector(".project-left-section");
      if (textFields) {
        textFields.innerHTML = `
          <div class="project-heading">
            <span class="project-location">${project.address}</span>
            <h1 class="project-title">${project.title}</h1>
            <span class="project-category">Проект в сфере ${project.type}</span>
          </div>
          
          <section class="description-section">
            <div class="text-block">
              <p>${project.shortDescription || "Описание проекта отсутствует"}</p>
            </div>
            <div class="image-block">
              <img class="org-image" src="static/${project.image}" onerror="this.onerror=null; this.src='static/FotoLogo.jpg';" alt="${project.title}">
            </div>
          </section>

          <section class="details-section">
            <p>${project.description}</p>
          </section>
        `;
      }
    })
    .catch(error => {
      console.error("Error loading project:", error);
      document.querySelector(".text-fields").innerHTML = "<h1>Ошибка загрузки данных проекта</h1>";
    });
});

document.addEventListener("DOMContentLoaded", () => {
  const urlParams = new URLSearchParams(window.location.search);
  const pageId = urlParams.get("id");

  if (!pageId) {
    document.querySelector(".main-content").innerHTML = "<h1>Проект не найден</h1>";
    return;
  }

  fetch("data/submitRegistration.json")
    .then(res => res.json())
    .then(data => {
      const org = data.find(o => o.dataId == pageId);
      if (!org) {
        document.querySelector(".main-content").innerHTML = "<h1>Организация не найдена</h1>";
        return;
      }

      // Заполняем инфо о проекте (можете расширить)
      document.title = `Проект: ${org.Name}`;
      
      // кнопка "Связаться с организатором"
      const contactBtn = document.querySelector(".contact-btn");
      if (contactBtn) {
        contactBtn.addEventListener("click", () => {
          window.location.href = `prof_organ.html?id=${org.dataId}`;
        });
      }
    })
    .catch(err => {
      console.error("Ошибка загрузки:", err);
      document.querySelector(".main-content").innerHTML = "<h1>Ошибка загрузки</h1>";
    });
});