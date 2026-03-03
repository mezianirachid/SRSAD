using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SRSAD.Models;
using Microsoft.AspNet.Identity;
using System.Net;

namespace SRSAD.ViewModels
{
    public class DashboardController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Dashboard
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var intervenant = db.Intervenants.FirstOrDefault(i => i.UserIdAspNet == userId);

            // Définir les dates en dehors des requêtes LINQ
            DateTime aujourdhui = DateTime.Today;
            DateTime ilYASeptJours = aujourdhui.AddDays(-7);
            DateTime dansDeuxJours = aujourdhui.AddDays(2);
            DateTime dansSeptJours = aujourdhui.AddDays(7);

            var model = new DashboardViewModel
            {
                // Statistiques globales
                TotalPatients = db.Usagers.Count(u => u.EstActif),
                PatientsActifs = db.Usagers.Count(u => u.EstActif && u.StatutsDossierUsager.Code == "INSCRIT"),
                PatientsHospitalises = db.Usagers.Count(u => u.EstActif && u.StatutsDossierUsager.Code == "HOSP"),
                PatientsRadies = db.Usagers.Count(u => !u.EstActif),

                // Visites - Corrigé : utilisation des variables définies en dehors
                VisitesAujourdhui = db.Visites.Count(v => DbFunctions.TruncateTime(v.DateVisite) == aujourdhui),
                VisitesCetteSemaine = db.Visites.Count(v =>
                    v.DateVisite >= ilYASeptJours &&
                    v.DateVisite <= aujourdhui),
                VisitesEnRetard = db.Assignations.Count(a =>
                    a.ProchaineVisiteDate < aujourdhui &&
                    (!a.DateFin.HasValue || a.DateFin >= aujourdhui)),

                // Équipements
                EquipementsTotal = db.Equipements.Count(e => e.EstActif),
                EquipementsDisponibles = db.HistoriqueStatutsEquipement
                    .Where(h => h.StatutsEquipementRef.Code == "DISPONIBLE" &&
                               h.DateChangement == db.HistoriqueStatutsEquipement
                                   .Where(h2 => h2.EquipementID == h.EquipementID)
                                   .Max(h2 => h2.DateChangement))
                    .Count(),
                EquipementsEnPret = db.PretsEquipement.Count(p => !p.DateFin.HasValue || p.DateFin >= aujourdhui),
                EquipementsBrises = db.HistoriqueStatutsEquipement
                    .Where(h => h.StatutsEquipementRef.Code == "BRISE" &&
                               h.DateChangement == db.HistoriqueStatutsEquipement
                                   .Where(h2 => h2.EquipementID == h.EquipementID)
                                   .Max(h2 => h2.DateChangement))
                    .Count(),
                EquipementsMaintenance = db.HistoriqueStatutsEquipement
                    .Where(h => h.StatutsEquipementRef.Code == "REPARATION" &&
                               h.DateChangement == db.HistoriqueStatutsEquipement
                                   .Where(h2 => h2.EquipementID == h.EquipementID)
                                   .Max(h2 => h2.DateChangement))
                    .Count(),

                // Cylindres
                CommandesCylindresEnCours = db.CommandesCylindres.Count(c => c.Statut == "EN_COURS"),
                FacturesEnAttente = db.ImportFacturationCylindres.Count(f => f.Statut == "PRELIM"),

                // Alertes
                PatientsAvecAlertes = db.UsagerParticularites
                    .Include(p => p.Usagers)
                    .Include(p => p.ParticularitesUsagerRef)
                    .Where(p => !p.DateFin.HasValue && p.ParticularitesUsagerRef.NiveauAlerte > 0)
                    .Select(p => p.Usagers)
                    .Distinct()
                    .Count(),

                InfectionsActives = db.UsagerInfections.Count(i => !i.DateFin.HasValue || i.DateFin >= aujourdhui),
                InfestationsActives = db.UsagerInfestations.Count(i => !i.DateFin.HasValue || i.DateFin >= aujourdhui),

                // Prochaines visites - Corrigé
                ProchainesVisites = db.Assignations
                    .Include(a => a.Usagers)
                    .Include(a => a.Intervenants)
                    .Include(a => a.TypesIntervenant)
                    .Where(a => a.ProchaineVisiteDate.HasValue &&
                               a.ProchaineVisiteDate >= aujourdhui &&
                               a.ProchaineVisiteDate <= dansSeptJours &&
                               (!a.DateFin.HasValue || a.DateFin >= aujourdhui))
                    .OrderBy(a => a.ProchaineVisiteDate)
                    .Take(10)
                    .ToList() // Matérialiser la requête avant de créer les ViewModels
                    .Select(a => new ProchaineVisiteViewModel
                    {
                        AssignationID = a.AssignationID,
                        PatientID = a.Usagers.UsagerID,
                        PatientNom = a.Usagers.Nom + " " + a.Usagers.Prenom,
                        IntervenantID = a.IntervenantID,
                        IntervenantNom = a.Intervenants.Nom + " " + a.Intervenants.Prenom,
                        ProchaineVisite = a.ProchaineVisiteDate.Value,
                        TypeIntervenant = a.TypesIntervenant.Libelle,
                        EstUrgent = a.ProchaineVisiteDate <= dansDeuxJours
                    }).ToList(),

                // Dernières visites
                DernieresVisites = db.Visites
                    .Include(v => v.Usagers)
                    .Include(v => v.Intervenants)
                    .Include(v => v.TypesIntervenant)
                    .OrderByDescending(v => v.DateVisite)
                    .Take(10)
                    .ToList()
                    .Select(v => new VisiteRecenteViewModel
                    {
                        VisiteID = v.VisiteID,
                        PatientID = v.Usagers.UsagerID,
                        PatientNom = v.Usagers.Nom + " " + v.Usagers.Prenom,
                        IntervenantNom = v.Intervenants.Nom + " " + v.Intervenants.Prenom,
                        DateVisite = v.DateVisite,
                        TypeIntervenant = v.TypesIntervenant.Libelle,
                        DureeMinutes = v.DureeMinutes ?? 0
                    }).ToList(),

                // Équipements récents
                EquipementsRecents = db.Equipements
                    .Include(e => e.TypesEquipement)
                    .OrderByDescending(e => e.DateCreation)
                    .Take(5)
                    .ToList()
                    .Select(e => new EquipementRecentViewModel
                    {
                        EquipementID = e.EquipementID,
                        NoInventaire = e.NoInventaireSRSAD,
                        TypeEquipement = e.TypesEquipement.Libelle,
                        Modele = e.Modele,
                        DateCreation = e.DateCreation
                    }).ToList(),

                // Nouveaux patients
                NouveauxPatients = db.Usagers
                    .OrderByDescending(u => u.DateCreation)
                    .Take(5)
                    .ToList()
                    .Select(u => new NouveauPatientViewModel
                    {
                        UsagerID = u.UsagerID,
                        NomComplet = u.Nom + " " + u.Prenom,
                        NoDossierSRSAD = u.NoDossierSRSAD,
                        DateCreation = u.DateCreation
                    }).ToList(),

                // Statistiques par statut
                StatsPatients = db.Usagers
                    .Where(u => u.EstActif)
                    .Include(u => u.StatutsDossierUsager)
                    .ToList()
                    .GroupBy(u => u.StatutsDossierUsager != null ? u.StatutsDossierUsager.Libelle : "Inconnu")
                    .Select(g => new StatItemViewModel
                    {
                        Libelle = g.Key,
                        Valeur = g.Count()
                    }).ToList(),

                StatsIntervenants = db.Intervenants
                    .Where(i => i.EstActif)
                    .Include(i => i.TypesIntervenant)
                    .ToList()
                    .GroupBy(i => i.TypesIntervenant != null ? i.TypesIntervenant.Libelle : "Inconnu")
                    .Select(g => new StatItemViewModel
                    {
                        Libelle = g.Key,
                        Valeur = g.Count()
                    }).ToList(),

                // Données de l'intervenant connecté
                IntervenantConnecte = intervenant != null ? new IntervenantDashboardViewModel
                {
                    IntervenantID = intervenant.IntervenantID,
                    NomComplet = intervenant.Nom + " " + intervenant.Prenom,
                    TypeIntervenant = intervenant.TypesIntervenant != null ? intervenant.TypesIntervenant.Libelle : "Inconnu",

                    MesPatients = db.Assignations
                        .Include(a => a.Usagers)
                        .Where(a => a.IntervenantID == intervenant.IntervenantID &&
                                   (!a.DateFin.HasValue || a.DateFin >= aujourdhui))
                        .Select(a => a.Usagers)
                        .Distinct()
                        .Count(),

                    MesVisitesAujourdhui = db.Visites
                        .Count(v => v.IntervenantID == intervenant.IntervenantID &&
                                   DbFunctions.TruncateTime(v.DateVisite) == aujourdhui),

                    MesProchainesVisites = db.Assignations
                        .Include(a => a.Usagers)
                        .Where(a => a.IntervenantID == intervenant.IntervenantID &&
                                   a.ProchaineVisiteDate.HasValue &&
                                   a.ProchaineVisiteDate >= aujourdhui &&
                                   (!a.DateFin.HasValue || a.DateFin >= aujourdhui))
                        .OrderBy(a => a.ProchaineVisiteDate)
                        .Take(5)
                        .ToList()
                        .Select(a => new ProchaineVisiteViewModel
                        {
                            AssignationID = a.AssignationID,
                            PatientID = a.Usagers.UsagerID,
                            PatientNom = a.Usagers.Nom + " " + a.Usagers.Prenom,
                            ProchaineVisite = a.ProchaineVisiteDate.Value,
                            EstUrgent = a.ProchaineVisiteDate <= dansDeuxJours
                        }).ToList()
                } : null
            };

            // Calculer les pourcentages
            if (model.EquipementsTotal > 0)
            {
                model.PourcentageDisponibles = (int)((double)model.EquipementsDisponibles / model.EquipementsTotal * 100);
                model.PourcentageEnPret = (int)((double)model.EquipementsEnPret / model.EquipementsTotal * 100);
            }

            // Récupérer les notifications non lues
            if (userId != null)
            {
                model.NotificationsNonLues = db.Notifications.Count(n => n.DestUserId == userId && !n.EstLue);
            }

            return View(model);
        }

        // GET: Dashboard/PatientDetails/5 (vue rapide depuis le dashboard)
        public ActionResult PatientDetails(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usager = db.Usagers
                .Include(u => u.SecteursSRSAD)
                .Include(u => u.StatutsDossierUsager)
                .Include(u => u.UsagerAdresses.Where(a => a.EstCourante))
                .Include(u => u.Assignations.Select(a => a.Intervenants))
                .Include(u => u.Assignations.Select(a => a.TypesIntervenant))
                .Include(u => u.PretsEquipement.Select(p => p.Equipements))
                .FirstOrDefault(u => u.UsagerID == id);

            if (usager == null)
                return HttpNotFound();

            DateTime aujourdhui = DateTime.Today;

            var model = new PatientQuickViewModel
            {
                UsagerID = usager.UsagerID,
                NomComplet = usager.Nom + " " + usager.Prenom,
                NoDossierSRSAD = usager.NoDossierSRSAD,
                NoDossierIPM = usager.NoDossierIPM,
                DateNaissance = usager.DateNaissance,
                Secteur = usager.SecteursSRSAD?.Nom,
                StatutDossier = usager.StatutsDossierUsager?.Libelle,
                AdresseCourante = usager.UsagerAdresses.FirstOrDefault() != null ?
                    usager.UsagerAdresses.FirstOrDefault().Adresse1 + ", " +
                    usager.UsagerAdresses.FirstOrDefault().Ville : "Aucune",
                Telephone = usager.Telephone,

                AssignationsActives = usager.Assignations
                    .Where(a => !a.DateFin.HasValue || a.DateFin >= aujourdhui)
                    .Select(a => a.Intervenants.Nom + " " + a.Intervenants.Prenom + " (" + a.TypesIntervenant.Libelle + ")")
                    .ToList(),

                EquipementsEnPret = usager.PretsEquipement
                    .Where(p => !p.DateFin.HasValue || p.DateFin >= aujourdhui)
                    .Select(p => p.Equipements.NoInventaireSRSAD + " - " + p.Equipements.Modele)
                    .ToList(),

                DerniereVisite = db.Visites
                    .Where(v => v.UsagerID == id)
                    .OrderByDescending(v => v.DateVisite)
                    .Select(v => v.DateVisite)
                    .FirstOrDefault(),

                ProchaineVisite = db.Assignations
                    .Where(a => a.UsagerID == id && a.ProchaineVisiteDate.HasValue)
                    .OrderBy(a => a.ProchaineVisiteDate)
                    .Select(a => a.ProchaineVisiteDate)
                    .FirstOrDefault()
            };

            return PartialView("_PatientQuickView", model);
        }

        // GET: Dashboard/RafraichirStats (pour actualisation AJAX)
        public JsonResult RafraichirStats()
        {
            DateTime aujourdhui = DateTime.Today;

            var stats = new
            {
                PatientsActifs = db.Usagers.Count(u => u.EstActif && u.StatutsDossierUsager.Code == "INSCRIT"),
                PatientsHospitalises = db.Usagers.Count(u => u.EstActif && u.StatutsDossierUsager.Code == "HOSP"),
                VisitesAujourdhui = db.Visites.Count(v => DbFunctions.TruncateTime(v.DateVisite) == aujourdhui),
                EquipementsDisponibles = db.HistoriqueStatutsEquipement
                    .Where(h => h.StatutsEquipementRef.Code == "DISPONIBLE" &&
                               h.DateChangement == db.HistoriqueStatutsEquipement
                                   .Where(h2 => h2.EquipementID == h.EquipementID)
                                   .Max(h2 => h2.DateChangement))
                    .Count(),
                AlertesActives = db.UsagerParticularites
                    .Count(p => !p.DateFin.HasValue && p.ParticularitesUsagerRef.NiveauAlerte > 0)
            };

            return Json(stats, JsonRequestBehavior.AllowGet);
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

    // ViewModels (à mettre dans un fichier séparé ou à la fin du contrôleur)
    public class DashboardViewModel
    {
        // Statistiques patients
        public int TotalPatients { get; set; }
        public int PatientsActifs { get; set; }
        public int PatientsHospitalises { get; set; }
        public int PatientsRadies { get; set; }

        // Statistiques visites
        public int VisitesAujourdhui { get; set; }
        public int VisitesCetteSemaine { get; set; }
        public int VisitesEnRetard { get; set; }

        // Statistiques équipements
        public int EquipementsTotal { get; set; }
        public int EquipementsDisponibles { get; set; }
        public int EquipementsEnPret { get; set; }
        public int EquipementsBrises { get; set; }
        public int EquipementsMaintenance { get; set; }
        public int PourcentageDisponibles { get; set; }
        public int PourcentageEnPret { get; set; }

        // Statistiques cylindres
        public int CommandesCylindresEnCours { get; set; }
        public int FacturesEnAttente { get; set; }

        // Alertes
        public int PatientsAvecAlertes { get; set; }
        public int InfectionsActives { get; set; }
        public int InfestationsActives { get; set; }

        // Listes
        public List<ProchaineVisiteViewModel> ProchainesVisites { get; set; }
        public List<VisiteRecenteViewModel> DernieresVisites { get; set; }
        public List<EquipementRecentViewModel> EquipementsRecents { get; set; }
        public List<NouveauPatientViewModel> NouveauxPatients { get; set; }
        public List<StatItemViewModel> StatsPatients { get; set; }
        public List<StatItemViewModel> StatsIntervenants { get; set; }

        // Intervenant connecté
        public IntervenantDashboardViewModel IntervenantConnecte { get; set; }

        // Notifications
        public int NotificationsNonLues { get; set; }
    }

    public class ProchaineVisiteViewModel
    {
        public int AssignationID { get; set; }
        public int PatientID { get; set; }
        public string PatientNom { get; set; }
        public int? IntervenantID { get; set; }
        public string IntervenantNom { get; set; }
        public DateTime ProchaineVisite { get; set; }
        public string TypeIntervenant { get; set; }
        public bool EstUrgent { get; set; }
        public string HeureRelative
        {
            get
            {
                var jours = (ProchaineVisite.Date - DateTime.Today).Days;
                if (jours == 0) return "Aujourd'hui";
                if (jours == 1) return "Demain";
                if (jours < 0) return "En retard";
                return $"Dans {jours} jours";
            }
        }
    }

    public class VisiteRecenteViewModel
    {
        public int VisiteID { get; set; }
        public int PatientID { get; set; }
        public string PatientNom { get; set; }
        public string IntervenantNom { get; set; }
        public DateTime DateVisite { get; set; }
        public string TypeIntervenant { get; set; }
        public int DureeMinutes { get; set; }
        public string HeureRelative
        {
            get
            {
                var jours = (DateTime.Today - DateVisite.Date).Days;
                if (jours == 0) return "Aujourd'hui";
                if (jours == 1) return "Hier";
                return $"Il y a {jours} jours";
            }
        }
    }

    public class EquipementRecentViewModel
    {
        public int EquipementID { get; set; }
        public string NoInventaire { get; set; }
        public string TypeEquipement { get; set; }
        public string Modele { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class NouveauPatientViewModel
    {
        public int UsagerID { get; set; }
        public string NomComplet { get; set; }
        public string NoDossierSRSAD { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class StatItemViewModel
    {
        public string Libelle { get; set; }
        public int Valeur { get; set; }
    }

    public class IntervenantDashboardViewModel
    {
        public int IntervenantID { get; set; }
        public string NomComplet { get; set; }
        public string TypeIntervenant { get; set; }
        public int MesPatients { get; set; }
        public int MesVisitesAujourdhui { get; set; }
        public List<ProchaineVisiteViewModel> MesProchainesVisites { get; set; }
    }

    public class PatientQuickViewModel
    {
        public int UsagerID { get; set; }
        public string NomComplet { get; set; }
        public string NoDossierSRSAD { get; set; }
        public string NoDossierIPM { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string Secteur { get; set; }
        public string StatutDossier { get; set; }
        public string AdresseCourante { get; set; }
        public string Telephone { get; set; }
        public List<string> AssignationsActives { get; set; }
        public List<string> EquipementsEnPret { get; set; }
        public DateTime? DerniereVisite { get; set; }
        public DateTime? ProchaineVisite { get; set; }
    }
}