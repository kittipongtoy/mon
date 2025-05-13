using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Models;
using Monklongpae.SingleR;
using Newtonsoft.Json;
using Xabe.FFmpeg;
using static Monklongpae.Models.oparetion;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Monklongpae.Controllers
{
    public class BackofficeController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;
        public readonly MonklongpaeContext db = new MonklongpaeContext();
        private readonly IStrogeManage _connectionManager;
        public BackofficeController(IWebHostEnvironment environment,
            IStrogeManage connectionManager
            )
        {
            _hostingEnvironment = environment;
            _connectionManager = connectionManager;
        }

        public async Task<IActionResult> Index()
        {
            var getcon = _connectionManager.GetConnections();
            foreach (var tt in getcon)
            {
                if (System.IO.File.Exists(tt.Value.ToString()))
                {
                    System.IO.File.Delete(tt.Value.ToString());
                    _connectionManager.RemoveConnection(tt.Value.ToString());
                }
            }

            var tel = HttpContext.Session.GetString("Tel");
            if (tel == null) return Redirect("/Home/Index");
            List<table1> table = new List<table1>();
            var count1 = db.User.Count();
            var count2 = db.User.Where(x => x.IdRole == 1).Count();
            var count3 = db.User.Where(x => x.IdRole == 2).Count();
            var ax = await db.NameVideo.OrderByDescending(x => x.IdNameVideo).Take(10).ToListAsync();
            foreach (var xc in ax)
            {
                var aca = await db.Catory.FirstOrDefaultAsync(x => x.IdCatory == xc.IdCatory);
                if (aca != null)
                {
                    var xcc = await db.Video.FirstOrDefaultAsync(x => x.IdNameVideo == xc.IdNameVideo);
                    if (xcc != null)
                    {
                        table.Add(new table1
                        {
                            datetime = Convert.ToDateTime(xc.CreateDate).ToString("dd/MM/yyyy hh:mm:ss"),
                            catory = aca.Name,
                            name = xc.Name,
                            timeclip = xcc.TimeVideo,
                            views = xcc.Views
                        });
                    }
                }
            }
            return View(new { count1, count2, count3, table });
        }


        public IActionResult User()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> User(User data)
        {
            if (data != null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.Tel == data.Tel);
                if (xx == null)
                {
                    data.ImagePath = "/img/logo_web.jpg";
                    if (data.IdRole == null)
                    {
                        data.IdRole = 1;
                    }
                    data.CreateDate = DateTime.Now;
                    data.PacketDateLimit = DateTime.Now;
                    data.Isactive = true;
                    db.User.Add(data);
                    await db.SaveChangesAsync();
                }
            }
            return View();
        }

        public IActionResult Cert_market()
        {
            return View();
        }

        public async Task<IActionResult> Cert_market_select(int ids)
        {
            var pd = await db.Payment.FirstOrDefaultAsync(x => x.IdPayment == ids);
            return Json(pd);
        }

        [HttpPost]
        public async Task<IActionResult> Cert_market(int IdPayment)
        {
            var cm = await db.Payment.FirstOrDefaultAsync(x => x.IdPayment == IdPayment && x.Isactive != null);
            if (cm is not null)
            {
                var xx = await db.User.FirstOrDefaultAsync(x => x.IdUser == cm.IdUser);
                var pd = await db.Packet.FirstOrDefaultAsync(x => x.IdPacket == cm.IdPacket);
                xx!.IdRole = 2;
                xx!.PacketDateLimit = DateTime.Now.AddDays(Convert.ToInt32(pd.Day));
                cm.Isactive = true;
                cm.Status = "ส่งสำเร็จ";
                cm.UpdateDate = DateTime.Now;
               await db.SaveChangesAsync();
            }
            return Ok("Success");
        }

        public async Task<IActionResult> ManageVideo()
        {
            var data = await db.Catory.ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Contact()
        {
            var contact = await db.Contact.FirstOrDefaultAsync(x => x.IdContact == 1);
            return View(contact);
        }

        [HttpPost]
        public async Task<IActionResult> Contact(Contact data)
        {
            var contact = await db.Contact.FirstOrDefaultAsync(x => x.IdContact == 1);
            if (contact != null)
            {
                contact.Tel = data.Tel;
                contact.Address = data.Address;
                contact.Map = data.Map;
                await db.SaveChangesAsync();
            }
            return View(contact);
        }

        public IActionResult Video_and_image()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Video_and_image(VideoPage data)
        {
            if (data != null)
            {
                if (data.Path != null && data.Path != "")
                {
                    db.VideoPage.Add(new VideoPage
                    {
                        Path = data.Path
                    });
                   await db.SaveChangesAsync();
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> upload_Video(IFormFile file)
        {
            var url = HttpContext.Request.Host;
            if (file != null)
            {
                if (file.Length > 0)
                {
                    string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");
                    string filePath1 = Path.Combine(uploads, file.FileName);
                    using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                    {
                        file.CopyTo(filestream1);
                    }

                    db.VideoPage.Add(new VideoPage
                    {
                        Path = "/video/" + file.FileName
                    });
                    await db.SaveChangesAsync();
                }
            }
            return Redirect("/Backoffice/Video_and_image");
        }

        public async Task<IActionResult> Payments()
        {
            var account = await db.Account.FirstOrDefaultAsync(x => x.IdAccount == 1);
            var data = await db.Packet.ToListAsync();
            if (account != null)
            {
                return View(new { account, data });
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Payments(Account data)
        {
            var account = await db.Account.FirstOrDefaultAsync(x => x.IdAccount == 1);
            if (account != null)
            {
                account.Account1 = data.Account1;
                account.NameAccout = data.NameAccout;
                account.AccountStatus = data.AccountStatus;
                await db.SaveChangesAsync();
                return View(account);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit_payment(Packet data)
        {
            var pp = await db.Packet.FindAsync(data.IdPacket);
            if (pp != null)
            {
                pp.Price = data.Price;
                pp.Status = data.Status;
                pp.Name = data.Name;
                pp.Day = data.Day;
                pp.Description = data.Description;
                await db.SaveChangesAsync();
            }
            return Redirect("/Backoffice/Payments");
        }

        public async Task<IActionResult> Message()
        {
            var data = await db.PageMessage.FirstOrDefaultAsync();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Message(PageMessage data)
        {
            List<PageMessage> data1 = new List<PageMessage>();
            var dbs = await db.PageMessage.FirstOrDefaultAsync(x => x.IdPage == 1);
            if (dbs is not null)
            {
                if (data.MessagePage1En != null)
                {
                    dbs.MessagePage1Th = data.MessagePage1Th;
                    dbs.MessagePage1En = data.MessagePage1En;
                }
                else if (data.MessagePage2En != null)
                {
                    dbs.MessagePage2Th = data.MessagePage2Th;
                    dbs.MessagePage2En = data.MessagePage2En;
                }
                else if (data.MessagePage3En != null)
                {
                    dbs.MessagePage3Th = data.MessagePage3Th;
                    dbs.MessagePage3En = data.MessagePage3En;
                }
                else if (data.MessagePage4En != null)
                {
                    dbs.MessagePage4Th = data.MessagePage4Th;
                    dbs.MessagePage4En = data.MessagePage4En;
                }
                await  db.SaveChangesAsync();
            }
            return View(dbs);
        }

        public async Task<IActionResult> AddCatoryAndVideo()
        {
            var data = await db.Catory.ToListAsync();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> AddCatoryAndVideo(Catory data)
        {
            var dbs = await db.Catory.FirstOrDefaultAsync(x => x.Name == data.Name);
            if (dbs == null)
            {
                if (data.Name != null && data.Name != "")
                {
                    db.Catory.Add(new Catory
                    {
                        Name = data.Name,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now
                    });
                }
                await db.SaveChangesAsync();
            }
            var data1 = await db.Catory.ToListAsync();
            return View(data1);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCate(int? ids)
        {
            if (ids != null)
            {
                var cate = await db.Catory.FindAsync(ids);
                if (cate != null)
                {
                    db.Catory.Remove(cate);
                    await db.SaveChangesAsync();
                    return Json("Success");
                }
                else
                {
                    return Json("Error");
                }
            }
            return Json("Error");
        }

        public async Task<IActionResult> getDatavideo(int ids)
        {
            return Json(new { data = await db.Video.FirstOrDefaultAsync(x => x.IdVideo == ids), market = await db.Market.Where(x => x.IdVideo == ids).ToListAsync() });
        }

        public async Task<IActionResult> delete_market(int ids)
        {
            var finds = await db.Market.FindAsync(ids);
            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");
            string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");
            string filePath = Path.Combine(_hostingEnvironment.WebRootPath, finds.PartImage);
            var sub1 = finds.PartImage.Split("/");
            filePath = Path.Combine(uploads1, sub1[2]);
            try
            {
                System.IO.File.Delete(filePath); // ลบไฟล์หลังจากอัพโหลดเสร็จ
                Console.WriteLine("ไฟล์ถูกลบเรียบร้อยแล้ว");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"เกิดข้อผิดพลาดในการลบไฟล์: {ex.Message}");
            }
            db.Market.Remove(finds);
            await db.SaveChangesAsync();
            return Ok("Success");
        }

        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> upload_file_media(upload_filevedio data)
        {
            try
            {
                if (data.idvideo != null)
                {
                    var datac = await db.Video.FindAsync(data.idvideo);

                    if (data.file != null && data.file.Length > 0)
                    {
                        string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");

                        string filePath = Path.Combine(uploads, data.file.FileName);
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Post, "https://stream.byteark.com/api/v1/videos");
                        request.Headers.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3MTY5MTUwMTAsInRva2VuSWQiOiI2NjU2MGI0MmMwODg0MTAxOWI3NTMzMmMiLCJ1c2VybmFtZSI6Im9yYW5nZXRlY2gvYXBpY2hhQG90cy5jby50aCJ9.-x9p6Ihx_mjxL4BsZvAe4vOyrFg-h0rVVQcma3wHoPo");
                        var content = new StringContent("{\"projectKey\": \"monklongpae-njrdcu\",\"videos\": [{\"title\": \"" + data.file1.FileName + "\"    }  ]}", null, "application/json");
                        request.Content = content;
                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var stringg = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(await response.Content.ReadAsStringAsync());
                        var obj1 = JsonConvert.DeserializeObject<List<oparetion.byteark>>(stringg);


                        using (Stream filestream = new FileStream(filePath, FileMode.Create))
                        {
                            data.file.CopyTo(filestream);
                        }
                        var request1 = new HttpRequestMessage(HttpMethod.Post, "https://stream.byteark.com/api/upload/v1/form-data/videos/" + obj1[0].key);
                        request1.Headers.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3MTY5MTUwMTAsInRva2VuSWQiOiI2NjU2MGI0MmMwODg0MTAxOWI3NTMzMmMiLCJ1c2VybmFtZSI6Im9yYW5nZXRlY2gvYXBpY2hhQG90cy5jby50aCJ9.-x9p6Ihx_mjxL4BsZvAe4vOyrFg-h0rVVQcma3wHoPo");
                        using (var streamsss = System.IO.File.OpenRead(filePath))
                        {
                            var content1 = new MultipartFormDataContent();
                            content1.Add(new StreamContent(streamsss), "file", filePath);
                            request1.Content = content1;
                            var response1 = await client.SendAsync(request1);
                            response.EnsureSuccessStatusCode();
                            Console.WriteLine(await response1.Content.ReadAsStringAsync());
                        }

                        var FFmpegpath = Path.Combine(_hostingEnvironment.WebRootPath, "ffmpeg");
                        FFmpeg.SetExecutablesPath(FFmpegpath, ffmpegExeutableName: "FFmpeg");
                        IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath);
                        var videoDuration = mediaInfo.VideoStreams.First().Duration.ToString();
                        TimeSpan duration = TimeSpan.Parse(videoDuration);
                        var Hour = string.Format("{0:D2}", duration.Hours);
                        var Minutes = string.Format("{0:D2}", duration.Minutes);
                        var Seconds = string.Format("{0:D2}", duration.Seconds);
                        var durationtime = "";
                        if (Hour != "00")
                        {
                            durationtime += Hour + " ชั่วโมง";
                        }
                        if (Minutes != "00")
                        {
                            if (Hour != "00")
                            {
                                durationtime += " " + Minutes + " นาที";
                            }
                            else
                            {
                                durationtime += Minutes + " นาที";
                            }
                        }
                        if (Seconds != "00")
                        {
                            if (Hour != "00" || Minutes != "00")
                            {
                                durationtime += " " + Seconds + " วินาที";
                            }
                            else
                            {
                                durationtime += Seconds + " วินาที";
                            }
                        }
                        datac.TimeVideo = durationtime;
                        datac.PathVideo = obj1[0].primaryPlaybackUrl.hls[0].url;

                        try
                        {
                            System.IO.File.Delete(filePath); // ลบไฟล์หลังจากอัพโหลดเสร็จ
                            Console.WriteLine("ไฟล์ถูกลบเรียบร้อยแล้ว");
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"เกิดข้อผิดพลาดในการลบไฟล์: {ex.Message}");
                        }
                    }
                    if (data.file1 != null && data.file1.Length > 0)
                    {
                        string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");

                        string filePath1 = Path.Combine(uploads1, data.file1.FileName);
                        using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                        {
                            data.file1.CopyTo(filestream1);
                        }
                        datac.PathDisplay = "/img/" + data.file1.FileName;
                    }
                    datac.Name = data.name;
                    datac.Description = data.description;

                    datac.LimitVideoUser = (data.timefree * 60);
                    await db.SaveChangesAsync();

                }
                else
                {
                    if (data != null)
                    {
                        if (data.file.Length > 0 && data.file1.Length > 0)
                        {
                            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");
                            string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");

                            string filePath1 = Path.Combine(uploads1, data.file1.FileName);
                            var client = new HttpClient();
                            var request = new HttpRequestMessage(HttpMethod.Post, "https://stream.byteark.com/api/v1/videos");
                            request.Headers.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3MTY5MTUwMTAsInRva2VuSWQiOiI2NjU2MGI0MmMwODg0MTAxOWI3NTMzMmMiLCJ1c2VybmFtZSI6Im9yYW5nZXRlY2gvYXBpY2hhQG90cy5jby50aCJ9.-x9p6Ihx_mjxL4BsZvAe4vOyrFg-h0rVVQcma3wHoPo");
                            var content = new StringContent("{\"projectKey\": \"monklongpae-njrdcu\",\"videos\": [{\"title\": \"" + data.file1.FileName + "\"    }  ]}", null, "application/json");
                            request.Content = content;
                            var response = await client.SendAsync(request);
                            response.EnsureSuccessStatusCode();
                            var stringg = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                            var obj1 = JsonConvert.DeserializeObject<List<oparetion.byteark>>(stringg);

                            using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                            {
                                data.file1.CopyTo(filestream1);
                            }
                            string filePath = Path.Combine(uploads, data.file.FileName);
                            using (Stream filestream = new FileStream(filePath, FileMode.Create))
                            {
                                data.file.CopyTo(filestream);
                            }
                            var request1 = new HttpRequestMessage(HttpMethod.Post, "https://stream.byteark.com/api/upload/v1/form-data/videos/" + obj1[0].key);
                            request1.Headers.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3MTY5MTUwMTAsInRva2VuSWQiOiI2NjU2MGI0MmMwODg0MTAxOWI3NTMzMmMiLCJ1c2VybmFtZSI6Im9yYW5nZXRlY2gvYXBpY2hhQG90cy5jby50aCJ9.-x9p6Ihx_mjxL4BsZvAe4vOyrFg-h0rVVQcma3wHoPo");
                            using (var streamsss = System.IO.File.OpenRead(filePath))
                            {
                                var content1 = new MultipartFormDataContent();
                                content1.Add(new StreamContent(streamsss), "file", filePath);
                                request1.Content = content1;
                                var response1 = await client.SendAsync(request1);
                                response.EnsureSuccessStatusCode();
                                Console.WriteLine(await response1.Content.ReadAsStringAsync());
                            }

                            var FFmpegpath = Path.Combine(_hostingEnvironment.WebRootPath, "ffmpeg");
                            FFmpeg.SetExecutablesPath(FFmpegpath, ffmpegExeutableName: "FFmpeg");
                            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath);
                            var videoDuration = mediaInfo.VideoStreams.First().Duration.ToString();
                            TimeSpan duration = TimeSpan.Parse(videoDuration);
                            var Hour = string.Format("{0:D2}", duration.Hours);
                            var Minutes = string.Format("{0:D2}", duration.Minutes);
                            var Seconds = string.Format("{0:D2}", duration.Seconds);
                            var durationtime = "";
                            if (Hour != "00")
                            {
                                durationtime += Hour + " ชั่วโมง";
                            }
                            if (Minutes != "00")
                            {
                                if (Hour != "00")
                                {
                                    durationtime += " " + Minutes + " นาที";
                                }
                                else
                                {
                                    durationtime += Minutes + " นาที";
                                }
                            }
                            if (Seconds != "00")
                            {
                                if (Hour != "00" || Minutes != "00")
                                {
                                    durationtime += " " + Seconds + " วินาที";
                                }
                                else
                                {
                                    durationtime += Seconds + " วินาที";
                                }
                            }

                            db.Video.Add(new Video
                            {
                                IdNameVideo = data.idnameVideo,
                                Name = data.name,
                                Description = data.description,
                                TimeVideo = durationtime,
                                PathVideo = obj1[0].primaryPlaybackUrl.hls[0].url,
                                PathDisplay = "/img/" + data.file1.FileName,
                                LimitVideoUser = (data.timefree * 60),
                                CreateDate = DateTime.Now,
                                Views = 0
                            });
                            await db.SaveChangesAsync();
                            //_connectionManager.AddConnection(filePath, filePath);
                            try
                            {
                                System.IO.File.Delete(filePath); // ลบไฟล์หลังจากอัพโหลดเสร็จ
                                Console.WriteLine("ไฟล์ถูกลบเรียบร้อยแล้ว");
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine($"เกิดข้อผิดพลาดในการลบไฟล์: {ex.Message}");
                            }
                        }
                    }
                }
                return Redirect("/Backoffice/AddCatoryAndVideo");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IActionResult> update_cate(string namecate, int cates)
        {
            var finds = await db.Catory.FindAsync(cates);
            finds.Name = namecate;
            await db.SaveChangesAsync();
            return Redirect("/Backoffice/AddCatoryAndVideo");
        }

        public async Task<IActionResult> get_pok(int ids)
        {
            return Ok(await db.NameVideo.FindAsync(ids));
        }

        public async Task<IActionResult> update_pok(upload_filevedio2 data)
        {
            try
            {
                if (data != null)
                {
                    var finds = await db.NameVideo.FindAsync(data.idVideoName);
                    if (finds != null)
                    {
                        if (data.file1.Length > 0)
                        {
                            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");
                            string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");
                            string filePath = Path.Combine(_hostingEnvironment.WebRootPath, finds.PathDisplay);
                            var sub1 = finds.PathDisplay.Split("/");
                            filePath = Path.Combine(uploads1, sub1[2]);
                            try
                            {
                                System.IO.File.Delete(filePath); // ลบไฟล์หลังจากอัพโหลดเสร็จ
                                Console.WriteLine("ไฟล์ถูกลบเรียบร้อยแล้ว");
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine($"เกิดข้อผิดพลาดในการลบไฟล์: {ex.Message}");
                            }
                            string filePath1 = Path.Combine(uploads1, data.file1.FileName);
                            using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                            {
                                data.file1.CopyTo(filestream1);
                            }
                            finds.PathDisplay = "/img/" + data.file1.FileName;
                        }
                        finds.Name = data.name;
                        await db.SaveChangesAsync();
                    }

                }
                return Redirect("/Backoffice/AddCatoryAndVideo");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IActionResult> add_market(marketdetail data)
        {
            if (data != null)
            {
                string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");
                foreach (var tt in data.marketalls)
                {
                    if (tt.images.Length > 0)
                    {
                        string filePath1 = Path.Combine(uploads1, tt.images.FileName);
                        using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                        {
                            tt.images.CopyTo(filestream1);
                        }
                        var timessub1 = tt.Showtime.ToString().Split(".");
                        var timessub2 = tt.Duration.ToString().Split(".");

                        db.Market.Add(new Market
                        {
                            Showtime = (Convert.ToDouble(timessub1[0]) * 60) + (Convert.ToDouble("0." + timessub1[1]) * 100),
                            Duration = (Convert.ToDouble(timessub2[0]) * 60) + (Convert.ToDouble("0." + timessub2[1]) * 100),
                            IdVideo = data.IdVideo,
                            CreateAt = DateTime.Now,
                            Url = tt.url,
                            PartImage = "/img/" + tt.images.FileName
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
            return Redirect("/Backoffice/ManageVideo");
        }

        public async Task<IActionResult> upload_file_media1(upload_filevedio1 data)
        {
            try
            {
                if (data != null)
                {
                    if (data.file1.Length > 0)
                    {
                        string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "video");
                        string uploads1 = Path.Combine(_hostingEnvironment.WebRootPath, "img");

                        string filePath1 = Path.Combine(uploads1, data.file1.FileName);
                        using (Stream filestream1 = new FileStream(filePath1, FileMode.Create))
                        {
                            data.file1.CopyTo(filestream1);
                        }

                        db.NameVideo.Add(new NameVideo
                        {
                            IdCatory = data.idCatory,
                            Name = data.name,
                            PathDisplay = "/img/" + data.file1.FileName,
                            CreateDate = DateTime.Now,
                        });
                        await db.SaveChangesAsync();
                    }
                }
                return Redirect("/Backoffice/AddCatoryAndVideo");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete_video(int ids)
        {
            var w = await db.Video.FirstOrDefaultAsync(x => x.IdVideo == ids);
            if (w is not null)
            {
                db.Video.Remove(w);
                await db.SaveChangesAsync();
                return Json("ลบวีดีโอเรียบร้อย");
            }
            return Json("ไม่พบวีดีโอ");
        }

        [HttpPost]
        public async Task<IActionResult> Delete_video4(int ids)
        {
            var w = await db.NameVideo.FirstOrDefaultAsync(x => x.IdNameVideo == ids);
            if (w is not null)
            {
                var vi = await db.Video.Where(x => x.IdNameVideo == w.IdNameVideo).ToListAsync();
                db.Video.RemoveRange(vi);
                db.NameVideo.Remove(w);
                await db.SaveChangesAsync();
                return Json("ลบเรียบร้อย");
            }
            return Json("ไม่พบวีดีโอ");
        }

        [HttpPost]
        public async Task<IActionResult> Delete_video1(int ids)
        {
            var w = await db.VideoPage.FirstOrDefaultAsync(x => x.IdVideoPage == ids);
            if (w is not null)
            {
                db.VideoPage.Remove(w);
                await db.SaveChangesAsync();
                return Json("ลบข้อมูลเรียบร้อย");
            }
            return Json("ไม่พบข้อมูล");
        }

        public async Task<IActionResult> Delete_video2(int ids)
        {
            var w = await db.User.FirstOrDefaultAsync(x => x.IdUser == ids);
            if (w is not null)
            {
                db.User.Remove(w);
                await db.SaveChangesAsync();
                return Json("ลบข้อมูลเรียบร้อย");
            }
            return Json("ไม่พบข้อมูล");
        }

        public async Task<IActionResult> Delete_video3(int ids)
        {
            var w = await db.Payment.FirstOrDefaultAsync(x => x.IdPayment == ids);
            if (w is not null)
            {
                db.Payment.Remove(w);
                await db.SaveChangesAsync();
                return Json("ลบข้อมูลเรียบร้อย");
            }
            return Json("ไม่พบข้อมูล");
        }

        [HttpPost]
        public async Task<IActionResult> table(int ids = 0)
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var customerData = await db.NameVideo.Where(x => x.IdCatory == ids).OrderByDescending(x => x.CreateDate).ToListAsync();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    customerData = customerData.Where(x => x.Name.Contains(searchValue) || x.Name.Contains(searchValue)).ToList();
                }
                recordsTotal = customerData.Count();
                var data = customerData.Skip(skip).Take(pageSize).ToList();
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };
                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> table4(int ids = 0)
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var customerData = await db.Video.Where(x => x.IdNameVideo == ids).OrderByDescending(x => x.CreateDate).ToListAsync();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    customerData = customerData.Where(x => x.Name.Contains(searchValue) || x.Name.Contains(searchValue)).ToList();
                }
                recordsTotal = customerData.Count();
                var data = customerData.Skip(skip).Take(pageSize).ToList();
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };
                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                 throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> table1()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var customerData = await db.VideoPage.ToListAsync();
                List<VideoPages> data1 = new List<VideoPages>();
                var line = 1;
                foreach (var xc in customerData)
                {
                    data1.Add(new VideoPages
                    {
                        no = line,
                        id_videopage = xc.IdVideoPage,
                        url = xc.Path
                    });
                    line++;
                }
                recordsTotal = data1.Count();
                var data = data1.Skip(skip).Take(pageSize).ToList();
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };
                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> table2()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var customerData = await db.User.ToListAsync();
                List<Usersc> data1 = new List<Usersc>();
                var line = 1;
                foreach (var xc in customerData)
                {
                    var status = "";
                    var limit_date = "ยังไม่ซื้อแพ็คเกจ";
                    if (xc.IdRole == 1)
                    {
                        status = "ฟรี";
                    }
                    else if (xc.IdRole == 2)
                    {
                        status = "VIP";
                    }
                    else if (xc.IdRole == 3)
                    {
                        status = "แอดมิน";
                    }
                    if (xc.PacketDateLimit != null)
                    {
                        var nows = DateTime.Now;
                        var limit = xc.PacketDateLimit - nows;
                        if (limit.Value.Days < 0)
                        {
                            limit_date = "0 วัน";
                        }
                        else
                        {
                            limit_date = limit.Value.Days.ToString() + " วัน";
                        }
                    }
                    if (xc.IdRole == 3)
                    {
                        limit_date = "วันไม่จำกัด";
                    }
                    data1.Add(new Usersc
                    {
                        no = line,
                        iduser = xc.IdUser,
                        firstname = xc.FirstName,
                        surname = xc.SurName,
                        tel = xc.Tel,
                        address = xc.Address,
                        idrole = status,
                        imagepath = xc.ImagePath,
                        packetdatelimit = limit_date,
                        password = xc.Password,
                        macaddress = xc.MacAddress
                    });
                    line++;
                }
                recordsTotal = data1.Count();

                if (pageSize != -1)
                {
                    data1 = data1.Skip(skip).Take(pageSize).ToList();
                }

                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data1 };
                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                 throw new Exception(ex.Message);
            }
        }

        public async Task<IActionResult> edit_online(int ids)
        {
            var userd = await db.User.FindAsync(ids);
            userd.MacAddress = null;
            await db.SaveChangesAsync();
            return Json("Success");
        }

        [HttpPost]
        public async Task<IActionResult> table3()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var customerData = await db.Payment
                                        .OrderBy(x => x.Isactive == null)            // null ท้ายสุด
                                        .OrderByDescending(x => x.Isactive == false)           // true ก่อน false
                                        .ToListAsync();
                List<Payments> data1 = new List<Payments>();
                var line = 1;
                var user = await db.User.ToListAsync();
                foreach (var xc in customerData)
                {
                    var xx = user.FirstOrDefault(x => x.IdUser == xc.IdUser);
                    if (xx != null)
                    {
                        data1.Add(new Payments
                        {
                            no = line,
                            IdPayment = xc.IdPayment,
                            Status = xc.Status,
                            CreateDate = xc.CreateDate == null ? null : Convert.ToDateTime(xc.CreateDate).ToString("dd/MM/yyyy HH:mm:ss"),
                            UpdateDate = xc.UpdateDate == null ? null : Convert.ToDateTime(xc.UpdateDate).ToString("dd/MM/yyyy HH:mm:ss"),
                            name = (xx.FirstName == null ? "" : xx.Tel) + " " + (xx.SurName == null ? " " : " "),
                            Isactive = xc.Isactive,
                            FilePayment = xc.FilePayment
                        });
                        line++;
                    }
                }
                recordsTotal = data1.Count();
                if (pageSize != -1)
                {
                    data1 = data1.Skip(skip).Take(pageSize).ToList();
                }
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data1 };
                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
