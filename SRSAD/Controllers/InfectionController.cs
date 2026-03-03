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
    public class InfectionController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Infection
        public ActionResult Index(string searchLibelle, string searchCategorie, bool? actifUniquement)
        {
            var infections = db.TypesInfection.AsQueryable();

            if (!string.IsNullOrEmpty(searchLibelle))
                infections = infections.Where(i => i.Libelle.Contains(searchLibelle));

            if (!string.IsNullOrEmpty(searchCategorie))
                infections = infections.Where(i => i.Categorie.Contains(searchCategorie));

            if (actifUniquement ?? true)
                infections = infections.Where(i => i.EstActif == true);

            return View(infections.OrderBy(i => i.Libelle).ToList());
        }

        // GET: Infection/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesInfection infection = db.TypesInfection.Find(id);
            if (infection == null)
                return HttpNotFound();

            return View(infection);
        }

        // GET: Infection/Create
        public ActionResult Create()
        {
            // Extraire les catégories distinctes de la table TypesInfection
            var categories = db.TypesInfection
                .Where(i => i.Categorie != null && i.Categorie != "")
                .Select(i => i.Categorie)
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

        // POST: Infection/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Libelle,Categorie,EstActif")] TypesInfection infection)
        {
            if (ModelState.IsValid)
            {
                db.TypesInfection.Add(infection);
                db.SaveChanges();

                JournaliserAction("CREATE", "TypesInfection", infection.TypeInfectionID.ToString(), null,
                    $"Création infection: {infection.Code} - {infection.Libelle}");

                TempData["Success"] = "Type d'infection créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(infection);
        }

        // GET: Infection/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TypesInfection infection = db.TypesInfection.Find(id);
            if (infection == null)
                return HttpNotFound();

            return View(infection);
        }

        // POST: Infection/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TypeInfectionID,Code,Libelle,Categorie,EstActif")] TypesInfection infection)
        {
            if (ModelState.IsValid)
            {
                var original = db.TypesInfection.AsNoTracking().FirstOrDefault(i => i.TypeInfectionID == infection.TypeInfectionID);

                db.Entry(infection).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "TypesInfection", infection.TypeInfectionID.ToString(), original, infection);

                TempData["Success"] = "Type d'infection modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(infection);
        }

        // POST: Infection/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            TypesInfection infection = db.TypesInfection.Find(id);

            bool estUtilise = db.UsagerInfections.Any(i => i.TypeInfectionID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce type d'infection ne peut pas être supprimé car il est utilisé par des patients.";
                return RedirectToAction("Index");
            }

            db.TypesInfection.Remove(infection);
            db.SaveChanges();

            JournaliserAction("DELETE", "TypesInfection", id.ToString(), infection, null);

            TempData["Success"] = "Type d'infection supprimé avec succès.";
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