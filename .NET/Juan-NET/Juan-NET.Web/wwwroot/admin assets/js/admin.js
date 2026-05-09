(function () {
  var body = document.body;
  var transitionLinks = document.querySelectorAll("[data-transition]");
  var menuToggle = document.querySelector("[data-menu-toggle]");
  var revealItems = document.querySelectorAll(".reveal");
  var countItems = document.querySelectorAll("[data-count]");
  var pulseItems = document.querySelectorAll("[data-pulse], .primary-button, .ghost-button, .filter-button");
  var productModal = document.querySelector("[data-product-modal]");
  var openProductModal = document.querySelector("[data-open-product-modal]");
  var closeProductModal = document.querySelectorAll("[data-close-product-modal]");
  var sliderModal = document.querySelector("[data-slider-modal]");
  var openSliderModal = document.querySelector("[data-open-slider-modal]");
  var closeSliderModal = document.querySelectorAll("[data-close-slider-modal]");
  var imageInput = document.querySelector("[data-image-input]");
  var imageName = document.querySelector("[data-image-name]");
  var sliderImageInput = document.querySelector("[data-slider-image-input]");
  var sliderImageName = document.querySelector("[data-slider-image-name]");
  var categoryChoices = document.querySelectorAll("[data-category-choice]");
  var confirmModal = document.querySelector("[data-confirm-modal]");
  var confirmMessage = document.querySelector("[data-confirm-message]");
  var confirmAccept = document.querySelector("[data-confirm-accept]");
  var confirmCancel = document.querySelectorAll("[data-confirm-cancel]");
  var pendingConfirm = null;

  window.addEventListener("load", function () {
    body.classList.add("page-ready");
    revealItems.forEach(function (item, index) {
      setTimeout(function () {
        item.classList.add("is-visible");
      }, 80 + index * 80);
    });
    countItems.forEach(function (item) {
      animateCount(item);
    });
  });

  transitionLinks.forEach(function (link) {
    link.addEventListener("click", function (event) {
      var target = link.getAttribute("href");
      if (!target || target === "#" || link.target === "_blank" || event.metaKey || event.ctrlKey) {
        return;
      }
      event.preventDefault();
      body.classList.remove("menu-open");
      body.classList.add("page-leave");
      setTimeout(function () {
        window.location.href = target;
      }, 260);
    });
  });

  if (menuToggle) {
    menuToggle.addEventListener("click", function () {
      body.classList.toggle("menu-open");
    });
  }

  if (productModal && productModal.classList.contains("is-open")) {
    body.classList.add("modal-open");
  }

  if (sliderModal && sliderModal.classList.contains("is-open")) {
    body.classList.add("modal-open");
  }

  if (openProductModal && productModal) {
    openProductModal.addEventListener("click", function () {
      productModal.classList.add("is-open");
      body.classList.add("modal-open");
    });
  }

  closeProductModal.forEach(function (item) {
    item.addEventListener("click", function () {
      productModal.classList.remove("is-open");
      body.classList.remove("modal-open");
    });
  });

  if (openSliderModal && sliderModal) {
    openSliderModal.addEventListener("click", function () {
      sliderModal.classList.add("is-open");
      body.classList.add("modal-open");
    });
  }

  closeSliderModal.forEach(function (item) {
    item.addEventListener("click", function () {
      sliderModal.classList.remove("is-open");
      body.classList.remove("modal-open");
    });
  });

  if (imageInput && imageName) {
    imageInput.addEventListener("change", function () {
      imageName.textContent = imageInput.files.length ? imageInput.files[0].name : "Choose product image";
    });
  }

  if (sliderImageInput && sliderImageName) {
    sliderImageInput.addEventListener("change", function () {
      sliderImageName.textContent = sliderImageInput.files.length ? sliderImageInput.files[0].name : "Choose slider image";
    });
  }

  categoryChoices.forEach(function (choice) {
    choice.addEventListener("change", syncCategoryChoices);
  });
  syncCategoryChoices();

  document.querySelectorAll("form[data-confirm]").forEach(function (form) {
    form.addEventListener("submit", function (event) {
      if (form.dataset.confirmed === "true") {
        delete form.dataset.confirmed;
        return;
      }

      var submitter = event.submitter;
      var message = submitter && submitter.getAttribute("data-confirm")
        ? submitter.getAttribute("data-confirm")
        : form.getAttribute("data-confirm");

      event.preventDefault();
      openConfirm(form, submitter, message);
    });
  });

  if (confirmAccept) {
    confirmAccept.addEventListener("click", function () {
      if (!pendingConfirm) {
        closeConfirm();
        return;
      }

      var confirmed = pendingConfirm;
      confirmed.form.dataset.confirmed = "true";
      closeConfirm();
      if (confirmed.submitter) {
        confirmed.form.requestSubmit(confirmed.submitter);
      } else {
        confirmed.form.requestSubmit();
      }
    });
  }

  confirmCancel.forEach(function (item) {
    item.addEventListener("click", closeConfirm);
  });

  document.addEventListener("click", function (event) {
    if (body.classList.contains("menu-open") && !event.target.closest(".sidebar") && !event.target.closest("[data-menu-toggle]")) {
      body.classList.remove("menu-open");
    }
  });

  pulseItems.forEach(function (item) {
    item.addEventListener("click", function (event) {
      var rect = item.getBoundingClientRect();
      var size = Math.max(rect.width, rect.height);
      var ring = document.createElement("span");
      ring.className = "pulse-ring";
      ring.style.width = size + "px";
      ring.style.height = size + "px";
      ring.style.left = event.clientX - rect.left + "px";
      ring.style.top = event.clientY - rect.top + "px";
      item.appendChild(ring);
      setTimeout(function () {
        ring.remove();
      }, 600);
    });
  });

  function animateCount(item) {
    var rawValue = item.getAttribute("data-count");
    var target = parseInt(rawValue, 10);
    var prefix = item.textContent.trim().charAt(0) === "$" ? "$" : "";
    var start = 0;
    var duration = 900;
    var startTime = null;

    function tick(timestamp) {
      if (!startTime) {
        startTime = timestamp;
      }
      var progress = Math.min((timestamp - startTime) / duration, 1);
      var value = Math.floor(start + (target - start) * easeOut(progress));
      item.textContent = prefix + value.toLocaleString("en-US");
      if (progress < 1) {
        requestAnimationFrame(tick);
      }
    }

    requestAnimationFrame(tick);
  }

  function easeOut(progress) {
    return 1 - Math.pow(1 - progress, 3);
  }

  function syncCategoryChoices() {
    if (!categoryChoices.length) {
      return;
    }

    var selectedCount = Array.prototype.filter.call(categoryChoices, function (choice) {
      return choice.checked;
    }).length;

    categoryChoices.forEach(function (choice) {
      choice.disabled = !choice.checked && selectedCount >= 3;
    });
  }

  function openConfirm(form, submitter, message) {
    if (!confirmModal) {
      form.dataset.confirmed = "true";
      if (submitter) {
        form.requestSubmit(submitter);
      } else {
        form.requestSubmit();
      }
      return;
    }

    pendingConfirm = { form: form, submitter: submitter };
    confirmMessage.textContent = message || "Are you sure you want to continue?";
    confirmModal.classList.add("is-open");
    body.classList.add("modal-open");
  }

  function closeConfirm() {
    pendingConfirm = null;

    if (confirmModal) {
      confirmModal.classList.remove("is-open");
    }

    if (!document.querySelector(".admin-modal.is-open")) {
      body.classList.remove("modal-open");
    }
  }
})();
