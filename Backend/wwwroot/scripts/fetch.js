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

// Вывод карточек организаций (главная страница)
document.addEventListener("DOMContentLoaded", function () {
  const postWrapper = document.querySelector("#post-wrapper");
  if (!postWrapper) return;

  function getOrganizationWord(count) {
    const lastDigit = count % 10;
    const lastTwoDigits = count % 100;
    if (lastTwoDigits >= 11 && lastTwoDigits <= 19) return "организаций";
    if (lastDigit === 1) return "организация";
    if (lastDigit >= 2 && lastDigit <= 4) return "организации";
    return "организаций";
  }

  postWrapper.innerHTML = "";
  fetch("../data/companies.json")
    .then((response) => {
      if (!response.ok) throw new Error("Network response was not ok");
      return response.json();
    })
    .then((data) => {
      const organizations = data.organizations;
      const totalOrganizations = organizations.length;
      const organizationsToShow = organizations.slice(0, 5);
      const remainingCount = totalOrganizations - 5;

      organizationsToShow.forEach((org) => {
        postWrapper.innerHTML += `
          <div data-id="${org.id}" class="organization-card">
            <img class="org-image" src="static/${org.image}" onerror="this.onerror=null; this.src='static/FotoLogo.jpg';">
            <h3 class="org-title">${org.title}</h3>
            <p class="org-description">${
              org.description.replace(/\s+/g, " ").trim().length > 150
                ? org.description.replace(/\s+/g, " ").trim().substring(0, 150) +
                  "..."
                : org.description.replace(/\s+/g, " ").trim()
            }</p>
            <div class="org-details">
              <div class="detail"><span class="detail-icon">📍</span><span class="detail-text">${org.address}</span></div>
              <div class="detail"><span class="detail-icon">🏛️</span><span class="detail-text">${org.type}</span></div>
            </div>
          </div>`;
      });

      if (remainingCount > 0) {
        const organizationWord = getOrganizationWord(remainingCount);
        postWrapper.innerHTML += `
          <div class="organization-card more-card" onclick="location.href='search.html'">
            <div class="more-content">
              <h3>Ещё ${remainingCount} ${organizationWord}</h3>
              <p>Нажмите, чтобы увидеть все организации</p>
              <button class="view-all-button">Смотреть все</button>
            </div>
          </div>`;
      }
    })
    .catch((error) => {
      console.error("Error loading organizations:", error);
      postWrapper.innerHTML = "<p>Произошла ошибка при загрузке данных</p>";
    });
});


// FAQ стрелочки
document.addEventListener("DOMContentLoaded", function () {
  const faqItems = document.querySelectorAll(".faq-item");
  faqItems.forEach((item) => {
    const arrow = item.querySelector(".farrow");
    item.addEventListener("click", () => {
      faqItems.forEach((otherItem) => {
        if (otherItem !== item && otherItem.classList.contains("active")) {
          otherItem.classList.remove("active");
          otherItem.querySelector(".farrow").classList.remove("farrow-rotated");
        }
      });
      item.classList.toggle("active");
      arrow.classList.toggle("farrow-rotated");
    });
  });
});

// Карта Яндекс (если есть контейнер)

function init() { const map = new ymaps.Map("map", { center: [55.751574, 37.573856], zoom: 10, }); const markersData = [ { coords: [55.751574, 37.573856], title: "Красная площадь" }, { coords: [55.733842, 37.588648], title: "Парк Горького" }, { coords: [55.710087, 37.614668], title: "Воробьевы горы" }, ]; const markersCollection = new ymaps.GeoObjectCollection(null, { preset: "islands#blueIcon", }); markersData.forEach((marker) => { const placemark = new ymaps.Placemark( marker.coords, { balloonContent: marker.title }, { preset: "islands#blueIcon" } ); markersCollection.add(placemark); }); map.geoObjects.add(markersCollection); const toggleButton = document.getElementById("toggleButton"); let markersVisible = true; if (toggleButton) { toggleButton.addEventListener("click", function () { if (markersVisible) { map.geoObjects.remove(markersCollection); toggleButton.textContent = "Показать метки"; } else { map.geoObjects.add(markersCollection); toggleButton.textContent = "Скрыть метки"; } markersVisible = !markersVisible; }); } }



// Выпадающее меню профиля (мобильное)
document.addEventListener("DOMContentLoaded", function () {
  const dropdownButton = document.getElementById("profileDropdownButton");
  const dropdownMenu = document.getElementById("profileDropdownMenu");
  if (dropdownButton && dropdownMenu) {
    dropdownButton.addEventListener("click", function (e) {
      e.stopPropagation();
      dropdownMenu.classList.toggle("show");
    });
    document.addEventListener("click", function (e) {
      if (!dropdownMenu.contains(e.target) && !dropdownButton.contains(e.target)) {
        dropdownMenu.classList.remove("show");
      }
    });
    dropdownMenu.addEventListener("click", function (e) {
      e.stopPropagation();
    });
  }
});

// Переключение карт
document.addEventListener("DOMContentLoaded", () => {
  const cards = document.querySelectorAll(".about-card");
  const prevBtn = document.querySelector(".prev-button");
  const nextBtn = document.querySelector(".next-button");
  const slider = document.querySelector(".about-slider");
  const indicatorsContainer = document.querySelector(".about-indicators");

  let currentIndex = 0;
  let autoSlide;
  let startX = 0;

  // Создаём точки
  cards.forEach((_, i) => {
    const dot = document.createElement("div");
    dot.classList.add("dot");
    if (i === 0) dot.classList.add("active");
    dot.addEventListener("click", () => {
      currentIndex = i;
      showCard(currentIndex);
      resetAutoSlide();
    });
    indicatorsContainer.appendChild(dot);
  });

  const dots = document.querySelectorAll(".dot");

  function showCard(index) {
    cards.forEach((card, i) => card.classList.toggle("active", i === index));
    dots.forEach((dot, i) => dot.classList.toggle("active", i === index));
  }

  function nextCard() {
    currentIndex = (currentIndex + 1) % cards.length;
    showCard(currentIndex);
  }

  function prevCard() {
    currentIndex = (currentIndex - 1 + cards.length) % cards.length;
    showCard(currentIndex);
  }

  // Автопрокрутка
  function startAutoSlide() {
    autoSlide = setInterval(nextCard, 60000);
  }

  function resetAutoSlide() {
    clearInterval(autoSlide);
    startAutoSlide();
  }

  // Навигация кнопками
  nextBtn.addEventListener("click", () => {
    nextCard();
    resetAutoSlide();
  });
  prevBtn.addEventListener("click", () => {
    prevCard();
    resetAutoSlide();
  });

  // Пауза при наведении
  slider.addEventListener("mouseenter", () => clearInterval(autoSlide));
  slider.addEventListener("mouseleave", startAutoSlide);

  // 📱 Свайп на мобильных
  slider.addEventListener("touchstart", (e) => {
    startX = e.touches[0].clientX;
    clearInterval(autoSlide); // останавливаем автопрокрутку во время свайпа
  });

  slider.addEventListener("touchend", (e) => {
    const endX = e.changedTouches[0].clientX;
    const diffX = endX - startX;

    if (Math.abs(diffX) > 50) { // порог для срабатывания свайпа
      if (diffX > 0) {
        prevCard(); // свайп вправо → предыдущий
      } else {
        nextCard(); // свайп влево → следующий
      }
    }

    resetAutoSlide(); // после свайпа перезапускаем автопрокрутку
  });

  // Запуск
  showCard(currentIndex);
  startAutoSlide();
});

