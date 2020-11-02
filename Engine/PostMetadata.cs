using Microsoft.Azure.Cosmos.Table;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Text;

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

        public Date Published { get; set; }

        public List<string> Tags { get; set; }
    }
}
