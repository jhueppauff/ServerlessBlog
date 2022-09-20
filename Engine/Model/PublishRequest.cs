using System;

namespace ServerlessBlog.Engine
{
    public class PublishRequest
    {
        public string Slug { get; set; }

        public DateTime PublishDate { get; set; }
    }
}
