using System;

namespace ServerlessBlog.Engine.Model
{

    public class Metric
    {
        /// <summary>
        /// Slug
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Session Id
        /// </summary>
        public string Slug { get; set; }

        public DateTime Timestamp { get; set; }
    }
}