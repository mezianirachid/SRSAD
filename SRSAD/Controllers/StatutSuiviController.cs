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
    public class StatutSuiviController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: StatutSuivi
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var statuts = db.StatutsSuivi.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                statuts = statuts.Where(s => s.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                statuts = statuts.Where(s => s.EstActif == true);

            return View(statuts.OrderBy(s => s.Libelle).ToList());
        }

        // GET: StatutSuivi/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsSuivi statut = db.StatutsSuivi.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // GET: StatutSuivi/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: StatutSuivi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif")] StatutsSuivi statut)
        {
            if (ModelState.IsValid)
            {
                db.StatutsSuivi.Add(statut);
                db.SaveChanges();

                JournaliserAction("CREATE", "StatutsSuivi", statut.StatutSuiviID.ToString(), null,
                    $"Création statut suivi: {statut.Code} - {statut.Libelle}");

                TempData["Success"] = "Statut de suivi créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(statut);
        }

        // GET: StatutSuivi/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsSuivi statut = db.StatutsSuivi.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // POST: StatutSuivi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StatutSuiviID,Code,Libelle,EstActif")] StatutsSuivi statut)
        {
            if (ModelState.IsValid)
            {
                var original = db.StatutsSuivi.AsNoTracking().FirstOrDefault(s => s.StatutSuiviID == statut.StatutSuiviID);

                db.Entry(statut).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "StatutsSuivi", statut.StatutSuiviID.ToString(), original, statut);

                TempData["Success"] = "Statut de suivi modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(statut);
        }

        // POST: StatutSuivi/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            StatutsSuivi statut = db.StatutsSuivi.Find(id);

            bool estUtilise = db.Assignations.Any(a => a.StatutSuiviID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce statut de suivi ne peut pas être supprimé car il est utilisé dans des assignations.";
                return RedirectToAction("Index");
            }

            db.StatutsSuivi.Remove(statut);
            db.SaveChanges();

            JournaliserAction("DELETE", "StatutsSuivi", id.ToString(), statut, null);

            TempData["Success"] = "Statut de suivi supprimé avec succès.";
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