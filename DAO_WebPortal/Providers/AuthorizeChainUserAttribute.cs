using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace DAO_WebPortal.Providers
{
    /// <summary>
    ///  Authorization attribute for methods which requires on-chain signing. (Requires signer connection)
    /// </summary>
    public class AuthorizeChainUserAttribute : ActionFilterAttribute
    {
        public AuthorizeChainUserAttribute()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                bool control = true;

                if(Program._settings.DaoBlockchain != null)
                {
                    if (context.HttpContext.Session.GetInt32("ChainSign") == null || context.HttpContext.Session.GetInt32("ChainSign") != 1)
                    {
                        control = false;
                    }

                    if (!control)
                    {
                        context.Result = new JsonResult("Unauthorized");
                    }
                }
                else
                {
                    //If dao uses central db then follow standard authorization process.
                    if (context.HttpContext.Session.GetInt32("UserID") == null || context.HttpContext.Session.GetInt32("UserID") <= 0)
                    {
                        control = false;
                    }

                    if (!control)
                    {
                        context.Result = new JsonResult("Unauthorized");
                    }
                }
            }
            catch
            {
                context.Result = new JsonResult("Unauthorized");
            }

        }
    }
}
