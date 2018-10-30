using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using Ctaima.RepsolOKN.Document.Upload.Function.Model;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ctaima.RepsolOKN.Document.Upload.Function.Extensions
{

    public static class HttpRequestExtensions
    {
        public static async Task<HttpResponseBody<T>> GetFormDataModelAsync<T>(this HttpRequestMessage req) where T : new()
        {

            var provider = new MultipartMemoryStreamProvider();
            await req.Content.ReadAsMultipartAsync(provider);

            T model = new T();
            var properties = model.GetType().GetProperties();
            foreach (var p in properties)
            {   
                var prop = provider.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.Name.Contains(p.Name)); 
                if (p.PropertyType == typeof(HttpContent))
                    p.SetValue(model, prop);
                else
                    p.SetValue(model, prop != null ? await prop.ReadAsStringAsync() : null);
            }

            var body = new HttpResponseBody<T> {Value = model};

            var results = new List<ValidationResult>();
            body.IsValid = Validator.TryValidateObject(body.Value, new ValidationContext(body.Value, null, null), results, true);
            body.ValidationResults = results;
            return body;
        }
    }

    public class DateFormatValidation : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var format = "dd/MM/yyyy";
            bool parsed = DateTime.TryParseExact((string)value, format,
                System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
            if (!parsed)
                return false;
            return true;
        }
    }
}
