using BulkyWebRazor.Data;
using BulkyWebRazor.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor.Pages.Categories
{
    [BindProperties]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDBContext _db;
        public Category category { get; set; }
        public DeleteModel(ApplicationDBContext db)
        {
            _db = db;
        }

        public void OnGet(int? Id)
        {
            if(Id != null && Id != 0)
            {
                category = _db.Categories.Find(Id);
            }
           
        }
        public IActionResult OnPost()
        {
            Console.WriteLine(category.Id);
            Category? categoryFromDb = _db.Categories.Find(category.Id);
            if(categoryFromDb == null)
            {
                return NotFound();
            }
            _db.Categories.Remove(categoryFromDb);
            _db.SaveChanges();
            TempData["success"] = "Record deleted successfully";
            return RedirectToPage("Index");
        }
    }
}
