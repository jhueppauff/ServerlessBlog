using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Constants
{
    public static class ServiceBusQueueNames
    {
        public const string NewBlogPostQueue = "created";
        public const string PublishBlogPostQueue = "scheduled";
    }
}
