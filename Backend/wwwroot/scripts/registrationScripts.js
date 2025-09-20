document.addEventListener('DOMContentLoaded', function() {
    // Получаем элементы кнопок
    const orgButton = document.querySelector('.btn-white');
    const investorButton = document.querySelector('.btn-green');
    const form = document.querySelector('.form-container');
    
    // Сохраняем исходное состояние формы (поля для организации)
    const originalFormHTML = form.innerHTML;
    
    // Функция для создания HTML для инвестора (без поля категории)
    function createInvestorFormHTML() {
        return `
            <label class="form-label">Краткое название организации</label>
            <input type="text" placeholder="Название" class="input-field">

            <label class="form-label">Юридический адрес</label>
            <input type="text" placeholder="Адрес" class="input-field">

            <label class="form-label">ФИО руководителя/контактного лица</label>
            <input type="text" placeholder="ФИО" class="input-field">

            <label class="form-label">Телефон руководителя/контактного лица</label>
            <input type="tel" placeholder="Номер телефона" class="input-field">

            <label class="form-label">Электронная почта</label>
            <input type="email" placeholder="Почта" class="input-field">

            <label class="form-label">Пароль</label>
            <input type="password" placeholder="Пароль" class="input-field">

            <label class="form-label">Повторите пароль</label>
            <input type="password" placeholder="Пароль" class="input-field">

            <button type="submit" class="btn-submit">зарегистрироваться</button>
        `;
    }
    
    // Функция для активации кнопки организации
    function activateOrgButton() {
        // Анимация исчезновения
        form.classList.add('fade-out');
        
        setTimeout(() => {
            orgButton.classList.remove('btn-white');
            orgButton.classList.add('btn-green');
            investorButton.classList.remove('btn-green');
            investorButton.classList.add('btn-white');
            
            form.innerHTML = originalFormHTML;
            
            // Анимация появления
            form.classList.remove('fade-out');
            form.classList.add('fade-in');
            
            setTimeout(() => {
                form.classList.remove('fade-in');
            }, 300);
        }, 300);
    }
    
    // Функция для активации кнопки инвестора
    function activateInvestorButton() {
        // Анимация исчезновения
        form.classList.add('fade-out');
        
        setTimeout(() => {
            investorButton.classList.remove('btn-white');
            investorButton.classList.add('btn-green');
            orgButton.classList.remove('btn-green');
            orgButton.classList.add('btn-white');
            
            form.innerHTML = createInvestorFormHTML();
            
            // Анимация появления
            form.classList.remove('fade-out');
            form.classList.add('fade-in');
            
            setTimeout(() => {
                form.classList.remove('fade-in');
            }, 300);
        }, 300);
    }
    
    // Добавляем обработчики событий
    orgButton.addEventListener('click', activateOrgButton);
    investorButton.addEventListener('click', activateInvestorButton);
    
    // Изначально активируем кнопку организации
    activateOrgButton();
    
});