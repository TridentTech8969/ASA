/* site.js — vanilla JS (no jQuery)
   - AOS init (if present)
   - Navbar scroll glass effect
   - Smooth anchors
   - Form enhancements (floating labels, email/phone helpers)
   - Stats counter, lazy loading
   - Product card hover
   - Contact form AJAX (posts to /Home/SendMail, expects JSON)
   - Scroll-to-top button, parallax, debounce utilities
*/

(function () {
    "use strict";

    // ===== Helpers =====
    const $ = (sel, root) => (root || document).querySelector(sel);
    const $$ = (sel, root) => Array.prototype.slice.call((root || document).querySelectorAll(sel));

    function debounce(fn, wait) {
        let t;
        return function () {
            clearTimeout(t);
            t = setTimeout(() => fn.apply(this, arguments), wait);
        };
    }

    function showNotification(message, type = "info") {
        // type: success | info | danger
        const n = document.createElement("div");
        n.className = `notification alert alert-${type} alert-dismissible fade show`;
        n.style.cssText = `
      position: fixed;
      top: 100px;
      right: 20px;
      z-index: 9999;
      min-width: 300px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.2);
    `;
        n.innerHTML = `
      <i class="fas fa-${type === "success" ? "check-circle" : type === "danger" ? "times-circle" : "info-circle"} me-2"></i>
      ${message || ""}
      <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close" style="float:right;"></button>
    `;
        document.body.appendChild(n);
        setTimeout(() => n.remove(), 5000);
    }

    function setLoading(btn, isLoading) {
        if (!btn) return;
        if (isLoading) {
            btn.dataset._original = btn.innerHTML;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Sending...';
            btn.disabled = true;
        } else {
            btn.innerHTML = btn.dataset._original || btn.innerHTML;
            btn.disabled = false;
        }
    }

    // ===== AOS Init =====
    function initAOS() {
        if (typeof AOS !== "undefined") {
            AOS.init({ duration: 1000, easing: "ease-in-out", once: true, mirror: false });
        }
    }

    // ===== Navbar scroll effect =====
    function handleNavbarScroll() {
        const navbar = $(".navbar");
        if (!navbar) return;
        if (window.scrollY > 50) {
            navbar.style.background = "rgba(26, 54, 93, 0.98)";
            navbar.style.backdropFilter = "blur(15px)";
            navbar.style.boxShadow = "0 4px 30px rgba(0,0,0,0.2)";
        } else {
            navbar.style.background = "rgba(26, 54, 93, 0.95)";
            navbar.style.backdropFilter = "blur(10px)";
            navbar.style.boxShadow = "0 2px 20px rgba(0,0,0,0.1)";
        }
    }

    // ===== Smooth anchors =====
    function initSmoothScrolling() {
        $$('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener("click", function (e) {
                const href = this.getAttribute("href");
                if (!href || href === "#") return;
                const target = document.querySelector(href);
                if (!target) return;
                e.preventDefault();
                target.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });
    }

    // ===== Form UX =====
    function initFormEnhancements() {
        $$(".form-control").forEach(input => {
            const parent = input.parentElement;
            if (input.value) parent?.classList.add("focused");
            input.addEventListener("focus", () => parent?.classList.add("focused"));
            input.addEventListener("blur", () => { if (!input.value) parent?.classList.remove("focused"); });
        });

        // Basic phone formatting (truncate to 10 digits, format 3-3-4)
        $$('input[type="tel"]').forEach(input => {
            input.addEventListener("input", e => {
                let v = e.target.value.replace(/\D/g, "");
                if (v.length > 10) v = v.substring(0, 10);
                if (v.length > 6) v = v.replace(/(\d{3})(\d{3})(\d{1,4})/, "$1-$2-$3");
                else if (v.length > 3) v = v.replace(/(\d{3})(\d{1,3})/, "$1-$2");
                e.target.value = v;
            });
        });

        // Email quick check (server still validates)
        $$('input[type="email"]').forEach(input => {
            input.addEventListener("blur", function () {
                const email = (this.value || "").trim();
                const ok = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
                this.classList.toggle("is-invalid", !!email && !ok);
                toggleValidationMessage(this, ok ? "" : "Please enter a valid email address");
            });
        });
    }

    function toggleValidationMessage(input, message) {
        if (!input) return;
        const existing = input.parentNode.querySelector(".invalid-feedback");
        if (existing) existing.remove();
        if (!message) return;
        const div = document.createElement("div");
        div.className = "invalid-feedback";
        div.textContent = message;
        input.parentNode.appendChild(div);
    }

    // ===== Stats counter =====
    function initStatsCounter() {
        const stats = $$(".stat-number");
        if (!("IntersectionObserver" in window) || !stats.length) return;

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateNumber(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        });
        stats.forEach(s => observer.observe(s));
    }

    function animateNumber(el) {
        const finalNumber = parseInt(el.textContent.replace(/\D/g, ""), 10) || 0;
        let current = 0;
        const step = Math.max(1, Math.floor(finalNumber / 100));
        const suffix = el.textContent.replace(/[\d,]/g, "");
        const timer = setInterval(() => {
            current += step;
            if (current >= finalNumber) { current = finalNumber; clearInterval(timer); }
            el.textContent = current.toLocaleString() + suffix;
        }, 20);
    }

    // ===== Lazy images =====
    function initLazyLoading() {
        const imgs = $$("img[data-src]");
        if (!("IntersectionObserver" in window) || !imgs.length) return;

        const io = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove("lazy");
                    io.unobserve(img);
                }
            });
        });
        imgs.forEach(img => io.observe(img));
    }

    // ===== Product hover =====
    function initProductHover() {
        $$(".product-card").forEach(card => {
            card.addEventListener("mouseenter", function () {
                this.style.transform = "translateY(-10px) scale(1.02)";
                this.style.boxShadow = "0 20px 50px rgba(0,0,0,0.2)";
            });
            card.addEventListener("mouseleave", function () {
                this.style.transform = "translateY(0) scale(1)";
                this.style.boxShadow = "0 5px 20px rgba(0,0,0,0.1)";
            });
        });
    }

    // ===== Contact form AJAX (no jQuery) =====
    function handleContactForm() {
        debugger
        const form = $("#contactForm");
        if (!form) return;

        const submitBtn = $("#submitBtn");
        form.addEventListener("submit", async function (e) {
            e.preventDefault();

            setLoading(submitBtn, true);

            try {
                // Prepare form data (includes AntiForgery token)
                const formData = new FormData(form);

                const res = await fetch("/Home/SendMail", {
                    method: "POST",
                    headers: { "Accept": "application/json" },
                    body: formData
                });

                const data = await res.json().catch(() => ({}));
                if (res.ok && data && data.success) {
                    showNotification(data.message || "Sent!", "success");
                    form.reset();
                    // remove focused class
                    $$(".form-control", form).forEach(i => i.parentElement?.classList.remove("focused"));
                } else {
                    showNotification((data && data.message) || "Could not send email.", "info");
                }
            } catch (err) {
                showNotification("An error occurred while sending your message. Please try again.", "danger");
                console.error(err);
            } finally {
                setLoading(submitBtn, false);
            }
        });
    }

    // ===== Scroll to top =====
    function initScrollToTop() {
        const btn = document.createElement("button");
        btn.innerHTML = '<i class="fas fa-arrow-up"></i>';
        btn.className = "scroll-to-top";
        btn.style.cssText = `
      position: fixed; bottom: 30px; right: 30px;
      background: var(--accent-color, #ff6b35); color: #fff;
      border: none; width: 50px; height: 50px; border-radius: 50%;
      cursor: pointer; opacity: 0; transform: translateY(100px);
      transition: all .3s ease; z-index: 1000; box-shadow: 0 5px 20px rgba(255,107,53,.3);
    `;
        document.body.appendChild(btn);

        window.addEventListener("scroll", () => {
            if (window.scrollY > 300) { btn.style.opacity = "1"; btn.style.transform = "translateY(0)"; }
            else { btn.style.opacity = "0"; btn.style.transform = "translateY(100px)"; }
        });

        btn.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
        btn.addEventListener("mouseenter", function () {
            this.style.transform = "translateY(-5px) scale(1.1)";
            this.style.boxShadow = "0 10px 30px rgba(255,107,53,.5)";
        });
        btn.addEventListener("mouseleave", function () {
            this.style.transform = "translateY(0) scale(1)";
            this.style.boxShadow = "0 5px 20px rgba(255,107,53,.3)";
        });
    }

    // ===== Parallax (hero background) =====
    function initParallax() {
        const hero = $(".hero-section");
        if (!hero) return;
        window.addEventListener("scroll", () => {
            const rate = window.pageYOffset * -0.5;
            hero.style.backgroundPosition = `center ${rate}px`;
        });
    }

    // ===== Boot =====
    document.addEventListener("DOMContentLoaded", function () {
        initAOS();
        handleNavbarScroll();
        initSmoothScrolling();
        initFormEnhancements();
        initStatsCounter();
        initLazyLoading();
        initProductHover();
        handleContactForm();
        initScrollToTop();
        initParallax();

        window.addEventListener("scroll", debounce(handleNavbarScroll, 10));
    });
})();