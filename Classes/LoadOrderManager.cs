using StarfieldLoadOrderManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarfieldLoadOrderManager.Classes
{
    class LoadOrderManager
    {
        private static LoadOrderManager _instance;

        public static LoadOrderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoadOrderManager();
                }
                return _instance;
            }
        }

        public string StarfieldFolder => ManagerSettings.Default.StarfieldFolder;

        public bool StarfieldFolderExists
        {
            get
            {
                return !string.IsNullOrEmpty(StarfieldFolder) && System.IO.Directory.Exists(StarfieldFolder);
            }
        }
    }
}
