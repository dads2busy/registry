using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using Questionnaire2.Models;
using Questionnaire2.DAL;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text;
using Questionnaire2.ViewModels;

namespace Questionnaire2.Controllers
{
    public class AppDataController : Controller
    {
        private readonly QuestionnaireContext _db = new QuestionnaireContext();
        private readonly UsersContext _udb = new UsersContext();

        // GET: AppData
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(RegisterExternalLoginModel mReg, string Command, int id = 0)
        {
            if (Command == "MonthlyReport")
            {
                // get all user responses
                var responses = _db.Responses
                    .Where(w => w.QuestionnaireId == 1)
                    .Select(r => new
                    {
                        userId = r.UserId,
                        questionText = r.QuestionText,
                        questionResponse = r.ResponseItem
                    }).ToList();

                // get all user final levels
                var finalLevels = _db.UserLevels
                    .Select(s => new
                    {
                        userId = s.UserId,
                        finalStepLevel = s.FinalStepLevel,
                        finalStepLevelDate = s.FinalStepLevelDate
                    }).ToList();

                // get distinct user ids
                var distinctIds = responses.Select(s => s.userId).Distinct();

                // create spreadsheet, add worksheet, name columns
                ExcelPackage pck = new ExcelPackage();
                var ws1 = pck.Workbook.Worksheets.Add("Users");
                ws1.Cells[1, 1].Value = "First Name";
                ws1.Cells[1, 2].Value = "Last Name";
                ws1.Cells[1, 3].Value = "EMail";
                ws1.Cells[1, 4].Value = "Home Address";
                ws1.Cells[1, 5].Value = "City";
                ws1.Cells[1, 6].Value = "State";
                ws1.Cells[1, 7].Value = "Zip";
                ws1.Cells[1, 8].Value = "Highest Edu";
                ws1.Cells[1, 9].Value = "EC Provider";
                ws1.Cells[1, 10].Value = "T/TA Provider";
                ws1.Cells[1, 11].Value = "Credentials";
                ws1.Cells[1, 12].Value = "Verified";
                ws1.Cells[1, 13].Value = "Final Level";

                // set spreadsheet start row
                var XLStartRow = 2;

                // set column list
                var columnList = new System.Collections.Generic.List<string>();
                columnList.Add("First Name");
                columnList.Add("Last Name");
                columnList.Add("EMail");
                columnList.Add("Home Address");
                columnList.Add("City");
                columnList.Add("State");
                columnList.Add("Zip");
                columnList.Add("Highest Level of Education");
                columnList.Add("Career Pathways");
                columnList.Add("TA Provider");

                // for each user create a row in spreadsheet
                foreach (var d_id in distinctIds)
                {
                    // fill first n columns of spreadsheet
                    for (int col = 0; col < columnList.Count; col++)
                    {
                        var temp = columnList[col];
                        if (responses.Any(x => x.userId == d_id && x.questionText.ToLower().Contains(columnList[col].ToString().ToLower())))
                        {
                            ws1.Cells[XLStartRow, col + 1].Value = responses.Where(x => x.userId == d_id && x.questionText.ToLower().Contains(columnList[col].ToString().ToLower())).FirstOrDefault().questionResponse;
                        }
                        else
                        {
                            ws1.Cells[XLStartRow, col + 1].Value = "NA";
                        }
                    }
                    XLStartRow = XLStartRow + 1;

                    // fill credentials column of spreadsheet
                    var credentialList = "";
                    if (responses.Any(a => a.userId == d_id && a.questionText.ToLower().Contains("credential") && a.questionText.ToLower().Contains("type")))
                    {
                        var credentials = responses.Where(c => c.userId == d_id && c.questionText.ToLower().Contains("credential") && c.questionText.ToLower().Contains("type"));                      
                        foreach (var c in credentials)
                        {
                            credentialList += c.questionResponse + ",";
                        }
                        // eliminate final comma
                        if (credentialList.Length > 1)
                        {
                            credentialList = credentialList.Substring(0, credentialList.Length - 1);
                        }
                    }
                    ws1.Cells[XLStartRow, 11].Value = credentialList;

                    // fill "verified" and "final level" columns of spreadsheet
                    if (finalLevels.Any(x => x.userId == d_id))
                    {
                        ws1.Cells[XLStartRow, 12].Value = "Yes";
                        ws1.Cells[XLStartRow, 13].Value = finalLevels.Where(x => x.userId == d_id).FirstOrDefault().finalStepLevel;
                    }
                    else
                    {
                        ws1.Cells[XLStartRow, 12].Value = "No";
                    }
                }

                // download spreadsheet
                var stream = new MemoryStream();
                pck.SaveAs(stream);
                string fileName = "Monthly_Report.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                stream.Position = 0;
                return File(stream, contentType, fileName);
            }

            if (Command == "ExportAllData")
            {
                var responses = _db.Responses
                .Select(r => new
                {
                    userId = r.UserId,
                    qCategoryId = r.QCategoryId,
                    qCategoryName = r.QCategoryName,
                    questionText = r.QuestionText,
                    questionResponse = r.ResponseItem
                }).ToList();

                var userLevels = _db.UserLevels
                .Select(r => new
                {
                    userId = r.UserId,
                    finalStepLevel = r.FinalStepLevel
                }).ToList();

                var verifications = _db.Verifications
                .Select(r => new
                {
                    userId = r.UserId,
                    itemInfo = r.ItemInfo,
                    itemStepLevel = r.ItemStepLevel,
                    itemverified = r.ItemVerified
                }).ToList();

                ExcelPackage pck = new ExcelPackage();

                var ws1 = pck.Workbook.Worksheets.Add("Responses");
                ws1.Cells[1, 1].Value = "UserId";
                ws1.Cells[1, 2].Value = "QCategoryId";
                ws1.Cells[1, 3].Value = "QCategoryName";
                ws1.Cells[1, 4].Value = "QuestionText";
                ws1.Cells[1, 5].Value = "QuestionResponse";
                for (int x = 0; x < responses.Count; x++)
                {
                    ws1.Cells[x + 2, 1].Value = responses[x].userId;
                    ws1.Cells[x + 2, 2].Value = responses[x].qCategoryId;
                    ws1.Cells[x + 2, 3].Value = responses[x].qCategoryName;
                    ws1.Cells[x + 2, 4].Value = responses[x].questionText;
                    ws1.Cells[x + 2, 5].Value = responses[x].questionResponse;
                }

                var ws2 = pck.Workbook.Worksheets.Add("UserLevels");
                ws2.Cells[1, 1].Value = "UserId";
                ws2.Cells[1, 2].Value = "FinalStepLevel";
                for (int x = 0; x < userLevels.Count; x++)
                {
                    ws2.Cells[x + 2, 1].Value = userLevels[x].userId;
                    ws2.Cells[x + 2, 2].Value = userLevels[x].finalStepLevel;
                }

                var ws3 = pck.Workbook.Worksheets.Add("Verifications");
                ws3.Cells[1, 1].Value = "UserId";
                ws3.Cells[1, 2].Value = "ItemInfo";
                ws3.Cells[1, 3].Value = "ItemStepLevel";
                ws3.Cells[1, 4].Value = "ItemVerified";
                for (int x = 0; x < verifications.Count; x++)
                {
                    ws3.Cells[x + 2, 1].Value = verifications[x].userId;
                    ws3.Cells[x + 2, 2].Value = verifications[x].itemInfo;
                    ws3.Cells[x + 2, 3].Value = verifications[x].itemStepLevel;
                    ws3.Cells[x + 2, 4].Value = verifications[x].itemverified;
                }

                var stream = new MemoryStream();
                pck.SaveAs(stream);

                string fileName = "myfilename.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }

            if (Command == "VerificationsReport")
            {
                var users = new List<UserInfo>();
                var userIds = _db.Verifications.Select(x => x.UserId).Distinct().ToList();
                for (var i = 0; i < userIds.Count(); i++)
                {
                    var userInfo = new UserInfo { UserId = userIds[i] };
                    userInfo.VerifiedCount = _db.Verifications.Count(x => x.UserId == userInfo.UserId && x.QuestionnaireId == 1 && x.ItemVerified);
                    userInfo.UnverifiedCount = _db.Verifications.Count(x => x.UserId == userInfo.UserId && x.QuestionnaireId == 1 && x.ItemVerified == false);

                    userInfo.Editable = !_db.Verifications.Any(
                        x => x.UserId == userInfo.UserId && x.QuestionnaireId == 1 && x.Editable == false);

                    var firstOrDefault = _udb.UserProfiles.FirstOrDefault(x => x.UserId == userInfo.UserId);
                    if (firstOrDefault != null)
                        userInfo.UserName = firstOrDefault.UserName;
                    var responses = _db.Responses.Where(x => x.UserId == userInfo.UserId && x.QCategoryName.ToUpper().Contains("PERSONAL"));

                    var orDefault = responses.FirstOrDefault(x => x.QuestionText.ToUpper().Contains("FIRST NAME"));
                    if (orDefault != null)
                        userInfo.FirstName = orDefault.ResponseItem;
                    var response = responses.FirstOrDefault(x => x.QuestionText.ToUpper().Contains("LAST NAME"));
                    if (response != null)
                        userInfo.LastName = response.ResponseItem;

                    users.Add(userInfo);
                }

                var usersVerified = users.Where(x => x.UnverifiedCount == 0);
                var usersUnverified = users.Where(x => x.UnverifiedCount != 0);

                var userVerifications = new UserVerifications();
                userVerifications.UsersVerified = usersVerified.ToList();
                userVerifications.UsersUnverified = usersUnverified.ToList();

                var tableHtml = "<html><head></head><body><table>";
                tableHtml += "<tr><td colspan=4><h1>User Verification Status</h1></td></tr>";
                tableHtml += "<tr><td colspan=4><h2>Unverified Users</h2></td></tr>";
                tableHtml += "<tr><th>First Name</th><th>Last Name</th><th>Username</th><th>Status</th></tr>";
                foreach (var user in usersUnverified)
                {
                    tableHtml += "<tr><td>" + user.FirstName + "</td>";
                    tableHtml += "<tr><td>" + user.LastName + "</td>";
                    tableHtml += "<tr><td>" + user.UserName + "</td>";
                    tableHtml += "<tr><td>" + user.VerifiedCount + "/" + user.UnverifiedCount + "</td>";
                }
                tableHtml += "<tr><td colspan=4><h2>Verified Users</h2></td></tr>";
                tableHtml += "<tr><th>First Name</th><th>Last Name</th><th>Username</th><th>Status</th></tr>";
                foreach (var user in usersVerified)
                {
                    tableHtml += "<tr><td>" + user.FirstName + "</td>";
                    tableHtml += "<tr><td>" + user.LastName + "</td>";
                    tableHtml += "<tr><td>" + user.UserName + "</td>";
                    tableHtml += "<tr><td>" + user.VerifiedCount + "/" + user.UnverifiedCount + "</td>";
                }
                tableHtml += "</table></body></html>";

                tableToPdf(this, new EventArgs(), tableHtml);
            }

            return View(_db.AppSettings.ToList());
        }

        public void tableToPdf(object sender, EventArgs e, string pageHtml)
        {
            //var table = document.GetElementbyId("verificationsTable");

            //Set page size as A4
            Document pdfDoc = new Document(PageSize.A4, 20, 10, 10, 10);

            try
            {
                var ms = new MemoryStream();
                PdfWriter.GetInstance(pdfDoc, ms);

                //Open PDF Document to write data
                pdfDoc.Open();

                ////Assign Html content in a string to write in PDF
                //string contents = "";

                //StreamReader sr;
                //try
                //{
                //    //Read file from server path
                //    sr = System.IO.File.OpenText(Server.MapPath("~/sample.html"));
                //    //store content in the variable
                //    contents = sr.ReadToEnd();
                //    sr.Close();
                //}
                //catch (Exception ex)
                //{

                //}

                //Read string contents using stream reader and convert html to parsed conent
                var parsedHtmlElements = HTMLWorker.ParseToList(new StringReader(pageHtml), null);

                //Get each array values from parsed elements and add to the PDF document
                foreach (var htmlElement in parsedHtmlElements)
                    pdfDoc.Add(htmlElement as IElement);

                //Close your PDF
                pdfDoc.Close();

                var ms2 = new MemoryStream(ms.ToArray());

                Response.Buffer = true;
                Response.ContentType = "application/pdf";
                //Set default file Name as current datetime
                Response.AddHeader("content-disposition", "attachment; filename=" + DateTime.Now.ToString("yyyyMMdd") + ".pdf");
                Response.Cache.SetCacheability(HttpCacheability.NoCache);

                ms2.WriteTo(Response.OutputStream);
                Response.End();

            }
            catch (Exception ex)
            {
                Response.Write(ex.ToString());
            }
        }

        // GET: AppData/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AppData/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AppData/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: AppData/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AppData/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: AppData/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AppData/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
