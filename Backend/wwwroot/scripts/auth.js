// --- Cookie helpers ---
function setCookie(name, value, days) {
  let expires = "";
  if (days) {
    const date = new Date();
    date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
    expires = "; expires=" + date.toUTCString();
  }
  document.cookie = `${name}=${value || ""}${expires}; path=/`;
}
function getCookie(name) {
  const nameEQ = name + "=";
  const ca = document.cookie.split(";");
  for (let c of ca) {
    while (c.charAt(0) === " ") c = c.substring(1);
    if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length);
  }
  return null;
}
function eraseCookie(name) {
  document.cookie = `${name}=; Max-Age=-99999999; path=/`;
}

// --- UI обновление ---
function updateUI(userEmail) {
  const headerAuth = document.querySelector(".right-section .auth-block-header");
  const mobileDropdown = document.getElementById("profileDropdownMenu");
  const authBlock = document.querySelector("#mobileMenu .auth-block");

  if (headerAuth) headerAuth.innerHTML = "";
  if (mobileDropdown) mobileDropdown.innerHTML = "";
  if (authBlock) authBlock.innerHTML = "";

  if (userEmail) {
    // Авторизован
    const uid = getCookie("userId");

    // --- ПК версия ---
    if (headerAuth) {
      headerAuth.innerHTML = `
        <div class="profile-button" onclick="location.href='prof_organ.html?id=${uid}'">Профиль</div>
        <div class="logout-button" id="logoutDesktop">Выход</div>
      `;
    }

    // --- Мобильное выпадающее меню ---
    if (mobileDropdown) {
      mobileDropdown.innerHTML = `
        <button class="dropdown-item" onclick="location.href='prof_organ.html?id=${uid}'">Профиль</button>
        <div class="dropdown-divider"></div>
        <button class="dropdown-item" id="logoutMobile">Выход</button>
      `;
    }

    // --- Боковое мобильное меню (offcanvas) ---
    if (authBlock) {
      authBlock.innerHTML = `
        <button class="btn w-100 mb-2 catalog" onclick="location.href='prof_organ.html?id=${uid}'">Профиль</button>
        <button class="btn w-100 mb-2 catalog" id="logoutSide">Выход</button>
      `;
    }

    // Выход
    document.getElementById("logoutDesktop")?.addEventListener("click", logout);
    document.getElementById("logoutMobile")?.addEventListener("click", logout);
    document.getElementById("logoutSide")?.addEventListener("click", logout);

  } else {
    // Не авторизован
    if (headerAuth) {
      headerAuth.innerHTML = `
        <div class="login-button" data-bs-toggle="modal" data-bs-target="#loginModal">Вход</div>
        <div class="register-button" onclick="location.href='registration.html'">Регистрация</div>
      `;
    }

    if (mobileDropdown) {
      mobileDropdown.innerHTML = `
        <button class="dropdown-item" data-bs-toggle="modal" data-bs-target="#loginModal">Вход</button>
        <div class="dropdown-divider"></div>
        <button class="dropdown-item" onclick="location.href='registration.html'">Регистрация</button>
      `;
    }

    if (authBlock) {
      authBlock.innerHTML = `
        <button class="btn w-100 mb-2 catalog" data-bs-toggle="modal" data-bs-target="#loginModal">Вход</button>
        <button class="btn w-100 mb-2 catalog" onclick="location.href='registration.html'">Регистрация</button>
      `;
    }
  }

  // 🔽 Проверка кнопки "Редактировать профиль"
  const editBtn = document.querySelector(".edit-btn");
  if (editBtn) {
    const savedUserId = getCookie("userId");
    let pageId = new URLSearchParams(window.location.search).get("id");
    const mainEl = document.querySelector("main[data-id]");
    if (mainEl) pageId = mainEl.dataset.id;

    if (userEmail && savedUserId && pageId && savedUserId === pageId) {
      editBtn.style.display = "flex";
    } else {
      editBtn.style.display = "none";
    }
  }
}

function logout() {
  eraseCookie("userEmail");
  eraseCookie("userId");
  location.reload();
}

// --- Авторизация ---
document.addEventListener("DOMContentLoaded", () => {
  const savedUser = getCookie("userEmail");
  updateUI(savedUser);

  const loginForm = document.getElementById("loginForm");
  if (loginForm) {
    loginForm.addEventListener("submit", async function (e) {
      e.preventDefault();
      const emailInput = this.querySelector('input[type="email"]');
      const passwordInput = this.querySelector('input[type="password"]');
      const email = emailInput.value.trim();
      const password = passwordInput.value.trim();
      emailInput.classList.remove("is-invalid");
      passwordInput.classList.remove("is-invalid");

      try {
        const response = await fetch("data/submitRegistration.json");
        if (!response.ok) throw new Error("Не удалось загрузить файл данных");

        const users = await response.json();
        const user = users.find((u) => u.Email === email);

        if (!user) {
          emailInput.classList.add("is-invalid");
          passwordInput.classList.add("is-invalid");
          alert("Пользователь не зарегистрирован");
          return;
        }
        if (user.Password !== password) {
          passwordInput.classList.add("is-invalid");
          alert("Неверный пароль");
          return;
        }

        setCookie("userEmail", user.Email, 7);
        setCookie("userId", user.dataId, 7);

        bootstrap.Modal.getInstance(document.getElementById("loginModal")).hide();
        updateUI(user.Email);
      } catch (err) {
        console.error("Ошибка авторизации:", err);
        alert("Ошибка при проверке пользователя: " + err.message);
      }
    });
  }
});

document.addEventListener("DOMContentLoaded", () => {
  function adjustDescriptionClamp(titleSelector, descSelector) {
    document.querySelectorAll(titleSelector).forEach(title => {
      const desc = title.closest("div").querySelector(descSelector);
      if (!desc) return;

      // Вычисляем количество строк у заголовка
      const lineHeight = parseFloat(window.getComputedStyle(title).lineHeight);
      const lines = Math.round(title.scrollHeight / lineHeight);

      // Ограничиваем описание
      if (lines > 1) {
        desc.style.setProperty("-webkit-line-clamp", "4");
      } else {
        desc.style.setProperty("-webkit-line-clamp", "5");
      }
    });
  }

  // Главная страница (organization-card)
  adjustDescriptionClamp(".org-title", ".org-description");

  // Поиск (search page)
  adjustDescriptionClamp(".org-name", ".org-description");

  // Страница проекта
  adjustDescriptionClamp(".project-title", ".project-description");
});
