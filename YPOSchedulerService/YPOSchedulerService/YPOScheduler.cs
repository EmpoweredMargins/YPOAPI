//****************************************************************************************
//Page/Module Name: 				YPOScheduler.cs
//Author Name:					    Austin Amar
//Date:							    Nov 07 2013
//Purpose:						    It includes events to schedule webservice functionality to insert 
//                                  Data from Ym to Informz .
//Table referred:				    N/A 
//Table updated:					N/A
//Most Important Related Files:	    N/A
//****************************************************************************************

//Chronological Development
//Ref No   Developer Name      Date            Severity        Description
//-----------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.IO;
using System.Configuration;
using YPOSchedulerService.YPOWebService;


namespace YPOSchedulerService
{
    public partial class YPOScheduler : ServiceBase
    {
        #region Declare Constant values

        private static string strPath = AppDomain.CurrentDomain.BaseDirectory;
        private const string strExecfile = "executionLog.txt"; //To maintain the execution logs in this txt file 
        Timer objTimer = new Timer();                         //Assign memory to object of the class

        #endregion
        public YPOScheduler()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                //Add this line to text file during start of service
                executionLog("start service", strExecfile);

                //Handle elapsed event
                objTimer.Elapsed += new ElapsedEventHandler(OnElapsedTime);

                //1 minute (= 60,000 milliseconds)                
                objTimer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["scheduleInterval"].ToString()); //Set scheduleInterval time from appconfig file for scheduling Window service

                //Enable the timer
                objTimer.Enabled = true;
                string strResponse = callYPOService();
                executionLog(strResponse, strExecfile);

            }
            catch (Exception ex)
            {
                executionLog(ex.ToString(), strExecfile); //Write error into the text file 
            }
        }

        protected override void OnStop()
        {
            //Disable the timer
            objTimer.Enabled = false;
            //Add this line to text file during stop of service
            executionLog("stopping service", strExecfile);
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                executionLog("Entry at Scheduled Time: ", strExecfile); //Add this line to text file during start of OnElapsedTime Event
                string strResponse = callYPOService();              //Store webservice response to the strResponse variable
                executionLog(strResponse, strExecfile);                 //Write webservice respons into the text file
            }
            catch (Exception ex)
            {
                executionLog(ex.ToString(), strExecfile); //Write error into the text file
            }
        }

        //****************************************************************************************
        //Author Name      : Austin Amar    Date  :Nov 07 2013
        //Input Parameters : N/A
        //Purpose          : This function is used to call webservice function 
        //                   and return webservice function response.
        //****************************************************************************************

        private string callYPOService()
        {
            YPOService objYPOService = new YPOService();//Assign memory to object of the class
            objYPOService.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["webServiceResponseTime"].ToString()); //set webservice response time from appconfig file 
            string strResponse = "Synchronization Process YM to Informz: " + objYPOService.synchronizationProcessFromYMendToYPO(); //called webservice function           
            objYPOService.Dispose();
            return strResponse;

        }


        //****************************************************************************************
        //Author Name      : Austin Amar    Date  :Nov 07 2013
        //Input Parameters : strMessage =>  Pass the message(error or response message) as parameter
        //                   strFileName => Text file name pass as parameter
        //Purpose          : This function is used to write error or response message into the text file
        //****************************************************************************************

        private void executionLog(string strMessage, string strFileName)
        {
            StreamWriter objStreamWriter = null; //Declare the object of StreamWriter class 

            try
            {
                string strLogTime = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> "; //Set logtime to variable

                //Initialize memory to object of StreamWriter class for the specified file on the specified path.
                //If the file exists, it appended. If the file does not exist, this constructor creates a new file
                objStreamWriter = new StreamWriter(strPath + strFileName, true);

                objStreamWriter.WriteLine(strLogTime + strMessage); //Write error message into the text file
                objStreamWriter.Flush(); //Clears all buffers for the current writer
            }
            catch
            {

            }
            finally
            {
                if (objStreamWriter != null)
                {
                    objStreamWriter.Dispose(); //Release memory from StreamWriter object
                    objStreamWriter.Close();   //Close the current StreamWriter object  
                }
            }


        }
    }
}
