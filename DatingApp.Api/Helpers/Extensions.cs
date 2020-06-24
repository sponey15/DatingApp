using Microsoft.AspNetCore.Http;

namespace DatingApp.Api.Helpers
{
    public static class Extensions
    {
        public static void AddAppliactionError(this HttpResponse response, string message)
        {
            response.Headers.Add("Appliaction-Error", message);
            response.Headers.Add("Access-Control-Expose-Headers", "Applicaton-Error");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
}