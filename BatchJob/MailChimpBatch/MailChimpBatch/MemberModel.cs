using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailChimpBatch
{

    public class Merge_fields
    {
        /// <summary>
        /// 
        /// </summary>
        public string FNAME { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LNAME { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string BIRTHDAY { get; set; }
    }

    public class Stats
    {
        /// <summary>
        /// 
        /// </summary>
        public int avg_open_rate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int avg_click_rate { get; set; }
    }

    public class Location
    {
        /// <summary>
        /// 
        /// </summary>
        public int latitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int longitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int gmtoff { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int dstoff { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string country_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timezone { get; set; }
    }

    public class MembersItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_address { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string unique_email_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Merge_fields merge_fields { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Stats stats { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ip_signup { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timestamp_signup { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ip_opt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timestamp_opt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int member_rating { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string last_changed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string language { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string vip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_client { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Location location { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string list_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<_linksItem> _links { get; set; }
    }

    public class _linksItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string rel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string href { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string targetSchema { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string schema { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public List<MembersItem> members { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string list_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int total_items { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<_linksItem> _links { get; set; }
    }

    public class DBParameter
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
