using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushSaaS
{
    public class Datasets
    {
        public dataset[] value { get; set; }
    }

    public class dataset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool AddRowsAPIEnabled { get; set; }
    }

    public class Tables
    {
        public table[] value { get; set; }
    }

    public class table
    {
        public string Name { get; set; }
    }

    public class Reports
    {
        public report[] value { get; set; }
    }

    public class report
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string WebUrl { get; set; }
        public string EmbedUrl { get; set; }

    }

    public class Imports
    {
        public import[] value { get; set; }
    }

    public class import
    {
        public string Id { get; set; }
        public string ImportState { get; set; }
        public string Name { get; set; }
        public string CreatedDateTime { get; set; }
        public string ImportSource { get; set; }
        public report[] Reports { get; set; }
        public dataset[] Datasets { get; set; }
    }

    public class Dashboards
    {
        public dashboard[] value { get; set; }
    }

    public class dashboard
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }

    public class Tiles
    {
        public tile[] value { get; set; }
    }

    public class tile
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string EmbedUrl { get; set; }
    }

    public class Groups
    {
        public group[] value { get; set; }
    }

    public class group
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IsReadOnly { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsCompete { get; set; }
        public DateTime ManufacturedOn { get; set; }
    }

    public class Product2
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsCompete { get; set; }
        public DateTime ManufacturedOn { get; set; }
        public string NewColumn { get; set; }
    }

    public class SequanceRequestJson
    {
        public SequenceRequest[] value { get; set; }
    }

    public class SequenceRequest
    {
        public int clientId { get; set; }
        public int sequenceNumber { get; set; }
    }
}
