using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SRSAD.Models;
using Microsoft.AspNet.Identity;

namespace SRSAD.ViewModels
{
    public class ManufacturierController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Manufacturier
        public ActionResult Index(string searchNom, string searchCode, bool? actifUniquement)
        {
            var manufacturiers = db.Manufacturiers.AsQueryable();

            if (!string.IsNullOrEmpty(searchNom))
                manufacturiers = manufacturiers.Where(m => m.Nom.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchCode))
                manufacturiers = manufacturiers.Where(m => m.Code.Contains(searchCode));

            if (actifUniquement ?? true)
                manufacturiers = manufacturiers.Where(m => m.EstActif == true);

            return View(manufacturiers.OrderBy(m => m.Nom).ToList());
        }

        // GET: Manufacturier/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Manufacturiers manufacturier = db.Manufacturiers.Find(id);
            if (manufacturier == null)
                return HttpNotFound();

            return View(manufacturier);
        }

        // GET: Manufacturier/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Manufacturier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Nom,EstActif")] Manufacturiers manufacturier)
        {
            if (ModelState.IsValid)
            {
                db.Manufacturiers.Add(manufacturier);
                db.SaveChanges();

                JournaliserAction("CREATE", "Manufacturiers", manufacturier.ManufacturierID.ToString(), null,
                    $"Création manufacturier: {manufacturier.Code} - {manufacturier.Nom}");

                TempData["Success"] = "Manufacturier créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(manufacturier);
        }

        // GET: Manufacturier/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Manufacturiers manufacturier = db.Manufacturiers.Find(id);
            if (manufacturier == null)
                return HttpNotFound();

            return View(manufacturier);
        }

        // POST: Manufacturier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ManufacturierID,Code,Nom,EstActif")] Manufacturiers manufacturier)
        {
            if (ModelState.IsValid)
            {
                var original = db.Manufacturiers.AsNoTracking().FirstOrDefault(m => m.ManufacturierID == manufacturier.ManufacturierID);

                db.Entry(manufacturier).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "Manufacturiers", manufacturier.ManufacturierID.ToString(), original, manufacturier);

                TempData["Success"] = "Manufacturier modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(manufacturier);
        }

        // POST: Manufacturier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            Manufacturiers manufacturier = db.Manufacturiers.Find(id);

            bool estUtilise = db.Equipements.Any(e => e.ManufacturierID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce manufacturier ne peut pas être supprimé car il est utilisé par des équipements.";
                return RedirectToAction("Index");
            }

            db.Manufacturiers.Remove(manufacturier);
            db.SaveChanges();

            JournaliserAction("DELETE", "Manufacturiers", id.ToString(), manufacturier, null);

            TempData["Success"] = "Manufacturier supprimé avec succès.";
            return RedirectToAction("Index");
        }

        private void JournaliserAction(string action, string table, string clePrimaire, object ancien, object nouveau)
        {
            var audit = new JournalAudit
            {
                DateHeure = DateTime.Now,
                Action = action,
                TableConcernee = table,
                ClePrimaire = clePrimaire,
                AnciennesValeurs = ancien != null ? Newtonsoft.Json.JsonConvert.SerializeObject(ancien) : null,
                NouvellesValeurs = nouveau != null ? Newtonsoft.Json.JsonConvert.SerializeObject(nouveau) : null,
                UtilisateurId = User.Identity.GetUserId()
            };

            db.JournalAudit.Add(audit);
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}