using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Models;
using NToastNotify;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using static Monklongpae.Models.oparetion;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Monklongpae.Controllers
{
    public class HomeController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;
        public readonly MonklongpaeContext db = new MonklongpaeContext();
        private readonly IToastNotification _toastNotification;

        public HomeController(IWebHostEnvironment environment, IToastNotification toastNotification)
        {
            _hostingEnvironment = environment;
            _toastNotification = toastNotification;
        }

        #region ปัญหาหลัก มี 3 ส่วนนี้ 
        public async Task<IActionResult> Index()
        {
            var tel = HttpContext.Session.GetString("Tel");
            List<Video> data4 = new List<Video>();

            if (tel is not null)
            {
                var user = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);

                var videoIds = await db.History
                    .Where(x => x.IdUser == user.IdUser)
                    .OrderByDescending(x => x.CreateDate)
                    .Take(5)
                    .Select(x => x.IdVideo)
                    .Distinct()
                    .ToListAsync();

                data4 = await db.Video
                    .Where(x => videoIds.Contains(x.IdVideo))
                    .OrderByDescending(x => x.CreateDate)
                    .ToListAsync();
            }

            var cato = await db.Catory.ToListAsync();
            var data3 = new List<VideosandCatory>();

            foreach (var xc in cato)
            {
                var videos = await db.NameVideo
                    .Where(x => x.IdCatory == xc.IdCatory)
                    .OrderByDescending(x => x.CreateDate)
                    .ToListAsync();

                if (videos.Any())
                {
                    data3.Add(new VideosandCatory
                    {
                        catory = xc.Name,
                        videos = videos
                    });
                }
            }

            var data2 = await db.Video.OrderByDescending(x => x.CreateDate).Take(5).ToListAsync();
            var data1 = await db.VideoPage.OrderByDescending(x => x.CreateDate).ToListAsync();
            var data = await db.PageMessage.FirstOrDefaultAsync(x => x.IdPage == 1);
            var message = "";

            var model = new IndexViewModel
            {
                data = data,
                data1 = data1,
                data2 = data2,
                data3 = data3,
                data4 = data4,
                message = message
            };

            return View(model);
        }

        public async Task<IActionResult> Update_online()
        {
            var tokens = Request.Cookies["tokensMons"];

            if (!string.IsNullOrEmpty(tokens))
            {
                var user = await db.User.FirstOrDefaultAsync(x => x.Token == tokens);
                if (user != null)
                {
                    user.WorkDate = DateTime.Now;

                    int limit_date = 0;
                    if (user.IdRole != 3)
                    {
                        if (user.PacketDateLimit != null)
                        {
                            var limit = user.PacketDateLimit - DateTime.Now;
                            if (limit.Value.Days > 0)
                            {
                                limit_date = limit.Value.Days;
                            }
                            else
                            {
                                limit_date = 0;
                            }
                        }
                        else if (user.IdRole != 3)
                        {
                            user.IdRole = 1;
                        }
                    }
                    else
                    {
                        limit_date = 2;
                    }
                    
                    var role = await db.Role.FirstOrDefaultAsync(x => x.IdRole == user.IdRole);

                    HttpContext.Session.SetString("Firstname", user.FirstName);
                    HttpContext.Session.SetString("Surnname", user.SurName);
                    HttpContext.Session.SetString("Tel", user.Tel);
                    HttpContext.Session.SetString("role", role?.Name ?? "user");
                    HttpContext.Session.SetString("image", user.ImagePath);
                    HttpContext.Session.SetInt32("package", limit_date);

                    var day = user.PacketDateLimit == null ? 0 : (user.PacketDateLimit - DateTime.Now).Value.Days;
                    HttpContext.Session.SetInt32("ExpiresDay", day);

                    await db.SaveChangesAsync();

                    if (string.IsNullOrEmpty(HttpContext.Session.GetString("Tel")))
                        return Json("login");

                    return Json("success");
                }
                else
                {

                }
            }
 
            var tel = HttpContext.Session.GetString("Tel");
            if (!string.IsNullOrEmpty(tel))
            {
                var user = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                if (user != null)
                {
                    string tokenText = user.Tel + DateTime.Now.ToString();
                    user.Token = generate_token(tokenText);
                    user.WorkDate = DateTime.Now;

                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(1),
                        Path = "/"
                    };

                    Response.Cookies.Append("tokensMons", tokenText, cookieOptions);
                    //Response.Cookies.Append("tokensMons", tokenText);
                    await db.SaveChangesAsync();

                    return Json("login");
                }
            }

            return Json("unauthorized");
        }

        private string generate_token(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        #endregion

        public class IndexViewModel
        {
            public PageMessage? data { get; set; }
            public List<VideoPage>? data1 { get; set; }
            public List<Video>? data2 { get; set; }
            public List<VideosandCatory>? data3 { get; set; }
            public List<Video>? data4 { get; set; }
            public string? message { get; set; }
        }

        public async Task<IActionResult> Money_bank(int id)
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel == null) return Redirect("/Login/Login");
            if (tel != null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                if (xx != null)
                {
                    var dca = await db.Payment.FirstOrDefaultAsync(x => x.IdUser == xx.IdUser && x.Isactive == false && x.FilePayment != null);
                    if (dca != null)
                    {
                        return Redirect("/Home/Money_file?id=" + dca.IdPayment);
                    }
                }
            }
            var data = await db.Packet.FirstOrDefaultAsync(x => x.IdPacket == id);
            var data1 = await db.Account.FirstOrDefaultAsync();

            return View(new { data, data1 });
        }

        public async Task<IActionResult> Money_file(int id)
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel is not null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                if (xx is not null)
                {
                    var awr = await db.Payment.FirstOrDefaultAsync(x => x.IdUser == xx.IdUser && x.Isactive == false);
                    if (awr == null)
                    {
                        db.Payment.Add(new Payment
                        {
                            Status = "รอส่งสลิป",
                            CreateDate = DateTime.Now,
                            IdPacket = id,
                            IdUser = xx.IdUser,
                            Isactive = false
                        });
                        await db.SaveChangesAsync();
                        var data = await db.Payment.FirstOrDefaultAsync(x => x.IdUser == xx.IdUser);
                        return View(data);
                    }
                    else
                    {
                        var data = awr;
                        return View(data);
                    }
                }
            }

            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> Money_file_s(IFormFile file, int idpack)
        {
            try
            {
                var tel = HttpContext.Session.GetString("Tel");
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);

                if (xx is not null)
                {
                    if (file != null)
                    {
                        if (file.Length > 0)
                        {
                            if (file != null)
                            {
                                string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "bill");
                                var cc1 = await db.Payment.FirstOrDefaultAsync(x => x.IdUser == xx.IdUser && x.Isactive == false);
                                if (cc1 == null)
                                {
                                    var adddata = new Payment
                                    {
                                        Status = "รอตรวจสอบ",
                                        CreateDate = DateTime.Now,
                                        IdPacket = idpack,
                                        IdUser = xx.IdUser,
                                        Isactive = false
                                    };

                                    db.Payment.Add(adddata);
                                    await db.SaveChangesAsync();
                                }
     
                                if (file.Length > 0)
                                {
                                    string filePath = Path.Combine(uploads, file.FileName);
                                    using (Stream filestream = new FileStream(filePath, FileMode.Create))
                                    {
                                        file.CopyTo(filestream);
                                    }
                                    var cc = await db.Payment.FirstOrDefaultAsync(x => (x.Isactive == false || x.Isactive ==null ) && x.IdUser == xx.IdUser && x.IdPacket == idpack);
                                    if (cc != null)
                                    {
                                        cc.FilePayment = "/bill/" + file.FileName;
                                        cc.Status = "รอตรวจสอบ";
                                        cc.UpdateDate = DateTime.Now;
                                        db.Entry(cc).State = EntityState.Modified;
                                        await db.SaveChangesAsync();

                                        _toastNotification.AddSuccessToastMessage("รอแอดมินตรวจสอบ");
                                        return Redirect("/Home/Index");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _toastNotification.AddSuccessToastMessage("กรุณาแนบไฟล์ชำระเงิน");
                    }
                    return RedirectToAction("Money_bank", "Home", new { id = idpack });
                }
                else
                {
                    _toastNotification.AddSuccessToastMessage("กรุณาแนบไฟล์ชำระเงิน");
                    return RedirectToAction("Money_bank", "Home", new { id = idpack });
                }

                //return View();
            }
            catch (Exception ex)
            {
                return Ok(ex.InnerException);
            }
        }

        public async Task<IActionResult> Video_total()
        {
            var cato = await db.Catory.ToListAsync();
            List<VideosandCatory> data1 = new List<VideosandCatory>();

            foreach (var xc in cato)
            {
                var xca = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).OrderByDescending(x => x.CreateDate).ToListAsync();
                if (xca != null && xca.Count() > 0)
                {
                    var sds = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).ToListAsync();
                    if (sds != null)
                    {
                        data1.Add(new VideosandCatory
                        {

                            catory = xc.Name,
                            videos = sds
                        });
                    }
                }
            }
            var data = await db.PageMessage.FirstOrDefaultAsync(x => x.IdPage == 1);
            return View(new { data, data1 });
        }

        public async Task<IActionResult> Money_page()
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel != null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                if (xx != null)
                {
                    var dca = await db.Payment.FirstOrDefaultAsync(x => x.IdUser == xx.IdUser && x.Isactive == false && x.FilePayment != null);
                    if (dca != null)
                    {
                        return Redirect("/Home/Money_file?id=" + dca.IdPayment);
                    }
                }
            }
            var data1 = await db.PageMessage.FirstOrDefaultAsync(x => x.IdPage == 1);
            var data = await db.Packet.ToListAsync();
            return View(new { data, data1 });
        }

        public async Task<IActionResult> Contact()
        {

            var data1 = await db.Contact.FirstOrDefaultAsync(x => x.IdContact == 1);
            var data = await db.PageMessage.FirstOrDefaultAsync(x => x.IdPage == 1);
            return View(new { data, data1 });
        }

        public async Task<IActionResult> Video_detail(int id, int idnamevideo)
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel == null) return Redirect("/Login/Login");

            if (idnamevideo == 0)
            {
                var ids = await db.Video.FindAsync(id);
                idnamevideo = Convert.ToInt32(ids!.IdNameVideo);
            }

            if (id == 0)
            {
                var ids = await db.Video.OrderBy(x => x.IdVideo).FirstOrDefaultAsync(x => x.IdNameVideo == idnamevideo);
                if(ids != null)
                { 
                    id = ids.IdVideo;
                }
            }

            var data = await db.Video.FirstOrDefaultAsync(x => x.IdVideo == id);
            var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
            List<history_video> datas = new List<history_video>();
            var data1 = await db.Video.Where(x => x.IdNameVideo == idnamevideo).OrderBy(x => x.IdVideo).ToListAsync();
            var ton = 1;
            foreach (var aasd in data1)
            {
                var statusc = "";
                if (aasd.IdVideo == id)
                {
                    statusc = "bg-danger";
                }
                datas.Add(new history_video
                {
                    idvideo = aasd.IdVideo,
                    idnamevideo = aasd.IdNameVideo,
                    name = "ตอน " + ton,
                    time = aasd.TimeVideo,
                    detail = aasd.Description,
                    status = statusc,
                    imgs = aasd.PathDisplay
                });
                ton++;
            }
            if (data != null)
            {
                data.Views = data.Views + 1;
            }
            db.History.Add(new History
            {
                IdVideo = id,
                IdUser = xx?.IdUser,
                CreateDate = DateTime.Now
            });
            await db.SaveChangesAsync();
            var data2 = datas.Where(x => x.idvideo > data?.IdVideo).FirstOrDefault();
            return View(new { data, datas, data2, idVideo = id });
        }

        public async Task<IActionResult> showmarket(int ids, double min)
        {
            var data = await db.Market.FirstOrDefaultAsync(x => x.IdVideo == ids && min >= x.Duration && min <= (x.Duration + x.Showtime));
            return Ok(data);
        }

        public async Task<IActionResult> Cancel_packet(int id)
        {
            var tel = HttpContext.Session.GetString("Tel");
            if (tel != null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == tel);
                if (xx != null)
                {
                    var cac = await db.Payment.FirstOrDefaultAsync(x => x.IdPayment == id);
                    if (cac != null)
                    {
                        db.Payment.Remove(cac);
                        await db.SaveChangesAsync();
                    }
                }
            }
            return Redirect("/Home/Money_page");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> searchcatory(string search)
        {

            var cato = await db.Catory.Where(x => x.Name != null).ToListAsync();
            var countcato = false;
            var countvideo = false;
            List<VideosandCatory> data3 = new List<VideosandCatory>();

            if (search != null && search != "")
            {
                var count1 = cato.Where(x => x.Name.Contains(search)).Count();
                if (count1 > 0)
                {
                    cato = cato.Where(x => x.Name.Contains(search)).ToList();
                    countcato = true;
                }
                var count2 = db.NameVideo.Where(x => x.Name.Contains(search)).Count();
                if (count2 > 0)
                {
                    countvideo = true;
                }
                if (countcato == false && countvideo == false)
                {
                    return Json(data3);
                }
            }

            foreach (var xc in cato)
            {
                if (countcato == true)
                {
                    var xca = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).OrderByDescending(x => x.CreateDate).ToListAsync();
                    if (xca != null && xca.Count() > 0)
                    {
                        var sds = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).ToListAsync();
                        data3.Add(new VideosandCatory
                        {

                            catory = xc.Name,
                            videos = sds,
                            //timeVideo = 
                        });
                    }
                }
                else if (countvideo == true)
                {

                    var xca = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory && x.Name.Contains(search)).ToListAsync();
                    if (xca != null && xca.Count() > 0)
                    {
                        var sds = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory && x.Name.Contains(search)).ToListAsync();
                        data3.Add(new VideosandCatory
                        {

                            catory = xc.Name,
                            videos = sds
                        });
                    }
                }
                else
                {
                    var xca = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).OrderByDescending(x => x.CreateDate).ToListAsync();
                    if (xca != null && xca.Count() > 0)
                    {
                        var sds = await db.NameVideo.Where(x => x.IdCatory == xc.IdCatory).ToListAsync();
                        data3.Add(new VideosandCatory
                        {

                            catory = xc.Name,
                            videos = sds
                        });
                    }
                }
            }
            return Json(data3);
        }


        [HttpGet]
        public async Task<IActionResult> CheckPackages()
        {
            var tokens = Request.Cookies["tokensMons"];
            var limit_date = 0;
            if (!string.IsNullOrEmpty(tokens))
            {
                var getuser = await db.User.FirstOrDefaultAsync(x => x.Token == tokens);
                if (getuser != null)
                {
                    var daysLeft = getuser.PacketDateLimit!.Value.Date - DateTime.Now.Date;
                    if (daysLeft.Days > 0)
                    {
                        limit_date = daysLeft.Days;
                    }
                    else
                    {
                        limit_date = 0;
                    }
                }
            }
            return Json(limit_date);
        }
    }
}