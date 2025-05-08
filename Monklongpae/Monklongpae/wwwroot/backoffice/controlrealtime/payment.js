"use strict";
var connection = new signalR.HubConnectionBuilder().withUrl("/getpayment").build();
connection.on("Payment", function (data) {
    var htmls = "";
    $("#countpayment").html(data.length);
    if (data.length > 0) {
        data.map(function (data) {
            htmls += '<a href="/Backoffice/Cert_market" class="dropdown-item"><h6 class="fw-normal mb-0">รออนุมัติสั่งซื้อ: ' + data.name +'</h6><small>'+data.time+'</small></a>';
        });
        
    } else {
        htmls = '<a href="javascript:void(0)" class="dropdown-item"><h6 class="fw-normal mb-0">ไม่มีผู้สั่งซื้อ</h6><small>ไม่มีข้อมูล</small></a>';
    }
    $("#messagepayment").html(htmls);
});



connection.start().then(function () {
    console.log("Connect...");
    setInterval(function () {
        connection.invoke("SendMessage").catch(function (err) {
            return console.error(err.toString());
        });
    },3000)
    
}).catch(function (err) {
    return console.error(err.toString());
});

$("#appect_payment").click(function () {
    console.log("send to client", $("#IdPayment").val());
    $.post("/Backoffice/Cert_market", { IdPayment: $("#IdPayment").val() }).then(function (data) {
        connection.invoke("UpdateData", $("#IdPayment").val()).catch(function (err) {
            return console.error(err.toString());
        });
        window.location.reload();
    }).catch(() => {
        console.log("error.")
    });
    
});

//setTimeout(() => {

//    connection.invoke("SendMessage").catch(function (err) {
//        return console.error(err.toString());
//    });
//}, 100);