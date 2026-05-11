(function () {
  var timers = document.querySelectorAll("[data-support-timer]");
  var hourLabels = document.querySelectorAll("[data-support-hours]");
  var hourBars = document.querySelectorAll("[data-support-hour-bar]");
  var startedAt = Date.now();

  if (!timers.length && !hourLabels.length && !hourBars.length) {
    return;
  }

  function pad(value) {
    return value.toString().padStart(2, "0");
  }

  function updateShiftStats() {
    var elapsed = Math.max(0, Date.now() - startedAt);
    var totalSeconds = Math.floor(elapsed / 1000);
    var hours = Math.floor(totalSeconds / 3600);
    var minutes = Math.floor(totalSeconds % 3600 / 60);
    var seconds = totalSeconds % 60;
    var decimalHours = elapsed / 3600000;

    timers.forEach(function (timer) {
      timer.textContent = pad(hours) + ":" + pad(minutes) + ":" + pad(seconds);
    });

    hourLabels.forEach(function (label) {
      var target = Number(label.dataset.target || 0);
      label.textContent = decimalHours.toFixed(2) + " / " + target;
    });

    hourBars.forEach(function (bar) {
      var target = Number(bar.dataset.target || 1);
      var progress = Math.min(100, decimalHours * 100 / target);
      bar.style.setProperty("--progress", progress + "%");
    });
  }

  updateShiftStats();
  setInterval(updateShiftStats, 1000);
})();
