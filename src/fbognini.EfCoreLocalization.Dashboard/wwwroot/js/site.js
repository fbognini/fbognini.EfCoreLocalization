// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    $.extend(true, $.fn.dataTable.defaults, {
        language: {
            url: "/_content/fbognini.EfCoreLocalization.Dashboard/lib/Datatables/it-IT.json"
        },
    });
});


var renderDatatableDateTime = function (from, to, locale) {
    // Argument shifting
    if (arguments.length === 1) {
        locale = 'en';
        to = from;
        from = 'YYYY-MM-DD';
    }
    else if (arguments.length === 2) {
        locale = 'en';
    }

    return function (d, type, row) {
        if (!d) {
            return type === 'sort' || type === 'type' ? 0 : d;
        }

        var m = window.moment(d, from, locale, true);
        for (var i = 1; i < from.length && !m.isValid(); i++) {
            m = window.moment(d, from, locale, true);
        }

        //if (!m.isValid()) {
        //    m = window.moment(d, "YYYY-MM-DDTHH:mm:ss.SSSSSSSZ", locale, true);
        //}

        // Order and type get a number value from Moment, everything else
        // sees the rendered value
        return m.format(type === 'sort' || type === 'type' ? 'x' : to);
    };
};