//"use strict";
//var connection = new signalR.HubConnectionBuilder().withUrl("/getpayment").build();
//connection.on("UpdateData", function (data) {
//    console.log(data);
//    console.log("session", sessionStorage.getItem("tels"));
//    if (sessionStorage.getItem("tels") != null || sessionStorage.getItem("tels") != "") {
//        if (data == sessionStorage.getItem("tels")) {
//            $.post("/Login/UpdateData", { tel: sessionStorage.getItem("tels") }).then(function (data) {
//                console.log(data);
//                setTimeout(() => {

//                }, 3000);
//            }).catch(() => {
//                console.log("error.")
//            })
//        }
//    }
//});

//connection.start().then(function () {
//    console.log("Connect...");


//}).catch(function (err) {
//    return console.error(err.toString());
//});



//connection.start().then(function () {
//    console.log("Connect...");


//}).catch(function (err) {
//    return console.error(err.toString());
//});


//setTimeout(() => {

//    connection.invoke("SendMessage").catch(function (err) {
//        return console.error(err.toString());
//    });
//}, 100);


"use strict";

// เพิ่ม .withAutomaticReconnect() ตรงนี้
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/getpayment")
    .withAutomaticReconnect() // <-- เพิ่มบรรทัดนี้
    .build();

connection.on("UpdateData", function (data) {
    console.log(data);
    console.log("session", sessionStorage.getItem("tels"));
    if (sessionStorage.getItem("tels") != null || sessionStorage.getItem("tels") != "") {
        if (data == sessionStorage.getItem("tels")) {
            $.post("/Login/UpdateData", { tel: sessionStorage.getItem("tels") }).then(function (data) {
                console.log(data);
                setTimeout(() => {

                }, 3000);
            }).catch(() => {
                console.log("error.")
            })
        }
    }
});

// เรียก start แค่ครั้งเดียว
connection.start().then(function () {
    console.log("Connect...");
}).catch(function (err) {
    return console.error(err.toString());
});

connection.onreconnecting((error) => {
    console.warn("กำลัง reconnect...", error);
});

connection.onreconnected((connectionId) => {
    console.log("เชื่อมต่อใหม่แล้ว", connectionId);
});

connection.onclose((error) => {
    console.error("การเชื่อมต่อถูกปิด", error);
});