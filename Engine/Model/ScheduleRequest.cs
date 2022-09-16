using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Model
{
    internal class ScheduelRequest
    {
        public string Slug { get; set; }

        public TimeSpan Delay { get; set; }
    }
}
