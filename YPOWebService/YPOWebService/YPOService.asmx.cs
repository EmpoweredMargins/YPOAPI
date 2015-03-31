//****************************************************************************************
//Page/Module Name: 				PsiChiService.asmx.cs
//Author Name:					    Reuben Ram
//Date:							    23 Oct 2013
//Purpose:						    It includes functions to Synchronize Data from YM end to Custom Database and vice versa 
//Table referred:				    N/A 
//Table updated:					N/A
//Most Important Related Files:	    clsYM.cs
//****************************************************************************************
//Chronological Development
//Ref No   Developer Name      Date            Severity        Description
//-----------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace YPOWebService
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]




    public class YPOService : System.Web.Services.WebService
    {
        clsYM objClsYm; //Declare the object of the class clsYM

        //****************************************************************************************
        //Author Name      : Reuben Ram        Date : 23 July 2013
        //Input Parameters : N/A  
        //Purpose          : This method used to add event attendies details from YM end to Informz end
        //****************************************************************************************

        [WebMethod]
        public string synchronizationProcessFromYMendToYPO()
        {
            string strResponse = string.Empty;
            objClsYm = new clsYM();    //Assign memorey to the object of the class objClsYm
            strResponse = objClsYm.addSubscriberToInformz();
            objClsYm = null;
            return strResponse;
        }
    }
}