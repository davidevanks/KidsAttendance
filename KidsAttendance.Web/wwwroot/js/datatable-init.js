$(function () {
    if (!$.fn.DataTable) {
        return;
    }

    var spanish = {
        processing: "Procesando...",
        search: "",
        searchPlaceholder: "Buscar...",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
        infoEmpty: "Mostrando 0 a 0 de 0 registros",
        infoFiltered: "(filtrado de _MAX_ registros totales)",
        loadingRecords: "Cargando...",
        zeroRecords: "No se encontraron resultados",
        emptyTable: "No hay datos disponibles",
        paginate: {
            first: "«",
            previous: "‹",
            next: "›",
            last: "»"
        },
        aria: {
            sortAscending: ": activar para ordenar ascendente",
            sortDescending: ": activar para ordenar descendente"
        }
    };

    $.extend(true, $.fn.dataTable.defaults, {
        language: spanish,
        responsive: {
            details: {
                type: "column",
                target: "tr"
            }
        },
        pageLength: 25,
        lengthChange: false,
        searching: true,
        ordering: true,
        info: true,
        pagingType: "simple_numbers",
        autoWidth: false,
        dom: "<'row'<'col-12 mb-2'f>>" +
             "<'row'<'col-12'tr>>" +
             "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>"
    });

    $(".js-datatable").each(function () {
        var $table = $(this);
        var options = {};

        var override = $table.data("datatable-options");
        if (override) {
            options = override;
        }

        $table.DataTable(options);
    });
});
