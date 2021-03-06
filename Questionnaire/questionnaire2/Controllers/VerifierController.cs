﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Questionnaire2.Controllers
{
    [Authorize(Roles = "Administrator, Verifier")]
    public class VerifierController : Controller
    {
        //
        // GET: /Verifier/
        
        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Verifier/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Verifier/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Verifier/Create

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

        //
        // GET: /Verifier/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /Verifier/Edit/5

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

        //
        // GET: /Verifier/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Verifier/Delete/5

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
