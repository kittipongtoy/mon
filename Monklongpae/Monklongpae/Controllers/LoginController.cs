using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Models;
using Newtonsoft.Json.Linq;
using NToastNotify;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static Monklongpae.Models.oparetion;

namespace Monklongpae.Controllers
{
    public class LoginController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;
        public readonly MonklongpaeContext db = new MonklongpaeContext();
        private readonly IToastNotification _toastNotification;
        public LoginController(IWebHostEnvironment environment, IToastNotification toastNotification)
        {
            _hostingEnvironment = environment;
            _toastNotification = toastNotification;
        }
        public IActionResult Login()
        {
            var mac = GetIPAddress();
            return View(new { mac });
        }

        public IActionResult Register()
        {
            return View();
        }

        public async Task<IActionResult> check_tel(string data)
        {
            var dbs = await db.User.FirstOrDefaultAsync(x => x.Tel == data);
            if (dbs != null)
            {
                return Json("success");
            }
            return Json("error");
        }

        public string generate_token(string text)
        {
            StringBuilder token = new StringBuilder();
            using (SHA256 sha256hash = SHA256.Create())
            {
                byte[] bytes = sha256hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                for (int i = 0; i < bytes.Length; i++)
                {
                    token.Append(bytes[i].ToString("x2"));
                }
            }
            return token.ToString();
        }

        public async Task<IActionResult> Account_me()
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel == null) return Redirect("/Home/Index");
            if (tel != null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                ViewBag.ids = xx.IdUser;
                ViewBag.fir = xx.FirstName;
                ViewBag.sur = xx.SurName;
                ViewBag.address = xx.Address;
                ViewBag.tel = xx.Tel;
                ViewBag.image = xx.ImagePath;
                var status = "";
                if (xx.IdRole == 1)
                {
                    status = "ฟรี";
                }
                else if (xx.IdRole == 2)
                {
                    status = "VIP";
                }
                else if (xx.IdRole == 3)
                {
                    status = "แอดมิน";
                }
                ViewBag.role = status;
            }
            return View();
        }

        public async Task<IActionResult> UpdateData(string tel)
        {
            var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel && x.Isactive == true);
            if (xx != null)
            {
                var limit_date = 0;
                var user = await db.User.FindAsync(xx.IdUser);
                if (user!.PacketDateLimit < DateTime.Now)
                {
                    user.IdRole = 1;
                }
                var tokens = generate_token(xx.Tel + DateTime.Now.ToString());
                user.OnlineStatus = true;
                user.Token = tokens;
                user.WorkDate = DateTime.Now;
                db.SaveChanges();
                if (user.PacketDateLimit != null)
                {
                    var nows = DateTime.Now;
                    var limit = xx.PacketDateLimit - nows;
                    limit_date = limit.Value.Days;
                    limit_date = limit_date < 0 ? 0 : limit_date;
                }
                var role =await db.Role.FirstOrDefaultAsync(x => x.IdRole == xx.IdRole);
                HttpContext.Session.SetString("Firstname", xx.FirstName);
                HttpContext.Session.SetString("Surnname", xx.SurName);
                HttpContext.Session.SetString("Tel", xx.Tel);
                HttpContext.Session.SetString("role", role.Name);
                HttpContext.Session.SetString("image", xx.ImagePath);
                HttpContext.Session.SetInt32("package", limit_date);
                var cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddHours(1);
                cookieOptions.Path = "/";
                Response.Cookies.Append("tokensMon", tokens, cookieOptions);
                _toastNotification.AddSuccessToastMessage("สามารถรับชมวิดีโอได้ รอ 3 วิ หรือกดรีเฟรช");
                return Ok("success.");
            }
            _toastNotification.AddErrorToastMessage("ไม่พบข้อมูล");
            return Ok("Error.");
        }

        [HttpPost]
        public async Task<IActionResult> Login(User data)
        {
            var user = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel && x.Isactive == true);

            if (user != null)
            {
                var now = DateTime.Now;

                // ถ้ามี Token และใช้งานภายใน 15 นาทีล่าสุด → ถือว่า login ซ้ำ
                if (user.IsOnline.GetValueOrDefault() && !string.IsNullOrEmpty(user.Token) &&
                user.WorkDate.HasValue &&
                (now - user.WorkDate.Value).TotalMinutes < 15)
                {
                    _toastNotification.AddErrorToastMessage("บัญชีนี้กำลังใช้งานบนเครื่องอื่น");
                    return Redirect("/Login/Login");
                }

                // ปรับปรุงสถานะ
                user.Token = generate_token(user.Tel + now.ToString());
                user.IsOnline = true;
                user.WorkDate = now;
                db.SaveChanges();

                // คำนวณวันเหลือ
                int limit_date = 0;
                if (user.PacketDateLimit.HasValue)
                {
                    var daysLeft = (user.PacketDateLimit.Value - DateTime.Now).Days;
                    limit_date = daysLeft < 0 ? 0 : daysLeft;
                }

                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == user.IdRole);

                // เก็บลง session และ cookie
                HttpContext.Session.SetString("Firstname", user.FirstName ?? "");
                HttpContext.Session.SetString("Surnname", user.SurName ?? "");
                HttpContext.Session.SetString("Tel", user.Tel);
                HttpContext.Session.SetString("role", role?.Name ?? "User");
                HttpContext.Session.SetString("image", user.ImagePath ?? "");
                HttpContext.Session.SetInt32("package", limit_date);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddHours(1),
                    Path = "/"
                };
                Response.Cookies.Append("tokensMon", user.Token, cookieOptions);

                _toastNotification.AddSuccessToastMessage("ล็อกอินสำเร็จ");
                return Redirect("/Home/Index");
            }
            else
            {
                // สมัครใหม่ถ้ายังไม่มีผู้ใช้
                var existing = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel);
                if (existing != null)
                {
                    _toastNotification.AddErrorToastMessage("ไม่ใช่เครื่องที่สมัคร");
                    return View();
                }

                var tokens = generate_token(data.Tel + DateTime.Now.ToString());
                data.FirstName = "";
                data.SurName = "";
                data.IdRole = 1;
                data.Isactive = true;
                data.CreateDate = DateTime.Now;
                data.ImagePath = "/img/logo_web.jpg";
                data.OnlineStatus = true;
                data.WorkDate = DateTime.Now;
                data.Token = tokens;
                data.IsOnline = true;

                db.User.Add(data);
                db.SaveChanges();

                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == data.IdRole);
                HttpContext.Session.SetString("Firstname", data.FirstName);
                HttpContext.Session.SetString("Surnname", data.SurName);
                HttpContext.Session.SetString("Tel", data.Tel);
                HttpContext.Session.SetString("role", role?.Name ?? "User");
                HttpContext.Session.SetString("image", data.ImagePath);
                HttpContext.Session.SetInt32("package", 0);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddHours(1),
                    Path = "/"
                };
                Response.Cookies.Append("tokensMon", tokens, cookieOptions);

                _toastNotification.AddSuccessToastMessage("สมัครสมาชิกสำเร็จ");
                return Redirect("/Home/Index");
            }




            ////data.MacAddress = GetIPAddress();
            //var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel && x.Isactive == true);
            //if (xx != null)
            //{
            //    if (xx.MacAddress == null || xx.MacAddress == "")
            //    {
            //        xx.MacAddress = data.MacAddress;
            //        db.SaveChanges();
            //    }
            //    if (xx.MacAddress != data.MacAddress)
            //    {
            //        _toastNotification.AddErrorToastMessage("ไม่ใช่เครื่องที่สมัคร");
            //        return Redirect("/Login/Login");
            //    }
            //    if (xx.IdRole == 3)
            //    {
            //        var limit_date = 0;

            //        if (xx.PacketDateLimit != null)
            //        {
            //            var nows = DateTime.Now;
            //            var limit = xx.PacketDateLimit - nows;
            //            limit_date = limit.Value.Days;
            //        }
            //        var user = await db.User.FindAsync(xx.IdUser);
            //        var tokens = generate_token(xx.Tel + DateTime.Now.ToString());
            //        user.OnlineStatus = true;
            //        user.Token = tokens;
            //        db.SaveChanges();

            //        var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == xx.IdRole);
            //        HttpContext.Session.SetString("Firstname", xx.FirstName);
            //        HttpContext.Session.SetString("Surnname", xx.SurName);
            //        HttpContext.Session.SetString("Tel", xx.Tel);
            //        HttpContext.Session.SetString("role", role.Name);
            //        HttpContext.Session.SetString("image", xx.ImagePath);
            //        HttpContext.Session.SetInt32("package", limit_date);
            //        var cookieOptions = new CookieOptions();
            //        cookieOptions.Expires = DateTime.Now.AddHours(1);
            //        cookieOptions.Path = "/";
            //        Response.Cookies.Append("tokensMon", tokens, cookieOptions);
            //        _toastNotification.AddSuccessToastMessage("ล็อกอินสำเร็จ");

            //        return Redirect("/Home/Index");
            //    }
            //    if (data.IdRole != 3)
            //    {
            //        var limit_date = 0;
            //        var user = await db.User.FindAsync(xx.IdUser);
            //        if (user.PacketDateLimit < DateTime.Now)
            //        {
            //            user.IdRole = 1;
            //        }
            //        var tokens = generate_token(xx.Tel + DateTime.Now.ToString());
            //        user.OnlineStatus = true;
            //        user.Token = tokens;
            //        user.WorkDate = DateTime.Now;
            //        db.Entry(user).State = EntityState.Modified;
            //        db.SaveChanges();

            //        if (user.PacketDateLimit != null)
            //        {
            //            var nows = DateTime.Now;
            //            var limit = xx.PacketDateLimit - nows;
            //            limit_date = limit.Value.Days;
            //            limit_date = limit_date < 0 ? 0 : limit_date;
            //        }
            //        var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == xx.IdRole);
            //        HttpContext.Session.SetString("Firstname", xx.FirstName);
            //        HttpContext.Session.SetString("Surnname", xx.SurName);
            //        HttpContext.Session.SetString("Tel", xx.Tel);
            //        HttpContext.Session.SetString("role", role.Name);
            //        HttpContext.Session.SetString("image", xx.ImagePath);
            //        HttpContext.Session.SetInt32("package", limit_date);
            //        var cookieOptions = new CookieOptions();
            //        cookieOptions.Expires = DateTime.Now.AddHours(1);
            //        cookieOptions.Path = "/";
            //        Response.Cookies.Append("tokensMon", tokens, cookieOptions);
            //        _toastNotification.AddSuccessToastMessage("ล็อกอินสำเร็จ");

            //        return Redirect("/Home/Index");
            //    }
            //    else
            //    {

            //    }
            //}
            //else
            //{
            //    var dad5 = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel);
            //    if (dad5 == null)
            //    {
            //        data.FirstName = "";
            //        data.SurName = "";
            //        data.IdRole = 1;
            //        data.Isactive = true;
            //        data.CreateDate = DateTime.Now;
            //        data.ImagePath = "/img/logo_web.jpg";
            //        data.OnlineStatus = false;
            //        data.WorkDate = DateTime.Now;
            //        data.OnlineStatus = true;
            //        var tokens = generate_token(data.Tel + DateTime.Now.ToString());
            //        data.Token = tokens;
            //        db.User.Add(data);
            //        db.SaveChanges();

            //        var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == data.IdRole);
            //        HttpContext.Session.SetString("Firstname", data.FirstName);
            //        HttpContext.Session.SetString("Surnname", data.SurName);
            //        HttpContext.Session.SetString("Tel", data.Tel);
            //        HttpContext.Session.SetString("role", role.Name);
            //        HttpContext.Session.SetString("image", data.ImagePath);
            //        HttpContext.Session.SetInt32("package", 0);
            //        var cookieOptions = new CookieOptions();
            //        cookieOptions.Expires = DateTime.Now.AddHours(1);
            //        cookieOptions.Path = "/";
            //        Response.Cookies.Append("tokensMon", tokens, cookieOptions);
            //        _toastNotification.AddSuccessToastMessage("สมัครสมาชิกสำเร็จ");

            //        return Redirect("/Home/Index");
            //    }
            //    else
            //    {
            //        _toastNotification.AddErrorToastMessage("ไม่ใช่เครื่องที่สมัคร");
            //        return View();
            //    }
            //}
            //_toastNotification.AddErrorToastMessage("ไม่ใช่เครื่องที่สมัคร");
            //return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User data)
        {
            var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel);
            if (xx == null)
            {
                data.FirstName = "";
                data.SurName = "";
                data.IdRole = 1;
                data.Isactive = true;
                data.CreateDate = DateTime.Now;
                data.ImagePath = "/img/logo_web.jpg";
                data.OnlineStatus = false;
                data.WorkDate = DateTime.Now;
                db.User.Add(data);

                db.SaveChanges();
                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == data.IdRole);
                HttpContext.Session.SetString("Firstname", data.FirstName);
                HttpContext.Session.SetString("Surnname", data.SurName);
                HttpContext.Session.SetString("Tel", data.Tel);
                HttpContext.Session.SetString("role", role.Name);
                HttpContext.Session.SetString("image", xx.ImagePath);

                return Redirect("/Home/Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Users data)
        {
            var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel);
            if (xx != null)
            {
                if (data.Images != null)
                {
                    string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "img");

                    if (data.Images.Length > 0)
                    {
                        string filePath = Path.Combine(uploads, data.Images.FileName);
                        using (Stream filestream = new FileStream(filePath, FileMode.Create))
                        {
                            data.Images.CopyTo(filestream);
                        }
                        xx.ImagePath = "/img/" + data.Images.FileName;
                    }
                }
                xx.FirstName = data.FirstName;
                xx.SurName = data.SurName;
                xx.Address = data.Address;
                if (data.Password != null && data.Password != "")
                {
                    xx.Password = data.Password;
                }
                db.SaveChanges();

                HttpContext.Session.SetString("Firstname", data.FirstName);
                HttpContext.Session.SetString("Surnname", data.SurName);
                HttpContext.Session.SetString("Tel", data.Tel);
                HttpContext.Session.SetString("image", xx.ImagePath);
            }
            return Redirect("/Login/Account_me");
        }

        public async Task<IActionResult> Logout()
        {
            //var tel = HttpContext.Session.GetString("Tel");
            //var user = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
            //user.OnlineStatus = false;
            //db.SaveChanges();
            //HttpContext.Session.Clear();
            //var cookieOptions = new CookieOptions();
            //cookieOptions.Expires = DateTime.Now.AddHours(1);
            //cookieOptions.Path = "/Home/Index";
            //Response.Cookies.Append("tokensMon", "");
            //return Redirect("/Home/Index");

            var tel = HttpContext.Session.GetString("Tel");
            if (!string.IsNullOrEmpty(tel))
            {
                var user = db.User.FirstOrDefault(u => u.Tel == tel);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.Token = null;
                    user.WorkDate = null;
                    db.SaveChanges();
                }
            }

            HttpContext.Session.Clear();
            Response.Cookies.Delete("tokensMon");
            return Redirect("/Login/Login");
        }

        [HttpPost]
        public async Task<IActionResult> cancelUser(int ids)
        {
            var xx = await db.User.FindAsync(ids);
            xx.Isactive = false;
            db.SaveChanges();
            HttpContext.Session.Clear();
            return Redirect("/Home/Index");
        }

        public string GetIPAddress()
        {
            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv6().ToString();
            //return remoteIpAddress;
            //GetClientMAC("::ffff:101.51.148.104");
            //Microsoft.AspNetCore.Http.HttpContext context;
            //string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            //if (!string.IsNullOrEmpty(ipAddress))
            //{
            //    string[] addresses = ipAddress.Split(',');
            //    if (addresses.Length != 0)
            //    {
            //        return addresses[0];
            //    }
            //}

            //return "";

            return GetClientMAC(remoteIpAddress); ;
        }

        [DllImport("Iphlpapi.dll")]
        private static extern int SendARP(Int32 dest, Int32 host, ref Int64 mac, ref Int32 length);

        [DllImport("Ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);

        private static string GetClientMAC(string strClientIP)
        {
            string mac_dest = "";
            try
            {
                Int32 ldest = inet_addr(strClientIP);
                Int32 lhost = inet_addr("");
                Int64 macinfo = new Int64();
                Int32 len = 6;
                int res = SendARP(ldest, 0, ref macinfo, ref len);
                string mac_src = macinfo.ToString("X");

                while (mac_src.Length < 12)
                {
                    mac_src = mac_src.Insert(0, "0");
                }

                for (int i = 0; i < 11; i++)
                {
                    if (0 == (i % 2))
                    {
                        if (i == 10)
                        {
                            mac_dest = mac_dest.Insert(0, mac_src.Substring(i, 2));
                        }
                        else
                        {
                            mac_dest = "-" + mac_dest.Insert(0, mac_src.Substring(i, 2));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception("L?i " + err.Message);
            }
            return mac_dest;
        }
    }
}
