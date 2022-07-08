using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveApi.LinkParsing
{
    public class WebLink
    {
        public Uri Link { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public IEnumerable<string> KeyList { get => Parameters.Keys; }
        public WebLink(Uri link, Dictionary<string, string> parameters)
        {
            Link = link;
            Parameters = parameters;
        }
        public WebLink(Uri link)
        {
            Link = link;
        }
    }
}
