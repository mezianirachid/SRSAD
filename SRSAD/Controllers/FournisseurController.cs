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
    public class FournisseurController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Fournisseur
        public ActionResult Index(string searchNom, string searchCode, bool? actifUniquement)
        {
            var fournisseurs = db.Fournisseurs.AsQueryable();

            if (!string.IsNullOrEmpty(searchNom))
                fournisseurs = fournisseurs.Where(f => f.Nom.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchCode))
                fournisseurs = fournisseurs.Where(f => f.Code.Contains(searchCode));

            if (actifUniquement ?? true)
                fournisseurs = fournisseurs.Where(f => f.EstActif == true);

            return View(fournisseurs.OrderBy(f => f.Nom).ToList());
        }

        // GET: Fournisseur/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Fournisseurs fournisseur = db.Fournisseurs.Find(id);
            if (fournisseur == null)
                return HttpNotFound();

            return View(fournisseur);
        }

        // GET: Fournisseur/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Fournisseur/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Nom,EstActif")] Fournisseurs fournisseur)
        {
            if (ModelState.IsValid)
            {
                db.Fournisseurs.Add(fournisseur);
                db.SaveChanges();

                JournaliserAction("CREATE", "Fournisseurs", fournisseur.FournisseurID.ToString(), null,
                    $"Création fournisseur: {fournisseur.Code} - {fournisseur.Nom}");

                TempData["Success"] = "Fournisseur créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(fournisseur);
        }

        // GET: Fournisseur/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Fournisseurs fournisseur = db.Fournisseurs.Find(id);
            if (fournisseur == null)
                return HttpNotFound();

            return View(fournisseur);
        }

        // POST: Fournisseur/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FournisseurID,Code,Nom,EstActif")] Fournisseurs fournisseur)
        {
            if (ModelState.IsValid)
            {
                var original = db.Fournisseurs.AsNoTracking().FirstOrDefault(f => f.FournisseurID == fournisseur.FournisseurID);

                db.Entry(fournisseur).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "Fournisseurs", fournisseur.FournisseurID.ToString(), original, fournisseur);

                TempData["Success"] = "Fournisseur modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(fournisseur);
        }

        // POST: Fournisseur/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            Fournisseurs fournisseur = db.Fournisseurs.Find(id);

            bool estUtilise = db.Equipements.Any(e => e.FournisseurID == id) ||
                              db.CommandesCylindres.Any(c => c.FournisseurID == id) ||
                              db.ImportFacturationCylindres.Any(i => i.FournisseurID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce fournisseur ne peut pas être supprimé car il est utilisé.";
                return RedirectToAction("Index");
            }

            db.Fournisseurs.Remove(fournisseur);
            db.SaveChanges();

            JournaliserAction("DELETE", "Fournisseurs", id.ToString(), fournisseur, null);

            TempData["Success"] = "Fournisseur supprimé avec succès.";
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