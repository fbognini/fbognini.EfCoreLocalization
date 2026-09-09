/**
 * Localization dashboard: languages, texts, translations and the export/import panel.
 *
 * @example
 * $(document).ready(function () {
 *     new LocalizationDashboard({ basePath: "/localization" }).start();
 * });
 *
 * Required markup, all ids as written here:
 * - navigation links carrying data-page="languages|texts|translations";
 * - one section per page, #pageLanguages, #pageTexts and #pageTranslations, each with the pageContent class;
 * - a table per page, #languagesTable, #textsTable and #translationsTable;
 * - the modals #languageModal, #textModal, #translationModal and #portabilityModal with the forms they contain;
 * - the translation filters #filterLanguage, #filterTextId and #filterResourceId;
 * - the portability controls #portabilityFormat, #portabilityResourceIds, #portabilityFile, #portabilityCreateTexts, #portabilityDeleteNotMatched, #portabilityMessage and #portabilityModalBody;
 * - the buttons #addLanguageButton, #saveLanguageButton, #addTextButton, #saveTextButton, #saveTranslationButton, #searchTranslationsButton, #clearTranslationsFiltersButton, #downloadExportButton, #uploadImportButton and #applyImportButton.
 *
 * @typedef {Object} LanguageDto
 * @property {string} id
 * @property {string} description
 * @property {boolean} isActive
 * @property {boolean} isDefault
 *
 * @typedef {Object} TextDto
 * @property {string} textId
 * @property {string} resourceId
 * @property {string} description
 * @property {string} createdOnUtc
 *
 * @typedef {Object} TranslationDto
 * @property {string} languageId
 * @property {string} textId
 * @property {string} resourceId
 * @property {string} destination
 * @property {string} updatedOnUtc
 *
 * @typedef {Object} ImportResultDto
 * @property {number} textsCreated
 * @property {number} textsUpdated
 * @property {number} textsDeleted
 * @property {number} translationsAdded
 * @property {number} translationsUpdated
 * @property {number} translationsUnchanged
 * @property {number} translationsSkipped
 * @property {number} translationsDeleted
 * @property {Array<{sourceRow: number, kind: string, textId: string, reason: string}>} errors
 */
(function () {
    "use strict";

    const DATATABLE_DEFAULTS = Object.freeze({
        scrollX: true,
        dom: `
    <'datatables_bottom_wrapper d-flex flex-column flex-md-row justify-content-between align-items-center'<''lB><''f>>
    <'row'<'col-sm-12'tr>>
    <'datatables_bottom_wrapper d-flex flex-column flex-md-row justify-content-between align-items-center'<'py-1'i><''p>>`,
        lengthMenu: [[10, 25, 50], [10, 25, 50]],
        lengthChange: true,
        pageLength: 10,
        paging: true,
        pagingType: "simple",
        stateSave: false,
        serverSide: true,
        autoWidth: false,
        buttons: []
    });

    const FULL_SEARCH_PARAMETERS = Object.freeze({
        q: true,
        lenght: true,
        start: true,
        sort: true
    });

    const DATE_TIME_PATTERN = "dd/MM/yyyy HH:mm";

    $.extend(true, $.fn.dataTable.defaults, DATATABLE_DEFAULTS);

    function byId(id) {
        return document.getElementById(id);
    }

    function escapeHtml(value) {
        const element = document.createElement("span");
        element.textContent = value ?? "";

        return element.innerHTML;
    }

    function renderDateTime() {
        return function (value) {
            if (!value) {
                return "";
            }

            return dateFns.format(new Date(value), DATE_TIME_PATTERN);
        };
    }

    function combineEndpointWithCurrentSearch(url) {
        const endpoint = new URL(url, window.location.origin);
        const params = new URLSearchParams(endpoint.search);

        for (const [key, value] of new URLSearchParams(window.location.search).entries()) {
            params.set(key, value);
        }

        endpoint.search = params.toString();

        return endpoint.toString();
    }

    function booleanBadge(value, trueClass) {
        if (value) {
            return `<span class="badge ${trueClass}">Yes</span>`;
        }

        return `<span class="badge bg-secondary">No</span>`;
    }

    function actionButton(action, label, buttonClass) {
        return `<button type="button" class="btn btn-sm ${buttonClass}" data-action="${action}">${label}</button>`;
    }

    function postJson(url, method, payload) {
        return fetch(url, {
            method: method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
    }

    /**
     * Wires a delegated click listener on a DataTables body and hands the handler the row the button belongs to.
     */
    function onRowAction(table, tableElement, handler) {
        tableElement.addEventListener("click", function (event) {
            const button = event.target.closest("button[data-action]");
            if (!button) {
                return;
            }

            const row = table.row(button.closest("tr")).data();
            if (!row) {
                return;
            }

            handler(button.dataset.action, row);
        });
    }

    class LanguagesPage {
        #apiBase;
        #tableElement;
        #table = null;
        #modal = null;

        constructor({ apiBase }) {
            this.#apiBase = apiBase;
            this.#tableElement = byId("languagesTable");
        }

        start() {
            byId("addLanguageButton").addEventListener("click", () => this.#showCreateModal());
            byId("saveLanguageButton").addEventListener("click", () => this.#save());
        }

        load() {
            if (!this.#table) {
                this.#initTable();
                return;
            }

            this.#table.ajax.reload();
        }

        #initTable() {
            const parameters = Object.assign({}, FULL_SEARCH_PARAMETERS);
            const options = {
                drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
                ajax: fullSearchDatatables(() => `${this.#apiBase}/languages`),
                order: [[0, "asc"]],
                columns: [
                    { data: "id" },
                    { data: "description" },
                    {
                        data: "isActive",
                        orderable: false,
                        render: (data) => booleanBadge(data, "bg-success")
                    },
                    {
                        data: "isDefault",
                        orderable: false,
                        render: (data) => booleanBadge(data, "bg-primary")
                    },
                    {
                        data: null,
                        orderable: false,
                        render: () => actionButton("edit", "Edit", "btn-primary")
                    }
                ]
            };

            overrideDatatableFullSearchOptionsWithLocation(options, parameters);
            this.#table = $(this.#tableElement).DataTable(options);

            onRowAction(this.#table, this.#tableElement, (action, row) => this.#edit(row));
        }

        #modalInstance() {
            if (!this.#modal) {
                this.#modal = new bootstrap.Modal(byId("languageModal"));
            }

            return this.#modal;
        }

        #showCreateModal() {
            byId("languageModalTitle").textContent = "Add Language";
            byId("languageForm").reset();
            byId("languageId").value = "";
            byId("languageCode").readOnly = false;
            byId("languageActive").checked = true;
            this.#modalInstance().show();
        }

        /**
         * @param {LanguageDto} language
         */
        #edit(language) {
            byId("languageModalTitle").textContent = "Edit Language";
            byId("languageId").value = language.id;
            byId("languageCode").value = language.id;
            byId("languageCode").readOnly = true;
            byId("languageDescription").value = language.description || "";
            byId("languageActive").checked = language.isActive || false;
            byId("languageDefault").checked = language.isDefault || false;
            this.#modalInstance().show();
        }

        #save() {
            const form = byId("languageForm");
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const id = byId("languageId").value;
            const payload = {
                id: byId("languageCode").value,
                description: byId("languageDescription").value,
                isActive: byId("languageActive").checked,
                isDefault: byId("languageDefault").checked
            };

            const url = id ? `${this.#apiBase}/languages/${encodeURIComponent(id)}` : `${this.#apiBase}/languages`;

            postJson(url, id ? "PUT" : "POST", payload)
                .then((response) => {
                    if (!response.ok) {
                        window.alert("Error saving language");
                        return;
                    }

                    this.#modalInstance().hide();
                    this.load();
                })
                .catch((error) => {
                    console.error(error);
                    window.alert("Error saving language");
                });
        }
    }

    class TextsPage {
        #apiBase;
        #tableElement;
        #onViewTranslations;
        #table = null;
        #modal = null;

        constructor({ apiBase, onViewTranslations }) {
            this.#apiBase = apiBase;
            this.#onViewTranslations = onViewTranslations;
            this.#tableElement = byId("textsTable");
        }

        start() {
            byId("addTextButton").addEventListener("click", () => this.#showCreateModal());
            byId("saveTextButton").addEventListener("click", () => this.#save());
        }

        load() {
            if (!this.#table) {
                this.#initTable();
                return;
            }

            this.#table.ajax.reload();
        }

        #initTable() {
            const parameters = Object.assign({}, FULL_SEARCH_PARAMETERS);
            const options = {
                drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
                ajax: fullSearchDatatables(() => `${this.#apiBase}/texts`),
                ordering: false,
                columns: [
                    { data: "textId" },
                    { data: "resourceId" },
                    { data: "description" },
                    {
                        data: "createdOnUtc",
                        render: renderDateTime()
                    },
                    {
                        data: null,
                        orderable: false,
                        render: () => `
                            ${actionButton("view", "View translations", "btn-primary me-1")}
                            ${actionButton("delete", "Delete", "btn-danger")}
                        `
                    }
                ]
            };

            overrideDatatableFullSearchOptionsWithLocation(options, parameters);
            this.#table = $(this.#tableElement).DataTable(options);

            onRowAction(this.#table, this.#tableElement, (action, row) => this.#handleRowAction(action, row));
        }

        /**
         * @param {string} action
         * @param {TextDto} text
         */
        #handleRowAction(action, text) {
            if (action === "view") {
                this.#onViewTranslations(text.textId, text.resourceId);
                return;
            }

            if (action === "delete") {
                this.#delete(text.textId, text.resourceId);
            }
        }

        #modalInstance() {
            if (!this.#modal) {
                this.#modal = new bootstrap.Modal(byId("textModal"));
            }

            return this.#modal;
        }

        #showCreateModal() {
            byId("textModalTitle").textContent = "Add Text";
            byId("textForm").reset();
            this.#modalInstance().show();
        }

        #save() {
            const form = byId("textForm");
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const payload = {
                textId: byId("textId").value,
                resourceId: byId("textResourceId").value,
                description: byId("textDescription").value || ""
            };

            postJson(`${this.#apiBase}/texts`, "POST", payload)
                .then((response) => {
                    if (!response.ok) {
                        window.alert("Error saving text");
                        return;
                    }

                    this.#modalInstance().hide();
                    this.load();
                })
                .catch((error) => {
                    console.error(error);
                    window.alert("Error saving text");
                });
        }

        #delete(textId, resourceId) {
            if (!window.confirm(`Are you sure you want to delete text "${textId}" in resource "${resourceId}"?`)) {
                return;
            }

            const url = `${this.#apiBase}/texts/${encodeURIComponent(textId)}/${encodeURIComponent(resourceId)}`;

            fetch(url, { method: "DELETE" })
                .then((response) => {
                    if (!response.ok) {
                        window.alert("Error deleting text");
                        return;
                    }

                    this.load();
                })
                .catch((error) => {
                    console.error(error);
                    window.alert("Error deleting text");
                });
        }
    }

    class TranslationsPage {
        static FILTER_KEYS = Object.freeze(["languageId", "textId", "resourceId"]);
        static FILTER_FIELD_IDS = Object.freeze({
            languageId: "filterLanguage",
            textId: "filterTextId",
            resourceId: "filterResourceId"
        });

        #apiBase;
        #tableElement;
        #table = null;
        #modal = null;
        #languagesLoaded = false;

        constructor({ apiBase }) {
            this.#apiBase = apiBase;
            this.#tableElement = byId("translationsTable");
        }

        start() {
            byId("searchTranslationsButton").addEventListener("click", () => this.#applyFilters());
            byId("clearTranslationsFiltersButton").addEventListener("click", () => this.#clearFilters());
            byId("saveTranslationButton").addEventListener("click", () => this.#save());
        }

        load() {
            this.#readFiltersFromUrl();
            this.#loadLanguageOptions();

            if (!this.#table) {
                this.#initTable();
                return;
            }

            this.#table.ajax.reload();
        }

        #initTable() {
            const parameters = Object.assign({}, FULL_SEARCH_PARAMETERS);
            const options = {
                drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
                ajax: fullSearchDatatables(() => combineEndpointWithCurrentSearch(`${this.#apiBase}/translations`)),
                order: [[0, "asc"]],
                columns: [
                    { data: "languageId" },
                    { data: "textId" },
                    { data: "resourceId" },
                    { data: "destination" },
                    {
                        data: "updatedOnUtc",
                        render: renderDateTime()
                    },
                    {
                        data: null,
                        orderable: false,
                        render: () => actionButton("edit", "Edit", "btn-primary")
                    }
                ]
            };

            overrideDatatableFullSearchOptionsWithLocation(options, parameters);
            this.#table = $(this.#tableElement).DataTable(options);

            onRowAction(this.#table, this.#tableElement, (action, row) => this.#edit(row));
        }

        #loadLanguageOptions() {
            if (this.#languagesLoaded) {
                return;
            }

            this.#languagesLoaded = true;

            fetch(`${this.#apiBase}/languages`)
                .then((response) => response.json())
                .then((payload) => {
                    const select = byId("filterLanguage");

                    select.innerHTML = `<option value="">All Languages</option>`;
                    payload.items.forEach((language) => {
                        const option = document.createElement("option");
                        option.value = language.id;
                        option.textContent = `${language.id} - ${language.description}`;
                        select.appendChild(option);
                    });

                    // Reapplied here: the options only exist now, so the value the url asked for could not be selected earlier.
                    this.#readFiltersFromUrl();
                })
                .catch((error) => {
                    this.#languagesLoaded = false;
                    console.error(error);
                });
        }

        #readFiltersFromUrl() {
            const params = new URLSearchParams(window.location.search);

            TranslationsPage.FILTER_KEYS.forEach((key) => {
                const field = byId(TranslationsPage.FILTER_FIELD_IDS[key]);
                if (!field) {
                    return;
                }

                field.value = params.get(key) || "";
            });
        }

        #writeFiltersToUrl() {
            const url = new URL(window.location.href);

            TranslationsPage.FILTER_KEYS.forEach((key) => {
                const value = byId(TranslationsPage.FILTER_FIELD_IDS[key]).value;
                if (!value) {
                    url.searchParams.delete(key);
                    return;
                }

                url.searchParams.set(key, value);
            });

            window.history.pushState({ page: "translations" }, "", url.toString());
        }

        #applyFilters() {
            this.#writeFiltersToUrl();
            this.load();
        }

        #clearFilters() {
            TranslationsPage.FILTER_KEYS.forEach((key) => {
                byId(TranslationsPage.FILTER_FIELD_IDS[key]).value = "";
            });

            this.#writeFiltersToUrl();
            this.load();
        }

        #modalInstance() {
            if (!this.#modal) {
                this.#modal = new bootstrap.Modal(byId("translationModal"));
            }

            return this.#modal;
        }

        /**
         * @param {TranslationDto} translation
         */
        #edit(translation) {
            byId("translationModalTitle").textContent = "Edit Translation";
            byId("translationLanguageId").value = translation.languageId;
            byId("translationTextId").value = translation.textId;
            byId("translationResourceId").value = translation.resourceId;
            byId("translationLanguageDisplay").value = translation.languageId;
            byId("translationTextIdDisplay").value = translation.textId;
            byId("translationResourceIdDisplay").value = translation.resourceId;
            byId("translationDestination").value = translation.destination || "";
            this.#modalInstance().show();
        }

        #save() {
            const form = byId("translationForm");
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const payload = {
                languageId: byId("translationLanguageId").value,
                textId: byId("translationTextId").value,
                resourceId: byId("translationResourceId").value,
                destination: byId("translationDestination").value
            };

            postJson(`${this.#apiBase}/translations`, "PUT", payload)
                .then((response) => {
                    if (!response.ok) {
                        window.alert("Error saving translation");
                        return;
                    }

                    this.#modalInstance().hide();
                    this.load();
                })
                .catch((error) => {
                    console.error(error);
                    window.alert("Error saving translation");
                });
        }
    }

    class PortabilityPanel {
        static MAX_ERRORS_SHOWN = 50;

        #apiBase;
        #onImported;
        #modal = null;
        #formatsLoaded = false;

        constructor({ apiBase, onImported }) {
            this.#apiBase = apiBase;
            this.#onImported = onImported;
        }

        start() {
            byId("downloadExportButton").addEventListener("click", () => this.#download());
            byId("uploadImportButton").addEventListener("click", () => this.#preview());
            byId("applyImportButton").addEventListener("click", () => this.#apply());
        }

        load() {
            if (this.#formatsLoaded) {
                return;
            }

            this.#formatsLoaded = true;

            fetch(`${this.#apiBase}/translations/formats`)
                .then((response) => response.json())
                .then((formats) => {
                    byId("portabilityFormat").innerHTML = formats
                        .map((format) => `<option value="${escapeHtml(format.name)}">${escapeHtml(format.name.toUpperCase())}</option>`)
                        .join("");
                })
                .catch((error) => {
                    this.#formatsLoaded = false;
                    console.error(error);
                });
        }

        #download() {
            const params = new URLSearchParams();
            params.set("format", byId("portabilityFormat").value);

            const resourceIds = byId("portabilityResourceIds").value.trim();
            if (resourceIds) {
                params.set("resourceIds", resourceIds);
            }

            window.location = `${this.#apiBase}/translations/export?${params.toString()}`;
        }

        // Two steps on purpose: a filtered export reimported with "Delete keys missing from the file" is a mass deletion.
        #preview() {
            this.#send(true).then((result) => {
                if (!result) {
                    return;
                }

                byId("portabilityModalBody").innerHTML = this.#summarize(result);
                this.#modalInstance().show();
            });
        }

        #apply() {
            this.#send(false).then((result) => {
                if (!result) {
                    return;
                }

                this.#modalInstance().hide();
                this.#message(`<div class="alert alert-success mb-0">${this.#summarize(result)}</div>`);
                this.#onImported();
            });
        }

        #modalInstance() {
            if (!this.#modal) {
                this.#modal = new bootstrap.Modal(byId("portabilityModal"));
            }

            return this.#modal;
        }

        #send(dryRun) {
            const input = byId("portabilityFile");
            if (!input.files.length) {
                this.#message(`<div class="alert alert-warning mb-0">Pick a file first.</div>`);
                return Promise.resolve(null);
            }

            const body = new FormData();
            body.append("file", input.files[0]);
            body.append("dryRun", dryRun);
            body.append("createMissingTexts", byId("portabilityCreateTexts").checked);
            body.append("deleteNotMatched", byId("portabilityDeleteNotMatched").checked);

            this.#message("");

            return fetch(`${this.#apiBase}/translations/import`, { method: "POST", body })
                .then((response) => response.json().then((payload) => ({ ok: response.ok, payload })))
                .then(({ ok, payload }) => {
                    if (!ok) {
                        this.#message(`<div class="alert alert-danger mb-0">${escapeHtml(payload.error) || "Import failed"}</div>`);
                        return null;
                    }

                    return payload;
                })
                .catch((error) => {
                    console.error(error);
                    this.#message(`<div class="alert alert-danger mb-0">Import failed</div>`);
                    return null;
                });
        }

        /**
         * @param {ImportResultDto} result
         */
        #summarize(result) {
            const counters = [
                `<strong>${result.textsCreated}</strong> keys created`,
                `<strong>${result.textsUpdated}</strong> descriptions updated`,
                `<strong>${result.translationsAdded}</strong> translations added`,
                `<strong>${result.translationsUpdated}</strong> translations updated`,
                `<strong>${result.translationsUnchanged}</strong> unchanged`,
                `<strong>${result.translationsSkipped}</strong> empty cells skipped`
            ];

            if (result.textsDeleted || result.translationsDeleted) {
                counters.push(`<strong class="text-danger">${result.textsDeleted}</strong> keys and <strong class="text-danger">${result.translationsDeleted}</strong> translations deleted`);
            }

            let html = `<p class="mb-2">${counters.join(", ")}.</p>`;

            if (!result.errors.length) {
                return html;
            }

            const rows = result.errors
                .slice(0, PortabilityPanel.MAX_ERRORS_SHOWN)
                .map((error) => `<tr><td>${escapeHtml(error.sourceRow)}</td><td>${escapeHtml(error.kind)}</td><td>${escapeHtml(error.textId)}</td><td>${escapeHtml(error.reason)}</td></tr>`)
                .join("");

            html += `<div class="table-responsive"><table class="table table-sm table-bordered mb-0">
                <thead><tr><th>Row</th><th>Kind</th><th>TextId</th><th>Reason</th></tr></thead>
                <tbody>${rows}</tbody></table></div>`;

            if (result.errors.length > PortabilityPanel.MAX_ERRORS_SHOWN) {
                html += `<p class="mb-0 mt-2">and ${result.errors.length - PortabilityPanel.MAX_ERRORS_SHOWN} more.</p>`;
            }

            return html;
        }

        #message(html) {
            byId("portabilityMessage").innerHTML = html;
        }
    }

    class LocalizationDashboard {
        static PAGES = Object.freeze(["languages", "texts", "translations"]);
        static SECTION_IDS = Object.freeze({
            languages: "pageLanguages",
            texts: "pageTexts",
            translations: "pageTranslations"
        });

        #basePath;
        #defaultPage;
        #languagesPage;
        #textsPage;
        #translationsPage;
        #portabilityPanel;

        /**
         * @param {Object} options
         * @param {string} options.basePath Mount path of the dashboard, without a trailing slash.
         * @param {string} [options.defaultPage] Page shown when the url matches none.
         */
        constructor({ basePath, defaultPage = "languages" }) {
            this.#basePath = basePath;
            this.#defaultPage = defaultPage;

            const apiBase = `${basePath}/api`;

            this.#languagesPage = new LanguagesPage({ apiBase });
            this.#textsPage = new TextsPage({
                apiBase,
                onViewTranslations: (textId, resourceId) => this.#showTranslationsFor(textId, resourceId)
            });
            this.#translationsPage = new TranslationsPage({ apiBase });
            this.#portabilityPanel = new PortabilityPanel({
                apiBase,
                onImported: () => this.#translationsPage.load()
            });
        }

        start() {
            this.#languagesPage.start();
            this.#textsPage.start();
            this.#translationsPage.start();
            this.#portabilityPanel.start();

            window.addEventListener("popstate", () => this.#navigate(this.#pageFromUrl(), false));

            document.querySelectorAll("[data-page]").forEach((link) => {
                link.addEventListener("click", (event) => {
                    event.preventDefault();
                    this.#navigate(link.dataset.page, true);
                });
            });

            this.#navigate(this.#pageFromUrl(), false);
        }

        #pageFromUrl() {
            const match = window.location.pathname.match(/\/(languages|texts|translations)$/);

            return match ? match[1] : this.#defaultPage;
        }

        #navigate(page, pushState) {
            if (!LocalizationDashboard.PAGES.includes(page)) {
                return;
            }

            if (pushState) {
                window.history.pushState({ page }, "", `${this.#basePath}/${page}`);
            }

            document.querySelectorAll("[data-page]").forEach((link) => {
                link.classList.toggle("active", link.dataset.page === page);
            });

            LocalizationDashboard.PAGES.forEach((name) => {
                byId(LocalizationDashboard.SECTION_IDS[name]).classList.toggle("active", name === page);
            });

            this.#loadPage(page);
        }

        #loadPage(page) {
            if (page === "languages") {
                this.#languagesPage.load();
                return;
            }

            if (page === "texts") {
                this.#textsPage.load();
                return;
            }

            this.#translationsPage.load();
            this.#portabilityPanel.load();
        }

        #showTranslationsFor(textId, resourceId) {
            const url = new URL(`${this.#basePath}/translations`, window.location.origin);
            url.searchParams.set("textId", textId);
            url.searchParams.set("resourceId", resourceId);

            window.history.pushState({ page: "translations" }, "", url.toString());
            this.#navigate("translations", false);
        }
    }

    window.LocalizationDashboard = LocalizationDashboard;
})();
