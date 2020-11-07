using System;

namespace TomTelegramBot.Video
{
    public class VideoJson
    {
        public VideoJson(int vodId = 0, string uuid = "0", string startedBy = "0", DateTime date = new DateTime(), string state = "0", string user = "0")
        {
            this.date = date;
            this.vodId = vodId;
            this.uuid = uuid;
            this.startedBy = startedBy;
            this.date = date;
            this.state = state;
            this.user = user;
        }

        public int vodId { get; set; }
        public string uuid { get; set; }
        public string startedBy { get; set; }
        public DateTime date { get; set; }
        public string state { get; set; }
        public string user { get; set; }
    }
}