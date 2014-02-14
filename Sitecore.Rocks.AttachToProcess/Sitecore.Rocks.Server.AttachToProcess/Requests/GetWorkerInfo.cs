using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Sitecore.Rocks.Server.Requests.AttachToProcess
{
    using System.IO;
    using System.Xml;

    public class GetWorkerInfo
    {
        public string Execute()
        {
            var writer = new StringWriter();
            var output = new XmlTextWriter(writer);
            output.WriteStartElement("result");

            output.WriteStartElement("machine");
            output.WriteStartElement("name");
            output.WriteValue(System.Environment.MachineName);
            output.WriteEndElement(); //name
            output.WriteEndElement(); //machine

            var process = Process.GetCurrentProcess();
            output.WriteStartElement("process");
            output.WriteStartElement("id");
            output.WriteValue(Process.GetCurrentProcess().Id);
            output.WriteEndElement(); //id
            output.WriteEndElement(); //process

            output.WriteStartElement("appPool");
            output.WriteStartElement("identity");
            output.WriteValue(WindowsIdentity.GetCurrent().Name);
            output.WriteEndElement(); //identity
            output.WriteEndElement(); //appPool

            output.WriteEndElement(); //result
            return writer.ToString();
        }
    }
}