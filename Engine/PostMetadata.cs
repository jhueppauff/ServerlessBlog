using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

namespace Engine
{
    public class PostMetadata : TableEntity
    {
        public PostMetadata()
        {
        }

        public PostMetadata(string Title)
        {
            PartitionKey = Title;
            RowKey = Title;
        }

        public string Title { get; set; }

        public string Slug { get { return this.PartitionKey; } set { this.PartitionKey = value; this.RowKey = value; } }

        public string Published { get; set; }

        public List<string> Tags { get; set; }

        public string Preview { get; set; }

        public string ImageUrl { get; set; }
    }
}
