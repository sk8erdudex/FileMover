using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FileMover
{
    class Mover
    {
        /*
         * sourceFolder: This is the folder that will contain the files you would like to have moved
         * destFolder: This is the folder that files from sourceFolder will move to
         * pollingInterval: This is the frequency(in milliseconds) in which the source folder will be checked for files needing to be moved         
         */
        string sourceFolder;
        List<string> destFolders = new List<string>();
        string pollingInterval;
        string ID;
        bool sourceLock = false;
        bool localMove = false;
        Timer timer = new Timer();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public Mover(string id, string srcFolder, List<string> dstFolders, string poll, string local)
        {
            log.Info(string.Format("Creating Mover with the following params\n\tID:{0}\n\tSource Folder:{1}\n\tPolling Interval:{2}ms\n\tLocal move set to: {3}\n\tWith the following Destination Folders:", id, srcFolder, poll, local));
            foreach (string folder in dstFolders)
            {
                log.Info(folder);
            }
            ID = id;
            sourceFolder = srcFolder;
            destFolders = dstFolders;
            pollingInterval = poll;
            localMove = bool.Parse(local);
            //Initialize the timer for this object
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            timer.Interval = double.Parse(pollingInterval);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string[] files = { };
            log.Debug(ID + " - Checking Source lock");
            if (!sourceLock)
            {
                log.Debug(ID + " - Source lock false");
                log.Debug(ID + " - Checking if source folder exists");
                if (Directory.Exists(sourceFolder))
                {
                    log.Debug(ID + " - Source folder exists");
                    try
                    {
                        log.Debug(ID + " - Setting source lock");
                        sourceLock = true;
                        log.Debug(ID + " - Getting filenames from source");                        
                        files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);                        
                    }
                    catch(Exception error)
                    {
                        log.Error(ID + " - Unable to get filenames from source");
                        log.Error(error.Message);
                        log.Debug(ID + " - Unsetting source lock");
                        sourceLock = false;
                        return;
                    }
                    log.Debug(ID + " - Checking if file count is greater than 0");
                    if (files.Count<string>() > 0)
                    {
                        log.Debug(string.Format(ID + " - File count is {0}", files.Count<string>()));
                        log.Debug(ID + " - Setting Source Lock to true");
                        sourceLock = true;
                        log.Debug(string.Format(ID + " - Beginning file iteration of {0}", sourceFolder));
                        foreach (string file in files)
                        {
                            log.Debug(string.Format(ID + " - Full path is {0}", file));
                            string fileName = Path.GetFileName(file);
                            log.Debug(string.Format(ID + " - Filename is {0}", fileName));
                            string tempFile = file;
                            bool success = true;
                            foreach (string rootFolder in destFolders)
                            {
                                string destFolder = "";
                                string destFile = "";
                                
                                try
                                {
                                    destFolder = Path.Combine(rootFolder, Path.GetDirectoryName(file).Substring(sourceFolder.Length + 1));
                                    destFile = Path.Combine(destFolder, fileName);
                                }
                                catch(Exception error)
                                {
                                    log.Error(ID + " - " + error.Message);
                                    success = false;
                                    break;
                                }
                                log.Debug(string.Format(ID + " - Destination filename is {0}", destFile));
                                log.Debug(ID + " - Checking if destination folder exists");
                                if (!Directory.Exists(destFolder))
                                {
                                    log.Debug(string.Format(ID + " - Destination folder {0} did not exist.  Creating folder now", destFolder));
                                    try
                                    {
                                        Directory.CreateDirectory(destFolder);
                                    }
                                    catch(Exception error)
                                    {
                                        log.Error(string.Format(ID + " - Unable to create directory: {0}\n\t" + error.Message, destFolder));
                                        break;
                                    }
                                }
                                try
                                {
                                    log.Debug(string.Format(ID + " - Beginning transfer of {0} to {1}", tempFile, destFile));
                                    File.Copy(tempFile, destFile, true);
                                    if(localMove)
                                    {
                                        tempFile = destFile;
                                    }
                                }
                                catch (Exception error)
                                {
                                    log.Error(ID + " - " + error.Message);
                                    success = false;
                                    break;                                    
                                }
                                log.Info(string.Format(ID + " - Successfully copied file {0} to {1}", fileName, destFile));
                            }
                            if (success)
                            {
                                log.Debug(string.Format(ID + " - Deleting file {0}", file));
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception error)
                                {
                                    log.Warn(ID + " - Error deleting file: " + error.Message);
                                }
                            if (Directory.GetFiles(Path.GetDirectoryName(file),"*",SearchOption.AllDirectories).Count() == 0 && Path.GetDirectoryName(file) != sourceFolder)
                                {
                                    log.Debug(string.Format(ID + " - Source Folder has had all files copied, removing folder {0}", Path.GetDirectoryName(file)));
                                    try
                                    {
                                        Directory.Delete(Path.GetDirectoryName(file), true);
                                    }
                                    catch(Exception error)
                                    {
                                        log.Warn(ID + " - Error deleting folder: " + error.Message);
                                    }
                                }
                            }                            
                        }
                        if (sourceLock)
                        {
                            log.Debug(ID + " - Unsetting source lock");
                            sourceLock = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories).Count() > 0)
                            {
                                foreach (string dir in Directory.GetDirectories(sourceFolder))
                                {
                                    if (Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Count() == 0)
                                    {
                                        log.Info(string.Format(ID + " - Found empty directory, cleaning up directory {0}", dir));
                                        Directory.Delete(dir, true);
                                    }
                                }
                            }
                        }
                        catch(DirectoryNotFoundException error)
                        {
                            log.Warn(error.Message);
                            if(sourceLock)
                            {
                                log.Debug(ID + " - Unsetting source lock");
                                sourceLock = false;
                            }
                        }
                        catch(Exception error)
                        {
                            log.Debug(ID + " - Folder cleanup error\t" + error.Message);
                            if (sourceLock)
                            {
                                log.Debug(ID + " - Unsetting source lock");
                                sourceLock = false;
                            }
                        }
                        log.Info(ID + " - No files to move");
                        if (sourceLock)
                        {
                            log.Debug(ID + " - Unsetting source lock");
                            sourceLock = false;
                        }
                    }
                }
                else
                {
                    log.Error(string.Format(ID + " - Source folder {0} does not exist", sourceFolder));
                }
            }
            else
            {
                log.Debug(ID + " - Source lock true, file transfer may be in progress");
            }
        }
    }
}
