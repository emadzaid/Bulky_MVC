using BulkyWebRazor.Data;
using BulkyWebRazor.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor.Pages.Categories
{
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDBContext _db;
        public Category CategoryObj { get; set; }
        public CreateModel(ApplicationDBContext db)
        {
            _db = db;
        }
        public IActionResult OnPost()
        {
            //Category category = new Category();
            //category.Name = Convert.ToString(Request.Form["Name"]);
            //category.DisplayOrder = Convert.ToInt32(Request.Form["DisplayOrder"]);

            _db.Categories.Add(CategoryObj);
            _db.SaveChanges();
            TempData["success"] = "Record created successfully";
            return RedirectToPage("Index");
           
        }
    }
}
