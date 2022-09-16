using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorNG.Models
{
    internal class PublishRequest
    {
        public string Slug { get; set; }

        public TimeSpan Delay { get; set; }
    }
}
