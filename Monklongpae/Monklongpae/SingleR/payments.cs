using Microsoft.AspNetCore.SignalR;
using Monklongpae.Models;
using static Monklongpae.Models.oparetion;

namespace Monklongpae.SingleR
{
    public class payments : Hub
    {
        public readonly MonklongpaeContext db = new MonklongpaeContext();
        public async Task SendMessage()
        {
            List<paymentsc> data = new List<paymentsc>();
            var payment = db.Payment.Where(x=>x.Isactive == false && x.FilePayment != null).Take(10).ToList();
            foreach(var xc in payment)
            {
                var xx = db.User.Where(x => x.IdUser == xc.IdUser).FirstOrDefault();
                if (xx != null)
                {
                    if (xx.CreateDate != null)
                    {
                        var times = (DateTime.Now - xc.CreateDate);
                        var timec = times.Value.Minutes + " นาที";
                        data.Add(new paymentsc
                        {
                            name = xx.Tel,
                            time = timec
                        });
                    }
                }
            }
            await Clients.All.SendAsync("Payment", data);
        }

        public async Task UpdateData(string IdPayment)
        {
            var cm = db.Payment.Find(Convert.ToInt32(IdPayment));
            var tel = " ";
            if (cm is not null)
            {
                var xx = db.User.Where(x => x.IdUser == cm.IdUser).FirstOrDefault();
                var pd = db.Packet.Where(x => x.IdPacket == cm.IdPacket).FirstOrDefault();
                tel = xx.Tel;
            }
            await Clients.All.SendAsync("UpdateData", tel);
        }
    }
}
