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
    public class TypeEquipementController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: TypeEquipement
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var types = db.TypesEquipement.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                types = types.Where(t => t.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                types = types.Where(t => t.EstActif == true);

            return View(types.OrderBy(t => t.Libelle).ToList());
        }

        // GET: TypeEquipement/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesEquipement type = db.TypesEquipement.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // GET: TypeEquipement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TypeEquipement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif")] TypesEquipement type)
        {
            if (ModelState.IsValid)
            {
                db.TypesEquipement.Add(type);
                db.SaveChanges();

                JournaliserAction("CREATE", "TypesEquipement", type.TypeEquipementID.ToString(), null,
                    $"Création type équipement: {type.Code} - {type.Libelle}");

                TempData["Success"] = "Type d'équipement créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(type);
        }

        // GET: TypeEquipement/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesEquipement type = db.TypesEquipement.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        // POST: TypeEquipement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TypeEquipementID,Code,Libelle,EstActif")] TypesEquipement type)
        {
            if (ModelState.IsValid)
            {
                var original = db.TypesEquipement.AsNoTracking().FirstOrDefault(t => t.TypeEquipementID == type.TypeEquipementID);

                db.Entry(type).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "TypesEquipement", type.TypeEquipementID.ToString(), original, type);

                TempData["Success"] = "Type d'équipement modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(type);
        }

        // POST: TypeEquipement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            TypesEquipement type = db.TypesEquipement.Find(id);

            bool estUtilise = db.Equipements.Any(e => e.TypeEquipementID == id) ||
                              db.GabaritsEntretien.Any(g => g.TypeEquipementID == id) ||
                              db.SeuilsInventaire.Any(s => s.TypeEquipementID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce type d'équipement ne peut pas être supprimé car il est utilisé.";
                return RedirectToAction("Index");
            }

            db.TypesEquipement.Remove(type);
            db.SaveChanges();

            JournaliserAction("DELETE", "TypesEquipement", id.ToString(), type, null);

            TempData["Success"] = "Type d'équipement supprimé avec succès.";
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