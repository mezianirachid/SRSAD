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
    public class MotifCommandeCylindreController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: MotifCommandeCylindre
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var motifs = db.MotifsCommandeCylindreRef.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                motifs = motifs.Where(m => m.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                motifs = motifs.Where(m => m.EstActif == true);

            return View(motifs.OrderBy(m => m.Libelle).ToList());
        }

        // GET: MotifCommandeCylindre/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            MotifsCommandeCylindreRef motif = db.MotifsCommandeCylindreRef.Find(id);
            if (motif == null)
                return HttpNotFound();

            return View(motif);
        }

        // GET: MotifCommandeCylindre/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MotifCommandeCylindre/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,ForfaitCode,EstActif")] MotifsCommandeCylindreRef motif)
        {
            if (ModelState.IsValid)
            {
                db.MotifsCommandeCylindreRef.Add(motif);
                db.SaveChanges();

                JournaliserAction("CREATE", "MotifsCommandeCylindreRef", motif.MotifCommandeID.ToString(), null,
                    $"Création motif commande: {motif.Code} - {motif.Libelle}");

                TempData["Success"] = "Motif de commande créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(motif);
        }

        // GET: MotifCommandeCylindre/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            MotifsCommandeCylindreRef motif = db.MotifsCommandeCylindreRef.Find(id);
            if (motif == null)
                return HttpNotFound();

            return View(motif);
        }

        // POST: MotifCommandeCylindre/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MotifCommandeID,Code,Libelle,ForfaitCode,EstActif")] MotifsCommandeCylindreRef motif)
        {
            if (ModelState.IsValid)
            {
                var original = db.MotifsCommandeCylindreRef.AsNoTracking().FirstOrDefault(m => m.MotifCommandeID == motif.MotifCommandeID);

                db.Entry(motif).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "MotifsCommandeCylindreRef", motif.MotifCommandeID.ToString(), original, motif);

                TempData["Success"] = "Motif de commande modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(motif);
        }

        // POST: MotifCommandeCylindre/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            MotifsCommandeCylindreRef motif = db.MotifsCommandeCylindreRef.Find(id);

            bool estUtilise = db.CommandesCylindres.Any(c => c.MotifCommandeID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce motif ne peut pas être supprimé car il est utilisé dans des commandes.";
                return RedirectToAction("Index");
            }

            db.MotifsCommandeCylindreRef.Remove(motif);
            db.SaveChanges();

            JournaliserAction("DELETE", "MotifsCommandeCylindreRef", id.ToString(), motif, null);

            TempData["Success"] = "Motif de commande supprimé avec succès.";
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