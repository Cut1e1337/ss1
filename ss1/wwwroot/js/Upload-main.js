// Функція для обробки зміни фото
function handleFileChange(event, boxIndex) {
    const fileInput = event.target;
    const fileCount = fileInput.files.length;

    // Якщо вибрано фото, оновлюємо фон кубика
    if (fileCount > 0) {
        const photoBox = document.getElementById(`photoBox${boxIndex}`);
        const file = fileInput.files[0];
        const reader = new FileReader();

        reader.onload = function (e) {
            photoBox.style.backgroundImage = `url(${e.target.result})`;
        };

        reader.readAsDataURL(file);

        // Додаємо новий кубик, якщо є місце
        if (photoCount < maxPhotos) {
            const nextBoxIndex = boxIndex + 1;
            const newPhotoBox = createPhotoBox(nextBoxIndex);
            document.querySelector('.upload-container').appendChild(newPhotoBox);
            photoCount++;
        }

        // Активуємо кнопку завантаження, якщо умови погоджені
        document.getElementById('submitBtn').disabled = !document.querySelector('input[name="acceptTerms"]').checked;
    }
}

// Створення нового кубика для завантаження фото
function createPhotoBox(index) {
    const photoBox = document.createElement('div');
    photoBox.classList.add('photo-box');
    photoBox.id = `photoBox${index}`;

    const input = document.createElement('input');
    input.type = 'file';
    input.name = 'files';
    input.accept = 'image/*';
    input.id = `fileInput${index}`;
    input.setAttribute('onchange', `handleFileChange(event, ${index})`);

    const icon = document.createElement('div');
    icon.classList.add('add-icon');
    icon.textContent = '+';

    photoBox.appendChild(input);
    photoBox.appendChild(icon);

    return photoBox;
}

// Функція для активації кнопки після погодження з умовами
function toggleSubmitButton() {
    const isChecked = document.querySelector('input[name="acceptTerms"]').checked;
    document.getElementById('submitBtn').disabled = !isChecked;
}

// Функція для асинхронного відправлення форми
function submitForm() {
    const formData = new FormData(document.getElementById('uploadForm'));

    fetch('/Photo/Upload', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                document.getElementById('successMessage').innerText = data.message;
            } else {
                document.getElementById('errorMessage').innerText = data.message;
            }
        })
        .catch(error => {
            document.getElementById('errorMessage').innerText = 'Виникла помилка при завантаженні.';
            console.error('Error:', error);
        });
}
