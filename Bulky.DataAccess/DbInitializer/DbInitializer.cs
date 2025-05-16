using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDBContext _db;
        public DbInitializer(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            ApplicationDBContext db
            ) {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;  
        }
        public void Initialize() 
        {
            // add all the pending migrations

            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) { }

            // create roles if they are not created

            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole { Name = SD.Role_Customer }).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole { Name = SD.Role_Employee }).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole { Name = SD.Role_Company }).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole { Name = SD.Role_Admin }).GetAwaiter().GetResult();

                // create the first admin account
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@bulky.com",
                    Email = "admin@bulky.com",
                    Name = "Emad Zaid",
                    PhoneNumber = "1234567890",
                    StreetAddress = "123 Street Ave",
                    State = "PU",
                    PostalCode = "12345",
                    City = "Lahore"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bulky.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();

            };

            
        }
    }
}
