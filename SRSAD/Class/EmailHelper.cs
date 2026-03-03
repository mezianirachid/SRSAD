using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace gestionStagesCemtl
{
    public class EmailHelper
    {
        #region Propietes

        public string SignatureCourrielCIUSSS
        {
            get
            {
                var signature = "<br /><br />L'équipe du service des stages et de la relève étudiante <br />";
                signature += "Direction des ressources humaines, des communications et des affaires juridiques<br />";
                signature += "Centre intégré universitaire de santé et de services sociaux de l’Est-de-l’Île-de-Montréal<br />";
                signature += "<a href='http://ciusss-estmtl.gouv.qc.ca/'>http://ciusss-estmtl.gouv.qc.ca/</a>";

                return signature;
            }
        }

        #endregion

        #region Methodes

        public void EnvoyerCourriel(string destinataire, string objet, string contenu, List<Attachment> attachments = null, string  from = null)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();

                mailMessage.To.Add(new MailAddress(destinataire));
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = contenu;
                mailMessage.Subject = objet;
                

                if (from != null)
                {
                    mailMessage.From = new MailAddress(from);
                }

                if (attachments != null)
                {
                    foreach (var item in attachments)
                    {
                        mailMessage.Attachments.Add(item);
                    }
                }

                using (var client = new SmtpClient())
                {
                    client.Send(mailMessage);
                }
            }
            catch (Exception )
            {
               
            }
        }

        #endregion
    }
}