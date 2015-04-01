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
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Web;
using System.Text;
using Aspose.Words;
using Aspose.Words.Saving;
using iTextSharp.text.html.simpleparser;
using Spire.Pdf.HtmlConverter;
using Spire.Pdf;
using Spire.Pdf.Graphics;
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
            
                    Response.Clear();
                    Response.AddHeader("content-disposition", "attachment; filename=\"Portfolio.docx\"");
                    Response.ContentType = "application/msword";
                    ms2.WriteTo(Response.OutputStream);
                    Response.End(); 
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

                    Aspose.Words.Document doc = new Aspose.Words.Document(ms2);
                    var ms3 = new MemoryStream();
                    doc.Save(ms3, SaveFormat.Pdf);

                    Response.Clear();
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("content-disposition", "attachment; filename=\"Portfolio.pdf\"");

                    ms3.WriteTo(Response.OutputStream);
                    Response.End();
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
                

                
                var appRoot = Request.PhysicalApplicationPath;
                var file = appRoot + "Content\\certificate.html";
                var graphic_1 = appRoot + "Images\\VDSS.png";
                var graphic_2 = appRoot + "Images\\Impact.png";
                var output = appRoot + "Content\\output3.pdf";

                //Create a pdf document.
                Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument();

                // Create one page
                // PdfPageBase page = doc.Pages.Add();

                //create section
                PdfSection section = doc.Sections.Add();
                section.PageSettings.Size = PdfPageSize.A4;
                section.PageSettings.Orientation = PdfPageOrientation.Landscape;
                PdfPageBase page = section.Pages.Add();               

                
                // VDSS Logo
                var image = Spire.Pdf.Graphics.PdfImage.FromFile(graphic_1);
                float width = image.Width * 0.45f;
                float height = image.Height * 0.45f;
                float x = (page.Canvas.ClientSize.Width - width) / 2;
                page.Canvas.DrawImage(image, x, 20, width, height);

                // Professional Development Certificate
                PdfStringFormat centerAlignment = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
                page.Canvas.DrawString("Professional Development Certificate",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 36f, PdfFontStyle.Italic),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 125, centerAlignment);

                // is hereby awarded to
                page.Canvas.DrawString("is hereby awarded to",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 16f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 170, centerAlignment);

                // Recipient
                string recipient = firstName + " " + middleInitial + " " + lastName;
                recipient = recipient.Replace("  ", " ");
                page.Canvas.DrawString(recipient,
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 28f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 215, centerAlignment);

                // in recognition of achievement of
                page.Canvas.DrawString("in recognition of achievement of",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 16f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 258, centerAlignment);

                // Level
                string level = certificateLevel;
                page.Canvas.DrawString(level,
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 28f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 303, centerAlignment);

                // Career Pathways for Early Childhood & School-Age Practitioners
                page.Canvas.DrawString("Career Pathways for Early Childhood &\nSchool-Age Practitioners",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 24f, PdfFontStyle.Bold),
                                       new PdfSolidBrush(Color.Black),
                                       page.Canvas.ClientSize.Width / 2, 362, centerAlignment);

                // Impact Logo
                var image2 = Spire.Pdf.Graphics.PdfImage.FromFile(graphic_2);
                float width2 = image2.Width * 0.2f;
                float height2 = image2.Height * 0.2f;
                float x2 = (page.Canvas.ClientSize.Width - width) / 2;
                page.Canvas.DrawImage(image2, x2 + 25, 405, width2, height2);

                // Date line
                page.Canvas.DrawString("_________________",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 24f, PdfFontStyle.Bold),
                                       new PdfSolidBrush(Color.Black),
                                       100, 415);

                // Formated Date
                string dateTime = certificateDate;
                page.Canvas.DrawString(dateTime,
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 18f, PdfFontStyle.Bold),
                                       new PdfSolidBrush(Color.Black),
                                       170, 420);
                
                // Signature line
                page.Canvas.DrawString("_________________",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 24f, PdfFontStyle.Bold),
                                       new PdfSolidBrush(Color.Black),
                                       445, 415);

                // Signature
                string signature = "Zelda Boyd";
                page.Canvas.DrawString(signature,
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 18f, PdfFontStyle.Bold),
                                       new PdfSolidBrush(Color.Black),
                                       505, 420);

                // Date
                page.Canvas.DrawString("Date",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 12f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       190, 445);

                // Office of Early Childhood Development Virginia Department of Social Services
                page.Canvas.DrawString("Office of Early Childhood Development\nVirginia Department of Social Services",
                                       new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.TimesRoman, 12f, PdfFontStyle.Regular),
                                       new PdfSolidBrush(Color.Black),
                                       450, 445);

                // Frame
                PdfPen pen = new PdfPen(Color.Black, 2.0f);
                page.Canvas.DrawRectangle(pen, new System.Drawing.Rectangle(new Point(3, 3), new Size(755, 509)));
                PdfPen pen2 = new PdfPen(Color.Black, 0.5f);
                page.Canvas.DrawRectangle(pen2, new System.Drawing.Rectangle(new Point(0, 0), new Size(762, 515)));

                //Save pdf file.
                //doc.SaveToFile(output);
                //doc.Close();

                doc.SaveToHttpResponse("Certificate.pdf", System.Web.HttpContext.Current.Response, HttpReadType.Open);
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View();
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

        public void tableToPdf(object sender, EventArgs e, string pageHtml)
        {
            //Set page size as A4
            iTextSharp.text.Document pdfDoc = new iTextSharp.text.Document(PageSize.A4, 20, 10, 10, 10);

            try
            {
                var ms = new MemoryStream();
                PdfWriter.GetInstance(pdfDoc, ms);
                //Open PDF Document to write data
                pdfDoc.Open();
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