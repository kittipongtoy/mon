using System;
using Microsoft.EntityFrameworkCore;
using Monklongpae.Models;

namespace Monklongpae.Helper
{
    public class ActivityTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityTrackingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, MonklongpaeContext db)
        {
            if (context.Session != null && context.Session.Keys.Contains("Tel"))
            {
                var tel = context.Session.GetString("Tel");

                if (!string.IsNullOrEmpty(tel))
                {
                    var user = await db.User.FirstOrDefaultAsync(u => u.Tel == tel);
                    if (user != null)
                    {
                        user.WorkDate = DateTime.Now;
                        await db.SaveChangesAsync();
                    }
                }
            }            
            await _next(context);
        }
    }
}
