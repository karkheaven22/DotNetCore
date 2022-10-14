using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace DotNetCore
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
        }

        public ApplicationRole(string roleName, string description) : base(roleName)
        {
            base.Name = roleName;
            this.Description = description;
        }

        [MaxLength(200)]
        public string Description { get; set; } = "";

        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}