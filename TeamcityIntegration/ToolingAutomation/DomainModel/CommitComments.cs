using System;
using System.Collections.Generic;
using System.Text;

namespace ToolingAutomation.DomainModel
{
    public class CommitComments
    {
        public string Version { get; set; }
        public List<CommitDetail> CommitDetails { get; set; }
    }

    public class CommitDetail
    {
        public string UserName { get; set; }
        public string Comment { get; set; }
    }
}
