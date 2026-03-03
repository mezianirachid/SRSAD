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
    public class TypeIntervenantController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: TypeIntervenant
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var types = db.TypesIntervenant.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                types = types.Where(t => t.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                types = types.Where(t => t.EstActif == true);

            return View(types.OrderBy(t => t.Libelle).ToList());
        }

        // GET: TypeIntervenant/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesIntervenant type = db.TypesIntervenant.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // GET: TypeIntervenant/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TypeIntervenant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif")] TypesIntervenant type)
        {
            if (ModelState.IsValid)
            {
                db.TypesIntervenant.Add(type);
                db.SaveChanges();

                JournaliserAction("CREATE", "TypesIntervenant", type.TypeIntervenantID.ToString(), null,
                    $"Création type intervenant: {type.Code} - {type.Libelle}");

                TempData["Success"] = "Type d'intervenant créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(type);
        }

        // GET: TypeIntervenant/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesIntervenant type = db.TypesIntervenant.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // POST: TypeIntervenant/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TypeIntervenantID,Code,Libelle,EstActif")] TypesIntervenant type)
        {
            if (ModelState.IsValid)
            {
                var original = db.TypesIntervenant.AsNoTracking().FirstOrDefault(t => t.TypeIntervenantID == type.TypeIntervenantID);

                db.Entry(type).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "TypesIntervenant", type.TypeIntervenantID.ToString(), original, type);

                TempData["Success"] = "Type d'intervenant modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(type);
        }

        // POST: TypeIntervenant/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            TypesIntervenant type = db.TypesIntervenant.Find(id);

            bool estUtilise = db.Intervenants.Any(i => i.TypeIntervenantID == id) ||
                              db.Assignations.Any(a => a.TypeIntervenantID == id) ||
                              db.Visites.Any(v => v.TypeIntervenantID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce type d'intervenant ne peut pas être supprimé car il est utilisé.";
                return RedirectToAction("Index");
            }

            db.TypesIntervenant.Remove(type);
            db.SaveChanges();

            JournaliserAction("DELETE", "TypesIntervenant", id.ToString(), type, null);

            TempData["Success"] = "Type d'intervenant supprimé avec succès.";
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