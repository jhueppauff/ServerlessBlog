using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Model
{
    internal class PageView
    {
        public string Slug { get; set; }

        public int Views { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
