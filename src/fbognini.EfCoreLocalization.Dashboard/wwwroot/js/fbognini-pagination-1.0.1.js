var getFullSearchUrlFromDatatablesServerSide = function (url, data) {


    console.log(data);

    var sorts = [];
    for (var i = 0; i < data.order?.length; i++) {

        var column = data.order[i].column;
        var dir = data.order[i].dir;

        sorts.push({ dir: dir, by: data.columns[column].data });
    }

    data.sort = sorts;

    return getFullSearchUrlFromDatatablesInner(url, data);
}

var getFullSearchUrlFromDatatablesClientSide = function (url, settings, data, parameters) {


    var sorts = [];
    for (var i = 0; i < data.order.length; i++) {

        var column = data.order[i][0];
        var dir = data.order[i][1];

        sorts.push({ dir: dir, by: settings.aoColumns[column].data });
    }

    data.sort = sorts;

    return getFullSearchUrlFromDatatablesInner(url, data, parameters);
}

var getFullSearchUrlFromDatatablesInner = function (url, data, parameters) {

    if (url[0] == '/') {
        url = location.origin + url
    }

    if (!parameters) {
        parameters = {
            q: true,
            length: true,
            start: true,
            sort: true,
        };
    }

    var newUrl = new URL(url);

    if (data.search != undefined) {

        newUrl.searchParams.delete('q');
        if (parameters.q) {
            newUrl.searchParams.set('q', data.search.value ?? "");
        }

        newUrl.searchParams.delete('length');
        if (parameters.length && data.length != -1) {
            newUrl.searchParams.set('length', data.length);
        }

        newUrl.searchParams.delete('start');
        if (parameters.start && data.length != -1) {
            newUrl.searchParams.set('start', data.start);
        }

        newUrl.searchParams.delete('sort-by');
        newUrl.searchParams.delete('sort-dir');

        if (parameters.sort) {

            for (var i = 0; i < data.sort.length; i++) {

                newUrl.searchParams.append('sort-by', data.sort[i].by);
                newUrl.searchParams.append('sort-dir', data.sort[i].dir);

            }
        }
    }

    return newUrl.toString();
}

var paginatedResponseToDatatables = function (draw, response) {
    if (response.pagination != null) {

        return {
            draw: draw,
            atLeast: response.pagination.atLeast,
            recordsTotal: response.pagination.total,
            recordsFiltered: response.pagination.partialTotal ?? response.pagination.total,
            data: response.items
        };
    }

    return {
        draw: draw,
        recordsTotal: response.items.length,
        recordsFiltered: response.items.length,
        data: response.items
    };
}

/**
 * 
 * @param {string | function} baseUrl
 * @returns
 */
var fullSearchDatatables = function (baseUrl) {

    return function (data, callback, settings) {

        const _baseUrl = typeof baseUrl === "function"
            ? baseUrl()
            : baseUrl;

        var url = getFullSearchUrlFromDatatablesServerSide(_baseUrl, data);

        $.ajax({
            url: url,
            type: "GET",
            contentType: 'application/json',
            success: function (response) {
                callback(paginatedResponseToDatatables(data.draw, response));
            }
        });
    }

}

var fullSearchDatatablesCallbackWithLocation = function (parameters) {
    return function (settings) {

        if (settings.json.atLeast) {

            var info = this.api().page.info();

            var total, template;

            if (settings.oLanguage.infoAtLeast) {

                template = settings.oLanguage.infoAtLeast;
                total = info.recordsTotal;
            }
            else {

                template = settings.oLanguage.info;
                total = "+" + info.recordsTotal;
            }

            var translatedInfo = template
                .replace('_START_', info.start + 1)
                .replace('_END_', info.end)
                .replace('_TOTAL_', total);

            $('.dataTables_info').html(translatedInfo);
        }

        var newUrl = getFullSearchUrlFromDatatablesClientSide(window.location.href, settings, settings.oSavedState, parameters);
        window.history.replaceState(null, '', newUrl);
    }
}


var overrideDatatableFullSearchOptionsWithLocation = function (options, parameters) {

    var urlParams = new URL(window.location.href).searchParams;

    if (!parameters) {
        parameters = {
            q: true,
            length: true,
            start: true,
            sort: true,
        };
    }

    var q = urlParams.get('q');
    var length = urlParams.get('length');
    var start = urlParams.get('start');

    var sortsDir = urlParams.getAll('sort-dir');
    var sortsBy = urlParams.getAll('sort-by');
    var sorts = [];

    var sortsLength = sortsDir.length < sortsBy.length ? sortsDir.length : sortsBy.length;
    for (var i = 0; i < sortsLength; i++) {
        sorts.push({ dir: sortsDir[i], by: sortsBy[i] })
    }

    var data = {
        search: {
            value: q
        },
        length: length,
        start: start,
        sort: sorts
    };

    if (parameters.sort && data.sort) {

        var order = [];

        for (var i = 0; i < data.sort.length; i++) {

            var index = options.columns.findIndex(function (item) {
                return item.data === data.sort[i].by;
            });

            if (index != -1) {
                order.push([index, data.sort[i].dir]);
            }
        }

        if (order.length > 0) {
            options.order = order;
        }
    }

    if (parameters.start && data.start && !isNaN(data.start)) {
        options.displayStart = parseInt(data.start);
    }

    if (parameters.length && data.length && !isNaN(data.length)) {
        options.pageLength = parseInt(data.length);
    }

    if (parameters.search && data.search && data.search.value) {
        options.search = { search: data.search.value };
    }
}

var getFullSearchQueryFromSelect2 = function (params) {

    var query = {
        q: params.term,
        length: 10,
        page: params.page || 1
    };

    return query;
}

var fullSearchSelect2 = function (baseUrl, options) {

    if (!options) {
        options = {};
    }

    return {
        url: baseUrl,
        traditional: true,
        data: function (params) {
            var query = getFullSearchQueryFromSelect2(params);

            // Allow additional query parameters
            if (options.queryParams) {
                if (typeof options.queryParams === 'function') {
                    // If it's a function, call it with params to get dynamic values
                    var additionalParams = options.queryParams(params);
                    if (additionalParams) {
                        Object.assign(query, additionalParams);
                    }
                } else if (typeof options.queryParams === 'object') {
                    // If it's an object, merge it directly
                    Object.assign(query, options.queryParams);
                }
            }

            return query;
        },
        processResults: function (data, params) {

            params.page = params.page || 1;

            var items = options.mapping
                ? data.items.map((element) => { return options.mapping(element); })
                : data.items;

            if (options.emptyOption) {
                items.unshift({ "id": "", "text": "" });
            }

            return {
                results: items,
                pagination: {
                    more: (params.page * 10) < data.pagination.total
                }
            };
        },
        cache: true
    }
}

var restCreateEditorDatatablesInner = function (url, d, success, error, options) {

    if (!options) {
        options = {};
    }

    if (options.before) {
        options.before();
    }

    var data = Object.values(d.data)[0];
    if (options.mapping) {
        data = options.mapping(data);
    }

    $.ajax({
        type: 'POST',
        url: url,
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (json) {
            if (options.after) {
                options.after();
            }

            success({ data: [json] });
        },
        error: function (xhr, errorMessage, thrown) {
            if (options.after) {
                options.after();
            }

            console.log(xhr);

            if (xhr.responseJSON.message) {
                success({
                    error: xhr.responseJSON.message
                });
            }
            else {
                error({});
            }
        }
    });
}

var restEditEditorDatatablesInner = function (url, d, success, error, options) {

    if (!options) {
        options = {};
    }

    if (options.before) {
        options.before();
    }

    var data = Object.values(d.data)[0];
    if (options.mapping) {
        data = options.mapping(data);
    }

    $.ajax({
        type: 'PUT',
        url: url,
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (json) {
            if (options.after) {
                options.after();
            }

            success({ data: [json] });
        },
        error: function (xhr, errorMessage, thrown) {
            if (options.after) {
                options.after();
            }

            console.log(xhr);

            if (xhr.responseJSON.message) {
                success({
                    error: xhr.responseJSON.message
                });
            }
            else {
                error({});
            }
        }
    });
}

var restRemoveEditorDatatablesInner = function (url, d, success, error, options) {

    if (!options) {
        options = {};
    }

    if (options.before) {
        options.before();
    }

    $.ajax({
        type: 'DELETE',
        url: url,
        success: function (json) {
            if (options.after) {
                options.after();
            }
            success({});
        },
        error: function (xhr, errorMessage, thrown) {
            if (options.after) {
                options.after();
            }
            error({});
        }
    });
}

var restCreateEditorDatatables = function (url, options) {
    return function (_, _, d, success, error) {

        restCreateEditorDatatablesInner(url, d, success, error, options);
    };
}

var restEditEditorDatatables = function (url, options) {
    return function (_, _, d, success, error) {

        var id = Object.keys(d.data)[0];
        restEditEditorDatatablesInner(url + "/" + id, d, success, error, options);
    };
}

var restRemoveEditorDatatables = function (url, options) {
    return function (_, _, d, success, error) {

        var id = Object.keys(d.data)[0];
        restRemoveEditorDatatablesInner(url + "/" + id, d, success, error, options);
    };
}

var restCrudEditorDatatables = function (url, options) {
    return {
        create: restCreateEditorDatatables(url, options),
        edit: restEditEditorDatatables(url, options),
        remove: restRemoveEditorDatatables(url, options)
    }
}