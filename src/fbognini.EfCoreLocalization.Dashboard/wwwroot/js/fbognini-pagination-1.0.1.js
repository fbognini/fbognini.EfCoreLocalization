var getFullSearchUrlFromDatatables = function (baseUrl, data) {

    if (baseUrl[0] == '/') {
        baseUrl = location.origin + baseUrl
    }
    var url = new URL(baseUrl);

    if (data.search != undefined) {
        url.searchParams.set('q', data.search.value);
        if (data.length == -1) {
            url.searchParams.set('length', "");
            url.searchParams.set('start', "");
        }
        else {
            url.searchParams.set('length', data.length);
            url.searchParams.set('start', data.start);
        }

        for (var i = 0; i < data.order.length; i++) {
            url.searchParams.append('sort-by', data.columns[data.order[i].column].data);
            url.searchParams.append('sort-dir', data.order[i].dir);
        }
    }

    return url.toString();
}

var paginatedResponseToDatatables = function (draw, response) {
    if (response.pagination != null) {
        return {
            draw: draw,
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

var fullSearchDatatables = function (baseUrl) {

    return function (data, callback, settings) {
        var url = getFullSearchUrlFromDatatables(baseUrl, data);

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
        data: getFullSearchQueryFromSelect2,
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
    return function(_, _, d, success, error) {

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