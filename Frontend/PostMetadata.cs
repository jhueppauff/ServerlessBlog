using Microsoft.Azure.Cosmos.Table;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessBlog.Frontend
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

        public string Published { get; set; }

        public List<string> Tags { get; set; }

        public string Preview { get; set; }
    }
}
