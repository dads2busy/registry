using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Questionnaire2.Models;
using Questionnaire2.DAL;
using Questionnaire2.ViewModels;
using WebMatrix.WebData;
using Questionnaire2.Helpers;
using System.IO;
using System.Transactions;
using System;
using System.Web;
using System.Text;
using Spire.Doc;
using Spire.Doc.Documents;
using System.Threading;
using System.Drawing;

namespace Questionnaire2.Controllers
{
    [Authorize(Roles = "Administrator, CareProvider")]
    public class ResponseController : Controller
    {
        private readonly QuestionnaireContext _db = new QuestionnaireContext();
        private readonly UsersContext _udb = new UsersContext();

        //
        // GET: /Response/

        public ActionResult Index()
        {
            return View(_db.Responses.ToList());
        }

        //
        // GET: /Response/Details/5

        public ActionResult Details(int id = 0)
        {
            Response response = _db.Responses.Find(id);
            if (response == null)
            {
                return HttpNotFound();
            }
            return View(response);
        }

        //
        // GET: /Response/Create

        public ActionResult Download()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            UserLevel userLevel = _db.UserLevels.Where(x => x.UserId == userId).FirstOrDefault();
            return View(userLevel);
        }

        //
        // POST: /Response/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Download(RegisterExternalLoginModel mReg, string Command, int id = 0)
        {
            if (Command == "MS Word")
            {
                try
                {
                    var userId = WebSecurity.GetUserId(User.Identity.Name);
                    var responses = _db.Responses.Where(x => x.UserId == userId).OrderBy(x => x.Ordinal).ThenBy(x => x.SubOrdinal).ThenBy(x => x.QQOrd).ToList();
                    var categories = new List<string> { "Personal Information", "Employment", "Education", "Coursework", "Certifications", "Licenses", "Credentials", "Training" };
                    var fui = new FormatUserInformation(responses, categories);
                    var formatted = fui.Format();
                    var ms = MakeWordFile.CreateDocument(formatted);
                    var ms2 = new MemoryStream(ms.ToArray());


                    Spire.Doc.Document doc = new Spire.Doc.Document(ms2);

                    doc.SaveToFile("Portfolio.docx", Spire.Doc.FileFormat.Docx, System.Web.HttpContext.Current.Response, HttpContentType.Attachment);
            
                    //Response.Clear();
                    //Response.AddHeader("content-disposition", "attachment; filename=\"Portfolio.docx\"");
                    //Response.ContentType = "application/msword";
                    //ms2.WriteTo(Response.OutputStream);
                    //Response.End(); 
                }
                catch (Exception ex)
                { Response.Write(ex.Message); }
            }
            else if (Command == "Pdf")
            {
                try
                {
                    var userId = WebSecurity.GetUserId(User.Identity.Name);
                    var responses = _db.Responses.Where(x => x.UserId == userId).OrderBy(x => x.Ordinal).ThenBy(x => x.SubOrdinal).ThenBy(x => x.QQOrd).ToList();
                    var categories = new List<string> { "Personal Information", "Employment", "Education", "Coursework", "Certifications", "Licenses", "Credentials", "Training" };
                    var fui = new FormatUserInformation(responses, categories);
                    var formatted = fui.Format();
                    var ms = MakeWordFile.CreateDocument(formatted);                   
                    var ms2 = new MemoryStream(ms.ToArray());

                    Spire.Doc.Document doc = new Spire.Doc.Document(ms2);

                    doc.SaveToFile("Portfolio.pdf", Spire.Doc.FileFormat.PDF, System.Web.HttpContext.Current.Response, HttpContentType.Attachment);

                    //var appRoot = Request.PhysicalApplicationPath;
                    //var output = appRoot + "Content\\Portfolio.pdf";
                    //var ms3 = new MemoryStream();

                    //Spire.Doc.Document doc = new Spire.Doc.Document(ms2);
                    //doc.SaveToStream(ms3, Spire.Doc.FileFormat.PDF);

                    //Response.Clear();
                    //Response.AddHeader("content-disposition", "attachment; filename=\"Portfolio.pdf\"");
                    //Response.ContentType = "application/pdf";
                    //ms3.WriteTo(Response.OutputStream);
                    //Response.End();

                    //doc.SaveToFile(output, Spire.Doc.FileFormat.PDF);
                    //doc.Close();
                }
                catch (Exception ex)
                { Response.Write(ex.Message); }
            }
            else if (Command == "Certificate")
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                UserLevel userLevel = _db.UserLevels.Where(q => q.UserId == userId).First();
                var certificateDate = userLevel.FinalStepLevelDate.ToString("d");
                var certificateLevel = userLevel.FinalStepLevel;
                var signature = "Zelda Boyd";
                
                string firstName = "";
                string lastName = "";
                string middleInitial = "";
                var user = _udb.UserProfiles.FirstOrDefault(s => s.UserId == userId);
                if (user != null)
                {
                    if (user.FirstName != null && user.FirstName != "")
                        firstName = user.FirstName;
                    else
                        firstName = "FirstName";

                    if (user.LastName != null && user.LastName != "")
                        lastName = user.LastName;
                    else
                        lastName = "LastName";

                    if (user.MiddleInitial != null && user.MiddleInitial != "")
                        middleInitial = user.MiddleInitial;
                    else
                        middleInitial = "";
                }

                string fullName = firstName + " " + (middleInitial != "" ? middleInitial + " " : "") + lastName;
                
                var appRoot = Request.PhysicalApplicationPath;
                var file = appRoot + "Content\\VPDR_Certificate_10.docx";
                var newFile = appRoot + "Content\\VPDR_Certificate_" + lastName + "_" + firstName + ".docx";
                var newPdf = appRoot + "Content\\VPDR_Certificate_" + lastName + "_" + firstName + ".pdf";

                Spire.Doc.Document doc = new Spire.Doc.Document();
                doc.LoadFromFile(file);
                doc.Replace("PROVIDER", fullName, true, true);
                doc.Replace("LEVEL", certificateLevel, true, true);
                doc.Replace("DATE", certificateDate, true, true);
                doc.Replace("SIGNATURE", signature, true, true);
                //doc.SaveToFile(newPdf, Spire.Doc.FileFormat.PDF);
                //doc.SaveToFile(newFile, Spire.Doc.FileFormat.Docx);

                var newPdfName = "VPDR_Certificate_" + lastName + "_" + firstName + ".pdf";
                doc.SaveToFile(newPdfName, Spire.Doc.FileFormat.PDF, System.Web.HttpContext.Current.Response, HttpContentType.Attachment);
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Download");
        }

        //
        // GET: /Response/Edit/5

        public ActionResult Edit(int id = 1)
        {
            ViewBag.Title = "Child Care Professional Registry Application";
            ViewBag.Message = "";
            var userId = WebSecurity.GetUserId(User.Identity.Name);

            var lockedSections =
                _db.Verifications.Where(x => x.UserId == userId && x.Editable == false).Select(x => x.QQCategoryId).ToList();     

            var viewModel = new QuestionnaireAppData();

            /* Add Fully Loaded (.Included) Questionnaire to the ViewModel */
            viewModel.Questionnaire =
                _db.Questionnaires
                    .Include(a => a.QuestionnaireQuestions
                        .Select(b => b.Question.QType).Select(c => c.Answers))
                    .Include(a => a.QuestionnaireQCategories
                        .Select(b => b.QCategory))
                    .Where(n => n.QuestionnaireId == id)
                    .Single();

            viewModel.Questionnaire.QuestionnaireQuestions = viewModel.Questionnaire.QuestionnaireQuestions.Where(x => x.UserId == userId || x.UserId == 0).ToList();

            var appQuestions = new List<Response>();
           
            var qqList = viewModel.Questionnaire.QuestionnaireQuestions.ToList();

            var distinctQQCId = qqList.Select(x => x.QQCategoryId).Distinct();

            for (var i = 0; i < viewModel.Questionnaire.QuestionnaireQuestions.Count(); i++)
            {
                
                var qqId = qqList[i].Id;
                var qqCatId = qqList.Single(x => x.Id == qqId).QQCategoryId;
                if (qqCatId != null)
                {
                    var qqCId = (int)qqCatId;

                    if (lockedSections.Contains(qqCId)) continue;
                }

                var responseItem = _db.Responses.Any(a => a.UserId == userId && a.QuestionnaireQuestionId == qqId) ? _db.Responses.Single(a => a.UserId == userId && a.QuestionnaireQuestionId == qqId).ResponseItem : "";

                var answers = qqList[i].Question.QType.Answers;

                var qCategoryName = qqList[i].QuestionnaireQCategory.QCategory.QCategoryName;
                if (qqList[i].QuestionnaireQCategory.QCategory.QCategoryName != "Personal Information" && qqList[i].QuestionnaireQCategory.SubOrdinal > 0)
                    qCategoryName += " (" + (qqList[i].QuestionnaireQCategory.SubOrdinal + 1) + ")";

                appQuestions.Add(new Response
                {
                    QuestionId = (int)qqList[i].QuestionId,
                    QuestionText = qqList[i].Question.QuestionText,
                    QTitle = qqList[i].Question.QTitle,
                    QTypeResponse = qqList[i].Question.QType.QTypeResponse,
                    QuestionnaireId = (int)qqList[i].QuestionnaireId,
                    QCategoryId = (int)qqList[i].QuestionnaireQCategory.QCategoryId,
                    QCategoryName = qCategoryName,
                    QuestionnaireQuestionId = qqList[i].Id,
                    QuestionnaireQCategoryId = (int)qqList[i].QQCategoryId,
                    QQOrd = qqList[i].Ordinal,
                    Ordinal = qqList[i].QuestionnaireQCategory.Ordinal,
                    SubOrdinal = qqList[i].QuestionnaireQCategory.SubOrdinal,
                    UserId = userId,
                    ResponseItem = responseItem,   
                    Answers = answers
                });
            }

            var returnList = appQuestions.OrderBy(x => x.Ordinal).ThenBy(x => x.SubOrdinal).ThenBy(x => x.QQOrd).ToList();
            viewModel.Responses = returnList;

            var qCategories = _db.QCategories.ToList();
            viewModel.QCategories = qCategories;

            return View(viewModel);
        }

        //
        // POST: /Response/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(IList<Response> model, FormCollection formCollection)
        {
            var qqcIds = model.Select(x => x.QuestionnaireQCategoryId).Distinct().ToList();

            
            var scope = new TransactionScope(
                // a new transaction will always be created
                TransactionScopeOption.RequiresNew,
                // we will allow volatile data to be read during transaction
                new TransactionOptions()
                {
                    IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
                }
            );

            if (ModelState.IsValid)
            {
                using (scope)
                {
                    try
                    {
                        foreach (var record in _db.Responses)
                        {
                            if (record.UserId == model[0].UserId && qqcIds.Contains(record.QuestionnaireQCategoryId))
                                _db.Responses.Remove(record);
                        }
                        _db.SaveChanges();

                        foreach (var r in model)
                        {
                            Response response = r;
                            _db.Responses.Add(response);
                            _db.SaveChanges();
                            var check = _db.Responses.Single(x => x.ResponseId == response.ResponseId);
                        }
                        scope.Complete();
                        return RedirectToAction("Edit", "Response", new { area="", id = 1 });
                    }
                    catch { }
                }
                //db.Entry(Response).State = EntityState.Modified;                
            }
            return View();
        }

        //
        // GET: /Response/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Response response = _db.Responses.Find(id);
            if (response == null)
            {
                return HttpNotFound();
            }
            return View(response);
        }

        //
        // POST: /Response/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Response response = _db.Responses.Find(id);
            _db.Responses.Remove(response);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        //public void tableToPdf(object sender, EventArgs e, string pageHtml)
        //{
        //    //Set page size as A4
        //    iTextSharp.text.Document pdfDoc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 20, 10, 10, 10);

        //    try
        //    {
        //        var ms = new MemoryStream();
        //        PdfWriter.GetInstance(pdfDoc, ms);
        //        //Open PDF Document to write data
        //        pdfDoc.Open();
        //        //Read string contents using stream reader and convert html to parsed conent
        //        var parsedHtmlElements = HTMLWorker.ParseToList(new StringReader(pageHtml), null);
        //        //Get each array values from parsed elements and add to the PDF document
        //        foreach (var htmlElement in parsedHtmlElements)
        //            pdfDoc.Add(htmlElement as IElement);
        //        //Close your PDF
        //        pdfDoc.Close();
        //        var ms2 = new MemoryStream(ms.ToArray());
        //        Response.Buffer = true;
        //        Response.ContentType = "application/pdf";
        //        //Set default file Name as current datetime
        //        Response.AddHeader("content-disposition", "attachment; filename=" + DateTime.Now.ToString("yyyyMMdd") + ".pdf");
        //        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        //        ms2.WriteTo(Response.OutputStream);
        //        Response.End();
        //    }
        //    catch (Exception ex)
        //    {
        //        Response.Write(ex.ToString());
        //    }
        //}

        protected override void Dispose(bool disposing)
        {
            _db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult AjaxTest()
        {
            var testString = "Hi there";
            return PartialView("_AjaxTest", testString);
        }
    }
}