var tbData;

$(document).ready(function () {
    loadTableData();
});

function loadTableData() {
        tbData = $('#tbData').DataTable({
        ajax: {
            url: '/admin/product/getall'
        },
        "columns": 
            [
                { data: 'title', "width": "25%" },
                { data: 'isbn', "width": "15%" },
                { data: 'listPrice', "width": "10%" },  
                { data: 'author', "width": "20%" },
                { data: 'category.name', "width": "15%" },
                { data: null, "width": "7.5%" },
                { data: null, "width": "7.5%" },

            ],

        "columnDefs": [
            {
                "targets": -2,
                "render": function (data, type, row) {
                    var editBtn = '<a href="/admin/product/upsert/' + row.id + '" class="btn btn-primary btn-sm"> Edit </a>';
                    return editBtn;
             }
            },

            {
                "targets": -1,
                "render": function (data, type, row) {
                    var dltBtn = '<a onClick="Delete(\'/admin/product/delete/' + row.id + '\')" class="btn btn-danger btn-sm"> Delete </a>';
                    return dltBtn;
                }
            }
        ]
    });
}

function Delete(url)
{
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
                    tbData.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    });
}
