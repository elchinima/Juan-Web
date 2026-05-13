(function () {
  var messagesBox = document.querySelector("[data-support-messages]");

  if (!messagesBox || !messagesBox.dataset.supportChatPoll) {
    return;
  }

  var form = document.querySelector(".support-chat-form");
  var operatorName = document.querySelector("[data-support-operator-name]");
  var operatorRole = document.querySelector("[data-support-operator-role]");
  var chatState = document.querySelector("[data-support-chat-state]");
  var lastSignature = "";
  var isSubmitting = false;

  function isNearBottom(element) {
    return element.scrollHeight - element.scrollTop - element.clientHeight < 90;
  }

  function createMessage(message) {
    var article = document.createElement("article");
    article.className = "support-chat-message " + (message.isOperator ? "agent" : "user");

    var strong = document.createElement("strong");
    strong.textContent = message.senderName || "";
    article.appendChild(strong);

    if (message.text) {
      var text = document.createElement("p");
      text.textContent = message.text;
      article.appendChild(text);
    }

    if (message.imageUrl) {
      var link = document.createElement("a");
      link.className = "support-chat-attachment";
      link.href = message.imageUrl;
      link.target = "_blank";
      link.rel = "noopener";

      var image = document.createElement("img");
      image.src = message.imageUrl;
      image.alt = "Support attachment";
      link.appendChild(image);
      article.appendChild(link);
    }

    return article;
  }

  function renderMessages(messages) {
    var signature = JSON.stringify(messages || []);

    if (signature === lastSignature) {
      return;
    }

    var shouldStickToBottom = isNearBottom(messagesBox);
    messagesBox.replaceChildren();

    (messages || []).forEach(function (message) {
      messagesBox.appendChild(createMessage(message));
    });

    lastSignature = signature;

    if (shouldStickToBottom) {
      messagesBox.scrollTop = messagesBox.scrollHeight;
    }
  }

  function updateUserHeader(data) {
    if (!operatorName || !chatState) {
      return;
    }

    if (data.isWaitingForOperator) {
      operatorName.textContent = data.operatorRole || "Operator is joining";
      chatState.className = "support-waiting-label";
      chatState.innerHTML = "Waiting<i aria-hidden=\"true\"></i>";

      if (operatorRole) {
        operatorRole.hidden = true;
      }

      return;
    }

    operatorName.textContent = data.operatorFullName || "Juan Support";
    chatState.className = "";
    chatState.textContent = "Online";

    if (operatorRole) {
      operatorRole.hidden = false;
      operatorRole.textContent = data.operatorRole || "Support Operator";
    }
  }

  function buildPollUrl() {
    var url = new URL(messagesBox.dataset.supportChatPoll, window.location.origin);
    var ticketId = messagesBox.dataset.supportCurrentTicketId;

    if (ticketId) {
      url.searchParams.set("ticketId", ticketId);
    }

    return url.toString();
  }

  async function refreshMessages() {
    try {
      var response = await fetch(buildPollUrl(), {
        headers: {
          "Accept": "application/json"
        }
      });

      if (!response.ok) {
        return;
      }

      var data = await response.json();

      if (data.isClosed === true && messagesBox.dataset.supportInactiveUrl) {
        window.location.href = messagesBox.dataset.supportInactiveUrl;
        return;
      }

      if (data.isActive === false && messagesBox.dataset.supportInactiveUrl) {
        window.location.href = messagesBox.dataset.supportInactiveUrl;
        return;
      }

      if (data.ticketId) {
        messagesBox.dataset.supportCurrentTicketId = data.ticketId;

        var ticketInput = document.querySelector("input[name='TicketId']");
        if (ticketInput) {
          ticketInput.value = data.ticketId;
        }
      }

      updateUserHeader(data);
      renderMessages(data.messages || []);
    } catch {
    }
  }

  if (form) {
    form.addEventListener("submit", async function (event) {
      if (!window.fetch || isSubmitting) {
        return;
      }

      event.preventDefault();
      isSubmitting = true;

      try {
        var response = await fetch(form.action, {
          method: form.method || "POST",
          body: new FormData(form),
          credentials: "same-origin"
        });

        if (response.ok) {
          var textInput = form.querySelector("input[name='Text']");
          var fileInput = form.querySelector("input[name='ImageFile']");

          if (textInput) {
            textInput.value = "";
          }

          if (fileInput) {
            fileInput.value = "";
          }

          await refreshMessages();
        }
      } finally {
        isSubmitting = false;
      }
    });
  }

  refreshMessages();
  setInterval(refreshMessages, 2000);
})();
