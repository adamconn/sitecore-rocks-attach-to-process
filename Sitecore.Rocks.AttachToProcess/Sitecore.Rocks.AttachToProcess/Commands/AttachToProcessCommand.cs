using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using EnvDTE;
using Sitecore.Rocks.AttachToProcess.Responses;
using Sitecore.VisualStudio.Commands;
using Sitecore.VisualStudio.ContentTrees;
using Sitecore.VisualStudio.ContentTrees.Items;
using Sitecore.VisualStudio.Data;
using Sitecore.VisualStudio.Data.DataServices;
using Sitecore.VisualStudio.Extensions.StringExtensions;

namespace Sitecore.Rocks.AttachToProcess.Commands
{
    [Command(Submenu = "Web Server")]
    public class AttachToProcessCommand : CommandBase
    {
        public AttachToProcessCommand()
        {
            base.Text = "Attach to Process";
            base.Group = "Debug";
            base.Icon = new Icon("resources/processwindow.png");
            base.SortingValue = 100;
        }
        public override bool CanExecute(object parameter)
        {
            var context = parameter as ContentTreeContext;
            if (context == null)
            {
                return false;
            }

            return true;
        }

        protected virtual GetWorkerInfoResponse ParseResponse(string response)
        {
            var obj = new GetWorkerInfoResponse();
            var element = response.ToXElement();
            if (element != null)
            {
                var machine = element.Element("machine");
                if (machine != null)
                {
                    var machineName = machine.Element("name");
                    if (machineName != null)
                    {
                        obj.MachineName = machineName.Value;
                    }
                }
                var process = element.Element("process");
                if (process != null)
                {
                    var id = process.Element("id");
                    if (id != null)
                    {
                        var pid = 0;
                        int.TryParse(id.Value, out pid);
                        obj.ProcessId = pid;
                    }
                }
            }
            return obj;
        }
        
        protected virtual void HandleResult(string response, ExecuteResult result)
        {
            if (!DataService.HandleExecute(response, result))
            {
                return;
            }
            var info = ParseResponse(response);
            var dte2 = GetCurrent();
            var debugger = (EnvDTE80.Debugger2)dte2.Debugger;
            var transport = debugger.Transports.Item("Default");
            var processes = debugger.GetProcesses(transport, info.MachineName);
            var proc = processes.OfType<EnvDTE80.Process2>().SingleOrDefault(p => p.ProcessID == info.ProcessId);
            if (proc == null)
            {
                return;
            }
            if (debugger.DebuggedProcesses != null && debugger.DebuggedProcesses.Count > 0)
            {
                debugger.DetachAll();
            }
            if (proc.IsBeingDebugged)
            {
                var msg = string.Format("A debugger is already attached to process {0} on machine {1}.", info.ProcessId, info.MachineName);
                AppHost.MessageBox(msg, "Unable to connect to process", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                proc.Attach();
            }
            catch (COMException ex)
            {
                AppHost.MessageBox(
                    "Unable to connect to the process. Another Visual Studio instance may already be connected.",
                    "Unable to connect to process", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public override void Execute(object parameter)
        {
            var context = parameter as ContentTreeContext;
            if (context == null)
            {
                return;
            }
            var item = context.SelectedItems.FirstOrDefault<BaseTreeViewItem>() as SiteTreeViewItem;
            if (item == null)
            {
                return;
            }
            var site = item.Site;
            if (site == null)
            {
                return;
            }
            site.DataService.ExecuteAsync("Sitecore.Rocks.Server.Requests.AttachToProcess.GetWorkerInfo,Sitecore.Rocks.Server.AttachToProcess", this.HandleResult);
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern void GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
        internal static DTE GetCurrent()
        {
            var rotEntry = String.Format(":{0}", System.Diagnostics.Process.GetCurrentProcess().Id);
            IRunningObjectTable rot;
            GetRunningObjectTable(0, out rot);
            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            enumMoniker.Reset();
            var fetched = IntPtr.Zero;
            var moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);
                if (displayName.EndsWith(rotEntry))
                {
                    object comObject;
                    rot.GetObject(moniker[0], out comObject);
                    return (DTE)comObject;
                }
            }
            return null;
        }        
    }
}
