using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml.Schema;

namespace MvcApplication1.Models
{
   
    public class FileViewModel
    {
        public HttpPostedFileBase File { get; set; }
    }
}