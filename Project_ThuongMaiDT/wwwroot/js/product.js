var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/product/getall' },
        "columns": [
            { data: 'title', "width": "20%" },
            { data: 'quantity', "width": "15%" },
            { data: 'listPrice', "width": "10%" },
            { data: 'authors.name', "width": "15%" },
            { data: 'category.name', "width": "10%" },
            {
                data: 'isActive',
                "render": function (data, type, row) {
                    let btnClass = data ? "btn-success" : "btn-secondary";
                    let btnText = data ? "Đang bán" : "Không bán";
                    return `<button class="btn ${btnClass}" onclick="UpdateIsActive(${row.id})">${btnText}</button>`;
                },
                "width": "10%"
            },
            {
                data: 'id',
                "render": function (data, type, row) {
                    let approveClass = row.isActive ? "btn-danger" : "btn-success";
                    let approveText = row.isActive ? "Hủy duyệt" : "Duyệt";

                    return `<div class="w-100 btn-group" role="group">
                        <a href="/admin/product/upsert?id=${data}" class="btn btn-primary mx-1">
                            <i class="bi bi-pencil-square"></i> Edit
                        </a>
                        <button class="btn ${approveClass} mx-1" onclick="UpdateIsActive(${data})">
                            <i class="bi bi-check-circle"></i> ${approveText}
                        </button>
                        <button onClick="Delete('/admin/product/delete/${data}')" class="btn btn-danger mx-1">
                            <i class="bi bi-trash-fill"></i> Delete
                        </button>
                    </div>`;
                },
                "width": "25%"
            },
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Bạn có chắc chắn?",
        text: "Bạn không thể hoàn tác sau khi xóa!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Có, xóa nó!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    });
}

function UpdateIsActive(id) {
    $.post('/admin/product/updateisactive/' + id, function (data) {
        if (data.success) {
            dataTable.ajax.reload();
            toastr.success(data.message);
        } else {
            toastr.error(data.message);
        }
    });
}
