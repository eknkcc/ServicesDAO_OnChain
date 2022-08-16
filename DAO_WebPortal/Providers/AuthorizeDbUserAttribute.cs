using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace DAO_WebPortal.Providers
{
    /// <summary>
    ///  Authorization attribute for off-chain methods such as forum, auctions etc.
    /// </summary>
    public class AuthorizeDbUserAttribute : ActionFilterAttribute
    {
        public AuthorizeDbUserAttribute()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                bool control = true;

                if (context.HttpContext.Session.GetInt32("UserID") == null || context.HttpContext.Session.GetInt32("UserID") <= 0)
                {
                    control = false;
                }

                if (!control)
                {
                    context.Result = new JsonResult("Unauthorized");
                }
            }
            catch
            {
                context.Result = new JsonResult("Unauthorized");
            }

        }
    }
}
