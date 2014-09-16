
using CoursesTests.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoursesTests.Controllers
{
    public class BaseController : Controller
    {
        protected HashSet<string> allowedFileExtensions =
         new HashSet<string> { "jpg", "jpeg", "png", "gif","bmp" };
       

      


     
        protected override void OnActionExecuting(
        ActionExecutingContext filterContext)
        {
            
            
            base.OnActionExecuting(filterContext);
        }

        protected void CreateDirectoryIfDontExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        protected void DeleteFileIfExist(string selectedFileFullPathPlusName)
        {
            if (System.IO.File.Exists(selectedFileFullPathPlusName))
            {
                System.IO.File.Delete(selectedFileFullPathPlusName);
            }

        }

      


         

       


        

    }
}
