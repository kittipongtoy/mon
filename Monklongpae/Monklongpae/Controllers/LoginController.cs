using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Models;
using NToastNotify;
using static Monklongpae.Models.oparetion;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
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
                ViewBag.ids = xx?.IdUser;
                ViewBag.fir = xx?.FirstName;
                ViewBag.sur = xx?.SurName;
                ViewBag.address = xx?.Address;
                ViewBag.tel = xx?.Tel;
                ViewBag.image = xx?.ImagePath;
                var status = "";
                if (xx?.IdRole == 1)
                {
                    status = "ฟรี";
                }
                else if (xx?.IdRole == 2)
                {
                    var day = xx.PacketDateLimit == null ? 0 : (xx.PacketDateLimit - DateTime.Now).Value.Days;
                    status = "VIP" + " จำนวนวัน คงเหลือ " + day + " วัน";
                }
                else if (xx?.IdRole == 3)
                {
                    status = "แอดมิน";
                }
                ViewBag.role = status;
            }
            return View();
        }

        public async Task<IActionResult> UpdateData(string tel)
        {
            var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel); //&& x.Isactive == true
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
                await db.SaveChangesAsync();
                if (user.PacketDateLimit != null)
                {
                    var nows = DateTime.Now;
                    var limit = xx.PacketDateLimit - nows;
                    limit_date = limit == null ? 0 : limit.Value.Days;
                    limit_date = limit_date <= 0 ? 0 : limit_date;
                }
                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == xx.IdRole);
                HttpContext.Session.SetString("Firstname", xx.FirstName);
                HttpContext.Session.SetString("Surnname", xx.SurName);
                HttpContext.Session.SetString("Tel", xx.Tel);
                HttpContext.Session.SetString("role", (role!.Name));
                HttpContext.Session.SetString("image", xx.ImagePath);
                HttpContext.Session.SetInt32("package", limit_date);

                var day = user.PacketDateLimit == null ? 0 : (user.PacketDateLimit - DateTime.Now).Value.Days;
                HttpContext.Session.SetInt32("ExpiresDay", day);

                var cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(1);
                cookieOptions.Path = "/";
                Response.Cookies.Append("tokensMons", tokens, cookieOptions);
                //Response.Cookies.Append("tokensMons", tokens);
                _toastNotification.AddSuccessToastMessage("สามารถรับชมวิดีโอได้ รอ 3 วิ หรือกดรีเฟรช");
                return Ok("success.");
            }
            _toastNotification.AddErrorToastMessage("ไม่พบข้อมูล");
            return Ok("Error.");
        }

        [HttpPost]
        public async Task<IActionResult> Login(User data)
        {
            var user = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel); //&& x.Isactive == true

            if (user != null)
            {
                var now = DateTime.Now;

                //// ถ้ามี Token และใช้งานภายใน 15 นาทีล่าสุด → ถือว่า login ซ้ำ
                //if (user.IsOnline.GetValueOrDefault() && !string.IsNullOrEmpty(user.Token) &&
                //user.WorkDate.HasValue &&
                //(now - user.WorkDate.Value).TotalMinutes < 15)
                //{
                //    _toastNotification.AddErrorToastMessage("บัญชีนี้กำลังใช้งานบนเครื่องอื่น");
                //    return Redirect("/Login/Login");
                //}

                // ปรับปรุงสถานะ
                user.Token = generate_token(user.Tel + now.ToString());
                user.IsOnline = true;
                user.WorkDate = now;
                await db.SaveChangesAsync();

                // คำนวณวันเหลือ
                int limit_date = 0;
                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == user.IdRole);
                if (role.IdRole == 3)
                {
                    limit_date = 2;
                }

                if (user.PacketDateLimit.HasValue && role.IdRole != 3)
                {
                    var daysLeft = (user.PacketDateLimit.Value - DateTime.Now).Days;
                    limit_date = daysLeft < 0 ? 0 : daysLeft;
                }

                // เก็บลง session และ cookie
                HttpContext.Session.SetString("Firstname", user.FirstName ?? "");
                HttpContext.Session.SetString("Surnname", user.SurName ?? "");
                HttpContext.Session.SetString("Tel", user.Tel);
                HttpContext.Session.SetString("role", role?.Name ?? "User");
                HttpContext.Session.SetString("image", user.ImagePath ?? "");
                HttpContext.Session.SetInt32("package", limit_date);

                var day = user.PacketDateLimit == null ? 0 : (user.PacketDateLimit - DateTime.Now).Value.Days;
                HttpContext.Session.SetInt32("ExpiresDay", day);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(1),
                    Path = "/"
                };
                Response.Cookies.Append("tokensMons", user.Token, cookieOptions);
                //Response.Cookies.Append("tokensMons", user.Token);
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
                await db.SaveChangesAsync();

                var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == data.IdRole);

                HttpContext.Session.SetString("Firstname", data.FirstName);
                HttpContext.Session.SetString("Surnname", data.SurName);
                HttpContext.Session.SetString("Tel", data.Tel);
                HttpContext.Session.SetString("role", role?.Name ?? "User");
                HttpContext.Session.SetString("image", data.ImagePath);
                HttpContext.Session.SetInt32("package", 0);

                var day = user.PacketDateLimit == null ? 0 : (user.PacketDateLimit - DateTime.Now).Value.Days;
                HttpContext.Session.SetInt32("ExpiresDay", day);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(1),
                    Path = "/"
                };

                Response.Cookies.Append("tokensMons", tokens, cookieOptions);
                //Response.Cookies.Append("tokensMons", tokens);

                _toastNotification.AddSuccessToastMessage("สมัครสมาชิกสำเร็จ");
                return Redirect("/Home/Index");
            }
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

                await db.SaveChangesAsync();
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
                await db.SaveChangesAsync();

                HttpContext.Session.SetString("Firstname", data.FirstName);
                HttpContext.Session.SetString("Surnname", data.SurName);
                HttpContext.Session.SetString("Tel", data.Tel);
                HttpContext.Session.SetString("image", xx.ImagePath);
            }
            return Redirect("/Login/Account_me");
        }

        public async Task<IActionResult> Logout()
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (!string.IsNullOrEmpty(tel))
            {
                var user = db.User.FirstOrDefault(u => u.Tel == tel);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.Token = null;
                    user.WorkDate = null;
                    await db.SaveChangesAsync();
                }
            }

            // เคลียร์ session
            HttpContext.Session.Clear();

            // เคลียร์ cookie ทั้งหมดของผู้ใช้ที่กำลังกด logout
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return Redirect("/Login/Login");
        }

        [HttpPost]
        public async Task<IActionResult> cancelUser(int ids)
        {
            var xx = await db.User.FindAsync(ids);
            xx.Isactive = false;
            await db.SaveChangesAsync();
            HttpContext.Session.Clear();
            return Redirect("/Home/Index");
        }

        public string GetIPAddress()
        {
            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv6().ToString();
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
