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
    public class InfestationController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Infestation
        public ActionResult Index(string searchLibelle, bool? actifUniquement)
        {
            var infestations = db.TypesInfestation.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                infestations = infestations.Where(i => i.Libelle.Contains(searchLibelle));

            if (actifUniquement ?? true)
                infestations = infestations.Where(i => i.EstActif == true);

            return View(infestations.OrderBy(i => i.Libelle).ToList());
        }

        // GET: Infestation/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesInfestation infestation = db.TypesInfestation.Find(id);
            if (infestation == null)
                return HttpNotFound();

            return View(infestation);
        }

        // GET: Infestation/Create
        public ActionResult Create()
        {
            // Extraire les catégories distinctes de la table TypesInfection
            var categories = db.TypesInfestation
                .Where(i => i.Libelle != null && i.Libelle != "")
                .Select(i => i.Libelle)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Ajouter "Autre" si pas déjà présent
            if (!categories.Contains("Autre"))
            {
                categories.Add("Autre");
            }

            ViewBag.Categories = new SelectList(categories);
            return View();
        }

        // POST: Infestation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,EstActif")] TypesInfestation infestation)
        {
            if (ModelState.IsValid)
            {
                db.TypesInfestation.Add(infestation);
                db.SaveChanges();

                JournaliserAction("CREATE", "TypesInfestation", infestation.TypeInfestationID.ToString(), null,
                    $"Création infestation: {infestation.Code} - {infestation.Libelle}");

                TempData["Success"] = "Type d'infestation créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(infestation);
        }

        // GET: Infestation/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesInfestation infestation = db.TypesInfestation.Find(id);
            if (infestation == null)
                return HttpNotFound();

            return View(infestation);
        }

        // POST: Infestation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TypeInfestationID,Code,Libelle,EstActif")] TypesInfestation infestation)
        {
            if (ModelState.IsValid)
            {
                var original = db.TypesInfestation.AsNoTracking().FirstOrDefault(i => i.TypeInfestationID == infestation.TypeInfestationID);

                db.Entry(infestation).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "TypesInfestation", infestation.TypeInfestationID.ToString(), original, infestation);

                TempData["Success"] = "Type d'infestation modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(infestation);
        }

        // POST: Infestation/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            TypesInfestation infestation = db.TypesInfestation.Find(id);

            bool estUtilise = db.UsagerInfestations.Any(i => i.TypeInfestationID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce type d'infestation ne peut pas être supprimé car il est utilisé par des patients.";
                return RedirectToAction("Index");
            }

            db.TypesInfestation.Remove(infestation);
            db.SaveChanges();

            JournaliserAction("DELETE", "TypesInfestation", id.ToString(), infestation, null);

            TempData["Success"] = "Type d'infestation supprimé avec succès.";
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