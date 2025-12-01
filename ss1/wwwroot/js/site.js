

        // Приховує хедер
		let lastScrollY = window.scrollY;

		window.addEventListener("scroll", () => {
			const navbar = document.querySelector(".navbar");

			if (window.scrollY > lastScrollY) { 
				navbar.classList.add("hidden");
			} else { // скрол вгору
				navbar.classList.remove("hidden");
			}

			lastScrollY = window.scrollY;
		});
        


        // Показуємо стрілку при прокрутці вниз
        window.onscroll = function() {
            const scrollToTopButton = document.querySelector('.scroll-to-top');
            if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
                scrollToTopButton.style.display = "block";
            } else {
                scrollToTopButton.style.display = "none";
            }
        };

        // Функція плавної прокрутки на початок сторінки
        function scrollToTop() {
            window.scrollTo({
                top: 0,
                behavior: 'smooth' // Плавна прокрутка
            });
        }


		// Анімація при прокрутці
		document.addEventListener('DOMContentLoaded', function () {
			const animateElements = document.querySelectorAll('.animate-on-scroll');

			function checkScroll() {
				animateElements.forEach(element => {
					const elementTop = element.getBoundingClientRect().top;
					const windowHeight = window.innerHeight;

					if (elementTop < windowHeight * 0.8) {
						element.classList.add('visible');
					}
				});
			}

			window.addEventListener('scroll', checkScroll);
			checkScroll(); 
		});

		// Плавна прокрутка для навігації
		document.querySelectorAll('a[href^="#"]').forEach(anchor => {
			anchor.addEventListener('click', function (e) {
				e.preventDefault();
				const target = document.querySelector(this.getAttribute('href'));
				if (target) {
					target.scrollIntoView({
						behavior: 'smooth',
						block: 'start'
					});
				}
			});
		});

		// Фіксована навігація при прокрутці
		//const navbar = document.getElementById('navbar');
		//let lastScroll = 0;

		//window.addEventListener('scroll', () => {
		//	const currentScroll = window.pageYOffset;

		//	if (currentScroll <= 0) {
		//		navbar.classList.remove('scroll-up');
		//		return;
		//	}

		//	if (currentScroll > lastScroll && !navbar.classList.contains('scroll-down')) {
		//		navbar.classList.remove('scroll-up');
		//		navbar.classList.add('scroll-down');
		//	} else if (currentScroll < lastScroll && navbar.classList.contains('scroll-down')) {
		//		navbar.classList.remove('scroll-down');
		//		navbar.classList.add('scroll-up');
		//	}
		//	lastScroll = currentScroll;
		//});

		// Мобільне меню
		//const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
		//const navLinks = document.querySelector('.nav-links');

		//mobileMenuBtn.addEventListener('click', () => {
		//	mobileMenuBtn.classList.toggle('active');
		//	navLinks.classList.toggle('active');
		//});



		// Мобільне Бургер

document.addEventListener("DOMContentLoaded", function () {
	const burger = document.getElementById('burger');
	const navLinks = document.querySelector('.nav-links');

	if (burger && navLinks) {
		burger.addEventListener('click', () => {
			navLinks.classList.toggle('active');
		});
	}
});
// Мобільне Бургер

function handleOrderClick() {
	const isAuthenticated = '@User.Identity.IsAuthenticated'.toLowerCase();

	if (isAuthenticated === 'true') {
		window.location.href = '/Profile';
	} else {
		showAlert("Замовити обробку можна в особистому кабінеті.");
	}
}

function showAlert(message) {
	if (document.querySelector('.alert-top')) return;

	const alertBox = document.createElement('div');
	alertBox.className = 'alert-top';
	alertBox.innerText = message;
	document.body.appendChild(alertBox);

	setTimeout(() => {
		alertBox.remove();
	}, 4000);
}


