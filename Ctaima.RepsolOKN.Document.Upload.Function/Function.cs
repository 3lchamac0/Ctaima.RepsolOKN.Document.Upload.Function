using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Ctaima.RepsolOKN.Document.Upload.Function.Extensions;
using Ctaima.RepsolOKN.Document.Upload.Function.Model;
using Newtonsoft.Json;
using Ctaima.RepsolOKN.Document.Upload.Function.Infrastructure;

namespace Ctaima.RepsolOKN.Document.Upload.Function
{
    public static class Function
    {
        /// <summary>
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DocumentUpload")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {  

                var body = await req.GetFormDataModelAsync<PostData>();  
                if (!body.IsValid)
                    return new BadRequestObjectResult($"Model is invalid: {string.Join(", ", body.ValidationResults.Select(s => s.ErrorMessage).ToArray())}");
                var data = body.Value; 

                var clientOlympus = new HttpApiManager()
                {
                    UrlAuthorize = Environment.GetEnvironmentVariable("UrlIdentityServer"),
                    ClientId = Environment.GetEnvironmentVariable("IdentityClientId"),
                    ClientSecret = Environment.GetEnvironmentVariable("IdentityClientSecret"),
                    Scope = "Olympus.full_access",

                    Url = Environment.GetEnvironmentVariable("UrlOlympus"),
                    TenantId = Environment.GetEnvironmentVariable("TenantIdOlympus")
                };

                await clientOlympus.Authorize(log);   

                // buscando compañia
                var companyJson = await clientOlympus.GetAsync($"/api/companies?Cif={data.Cif}", log);
                var companyArray = JsonConvert.DeserializeObject<Company[]>(companyJson);   
                if (companyArray.Length == 0)
                    return new BadRequestObjectResult($"No company was found with this cif: {data.Cif}");
                var company = companyArray[0];

                // buscando empleado
                var employeesJson = await clientOlympus.GetAsync($"/api/employees?identitycardnumber={data.Dni}&companyid={company.Companyid}", log);
                if (string.IsNullOrEmpty(employeesJson))
                    return new BadRequestObjectResult($"No employee was found with cif: {data.Cif} and dni: {data.Dni}");
                var employee = JsonConvert.DeserializeObject<Employee>(employeesJson);

                var clientAlexandria = new HttpApiManager()
                {
                    UrlAuthorize = Environment.GetEnvironmentVariable("UrlIdentityServer"),
                    ClientId = Environment.GetEnvironmentVariable("IdentityClientId"),
                    ClientSecret = Environment.GetEnvironmentVariable("IdentityClientSecret"),
                    Scope = "Alexandria.full_access",

                    Url = Environment.GetEnvironmentVariable("UrlAlexandria"),
                    TenantId = Environment.GetEnvironmentVariable("TenantIdOlympus")
                };
                await clientAlexandria.Authorize(log);

                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent("31"), "metadata[IdLoginCreator]");
                form.Add(new StringContent(data.DocumentId), "metadata[IdDocumento]");
                form.Add(new StringContent(data.ExpeditionDate), "metadata[FechaExpedicion]");
                form.Add(new StringContent(data.ExpirationDate), "metadata[FechaCaducidad]");
                form.Add(new StringContent(employee.EmployeeId), "metadata[IdRecurso]");
                form.Add(new StringContent("Employee"), "metadata[TipoDocumento]");

                if (body.Value.File != null)
                {
                    var fileInfo = body.Value.File.Headers.ContentDisposition;
                    var fileData = await body.Value.File.ReadAsByteArrayAsync();


                    if (body.Value.File.Headers.ContentLength != null)
                        form.Add(new ByteArrayContent(fileData, 0, (int)body.Value.File.Headers.ContentLength), fileInfo.Name,
                            fileInfo.FileName);
                }

                var resultUpload = await clientAlexandria.PostAsync("/api/documents", form, log);
                if (string.IsNullOrEmpty(resultUpload))
                    return new BadRequestObjectResult("Error when uploading the document to Alexandria");
                var fileUpload = JsonConvert.DeserializeObject<FileUploadResult>(resultUpload);


                var resultValidation = await clientAlexandria.PostAsync($"/api/documents/{fileUpload.Id}/validations","", log);
                if (string.IsNullOrEmpty(resultValidation))
                    return new BadRequestObjectResult("Error when validate the document to Alexandria");

                var result = JsonConvert.DeserializeObject<ResultValidation>(resultValidation);

                return (ActionResult)new OkObjectResult(JsonConvert.SerializeObject(result));

                //var tempResult = new ResultValidation()
                //{
                //    Id = 265,
                //    IdRequeriment = data.DocumentId,
                //    LastStatus = true,
                //    ExpirationDate = "2018-10-22T21:26:00.546Z",
                //    ExpeditionDate = "2018-10-22T21:26:00.546Z"
                //};

                //return (ActionResult) new OkObjectResult(JsonConvert.SerializeObject(tempResult));  
            }
            catch (global::System.Exception e)
            {
                log.Error($"Error: {e.Message}");
                return new BadRequestObjectResult($"Exception {e.Message}");
            }
        }

    }    
}
