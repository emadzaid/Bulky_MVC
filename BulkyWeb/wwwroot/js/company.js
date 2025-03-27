var companyTb;

$(document).ready(function () {
    loadCompanyTable();
});
function loadCompanyTable() {

    companyTb = $('#companyTb').DataTable({
        ajax: {
            url: '/admin/company/getall'
        },

        "columns": [
            { data: 'name', "width": "20%" },
            { data: 'streetAddress', "width": "20%" },
            { data: 'city', "width": "10%" },
            { data: 'state', "width": "10%" },
            { data: 'postalCode', "width": "10%" },
            { data: 'phoneNumber', "width": "10%" },
            { data: null, "width": "7.5%" },
            { data: null, "width": "7.5%" },

        ],

        "columnDefs": [
            {
                "targets": -2,
                "render": function (data, type, row) {
                    var editBtn = '<a href="/admin/company/upsert/' + row.id + '" class="btn btn-primary btn-sm"> Edit </a>';
                    return editBtn;
                }
            },

            {
                "targets": -1,
                "render": function (data, type, row) {
                    var dltBtn = '<a onClick="Delete(\'/admin/company/delete/' + row.id + '\')" class="btn btn-danger btn-sm"> Delete </a>';
                    return dltBtn;
                }
            }
        ]
    });
}
function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    companyTb.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    });
}
