using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System.Web.Services.Description;
//https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/hubs-api-guide-javascript-client#crossdomain
[assembly: OwinStartupAttribute(typeof(MyHub.Startup))]
namespace MyHub
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureAuth(app);
            //app.MapSignalR();

            app.Map("/signalr", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                //var hubConfiguration = new HubConfiguration { };
                var hubConfiguration = new HubConfiguration
                {
                    EnableDetailedErrors = true,

                };
                map.RunSignalR(hubConfiguration);
            });
        }


    }
}
