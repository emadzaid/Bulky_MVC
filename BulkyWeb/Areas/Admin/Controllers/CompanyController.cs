using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWorkRepository _unitOfWork;
        public CompanyController(IUnitOfWorkRepository unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> companyList = _unitOfWork.Company.GetAll().ToList();
            return View(companyList);
        }

        public IActionResult Upsert(int? Id)
        {
            Company company = new Company();
            if (Id == null || Id == 0)
            {
                return View(company);
            }
            else
            {
                Company companyFromDb = _unitOfWork.Company.Get(u => u.Id == Id);
                return View(companyFromDb);
            }
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                }

                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");

            }

            return View(company);
        }

        #region API_CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data = companyList});
        }

        [HttpDelete]

        public IActionResult Delete(int id)
        {
            Company companyExist = _unitOfWork.Company.Get(u => u.Id == id);
            if(companyExist == null)
            {
                return Json(new {success = false, message = "Company not found in DB"});
            }
            else
            {
                _unitOfWork.Company.Remove(companyExist);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Company deleted successfully"});

            }
        }
        #endregion
    }

}

