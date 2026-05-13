(function () {
  var timers = document.querySelectorAll("[data-support-timer]");
  var hourLabels = document.querySelectorAll("[data-support-hours]");
  var hourBars = document.querySelectorAll("[data-support-hour-bar]");
  var sourceTimer = timers[0];
  var statusUrl = sourceTimer ? sourceTimer.dataset.statusUrl : "";
  var baseSeconds = sourceTimer ? Number(sourceTimer.dataset.initialSeconds || 0) : 0;
  var syncedAt = Date.now();
  var isLimitReached = baseSeconds >= 43200;

  if (!timers.length && !hourLabels.length && !hourBars.length) {
    return;
  }

  function pad(value) {
    return value.toString().padStart(2, "0");
  }

  function updateShiftStats() {
    var elapsedSeconds = isLimitReached ? 0 : Math.floor(Math.max(0, Date.now() - syncedAt) / 1000);
    var totalSeconds = Math.min(43200, baseSeconds + elapsedSeconds);
    var hours = Math.floor(totalSeconds / 3600);
    var minutes = Math.floor(totalSeconds % 3600 / 60);
    var seconds = totalSeconds % 60;
    var decimalHours = totalSeconds / 3600;

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

  async function syncShiftStats() {
    if (!statusUrl) {
      return;
    }

    try {
      var response = await fetch(statusUrl, {
        headers: {
          "Accept": "application/json"
        }
      });

      if (!response.ok) {
        return;
      }

      var data = await response.json();
      baseSeconds = Number(data.seconds || 0);
      isLimitReached = Boolean(data.isLimitReached);
      syncedAt = Date.now();
      updateShiftStats();
    } catch {
    }
  }

  updateShiftStats();
  syncShiftStats();
  setInterval(updateShiftStats, 1000);
  setInterval(syncShiftStats, 15000);
})();

(function () {
  var queue = document.querySelector("[data-support-report-queue]");

  if (!queue || !queue.dataset.supportReportQueueUrl) {
    return;
  }

  var tokenSource = document.querySelector("[data-support-token-source] input[name='__RequestVerificationToken']");
  var lastSignature = "";

  function createCell(tagName, text) {
    var element = document.createElement(tagName);
    element.textContent = text || "";
    return element;
  }

  function createReportRow(report, hasActiveReport) {
    var row = document.createElement("article");
    row.className = "support-table-row";

    row.appendChild(createCell("span", report.code));
    row.appendChild(createCell("strong", report.customer));
    row.appendChild(createCell("p", report.category));

    var priority = createCell("em", report.priority);
    priority.className = "priority-pill " + (report.priority || "").toLowerCase();
    row.appendChild(priority);

    var status = createCell("em", report.status);
    status.className = "status-pill";
    row.appendChild(status);

    var form = document.createElement("form");
    form.method = "post";
    form.action = queue.dataset.supportTakeUrl + "/" + report.id;

    if (tokenSource) {
      var hidden = document.createElement("input");
      hidden.type = "hidden";
      hidden.name = tokenSource.name;
      hidden.value = tokenSource.value;
      form.appendChild(hidden);
    }

    var button = document.createElement("button");
    button.className = "primary-button compact-button";
    button.type = "submit";
    button.textContent = "Take";
    button.disabled = hasActiveReport;
    form.appendChild(button);
    row.appendChild(form);

    return row;
  }

  function createEmptyRow() {
    var row = document.createElement("article");
    row.className = "support-table-row";
    row.dataset.supportEmptyRow = "";
    row.appendChild(createCell("span", "No reports"));
    row.appendChild(createCell("strong", "Queue is empty"));
    row.appendChild(createCell("p", "New reports will appear here."));

    var priority = createCell("em", "Low");
    priority.className = "priority-pill low";
    row.appendChild(priority);

    var status = createCell("em", "Open");
    status.className = "status-pill";
    row.appendChild(status);
    row.appendChild(createCell("time", new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })));

    return row;
  }

  async function refreshQueue() {
    try {
      var response = await fetch(queue.dataset.supportReportQueueUrl, {
        headers: {
          "Accept": "application/json"
        }
      });

      if (!response.ok) {
        return;
      }

      var data = await response.json();
      var signature = JSON.stringify(data);

      if (signature === lastSignature) {
        return;
      }

      var hasActiveReport = Boolean(data.activeReportId);
      queue.replaceChildren();

      if (!data.reports || !data.reports.length) {
        queue.appendChild(createEmptyRow());
      } else {
        data.reports.forEach(function (report) {
          queue.appendChild(createReportRow(report, hasActiveReport));
        });
      }

      lastSignature = signature;

      var sidebarCount = document.querySelector(".sidebar-card strong");
      if (sidebarCount) {
        sidebarCount.textContent = data.reports ? data.reports.length : 0;
      }
    } catch {
    }
  }

  refreshQueue();
  setInterval(refreshQueue, 2000);
})();
