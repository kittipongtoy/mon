namespace Monklongpae.Models
{
    public class oparetion
    {
        public class Users
        {
            public string? FirstName { get; set; }
            public string? SurName { get; set; }
            public string? Tel { get; set; }
            public string? Password { get; set; }
            public string? Address { get; set; }
            public DateTime? UpdateDate { get; set; }
            public IFormFile? Images { get; set; }
        }

        public class VideosandCatory
        {
            public string? catory { get; set; }
            public List<NameVideo>? videos { get; set; }
            public string? timeVideo { get; set; }
        }

        public class table1
        {
            public string? datetime { get; set; }
            public string? catory { get; set; }
            public string? name { get; set; }
            public string? timeclip { get; set; }
            public int? views { get; set; }
        }
        public class upload_filevedio
        {
            public IFormFile? file1 { get; set; }
            public IFormFile? file { get; set; }
            public string? name { get; set; }
            public string? description { get; set; }
            public int? idnameVideo { get; set; }
            public int? timefree { get; set; }
            public int? idvideo { get; set; }
        }

        public class upload_filevedio1
        {
            public IFormFile? file1 { get; set; }
            public int? nameVideo { get; set; }
            public string? name { get; set; }
            public string? description { get; set; }
            public int? idCatory { get; set; }
        }
        public class upload_filevedio2
        {
            public IFormFile? file1 { get; set; }
            public int? nameVideo { get; set; }
            public string? name { get; set; }
            public string? description { get; set; }
            public int? idVideoName { get; set; }
        }
        public class VideoPages
        {
            public int? no { get; set; }
            public int? id_videopage { get; set; }
            public string? url { get; set; }

        }
        public class Usersc
        {
            public int? no { get; set; }
            public int? iduser { get; set; }
            public string? firstname { get; set; }
            public string? surname { get; set; }
            public string? tel { get; set; }
            public string? password { get; set; }
            public string? address { get; set; }
            public string? createdate { get; set; }
            public string? idrole { get; set; }
            public string? imagepath { get; set; }
            public string? packetdatelimit { get; set; }
            public string? macaddress { get; set; }
        }

        public class Payments
        {
            public int? no { get; set; }
            public int? IdPayment { get; set; }
            public string? FilePayment { get; set; }
            public string? Status { get; set; }
            public string? CreateDate { get; set; }
            public string? UpdateDate { get; set; }
            public string? name { get; set; }
            public string? IdPacket { get; set; }
            public bool? Isactive { get; set; }
        }
        public class paymentsc
        {
            public string? name { get; set; }
            public string? time { get; set; }
        }

        public class history_video
        {
            public string? imgs { get; set; }
            public int? idvideo { get; set; }
            public int? idnamevideo { get; set; }
            public string? name { get; set; }
            public string? time { get; set; }
            public string? detail { get; set; }
            public string? status { get; set; }
        }


        public class byteark
        {
            public string? key { get; set; }
            public string? embeddedUrl { get; set; }
            public primaryPlaybackUrl? primaryPlaybackUrl { get; set; }
        }

        public class primaryPlaybackUrl
        {
            public string? name { get; set; }
            public List<hls>? hls { get; set; }
        }

        public class hls
        {
            public string? url { get; set; }
        }


        public class product
        {
            public IFormFile? file { get; set; }
            public string? url { get; set; }
            public int? timeshow { get; set; }
            public int? minshow { get; set; }
            public int? ids { get; set; }
        }

        public class marketdetail
        {
            public List<marketall>? marketalls { get; set; }
            public int? IdVideo { get; set; }
        }
        public class marketall
        {
            public IFormFile? images { get; set; }
            public string? url { get; set; }
            public double? Duration { get; set; }
            public double? Showtime { get; set; }
        }
    }
}
