using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Configuration;

namespace iLgs.Services
{
    public class DirectoryService : IDirectoryService
    {         
        public DirectoryService()
        {

        }
        public string GetItemImageDirectory()
        {
            return ConfigurationManager.AppSettings["ImageFolder"].ToString() + "Items/";
        }
    }
}