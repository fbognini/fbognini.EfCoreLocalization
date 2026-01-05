const DataTable = $.fn.dataTable;

$.extend(true, DataTable.defaults, {
    //language: {
    //    url: "/_content/fbognini.EfCoreLocalization.Dashboard/lib/Datatables/it-IT.json"
    //},
    scrollX: true,
    dom: `

    <'datatables_bottom_wrapper d-flex flex-column flex-md-row justify-content-between align-items-center'<''lB><''f>>
    <'row'<'col-sm-12'tr>>
    <'datatables_bottom_wrapper d-flex flex-column flex-md-row justify-content-between align-items-center'<'py-1'i><''p>>`,
    // dom: "<'row mb-3'<'col-sm-4'l><'col-sm-8 text-end'<'d-flex justify-content-end'fB>>>t<'d-flex align-items-center'<'me-auto'i><'mb-0'p>>",
    // dom: "<'row mb-3'<'col-sm-8 multi-buttons'B><'col-sm-4 text-right'<'d-flex justify-content-end 'f>>>t<'row mt-3'<'col-sm-4'i><'col-sm-8 d-flex justify-content-end'<'row'<'col-md-auto align-self-end'l><'col-md-auto'p>>>>",
    lengthMenu: [[10, 25, 50], [10, 25, 50]],
    lengthChange: true,
    pageLength: 10,
    // Paging Setups
    paging: true,
    pagingType: 'simple',
    stateSave: false,
    serverSide: true,
    autoWidth: false,
    buttons: [],
});

const combineEndpointWithCurrentSearch = function (url) {

    const baseUrl = new URL(url, window.location.origin);
    const params = new URLSearchParams(baseUrl.search);

    const currentParams = new URLSearchParams(window.location.search);
    for (const [key, value] of currentParams.entries()) {
        params.set(key, value);
    }

    baseUrl.search = params.toString();

    return baseUrl.toString();
}

const renderDatatableDateTime = function (to, locale) {

    if (arguments.length === 0) {
        to = 'dd/MM/yyyy HH:mm';
        locale = 'en';
    }
    else if (arguments.length === 1) {
        locale = 'en';
    }

    return function (d, type, row) {
        if (!d) {
            return '';
        }

        return dateFns.format(new Date(d), to);
    }
};


// Global configuration - BASE_PATH will be replaced by server
let BASE_PATH = '{{BASE_PATH}}';
// If still has placeholder, try to detect from current URL
if (BASE_PATH.includes('{{BASE_PATH}}')) {
    const path = window.location.pathname;
    const match = path.match(/^(\/[^\/]+)/);
    BASE_PATH = match ? match[1] : '/localization';
}
const API_BASE = `${BASE_PATH}/api`;

// Router
const router = {
    currentPage: 'languages',
    init() {
        // Handle popstate (back/forward navigation)
        window.addEventListener('popstate', () => this.handleRoute());
        // Handle initial load
        this.handleRoute();

        // Intercept link clicks to prevent default navigation
        document.querySelectorAll('.nav-link[data-page]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = link.getAttribute('data-page');
                this.navigate(page, true);
            });
        });
    },
    handleRoute() {
        const path = window.location.pathname;
        const match = path.match(/\/(languages|texts|translations)$/);
        const page = match ? match[1] : 'languages';
        this.navigate(page, false);
    },
    navigate(page, updateUrl = true) {
        if (updateUrl) {
            const newPath = `${BASE_PATH}/${page}`;
            window.history.pushState({ page }, '', newPath);
        }

        // Update nav
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('data-page') === page) {
                link.classList.add('active');
            }
        });

        // Update content
        document.querySelectorAll('.page-content').forEach(content => {
            content.classList.remove('active');
        });

        const targetPage = document.getElementById(`page-${page}`);
        if (targetPage) {
            targetPage.classList.add('active');
            this.currentPage = page;

            // Load page data
            switch (page) {
                case 'languages':
                    languagesManager.load();
                    break;
                case 'texts':
                    textsManager.load();
                    break;
                case 'translations':
                    // Load filters from URL when navigating to translations page
                    translationsManager.loadFiltersFromUrl();
                    translationsManager.load();
                    break;
            }
        }
    }
};

// Languages Manager
const languagesManager = {
    table: null,
    modal: null,

    init() {
        this.modal = new bootstrap.Modal(document.getElementById('language-modal'));

        let parameters = {
            q: true,
            lenght: true,
            start: true,
            sort: true,
        };

        const options = {
            drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
            ajax: fullSearchDatatables(() => `${API_BASE}/languages`),
            order: [[0, 'asc']],
            columns: [
                { data: 'id' },
                { data: 'description' },
                {
                    data: 'isActive',
                    orderable: false,
                    render: (data) => data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-secondary">No</span>'
                },
                {
                    data: 'isDefault',
                    orderable: false,
                    render: (data) => data ? '<span class="badge bg-primary">Yes</span>' : '<span class="badge bg-secondary">No</span>'
                },
                {
                    data: null,
                    orderable: false,
                    render: (data, type, row) => {
                        const id = row.id.replace(/'/g, "\\'");
                        return `<button class="btn btn-sm btn-primary" onclick="languagesManager.edit('${id}')">Edit</button>`;
                    }
                }
            ],
        };

        overrideDatatableFullSearchOptionsWithLocation(options, parameters);
        this.table = $('#languages-table').DataTable(options);

    },

    load() {
        if (this.table) {
            this.table.ajax.reload();
        } else {
            this.init();
        }
    },

    showCreateModal() {
        document.getElementById('language-modal-title').textContent = 'Add Language';
        document.getElementById('language-form').reset();
        document.getElementById('language-id').value = '';
        document.getElementById('language-active').checked = true;
        this.modal.show();
    },

    edit(id) {
        // Find the row in the DataTable
        const rowData = this.table.rows().data().toArray().find(row => row.id === id);
        
        if (!rowData) {
            alert('Language not found');
            return;
        }

        document.getElementById('language-modal-title').textContent = 'Edit Language';
        document.getElementById('language-id').value = rowData.id;
        document.getElementById('language-code').value = rowData.id;
        document.getElementById('language-code').readOnly = true;
        document.getElementById('language-description').value = rowData.description || '';
        document.getElementById('language-active').checked = rowData.isActive || false;
        document.getElementById('language-default').checked = rowData.isDefault || false;
        this.modal.show();
    },

    save() {
        const form = document.getElementById('language-form');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const id = document.getElementById('language-id').value;
        const data = {
            id: document.getElementById('language-code').value,
            description: document.getElementById('language-description').value,
            isActive: document.getElementById('language-active').checked,
            isDefault: document.getElementById('language-default').checked
        };

        const url = id ? `${API_BASE}/languages/${id}` : `${API_BASE}/languages`;
        const method = id ? 'PUT' : 'POST';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(res => {
                if (res.ok) {
                    this.modal.hide();
                    this.load();
                } else {
                    alert('Error saving language');
                }
            })
            .catch(err => {
                console.error(err);
                alert('Error saving language');
            });
    }
};

// Texts Manager
const textsManager = {
    table: null,
    modal: null,

    init() {
        this.modal = new bootstrap.Modal(document.getElementById('text-modal'));

        let parameters = {
            q: true,
            lenght: true,
            start: true,
            sort: true,
        };

        const options = {
            drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
            ajax: fullSearchDatatables(() => `${API_BASE}/texts`),
            ordering: false,
            columns: [
                { data: 'textId' },
                { data: 'resourceId' },
                { data: 'description' },
                {
                    data: 'createdOnUtc',
                    render: renderDatatableDateTime()
                },
                {
                    data: null,
                    orderable: false,
                    render: (data, type, row) => {
                        const textId = row.textId.replace(/'/g, "\\'");
                        const resourceId = row.resourceId.replace(/'/g, "\\'");
                        return `
                            <button class="btn btn-sm btn-primary me-1" onclick="textsManager.viewTranslations('${textId}', '${resourceId}')">View translations</button>
                            <button class="btn btn-sm btn-danger" onclick="textsManager.delete('${textId}', '${resourceId}')">Delete</button>
                        `;
                    }
                }
            ],
        };

        overrideDatatableFullSearchOptionsWithLocation(options, parameters);
        this.table = $('#texts-table').DataTable(options);

    },

    load() {
        if (this.table) {
            this.table.ajax.reload();
        } else {
            this.init();
        }
    },

    showCreateModal() {
        document.getElementById('text-modal-title').textContent = 'Add Text';
        document.getElementById('text-form').reset();
        this.modal.show();
    },

    save() {
        const form = document.getElementById('text-form');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const data = {
            textId: document.getElementById('text-id').value,
            resourceId: document.getElementById('text-resourceid').value,
            description: document.getElementById('text-description').value || ''
        };

        fetch(`${API_BASE}/texts`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(res => {
                if (res.ok) {
                    this.modal.hide();
                    this.load();
                } else {
                    alert('Error saving text');
                }
            })
            .catch(err => {
                console.error(err);
                alert('Error saving text');
            });
    },

    viewTranslations(textId, resourceId) {
        // Navigate to translations page with filters
        const url = new URL(`${BASE_PATH}/translations`, window.location.origin);
        url.searchParams.set('textId', textId);
        url.searchParams.set('resourceId', resourceId);
        
        // Update URL and navigate
        window.history.pushState({ page: 'translations' }, '', url.toString());
        router.navigate('translations', false);
    },

    delete(textId, resourceId) {
        if (!confirm(`Are you sure you want to delete text "${textId}" in resource "${resourceId}"?`)) {
            return;
        }

        fetch(`${API_BASE}/texts/${encodeURIComponent(textId)}/${encodeURIComponent(resourceId)}`, {
            method: 'DELETE'
        })
            .then(res => {
                if (res.ok) {
                    this.load();
                } else {
                    alert('Error deleting text');
                }
            })
            .catch(err => {
                console.error(err);
                alert('Error deleting text');
            });
    }
};

// Translations Manager
const translationsManager = {
    table: null,
    modal: null,
    languages: [],
    filters: {
        languageId: '',
        textId: '',
        resourceId: ''
    },

    init() {
        this.modal = new bootstrap.Modal(document.getElementById('translation-modal'));

        // Load filters from URL
        this.loadFiltersFromUrl();

        // Load languages for filter
        fetch(`${API_BASE}/languages`)
            .then(res => res.json())
            .then(response => {
                this.languages = response.items;
                const select = document.getElementById('filter-language');
                select.innerHTML = '<option value="">All Languages</option>';
                this.languages.forEach(lang => {
                    const option = document.createElement('option');
                    option.value = lang.id;
                    option.textContent = `${lang.id} - ${lang.description}`;
                    select.appendChild(option);
                });

                // Set selected value from filters
                if (this.filters.languageId) {
                    select.value = this.filters.languageId;
                }
            });

        let parameters = {
            q: true,
            lenght: true,
            start: true,
            sort: true,
        };

        const options = {
            drawCallback: fullSearchDatatablesCallbackWithLocation(parameters),
            ajax: fullSearchDatatables(() => combineEndpointWithCurrentSearch(`${API_BASE}/translations`)),
            order: [[0, 'asc']],
            columns: [
                { data: 'languageId' },
                { data: 'textId' },
                { data: 'resourceId' },
                { data: 'destination' },
                {
                    data: 'updatedOnUtc',
                    render: renderDatatableDateTime()
                },
                {
                    data: null,
                    orderable: false,
                    render: (data, type, row) => {
                        const langId = row.languageId.replace(/'/g, "\\'");
                        const textId = row.textId.replace(/'/g, "\\'");
                        const resId = row.resourceId.replace(/'/g, "\\'");
                        return `<button class="btn btn-sm btn-primary" onclick="translationsManager.edit('${langId}', '${textId}', '${resId}')">Edit</button>`;
                    }
                }
            ],
        };

        overrideDatatableFullSearchOptionsWithLocation(options, parameters);
        this.table = $('#translations-table').DataTable(options);

    },

    loadFiltersFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);
        this.filters.languageId = urlParams.get('languageId') || '';
        this.filters.textId = urlParams.get('textId') || '';
        this.filters.resourceId = urlParams.get('resourceId') || '';

        // Update form fields
        if (document.getElementById('filter-language')) {
            document.getElementById('filter-language').value = this.filters.languageId;
        }
        if (document.getElementById('filter-textid')) {
            document.getElementById('filter-textid').value = this.filters.textId;
        }
        if (document.getElementById('filter-resourceid')) {
            document.getElementById('filter-resourceid').value = this.filters.resourceId;
        }
    },

    updateUrlWithFilters() {
        const url = new URL(window.location.href);

        // Remove existing filter params
        url.searchParams.delete('languageId');
        url.searchParams.delete('textId');
        url.searchParams.delete('resourceId');

        // Add current filters
        if (this.filters.languageId) {
            url.searchParams.set('languageId', this.filters.languageId);
        }
        if (this.filters.textId) {
            url.searchParams.set('textId', this.filters.textId);
        }
        if (this.filters.resourceId) {
            url.searchParams.set('resourceId', this.filters.resourceId);
        }

        // Update URL without reload
        window.history.pushState({ filters: this.filters }, '', url.toString());
    },

    load() {
        this.loadFiltersFromUrl();

        if (this.table) {
            this.table.ajax.reload();
        } else {
            this.init();
        }
    },

    applyFilters() {
        this.filters.languageId = document.getElementById('filter-language').value;
        this.filters.textId = document.getElementById('filter-textid').value;
        this.filters.resourceId = document.getElementById('filter-resourceid').value;

        this.updateUrlWithFilters();
        this.load();
    },

    clearFilters() {
        document.getElementById('filter-language').value = '';
        document.getElementById('filter-textid').value = '';
        document.getElementById('filter-resourceid').value = '';
        this.filters = { languageId: '', textId: '', resourceId: '' };

        this.updateUrlWithFilters();
        this.load();
    },

    edit(languageId, textId, resourceId) {
        // Find the row in the DataTable
        const rowData = this.table.rows().data().toArray().find(row => 
            row.languageId === languageId && 
            row.textId === textId && 
            row.resourceId === resourceId
        );
        
        if (!rowData) {
            alert('Translation not found');
            return;
        }

        document.getElementById('translation-modal-title').textContent = 'Edit Translation';
        document.getElementById('translation-languageid').value = rowData.languageId;
        document.getElementById('translation-textid').value = rowData.textId;
        document.getElementById('translation-resourceid').value = rowData.resourceId;
        document.getElementById('translation-language-display').value = rowData.languageId;
        document.getElementById('translation-textid-display').value = rowData.textId;
        document.getElementById('translation-resourceid-display').value = rowData.resourceId;
        document.getElementById('translation-destination').value = rowData.destination || '';
        this.modal.show();
    },

    save() {
        const form = document.getElementById('translation-form');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const data = {
            languageId: document.getElementById('translation-languageid').value,
            textId: document.getElementById('translation-textid').value,
            resourceId: document.getElementById('translation-resourceid').value,
            destination: document.getElementById('translation-destination').value
        };

        fetch(`${API_BASE}/translations`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(res => {
                if (res.ok) {
                    this.modal.hide();
                    this.load();
                } else {
                    alert('Error saving translation');
                }
            })
            .catch(err => {
                console.error(err);
                alert('Error saving translation');
            });
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    router.init();
});