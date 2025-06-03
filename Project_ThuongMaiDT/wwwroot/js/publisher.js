var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/publisher/getall' },  // Endpoint lấy dữ liệu của nhà xuất bản
        "columns": [
            { data: 'name', "width": "30%" },  // Hiển thị tên của nhà xuất bản
            {
                data: 'imageUrl',
                "render": function (data, type, row) {
                    return `<img src="${data}" alt="Publisher Image" width="50" />`;
                },
                "width": "20%"  // Hiển thị hình ảnh của nhà xuất bản
            },
            { data: 'description', "width": "30%" },  // Hiển thị mô tả nhà xuất bản
            {
                data: 'id',
                "render": function (data, type, row) {
                    return `<div class="w-100 btn-group" role="group">
                        <a href="/admin/publisher/upsert?id=${data}" class="btn btn-primary mx-1">
                            <i class="bi bi-pencil-square"></i> Sửa
                        </a>
                        <button onClick="Delete('/admin/publisher/delete/${data}')" class="btn btn-danger mx-1">
                            <i class="bi bi-trash-fill"></i> Xóa
                        </button>
                    </div>`;
                },
                "width": "20%"  // Thêm các nút hành động (Edit, Delete)
            }
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
