document.addEventListener('DOMContentLoaded', () => {
    document.querySelector('.registration-form')?.addEventListener('submit', async function (e) {
        e.preventDefault();

        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('password2').value;

        if (password !== confirmPassword) {
            alert('Пароли не совпадают!');
            return;
        }

        const formData = {
            Name: document.getElementById('organizationName').value.trim(),
            LeaderName: document.getElementById('FIO').value.trim(),
            Phone: document.getElementById('phone').value.replace(/\D/g, ''),
            Email: document.getElementById('mail').value.trim(),
            Password: password,
            FullAddress: document.getElementById('address').value.trim(),
            Industry: document.getElementById('industry').value.trim()
        };

        for (const [key, value] of Object.entries(formData)) {
            if (!value) {
                alert(`Поле ${key} обязательно для заполнения`);
                return;
            }
        }

        try {
            const response = await fetch('/companies/create', {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (!response.ok) {
                // Начало обработки ошибок
                const errors = result.errors || [];
                const errorMessage = errors.length > 0 ? errors.join(', ') : 'Нет ошибок';
                if (errors.length > 0) { throw new Error(`Ошибка: ${result.message}\n${errorMessage}\nОшибка сервера: ${response.status}`); }

                // Конец обработки ошибок
            }

            alert('Регистрация успешна!');


        } catch (error) {
            alert(error.message);
        }
    });
});


document.getElementById('phone').addEventListener('input', function (e) {
    let x = e.target.value.replace(/\D/g, '').match(/(\d{0,1})(\d{0,3})(\d{0,3})(\d{0,2})(\d{0,2})/);
    e.target.value = !x[2] ? x[1] : x[1] + ' (' + x[2] + ') ' + x[3] + (x[4] ? '-' + x[4] : '') + (x[5] ? '-' + x[5] : '');
});

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