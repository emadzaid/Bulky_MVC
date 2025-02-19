using BulkyWebRazor.Data;
using BulkyWebRazor.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDBContext _db;
        [BindProperty]
        public Category Category { get; set; }
        public EditModel(ApplicationDBContext db) {
            _db = db;
        }
        public void OnGet(int? Id)
        {
            if(Id != null && Id != 0)
            {
               Category = _db.Categories.Find(Id);
            }
        }
        public IActionResult OnPost()
        {
            if(ModelState.IsValid)
            {
                _db.Categories.Update(Category);
                _db.SaveChanges();
                TempData["success"] = "Record updated successfully";
                return RedirectToPage("Index");
            }

            return Page();

        }
    }
}
