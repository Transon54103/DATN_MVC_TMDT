var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/author/getall' },
        "columns": [
            { data: 'name', "width": "25%" },
            {
                data: 'imageUrl',
                "render": function (data) {
                    return data ? `<img src="${data}" width="50px" height="50px" style="border-radius:5px;"/>` : "No Image";
                },
                "width": "40%"
            },
            {
                data: 'authorId',
                "render": function (data, type, row) {
                    let approveClass = row.isActive ? "btn-danger" : "btn-success";
                    let approveText = row.isActive ? "Hủy duyệt" : "Duyệt";

                    return `<div class="w-100 btn-group" role="group">
                        <a href="/admin/author/upsert?id=${data}" class="btn btn-primary mx-1">
                            <i class="bi bi-pencil-square"></i> Sửa
                        </a>
                        <button onClick="Delete('/admin/author/delete/${data}')" class="btn btn-danger mx-1">
                            <i class="bi bi-trash-fill"></i> Xóa
                        </button>
                    </div>`;
                },
                "width": "30%"
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
                    toastr.success("Đã xóa thành công!");
                }
            })
        }
    });
}


