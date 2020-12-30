using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ResourceManagementImport
{
    public class LogHandler
    {
        #region Constructor

        public LogHandler(string LogPath)
        {
            try
            {
                string logFileName = string.Format(
                                            "{0}-{1}-{2}-{3}-{4}-{5}.log",
                                            DateTime.Now.Year.ToString("0000"),
                                            DateTime.Now.Month.ToString("00"),
                                            DateTime.Now.Day.ToString("00"),
                                            DateTime.Now.Hour.ToString("00"),
                                            DateTime.Now.Minute.ToString("00"),
                                            DateTime.Now.Second.ToString("00")
                                        );
                string LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogPath);
                DirectoryInfo dirInfo = new DirectoryInfo(LogFolder);
                if (!dirInfo.Exists)
                    dirInfo.Create();
                this.LogFilePath = System.IO.Path.Combine(LogFolder, logFileName);

                //  Log
                this.Log("Log created");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        #endregion
        #region Properties

        /// <summary>
        /// The full path to the logfile
        /// </summary>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// Gets a Time stamp for logging
        /// </summary>
        private string timeStamp
        { get { return string.Format("{0}:{1}:{2} ", DateTime.Now.Hour.ToString("00"), DateTime.Now.Minute.ToString("00"), DateTime.Now.Second.ToString("00")); } }

        #endregion
        #region Methods

        public void Log(string Message)
        { this.logToFile(string.Format("{0}: {1}", this.timeStamp, Message)); }

        public void Log(string Message, params object[] Params)
        { this.Log(string.Format(Message, Params)); }

        public void LogEmptyLine()
        { this.logToFile(string.Empty); }

        #endregion
        #region Private Functions

        /// <summary>
        /// Logs a message to a file on the disk.
        /// </summary>
        private void logToFile(string Message)
        {

            try
            {
                FileStream fs = new FileStream(this.LogFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    sw.WriteLine(Message);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                string eventSource = "ResourceManagement";
                if (!EventLog.SourceExists(eventSource))
                {
                    EventLog.CreateEventSource(eventSource, eventSource);
                }
                EventLog evtLog = new EventLog();
                evtLog.Source = eventSource;
                evtLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }

        #endregion
        #region Exception Handlers

        public void TraceException(FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault> ex)
        {

            this.Log("--- EXCEPTION THROWN -----------------------------------");

            this.Log("Fault Exception Thrown: {0}", ex.GetType().ToString());
            this.Log(string.Empty);
            this.Log("Message  : {0}", ex.Message);
            this.Log("Action   : {0}", ex.Action ?? "[NULL]");
            this.Log("Code     : {0}", ex.Code != null ? ex.Code.Name : "[NULL]");
            this.Log("Source   : {0}", ex.Source ?? "[NULL]");
            this.Log("Reason   : {0}", ex.Reason ?? new System.ServiceModel.FaultReason("[NULL]"));

            if (ex.Detail != null)
            {
                this.Log(string.Empty);
                this.Log("Detail:");
                this.Log("Message     : {0}", ex.Detail.Message);
                this.Log("Error Code  : {0}", ex.Detail.ErrorCode);
                this.Log("Time Stamp  : {0}", ex.Detail.Timestamp);
                this.Log("Trace Text  : {0}", ex.Detail.TraceText);
                this.Log("Details     : [EMPTY/NULL]");

            }

            if (ex.Data != null)
            {
                if (ex.Data.Count.Equals(0))
                { this.Log("Data     : [EMPTY]"); }
                else
                {
                    this.Log("Data:");
                    foreach (object key in ex.Data.Keys)
                    { this.Log("Key {0} - Value {1}", key, ex.Data[key]); }
                }
            }
            else { this.Log("Data     : [NULL]"); }
            this.Log(string.Empty);
            this.Log("Stack    : \n{0}", ex.StackTrace);
            if (ex.InnerException != null) { this.Log("Inner Exception: {0}", ex.Message); }

            //  End of log
            this.Log("--------------------------------------------------------");


        }

        public void TraceException(System.ServiceModel.FaultException ex)
        {

            this.Log("--- EXCEPTION THROWN -----------------------------------");

            this.Log("Fault Exception Thrown: {0}", ex.GetType().ToString());
            this.Log(string.Empty);
            this.Log("Message  : {0}", ex.Message);
            this.Log("Action   : {0}", ex.Action ?? "[NULL]");
            this.Log("Code     : {0}", ex.Code != null ? ex.Code.Name : "[NULL]");
            this.Log("Source   : {0}", ex.Source ?? "[NULL]");
            if (ex.Data != null)
            {
                if (ex.Data.Count.Equals(0))
                { this.Log("Data     : [EMPTY]"); }
                else
                {
                    this.Log("Data:");
                    foreach (object key in ex.Data.Keys)
                    { this.Log("Key {0} - Value {1}", key, ex.Data[key]); }
                }
            }
            else { this.Log("Data     : [NULL]"); }
            this.Log(string.Empty);
            this.Log("Stack    : \n{0}", ex.StackTrace);
            if (ex.InnerException != null) { this.Log("Inner Exception: {0}", ex.Message); }

            //  End of log
            this.Log("--------------------------------------------------------");

        }

        public void TraceException(Exception ex)
        {
            this.Log("--- EXCEPTION THROWN IN -----------------------------------");

            this.Log("Exception Thrown: {0}", ex.GetType().ToString());
            this.Log("Message: {0}", ex.Message);
            this.Log("Stack: {0}", ex.StackTrace);
            if (ex.InnerException != null) { this.Log("Inner Exception: {0}", ex.Message); }

            //  End of log
            this.Log("--------------------------------------------------------");

        }

        public void TraceEx(Exception ex)
        {
            this.Log("--- EXCEPTION THROWN IN -----------------------------------");

            this.Log("Exception Thrown: {0}", ex.GetType().ToString());
            this.Log("Message: {0}", ex.Message);
            this.Log("Stack: {0}", ex.StackTrace);
            if (ex.InnerException != null) { this.Log("Inner Exception: {0}", ex.Message); }

            //  End of log
            this.Log("--------------------------------------------------------");

        }

        #endregion

    }
}
