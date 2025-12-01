//const openBtn = document.getElementById('clickToCallBtn');
//const callWidget = document.getElementById('callWidget');
//const closeBtn = document.querySelector('.close');
//const form = document.getElementById('contactForm');
//const textarea = document.querySelector('textarea[name="message"]');
//const remainingChars = document.getElementById('remainingChars');
//const charCount = document.getElementById('charCount');

//// Показати форму при натисканні на кнопку
//openBtn.onclick = () => {
//    callWidget.classList.remove('hidden');
//    openBtn.style.display = 'none';
//};

//// Закрити форму
//closeBtn.onclick = () => {
//    callWidget.classList.add('hidden');
//    openBtn.style.display = 'block';
//};

//// Оновлення лічильника символів
//textarea.addEventListener('input', function () {
//    const maxChars = 750;
//    const charsLeft = maxChars - textarea.value.length;

//    // Оновлюємо кількість залишкових символів
//    //remainingChars.textContent = charsLeft;

//    // Якщо залишилось 250 символів або менше, вивести попередження
//    if (charsLeft <= 250) {
//        charCount.style.color = '#f77a7a'; // Колір червоний
//        charCount.textContent = 'Ви наближаєтесь до ліміту символів!';
//    }
//    //else {
//    //    charCount.style.color = '#636e72'; // Нейтральний колір
//    //    charCount.textContent = `Залишилось символів: ${charsLeft}`;
//    //}

//    // Відображення продяного знака при введенні понад 500 символів
//    if (textarea.value.length > 500) {
//        textarea.classList.add('exceed-limit');
//    } else {
//        textarea.classList.remove('exceed-limit');
//    }
//});

//// Відправка форми
//form.onsubmit = function (e) {
//    // e.preventDefault(); // Лишити, якщо хочеш запобігти перезавантаженню сторінки
//    openBtn.style.display = 'block';
//};



    document.addEventListener("DOMContentLoaded", function () {
        const openBtn = document.getElementById("clickToCallBtn");
    const closeBtn = document.querySelector(".call-widget .close");
    const modal = document.getElementById("callWidget");
    const form = document.getElementById("contactForm");
    const message = document.getElementById("message");
    const charCount = document.getElementById("remainingChars");
    const result = document.getElementById("formResult");

        // Відкриття
        openBtn.addEventListener("click", () => modal.classList.remove("hidden"));

        // Закриття
        closeBtn.addEventListener("click", () => modal.classList.add("hidden"));

        // Лічильник символів
        message.addEventListener("input", () => {
            const left = 750 - message.value.length;
    charCount.textContent = left;
        });

    // Відправка через fetch
    form.addEventListener("submit", function (e) {
        e.preventDefault();

    const name = document.getElementById("name").value.trim();
    const email = document.getElementById("email").value.trim();
    const messageText = message.value.trim();

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        result.innerHTML = '<span style="color:red;">❌ Невірний формат email</span>';
    return;
            }

            if (messageText.length > 750) {
        result.innerHTML = '<span style="color:red;">❌ Повідомлення перевищує 750 символів</span>';
    return;
            }

    fetch('/Contact/SendEmail', {
        method: 'POST',
    headers: {'Content-Type': 'application/json' },
    body: JSON.stringify({name, email, message: messageText })
            })
            .then(res => res.ok ? res.text() : Promise.reject(res.text()))
            .then(data => {
        result.innerHTML = '<span style="color:green;">✅ Повідомлення успішно надіслано!</span>';
    form.reset();
    charCount.textContent = '750';
                setTimeout(() => modal.classList.add("hidden"), 2000);
            })
            .catch(async err => {
                const errorText = typeof err === 'string' ? err : await err;
    result.innerHTML = `<span style="color:red;">❌ ${errorText}</span>`;
            });
        });
    });

