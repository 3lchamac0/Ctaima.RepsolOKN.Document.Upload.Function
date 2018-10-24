using System;
using System.Collections.Generic;
using System.Text;

namespace Ctaima.RepsolOKN.Document.Upload.Function.Model
{
    public class ResultValidation
    {
        public int Id { get; set; }
        public string IdRequeriment { get; set; }
        public bool LastStatus { get; set; }   
        public string ExpeditionDate { get; set; }
        public string ExpirationDate { get; set; }
    }
}
