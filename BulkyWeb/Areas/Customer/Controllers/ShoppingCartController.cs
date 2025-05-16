using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Common;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IUnitOfWorkRepository _unitOfWork;
        [BindProperty]
        public ShoppingCartVM shoppingCartVM { get; set; }
        public ShoppingCartController(IUnitOfWorkRepository unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the Id of logged in user

            ShoppingCartVM shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            
            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.price = GetPriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);
            }

            return View(shoppingCartVM);
        }

        public IActionResult Plus(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count <= 1)
            {
                // Remove
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb != null)
            {
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count());
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return NotFound();
            }
            
        }

        [HttpGet]
        public IActionResult Summary()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the Id of logged in user

            ShoppingCartVM shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id ==  userId);
            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.price = GetPriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);

            }

            return View(shoppingCartVM);
        } 

        [HttpPost]
        public IActionResult SummaryPost()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the Id of logged in user
            shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
            
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;
            shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u=>u.Id == userId);


            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.price = GetPriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);

            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // it is regular customer account and we need to capture payment
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            }
            else 
            {
                // it is a companyy account
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }

            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.price,
                    Count = cart.Count,
                };

                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/ShoppingCart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl =   domain + "customer/ShoppingCart/Index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in shoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long) item.price * 100,
                            Currency = "USD",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title,
                            }
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new Stripe.Checkout.SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentID(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
           
             return RedirectToAction(nameof(OrderConfirmation), new {id = shoppingCartVM.OrderHeader.Id });
        }

        [HttpGet]
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);


                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.Save();
                }
            }

            List<ShoppingCart> shoppingCartItems = _unitOfWork.ShoppingCart.GetAll(u=> u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCartItems);
            _unitOfWork.Save();

            return View(id);
       
        }

        private double GetPriceBasedOnQuantity(ShoppingCart cart)
        {
            if(cart.Count <= 50) // less than 50 price
            {
                return cart.Product.Price;
            }

            else
            {
                if( cart.Count > 50 && cart.Count <= 100) // between 50 - 100 price
                {
                    return cart.Product.Price50;
                }

                else
                {
                    return cart.Product.Price100; // price for greater than 100
                }
            }
        }
    }
}
