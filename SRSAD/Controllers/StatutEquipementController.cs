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
    public class StatutEquipementController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: StatutEquipement
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var statuts = db.StatutsEquipementRef.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                statuts = statuts.Where(s => s.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                statuts = statuts.Where(s => s.EstActif == true);

            return View(statuts.OrderBy(s => s.Libelle).ToList());
        }

        // GET: StatutEquipement/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsEquipementRef statut = db.StatutsEquipementRef.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // GET: StatutEquipement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: StatutEquipement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif,DeclencheNotif")] StatutsEquipementRef statut)
        {
            if (ModelState.IsValid)
            {
                db.StatutsEquipementRef.Add(statut);
                db.SaveChanges();

                JournaliserAction("CREATE", "StatutsEquipementRef", statut.StatutEquipementID.ToString(), null,
                    $"Création statut équipement: {statut.Code} - {statut.Libelle}");

                TempData["Success"] = "Statut d'équipement créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(statut);
        }

        // GET: StatutEquipement/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            StatutsEquipementRef statut = db.StatutsEquipementRef.Find(id);
            if (statut == null)
                return HttpNotFound();

            return View(statut);
        }

        // POST: StatutEquipement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StatutEquipementID,Code,Libelle,EstActif,DeclencheNotif")] StatutsEquipementRef statut)
        {
            if (ModelState.IsValid)
            {
                var original = db.StatutsEquipementRef.AsNoTracking().FirstOrDefault(s => s.StatutEquipementID == statut.StatutEquipementID);

                db.Entry(statut).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "StatutsEquipementRef", statut.StatutEquipementID.ToString(), original, statut);

                TempData["Success"] = "Statut d'équipement modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(statut);
        }

        // POST: StatutEquipement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            StatutsEquipementRef statut = db.StatutsEquipementRef.Find(id);

            bool estUtilise = db.HistoriqueStatutsEquipement.Any(h => h.StatutEquipementID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce statut ne peut pas être supprimé car il est utilisé dans l'historique des équipements.";
                return RedirectToAction("Index");
            }

            db.StatutsEquipementRef.Remove(statut);
            db.SaveChanges();

            JournaliserAction("DELETE", "StatutsEquipementRef", id.ToString(), statut, null);

            TempData["Success"] = "Statut d'équipement supprimé avec succès.";
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