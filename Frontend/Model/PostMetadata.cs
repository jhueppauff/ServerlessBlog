﻿namespace ServerlessBlog.Frontend.Model
{
    public class PostMetadata
    {
        public PostMetadata()
        {
        }

        public PostMetadata(string Title)
        {
            PartitionKey = Title;
            RowKey = Title;
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Title { get; set; }

        public string Published { get; set; }

        public string Tags { get; set; }

        public string Preview { get; set; }

        public string ImageUrl { get; set; }

        public bool IsPublic { get; set; }
    }
}
