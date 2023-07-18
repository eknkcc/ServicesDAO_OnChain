using DAO_WebPortal.Utility;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DAO_WebPortal.Resources;
using Helpers.Models.IdentityModels;
using static Helpers.Constants.Enums;
using System.Text.RegularExpressions;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.NotificationModels;
using Helpers.Models.WebsiteViewModels;
using Helpers.Models.KYCModels;
using Newtonsoft.Json;

namespace DAO_WebPortal.Controllers
{
    /// <summary>
    ///  Controller for public views and public actions
    /// </summary>
    public class PublicController : Controller
    {
        #region Views
        public IActionResult Index()
        {
            List<AuctionViewModel> auctionsModel = new List<AuctionViewModel>();

            try
            {
                //Get model from ApiGateway
                var auctionsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetAuctions");
                //Parse response
                auctionsModel = Helpers.Serializers.DeserializeJson<List<AuctionViewModel>>(auctionsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(auctionsModel);
        }

        [Route("Privacy-Policy")]
        public IActionResult Privacy_Policy()
        {
            ViewBag.HeaderTitle = "Privacy Policy";
            ViewBag.HeaderSubTitle = "ServicesDAO privacy policy and user agreement";


            return View();
        }

        [Route("Contact")]
        public IActionResult Contact()
        {
            ViewBag.HeaderTitle = "Contact";
            ViewBag.HeaderSubTitle = "Feel free to reach out for any questions and wishes.";

            return View();
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View();
        }

        [Route("Price-Discovery")]
        public IActionResult Price_Discovery()
        {
            ViewBag.HeaderTitle = "Price Discovery";
            ViewBag.HeaderSubTitle = "Ongoing and completed biddings.";

            List<AuctionViewModel> auctionsModel = new List<AuctionViewModel>();

            try
            {
                //Get model from ApiGateway
                var auctionsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetAuctions");
                //Parse response
                auctionsModel = Helpers.Serializers.DeserializeJson<List<AuctionViewModel>>(auctionsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(auctionsModel);
        }

        [Route("Price-Discovery-Detail/{AuctionID}")]
        public IActionResult Price_Discovery_Detail(int AuctionID)
        {
            ViewBag.HeaderTitle = "Auction Bids";
            ViewBag.HeaderSubTitle = "Bids given by the DAO users for Auction #" + AuctionID;

            AuctionDetailViewModel AuctionDetailModel = new AuctionDetailViewModel();

            try
            {
                //Get auction model from ApiGateway
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetAuctionDetail?id=" + AuctionID);
                //Parse response
                AuctionDetailModel.Auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                //If auction isn't completed users can't see bids
                if (AuctionDetailModel.Auction.Status == AuctionStatusTypes.PublicBidding || AuctionDetailModel.Auction.Status == AuctionStatusTypes.InternalBidding)
                {
                    return RedirectToAction("Price-Discovery");
                }

                //Get bids model from ApiGateway
                var auctionBidsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetAuctionBids?auctionid=" + AuctionID);

                //Parse response
                AuctionDetailModel.BidItems = Helpers.Serializers.DeserializeJson<List<AuctionBidItemModel>>(auctionBidsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(AuctionDetailModel);
        }

        [Route("Public-Job-Detail/{JobID}")]
        public IActionResult Public_Job_Detail(int JobID)
        {
            ViewBag.HeaderTitle = "Job Detail";
            ViewBag.HeaderSubTitle = "Detailed explanation of the Job #" + JobID;

            JobPostViewModel model = new JobPostViewModel();

            try
            {
                //Get model from ApiGateway
                var url = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetJobDetail?jobid=" + JobID);
                //Parse response
                model = Helpers.Serializers.DeserializeJson<JobPostViewModel>(url);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(model);
        }
        #endregion

        #region Login & Register Methods

        /// <summary>
        ///  User login function
        /// </summary>
        /// <param name="email">User's email or username</param>
        /// <param name="password">User's password</param>
        /// <param name="usercode">Captcha code (Needed after 3 failed requests)</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Login(string email, string password, string usercode)
        {
            try
            {
                // Check captcha only after 3 failed requests
                int failCount = Convert.ToInt32(HttpContext.Session.GetInt32("FailCount"));
                //if (failCount > 3 && !Utility.Captcha.ValidateCaptchaCode("securityCodeLogin", usercode, HttpContext))
                //{
                //    failCount++;
                //    HttpContext.Session.SetInt32("FailCount", failCount);
                //    return base.Json(new SimpleResponse { Success = false, Message = Lang.WrongErrorCodeEntered });
                //}

                //Get client Ip and Port
                string ip = IpHelper.GetClientIpAddress(HttpContext);
                string port = IpHelper.GetClientPort(HttpContext);

                //Create model
                LoginModel LoginModelPost = new LoginModel() { email = email, pass = password, ip = ip, port = port, application = Helpers.Constants.Enums.AppNames.DAO_WebPortal };

                //Post model to ApiGateway
                var loginJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/Login", Helpers.Serializers.SerializeJson(LoginModelPost));

                //Parse response
                LoginResponse loginModel = Helpers.Serializers.DeserializeJson<LoginResponse>(loginJson);

                if (loginModel.UserId != 0 && loginModel != null && loginModel.IsSuccessful == true)
                {
                    string token = loginModel.Token.ToString();
                    string login_email = loginModel.Email.ToString();

                    HttpContext.Session.SetInt32("UserID", loginModel.UserId);
                    HttpContext.Session.SetString("Email", login_email);
                    HttpContext.Session.SetString("Token", token);
                    HttpContext.Session.SetString("LoginType", "user");
                    HttpContext.Session.SetString("NameSurname", loginModel.NameSurname);
                    HttpContext.Session.SetString("UserType", loginModel.UserType.ToString());
                    HttpContext.Session.SetString("ProfileImage", loginModel.ProfileImage);
                    HttpContext.Session.SetString("KYCStatus", loginModel.KYCStatus.ToString());
                    if (!string.IsNullOrEmpty(loginModel.WalletAddress))
                    {
                        HttpContext.Session.SetString("WalletAddress", loginModel.WalletAddress.ToString());
                    }
                    //If user didn't sign in with Casper Signer
                    if (HttpContext.Session.GetInt32("ChainSign") != 1)
                    {
                        HttpContext.Session.SetInt32("ChainSign", -1);
                    }

                    TempData["toastr-message"] = Lang.SuccessLogin;
                    TempData["toastr-type"] = "success";
                    
                    return base.Json(new SimpleResponse { Success = true, Message = Lang.SuccessLogin });
                }
                else
                {
                    failCount++;
                    HttpContext.Session.SetInt32("FailCount", failCount);
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorUsernamePassword });
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        /// <summary>
        ///  New user registration function
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="username">Username</param>
        /// <param name="namesurname">Name Surname</param>
        /// <param name="password">Password</param>
        /// <param name="repass">Password confirmation</param>
        /// <param name="usercode">Captcha code (Needed after 3 failed requests)</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Register(string email, string username, string namesurname, string password, string repass, string usercode)
        {
            try
            {
                // Check captcha only after 3 failed requests
                int failCount = Convert.ToInt32(HttpContext.Session.GetInt32("FailCount"));
                //if (failCount > 3 && !Captcha.ValidateCaptchaCode("securityCodeRegister", usercode, HttpContext))
                //{
                //    failCount++;
                //    HttpContext.Session.SetInt32("FailCount", failCount);
                //    return base.Json(new SimpleResponse { Success = false, Message = Lang.WrongErrorCodeEntered });
                //}

                //Password match control
                if (password != repass)
                {
                    failCount++;
                    HttpContext.Session.SetInt32("FailCount", failCount);
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.NotCompatiblePass });
                }

                //Password strength control
                if (!Regex.IsMatch(password, @"^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])"))
                {
                    failCount++;
                    HttpContext.Session.SetInt32("FailCount", failCount);
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorPasswordMsg });
                }

                //Get client Ip and Port
                string ip = IpHelper.GetClientIpAddress(HttpContext);
                string port = IpHelper.GetClientPort(HttpContext);

                //Post model to ApiGateway
                var registerJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/Register", Helpers.Serializers.SerializeJson(new RegisterModel() { email = email, username = username, namesurname = namesurname, password = password, ip = ip, port = port }));

                //Parse response
                SimpleResponse registerResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(registerJson);

                if (registerResponse.Success == false)
                {
                    failCount++;
                    if (registerResponse.Message == "Username already exists.")
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorUserMsg });
                    }
                    else if (registerResponse.Message == "Email already exists.")
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorMailMsg });
                    }
                    else if (registerResponse.Message == "User post error")
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.UnexpectedError });
                    }
                    else
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
                    }
                }
                else
                {
                    return base.Json(new SimpleResponse { Success = true, Message = Lang.RegisterEmailSent });
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        /// <summary>
        /// Completes user registration from activation link in the confirmation email
        /// </summary>
        /// <param name="str">Encrypted user information in the registration email</param>
        /// <returns></returns>
        public ActionResult RegisterCompleteView(string str)
        {
            try
            {
                //Get result
                var completeJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/RegisterComplete", Helpers.Serializers.SerializeJson(new Helpers.Models.IdentityModels.RegisterCompleteModel() { registerToken = str }));

                //Parse result
                SimpleResponse completeResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(completeJson);

                if (completeResponse.Success)
                {
                    TempData["message"] = Lang.RegisterComplete;
                }
                else
                {
                    TempData["message"] = Lang.UnexpectedError;
                }
            }
            catch (Exception ex)
            {
                TempData["message"] = Lang.ErrorNote;

                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sends password reset email to user's email
        /// </summary>
        /// <param name="email">User's email</param>
        /// <param name="usercode">Captcha code (Needed after 3 failed requests)</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ResetPassword(string email, string usercode)
        {

            try
            {
                // Check captcha only after 3 failed requests
                int failCount = Convert.ToInt32(HttpContext.Session.GetInt32("FailCount"));
                //if (failCount > 3 && !Captcha.ValidateCaptchaCode("securityCodeResetPass", usercode, HttpContext))
                //{
                //    failCount++;
                //    HttpContext.Session.SetInt32("FailCount", failCount);
                //    return base.Json(new SimpleResponse { Success = false, Message = Lang.WrongErrorCodeEntered });
                //}

                //Post model to ApiGateway
                var resetJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/ResetPassword", Helpers.Serializers.SerializeJson(new ResetPasswordModel() { email = email }));

                //Parse result
                SimpleResponse resetResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(resetJson);

                if (resetResponse.Success)
                {
                    failCount++;
                    return base.Json(new SimpleResponse { Success = true, Message = Lang.PasswordResetSuccess });
                }
                else
                {
                    failCount++;
                    if (resetResponse.Message == "Email error")
                    {
                        HttpContext.Session.SetInt32("FailCount", failCount);
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.EmailError });
                    }
                    else
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.UnexpectedError });
                    }
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.UnexpectedError });
            }
        }

        /// <summary>
        /// Opens password reset model from 
        /// </summary>
        /// <param name="str">Encrypted user information in the password reset email</param>
        /// <returns></returns>
        public ActionResult ResetPasswordView(string str)
        {
            try
            {
                //Set password change token into session
                HttpContext.Session.SetString("passwordchangetoken", str);

                //Decrypt information
                string decryptedToken = Helpers.Encryption.DecryptString(str);

                //Check if format is valid
                if (decryptedToken.Split('|').Length > 1)
                {
                    //Set user's email
                    HttpContext.Session.SetString("passwordchangeemail", decryptedToken.Split('|')[0]);
                    TempData["action"] = "resetpassword";
                }
                else
                {
                    TempData["message"] = Lang.IncorrectPassReset;
                }
            }
            catch (Exception ex)
            {
                TempData["message"] = Lang.ErrorNote;

                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sets user new password
        /// </summary>
        /// <param name="newpass">New password</param>
        /// <param name="newpassagain">New password confirmation</param>
        /// <param name="usercode">Captcha code (Needed after 3 failed requests)</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ResetPasswordComplete(string newpass, string newpassagain, string usercode)
        {
            try
            {
                // Check captcha only after 3 failed requests
                int failCount = Convert.ToInt32(HttpContext.Session.GetInt32("FailCount"));
                //if (failCount > 3 && !Captcha.ValidateCaptchaCode("securityCodeResetPassComplete", usercode, HttpContext))
                //{
                //    failCount++;
                //    HttpContext.Session.SetInt32("FailCount", failCount);
                //    return base.Json(new SimpleResponse { Success = false, Message = Lang.WrongErrorCodeEntered });
                //}

                //Password match control
                if (newpass != newpassagain)
                {
                    failCount++;
                    HttpContext.Session.SetInt32("FailCount", failCount);
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.NotCompatiblePass });
                }

                //Password strength control
                if (!Regex.IsMatch(newpass, @"^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])"))
                {
                    failCount++;
                    HttpContext.Session.SetInt32("FailCount", failCount);
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorPasswordMsg });
                }

                //Post model to ApiGateway
                var resetJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/ResetPasswordComplete", Helpers.Serializers.SerializeJson(new ResetCompleteModel() { newPass = newpass, passwordChangeToken = HttpContext.Session.GetString("passwordchangetoken") }));

                //Parse result
                SimpleResponse resetResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(resetJson);

                if (resetResponse.Success)
                {
                    return base.Json(new SimpleResponse { Success = true, Message = Lang.UpdatePassword });
                }
                else
                {
                    if (resetResponse.Message == "Renew expired")
                    {
                        HttpContext.Session.SetString("passwordchangeemail", "true");

                        return base.Json(new SimpleResponse { Success = false, Message = Lang.RenewExpired });
                    }
                    else
                    {
                        return base.Json(new SimpleResponse { Success = false, Message = Lang.UnexpectedError });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        /// <summary>
        /// User logout function
        /// </summary>
        /// <returns></returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        #endregion

        /// <summary>
        ///  Sends user's message as email to system admins
        /// </summary>
        /// <param name="namesurname">User's name surname</param>
        /// <param name="email">User's email</param>
        /// <param name="message">User's message</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitContactForm(string namesurname, string email, string message, string usercode)
        {
            try
            {
                // Check captcha
                if (!Utility.Captcha.ValidateCaptchaCode("securityCodeContact", usercode, HttpContext))
                {
                    return base.Json(new SimpleResponse { Success = false, Message = Lang.WrongErrorCodeEntered });
                }

                //Create email model
                SendEmailModel model = new SendEmailModel();
                model.Subject = "Contact form submission from anonymous user";
                model.Content = "Name surname: " + namesurname + ", Email:" + email + ", Message:" + message;

                //Send email to system Admin
                string jsonResponse = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/Notification/SendPublicContactEmail", Helpers.Serializers.SerializeJson(model));

                //Parse response
                SimpleResponse res = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResponse);

                if (res.Success == false)
                {
                    res.Message = "Currently we are unable to send your message. Please try again later.";
                }

                return Json(res);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        /// <summary>
        /// Creates captcha image for public forms
        /// This method is called after 3 failed requests
        /// </summary>
        /// <param name="code">Captcha code</param>
        /// <returns>Captcha image</returns>
        [Route("get-captcha-image")]
        public IActionResult GetCaptchaImage(string code)
        {
            try
            {
                int width = 100;
                int height = 36;
                var captchaCode = Captcha.GenerateCaptchaCode();
                var result = Captcha.GenerateCaptchaImage(width, height, captchaCode);
                HttpContext.Session.SetString(code, result.CaptchaCode);
                Stream s = new MemoryStream(result.CaptchaByteData);
                return new FileStreamResult(s, "image/png");
            }
            catch
            {

            }

            return Json("");
        }

        [HttpPost("KycCallBack", Name = "KycCallBack")]
        public SimpleResponse KycCallBack([FromBody] KYCCallBack Response)
        {
            SimpleResponse model = new SimpleResponse();
            try
            {
                Program.monitizer.AddConsole(Response.ToString());

                var userJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/KycCallBack", JsonConvert.SerializeObject(Response), HttpContext.Session.GetString("Token"));
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return model;
        }


        #region ChainTest

        public IActionResult ChainTest()
        {
            try
            {
                //string rpcUrl = "http://136.243.187.84:7777/rpc";

                //CasperClient casperClient = new CasperClient(rpcUrl);
                //var result = casperClient.RpcService.GetStateRootHash();
            }
            catch (Exception ex)
            {

            }


            return View();
        }

        // public JsonResult GetBalance(string accountKey)
        // {
        //     try
        //     {
        //         string rpcUrl = "http://136.243.187.84:7777/rpc";

        //         CasperClient casperClient = new CasperClient(rpcUrl);
        //         //var result = casperClient.RpcService.GetAccountInfo(accountKey);
        //         var result = casperClient.RpcService.GetAccountBalance(accountKey);
        //         return Json(result);
        //     }
        //     catch (Exception ex)
        //     {

        //     }

        //     return Json("");
        // }

        // public JsonResult GetAccountHash(string accountKey)
        // {
        //     try
        //     {
        //         string rpcUrl = "http://136.243.187.84:7777/rpc";

        //         CasperClient casperClient = new CasperClient(rpcUrl);
        //         var result = casperClient.HashService.GetAccountHash(accountKey);

        //         return Json(result);
        //     }
        //     catch (Exception ex)
        //     {

        //     }

        //     return Json("");
        // }

        // public JsonResult CreateDeployObject(string fromAccount, string toAccount, double amount)
        // {
        //     try
        //     {
        //         string rpcUrl = "http://136.243.187.84:7777/rpc";
        //         CasperClient casperClient = new CasperClient(rpcUrl);

        //         //Make the Deploy request
        //         PutDeployStoredContractByHashRequest request = MakeDeployStoredContractByHash(casperClient, fromAccount, toAccount, amount);
        //         string json = JsonConvert.SerializeObject(request.Parameters);
        //         //Dispatch the Deploy
        //         //var result = casperClient.DeployService.PutDeploy(request);
        //         return Json(json);
        //     }
        //     catch (Exception ex)
        //     {

        //     }

        //     return Json("");
        // }

        // public JsonResult CreateTransferObject(string fromAccount, string toAccount, double amount)
        // {
        //     try
        //     {
        //         string rpcUrl = "http://136.243.187.84:7777/rpc";
        //         CasperClient casperClient = new CasperClient(rpcUrl);

        //         ulong id = 287821;
        //         //Set the amount in motes
        //         var normAmount = (ulong)(amount * 1000000000);

        //         //Create Stored Contract By Hash Request
        //         PutDeployTransferRequest putDeployRequest = new PutDeployTransferRequest();
        //         putDeployRequest.id = casperClient.RpcService.JsonRpcId;
        //         putDeployRequest.jsonrpc = casperClient.RpcService.JsonRpcVersion;
        //         putDeployRequest.Parameters = new PutDeployTransferParameters();
        //         putDeployRequest.Parameters.deploy = new PutDeployTransfer();
        //         putDeployRequest.Parameters.deploy.approvals = new List<Approval>();

        //         //Set Payment for Delegate
        //         decimal delegatePayment = 10000000000;
        //         string delegatePaymentByte = ByteUtil.ByteArrayToHex(TypesSerializer.Getu512SerializerWithLength(delegatePayment));
        //         //Set Payment arguments
        //         var argsPayment = new List<DeployNamedArg>();
        //         argsPayment.Add(new DeployNamedArg("amount", new CLValue() { cl_type = CLType.CLTypeEnum.U512, bytes = delegatePaymentByte, parsed = delegatePayment.ToString() }));
        //         //Set Deploy Payment
        //         var payment = new DeployPayment();
        //         payment.ModuleBytes = new DeployModuleBytes(argsPayment);
        //         payment.ModuleBytes.module_bytes = "";
        //         putDeployRequest.Parameters.deploy.payment = payment;

        //         //Set Transfer
        //         string amountBytes = ByteUtil.ByteArrayToHex(TypesSerializer.Getu512SerializerWithLength(normAmount));

        //         string idBytes = ByteUtil.ByteArrayToHex(TypesSerializer.Getu64SerializerWithPrefixOption(id));

        //         //Set Trasfer Arguments
        //         var argsDelegate = new List<DeployNamedArg>();
        //         argsDelegate.Add(new DeployNamedArg("amount", new CLValue() { cl_type = CLType.CLTypeEnum.U512, bytes = amountBytes, parsed = amount.ToString() }));
        //         argsDelegate.Add(new DeployNamedArg("target", new CLValue() { cl_type = CLType.CLTypeEnum.PublicKey, bytes = toAccount, parsed = toAccount }));
        //         argsDelegate.Add(new DeployNamedArg("id", new CLValue() { cl_type = CLType.CLTypeEnum.U64, bytes = idBytes, parsed = id.ToString() }));
        //         putDeployRequest.Parameters.deploy.session = new DeploySessionTransfer();
        //         putDeployRequest.Parameters.deploy.session.Transfer = new DeployTransfer(argsDelegate);

        //         //Set Header
        //         putDeployRequest.Parameters.deploy.header = new DeployHeader();
        //         putDeployRequest.Parameters.deploy.header.account = fromAccount;
        //         putDeployRequest.Parameters.deploy.header.timestamp = DateTime.Now;
        //         putDeployRequest.Parameters.deploy.header.ttl = "30m";
        //         putDeployRequest.Parameters.deploy.header.gas_price = 1;
        //         //Get the Body hash
        //         byte[] paymentBytes = putDeployRequest.Parameters.deploy.payment.ModuleBytes.ToBytes();
        //         byte[] delegateBytes = putDeployRequest.Parameters.deploy.session.Transfer.ToBytes();
        //         byte[] combined = ByteUtil.CombineBytes(paymentBytes, delegateBytes);
        //         putDeployRequest.Parameters.deploy.header.body_hash = casperClient.HashService.GetHashToHexFixedSize(combined, 32);

        //         putDeployRequest.Parameters.deploy.header.dependencies = new List<string>();
        //         putDeployRequest.Parameters.deploy.header.chain_name = "casper-test";

        //         //Get the serialized header
        //         byte[] serializedHeader = casperClient.DeployService.GetSerializedHeader(putDeployRequest.Parameters.deploy.header);
        //         string hashedHeader = casperClient.HashService.GetHashToHexFixedSize(serializedHeader, 32);

        //         //Set Deploy Hash
        //         putDeployRequest.Parameters.deploy.hash = hashedHeader;

        //         string json = JsonConvert.SerializeObject(putDeployRequest.Parameters);

        //         return Json(json);
        //     }
        //     catch (Exception ex)
        //     {

        //     }

        //     return Json("");
        // }

        // public static PutDeployStoredContractByHashRequest MakeDeployStoredContractByHash(CasperClient client, string fromAccount, string toAccount, double amount)
        // {
        //     // //Amount to transfer in CSPR tokens
        //     // double amount = 10;
        //     // //From Account
        //     // string fromAccount = "01da19d95aae08da9df0c3a7ba8fbb368af4fb99e7f522b6471963473295955031";
        //     // //To this Account
        //     // string toAccount = "01c4328cde0ce19e18e8bf61cb0f62af889b928a1b958ce69c401e21b07fb7acd6";

        //     //The id of the transaction
        //     ulong id = 1;
        //     // //The public key pem file location in disk. This key should match the sender account
        //     // string publicKeyLocation = @"C:\tmp\Keys\public_key.pem";
        //     // //The private key pem file location in disk. This key should match the sender account
        //     // string privateKeyLocation = @"C:\tmp\Keys\secret_key.pem";

        //     //Set the amount in motes
        //     var normAmount = (ulong)(amount * 1000000000);

        //     //Create Stored Contract By Hash Request
        //     PutDeployStoredContractByHashRequest putDeployRequest = new PutDeployStoredContractByHashRequest();
        //     putDeployRequest.id = client.RpcService.JsonRpcId;
        //     putDeployRequest.jsonrpc = client.RpcService.JsonRpcVersion;
        //     putDeployRequest.Parameters = new PutDeployStoredContractByHashParameters();
        //     putDeployRequest.Parameters.deploy = new PutDeployStoredContractByHash();


        //     //Set Payment for Delegate
        //     decimal delegatePayment = 2500010000;
        //     string delegatePaymentByte = ByteUtil.ByteArrayToHex(TypesSerializer.Getu512SerializerWithLength(delegatePayment));
        //     //Set Payment arguments
        //     var argsPayment = new List<DeployNamedArg>();
        //     argsPayment.Add(new DeployNamedArg("amount", new CLValue() { cl_type = CLType.CLTypeEnum.U512, bytes = delegatePaymentByte, parsed = delegatePayment.ToString() }));
        //     //Set Deploy Payment
        //     var payment = new DeployPayment();
        //     payment.ModuleBytes = new DeployModuleBytes(argsPayment);
        //     payment.ModuleBytes.module_bytes = "";
        //     putDeployRequest.Parameters.deploy.payment = payment;

        //     //Set Transfer
        //     string amountBytes = ByteUtil.ByteArrayToHex(TypesSerializer.Getu512SerializerWithLength(normAmount));
        //     //Set Trasfer Arguments
        //     var argsDelegate = new List<DeployNamedArg>();
        //     argsDelegate.Add(new DeployNamedArg("delegator", new CLValue() { cl_type = CLType.CLTypeEnum.PublicKey, bytes = fromAccount, parsed = fromAccount }));
        //     argsDelegate.Add(new DeployNamedArg("validator", new CLValue() { cl_type = CLType.CLTypeEnum.PublicKey, bytes = toAccount, parsed = toAccount }));
        //     argsDelegate.Add(new DeployNamedArg("amount", new CLValue() { cl_type = CLType.CLTypeEnum.U512, bytes = amountBytes, parsed = amount.ToString() }));

        //     putDeployRequest.Parameters.deploy.session = new DeploySessionStoredContractByHash();
        //     putDeployRequest.Parameters.deploy.session.StoredContractByHash = new DeployStoredContractByHash(argsDelegate);
        //     putDeployRequest.Parameters.deploy.session.StoredContractByHash.entry_point = StakingDeployEnum.Delegate.ToString().ToLower();

        //     //Set Hash Key of Delegate Contract
        //     putDeployRequest.Parameters.deploy.session.StoredContractByHash.hash = "ccb576d6ce6dec84a551e48f0d0b7af89ddba44c7390b690036257a04a3ae9ea";

        //     //Set Header
        //     putDeployRequest.Parameters.deploy.header = new DeployHeader();
        //     putDeployRequest.Parameters.deploy.header.account = fromAccount;
        //     putDeployRequest.Parameters.deploy.header.timestamp = DateTime.Now;
        //     putDeployRequest.Parameters.deploy.header.ttl = "30m";
        //     putDeployRequest.Parameters.deploy.header.gas_price = 1;
        //     //Get the Body hash
        //     byte[] paymentBytes = putDeployRequest.Parameters.deploy.payment.ModuleBytes.ToBytes();
        //     byte[] delegateBytes = putDeployRequest.Parameters.deploy.session.StoredContractByHash.ToBytes();
        //     byte[] combined = ByteUtil.CombineBytes(paymentBytes, delegateBytes);
        //     putDeployRequest.Parameters.deploy.header.body_hash = client.HashService.GetHashToHexFixedSize(combined, 32);

        //     putDeployRequest.Parameters.deploy.header.dependencies = new List<string>();
        //     putDeployRequest.Parameters.deploy.header.chain_name = "casper";

        //     //Get the serialized header
        //     byte[] serializedHeader = client.DeployService.GetSerializedHeader(putDeployRequest.Parameters.deploy.header);
        //     string hashedHeader = client.HashService.GetHashToHexFixedSize(serializedHeader, 32);

        //     //Set Deploy Hash
        //     putDeployRequest.Parameters.deploy.hash = hashedHeader;

        //     //Set Approval
        //     // var keys = client.SigningService.GetKeyPairFromFile(publicKeyLocation, privateKeyLocation, SignAlgorithmEnum.ed25519);
        //     // putDeployRequest.Parameters.deploy.approvals = new List<Approval>();
        //     // putDeployRequest.Parameters.deploy.approvals.Add(client.DeployService.SignApproval(fromAccount, putDeployRequest.Parameters.deploy.hash, keys));


        //     return putDeployRequest;
        // }

        // [HttpPost]
        // public JsonResult SendSignedDeploy(string val)
        // {
        //     try
        //     {
        //         string rpcUrl = "http://136.243.187.84:7777/rpc";
        //         CasperClient casperClient = new CasperClient(rpcUrl);

        //         PutDeployTransferRequest putDeployRequest = new PutDeployTransferRequest();
        //         putDeployRequest.id = casperClient.RpcService.JsonRpcId;
        //         putDeployRequest.jsonrpc = casperClient.RpcService.JsonRpcVersion;
        //         putDeployRequest.Parameters = new PutDeployTransferParameters();
        //         putDeployRequest.Parameters.deploy = new PutDeployTransfer();
        //         putDeployRequest.Parameters.deploy.approvals = new List<Approval>();

        //         putDeployRequest.Parameters.deploy = JsonConvert.DeserializeObject<PutDeployTransfer>(val);

        //         //Dispatch the Deploy
        //         //var result = casperClient.DeployService.PutDeploy(val);

        //         //Console.WriteLine($"Transfer Executed, Deploy Hash: {result.result.deploy_hash}");
        //         //Wait until deploy is completed. This may take few seconds\minutes
        //         //var deployResult = await client.RpcService.AwaitUntilDeployCompletedAsync(result.result.deploy_hash);

        //     }
        //     catch (Exception ex)
        //     {

        //     }

        //     return Json("");
        // }

        #endregion

    }
}
