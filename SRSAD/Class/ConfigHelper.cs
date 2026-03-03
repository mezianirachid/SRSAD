//using gestionStagesCemtl.Models.Enums;
//using System.Configuration;

//namespace gestionStagesCemtl
//{
//    public class ConfigHelper
//    {
//        #region Propietes

//        public EnvironnementEnum Environnement { get; private set; }
//        public int DocumentMaxSize { get; private set; }

//        #endregion

//        #region Constructeur

//        public ConfigHelper()
//        {
//            switch (ConfigurationManager.AppSettings["environnement"])
//            {
//                case "dev": Environnement = EnvironnementEnum.Dev; break;
//                case "preprod": Environnement = EnvironnementEnum.Preprod; break;
//                case "prod": Environnement = EnvironnementEnum.Prod; break;
//                default: Environnement = EnvironnementEnum.Null; break;
//            }

//            DocumentMaxSize = int.Parse(ConfigurationManager.AppSettings["documentMaxSizeInKB"]);
//        }

//        #endregion
//    }
//}