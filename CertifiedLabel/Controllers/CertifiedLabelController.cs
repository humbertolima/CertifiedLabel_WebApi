using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CertifiedLabel.Models;
using System.Net.Http.Formatting;
using System.Data.Entity;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Web.Mvc;
using System.Web;

namespace CertifiedLabel.Controllers
{

    public class CertifiedLabelController : ApiController
    {        
        private CertifiedLabelEntities1 db1 = new CertifiedLabelEntities1();
        private string url = System.Configuration.ConfigurationManager.AppSettings["url"];
        private string useremail = System.Configuration.ConfigurationManager.AppSettings["useremail"];
        private string userid = System.Configuration.ConfigurationManager.AppSettings["userid"];
        private string signature_request = System.Configuration.ConfigurationManager.AppSettings["signature_request"];
        private string label_request = System.Configuration.ConfigurationManager.AppSettings["label_request"];
        private string disk = System.Configuration.ConfigurationManager.AppSettings["disk"];
        HttpClient client = new HttpClient();

        // GET: 
        /* api/CertifiedLabel?UserID=1689&SenderID=1865&UserEmail=csalazar@miamigov.com&DepartmentName=Test2&AplicationName=Test2&Sector=Test&SenderAddress=Test&SenderAddress2=Test&SenderCity=Miami&SenderState=FL&SenderZip=33141&SenderPhone=12345678&CaseNumber=Test123456&SupervisorName=JuanCarlos&Description=Test&Subject=Test&Date=06/26/2016&Contact=Test&Company=Test&Address=Test&Address2=Test&City=Miami&State=FL&Zip=33181*/

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================

        // Get Label by parameters
        public HttpResponseMessage GetLabel_by_parameters(
        string Contact,
        string Company,
        string Address,
        string Address2,
        string City,
        string State,
        string Zip,
        string ReferenceNumber,
        string ReferenceID,
        string SenderContact,
        string EnteredBy,
        string DepartmentID,
        string SectionID,
        string DepartmentName,
        string ApplicationName,
        string SenderAddress,
        string SenderAddress2,
        string SenderCity,
        string SenderState,
        string SenderZip)
        {
            try
            {
                string urlAddress = label_request;
                urlAddress += "UserID=" + userid;
                urlAddress += "&UserEmail=" + useremail;
                urlAddress += "&AddresseeContact=" + Remove_Special_Char(Contact);
                urlAddress += "&AddresseeCompany=" + Remove_Special_Char(Company);
                urlAddress += "&AddresseeAddress=" + Remove_Special_Char(Address);
                urlAddress += "&AddresseeAddress2=" + Remove_Special_Char(Address2);
                urlAddress += "&AddresseeCity=" + Remove_Special_Char(City);
                urlAddress += "&AddresseeState=" + Remove_Special_Char(State);
                urlAddress += "&AddresseeZip=" + Zip;
                urlAddress += "&InternalCode=x_internalcode";
                urlAddress += "&FileNumber=" + Remove_Special_Char(ReferenceNumber);
                urlAddress += "&InternalFileNumber=x_internalfilenumber";
                urlAddress += "&SenderContact=" + Remove_Special_Char(SenderContact);
                urlAddress += "&SenderCompany=Company: " + Remove_Special_Char(DepartmentName);
                urlAddress += "&SenderAddress=" + Remove_Special_Char(SenderAddress);
                urlAddress += "&SenderAddress2=" + Remove_Special_Char(SenderAddress2); ;
                urlAddress += "&SenderCity=" + Remove_Special_Char(SenderCity);
                urlAddress += "&SenderState=" + Remove_Special_Char(SenderState);
                urlAddress += "&SenderZip=" + Remove_Special_Char(SenderZip);
                urlAddress += "&Weight=1";
                urlAddress += "&ReturnReceipt=";
                urlAddress += "&ERR=checkbox";
                urlAddress += "&RestrictedDelivery=";
                urlAddress += "&Optlbl=1";
                urlAddress += "&SenderID=1865";
                urlAddress += "&CustomerNumber=";
                urlAddress += "&DUNSNumber=900176001";
                urlAddress += "&ERRDeliveryMethod=FTP";
                urlAddress += "&ERRPaymentMethod=Meter/PCPostage";
                urlAddress += "&PackageType=Letters";
                urlAddress += "&FormType=15";

                HttpResponseMessage response = new HttpResponseMessage();

                response = client.GetAsync(urlAddress).Result;

                if (response.IsSuccessStatusCode)
                {
                    string filename = GetName(response.RequestMessage.RequestUri.LocalPath);

                    //Stores information of the label in CertifiedLabel database
                    CertifiedLabel.Models.CertifiedLabel label = new CertifiedLabel.Models.CertifiedLabel() { CertifiedNumber = filename, ApplicationName = ApplicationName, DepartmentID = int.Parse(DepartmentID), SectionID = int.Parse(SectionID), ReferenceNumber = ReferenceNumber, ReferenceID = int.Parse(ReferenceID), CreatedDate = DateTime.Now, IsReceived = false };
                    db1.CertifiedLabels.Add(label);
                    db1.SaveChanges();

                    string myFileName = filename + "_Label.pdf";  //Certified Number generated by Certified Label Service            
                    string myFullPath = disk + @"\" + myFileName; //Path where labels are going to be stored

                    FileStream stream = new FileStream(myFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                    response.Content.CopyToAsync(stream).ContinueWith(
                      (copyTask) =>
                      {
                          stream.Close();
                      });

                    //Returns to the application the Certified Number
                    response.ReasonPhrase = filename.ToString();
                    //Returns to the application the PDF label

                    return response;
                }
                else
                {
                    return new HttpResponseMessage(response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message, ex);
            }
        }

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================

        // Get Signatures by Range of dates (Job to run at night)
        public void Get_Signatures_Range(DateTime begindate, DateTime enddate)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                string request = "";
                //List<CertifiedLabel.Models.CertifiedLabel> certifiedlabels = db1.CertifiedLabels.Where(x => x.CreatedDate >= begindate && x.CreatedDate <= enddate && x.IsReceived == false).ToList();
                List<CertifiedLabel.Models.CertifiedLabel> certifiedlabels = db1.CertifiedLabels.Where(x => x.CreatedDate >= begindate && x.CreatedDate <= enddate && x.ReceivedDate == null).ToList(); //
                foreach (CertifiedLabel.Models.CertifiedLabel t in certifiedlabels)
                {
                    request = signature_request + t.CertifiedNumber + ".pdf";
                    response = client.GetAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        CertifiedLabel.Models.CertifiedLabel label = db1.CertifiedLabels.Where(x => x.CertifiedNumber == t.CertifiedNumber).FirstOrDefault();
                        label.IsReceived = true;
                        label.ReceivedDate = DateTime.Now;
                        db1.Entry(label).State = EntityState.Modified;
                        db1.SaveChanges();                        

                        string myFileName = t.CertifiedNumber + "_Signature.pdf";
                        string myFullPath = disk + @"\" + myFileName; //Path where labels are going to be stored

                        FileStream stream = new FileStream(myFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                        response.Content.CopyToAsync(stream).ContinueWith(
                          (copyTask) =>
                          {
                              stream.Close();
                          });                        
                    }
                }
                //db.SaveChanges();
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                throw ex;               
                //Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message, ex);
            }
        }

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================

        // Get Signature giving a Certified Number
        public HttpResponseMessage Get_Signature_Certified_Number(string CertifiedNumber)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                string request = signature_request + CertifiedNumber + ".pdf";
                response = client.GetAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    CertifiedLabel.Models.CertifiedLabel label = db1.CertifiedLabels.Where(x => x.CertifiedNumber == CertifiedNumber).FirstOrDefault();
                    label.IsReceived = true;
                    label.ReceivedDate = DateTime.Now;
                    db1.Entry(label).State = EntityState.Modified;
                    db1.SaveChanges();

                    string myFileName = CertifiedNumber + "_Signature.pdf";
                    string myFullPath = disk + @"\" + myFileName; //Path where labels are going to be stored

                    FileStream stream = new FileStream(myFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                    response.Content.CopyToAsync(stream).ContinueWith(
                      (copyTask) =>
                      {
                          stream.Close();
                      });
                }
                else
                {
                    //Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("No signature for this letter yet"));
                    string message = "No signature for this letter yet";
                    response.ReasonPhrase = message.ToString();
                    HttpStatusCode Status = HttpStatusCode.BadRequest;
                    response.StatusCode = Status;
                    return response;
                    //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No signature for this letter yet", new Exception("No signature for this letter yet"));
                }

                return response;
            }

            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message, ex);
            }
        }

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================
        
        // Get All Signatures        
        public void Get_Signatures()
        {    
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                string request = "";

                foreach (CertifiedLabel.Models.CertifiedLabel t in db1.CertifiedLabels.Where(x => x.ReceivedDate == null).ToList())
                {

                    request = signature_request + t.CertifiedNumber + ".pdf";
                    response = client.GetAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        CertifiedLabel.Models.CertifiedLabel label = db1.CertifiedLabels.Where(x => x.CertifiedNumber == t.CertifiedNumber).FirstOrDefault();
                        label.IsReceived = true;
                        label.ReceivedDate = DateTime.Now;
                        db1.Entry(label).State = EntityState.Modified;
                        db1.SaveChanges();

                        string myFileName = t.CertifiedNumber + "_Signature.pdf";
                        string myFullPath = disk + @"\" + myFileName; //Path where labels are going to be stored

                        FileStream stream = new FileStream(myFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                        response.Content.CopyToAsync(stream).ContinueWith(
                          (copyTask) =>
                          {
                              stream.Close();
                          });                        
                    }
                }               
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                throw ex;
            }
        }

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================

        // Remove Special Characters
        private string Remove_Special_Char(string str)
        {
            if (str == null)
            {
                return str;
            }
            else
            {
                str.Replace('#', '*');
                str.Replace('&', '$');
                str.Trim();
                return str;
            }
        }

        //================================================================================================================================================================================================================
        //================================================================================================================================================================================================================

        // Certified Number
        private string GetName(string filepath)
        {
            char[] temp = new char[26];
            filepath.CopyTo(9, temp, 0, 26);
            return new string(temp);
        }

    }
}


