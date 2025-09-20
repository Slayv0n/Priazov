function scrollToBottom() {
            window.scrollTo({
                top: document.body.scrollHeight,
                behavior: 'smooth' // Плавная прокрутка
            });
        }


        
document.addEventListener('DOMContentLoaded', function() {
    const postWrapper = document.querySelector("#post-wrapper");
    
    postWrapper.addEventListener("click", (e) => {
        // Находим ближайшую родительскую карточку с data-id
        const card = e.target.closest('[data-id]');
        
        if (!card) return; // Если клик не по карточке - выходим
        
        const id = card.dataset.id;
        
        // Перенаправляем на project.html с параметром id
        window.location.href = `project-profile.html?id=${id}`;
    });
});


// Вывод карточек организаций
document.addEventListener('DOMContentLoaded', function() {
    const postWrapper = document.querySelector("#post-wrapper");
    
    // Функция для правильного склонения слова "организация"
    function getOrganizationWord(count) {
        const lastDigit = count % 10;
        const lastTwoDigits = count % 100;
        
        if (lastTwoDigits >= 11 && lastTwoDigits <= 19) {
            return 'организаций';
        }
        
        if (lastDigit === 1) {
            return 'организация';
        }
        
        if (lastDigit >= 2 && lastDigit <= 4) {
            return 'организации';
        }
        
        return 'организаций';
    }

    // Очищаем контейнер
    postWrapper.innerHTML = '';

    // Загружаем данные из JSON файла
    fetch('../data/companies.json')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            const organizations = data.organizations;
            const totalOrganizations = organizations.length;
            const organizationsToShow = organizations.slice(0, 5); // Берем только первые 5 организаций
            const remainingCount = totalOrganizations - 5; // Количество оставшихся организаций

            // Добавляем карточки из загруженных данных (только первые 5)
            organizationsToShow.forEach((org) => {
                postWrapper.innerHTML += `
                    <div data-id="${org.id}" class="organization-card">
                        <img class="org-image" src="static/${org.image}" onerror="this.onerror=null; this.src='static/FotoLogo.jpg';">
                        <h3 class="org-title">${org.title}</h3>
                        <p class="org-description">${org.description.replace(/\s+/g, ' ').trim().length > 150 ? org.description.replace(/\s+/g, ' ').trim().substring(0, 150) + '...' : org.description.replace(/\s+/g, ' ').trim()}</p>
                        <div class="org-details">
                            <div class="detail">
                                <span class="detail-icon">📍</span>
                                <span class="detail-text">${org.address}</span>
                            </div>
                            <div class="detail">
                                <span class="detail-icon">🏛️</span>
                                <span class="detail-text">${org.type}</span>
                            </div>
                        </div>
                    </div>`;
            });

            // Добавляем карточку с количеством оставшихся организаций
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
        .catch(error => {
            console.error('Error loading organizations:', error);
            postWrapper.innerHTML = '<p>Произошла ошибка при загрузке данных</p>';
        });
});
//Стрелочки
document.addEventListener('DOMContentLoaded', function () {
            const faqItems = document.querySelectorAll('.faq-item');

            faqItems.forEach(item => {
                const question = item.querySelector('.faq-question');
                const arrow = item.querySelector('.farrow');

                question.addEventListener('click', () => {
                    // Закрываем все открытые элементы, кроме текущего
                    faqItems.forEach(otherItem => {
                        if (otherItem !== item && otherItem.classList.contains('active')) {
                            otherItem.classList.remove('active');
                            otherItem.querySelector('.farrow').classList.remove('farrow-rotated');
                        }
                    });

                    // Переключаем текущий элемент
                    item.classList.toggle('active');
                    arrow.classList.toggle('farrow-rotated');
                });
            });
        });

//Инициализация карты
function init() {
    // Создаем карту с центром в Москве
    const map = new ymaps.Map('map', {
        center: [55.751574, 37.573856],  // Москва
        zoom: 10
    });

    // Массив с координатами и данными меток
    const markersData = [
        { coords: [55.751574, 37.573856], title: 'Красная площадь' },
        { coords: [55.733842, 37.588648], title: 'Парк Горького' },
        { coords: [55.710087, 37.614668], title: 'Воробьевы горы' }
    ];

    // Коллекция для хранения меток
    const markersCollection = new ymaps.GeoObjectCollection(null, {
        preset: 'islands#blueIcon'  // Стандартный стиль для меток
    });

    // Создаем метки и добавляем их в коллекцию
    markersData.forEach(marker => {
        const placemark = new ymaps.Placemark(
            marker.coords,
            { balloonContent: marker.title },
            { preset: 'islands#blueIcon' }
        );
        markersCollection.add(placemark);
    });

    // Добавляем коллекцию меток на карту
    map.geoObjects.add(markersCollection);

    // Обработчик для кнопки переключения меток
    const toggleButton = document.getElementById('toggleButton');
    let markersVisible = true;

    toggleButton.addEventListener('click', function () {
        if (markersVisible) {
            // Удаляем коллекцию с карты
            map.geoObjects.remove(markersCollection);
            toggleButton.textContent = 'Показать метки';
        } else {
            // Добавляем коллекцию обратно на карту
            map.geoObjects.add(markersCollection);
            toggleButton.textContent = 'Скрыть метки';
        }
        markersVisible = !markersVisible;
    });
}

// Функция для переключения выпадающего меню
document.addEventListener('DOMContentLoaded', function() {
    const dropdownButton = document.getElementById('profileDropdownButton');
    const dropdownMenu = document.getElementById('profileDropdownMenu');
    
    if (dropdownButton && dropdownMenu) {
        dropdownButton.addEventListener('click', function(e) {
            e.stopPropagation();
            dropdownMenu.classList.toggle('show');
        });
        
        // Закрытие меню при клике вне его
        document.addEventListener('click', function(e) {
            if (!dropdownMenu.contains(e.target) && !dropdownButton.contains(e.target)) {
                dropdownMenu.classList.remove('show');
            }
        });
        
        // Предотвращаем закрытие при клике внутри меню
        dropdownMenu.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    }
});