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

function optionalValue(form, name) {
    const value = formValue(form, name);
    return value || null;
}

function booleanValue(form, name) {
    return formValue(form, name).toLowerCase() === "true";
}

function jsonValue(form, name) {
    const value = formValue(form, name);
    if (!value) {
        return null;
    }

    return JSON.parse(value);
}

function csvNumberArray(form, name) {
    const value = formValue(form, name);
    if (!value) {
        return [];
    }

    return value
        .split(",")
        .map((item) => item.trim())
        .filter(Boolean)
        .map((item) => Number(item))
        .filter((item) => Number.isFinite(item) && item > 0);
}

function dateTimeValue(form, name) {
    const value = formValue(form, name);
    return value ? new Date(value).toISOString() : null;
}

function buildQuery(params) {
    const searchParams = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
        if (value === null || value === undefined || value === "") {
            return;
        }

        searchParams.set(key, String(value));
    });

    const query = searchParams.toString();
    return query ? "?" + query : "";
}

function handleLocalError(action, error) {
    appendLog(action + " failed before the request was sent.", {
        error: error instanceof Error ? error.message : String(error)
    });

    showResponse("LOCAL", action, "error", {
        success: false,
        error: error instanceof Error ? error.message : String(error)
    });
}

function buildStoreBody(form) {
    return {
        name: formValue(form, "name"),
        operatingHours: optionalValue(form, "operatingHours"),
        socialMediaLinks: jsonValue(form, "socialMediaLinks"),
        description: optionalValue(form, "description"),
        phoneNumber: optionalValue(form, "phoneNumber"),
        email: optionalValue(form, "email"),
        floorNumber: optionalValue(form, "floorNumber"),
        storeImageUrl: optionalValue(form, "storeImageUrl"),
        categoryIds: csvNumberArray(form, "categoryIds")
    };
}

function buildOfferBody(form) {
    return {
        storeId: formValue(form, "storeId"),
        title: formValue(form, "title"),
        description: optionalValue(form, "description"),
        startAt: dateTimeValue(form, "startAt"),
        endAt: dateTimeValue(form, "endAt"),
        isActive: booleanValue(form, "isActive")
    };
}

function buildAnnouncementBody(form) {
    return {
        storeId: optionalValue(form, "storeId"),
        title: formValue(form, "title"),
        content: formValue(form, "content"),
        announcementType: optionalValue(form, "announcementType"),
        priority: optionalValue(form, "priority"),
        isActive: booleanValue(form, "isActive"),
        isPinned: booleanValue(form, "isPinned"),
        imageUrl: optionalValue(form, "imageUrl"),
        startDate: dateTimeValue(form, "startDate"),
        endDate: dateTimeValue(form, "endDate")
    };
}

function dashboardQueryFromForm(form) {
    return buildQuery({
        from: dateTimeValue(form, "from"),
        to: dateTimeValue(form, "to")
    });
}

function bindClick(id, handler) {
    const element = document.getElementById(id);
    if (!element) {
        return;
    }

    element.addEventListener("click", handler);
}

function bindSubmit(id, handler) {
    const form = document.getElementById(id);
    if (!form) {
        return;
    }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        try {
            await handler(event.currentTarget);
        } catch (error) {
            handleLocalError(id, error);
        }
    });
}

bindClick("saveSessionButton", () => {
    setSessionId(getSessionId());
    showResponse("LOCAL", "session", "saved", {
        sessionId: getSessionId()
    });
});

bindClick("clearSessionButton", () => {
    setSessionId("");
    showResponse("LOCAL", "session", "cleared", {
        sessionId: null
    });
});

bindClick("copySessionButton", async () => {
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

bindSubmit("registerForm", async (form) => {
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

bindSubmit("managerQuickLoginForm", async (form) => {
    await callApi("/api/auth/manager-quick-login", {
        method: "POST",
        body: {
            managerId: formValue(form, "managerId")
        }
    });
});

bindSubmit("loginForm", async (form) => {
    await callApi("/api/auth/login", {
        method: "POST",
        body: {
            phoneNumber: formValue(form, "phoneNumber"),
            password: formValue(form, "password"),
            mallID: formValue(form, "mallID")
        }
    });
});

bindClick("logoutButton", async () => {
    await callApi("/api/auth/logout", {
        method: "POST",
        body: {
            sessionId: getSessionId()
        },
        autoClearSession: true
    });
});

bindClick("pointsButton", async () => {
    await callApi("/api/userinfo/points", {
        requiresSession: true
    });
});

bindClick("myCouponsButton", async () => {
    await callApi("/api/coupons/user", {
        requiresSession: true
    });
});

bindClick("offersButton", async () => {
    await callApi("/api/offers", {
        requiresSession: true
    });
});

bindClick("announcementsButton", async () => {
    await callApi("/api/announcements", {
        requiresSession: true
    });
});

bindClick("storesButton", async () => {
    await callApi("/api/stores", {
        requiresSession: true
    });
});

bindClick("myReceiptsButton", async () => {
    await callApi("/api/transactions/my-receipts", {
        requiresSession: true
    });
});

bindClick("chatbotHistoryButton", async () => {
    await callApi("/api/chatbot/history", {
        requiresSession: false
    });
});

bindClick("managedStoresButton", async () => {
    await callApi("/api/stores/manage", {
        requiresSession: true
    });
});

bindClick("managedOffersButton", async () => {
    await callApi("/api/offers/manage", {
        requiresSession: true
    });
});

bindClick("managedAnnouncementsButton", async () => {
    await callApi("/api/announcements/manage", {
        requiresSession: true
    });
});

bindSubmit("storeByIdForm", async (form) => {
    await callApi("/api/stores/" + encodeURIComponent(formValue(form, "storeId")), {
        requiresSession: true
    });
});

bindSubmit("couponsForm", async (form) => {
    const isActive = formValue(form, "isActive");
    const query = isActive === "all" ? "" : "?isActive=" + encodeURIComponent(isActive);

    await callApi("/api/coupons" + query, {
        requiresSession: true
    });
});

bindSubmit("couponByIdForm", async (form) => {
    await callApi("/api/coupons/" + encodeURIComponent(formValue(form, "couponId")), {
        requiresSession: true
    });
});

bindSubmit("redeemCouponForm", async (form) => {
    await callApi("/api/coupons/redeem", {
        method: "POST",
        requiresSession: true,
        body: {
            couponId: formValue(form, "couponId")
        }
    });
});

bindSubmit("receiptByIdForm", async (form) => {
    await callApi("/api/transactions/" + encodeURIComponent(formValue(form, "transactionId")), {
        requiresSession: true
    });
});

bindSubmit("receiptFiltersForm", async (form) => {
    const query = buildQuery({
        storeId: optionalValue(form, "storeId"),
        status: optionalValue(form, "status"),
        from: dateTimeValue(form, "from"),
        to: dateTimeValue(form, "to"),
        page: formValue(form, "page"),
        pageSize: formValue(form, "pageSize")
    });

    await callApi("/api/transactions/my-receipts" + query, {
        requiresSession: true
    });
});

bindSubmit("chatbotAskForm", async (form) => {
    const message = formValue(form, "msg") || formValue(form, "message");

    await callApi("/api/chatbot/ask", {
        method: "POST",
        requiresSession: false,
        body: {
            msg: message
        }
    });
});

bindSubmit("chatbotHistoryForm", async (form) => {
    await callApi("/api/chatbot/history", {
        requiresSession: false
    });
});

bindSubmit("createStoreForm", async (form) => {
    await callApi("/api/stores", {
        method: "POST",
        requiresSession: true,
        body: buildStoreBody(form)
    });
});

bindSubmit("updateStoreForm", async (form) => {
    await callApi("/api/stores/" + encodeURIComponent(formValue(form, "storeId")), {
        method: "PUT",
        requiresSession: true,
        body: buildStoreBody(form)
    });
});

bindSubmit("createOfferForm", async (form) => {
    await callApi("/api/offers", {
        method: "POST",
        requiresSession: true,
        body: buildOfferBody(form)
    });
});

bindSubmit("updateOfferForm", async (form) => {
    await callApi("/api/offers/" + encodeURIComponent(formValue(form, "offerId")), {
        method: "PUT",
        requiresSession: true,
        body: buildOfferBody(form)
    });
});

bindSubmit("offerStatusForm", async (form) => {
    await callApi("/api/offers/" + encodeURIComponent(formValue(form, "offerId")) + "/status", {
        method: "PATCH",
        requiresSession: true,
        body: {
            isActive: booleanValue(form, "isActive")
        }
    });
});

bindSubmit("deleteOfferForm", async (form) => {
    await callApi("/api/offers/" + encodeURIComponent(formValue(form, "offerId")), {
        method: "DELETE",
        requiresSession: true
    });
});

bindSubmit("createAnnouncementForm", async (form) => {
    await callApi("/api/announcements", {
        method: "POST",
        requiresSession: true,
        body: buildAnnouncementBody(form)
    });
});

bindSubmit("updateAnnouncementForm", async (form) => {
    await callApi("/api/announcements/" + encodeURIComponent(formValue(form, "announcementId")), {
        method: "PUT",
        requiresSession: true,
        body: buildAnnouncementBody(form)
    });
});

bindSubmit("announcementStatusForm", async (form) => {
    await callApi("/api/announcements/" + encodeURIComponent(formValue(form, "announcementId")) + "/status", {
        method: "PATCH",
        requiresSession: true,
        body: {
            isActive: booleanValue(form, "isActive")
        }
    });
});

bindSubmit("announcementPinForm", async (form) => {
    await callApi("/api/announcements/" + encodeURIComponent(formValue(form, "announcementId")) + "/pin", {
        method: "PATCH",
        requiresSession: true,
        body: {
            isPinned: booleanValue(form, "isPinned")
        }
    });
});

bindSubmit("deleteAnnouncementForm", async (form) => {
    await callApi("/api/announcements/" + encodeURIComponent(formValue(form, "announcementId")), {
        method: "DELETE",
        requiresSession: true
    });
});

bindClick("dashboardSummaryButton", async () => {
    const form = document.getElementById("dashboardFiltersForm");
    await callApi("/api/dashboard/summary" + dashboardQueryFromForm(form), {
        requiresSession: true
    });
});

bindClick("dashboardSalesButton", async () => {
    const form = document.getElementById("dashboardFiltersForm");
    await callApi("/api/dashboard/sales" + dashboardQueryFromForm(form), {
        requiresSession: true
    });
});

bindClick("dashboardPointsButton", async () => {
    const form = document.getElementById("dashboardFiltersForm");
    await callApi("/api/dashboard/points" + dashboardQueryFromForm(form), {
        requiresSession: true
    });
});

bindClick("dashboardCouponsButton", async () => {
    const form = document.getElementById("dashboardFiltersForm");
    await callApi("/api/dashboard/coupons" + dashboardQueryFromForm(form), {
        requiresSession: true
    });
});

bindClick("dashboardActivityButton", async () => {
    const form = document.getElementById("dashboardFiltersForm");
    await callApi("/api/dashboard/activity" + dashboardQueryFromForm(form), {
        requiresSession: true
    });
});

bindSubmit("redeemSerialForm", async (form) => {
    await callApi("/api/coupons/redeem-by-serial", {
        method: "POST",
        body: {
            serialNumber: formValue(form, "serialNumber")
        }
    });
});

bindSubmit("transactionForm", async (form) => {
    await callApi("/api/transactions", {
        method: "POST",
        body: {
            phoneNumber: formValue(form, "phoneNumber"),
            storeId: formValue(form, "storeId"),
            mallID: formValue(form, "mallID"),
            receiptId: formValue(form, "receiptId"),
            receiptDescription: optionalValue(form, "receiptDescription"),
            price: Number(formValue(form, "price"))
        }
    });
});

setSessionId(localStorage.getItem(sessionStorageKey) || "");

if (typeof window.__setTestConsoleBootStatus === "function") {
    window.__setTestConsoleBootStatus("client script initialized.");
}
