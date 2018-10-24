using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using Ctaima.RepsolOKN.Document.Upload.Function.Extensions;

namespace Ctaima.RepsolOKN.Document.Upload.Function.Model
{
    public class PostData
    {
        [Required]
        public string Cif { get; set; }
        [Required]
        public string Dni { get; set; }
        [Required]
        public string Center { get; set; }
        [Required]
        public string DocumentId { get; set; }
        [Required]
        [DateFormatValidation]
        public string ExpeditionDate { get; set; }
        [Required]
        [DateFormatValidation]
        public string ExpirationDate { get; set; }
        public HttpContent File { get; set; }
    }
}
