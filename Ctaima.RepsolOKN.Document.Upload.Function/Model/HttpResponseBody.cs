using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ctaima.RepsolOKN.Document.Upload.Function.Model
{
    public class HttpResponseBody<T>
    {
        public bool IsValid { get; set; }
        public T Value { get; set; }

        public IEnumerable<ValidationResult> ValidationResults { get; set; }
    }
}
