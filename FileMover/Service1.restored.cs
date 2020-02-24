using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileMover
{
    public partial class Service1 : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Info("Starting Service");
            string configFile = ConfigurationManager.AppSettings["Configuration"];
            XDocument config = new XDocument();
            List<string> MoverIds = new List<string>();
            if (File.Exists(configFile))
            {

                log.Info(string.Format("Reading Movers from {0}",configFile));
                try
                {
                    config = XDocument.Load(configFile);
                }
                catch(Exception e)
                {
                    log.Error(e.Message);
                    Stop();
                }
                try
                {
                    var movers = config.Descendants("Mover");
                    foreach (var moverEntry in movers)
                    {
                        string ID = "";
                        string srcFolder = "";
                        List<string> dstFolders = new List<string>();
                        string pollInterval = "";
                        string localMove = "false";
                        try
                        {
                            ID = (string)moverEntry.Element("id");
                        }
                        catch(NullReferenceException e)
                        {
                            log.Error("id element not found in a mover\n" + e.Message);
                            Stop();
                            break;
                        }
                        catch(Exception e)
                        {
                            log.Error(e.Message);
                        }
                        try
                        {
                            srcFolder = (string)moverEntry.Element("srcFolder");
                        }
                        catch(NullReferenceException e)
                        {
                            log.Error(ID + " - Source folder not specified\n" + e.Message);
                            Stop();
                            break;
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                        }
                        try
                        {
                            var destFolders = moverEntry.Descendants("dstFolder");
                            foreach (var folderEntry in destFolders)
                            {
                                dstFolders.Add((string)folderEntry.Value);                                
                            }
                        }
                        catch (NullReferenceException e)
                        {
                            log.Error(ID + " - Destination Folder not specified\n" + e.Message);
                            Stop();
                            break;
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                        }
                        try
                        {
                            pollInterval = (string)moverEntry.Element("pollingInterval");
                        }
                        catch (NullReferenceException e)
                        {
                            log.Error(ID + " - Polling Interval not specified\n" + e.Message);
                            Stop();
                            break;
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                        }
                        try
                        {
                            localMove = (string)moverEntry.Element("localMove");
                        }
                        catch (Exception e)
                        {
                            log.Error("localMove option missing\n" + e.Message);
                        }
                        if (!MoverIds.Contains(ID))
                        {
                            MoverIds.Add(ID);
                            Mover mover = new Mover(ID, srcFolder, dstFolders, pollInterval, localMove);
                        }
                        else
                        {
                            log.Error("Two movers have the same ID " + ID);
                            Stop();
                            break;
                        }                        
                    }                
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    Stop();
                }
            }
            else
            {
                log.Error(string.Format("Configuration file does not exist! {0}", configFile));
                Stop();
            }
        }

        protected override void OnStop()
        {
            log.Info("Stopping Service");
        }
    }
}