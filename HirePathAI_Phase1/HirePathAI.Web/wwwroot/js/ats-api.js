"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("atsAnalysisForm");
    const analyzeButton = document.getElementById("analyzeButton");
    const loadingPanel = document.getElementById("loadingPanel");
    const resultPanel = document.getElementById("resultPanel");
    const alertBox = document.getElementById("atsAlert");
    const refreshButton =
        document.getElementById("refreshAnalysesButton");

    const apiBaseUrl = "/api/v1/ats";

    form?.addEventListener("submit", analyzeResume);
    refreshButton?.addEventListener(
        "click",
        loadPreviousAnalyses);

    loadPreviousAnalyses();

    async function analyzeResume(event) {
        event.preventDefault();
        hideAlert();

        const jobId =
            document.getElementById("jobId").value;

        const resumeInput =
            document.getElementById("resumeFile");

        const resumeFile =
            resumeInput.files?.[0];

        if (!jobId) {
            showAlert(
                "Please select a job.",
                "warning");
            return;
        }

        if (!resumeFile) {
            showAlert(
                "Please select a resume PDF.",
                "warning");
            return;
        }

        if (!resumeFile.name
            .toLowerCase()
            .endsWith(".pdf")) {
            showAlert(
                "Only PDF files are allowed.",
                "warning");
            return;
        }

        if (resumeFile.size >
            10 * 1024 * 1024) {
            showAlert(
                "The file must be smaller than 10 MB.",
                "warning");
            return;
        }

        const formData = new FormData();

        formData.append("jobId", jobId);
        formData.append(
            "resumeFile",
            resumeFile);

        setLoading(true);

        try {
            const response = await fetch(
                `${apiBaseUrl}/analyze`,
                {
                    method: "POST",
                    headers: buildAuthorizationHeaders(),
                    body: formData
                });

            const responseData =
                await readJsonResponse(response);

            if (!response.ok) {
                throw new Error(
                    responseData?.message ??
                    "Resume analysis failed.");
            }

            displayAnalysis(responseData);

            await loadPreviousAnalyses();

            showAlert(
                "Resume analysis completed successfully.",
                "success");
        } catch (error) {
            console.error(error);

            showAlert(
                error.message ??
                "An unexpected error occurred.",
                "danger");
        } finally {
            setLoading(false);
        }
    }

    async function loadPreviousAnalyses() {
        const tableBody =
            document.getElementById(
                "analysisTableBody");

        tableBody.innerHTML = `
            <tr>
                <td colspan="6"
                    class="text-center text-muted">
                    Loading...
                </td>
            </tr>`;

        try {
            const response = await fetch(
                `${apiBaseUrl}/analyses`,
                {
                    headers:
                        buildAuthorizationHeaders()
                });

            if (response.status === 401) {
                tableBody.innerHTML = `
                    <tr>
                        <td colspan="6"
                            class="text-center text-danger">
                            Please log in before viewing analyses.
                        </td>
                    </tr>`;
                return;
            }

            if (response.status === 403) {
                tableBody.innerHTML = `
                    <tr>
                        <td colspan="6"
                            class="text-center text-danger">
                            Your account does not have
                            permission to view ATS analyses.
                        </td>
                    </tr>`;
                return;
            }

            const analyses =
                await readJsonResponse(response);

            if (!response.ok) {
                throw new Error(
                    analyses?.message ??
                    "Could not load analyses.");
            }

            renderAnalysisTable(
                Array.isArray(analyses)
                    ? analyses
                    : []);
        } catch (error) {
            console.error(error);

            tableBody.innerHTML = `
                <tr>
                    <td colspan="6"
                        class="text-center text-danger">
                        ${escapeHtml(error.message)}
                    </td>
                </tr>`;
        }
    }

    function displayAnalysis(result) {
        setText(
            "candidateName",
            result.candidateName ||
            "Unknown Candidate");

        const contactParts = [
            result.candidateEmail,
            result.candidatePhone,
            result.jobTitle
        ].filter(Boolean);

        setText(
            "candidateContact",
            contactParts.join(" • "));

        setText(
            "atsScore",
            `${formatScore(result.atsScore)}%`);

        setText(
            "matchPercentage",
            `${formatScore(
                result.matchPercentage)}%`);

        setText(
            "skillsScore",
            `${formatScore(
                result.skillsScore)}%`);

        setText(
            "experienceScore",
            `${formatScore(
                result.experienceScore)}%`);

        setText(
            "educationScore",
            `${formatScore(
                result.educationScore)}%`);

        setText(
            "recommendation",
            result.recommendation ||
            "No recommendation");

        setText(
            "analysisSummary",
            result.summary ||
            "No summary was provided.");

        renderTags(
            "extractedSkills",
            result.extractedSkills,
            "bg-secondary");

        renderTags(
            "matchedSkills",
            result.matchedSkills,
            "bg-success");

        renderTags(
            "missingSkills",
            result.missingSkills,
            "bg-danger");

        renderList(
            "educationList",
            result.education);

        renderList(
            "experienceList",
            result.experience);

        renderList(
            "certificationList",
            result.certifications);

        resultPanel.classList.remove("d-none");

        resultPanel.scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    }

    function renderAnalysisTable(analyses) {
        const tableBody =
            document.getElementById(
                "analysisTableBody");

        if (analyses.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="6"
                        class="text-center text-muted">
                        No resume analyses are available.
                    </td>
                </tr>`;
            return;
        }

        tableBody.innerHTML =
            analyses.map(item => `
                <tr>
                    <td>
                        <strong>
                            ${escapeHtml(
                item.candidateName)}
                        </strong>
                        <br />
                        <small class="text-muted">
                            ${escapeHtml(
                    item.candidateEmail ?? "")}
                        </small>
                    </td>
                    <td>
                        ${escapeHtml(item.jobTitle)}
                    </td>
                    <td>
                        ${formatScore(
                        item.atsScore)}%
                    </td>
                    <td>
                        ${formatScore(
                            item.matchPercentage)}%
                    </td>
                    <td>
                        ${escapeHtml(
                                item.recommendation)}
                    </td>
                    <td>
                        ${formatDate(
                                    item.processedAt)}
                    </td>
                </tr>
            `).join("");
    }

    function renderTags(
        elementId,
        items,
        backgroundClass) {
        const container =
            document.getElementById(elementId);

        const values =
            Array.isArray(items)
                ? items
                : [];

        if (values.length === 0) {
            container.innerHTML =
                `<span class="text-muted">
                    None
                 </span>`;
            return;
        }

        container.innerHTML =
            values.map(value => `
                <span class="badge ${backgroundClass}">
                    ${escapeHtml(value)}
                </span>
            `).join("");
    }

    function renderList(elementId, items) {
        const list =
            document.getElementById(elementId);

        const values =
            Array.isArray(items)
                ? items
                : [];

        if (values.length === 0) {
            list.innerHTML =
                `<li class="text-muted">None</li>`;
            return;
        }

        list.innerHTML =
            values.map(value =>
                `<li>${escapeHtml(value)}</li>`)
                .join("");
    }

    function buildAuthorizationHeaders() {
        const token =
            localStorage.getItem("token") ??
            localStorage.getItem("accessToken") ??
            sessionStorage.getItem("token") ??
            sessionStorage.getItem("accessToken");

        if (!token) {
            return {};
        }

        return {
            Authorization: `Bearer ${token}`
        };
    }

    async function readJsonResponse(response) {
        const contentType =
            response.headers.get("content-type");

        if (!contentType?.includes(
            "application/json")) {
            return null;
        }

        return await response.json();
    }

    function setLoading(isLoading) {
        analyzeButton.disabled = isLoading;

        analyzeButton.textContent =
            isLoading
                ? "Analysing..."
                : "Analyse Resume";

        loadingPanel.classList.toggle(
            "d-none",
            !isLoading);
    }

    function showAlert(message, type) {
        alertBox.textContent = message;
        alertBox.className =
            `alert alert-${type}`;
    }

    function hideAlert() {
        alertBox.className =
            "alert d-none";
        alertBox.textContent = "";
    }

    function setText(elementId, value) {
        const element =
            document.getElementById(elementId);

        if (element) {
            element.textContent =
                value ?? "";
        }
    }

    function formatScore(value) {
        const number = Number(value);

        return Number.isFinite(number)
            ? number.toFixed(1)
            : "0.0";
    }

    function formatDate(value) {
        const date = new Date(value);

        return Number.isNaN(date.getTime())
            ? ""
            : date.toLocaleString();
    }

    function escapeHtml(value) {
        const element =
            document.createElement("div");

        element.textContent =
            value == null
                ? ""
                : String(value);

        return element.innerHTML;
    }
});