using System.Collections.Generic;

namespace Discord_Member_Check
{
    public class Source
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class Metadata
    {
        public bool primary { get; set; }
        public Source source { get; set; }
    }

    public class Name
    {
        public Metadata metadata { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string displayNameLastFirst { get; set; }
        public string unstructuredName { get; set; }
    }

    public class Photo
    {
        public Metadata metadata { get; set; }
        public string url { get; set; }
    }

    public class GoogleJson
    {
        public string resourceName { get; set; }
        public string etag { get; set; }
        public List<Name> names { get; set; }
        public List<Photo> photos { get; set; }
    }

    public class PageInfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }

    public class Item
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
    }

    public class YoutubeChannelMeJson
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<Item> items { get; set; }
    }
}
