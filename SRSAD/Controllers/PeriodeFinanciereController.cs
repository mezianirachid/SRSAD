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
    public class PeriodeFinanciereController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: PeriodeFinanciere
        public ActionResult Index(string searchCode, bool? activesUniquement)
        {
            var periodes = db.PeriodesFinancieresRef.AsQueryable();

            if (!string.IsNullOrEmpty(searchCode))
                periodes = periodes.Where(p => p.Code.Contains(searchCode));

            if (activesUniquement ?? false)
                periodes = periodes.Where(p => p.EstActive == true);

            return View(periodes.OrderByDescending(p => p.DateDebut).ToList());
        }

        // GET: PeriodeFinanciere/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            PeriodesFinancieresRef periode = db.PeriodesFinancieresRef.Find(id);
            if (periode == null)
                return HttpNotFound();

            return View(periode);
        }

        // GET: PeriodeFinanciere/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PeriodeFinanciere/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,DateDebut,DateFin,EstActive")] PeriodesFinancieresRef periode)
        {
            if (ModelState.IsValid)
            {
                // Vérifier qu'une seule période active
                if (periode.EstActive)
                {
                    var activeExistante = db.PeriodesFinancieresRef.FirstOrDefault(p => p.EstActive);
                    if (activeExistante != null)
                    {
                        ModelState.AddModelError("EstActive", "Une période financière est déjà active. Désactivez-la d'abord.");
                        return View(periode);
                    }
                }

                db.PeriodesFinancieresRef.Add(periode);
                db.SaveChanges();

                JournaliserAction("CREATE", "PeriodesFinancieresRef", periode.PeriodeFinID.ToString(), null,
                    $"Création période financière: {periode.Code}");

                TempData["Success"] = "Période financière créée avec succès.";
                return RedirectToAction("Index");
            }

            return View(periode);
        }

        // GET: PeriodeFinanciere/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            PeriodesFinancieresRef periode = db.PeriodesFinancieresRef.Find(id);
            if (periode == null)
                return HttpNotFound();

            return View(periode);
        }

        // POST: PeriodeFinanciere/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PeriodeFinID,Code,DateDebut,DateFin,EstActive")] PeriodesFinancieresRef periode)
        {
            if (ModelState.IsValid)
            {
                // Vérifier qu'une seule période active
                if (periode.EstActive)
                {
                    var activeExistante = db.PeriodesFinancieresRef
                        .FirstOrDefault(p => p.EstActive && p.PeriodeFinID != periode.PeriodeFinID);
                    if (activeExistante != null)
                    {
                        ModelState.AddModelError("EstActive", "Une autre période financière est déjà active.");
                        return View(periode);
                    }
                }

                var original = db.PeriodesFinancieresRef.AsNoTracking().FirstOrDefault(p => p.PeriodeFinID == periode.PeriodeFinID);

                db.Entry(periode).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "PeriodesFinancieresRef", periode.PeriodeFinID.ToString(), original, periode);

                TempData["Success"] = "Période financière modifiée avec succès.";
                return RedirectToAction("Index");
            }
            return View(periode);
        }

        // POST: PeriodeFinanciere/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            PeriodesFinancieresRef periode = db.PeriodesFinancieresRef.Find(id);

            bool estUtilise = db.ImportFacturationCylindres.Any(i => i.PeriodeFinID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Cette période financière ne peut pas être supprimée car elle est utilisée dans des imports de facturation.";
                return RedirectToAction("Index");
            }

            db.PeriodesFinancieresRef.Remove(periode);
            db.SaveChanges();

            JournaliserAction("DELETE", "PeriodesFinancieresRef", id.ToString(), periode, null);

            TempData["Success"] = "Période financière supprimée avec succès.";
            return RedirectToAction("Index");
        }

        // POST: PeriodeFinanciere/Activer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activer(int id)
        {
            var periode = db.PeriodesFinancieresRef.Find(id);
            if (periode == null)
                return HttpNotFound();

            // Désactiver toutes les périodes
            var periodesActives = db.PeriodesFinancieresRef.Where(p => p.EstActive);
            foreach (var p in periodesActives)
            {
                p.EstActive = false;
            }

            periode.EstActive = true;
            db.SaveChanges();

            JournaliserAction("ACTIVER", "PeriodesFinancieresRef", id.ToString(), null,
                $"Activation période financière: {periode.Code}");

            TempData["Success"] = "Période financière activée avec succès.";
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