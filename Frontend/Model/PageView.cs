using System;
using Microsoft.Azure.Cosmos.Table;

namespace ServerlessBlog.Frontend.Model
{
    public class PageView : TableEntity
    {
        public PageView()
        {
        }

        public PageView(string slug)
        {
            PartitionKey = slug;
            RowKey = DateTime.UtcNow.ToString("yyyyMMdd:mm:ss");
        }
    }
}
