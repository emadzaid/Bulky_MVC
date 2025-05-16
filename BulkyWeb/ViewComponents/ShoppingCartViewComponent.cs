using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWorkRepository _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWorkRepository unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                if(HttpContext.Session.GetInt32(SD.SessionCart) != null)
                {
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
                else
                {
                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count());
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
               
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }  
    }
}
