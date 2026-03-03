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
    public class TypeLivraisonController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: TypeLivraison
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var types = db.TypesLivraisonRef.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                types = types.Where(t => t.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                types = types.Where(t => t.EstActif == true);

            return View(types.OrderBy(t => t.Libelle).ToList());
        }

        // GET: TypeLivraison/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesLivraisonRef type = db.TypesLivraisonRef.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // GET: TypeLivraison/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TypeLivraison/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,Surcout,EstActif")] TypesLivraisonRef type)
        {
            if (ModelState.IsValid)
            {
                db.TypesLivraisonRef.Add(type);
                db.SaveChanges();

                JournaliserAction("CREATE", "TypesLivraisonRef", type.TypeLivraisonID.ToString(), null,
                    $"Création type livraison: {type.Code} - {type.Libelle}");

                TempData["Success"] = "Type de livraison créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(type);
        }

        // GET: TypeLivraison/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesLivraisonRef type = db.TypesLivraisonRef.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // POST: TypeLivraison/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TypeLivraisonID,Code,Libelle,Surcout,EstActif")] TypesLivraisonRef type)
        {
            if (ModelState.IsValid)
            {
                var original = db.TypesLivraisonRef.AsNoTracking().FirstOrDefault(t => t.TypeLivraisonID == type.TypeLivraisonID);

                db.Entry(type).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "TypesLivraisonRef", type.TypeLivraisonID.ToString(), original, type);

                TempData["Success"] = "Type de livraison modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(type);
        }

        // POST: TypeLivraison/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            TypesLivraisonRef type = db.TypesLivraisonRef.Find(id);

            bool estUtilise = db.CommandesCylindres.Any(c => c.TypeLivraisonID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce type de livraison ne peut pas être supprimé car il est utilisé dans des commandes.";
                return RedirectToAction("Index");
            }

            db.TypesLivraisonRef.Remove(type);
            db.SaveChanges();

            JournaliserAction("DELETE", "TypesLivraisonRef", id.ToString(), type, null);

            TempData["Success"] = "Type de livraison supprimé avec succès.";
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