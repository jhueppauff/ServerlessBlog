using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessBlog.Engine
{
    public class PublishRequest
    {
        public string Slug { get; set; }

        public DateTime PublishDate { get; set; }
    }
}
