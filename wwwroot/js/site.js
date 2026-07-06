(() => {
  'use strict';

  document.addEventListener('DOMContentLoaded', () => {
    initNav();
    initReveal();
    initRoomFilters();
    initGallery();
    initTestimonials();
    initReservationForm();
  });

  // ---------- nav ----------

  function initNav() {
    const nav = document.getElementById('site-nav');
    const toggle = document.getElementById('nav-toggle');
    const links = document.getElementById('nav-links');
    if (!nav) return;

    const onScroll = () => {
      const solid = (window.scrollY || document.documentElement.scrollTop || 0) > 40;
      nav.classList.toggle('is-solid', solid);
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    if (toggle && links) {
      toggle.addEventListener('click', () => {
        const open = links.classList.toggle('is-open');
        toggle.classList.toggle('is-open', open);
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
      });

      links.querySelectorAll('a').forEach((a) => {
        a.addEventListener('click', () => {
          links.classList.remove('is-open');
          toggle.classList.remove('is-open');
          toggle.setAttribute('aria-expanded', 'false');
        });
      });
    }
  }

  // ---------- scroll reveal ----------

  function initReveal() {
    const items = document.querySelectorAll('.reveal');
    if (!items.length) return;

    if (!('IntersectionObserver' in window)) {
      items.forEach((el) => el.classList.add('is-visible'));
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: '0px 0px -60px 0px' }
    );

    items.forEach((el) => observer.observe(el));
  }

  // ---------- room filters ----------

  function initRoomFilters() {
    const filterBar = document.getElementById('room-filters');
    if (!filterBar) return;

    const chips = filterBar.querySelectorAll('.chip');
    const cards = document.querySelectorAll('.room-card-wrap');

    chips.forEach((chip) => {
      chip.addEventListener('click', () => {
        const filter = chip.dataset.filter;

        chips.forEach((c) => c.classList.toggle('is-active', c === chip));

        cards.forEach((card) => {
          const show = filter === 'all' || card.dataset.category === filter;
          card.classList.toggle('is-hidden', !show);
        });
      });
    });

    document.querySelectorAll('.btn-book').forEach((btn) => {
      btn.addEventListener('click', () => {
        const roomSelect = document.getElementById('room-select');
        if (roomSelect) {
          roomSelect.value = btn.dataset.room || '';
        }
        const contact = document.getElementById('contact');
        if (contact) {
          contact.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    });
  }

  // ---------- gallery lightbox ----------

  function initGallery() {
    const items = Array.from(document.querySelectorAll('.gallery-item'));
    const lightbox = document.getElementById('lightbox');
    if (!items.length || !lightbox) return;

    const image = document.getElementById('lightbox-image');
    const caption = document.getElementById('lightbox-caption');
    const closeBtn = document.getElementById('lightbox-close');
    const prevBtn = document.getElementById('lightbox-prev');
    const nextBtn = document.getElementById('lightbox-next');

    let index = 0;

    const show = (i) => {
      index = (i + items.length) % items.length;
      const item = items[index];
      const src = item.querySelector('img').getAttribute('src');
      const cap = item.querySelector('img').getAttribute('alt') || '';
      image.setAttribute('src', src);
      image.setAttribute('alt', cap);
      caption.textContent = cap;
    };

    const open = (i) => {
      show(i);
      lightbox.classList.add('is-open');
      document.body.style.overflow = 'hidden';
    };

    const close = () => {
      lightbox.classList.remove('is-open');
      document.body.style.overflow = '';
    };

    items.forEach((item, i) => {
      item.addEventListener('click', () => open(i));
    });

    closeBtn.addEventListener('click', close);
    prevBtn.addEventListener('click', (e) => { e.stopPropagation(); show(index - 1); });
    nextBtn.addEventListener('click', (e) => { e.stopPropagation(); show(index + 1); });

    lightbox.addEventListener('click', (e) => {
      if (e.target === lightbox) close();
    });

    document.addEventListener('keydown', (e) => {
      if (!lightbox.classList.contains('is-open')) return;
      if (e.key === 'Escape') close();
      if (e.key === 'ArrowRight') show(index + 1);
      if (e.key === 'ArrowLeft') show(index - 1);
    });
  }

  // ---------- testimonial carousel ----------

  function initTestimonials() {
    const stage = document.getElementById('testimonial-stage');
    const dotsBar = document.getElementById('testimonial-dots');
    if (!stage || !dotsBar) return;

    const slides = Array.from(stage.querySelectorAll('.testimonial'));
    const dots = Array.from(dotsBar.querySelectorAll('.dot'));
    if (!slides.length) return;

    let current = 0;
    let timer = null;

    const goTo = (i) => {
      current = (i + slides.length) % slides.length;
      slides.forEach((s, idx) => s.classList.toggle('is-active', idx === current));
      dots.forEach((d, idx) => d.classList.toggle('is-active', idx === current));
    };

    const start = () => {
      stop();
      timer = window.setInterval(() => goTo(current + 1), 6000);
    };

    const stop = () => {
      if (timer) window.clearInterval(timer);
      timer = null;
    };

    dots.forEach((dot, i) => {
      dot.addEventListener('click', () => {
        goTo(i);
        start();
      });
    });

    stage.addEventListener('mouseenter', stop);
    stage.addEventListener('mouseleave', start);

    start();
  }

  // ---------- reservation form ----------

  function initReservationForm() {
    const form = document.getElementById('reservation-form');
    const fields = document.getElementById('reservation-form-fields');
    const thankYou = document.getElementById('thank-you');
    const errorBox = document.getElementById('form-error');
    const submitBtn = document.getElementById('form-submit');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      errorBox.hidden = true;
      errorBox.textContent = '';
      form.querySelectorAll('.is-invalid').forEach((el) => el.classList.remove('is-invalid'));

      submitBtn.disabled = true;
      submitBtn.classList.add('is-loading');

      try {
        const response = await fetch(form.action, {
          method: 'POST',
          body: new FormData(form),
          headers: { 'X-Requested-With': 'XMLHttpRequest' },
        });

        const data = await response.json().catch(() => null);

        if (response.ok && data && data.ok) {
          const message = document.getElementById('thank-you-message');
          fields.hidden = true;
          thankYou.hidden = false;
          thankYou.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else if (data && data.errors) {
          showErrors(data.errors);
        } else {
          errorBox.textContent = 'Something went wrong sending your request. Please call or text us instead.';
          errorBox.hidden = false;
        }
      } catch (err) {
        errorBox.textContent = 'Something went wrong sending your request. Please call or text us instead.';
        errorBox.hidden = false;
      } finally {
        submitBtn.disabled = false;
        submitBtn.classList.remove('is-loading');
      }
    });

    function showErrors(errors) {
      const messages = [];
      Object.keys(errors).forEach((key) => {
        const fieldName = key.split('.').pop();
        const input = form.querySelector(`[name$="${fieldName}"]`);
        if (input) input.classList.add('is-invalid');
        messages.push(...errors[key]);
      });
      errorBox.textContent = messages.join(' ');
      errorBox.hidden = messages.length === 0;
    }
  }
})();
