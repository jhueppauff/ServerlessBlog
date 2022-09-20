using System;

namespace ServerlessBlog.Engine
{
    public class PageView
    {
        public string Slug { get; set; }

        public int Views { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
