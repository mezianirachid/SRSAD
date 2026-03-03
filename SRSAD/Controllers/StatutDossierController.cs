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
    public class StatutDossierController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: StatutDossier
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var statuts = db.StatutsDossierUsager.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                statuts = statuts.Where(s => s.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                statuts = statuts.Where(s => s.EstActif == true);

            return View(statuts.OrderBy(s => s.Libelle).ToList());
        }

        // GET: StatutDossier/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsDossierUsager statut = db.StatutsDossierUsager.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // GET: StatutDossier/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: StatutDossier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif")] StatutsDossierUsager statut)
        {
            if (ModelState.IsValid)
            {
                db.StatutsDossierUsager.Add(statut);
                db.SaveChanges();

                JournaliserAction("CREATE", "StatutsDossierUsager", statut.StatutDossierID.ToString(), null,
                    $"Création statut dossier: {statut.Code} - {statut.Libelle}");

                TempData["Success"] = "Statut de dossier créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(statut);
        }

        // GET: StatutDossier/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsDossierUsager statut = db.StatutsDossierUsager.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // POST: StatutDossier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StatutDossierID,Code,Libelle,EstActif")] StatutsDossierUsager statut)
        {
            if (ModelState.IsValid)
            {
                var original = db.StatutsDossierUsager.AsNoTracking().FirstOrDefault(s => s.StatutDossierID == statut.StatutDossierID);

                db.Entry(statut).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "StatutsDossierUsager", statut.StatutDossierID.ToString(), original, statut);

                TempData["Success"] = "Statut de dossier modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(statut);
        }

        // POST: StatutDossier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            StatutsDossierUsager statut = db.StatutsDossierUsager.Find(id);

            bool estUtilise = db.Usagers.Any(u => u.StatutDossierID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce statut de dossier ne peut pas être supprimé car il est utilisé par des patients.";
                return RedirectToAction("Index");
            }

            db.StatutsDossierUsager.Remove(statut);
            db.SaveChanges();

            JournaliserAction("DELETE", "StatutsDossierUsager", id.ToString(), statut, null);

            TempData["Success"] = "Statut de dossier supprimé avec succès.";
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