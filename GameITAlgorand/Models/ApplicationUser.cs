using Microsoft.AspNetCore.Identity;

namespace GameITAlgorand.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string AccountAddress { get; set; }
        public string Key { get; set; }
    }
}
