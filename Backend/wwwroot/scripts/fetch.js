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