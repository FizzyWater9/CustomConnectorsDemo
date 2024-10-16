using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ConnectorsDemo
{
    public class ConnectorFunction
    {
        private readonly ILogger<ConnectorFunction> _logger;

        public ConnectorFunction(ILogger<ConnectorFunction> log)
        {
            _logger = log;
        }

        private static List<User> users = new List<User>
        {
            new User { FirstName = "John", LastName = "Doe", Status = "new", PhoneNumber = "1234567890", Email = "john.doe@example.com", CostPerAcre = 3.5, Address = "123 Main St", PropertySize = 1500 },
            new User { FirstName = "Jane", LastName = "Smith", Status = "new", PhoneNumber = "0987654321", Email = "jane.smith@example.com", CostPerAcre = 4.0, Address = "456 Elm St", PropertySize = 2000 }
        };

        [FunctionName("GetUserContactInfo")]
        [OpenApiOperation(operationId: "GetUserContactInfo", tags: new[] { "User" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<User>), Description = "The contact information of all users")]
        public static IActionResult GetUserContactInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/contactinfo")] HttpRequest req,
            ILogger log)
        {
            var result = users.Select(u => new { u.FirstName, u.LastName, u.Address, u.PhoneNumber, u.Email }).ToList();
            users.ForEach(u => u.Status = "integrated");
            return new OkObjectResult(result);
        }

        [FunctionName("GetUserCostPerAcreAndSize")]
        [OpenApiOperation(operationId: "GetUserCostPerAcreAndSize", tags: new[] { "User" })]
        [OpenApiParameter(name: "lastName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The last name of the user")]
        public static IActionResult GetUserInterestAndSize(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{lastName}/interestandsize")] HttpRequest req,
            ILogger log, string lastName)
        {
            var user = users.FirstOrDefault(u => u.LastName == lastName);
            if (user == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(new { user.CostPerAcre, user.PropertySize });
        }

        [FunctionName("GetCalculatedCost")]
        [OpenApiOperation(operationId: "GetCalculatedCost", tags: new[] { "User" })]
        [OpenApiParameter(name: "lastName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The last name of the user")]
        public static IActionResult GetCalculatedCost(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{lastName}/calculatedcost")] HttpRequest req,
            ILogger log, string lastName)
        {
            var user = users.FirstOrDefault(u => u.LastName == lastName);
            if (user == null)
            {
                return new NotFoundResult();
            }
            var calculatedCost = user.CostPerAcre * user.PropertySize;
            return new OkObjectResult(new { CalculatedCost = calculatedCost });
        }

        [FunctionName("AddUser")]
        [OpenApiOperation(operationId: "AddUser", tags: new[] { "User" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(User), Description = "The new user to add")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "The user was added successfully")]
        public static async Task<IActionResult> AddUser(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/add")] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newUser = JsonConvert.DeserializeObject<User>(requestBody);
            users.Add(newUser);
            return new OkResult();
        }

        [FunctionName("UpdateUserContact")]
        [OpenApiOperation(operationId: "UpdateUserContact", tags: new[] { "User" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UserUpdate), Description = "The user contact information to update")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "The user contact information was updated successfully")]
        public static async Task<IActionResult> UpdateUserContact(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/updatecontact")] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateUser = JsonConvert.DeserializeObject<UserUpdate>(requestBody);
            var user = users.FirstOrDefault(u => u.LastName == updateUser.LastName);
            if (user == null)
            {
                return new NotFoundResult();
            }
            user.Email = updateUser.Email;
            user.PhoneNumber = updateUser.PhoneNumber;
            return new OkResult();
        }

        public class User
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Status { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
            public double CostPerAcre { get; set; }
            public string Address { get; set; }
            public int PropertySize { get; set; }
        }

        public class UserUpdate
        {
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
        }

    }
}

