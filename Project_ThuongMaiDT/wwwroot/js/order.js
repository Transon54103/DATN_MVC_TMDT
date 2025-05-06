var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else if (url.includes("completed")) {
        loadDataTable("completed");
    }
    else if (url.includes("pending")) {
        loadDataTable("pending");
    }
    else if (url.includes("approved")) {
        loadDataTable("approved");
    }
    else {
        loadDataTable("all");
    }
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + status },
        "order": [[0, "desc"]], // Sort ID descending
        "language": {
            "emptyTable": "Không có dữ liệu",
            "lengthMenu": "Hiển thị _MENU_ mục",
            "info": "Hiển thị _START_ đến _END_ của _TOTAL_ mục",
            "infoEmpty": "Hiển thị 0 đến 0 của 0 mục",
            "infoFiltered": "(lọc từ _MAX_ mục)",
            "loadingRecords": "Đang tải...",
            "processing": "Đang xử lý...",
            "search": "Tìm kiếm:",
            "zeroRecords": "Không tìm thấy kết quả phù hợp",
            "paginate": {
                "first": "Đầu",
                "last": "Cuối",
                "next": "Sau",
                "previous": "Trước"
            }
        },
        "columns": [
            {
                data: 'id',
                "width": "10%",
                "render": function (data) {
                    return 'DH2025' + data.toString().padStart(4, '0'); // Format ID as DH2025xxxx
                },
                title: "Mã đơn hàng"
            },
            { data: 'name', "width": "20%", title: "Tên người đặt" },
            { data: 'phoneNumber', "width": "20%", title: "Số điện thoại" },
            {
                data: null,
                "render": function (data) {
                    if (data.applicationUser && data.applicationUser.email) {
                        return data.applicationUser.email;
                    } else if (data.guestEmail) {
                        return data.guestEmail;
                    } else {
                        return "Không có email";
                    }
                },
                "width": "15%",
                "title": "Email"
            },
            { data: 'orderStatus', "width": "10%", title: "Trạng thái" },
            {
                data: 'orderTotal',
                render: function (data) {
                    return parseFloat(data).toLocaleString('vi-VN') + ' VNĐ';
                },
                "width": "10%",
                title: "Tổng đơn"
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                        <a href="/admin/order/details?orderId=${data}" class="btn btn-primary mx-2" title="Xem chi tiết">
                            <i class="bi bi-pencil-square"></i>
                        </a>               
                    </div>`;
                },
                "width": "10%",
                "title": "Hành động"
            }
        ]
    });
}