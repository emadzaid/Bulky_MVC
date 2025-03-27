var tbData;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadTableData("inprocess");
    }
    else {
        if (url.includes("pending")) {
            loadTableData("pending");
        }
        else {
            if (url.includes("approved")) {
                loadTableData("approved");
            }
            else {
                if (url.includes("completed")) {
                    loadTableData("completed");
                }
                else {
                    loadTableData("all")
                }
            }
        }
    }
});

function loadTableData(status) {

        tbData = $('#tbData').DataTable({
            ajax: {
                url: '/admin/order/getall?status=' + status
        },
        "columns": 
            [
                { data: 'id', "width": "5%" },
                { data: 'name', "width": "10%" },
                { data: 'applicationUser.phoneNumber', "width": "10%" },
                { data: 'applicationUser.email', "width": "15%" },  
                { data: 'orderStatus', "width": "10%" },
                { data: 'orderTotal', "width": "10%" },
                { data: null, "width": "5%" },
      
            ],

        "columnDefs": [
            {
                "targets": -1,
                "render": function (data, type, row) {
                    var editBtn = '<a href="/admin/order/details?orderId=' + row.id + '" class="btn btn-primary btn-sm"> <i class="bi bi-pen"></i> </a>';
                    return editBtn;
             }
            },

        ]
    });
}
