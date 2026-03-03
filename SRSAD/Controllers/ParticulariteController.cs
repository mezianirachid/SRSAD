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
    public class ParticulariteController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Particularite
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var particularites = db.ParticularitesUsagerRef.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                particularites = particularites.Where(p => p.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                particularites = particularites.Where(p => p.EstActif == true);

            return View(particularites.OrderBy(p => p.Libelle).ToList());
        }

        // GET: Particularite/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ParticularitesUsagerRef particularite = db.ParticularitesUsagerRef.Find(id);
            if (particularite == null)
                return HttpNotFound();

            return View(particularite);
        }

        // GET: Particularite/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Particularite/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,NiveauAlerte,EstActif")] ParticularitesUsagerRef particularite)
        {
            if (ModelState.IsValid)
            {
                db.ParticularitesUsagerRef.Add(particularite);
                db.SaveChanges();

                JournaliserAction("CREATE", "ParticularitesUsagerRef", particularite.ParticulariteID.ToString(), null,
                    $"Création particularité: {particularite.Code} - {particularite.Libelle}");

                TempData["Success"] = "Particularité créée avec succès.";
                return RedirectToAction("Index");
            }

            return View(particularite);
        }

        // GET: Particularite/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ParticularitesUsagerRef particularite = db.ParticularitesUsagerRef.Find(id);
            if (particularite == null)
                return HttpNotFound();

            return View(particularite);
        }

        // POST: Particularite/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ParticulariteID,Code,Libelle,NiveauAlerte,EstActif")] ParticularitesUsagerRef particularite)
        {
            if (ModelState.IsValid)
            {
                var original = db.ParticularitesUsagerRef.AsNoTracking().FirstOrDefault(p => p.ParticulariteID == particularite.ParticulariteID);

                db.Entry(particularite).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "ParticularitesUsagerRef", particularite.ParticulariteID.ToString(), original, particularite);

                TempData["Success"] = "Particularité modifiée avec succès.";
                return RedirectToAction("Index");
            }
            return View(particularite);
        }

        // POST: Particularite/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            ParticularitesUsagerRef particularite = db.ParticularitesUsagerRef.Find(id);

            bool estUtilise = db.UsagerParticularites.Any(p => p.ParticulariteID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Cette particularité ne peut pas être supprimée car elle est utilisée par des patients.";
                return RedirectToAction("Index");
            }

            db.ParticularitesUsagerRef.Remove(particularite);
            db.SaveChanges();

            JournaliserAction("DELETE", "ParticularitesUsagerRef", id.ToString(), particularite, null);

            TempData["Success"] = "Particularité supprimée avec succès.";
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