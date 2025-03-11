using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModels;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWorkRepository _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWorkRepository unitOfWork, IWebHostEnvironment webHostEnvironment) { 
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            
        }
        public IActionResult Index()
        {
            List<Product> productsList = _unitOfWork.Product.GetAll("Category").ToList();
            return View(productsList);
        }

        public IActionResult Upsert(int? Id)
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
               .GetAll().Select(u => new SelectListItem
               {
                   Text = u.Name,
                   Value = u.Id.ToString()
               });

            // ViewBag.CategoryList = CategoryList;
            // ViewData["CategoryList"] = CategoryList;

            ProductVM productVM = new ()
            {
                CategoryList = CategoryList,
                Product = new Product(),
            };

            if(Id == 0 || Id == null)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == Id, "Category");
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM ProductVM, IFormFile? file)
        {
            if(ModelState.IsValid)
            {
                if (file != null)
                {
                    string rootPath = _webHostEnvironment.WebRootPath;
                    // set filename using guid
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    // get the path where we'll store image
                    string productPath = Path.Combine(rootPath, @"images\products");

                    if(!string.IsNullOrEmpty(ProductVM.Product.ImageUrl))
                    {
                        // delete old image
                        var oldImagePath = Path.Combine(rootPath, (ProductVM.Product.ImageUrl).TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }

                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductVM.Product.ImageUrl = @"\images\products\" + fileName; 
                }

                if(ProductVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(ProductVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(ProductVM.Product);

                }

                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");

            } else
            {
                ProductVM.CategoryList = _unitOfWork.Category.GetAll()
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });


                return View(ProductVM);
            }
        }

        /*
        [HttpGet]
        public IActionResult Edit(int? Id)
        {
            Product productFromDb = _unitOfWork.Product.Get(u => u.Id == Id);
            if (productFromDb == null)
            {
                return NotFound();
            }
            else
            {
                return View(productFromDb);

            }

        }

        [HttpPost]
        public IActionResult Edit(Product? product)
        {
            if(ModelState.IsValid)
            {
                _unitOfWork.Product.Update(product);
                TempData["success"] = "Product updated successfully";
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }  else
            {
                return View();
            }
        }

        
         
        public IActionResult Delete(int ? Id)
        {
            Product productFromDb = _unitOfWork.Product.Get(u => u.Id == Id);
            if(productFromDb == null)
            {
                return NotFound();
            }
            else
            {
               return View(productFromDb);
            }
        }

        [HttpPost]
        public IActionResult Delete(Product product)
        {
            if(product != null)
            {
                _unitOfWork.Product.Remove(product);
                TempData["success"] = "Product delete successfully";
                _unitOfWork.Save();
                return RedirectToAction("Index");
            } 
            else
            {
                return View();
            }

        }

        */

        #region API_CALLS

        public IActionResult GetAll()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll("Category");
            return Json(new {data = productList});
        }

        [HttpDelete]
        public IActionResult Delete(int? Id)
        {
            Product productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == Id);

            if(productToBeDeleted == null)
            {
                return Json(new { success = false, message="Record does not exist"});
            }
            else
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, (productToBeDeleted.ImageUrl).TrimStart('\\'));
                if(System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);

                }

                _unitOfWork.Product.Remove(productToBeDeleted);
                _unitOfWork.Save();

                return Json(new { success = true, message = "Record deleted successfully"});

            }

        }

        #endregion

    }
}
