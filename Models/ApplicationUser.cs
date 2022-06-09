using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace DotNetCore
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base()
        {
            Id = Guid.NewGuid().ToString();
        }

        [MaxLength(50)]
        public string FirstName { get; set; }

        [MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(50)]
        public string Password { get; set; } = "";
    }
}