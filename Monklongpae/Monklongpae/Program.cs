using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Helper;
using Monklongpae.Models;
using Monklongpae.SingleR;
using NToastNotify;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews().AddNToastNotifyNoty();


builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IStrogeManage, StrogeManage>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Monklongpae.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30); // เพิ่ม timeout ได้
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(System.Net.IPAddress.Parse("Your IP Address"));
});


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("https://monklongpae.com/",
                                              "https://orangetech-0jc76h.cdn.byteark.com/",
                                              "https://localhost:44393");
                      });
});


builder.Services.AddDbContext<MonklongpaeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    //builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseNToastNotify();

app.UseRouting();

app.UseSession(); // ✅ ต้องมาก่อน middleware ที่ใช้ Session

app.UseMiddleware<ActivityTrackingMiddleware>(); // ✅ ย้ายมาตรงนี้

app.UseAuthorization();


app.UseCookiePolicy();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<payments>("/getpayment");
app.MapFallbackToFile("/React/index.html");


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonklongpaeContext>();

    var users = await db.User.Where(u => u.IsOnline == true || u.Token != null || u.WorkDate != null)
        .Select(x=> new User
        { 
            Address = x.Address,
            SurName = x.SurName,
            OnlineStatus = x.OnlineStatus,
            CreateDate = x.CreateDate,
            FirstName = x.FirstName,
            IdRole = x.IdRole,
            IdUser = x.IdUser,
            ImagePath = x.ImagePath,
            Isactive = x.Isactive,
            MacAddress = x.MacAddress,
            PacketDateLimit = x.PacketDateLimit,
            Password = x.Password,
            Tel = x.Tel,
            UpdateDate = x.UpdateDate,

            IsOnline = null,
            Token = null,
            WorkDate = null
        }).ToListAsync();
    db.User.UpdateRange(users);

    var payment = await db.Payment.Where(x => x.Isactive == false && x.CreateDate != null && x.CreateDate.Value.AddMinutes(30) <= DateTime.Now)
    .Select(x => new Payment
    {
        CreateDate = x.CreateDate,
        Status = "หมดเวลา การขำระเงิน",
        FilePayment = x.FilePayment,
        IdPacket = x.IdPacket,
        IdUser    = x.IdUser,
        Isactive = null,
        UpdateDate = x.UpdateDate,
        IdPayment = x.IdPayment,
    }).ToListAsync();

    db.Payment.UpdateRange(payment);

    await db.SaveChangesAsync();
}

app.Run();
