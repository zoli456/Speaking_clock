// js/main.js
(() => {
    'use strict';

    const THEME_KEY = 'theme_preference';
    const rootElement = document.documentElement;
    const themeToggler = document.getElementById('theme-toggler');
    const sunIcon = document.querySelector('.theme-icon-sun');
    const moonIcon = document.querySelector('.theme-icon-moon');

    const getPreferredTheme = () => {
        const storedTheme = localStorage.getItem(THEME_KEY);
        if (storedTheme) {
            return storedTheme;
        }
        // If no stored theme, check OS preference
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };

    const setTheme = (theme) => {
        if (theme === 'auto') {
            rootElement.setAttribute('data-bs-theme', (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'));
        } else {
            rootElement.setAttribute('data-bs-theme', theme);
        }
        localStorage.setItem(THEME_KEY, theme);
        updateTogglerIcon(theme);
    };

    const updateTogglerIcon = (theme) => {
        if (!sunIcon || !moonIcon) return;

        if (theme === 'dark') {
            sunIcon.classList.add('d-none');
            moonIcon.classList.remove('d-none');
        } else {
            sunIcon.classList.remove('d-none');
            moonIcon.classList.add('d-none');
        }
    };

    // Set initial theme on page load
    const initialTheme = getPreferredTheme();
    setTheme(initialTheme);


    if (themeToggler) {
        themeToggler.addEventListener('click', () => {
            const currentTheme = rootElement.getAttribute('data-bs-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }

    // Listen for OS theme changes if the current preference is 'auto' or not set explicitly by user
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = localStorage.getItem(THEME_KEY);
        if (!storedTheme || storedTheme === 'auto') { 
             setTheme(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        }
    });


    // Function to decode Base64 strings
    function decodeBase64(encoded) {
        try {
            return atob(encoded);
        } catch (e) {
            console.error("Failed to decode Base64 string:", e);
            return null;
        }
    }

    // Attach event listeners to all redirect buttons
    const redirectButtons = document.querySelectorAll('.redirect-button');
    redirectButtons.forEach(button => {
        button.addEventListener('click', () => {
            const encodedURL = button.getAttribute('data-encoded');
            if (encodedURL) {
                const decodedURL = decodeBase64(encodedURL);
                if (decodedURL) {
                    window.location.href = decodedURL;
                }
            }
        });
    });

    // Unscramble content on page load
    const contentDiv = document.getElementById('scrambledContent');
    if (contentDiv) {
        const encodedContent = contentDiv.getAttribute('data-encoded');
        if (encodedContent) {
            const decodedContent = decodeBase64(encodedContent);
            if (decodedContent) {
                contentDiv.innerHTML = decodedContent;
                contentDiv.classList.remove('scrambled');
                contentDiv.classList.add('unscrambled');

                const iframe = contentDiv.querySelector('iframe');
                if (iframe) {
                    iframe.classList.add('w-100'); // Make iframe responsive
                }
            }
        }
    }

    // Update footer year
    const currentYearSpan = document.getElementById('currentYear');
    if (currentYearSpan) {
        currentYearSpan.textContent = new Date().getFullYear();
    }

    // Modal image handler
    const imageModal = document.getElementById('imageModal');
    if (imageModal) {
        const modalImage = document.getElementById('modalImage');

        imageModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const triggerLink = event.relatedTarget;
            
            // Get the image source from the data-img-src attribute
            const imageSource = triggerLink.getAttribute('data-img-src');
            
            // Get the alt text from the image inside the link
            const imageAlt = triggerLink.querySelector('img').getAttribute('alt');

            // Update the modal's image source and alt text
            if (modalImage && imageSource) {
                modalImage.setAttribute('src', imageSource);
            }
            if (modalImage && imageAlt) {
                modalImage.setAttribute('alt', imageAlt + " (nagyított)");
            }
            
        });
    }
})();