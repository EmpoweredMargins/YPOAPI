//****************************************************************************************
//Page/Module Name: 				clsYM.cs
//Author Name:					    Reuben Ram
//Date:							    23 Oct 2013
//Purpose:						    It includes functions to access Your Membership Api 
//Table referred:				    N/A 
//Table updated:					N/A
//Most Important Related Files:	    clsCommon.cs,clsYmToDBFields.cs
//****************************************************************************************

//Chronological Development
//Ref No   Developer Name      Date            Severity        Description
//-----------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.Xml;
using YMSDK;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using YMSDK.Providers;
using System.Data.SqlClient;
using YMSDK.Entities;
using System.Xml.Linq;
using System.Net.Mail;
using YPOWebService.Classes;
using System.Text.RegularExpressions;

namespace YPOWebService
{
    class clsYM
    {
        #region Private Members Declaration

        /////////////////////////////////////// Start Block: Initialize constant values to access YM API functions. ///////////////////////////////

        // For YPO
        private string strApiKeyPublic = ConfigurationManager.AppSettings["apiKeyPublic"];    //Get ApikeyPublic from webconfig  "D2904A2F-64BD-4524-9373-BE505877E030";
        private string strApiKeySA = ConfigurationManager.AppSettings["apiKeySA"];            //Get ApikeySa from webconfig   "D30CBD5B-486F-4640-BBF3-2D68540220B0";
        private string strApiSAPasscode = ConfigurationManager.AppSettings["apiSAPasscode"];  //Get ApiSAPasscode from webconfig "3UIZa6H9sqiX";

        private const string strApiEndpoint = "https://api.yourmembership.com/";
        private const string strApiVersion = "2.00";
        private const string strApiCallOrigin = "YMSDK Project";

        private string strTestingStatus = ConfigurationManager.AppSettings["Testing"];  //Testing purpose 
        private int intTestingRecords = Convert.ToInt32(ConfigurationManager.AppSettings["TestingRecords"]);  //Testing purpose

        private string strYMUID = ConfigurationManager.AppSettings["UID"];  //Get YM User id from webconfig 
        private string strYMPwd = ConfigurationManager.AppSettings["Pwd"];  //Get YM password  from webconfig 

        private static string strPath = HttpContext.Current.Request.PhysicalApplicationPath;
        private const string strLogfile = "errorLog.txt"; //To maintain the error logs in this file 

        private int intBackDays = Convert.ToInt32(ConfigurationManager.AppSettings["BackDays"]);  //Set how many days back to current date data synchronize

        //////////////////////////////////////////////////////////////////   End Block  ///////////////////////////////////////////////////////////////       

        XmlHttpProvider objProvider;           //Declare the XmlHttpProvider Object
        ApiManager objManager;                 //Declare the ApiManager Object   
        ApiResponse objResponse;               //Declare the ApiResponse Object
        private bool boolApiSessionInitialized = false; //Declare bool variable for Session initialize or not

        #endregion

        #region API Helper Methods

        //****************************************************************************************
        //Author Name      : Austin Amar        Date : 23 Oct 2013
        //Input Parameters : N/A 
        //Purpose          : This method is used to initialize the provider of YM API with CLA constant values.
        //****************************************************************************************

        private void initializeProvider()
        {
            objProvider = new XmlHttpProvider(strApiEndpoint); //Assign Memory to object of XmlHttpProvider for APIEndpoint 
            objManager = new ApiManager(objProvider);         //Assign Memory to object of ApiManager  

            objManager.Version = strApiVersion;
            objManager.ApiKeyPublic = strApiKeyPublic;
            objManager.ApiKeySa = strApiKeySA;
            objManager.SaPasscode = strApiSAPasscode;
            objManager.CallOrigin = strApiCallOrigin;

        }

        //****************************************************************************************
        //Author Name      : Austin Amar        Date : 23 Oct 2013
        //Input Parameters : N/A 
        //Purpose          : This method used to initialize the Session of YM API for any user.
        //****************************************************************************************

        private void initializeAPISession()
        {
            if (!boolApiSessionInitialized)
            {
                ApiResponse response = objManager.SessionCreate(); //create session for user with the help of API function 

                boolApiSessionInitialized = true;
            }
        }

        //****************************************************************************************
        //Author Name      : Austin Amar        Date : 23 Oct 2013
        //Input Parameters : N/A 
        //Purpose          : This method is used to destroy the Session of YM API for user
        //****************************************************************************************

        private void abandonSession()
        {
            ApiResponse response = objManager.SessionAbandon(); //dispose the session for user
            boolApiSessionInitialized = false;
        }

        #endregion

        #region Public funtions

        //****************************************************************************************
        //Author Name      : Austin Amar    Date  :23 Oct 2013
        //Input Parameters : strErrorMessage => strErrorMessage as a parameter
        //Purpose          : This function is used to write error message into the text file
        //****************************************************************************************

        public void errorLog(string strErrorMessage)
        {
            StreamWriter objStreamWriter = null; //Declare the object of StreamWriter class 
            try
            {
                string strLogTime = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> "; //Set logtime to variable
                //Initialize memory to object of StreamWriter class for the specified file on the specified path.
                //If the file exists, it appended. If the file does not exist, this constructor creates a new file
                objStreamWriter = new StreamWriter(strPath + strLogfile, true);
                objStreamWriter.WriteLine(strLogTime + strErrorMessage); //Write error message into the text file
                objStreamWriter.Flush(); //Clears all buffers for the current writer
            }
            catch
            {
            }
            finally
            {
                if (objStreamWriter != null)
                {
                    objStreamWriter.Dispose();    //Release memory from StreamWriter object.
                    objStreamWriter.Close();      //Close the current StreamWriter object.
                }
            }
        }

        //********************************************************************************************************************
        //Author Name      : Reuben Ram   Date  :23 Oct 2013
        //Input Parameters : N/A 
        //Purpose          : This function is used to add subscriber to informz recipient list.
        //********************************************************************************************************************

        public string addSubscriberToInformz()
        {
            string strResponse = string.Empty;
            string strYmEventName = string.Empty;
            string strInformzEventName = string.Empty;
            clsInformz objInformz = new clsInformz();
            List<String> lstInformzEventName = new List<String>();
            List<String> lstYmRegistrationIds = new List<String>();
            List<String> lstYmEventRegistratians = new List<String>();

            try
            {
                initializeProvider();   // intialize the YM API with constant values.
                initializeAPISession();  // intialize the YM API Session.

                DateTime dtStartDate = DateTime.Now.AddDays(-1); // Set start date to yesterday. 

                // To get only upcomming event IDs from YM.
                objResponse = objManager.SaEventGetIDs(new DateTime(dtStartDate.Year, dtStartDate.Month, dtStartDate.Day), new DateTime(DateTime.Now.Year + 2, 1, 1), "");
                IEnumerable<DataItem> lstEventIds = objResponse.MethodResults.GetNamedItems("EventID").AsEnumerable(); // Store event id of upcomming Events.

                if (lstEventIds.Count() > 0) //Checking EventId list is not empty.
                {
                    lstInformzEventName = objInformz.getMailingTemplateFromInformz();  //Get Event name from informz.

                    if (lstInformzEventName.Count() > 0)  // Check if event name list is not empty.
                    {
                        foreach (DataItem item in lstEventIds)  // traverse events list.
                        {
                            int intEventID = Convert.ToInt32(item.Value);
                            lstYmRegistrationIds = getRegistrationIdFromEventsId(intEventID); //get the registration ids based on eventid.
                            if (lstYmRegistrationIds.Count > 0)    // Check if there is registration id in event.
                            {
                                foreach (string strRegistrationId in lstYmRegistrationIds)
                                {
                                    // Get Event Registration details and sessions for the provided Event and Event Registration ID.
                                    objResponse = objManager.SaEventsEventRegistrationGet(intEventID, strRegistrationId, null);
                                    if (objResponse.ErrorCode == ApiErrorCode.NoError)  // Check if there is no error.
                                    {
                                        ApiMethodResults objRegistrationDetail = objResponse.MethodResults;
                                        if (objRegistrationDetail.Items.Count > 0)
                                        {
                                            XmlDocument objXmlDoc = new XmlDocument();
                                            objXmlDoc.LoadXml("<parent>"+ Convert.ToString(objRegistrationDetail.ValueRaw)+"</parent>");
                                            DataTable objDtRegistrantSession = new DataTable();    // Create datatable object for storing registrant details.
                                            objDtRegistrantSession = SessionDataTable("<XmlDS><parent>" + Convert.ToString(objRegistrationDetail.ValueRaw) + "</parent></XmlDS>");   // Get data from xml response.
                                            // Check if datatable contains session 
                                            if (objDtRegistrantSession.Rows.Count > 0)
                                            {
                                                DataRow objRows = objDtRegistrantSession.Rows[0];  // Get first column of data row
                                                // Traverse datarows
                                                foreach (string strSessionName in objRows.ItemArray)
                                                {
                                                    // Check if session name in YM equals to informz email template
                                                    if (lstInformzEventName.Contains(Convert.ToString(strSessionName)))
                                                    {
                                                        string strEmailAddress = string.Empty;
                                                        string strBadgeNumber = string.Empty;

                                                        strEmailAddress = Convert.ToString(objXmlDoc.GetElementsByTagName("strEmail").Item(0).InnerText);
                                                        strBadgeNumber = Convert.ToString(objXmlDoc.GetElementsByTagName("BadgeNumber").Item(0).InnerText);
                                                        // Check if email address and badge number is null
                                                        if (!string.IsNullOrEmpty(strEmailAddress) && checkEmail(strEmailAddress))  //check if email address is not empty or valid.
                                                        {
                                                            //strResponse += objInformz.addSubscriberToInformzList(strEmailAddress, strBadgeNumber);
                                                            strResponse += objInformz.addSubscriberToInformzList(strEmailAddress, strBadgeNumber, Convert.ToString(strSessionName));
                                                        }
                                                        else
                                                        {
                                                            strResponse += "Email Address is not valid or Email Address is not exits in addSubscriberToInformz function." + Environment.NewLine;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        strResponse += "Due to some problem this error occured: " + objResponse.ErrorMessage + "in addSubscriberToInformz function." + Environment.NewLine;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                strResponse += "Due to some problem this error occured: " + ex.Message + "in addSubscriberToInformz function." + Environment.NewLine;
                errorLog(ex.Message);
            }
            objInformz = null;
            lstInformzEventName = null;
            lstYmRegistrationIds = null;
            lstYmEventRegistratians = null;
            abandonSession();
            return strResponse;
        }

        //****************************************************************************************
        //Author Name      : Reuben Ram   Date  :23 Oct 2013
        //Input Parameters : intEventId=> intEventId pass as parameter
        //Purpose          : This function is used to get events name based on event id
        //****************************************************************************************

        public string getEventsFromEventsId(int intEventId)
        {
            string strResponse = string.Empty;
            objResponse = objManager.EventsEventGet(intEventId);
            if (objResponse.ErrorCode == ApiErrorCode.NoError)
            {
                strResponse = Convert.ToString(objResponse.MethodResults.GetNamedItem("Name").Value);
            }
            else
            {
                strResponse += "Due to some problem this error occured: " + objResponse.ErrorMessage + "in addSubscriberToInformz function. ";
            }
            return strResponse;
        }


        //****************************************************************************************
        //Author Name      : Reuben Ram   Date  :23 Oct 2013
        //Input Parameters : intEventId=> intEventId pass as parameter
        //Purpose          : This function is used to get Registration ids based on event id
        //****************************************************************************************

        public List<string> getRegistrationIdFromEventsId(int intEventId)
        {
            List<string> listRegistrationId = new List<string>(); 
            objResponse = objManager.SaEventsEventRegistrationsGetIDs(intEventId);
            if (objResponse.ErrorCode == ApiErrorCode.NoError)
            {
                ApiMethodResults objMethodResponse = objResponse.MethodResults;
                if (objMethodResponse.Items.Count > 0)
                {
                    foreach (DataItem diRegestrationId in objMethodResponse.Items)
                    {
                        listRegistrationId.Add(diRegestrationId.Value);
                    }
                }
            }
            else
            {
                // Not required. error will be handled by main function(addSubscriberToInformz) catch block.
            }
            return listRegistrationId;
        }

        //****************************************************************************************
        //Author Name      : Reuben Ram   Date  :24 Oct 2013
        //Input Parameters : strEmailAddress=> strEmailAddress pass as parameter
        //Purpose          : This function is used to check is email address is valid or not. 
        //****************************************************************************************

        public bool checkEmail(string strEmailAddress)
        {
            // regular expression for email address.
            string patternStrict = @"^(([^<>()[\]\\.,;:\s@\""]+"
                                   + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@"
                                   + @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                                   + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+"
                                   + @"[a-zA-Z]{2,}))$";

            Regex reStrict = new Regex(patternStrict);                // Create object of Regex and assign regular expression to Regex object.     
            bool isStrictMatch = reStrict.IsMatch(strEmailAddress);   // Check regular expression to check Email Address is valid or not. 
            reStrict = null;
            return isStrictMatch;
        }

        //****************************************************************************************
        //Author Name      : Reuben Ram   Date  :24 Oct 2013
        //Input Parameters : strXml=> strXml pass as parameter
        //Purpose          : This function is used to parse xml and assign data to datatable. 
        //****************************************************************************************

        public DataTable SessionDataTable(string strXml)
        {
            DataSet objDataSet = new DataSet();
            DataTable objDataTable = new DataTable("Session");
            objDataTable.Columns.Add("SessionName", typeof(string));      // Creating column in datatable for session.
            objDataSet.Tables.Add(objDataTable);                          // Add datatable to data set.
            StringReader objXmlSR = new StringReader(strXml);             // create object of string reader 
            objDataSet.ReadXml(objXmlSR, XmlReadMode.IgnoreSchema);       // Read the xml from string.

            /****************************** This Block of code is used to remove empty row from the data table ************************************/
            for (int h = 0; h < objDataTable.Rows.Count; h++)
            {
                if (objDataTable.Rows[h].IsNull(0) == true)
                {
                    objDataTable.Rows[h].Delete();
                }
            }
            /*************************************************************** End Block *************************************************************/
            objXmlSR.Dispose();
            return objDataTable;
        }

       #endregion
    }
}
