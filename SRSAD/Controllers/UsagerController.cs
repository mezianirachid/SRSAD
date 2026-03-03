using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using SRSAD.Models;
using SRSAD.ViewModels;
 

namespace SRSAD.ViewModels
{
    public class UsagerController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Usager/Index (Liste des usagers)
        public ActionResult Index(string searchNom, string searchPrenom, string searchIPM, int? secteurId, int? statutDossierId)
        {
            var usagers = db.Usagers
                .Include(u => u.SecteursSRSAD)
                .Include(u => u.StatutsDossierUsager)
                .Include(u => u.UsagerAdresses)
                .Where(u => u.EstActif);

            // Filtres
            if (!string.IsNullOrEmpty(searchNom))
                usagers = usagers.Where(u => u.Nom.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchPrenom))
                usagers = usagers.Where(u => u.Prenom.Contains(searchPrenom));

            if (!string.IsNullOrEmpty(searchIPM))
                usagers = usagers.Where(u => u.NoDossierIPM.Contains(searchIPM));

            if (secteurId.HasValue)
                usagers = usagers.Where(u => u.SecteurID == secteurId);

            if (statutDossierId.HasValue)
                usagers = usagers.Where(u => u.StatutDossierID == statutDossierId);

            var model = usagers.OrderBy(u => u.Nom)
                .Select(u => new UsagerListViewModel
                {
                    UsagerID = u.UsagerID,
                    NoDossierSRSAD = u.NoDossierSRSAD,
                    NoDossierIPM = u.NoDossierIPM,
                    NomComplet = u.Nom + " " + u.Prenom,
                    DateNaissance = u.DateNaissance,
                    RAMQ = u.RAMQ,
                    Secteur = u.SecteursSRSAD.Nom,
                    StatutDossier = u.StatutsDossierUsager.Libelle,
                    EstActif = u.EstActif,
                    AdresseCourante = u.UsagerAdresses
                        .Where(a => a.EstCourante)
                        .Select(a => a.Adresse1 + ", " + a.Ville)
                        .FirstOrDefault(),
                    Telephone = u.Telephone
                }).ToList();

            ViewBag.Secteurs = new SelectList(db.SecteursSRSAD.Where(s => s.EstActif), "SecteurID", "Nom");
            ViewBag.Statuts = new SelectList(db.StatutsDossierUsager.Where(s => s.EstActif), "StatutDossierID", "Libelle");

            return View(model);
        }

        // GET: Usager/Create (Étape 1 : Recherche dans E-Clinibase)
        public ActionResult Create()
        {
            return View(new RechercheEClinibaseViewModel());
        }

        // POST: Usager/Create (Étape 1 : Soumission de la recherche)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RechercheEClinibaseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-afficher la vue avec les erreurs
                return View(model);
            }

            // Vérifier qu'au moins un critère de recherche est fourni
            if (string.IsNullOrEmpty(model.Nom) && string.IsNullOrEmpty(model.Prenom) &&
                !model.DateNaissance.HasValue && string.IsNullOrEmpty(model.RAMQ) &&
                string.IsNullOrEmpty(model.NoIPM))
            {
                ModelState.AddModelError("", "Veuillez fournir au moins un critère de recherche.");
                return View(model);
            }

            try
            {
                // Simuler un délai d'appel API
                await Task.Delay(500);

                // Recherche dans E-Clinibase (simulée)
                var resultats = await RechercherDansEClinibase(model);

                if (!resultats.Any())
                {
                    TempData["Error"] = "Patient non trouvé dans E-Clinibase. La création est impossible.";
                    return RedirectToAction("Create");
                }

                // Si un seul résultat, aller directement à l'import
                if (resultats.Count == 1)
                {
                    return RedirectToAction("Importer", new { ipm = resultats.First().NoIPM });
                }

                // Sinon, afficher la liste des résultats
                return View("ResultatsRecherche", resultats);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erreur lors de la recherche dans E-Clinibase : " + ex.Message);
                return View(model);
            }
        }

        // GET: Usager/Importer/{ipm} (Étape 2 : Import des données)
        public ActionResult Importer(string ipm)
        {
            if (string.IsNullOrEmpty(ipm))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Vérifier si le patient existe déjà
            var existant = db.Usagers.FirstOrDefault(u => u.NoDossierIPM == ipm);
            if (existant != null)
            {
                TempData["Warning"] = "Ce patient existe déjà dans SRSAD.";
                return RedirectToAction("Details", new { id = existant.UsagerID });
            }

            // Récupérer les données d'E-Clinibase
            var donnees = RecupererDonneesCompletesEClinibase(ipm);

            if (donnees == null)
            {
                TempData["Error"] = "Impossible de récupérer les données d'E-Clinibase.";
                return RedirectToAction("Create");
            }

            // Préparer le ViewModel avec toutes les données importées
            var viewModel = new ImportEClinibaseViewModel
            {
                // Informations de base
                NoDossierIPM = donnees.NoIPM,
                RAMQ = donnees.RAMQ,
                Nom = donnees.Nom,
                Prenom = donnees.Prenom,
                DateNaissance = donnees.DateNaissance,
                Sexe = donnees.Sexe,
                Telephone = donnees.Telephone,

                // Adresses (toutes importées automatiquement)
                Adresses = donnees.Adresses.Select(a => new AdresseImportViewModel
                {
                    TypeAdresse = a.TypeAdresse,
                    SourceAdresse = "ECLINIBASE",
                    Adresse1 = a.Adresse1,
                    Adresse2 = a.Adresse2,
                    Ville = a.Ville,
                    Province = a.Province,
                    CodePostal = a.CodePostal,
                    Pays = a.Pays,
                    Telephone = a.Telephone,
                    DateDebutValidite = a.DateDebutValidite,
                    DateFinValidite = a.DateFinValidite,
                    EClinibaseAdresseId = a.EClinibaseAdresseId,
                    EstCourante = a.TypeAdresse == "PERMANENTE" // L'adresse permanente est courante par défaut
                }).ToList(),

                // Informations administratives SRSAD (à compléter)
                EstActif = true,
                CodeHydroQuebec = false,
                Pays = "Canada",
                Province = "Québec",
                DateDerniereSyncEClinibase = DateTime.Now
            };

            // Charger les listes déroulantes
            ViewBag.CLSCID = new SelectList(db.CLSC.Where(c => c.EstActif), "CLSCID", "NomCLSC");
            ViewBag.SecteurID = new SelectList(db.SecteursSRSAD.Where(s => s.EstActif), "SecteurID", "Nom");
            ViewBag.StatutDossierID = new SelectList(db.StatutsDossierUsager.Where(s => s.EstActif), "StatutDossierID", "Libelle");
            ViewBag.AgentPayeurID = new SelectList(db.AgentsPayeurs.Where(a => a.EstActif), "AgentPayeurID", "Nom");

            return View(viewModel);
        }

        // POST: Usager/Importer (Étape 3 : Création finale)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Importer(ImportEClinibaseViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Générer le numéro de dossier SRSAD
                        string noDossierSRSAD = GenererNumeroDossierSRSAD();

                        // Créer l'usager
                        var usager = new Usagers
                        {
                            NoDossierIPM = model.NoDossierIPM,
                            NoDossierSRSAD = noDossierSRSAD,
                            RAMQ = model.RAMQ,
                            Nom = model.Nom,
                            Prenom = model.Prenom,
                            DateNaissance = model.DateNaissance,
                            Sexe = model.Sexe,
                            Telephone = model.Telephone,

                            // Adresse snapshot (prendre l'adresse permanente)
                            Adresse = model.Adresses.FirstOrDefault(a => a.TypeAdresse == "PERMANENTE")?.Adresse1,
                            Ville = model.Adresses.FirstOrDefault(a => a.TypeAdresse == "PERMANENTE")?.Ville,
                            Province = model.Adresses.FirstOrDefault(a => a.TypeAdresse == "PERMANENTE")?.Province,
                            CodePostal = model.Adresses.FirstOrDefault(a => a.TypeAdresse == "PERMANENTE")?.CodePostal,
                            Pays = model.Adresses.FirstOrDefault(a => a.TypeAdresse == "PERMANENTE")?.Pays ?? "Canada",

                            // Informations administratives
                            CLSCID = model.CLSCID,
                            SecteurID = model.SecteurID,
                            StatutDossierID = model.StatutDossierID,
                            CodeHydroQuebec = model.CodeHydroQuebec,
                            AgentPayeurID = model.AgentPayeurID,
                            CentreDeCout = model.CentreDeCout,
                            BonCommande = model.BonCommande,
                            StatutTabagique = model.StatutTabagique,

                            // Métadonnées
                            EstActif = true,
                            DateCreation = DateTime.Now,
                            CreeParUserId = User.Identity.GetUserId(),
                            DateDerniereSyncEClinibase = DateTime.Now
                        };

                        db.Usagers.Add(usager);
                        db.SaveChanges();

                        // Importer toutes les adresses
                        foreach (var adresseVM in model.Adresses)
                        {
                            var adresse = new UsagerAdresses
                            {
                                UsagerID = usager.UsagerID,
                                TypeAdresse = adresseVM.TypeAdresse,
                                SourceAdresse = "ECLINIBASE",
                                EstCourante = adresseVM.TypeAdresse == "PERMANENTE",
                                Adresse1 = adresseVM.Adresse1,
                                Adresse2 = adresseVM.Adresse2,
                                Ville = adresseVM.Ville,
                                Province = adresseVM.Province,
                                CodePostal = adresseVM.CodePostal,
                                Pays = adresseVM.Pays,
                                Telephone = adresseVM.Telephone,
                                DateDebutValidite = adresseVM.DateDebutValidite,
                                DateFinValidite = adresseVM.DateFinValidite,
                                EClinibaseAdresseId = adresseVM.EClinibaseAdresseId,
                                DateCreation = DateTime.Now,
                                CreeParUserId = User.Identity.GetUserId()
                            };
                            db.UsagerAdresses.Add(adresse);
                        }

                        db.SaveChanges();

                        // Journaliser l'action
                        JournaliserAction("IMPORT_ECLINIBASE", "Usagers", usager.UsagerID.ToString(), null,
                            $"Création depuis E-Clinibase - IPM: {model.NoDossierIPM}");

                        transaction.Commit();

                        TempData["Success"] = $"✅ Patient créé avec succès !\n" +
                            $"Numéro de dossier SRSAD : {noDossierSRSAD}\n" +
                            $"{model.Adresses.Count} adresse(s) importée(s) depuis E-Clinibase.";

                        return RedirectToAction("Details", new { id = usager.UsagerID });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "❌ Erreur lors de la création : " + ex.Message);
                    }
                }
            }

            // Recharger les listes déroulantes en cas d'erreur
            ViewBag.CLSCID = new SelectList(db.CLSC.Where(c => c.EstActif), "CLSCID", "NomCLSC", model.CLSCID);
            ViewBag.SecteurID = new SelectList(db.SecteursSRSAD.Where(s => s.EstActif), "SecteurID", "Nom", model.SecteurID);
            ViewBag.StatutDossierID = new SelectList(db.StatutsDossierUsager.Where(s => s.EstActif), "StatutDossierID", "Libelle", model.StatutDossierID);
            ViewBag.AgentPayeurID = new SelectList(db.AgentsPayeurs.Where(a => a.EstActif), "AgentPayeurID", "Nom", model.AgentPayeurID);

            return View(model);
        }

        // GET: Usager/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers
                .Include(u => u.SecteursSRSAD)
                .Include(u => u.CLSC)
                .Include(u => u.StatutsDossierUsager)
                .Include(u => u.AgentsPayeurs)
                .Include(u => u.UsagerAdresses)
                .Include(u => u.UsagerParticularites.Select(p => p.ParticularitesUsagerRef))
                .Include(u => u.UsagerInfections.Select(i => i.TypesInfection))
                .Include(u => u.UsagerInfestations.Select(i => i.TypesInfestation))
                .FirstOrDefault(u => u.UsagerID == id);

            if (usager == null)
                return HttpNotFound();

            var model = new UsagerDetailsViewModel
            {
                UsagerID = usager.UsagerID,
                NoDossierSRSAD = usager.NoDossierSRSAD,
                NoDossierIPM = usager.NoDossierIPM,
                RAMQ = usager.RAMQ,
                Nom = usager.Nom,
                Prenom = usager.Prenom,
                DateNaissance = usager.DateNaissance,
                Sexe = usager.Sexe,
                Telephone = usager.Telephone,

                // Adresses
                Adresses = usager.UsagerAdresses
                    .OrderByDescending(a => a.EstCourante)
                    .ThenByDescending(a => a.DateCreation)
                    .Select(a => new UsagerAdresseViewModel
                    {
                        UsagerAdresseID = a.UsagerAdresseID,
                        TypeAdresse = a.TypeAdresse,
                        SourceAdresse = a.SourceAdresse,
                        EstCourante = a.EstCourante,
                        AdresseComplete = $"{a.Adresse1} {a.Adresse2}, {a.Ville}, {a.Province} {a.CodePostal}".Trim(),
                        Telephone = a.Telephone,
                        DateDebutValidite = a.DateDebutValidite,
                        DateFinValidite = a.DateFinValidite,
                        EClinibaseAdresseId = a.EClinibaseAdresseId
                    }).ToList(),

                // Informations administratives
                Secteur = usager.SecteursSRSAD?.Nom,
                CLSC = usager.CLSC?.NomCLSC,
                StatutDossier = usager.StatutsDossierUsager?.Libelle,
                CodeHydroQuebec = usager.CodeHydroQuebec,
                AgentPayeur = usager.AgentsPayeurs?.Nom,
                CentreDeCout = usager.CentreDeCout,
                BonCommande = usager.BonCommande,
                StatutTabagique = usager.StatutTabagique,
                EstActif = usager.EstActif,

                // Métadonnées
                DateCreation = usager.DateCreation,
                DateDerniereSyncEClinibase = usager.DateDerniereSyncEClinibase
            };

            return View(model);
        }

        // GET: Usager/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            ViewBag.CLSCID = new SelectList(db.CLSC.Where(c => c.EstActif), "CLSCID", "NomCLSC", usager.CLSCID);
            ViewBag.SecteurID = new SelectList(db.SecteursSRSAD.Where(s => s.EstActif), "SecteurID", "Nom", usager.SecteurID);
            ViewBag.StatutDossierID = new SelectList(db.StatutsDossierUsager.Where(s => s.EstActif), "StatutDossierID", "Libelle", usager.StatutDossierID);
            ViewBag.AgentPayeurID = new SelectList(db.AgentsPayeurs.Where(a => a.EstActif), "AgentPayeurID", "Nom", usager.AgentPayeurID);

            return View(usager);
        }

        // POST: Usager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usagers usager)
        {
            if (ModelState.IsValid)
            {
                var usagerOriginal = db.Usagers.AsNoTracking().FirstOrDefault(u => u.UsagerID == usager.UsagerID);

                usager.DateModification = DateTime.Now;
                usager.ModifieParUserId = User.Identity.GetUserId();

                db.Entry(usager).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "Usagers", usager.UsagerID.ToString(), usagerOriginal,
                    $"Modification du patient {usager.Nom} {usager.Prenom}");

                TempData["Success"] = "Patient modifié avec succès.";
                return RedirectToAction("Details", new { id = usager.UsagerID });
            }

            ViewBag.CLSCID = new SelectList(db.CLSC.Where(c => c.EstActif), "CLSCID", "NomCLSC", usager.CLSCID);
            ViewBag.SecteurID = new SelectList(db.SecteursSRSAD.Where(s => s.EstActif), "SecteurID", "Nom", usager.SecteurID);
            ViewBag.StatutDossierID = new SelectList(db.StatutsDossierUsager.Where(s => s.EstActif), "StatutDossierID", "Libelle", usager.StatutDossierID);
            ViewBag.AgentPayeurID = new SelectList(db.AgentsPayeurs.Where(a => a.EstActif), "AgentPayeurID", "Nom", usager.AgentPayeurID);

            return View(usager);
        }

        // POST: Usager/Desactiver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Desactiver(int id, string motif)
        {
            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            usager.EstActif = false;
            usager.DateModification = DateTime.Now;
            usager.ModifieParUserId = User.Identity.GetUserId();

            db.SaveChanges();

            JournaliserAction("DESACTIVER", "Usagers", id.ToString(), null,
                $"Désactivation du patient. Motif : {motif}");

            TempData["Success"] = "Patient désactivé avec succès.";
            return RedirectToAction("Details", new { id });
        }

        // POST: Usager/Reactiver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reactiver(int id)
        {
            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            usager.EstActif = true;
            usager.DateModification = DateTime.Now;
            usager.ModifieParUserId = User.Identity.GetUserId();

            db.SaveChanges();

            JournaliserAction("REACTIVER", "Usagers", id.ToString(), null, "Réactivation du patient");

            TempData["Success"] = "Patient réactivé avec succès.";
            return RedirectToAction("Details", new { id });
        }

        // GET: Usager/Adresses/5
        public ActionResult Adresses(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            var adresses = db.UsagerAdresses
                .Where(a => a.UsagerID == id)
                .OrderByDescending(a => a.EstCourante)
                .ThenByDescending(a => a.DateCreation)
                .ToList();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            return View(adresses);
        }

        // GET: Usager/CreateAdresse/5
        public ActionResult CreateAdresse(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            var adresse = new UsagerAdresses
            {
                UsagerID = usager.UsagerID,
                TypeAdresse = "PERMANENTE",
                SourceAdresse = "MANUEL",
                EstCourante = !db.UsagerAdresses.Any(a => a.UsagerID == id && a.EstCourante),
                DateDebutValidite = DateTime.Today,
                Pays = "Canada",
                Province = "Québec"
            };

            return View(adresse);
        }

        // POST: Usager/CreateAdresse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAdresse(UsagerAdresses adresse)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Si c'est l'adresse courante, désactiver les autres
                        if (adresse.EstCourante)
                        {
                            var autresAdresses = db.UsagerAdresses
                                .Where(a => a.UsagerID == adresse.UsagerID && a.EstCourante);
                            foreach (var a in autresAdresses)
                            {
                                a.EstCourante = false;
                            }
                        }

                        adresse.DateCreation = DateTime.Now;
                        adresse.CreeParUserId = User.Identity.GetUserId();

                        db.UsagerAdresses.Add(adresse);
                        db.SaveChanges();

                        // Mettre à jour l'adresse snapshot dans Usagers
                        if (adresse.EstCourante)
                        {
                            MettreAJourAdresseSnapshot(adresse.UsagerID);
                        }

                        JournaliserAction("CREATE", "UsagerAdresses", adresse.UsagerAdresseID.ToString(), null,
                            "Ajout d'adresse pour le patient");

                        transaction.Commit();

                        TempData["Success"] = "Adresse ajoutée avec succès.";
                        return RedirectToAction("Adresses", new { id = adresse.UsagerID });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Erreur lors de l'ajout de l'adresse : " + ex.Message);
                    }
                }
            }

            var usager = db.Usagers.Find(adresse.UsagerID);
            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = adresse.UsagerID;

            return View(adresse);
        }

        // POST: Usager/SetAdresseCourante/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetAdresseCourante(int id)
        {
            var adresse = db.UsagerAdresses.Find(id);
            if (adresse == null)
                return HttpNotFound();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Désactiver les autres adresses courantes
                    var autresAdresses = db.UsagerAdresses
                        .Where(a => a.UsagerID == adresse.UsagerID && a.EstCourante && a.UsagerAdresseID != id);
                    foreach (var a in autresAdresses)
                    {
                        a.EstCourante = false;
                    }

                    adresse.EstCourante = true;
                    db.SaveChanges();

                    // Mettre à jour l'adresse snapshot
                    MettreAJourAdresseSnapshot(adresse.UsagerID);

                    JournaliserAction("SET_COURANTE", "UsagerAdresses", id.ToString(), null,
                        "Définition de l'adresse courante");

                    transaction.Commit();

                    TempData["Success"] = "Adresse courante mise à jour.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Erreur : " + ex.Message;
                }
            }

            return RedirectToAction("Adresses", new { id = adresse.UsagerID });
        }

        // GET: Usager/Particularites/5
        public ActionResult Particularites(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            var particularites = db.UsagerParticularites
                .Include(p => p.ParticularitesUsagerRef)
                .Where(p => p.UsagerID == id)
                .OrderByDescending(p => p.DateDebut)
                .ToList();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            return View(particularites);
        }

        // GET: Usager/CreateParticularite/5
        public ActionResult CreateParticularite(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;
            ViewBag.ParticulariteID = new SelectList(
                db.ParticularitesUsagerRef.Where(p => p.EstActif),
                "ParticulariteID", "Libelle");

            var particularite = new UsagerParticularites
            {
                UsagerID = usager.UsagerID,
                DateDebut = DateTime.Today
            };

            return View(particularite);
        }

        // POST: Usager/CreateParticularite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateParticularite(UsagerParticularites particularite)
        {
            if (ModelState.IsValid)
            {
                particularite.DateCreation = DateTime.Now;
                particularite.CreeParUserId = User.Identity.GetUserId();

                db.UsagerParticularites.Add(particularite);
                db.SaveChanges();

                JournaliserAction("CREATE", "UsagerParticularites",
                    particularite.UsagerID + "-" + particularite.ParticulariteID, null,
                    "Ajout de particularité");

                TempData["Success"] = "Particularité ajoutée avec succès.";
                return RedirectToAction("Particularites", new { id = particularite.UsagerID });
            }

            var usager = db.Usagers.Find(particularite.UsagerID);
            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = particularite.UsagerID;
            ViewBag.ParticulariteID = new SelectList(
                db.ParticularitesUsagerRef.Where(p => p.EstActif),
                "ParticulariteID", "Libelle", particularite.ParticulariteID);

            return View(particularite);
        }

        // POST: Usager/TerminerParticularite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TerminerParticularite(int id, int particulariteId)
        {
            var particularite = db.UsagerParticularites
                .FirstOrDefault(p => p.UsagerID == id && p.ParticulariteID == particulariteId && !p.DateFin.HasValue);

            if (particularite == null)
                return HttpNotFound();

            particularite.DateFin = DateTime.Today;
            db.SaveChanges();

            JournaliserAction("TERMINER", "UsagerParticularites", id + "-" + particulariteId, null,
                "Fin de particularité");

            TempData["Success"] = "Particularité terminée.";
            return RedirectToAction("Particularites", new { id });
        }

        // GET: Usager/Infections/5
        public ActionResult Infections(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            var infections = db.UsagerInfections
                .Include(i => i.TypesInfection)
                .Where(i => i.UsagerID == id)
                .OrderByDescending(i => i.DateDebut)
                .ToList();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            return View(infections);
        }

        // GET: Usager/CreateInfection/5
        public ActionResult CreateInfection(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;
            ViewBag.TypeInfectionID = new SelectList(
                db.TypesInfection.Where(t => t.EstActif),
                "TypeInfectionID", "Libelle");

            var infection = new UsagerInfections
            {
                UsagerID = usager.UsagerID,
                DateDebut = DateTime.Today,
                Source = "MANUEL"
            };

            return View(infection);
        }

        // POST: Usager/CreateInfection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateInfection(UsagerInfections infection)
        {
            if (ModelState.IsValid)
            {
                infection.DateCreation = DateTime.Now;
                infection.CreeParUserId = User.Identity.GetUserId();

                db.UsagerInfections.Add(infection);
                db.SaveChanges();

                JournaliserAction("CREATE", "UsagerInfections", infection.UsagerInfectionID.ToString(), null,
                    "Ajout d'infection");

                TempData["Success"] = "Infection ajoutée avec succès.";
                return RedirectToAction("Infections", new { id = infection.UsagerID });
            }

            var usager = db.Usagers.Find(infection.UsagerID);
            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = infection.UsagerID;
            ViewBag.TypeInfectionID = new SelectList(
                db.TypesInfection.Where(t => t.EstActif),
                "TypeInfectionID", "Libelle", infection.TypeInfectionID);

            return View(infection);
        }

        // POST: Usager/TerminerInfection/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TerminerInfection(int id)
        {
            var infection = db.UsagerInfections.Find(id);
            if (infection == null)
                return HttpNotFound();

            infection.DateFin = DateTime.Today;
            db.SaveChanges();

            JournaliserAction("TERMINER", "UsagerInfections", id.ToString(), null, "Fin d'infection");

            TempData["Success"] = "Infection terminée.";
            return RedirectToAction("Infections", new { id = infection.UsagerID });
        }

        // GET: Usager/Infestations/5
        public ActionResult Infestations(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            var infestations = db.UsagerInfestations
                .Include(i => i.TypesInfestation)
                .Where(i => i.UsagerID == id)
                .OrderByDescending(i => i.DateDebut)
                .ToList();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            return View(infestations);
        }

        // GET: Usager/CreateInfestation/5
        public ActionResult CreateInfestation(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;
            ViewBag.TypeInfestationID = new SelectList(
                db.TypesInfestation.Where(t => t.EstActif),
                "TypeInfestationID", "Libelle");

            var infestation = new UsagerInfestations
            {
                UsagerID = usager.UsagerID,
                DateDebut = DateTime.Today,
                Source = "MANUEL"
            };

            return View(infestation);
        }

        // POST: Usager/CreateInfestation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateInfestation(UsagerInfestations infestation)
        {
            if (ModelState.IsValid)
            {
                infestation.DateCreation = DateTime.Now;
                infestation.CreeParUserId = User.Identity.GetUserId();

                db.UsagerInfestations.Add(infestation);
                db.SaveChanges();

                JournaliserAction("CREATE", "UsagerInfestations", infestation.UsagerInfestationID.ToString(), null,
                    "Ajout d'infestation");

                TempData["Success"] = "Infestation ajoutée avec succès.";
                return RedirectToAction("Infestations", new { id = infestation.UsagerID });
            }

            var usager = db.Usagers.Find(infestation.UsagerID);
            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = infestation.UsagerID;
            ViewBag.TypeInfestationID = new SelectList(
                db.TypesInfestation.Where(t => t.EstActif),
                "TypeInfestationID", "Libelle", infestation.TypeInfestationID);

            return View(infestation);
        }

        // POST: Usager/TerminerInfestation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TerminerInfestation(int id)
        {
            var infestation = db.UsagerInfestations.Find(id);
            if (infestation == null)
                return HttpNotFound();

            infestation.DateFin = DateTime.Today;
            db.SaveChanges();

            JournaliserAction("TERMINER", "UsagerInfestations", id.ToString(), null, "Fin d'infestation");

            TempData["Success"] = "Infestation terminée.";
            return RedirectToAction("Infestations", new { id = infestation.UsagerID });
        }

        // GET: Usager/Historique/5
        public ActionResult Historique(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers.Find(id);
            if (usager == null)
                return HttpNotFound();

            var historique = new List<HistoriqueActionViewModel>();

            // Journal d'audit
            historique.AddRange(db.JournalAudit
                .Where(j => j.TableConcernee == "Usagers" && j.ClePrimaire == id.ToString())
                .OrderByDescending(j => j.DateHeure)
                .Select(j => new HistoriqueActionViewModel
                {
                    Date = j.DateHeure,
                    Type = "Audit",
                    Action = j.Action,
                    Description = j.Description,
                    Utilisateur = j.UtilisateurId
                }));

            ViewBag.UsagerNom = usager.Nom + " " + usager.Prenom;
            ViewBag.UsagerID = usager.UsagerID;

            return View(historique);
        }

        // Méthodes privées
        private async Task<List<ResultatRechercheEClinibaseViewModel>> RechercherDansEClinibase(RechercheEClinibaseViewModel criteres)
        {
            // Simuler un appel API à E-Clinibase
            var resultats = new List<ResultatRechercheEClinibaseViewModel>();

            // Simuler un délai d'appel API
            await Task.Delay(500);

            // Dans la réalité, vous feriez un appel HTTP à un service externe
            // Exemple : 
            // using (var client = new HttpClient())
            // {
            //     client.BaseAddress = new Uri("https://api.e-clinibase.ca/");
            //     var response = await client.GetAsync($"patients?nom={criteres.Nom}&prenom={criteres.Prenom}&ramq={criteres.RAMQ}");
            //     if (response.IsSuccessStatusCode)
            //     {
            //         resultats = await response.Content.ReadAsAsync<List<ResultatRechercheEClinibaseViewModel>>();
            //     }
            // }

            // Données simulées pour la démonstration
            // Ces données seraient normalement retournées par l'API E-Clinibase
            if (!string.IsNullOrEmpty(criteres.Nom) || !string.IsNullOrEmpty(criteres.RAMQ) || !string.IsNullOrEmpty(criteres.NoIPM))
            {
                // Simuler une correspondance exacte par RAMQ ou IPM
                if (criteres.RAMQ == "DUPJ75051501" || criteres.NoIPM == "IPM123456")
                {
                    resultats.Add(new ResultatRechercheEClinibaseViewModel
                    {
                        NoIPM = "IPM123456",
                        Nom = "Dupont",
                        Prenom = "Jean",
                        DateNaissance = new DateTime(1975, 5, 15),
                        RAMQ = "DUPJ75051501",
                        Sexe = "M",
                        AdresseComplete = "456 Rue Sherbrooke, Montréal, QC H2A 1B2",
                        Telephone = "514-555-1234"
                    });
                }

                if (criteres.RAMQ == "TREM82082302" || criteres.NoIPM == "IPM789012")
                {
                    resultats.Add(new ResultatRechercheEClinibaseViewModel
                    {
                        NoIPM = "IPM789012",
                        Nom = "Tremblay",
                        Prenom = "Marie",
                        DateNaissance = new DateTime(1982, 8, 23),
                        RAMQ = "TREM82082302",
                        Sexe = "F",
                        AdresseComplete = "123 Rue Principale, Laval, QC H7A 1C3",
                        Telephone = "450-555-5678"
                    });
                }

                // Simuler une recherche par nom
                if (!string.IsNullOrEmpty(criteres.Nom) && criteres.Nom.ToLower() == "gagnon")
                {
                    resultats.Add(new ResultatRechercheEClinibaseViewModel
                    {
                        NoIPM = "IPM345678",
                        Nom = "Gagnon",
                        Prenom = "Pierre",
                        DateNaissance = new DateTime(1968, 3, 10),
                        RAMQ = "GAGP68031003",
                        Sexe = "M",
                        AdresseComplete = "789 Rue des Érables, Longueuil, QC J4A 1B5",
                        Telephone = "450-555-9012"
                    });
                }
            }

            return resultats;
        }
        private DonneesEClinibaseVewmodel RecupererDonneesCompletesEClinibase(string ipm)
        {
            if (ipm == "IPM123456")
            {
                return new DonneesEClinibaseVewmodel
                {
                    NoIPM = "IPM123456",
                    RAMQ = "DUPJ75051501",
                    Nom = "Dupont",
                    Prenom = "Jean",
                    DateNaissance = new DateTime(1975, 5, 15),
                    Sexe = "M",
                    Telephone = "514-555-1234",
                    Adresses = new List<AdresseEClinibaseViewModel>
                    {
                        new AdresseEClinibaseViewModel
                        {
                            TypeAdresse = "PERMANENTE",
                            Adresse1 = "456 Rue Sherbrooke",
                            Adresse2 = null,
                            Ville = "Montréal",
                            Province = "Québec",
                            CodePostal = "H2A 1B2",
                            Pays = "Canada",
                            Telephone = "514-555-1234",
                            DateDebutValidite = new DateTime(2020, 1, 1),
                            EClinibaseAdresseId = "ADDR001"
                        },
                        new AdresseEClinibaseViewModel
                        {
                            TypeAdresse = "TEMPORAIRE",
                            Adresse1 = "789 Rue Temporaire",
                            Adresse2 = "App 3",
                            Ville = "Montréal",
                            Province = "Québec",
                            CodePostal = "H3B 2Y5",
                            Pays = "Canada",
                            Telephone = "514-555-4321",
                            DateDebutValidite = new DateTime(2024, 1, 1),
                            DateFinValidite = new DateTime(2024, 6, 30),
                            EClinibaseAdresseId = "ADDR003"
                        },
                        new AdresseEClinibaseViewModel
                        {
                            TypeAdresse = "ANTERIEURE",
                            Adresse1 = "111 Rue Ancienne",
                            Adresse2 = null,
                            Ville = "Montréal",
                            Province = "Québec",
                            CodePostal = "H4C 1X2",
                            Pays = "Canada",
                            Telephone = "514-555-9999",
                            DateDebutValidite = new DateTime(2015, 3, 1),
                            DateFinValidite = new DateTime(2019, 12, 31),
                            EClinibaseAdresseId = "ADDR004"
                        }
                    }
                };
            }
            else
            {
                return new DonneesEClinibaseVewmodel
                {
                    NoIPM = "IPM789012",
                    RAMQ = "TREM82082302",
                    Nom = "Tremblay",
                    Prenom = "Marie",
                    DateNaissance = new DateTime(1982, 8, 23),
                    Sexe = "F",
                    Telephone = "450-555-5678",
                    Adresses = new List<AdresseEClinibaseViewModel>
                    {
                        new AdresseEClinibaseViewModel
                        {
                            TypeAdresse = "PERMANENTE",
                            Adresse1 = "123 Rue Principale",
                            Adresse2 = null,
                            Ville = "Laval",
                            Province = "Québec",
                            CodePostal = "H7A 1C3",
                            Pays = "Canada",
                            Telephone = "450-555-5678",
                            DateDebutValidite = new DateTime(2018, 6, 1),
                            EClinibaseAdresseId = "ADDR002"
                        }
                    }
                };
            }
        }

        private void MettreAJourAdresseSnapshot(int usagerId)
        {
            var adresseCourante = db.UsagerAdresses
                .FirstOrDefault(a => a.UsagerID == usagerId && a.EstCourante);

            if (adresseCourante != null)
            {
                var usager = db.Usagers.Find(usagerId);
                usager.Adresse = adresseCourante.Adresse1;
                usager.Ville = adresseCourante.Ville;
                usager.Province = adresseCourante.Province;
                usager.CodePostal = adresseCourante.CodePostal;
                usager.Pays = adresseCourante.Pays;
                usager.Telephone = adresseCourante.Telephone;

                db.SaveChanges();
            }
        }

        private string GenererNumeroDossierSRSAD()
        {
            int annee = DateTime.Now.Year;
            int periode = (DateTime.Now.Month - 1) / 3 + 1;

            string prefix = $"S-{annee}-{periode}";

            int compteur = db.Usagers
                .Count(u => u.NoDossierSRSAD.StartsWith(prefix)) + 1;

            return $"{prefix}-{compteur:D4}";
        }

        private void JournaliserAction(string action, string table, string clePrimaire, object ancien, string description)
        {
            var audit = new JournalAudit
            {
                DateHeure = DateTime.Now,
                Action = action,
                TableConcernee = table,
                ClePrimaire = clePrimaire,
                Description = description,
                AnciennesValeurs = ancien != null ? Newtonsoft.Json.JsonConvert.SerializeObject(ancien) : null,
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