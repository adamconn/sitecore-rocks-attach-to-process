using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.Rocks.AttachToProcess.Responses
{
    public class GetWorkerInfoResponse
    {
        public string MachineName { get; set; }
        public int ProcessId { get; set; }
    }
}
