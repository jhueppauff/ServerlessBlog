using System.Collections.Generic;

namespace EditorNG.Models
{
    public class PostMetadata 
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Title { get; set; }

        public string Slug { get { return this.PartitionKey; } set { this.PartitionKey = value; this.RowKey = value; } }

        public string Published { get; set; }

        public List<string> Tags { get; set; }

        public string Preview { get; set; }

        public string ImageUrl{ get; set; }
    }
}
