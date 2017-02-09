using System.Web.Mvc;

namespace CertifiedLabel.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {

            context.MapRoute(
                "Admin_elmah",
                "Admin/elmah/{type}",
                new { action = "Index", controller = "Elmah", type = UrlParameter.Optional }
            );            
        }
    }
}