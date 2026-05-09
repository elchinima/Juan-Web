(function () {
  var body = document.body;
  var transitionLinks = document.querySelectorAll("[data-transition]");
  var menuToggle = document.querySelector("[data-menu-toggle]");
  var revealItems = document.querySelectorAll(".reveal");
  var countItems = document.querySelectorAll("[data-count]");
  var pulseItems = document.querySelectorAll("[data-pulse], .primary-button, .ghost-button, .filter-button");

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
})();
