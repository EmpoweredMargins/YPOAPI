//****************************************************************************************
//Page/Module Name            :   clsInformz.cs
//Author Name                 :	  Reuben Ram
//Date                        :	  23 Oct 2013
//Purpose                     :	  This class includes methods to access and insert records with the help of Informz Api 
//Table referred              :	  N/A 
//Table updated               :	  N/A
//Most Important Related Files:	  clsCommon.cs,clsYmToDBFields.cs
//****************************************************************************************
//Chronological Development
//Ref No   Developer Name      Date            Severity        Description
//-----------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using YPOWebService.InformzService;
using System.Xml;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Xml.Linq;

namespace YPOWebService.Classes
{
    public class clsInformz
    {
        #region Private Members Declaration

        ///////////////////////////////////////Start Block: Initialize constant values to access Informz API functions./////////////////////////////////

        InformzWebService objClient;
        private string strInformzUID = ConfigurationManager.AppSettings["InformzUser"];  //Get Informz User id from webconfig 
        private string strInformzPwd = ConfigurationManager.AppSettings["InformzPwd"];  //Get Informz password  from webconfig 
        private string strInformzBrandId = ConfigurationManager.AppSettings["InformzBrandId"];  //Get YM Informz brand id from webconfig 
        private static string strPath = HttpContext.Current.Request.PhysicalApplicationPath;
        private const string strApiNamespace = "http://partner.informz.net/aapi/2009/08/";

        //////////////////////////////////////////////////////////////// End Block  //////////////////////////////////////////////////////////////////       

        #endregion

        #region API Helper Methods

        //*********************************************************************************************************************
        //Author Name      : Reuben Ram   Date  :23 Oct 2013
        //Input Parameters : intEventId=> intEventId pass as parameter
        //Purpose          : This function is used to compress byte array using GzipStream 
        //*********************************************************************************************************************

        public static byte[] Compress(byte[] arrRaw)
        {
            using (MemoryStream objMemory = new MemoryStream()) //Creating object of memory stream.
            {
                // Create a compression stream pointing to the destiantion stream.
                using (System.IO.Compression.GZipStream objGzipStream = new GZipStream(objMemory, CompressionMode.Compress))
                {
                    objGzipStream.Write(arrRaw, 0, arrRaw.Length);         // Use GZipStream to write compressed bytes to the byte array.
                }
                return objMemory.ToArray();
            }
        }

        //*********************************************************************************************************************
        //Author Name      : Reuben Ram   Date  :23 Oct 2013
        //Input Parameters : strResponse => strResponse pass as parameter
        //Purpose          : This function is used to decompress string using GzipStream 
        //*********************************************************************************************************************

        public static string Decompress(string strResponse)
        {
            var arrCompressedResponse = Convert.FromBase64String(strResponse);  // Convert base64 encoded string to byte array.
            arrCompressedResponse = arrCompressedResponse.Skip(4).ToArray();     // Remove the additional 4 bytes from array.
            byte[] arrBuffer = new byte[arrCompressedResponse.Length];     // Create a buffer. 
            using (var objSource = new MemoryStream(arrCompressedResponse))
            using (var objDestination = new MemoryStream())
            {
                // Create a compression stream pointing to the destination stream.
                using (var gs = new GZipStream(objSource, CompressionMode.Decompress))
                {
                    int intBytesRead;
                    while ((intBytesRead = gs.Read(arrBuffer, 0, arrBuffer.Length)) > 0)// Read the compressed data into the buffer.
                        objDestination.Write(arrBuffer, 0, intBytesRead);
                }
                arrBuffer = null;
                return Encoding.UTF8.GetString(objDestination.ToArray());  // Return byte array.
            }
        }

        //******************************************************************************************************************
        //Author Name      : Reuben Ram   Date  :24 Oct 2013
        //Input Parameters : N/A 
        //Purpose          : This function is used to get list of all mailings from informz.
        //*******************************************************************************************************************

        public List<string> getMailingTemplateFromInformz()
        {
            byte[] arrRequest;
            byte[] arrRequestHeader;
            byte[] arrCompressRequest;
            string strRequest = string.Empty;
            string strResponse = string.Empty;
            string strDeCompress = string.Empty;
            string strInnerResponse = string.Empty;
            string strEnocededRequest = string.Empty;
            List<string> lstMailingTemplate = new List<string>(); // Create mailing list object and assign memory. 
            objClient = new InformzWebService();
            XmlDocument objXmlDoc = new XmlDocument();

            /*************************************** Creating mailing request to Informz Api ****************************/

            // Get the name of mailing and convert it into byte array.
            arrRequest = Encoding.UTF8.GetBytes("<Grid type=\"mailing\"><ReturnFields><DataElement>name</DataElement></ReturnFields></Grid>");
            arrRequestHeader = BitConverter.GetBytes(arrRequest.Length);             // Create header for requested string.
            arrCompressRequest = Compress(arrRequest);                               // Compress the request.

            // Add additional header to request and encode it into base64 format.
            strEnocededRequest = Convert.ToBase64String(arrRequestHeader.Concat(arrCompressRequest).ToArray());

            // Create a full request format for informz api.
            strRequest = @"<GridRequest xmlns=" + '"' + strApiNamespace + '"' + ">" +
                                 "<Brand id=" + '"' + strInformzBrandId + '"' + ">Partner</Brand>" +
                                 "<User>" + strInformzUID + "</User>" +
                                 "<Password>" + strInformzPwd + "</Password>" +
                                 "<Grids>" + strEnocededRequest + "</Grids>" +
                          "</GridRequest>";

            /*************************************** End creating mailing request to Informz Api ***************************/

            strResponse = objClient.PostInformzMessage(strRequest);     // Send request to informz api and get the response.

            objXmlDoc.LoadXml(strResponse);   // Load response to xml docment.
            XmlNodeList objGridNode = objXmlDoc.GetElementsByTagName("Grids");  // Get Grid node from xml document. 
            foreach (XmlNode response in objGridNode)
            {
                strInnerResponse = Convert.ToString(response.InnerXml);       // Get the compress and encoded response from xml document.
            }

            strDeCompress = Convert.ToString(clsInformz.Decompress(strInnerResponse));  // Decompress the response.
            objXmlDoc.LoadXml(strDeCompress);  // Load decompress response to xml document.
            XmlNodeList objFieldNode = objXmlDoc.GetElementsByTagName("Field"); // Get Field node from xml document. 

            foreach (XmlNode response in objFieldNode)
            {
                lstMailingTemplate.Add(response.InnerText);   // Add mailing name in list.
            }

            objXmlDoc = null;
            objGridNode = null;
            objFieldNode = null;
            objClient = null;
            return lstMailingTemplate;
        }

        //******************************************************************************************************************
        //Author Name      : Reuben Ram   Date  :24 Oct 2013
        //Input Parameters : strEmail => strEmail as a parameter, strBadgeNumber => strBadgeNumber as parameter
        //Purpose          : This function is used to add user to informz database.
        //*******************************************************************************************************************
        public string addSubscriberToInformzList(string strEmail, string strBadgeNumber)
        {
            byte[] arrRequest;
            byte[] arrRequestHeader;
            byte[] arrCompressRequest;
            string strRequest = string.Empty;
            string strResponse = string.Empty;
            string strDeCompress = string.Empty;
            string strInnerResponse = string.Empty;
            string strEnocededRequest = string.Empty;
            string strReturnResponse = string.Empty;
            objClient = new InformzWebService();

            /*************************************** Creating mailing request to Informz Api *********************************/

            // Insert the subscriber details and convert it into byte array.
            arrRequest = Encoding.UTF8.GetBytes("<Subscribe>" +
                                                    "<InterestDetails>" +
                                                        "<InterestNames>" +
                                                            //"<InterestName>Sample Interest 1</InterestName>" +
                                                            "<InterestName>Amar</InterestName>" +
                                                         "</InterestNames>" +
                                                         "<InterestAction>AddOrUpdateSubscribers</InterestAction>" +
                                                    "</InterestDetails>" +
                                                    "<SubscriberData>" +
                                                         "<Subscribers>" +
                                                            "<Subscriber>" +
                                                                "<Email>" + strEmail + "</Email>" +
                                                                "<ID>" + strBadgeNumber + "</ID>" +
                                                            "</Subscriber>" +
                                                        "</Subscribers>" +
                                                    "</SubscriberData>" +
                                                  "</Subscribe>");
            arrRequestHeader = BitConverter.GetBytes(arrRequest.Length);     // Create header for requested string.
            arrCompressRequest = Compress(arrRequest);

            // Add additional header to request and encode it into base64 format.
            strEnocededRequest = Convert.ToBase64String(arrRequestHeader.Concat(arrCompressRequest).ToArray());
            strRequest = "<ActionRequest xmlns=" + '"' + strApiNamespace + '"' + ">" +
                            "<Brand id=" + '"' + strInformzBrandId + '"' + ">Partner</Brand>" +
                                "<User>" + strInformzUID + "</User>" +
                                "<Password>" + strInformzPwd + "</Password>" +
                                "<Actions>" + strEnocededRequest + "</Actions>" +
                         "</ActionRequest>";

            /*************************************** End creating mailing request to Informz Api ******************************/

            strResponse = objClient.PostInformzMessage(strRequest);
            XmlDocument objXmlDoc = new XmlDocument();
            objXmlDoc.LoadXml(strResponse);    // Load response to xml docment.
            XmlNodeList objParentNode = objXmlDoc.GetElementsByTagName("Responses");  // Get Response node from xml document. 
            foreach (XmlNode response in objParentNode)
            {
                strInnerResponse = Convert.ToString(response.InnerXml);     // Get the compress and encoded response from xml document.
            }

            string strDeCompressResponse = Convert.ToString(Decompress(strInnerResponse));
            objXmlDoc.LoadXml(strDeCompressResponse);   // Load response to xml docment.
            string strResponseStatus = Convert.ToString(objXmlDoc.GetElementsByTagName("Status").Item(0).InnerText);

            if (!String.IsNullOrEmpty(strResponseStatus) && strResponseStatus.Equals("success"))
            {
                int intRecordInserted = Convert.ToInt32(objXmlDoc.GetElementsByTagName("NewSubscriberCount").Item(0).InnerText);
                int intRecordExits = Convert.ToInt32(objXmlDoc.GetElementsByTagName("PreviousSubscriberCount").Item(0).InnerText);
                if (intRecordInserted > 0)
                {
                    strReturnResponse += strEmail + " is added at Informz." + Environment.NewLine;
                }
                else
                {
                    strReturnResponse += strEmail + " is already Exits and Updated." + Environment.NewLine;
                }
            }
            else
            {
                strReturnResponse += "Due to some problem this record " + strEmail + " is not inserted/Updated at Informz." + Environment.NewLine;
            }
            objXmlDoc = null;
            objParentNode = null;
            objClient = null;
            return strReturnResponse;
        }

        //******************************************************************************************************************
        //Author Name      : Amar Date  :16 Feb 2014
        //Input Parameters : strEmail => strEmail as a parameter, strBadgeNumber => strBadgeNumber as parameter,strSessionName=> strSessionName as parameter
        //Purpose          : This function is used to add user to informz database.
        //*******************************************************************************************************************
        public string addSubscriberToInformzList(string strEmail, string strBadgeNumber, string strSessionName)
        {
            byte[] arrRequest;
            byte[] arrRequestHeader;
            byte[] arrCompressRequest;
            string strRequest = string.Empty;
            string strResponse = string.Empty;
            string strDeCompress = string.Empty;
            string strInnerResponse = string.Empty;
            string strEnocededRequest = string.Empty;
            string strReturnResponse = string.Empty;
            objClient = new InformzWebService();

            /*************************************** Creating mailing request to Informz Api *********************************/

            // Insert the subscriber details and convert it into byte array.
            arrRequest = Encoding.UTF8.GetBytes("<Subscribe>" +
                                                    "<InterestDetails>" +
                                                        "<InterestNames>" +
                                                         "<InterestName>"+ strSessionName +"</InterestName>" +
                                                         "</InterestNames>" +
                                                         "<InterestAction>AddOrUpdateSubscribers</InterestAction>" +
                                                    "</InterestDetails>" +
                                                    "<SubscriberData>" +
                                                         "<Subscribers>" +
                                                            "<Subscriber>" +
                                                                "<Email>" + strEmail + "</Email>" +
                                                                "<ID>" + strBadgeNumber + "</ID>" +
                                                            "</Subscriber>" +
                                                        "</Subscribers>" +
                                                    "</SubscriberData>" +
                                                  "</Subscribe>");
            arrRequestHeader = BitConverter.GetBytes(arrRequest.Length);     // Create header for requested string.
            arrCompressRequest = Compress(arrRequest);

            // Add additional header to request and encode it into base64 format.
            strEnocededRequest = Convert.ToBase64String(arrRequestHeader.Concat(arrCompressRequest).ToArray());
            strRequest = "<ActionRequest xmlns=" + '"' + strApiNamespace + '"' + ">" +
                            "<Brand id=" + '"' + strInformzBrandId + '"' + ">Partner</Brand>" +
                                "<User>" + strInformzUID + "</User>" +
                                "<Password>" + strInformzPwd + "</Password>" +
                                "<Actions>" + strEnocededRequest + "</Actions>" +
                         "</ActionRequest>";

            /*************************************** End creating mailing request to Informz Api ******************************/

            strResponse = objClient.PostInformzMessage(strRequest);
            XmlDocument objXmlDoc = new XmlDocument();
            objXmlDoc.LoadXml(strResponse);    // Load response to xml docment.
            XmlNodeList objParentNode = objXmlDoc.GetElementsByTagName("Responses");  // Get Response node from xml document. 
            foreach (XmlNode response in objParentNode)
            {
                strInnerResponse = Convert.ToString(response.InnerXml);     // Get the compress and encoded response from xml document.
            }

            string strDeCompressResponse = Convert.ToString(Decompress(strInnerResponse));
            objXmlDoc.LoadXml(strDeCompressResponse);   // Load response to xml docment.
            string strResponseStatus = Convert.ToString(objXmlDoc.GetElementsByTagName("Status").Item(0).InnerText);

            if (!String.IsNullOrEmpty(strResponseStatus) && strResponseStatus.Equals("success"))
            {
                int intRecordInserted = Convert.ToInt32(objXmlDoc.GetElementsByTagName("NewSubscriberCount").Item(0).InnerText);
                int intRecordExits = Convert.ToInt32(objXmlDoc.GetElementsByTagName("PreviousSubscriberCount").Item(0).InnerText);
                if (intRecordInserted > 0)
                {
                    strReturnResponse += strEmail + " is added at Informz." + Environment.NewLine;
                }
                else
                {
                    strReturnResponse += strEmail + " is already Exits and Updated." + Environment.NewLine;
                }
            }
            else
            {
                strReturnResponse += "Due to some problem this record " + strEmail + " is not inserted/Updated at Informz." + Environment.NewLine;
            }
            objXmlDoc = null;
            objParentNode = null;
            objClient = null;
            return strReturnResponse;
        }

        #endregion
    }
}