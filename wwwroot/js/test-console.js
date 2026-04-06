const appRoot = document.getElementById("testConsoleApp");
const sessionHeaderName = appRoot.dataset.sessionHeader;
const pointsStreamUrl = appRoot.dataset.pointsStreamUrl;
const sessionStorageKey = "test-console-session-id";
const sessionInput = document.getElementById("sessionIdInput");
const sessionStatus = document.getElementById("sessionStatus");
const streamStatus = document.getElementById("streamStatus");
const pointsStatus = document.getElementById("pointsStatus");
const responseOutput = document.getElementById("responseOutput");
const clientLogOutput = document.getElementById("clientLogOutput");
const lastRequest = document.getElementById("lastRequest");
const lastStatus = document.getElementById("lastStatus");
let pointsStream = null;

if (typeof window.__setTestConsoleBootStatus === "function") {
    window.__setTestConsoleBootStatus("client script loaded.");
}

function appendLog(message, details = null) {
    const timestamp = new Date().toLocaleTimeString();
    const entry = "[" + timestamp + "] " + message;
    const extra =
        details === null
            ? ""
            : "\n" + (typeof details === "string"
                ? details
                : JSON.stringify(details, null, 2));

    if (clientLogOutput) {
        if (clientLogOutput.textContent === "Ready.") {
            clientLogOutput.textContent = entry + extra;
        } else {
            clientLogOutput.textContent = entry + extra + "\n\n" + clientLogOutput.textContent;
        }
    }

    console.log(message, details ?? "");
}

function getSessionId() {
    return sessionInput.value.trim();
}

function setStreamStatus(text, isActive) {
    streamStatus.textContent = text;
    streamStatus.className = isActive ? "status" : "status empty";
}

function setPointsStatus(points) {
    if (typeof points === "number" && Number.isFinite(points)) {
        pointsStatus.textContent = "Points: " + points;
        pointsStatus.className = "status";
        return;
    }

    pointsStatus.textContent = "Points: -";
    pointsStatus.className = "status empty";
}

function closePointsStream() {
    if (pointsStream) {
        pointsStream.close();
        pointsStream = null;
    }
}

function connectPointsStream() {
    closePointsStream();

    const sessionId = getSessionId();
    if (!sessionId) {
        setStreamStatus("Live updates offline", false);
        setPointsStatus(null);
        appendLog("Skipped live updates connection because there is no active session.");
        return;
    }

    setStreamStatus("Connecting to live updates", false);
    appendLog("Connecting to live updates stream.", {
        url: pointsStreamUrl
    });

    const streamUrl = pointsStreamUrl + "?sessionId=" + encodeURIComponent(sessionId);
    const eventSource = new EventSource(streamUrl);
    pointsStream = eventSource;

    eventSource.onopen = () => {
        if (pointsStream !== eventSource) {
            return;
        }

        setStreamStatus("Live updates connected", true);
        appendLog("Live updates stream connected.");
    };

    eventSource.addEventListener("points-updated", (event) => {
        if (pointsStream !== eventSource) {
            return;
        }

        const payload = JSON.parse(event.data);
        if (typeof payload.totalPoints === "number") {
            setPointsStatus(payload.totalPoints);
        }

        appendLog("Received points update.", payload);
    });

    eventSource.onerror = () => {
        if (pointsStream !== eventSource) {
            return;
        }

        setStreamStatus("Live updates reconnecting", false);
        appendLog("Live updates stream reported an error and will retry.");
    };
}

function setSessionId(sessionId) {
    const value = (sessionId || "").trim();
    sessionInput.value = value;

    if (value) {
        localStorage.setItem(sessionStorageKey, value);
        sessionStatus.textContent = "Active session loaded";
        sessionStatus.className = "status";
        appendLog("Session saved.", {
            sessionId: value
        });
        connectPointsStream();
        return;
    }

    localStorage.removeItem(sessionStorageKey);
    sessionStatus.textContent = "No active session";
    sessionStatus.className = "status empty";
    appendLog("Session cleared.");
    closePointsStream();
    setStreamStatus("Live updates offline", false);
    setPointsStatus(null);
}

function showResponse(method, url, status, payload) {
    lastRequest.textContent = "Request: " + method + " " + url;
    lastStatus.textContent = "Status: " + status;

    if (payload && typeof payload === "object") {
        if (typeof payload.totalPoints === "number") {
            setPointsStatus(payload.totalPoints);
        }

        if (typeof payload.newTotalPoints === "number") {
            setPointsStatus(payload.newTotalPoints);
        }
    }

    responseOutput.textContent =
        typeof payload === "string"
            ? payload
            : JSON.stringify(payload, null, 2);
}

async function parseResponse(response) {
    const text = await response.text();
    if (!text) {
        return null;
    }

    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

async function callApi(url, options = {}) {
    const {
        method = "GET",
        body = null,
        requiresSession = false,
        autoClearSession = false
    } = options;

    const headers = {};
    if (body !== null) {
        headers["Content-Type"] = "application/json";
    }

    if (requiresSession) {
        const sessionId = getSessionId();
        if (!sessionId) {
            appendLog("Blocked request because there is no active session.", {
                method,
                url
            });
            showResponse(method, url, "blocked", {
                error: "A session id is required for this request."
            });
            return;
        }

        headers[sessionHeaderName] = sessionId;
    }

    appendLog("Sending request.", {
        method,
        url,
        requiresSession,
        body
    });

    try {
        const response = await fetch(url, {
            method,
            headers,
            body: body !== null ? JSON.stringify(body) : undefined
        });

        const payload = await parseResponse(response);
        if (payload && typeof payload === "object" && payload.sessionId) {
            setSessionId(payload.sessionId);
        }

        if (autoClearSession && response.ok) {
            setSessionId("");
        }

        appendLog("Received response.", {
            method,
            url,
            status: response.status
        });

        showResponse(method, url, response.status, payload ?? {});
    } catch (error) {
        appendLog("Request failed before a response was received.", {
            method,
            url,
            error: error instanceof Error ? error.message : String(error)
        });

        showResponse(method, url, "error", {
            success: false,
            error: error instanceof Error ? error.message : String(error)
        });
    }
}

function formValue(form, name) {
    return new FormData(form).get(name)?.toString().trim() || "";
}

document.getElementById("saveSessionButton").addEventListener("click", () => {
    setSessionId(getSessionId());
    showResponse("LOCAL", "session", "saved", {
        sessionId: getSessionId()
    });
});

document.getElementById("clearSessionButton").addEventListener("click", () => {
    setSessionId("");
    showResponse("LOCAL", "session", "cleared", {
        sessionId: null
    });
});

document.getElementById("copySessionButton").addEventListener("click", async () => {
    const sessionId = getSessionId();
    if (!sessionId) {
        showResponse("LOCAL", "session", "empty", {
            error: "There is no session id to copy."
        });
        return;
    }

    if (!navigator.clipboard) {
        showResponse("LOCAL", "session", "unsupported", {
            error: "Clipboard access is not available in this browser."
        });
        return;
    }

    await navigator.clipboard.writeText(sessionId);
    showResponse("LOCAL", "session", "copied", {
        sessionId
    });
});

document.getElementById("registerForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/auth/register", {
        method: "POST",
        body: {
            name: formValue(form, "name"),
            phoneNumber: formValue(form, "phoneNumber"),
            password: formValue(form, "password"),
            mallID: formValue(form, "mallID")
        }
    });
});

document.getElementById("loginForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/auth/login", {
        method: "POST",
        body: {
            phoneNumber: formValue(form, "phoneNumber"),
            password: formValue(form, "password"),
            mallID: formValue(form, "mallID")
        }
    });
});

document.getElementById("logoutButton").addEventListener("click", async () => {
    await callApi("/api/auth/logout", {
        method: "POST",
        body: {
            sessionId: getSessionId()
        },
        autoClearSession: true
    });
});

document.getElementById("pointsButton").addEventListener("click", async () => {
    await callApi("/api/userinfo/points", {
        requiresSession: true
    });
});

document.getElementById("myCouponsButton").addEventListener("click", async () => {
    await callApi("/api/coupons/user", {
        requiresSession: true
    });
});

document.getElementById("offersButton").addEventListener("click", async () => {
    appendLog("Offers button clicked.");
    await callApi("/api/offers", {
        requiresSession: true
    });
});

document.getElementById("storesButton").addEventListener("click", async () => {
    appendLog("Stores button clicked.");
    await callApi("/api/stores", {
        requiresSession: true
    });
});

document.getElementById("storeByIdForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/stores/" + encodeURIComponent(formValue(form, "storeId")), {
        requiresSession: true
    });
});

document.getElementById("couponsForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const isActive = formValue(form, "isActive");
    const query = isActive === "all" ? "" : "?isActive=" + encodeURIComponent(isActive);

    await callApi("/api/coupons" + query, {
        requiresSession: true
    });
});

document.getElementById("couponByIdForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/coupons/" + encodeURIComponent(formValue(form, "couponId")), {
        requiresSession: true
    });
});

document.getElementById("redeemCouponForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/coupons/redeem", {
        method: "POST",
        requiresSession: true,
        body: {
            couponId: formValue(form, "couponId")
        }
    });
});

document.getElementById("redeemSerialForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/coupons/redeem-by-serial", {
        method: "POST",
        body: {
            serialNumber: formValue(form, "serialNumber")
        }
    });
});

document.getElementById("transactionForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const description = formValue(form, "receiptDescription");

    await callApi("/api/transactions", {
        method: "POST",
        body: {
            phoneNumber: formValue(form, "phoneNumber"),
            storeId: formValue(form, "storeId"),
            mallID: formValue(form, "mallID"),
            receiptId: formValue(form, "receiptId"),
            receiptDescription: description || null,
            price: Number(formValue(form, "price"))
        }
    });
});

document.getElementById("transactionByIdForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;

    await callApi("/api/transactions/" + encodeURIComponent(formValue(form, "transactionId")));
});

setSessionId(localStorage.getItem(sessionStorageKey) || "");

if (typeof window.__setTestConsoleBootStatus === "function") {
    window.__setTestConsoleBootStatus("client script initialized.");
}
