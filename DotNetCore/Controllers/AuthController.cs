using DotNetCore.Hubs;
using DotNetCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static DotNetCore.Models.Constants;

namespace DotNetCore.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtFactory _jwtFactory;
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public AuthController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtFactory jwtFactory,
            IOptions<JwtIssuerOptions> jwtOptions,
            IWebHostEnvironment hostingEnvironment,
            ApplicationDbContext dbContext,
            IHubContext<ChatHub> hubContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtFactory = jwtFactory;
            _jwtOptions = jwtOptions.Value;
            _hostingEnvironment = hostingEnvironment;
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Post(CredentialsViewModel credentials)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userToVerify = await _userManager.FindByNameAsync(credentials.UserName);
            if (userToVerify == null) return BadRequest("User not found!");

            if (await _userManager.CheckPasswordAsync(userToVerify, credentials.Password))
            {
                var JwtToken = await GenerateJwtToken(credentials.UserName);
                return Ok(JwtToken);
            }
            return BadRequest("Invalid username or password.");
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity.IsAuthenticated)
                _ = User.Identity.Name;

            return Ok(await _userManager.FindByIdAsync(User.Identity.Name));
        }

        private async Task<LoginResponse> GenerateJwtToken(string UserName)
        {
            ApplicationUser UserData = await _userManager.FindByNameAsync(UserName);
            var Roles = await _userManager.GetRolesAsync(UserData);
            var accessToken = await _jwtFactory.GenerateEncodedToken(UserData.Id, Roles);
            return new LoginResponse(accessToken, UserData.SecurityStamp);
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return await Task.FromResult<ClaimsIdentity>(null);

            // get the user to verifty
            var userToVerify = await _userManager.FindByNameAsync(userName);

            if (userToVerify == null) return await Task.FromResult<ClaimsIdentity>(null);

            // check the credentials
            if (await _userManager.CheckPasswordAsync(userToVerify, password))
            {
                return await Task.FromResult(_jwtFactory.GenerateClaimsIdentity(userName, userToVerify.Id.ToString()));
            }

            // Credentials are invalid, or account doesn't exist
            return await Task.FromResult<ClaimsIdentity>(null);
        }

        [AllowAnonymous]
        [HttpPost("UploadFile")]
        public IActionResult UploadFile(IFormFile file)
        {
            //1mb
            long MaxSize = 1 * 1024 * 1024;

            if (file == null)
                return BadRequest("Required File");

            if (file.Length > MaxSize)
                return BadRequest("Max Length");

            SaveFile(file);
            try
            {
                switch (file.ContentType)
                {
                    case "text/xml":
                        ReadXml(file.FileName);
                        return Ok("Ok");

                    case "text/csv":
                        ReadCsv(file.FileName);
                        return Ok("Ok");

                    default:
                        return BadRequest("Unknown format");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private void SaveFile(IFormFile file)
        {
            string path = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (FileStream stream = new(Path.Combine(path, file.FileName), FileMode.Create))
            {
                file.CopyTo(stream);
                stream.Dispose();
                stream.Close();
            }
        }

        private void SaveData(List<Transaction> data)
        {
            _dbContext.Transaction.AddRange(data);
            _dbContext.SaveChanges();
        }

        private void ReadXml(string FileName)
        {
            try
            {
                List<Transaction> i = new List<Transaction>();
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", FileName));

                foreach (XmlNode node in doc.SelectNodes("/Transactions/Transaction"))
                {
                    i.Add(new Transaction
                    {
                        TransactionId = node.Attributes["id"].Value,
                        Amount = decimal.Parse(node["PaymentDetails"]?.SelectSingleNode("Amount")?.InnerText),
                        CurrencyCode = node["PaymentDetails"]?.SelectSingleNode("CurrencyCode")?.InnerText,
                        TransactionDate = DateTime.Parse(node["TransactionDate"]?.InnerText),
                        Status = (TransactionStatus)Enum.Parse(typeof(TransactionStatus), node["Status"]?.InnerText)
                    });
                }

                bool isValid = TryValidateModel(i);
                if (isValid)
                    SaveData(i);
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
            catch (FormatException ex)
            {
                throw ex;
            }
        }

        private void ReadCsv(string FileName)
        {
            try
            {
                List<Transaction> i = new List<Transaction>();

                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                using (var reader = new StreamReader(Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", FileName)))
                {
                    while (!reader.EndOfStream)
                    {
                        var lineData = CSVParser.Split(reader.ReadLine());
                        for (int j = 0; j < lineData.Length; j++)
                        {
                            lineData[j] = lineData[j].TrimStart(' ', '"');
                            lineData[j] = lineData[j].TrimEnd('"');
                        }

                        i.Add(new Transaction
                        {
                            TransactionId = lineData[0],
                            Amount = decimal.Parse(lineData[1]),
                            CurrencyCode = lineData[2],
                            TransactionDate = DateTime.ParseExact(lineData[3], "dd/MM/yyyy hh:mm:ss", CultureInfo.InvariantCulture),
                            Status = (TransactionStatus)Enum.Parse(typeof(TransactionStatus), lineData[4])
                        });
                    }
                }

                bool isValid = TryValidateModel(i);
                if (isValid)
                    SaveData(i);
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
            catch (FormatException ex)
            {
                throw ex;
            }
        }

        [AllowAnonymous]
        [HttpGet("ReadXml")]
        public List<Transaction> ReadXml()
        {
            try
            {
                List<Transaction> i = new List<Transaction>();
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "new.xml"));

                foreach (XmlNode node in doc.SelectNodes("/Transactions/Transaction"))
                {
                    i.Add(new Transaction
                    {
                        TransactionId = node.Attributes["id"].Value,
                        Amount = decimal.Parse(node["PaymentDetails"]?.SelectSingleNode("Amount")?.InnerText),
                        CurrencyCode = node["PaymentDetails"]?.SelectSingleNode("CurrencyCode")?.InnerText,
                        TransactionDate = DateTime.Parse(node["TransactionDate"]?.InnerText),
                        Status = (TransactionStatus)Enum.Parse(typeof(TransactionStatus), node["Status"]?.InnerText)
                    }); ;
                }

                bool isValid = TryValidateModel(i);
                if (isValid)
                    SaveData(i);
                return i;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
            catch (FormatException ex)
            {
                throw ex;
            }
        }

        [AllowAnonymous]
        [HttpGet("ReadCsv")]
        public List<Transaction> ReadCsv()
        {
            try
            {
                List<Transaction> i = new List<Transaction>();

                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                using (var reader = new StreamReader(Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "newc.csv")))
                {
                    while (!reader.EndOfStream)
                    {
                        var lineData = CSVParser.Split(reader.ReadLine());
                        for (int j = 0; j < lineData.Length; j++)
                        {
                            lineData[j] = lineData[j].TrimStart(' ', '"');
                            lineData[j] = lineData[j].TrimEnd('"');
                        }

                        i.Add(new Transaction
                        {
                            TransactionId = lineData[0],
                            Amount = decimal.Parse(lineData[1]),
                            CurrencyCode = lineData[2],
                            TransactionDate = DateTime.ParseExact(lineData[3], "dd/MM/yyyy hh:mm:ss", CultureInfo.InvariantCulture),
                            Status = (TransactionStatus)Enum.Parse(typeof(TransactionStatus), lineData[4])
                        });
                    }
                }

                bool isValid = TryValidateModel(i);
                if (isValid)
                    SaveData(i);
                return i;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
            catch (FormatException ex)
            {
                throw ex;
            }
        }

        [AllowAnonymous]
        [HttpGet("ReadTransaction")]
        public IActionResult ReadTransaction()
        {
            return Ok(_dbContext.Transaction.Select(m => new ViewTransaction
            {
                id = m.TransactionId,
                payment = $"{m.Amount:n2} {m.CurrencyCode}",
                StatusEnum = m.Status
            }).ToList()); ;
        }

        [AllowAnonymous]
        [HttpGet("ReadTransaction/Date/{FrDate}/{ToDate}")]
        public IActionResult ReadTransaction(DateTime FrDate, DateTime ToDate)
        {
            var DbF = Microsoft.EntityFrameworkCore.EF.Functions;
            return Ok(_dbContext.Transaction.Where(m => m.TransactionDate >= FrDate && m.TransactionDate <= ToDate).Select(m => new ViewTransaction
            {
                id = m.TransactionId,
                payment = $"{m.Amount:n2} {m.CurrencyCode}",
                StatusEnum = m.Status
            }).ToList()); ;
        }

        [AllowAnonymous]
        [HttpGet("ReadTransaction/Currency/{Currency}")]
        public IActionResult ReadTransaction(string Currency)
        {
            var DbF = Microsoft.EntityFrameworkCore.EF.Functions;
            return Ok(_dbContext.Transaction.Where(m => m.CurrencyCode == Currency).Select(m => new ViewTransaction
            {
                id = m.TransactionId,
                payment = $"{m.Amount:n2} {m.CurrencyCode}",
                StatusEnum = m.Status
            }).ToList()); ;
        }

        [AllowAnonymous]
        [HttpGet("ReadTransaction/Status/{Status}")]
        public IActionResult ReadTransactionStatus(TransactionStatus Status)
        {
            var DbF = Microsoft.EntityFrameworkCore.EF.Functions;
            return Ok(_dbContext.Transaction.Where(m => m.Status == Status).Select(m => new ViewTransaction
            {
                id = m.TransactionId,
                payment = $"{m.Amount:n2} {m.CurrencyCode}",
                StatusEnum = m.Status
            }).ToList()); ;
        }

        [AllowAnonymous]
        [HttpGet("Init")]
        public IActionResult Init()
        {
            ISeriesLine<ApplicationDbContext> _job = new JobSeriesLine<ApplicationDbContext>(_dbContext);
            _job.Init();
            _job.Context.Save();

            var doc = _job.GenerateDocument(EnumSerialCode.Customer);
            _job.Context.Save();
            return Ok(doc);
        }

        [AllowAnonymous]
        [HttpGet("send")]
        public async Task<IActionResult> send()
        {
            await _hubContext.Clients.All.SendAsync("broadcastMessage", "server", $"Home page loaded at: {DateTime.Now}");
            return Ok("");
        }
    }
}