
// scripts/registrationScripts.js
(function () {
    'use strict';

    const DADATA_TOKEN = "27968798a321926c35170d91267f54526a5b7ab9";

    // -------------------
    // Инициализация DaData
    // -------------------
    function initDadataSuggestions() {
        if (!window.jQuery || !$.fn || !$.fn.suggestions) {
            console.warn("jQuery или DaData suggestions ещё не загружены");
            return;
        }

        // Оборачиваем в try/catch — чтобы не ломать всё при ошибке виджета
        try {
            $("#organizationName").suggestions({
                token: DADATA_TOKEN,
                type: "PARTY",
                count: 7,
                minChars: 2,
                onSelect: function (suggestion) {
                    const data = suggestion?.data;
                    if (data?.address?.value) {
                        $("#address").val(data.address.value);
                    }
                    if (data?.management?.name) {
                        $("#FIO").val(data.management.name);
                    }
                }
            });

            $("#address").suggestions({
                token: DADATA_TOKEN,
                type: "ADDRESS",
                count: 7,
                minChars: 3
            });

            $("#FIO").suggestions({
                token: DADATA_TOKEN,
                type: "NAME",
                count: 5,
                minChars: 1
            });

            $("#mail").suggestions({
                token: DADATA_TOKEN,
                type: "EMAIL",
                count: 5,
                minChars: 3
            });

            console.log("DaData: подсказки инициализированы");
        } catch (err) {
            console.warn("DaData init error:", err);
        }
    }

    // -------------------
    // Переключение форм
    // -------------------
    document.addEventListener('DOMContentLoaded', function () {
        const orgButton = document.querySelector('.btn-white');
        const investorButton = document.querySelector('.btn-green');
        const form = document.querySelector('.form-container');

        // текущий тип пользователя (храним отдельно, не полагаясь на DOM)
        let currentUserType = 'organization';

        // Сохраняем исходную разметку формы (включая hidden #userType)
        const originalFormHTML = form.innerHTML;

        function createInvestorFormHTML() {
            // Вставляем скрытое поле userType, чтобы DOM-консистентность была сохранена
            return `
                <input type="hidden" id="userType" name="userType" value="investor">
                <label class="form-label">Краткое название организации</label>
                <input type="text" placeholder="Название" class="input-field" id="organizationName">

                <label class="form-label">Юридический адрес</label>
                <input type="text" placeholder="Адрес" class="input-field" id="address">

                <label class="form-label">ФИО руководителя/контактного лица</label>
                <input type="text" placeholder="ФИО" class="input-field" id="FIO">

                <label class="form-label">Телефон руководителя/контактного лица</label>
                <input type="tel" placeholder="+7 (XXX) XXX-XX-XX" class="input-field" id="phone">

                <label class="form-label">Электронная почта</label>
                <input type="email" placeholder="Почта" class="input-field" id="mail">

                <label class="form-label">Пароль</label>
                <input type="password" placeholder="Пароль" class="input-field" id="password">

                <label class="form-label">Повторите пароль</label>
                <input type="password" placeholder="Пароль" class="input-field" id="password2">

                <button type="submit" class="btn-submit">зарегистрироваться</button>
            `;
        }

        function activateOrgButton() {
            form.classList.add('fade-out');
            setTimeout(() => {
                currentUserType = 'organization';

                orgButton.classList.add('btn-green');
                orgButton.classList.remove('btn-white');
                investorButton.classList.add('btn-white');
                investorButton.classList.remove('btn-green');

                // Восстанавливаем исходную разметку (в ней уже есть hidden #userType)
                form.innerHTML = originalFormHTML;

                // Обновим hidden поле, если оно присутствует (на случай, если value изменился)
                const userTypeEl = document.getElementById('userType');
                if (userTypeEl) userTypeEl.value = 'organization';

                setupPhoneFormatting();
                setupFormSubmit();
                initDadataSuggestions();

                form.classList.remove('fade-out');
                form.classList.add('fade-in');
                setTimeout(() => form.classList.remove('fade-in'), 300);
            }, 300);
        }

        function activateInvestorButton() {
            form.classList.add('fade-out');
            setTimeout(() => {
                currentUserType = 'investor';

                investorButton.classList.add('btn-green');
                investorButton.classList.remove('btn-white');
                orgButton.classList.add('btn-white');
                orgButton.classList.remove('btn-green');

                form.innerHTML = createInvestorFormHTML();

                // Обновляем hidden поле если нужно (createInvestorFormHTML уже его вставляет),
                // но на всякий случай:
                const userTypeEl = document.getElementById('userType');
                if (userTypeEl) userTypeEl.value = 'investor';

                setupPhoneFormatting();
                setupFormSubmit();
                initDadataSuggestions();

                form.classList.remove('fade-out');
                form.classList.add('fade-in');
                setTimeout(() => form.classList.remove('fade-in'), 300);
            }, 300);
        }

        // -------------------
        // Маска телефона (replace handler, чтобы не дублировать)
        // -------------------
        function setupPhoneFormatting() {
            const phoneInput = document.getElementById('phone');
            if (phoneInput) {
                phoneInput.oninput = function (e) {
                    let digits = e.target.value.replace(/\D/g, "");

                    if (digits.startsWith("8")) {
                        digits = "7" + digits.substring(1);
                    }
                    if (!digits.startsWith("7")) {
                        digits = "7" + digits;
                    }
                    digits = digits.substring(0, 11);

                    let formatted = "+7";
                    if (digits.length > 1) formatted += " (" + digits.substring(1, 4);
                    if (digits.length >= 4) formatted += ") " + digits.substring(4, 7);
                    if (digits.length >= 7) formatted += "-" + digits.substring(7, 9);
                    if (digits.length >= 9) formatted += "-" + digits.substring(9, 11);

                    e.target.value = formatted;
                };
            }
        }

        // -------------------
        // Отправка формы (безопасные чтения полей)
        // -------------------
        function setupFormSubmit() {
            const formElement = document.querySelector('.form-container');
            if (!formElement) return;

            formElement.onsubmit = async function (e) {
                e.preventDefault();

                const password = document.getElementById('password')?.value.trim() || "";
                const confirmPassword = document.getElementById('password2')?.value.trim() || "";

                if (password.length < 8 || password.length > 20) {
                    alert("Пароль должен содержать от 8 до 20 символов!");
                    return;
                }

                if (password !== confirmPassword) {
                    alert('Пароли не совпадают!');
                    return;
                }

                // Используем переменную currentUserType (не обращаемся напрямую к DOM за userType)
                const userType = currentUserType;

                function getValue(id) {
                    const el = document.getElementById(id);
                    return el ? el.value.trim() : "";
                }

                function getDigits(id) {
                    const el = document.getElementById(id);
                    return el ? el.value.replace(/\D/g, '') : "";
                }

                const formData = {
                    UserType: userType,
                    Name: getValue('organizationName'),
                    LeaderName: getValue('FIO'),
                    Phone: getDigits('phone'),
                    Email: getValue('mail'),
                    Password: password,
                    FullAddress: getValue('address'),
                    RegistrationDate: new Date().toISOString()
                };

                if (userType === 'organization') {
                    const industryElement = document.getElementById('industry');
                    if (industryElement) {
                        formData.Industry = industryElement.value.trim();
                        if (!formData.Industry) {
                            alert("Поле Категория обязательно для организаций!");
                            return;
                        }
                    } else {
                        // если DOM не содержит select — считаем это ошибкой для организации
                        alert("Поле Категория обязательно для организаций!");
                        return;
                    }
                }

                // Проверка обязательных полей (для инвестора пропускаем Industry)
                for (const [key, value] of Object.entries(formData)) {
                    if (userType === "investor" && key === "Industry") {
                        continue;
                    }
                    if (!value) {
                        alert(`Поле ${key} обязательно для заполнения`);
                        return;
                    }
                }

                try {
                    const jsonData = JSON.stringify(formData, null, 2);
                    const blob = new Blob([jsonData], { type: 'application/json' });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = 'submitRegistration.json';
                    document.body.appendChild(a);
                    a.click();
                    setTimeout(() => {
                        document.body.removeChild(a);
                        URL.revokeObjectURL(url);
                    }, 100);

                    alert('Данные сохранены в файл submitRegistration.json!');
                    this.reset();
                } catch (error) {
                    alert('Ошибка при создании файла: ' + error.message);
                }
            };
        }

        // -------------------
        // Старт (подключаем обработчики)
        // -------------------
        if (orgButton) orgButton.addEventListener('click', activateOrgButton);
        if (investorButton) investorButton.addEventListener('click', activateInvestorButton);

        // По умолчанию показываем организацию (внутри activateOrgButton восстановится hidden #userType и инициализации)
        activateOrgButton();
    });

})();
