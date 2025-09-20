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


document.addEventListener('DOMContentLoaded', function() {
    const postWrapper = document.querySelector("#post-wrapper");
    
    // Очищаем контейнер
    postWrapper.innerHTML = '';

    // Загружаем данные из JSON файла
    fetch('/data/companies.json')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Добавляем карточки из загруженных данных
            data.organizations.forEach((org) => {
                postWrapper.innerHTML += `
                    <div data-id="${org.id}" class="organization-card">
                        <div class="card-image"><img class="org-image" src="static/${org.image}" onerror="this.onerror=null; this.src='static/FotoLogo.jpg';" alt="${org.title}"></div>
                        <div class="card-content">
                            <h3 class="org-name">${org.title}</h3>
                            <p class="org-description">${org.description.replace(/\s+/g, ' ').trim().length > 150 ? org.description.replace(/\s+/g, ' ').trim().substring(0, 150) + '...' : org.description.replace(/\s+/g, ' ').trim()}</p>
                            <button class="details-button" onclick="location.href='project.html'">Подробнее</button>
                        </div>
                    </div>`;
            });
        })
        .catch(error => {
            console.error('Error loading organizations:', error);
            postWrapper.innerHTML = '<p>Произошла ошибка при загрузке данных</p>';
        });
});