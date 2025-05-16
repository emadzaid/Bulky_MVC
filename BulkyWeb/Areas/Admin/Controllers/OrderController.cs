using System;
using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWorkRepository _unitOfWork;
        public OrderController(IUnitOfWorkRepository unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Details(int orderId)
        {

            OrderVM orderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            }; 
        
            return View(orderVM);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails(OrderVM orderVM)
        {
            OrderHeader orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
            if(!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }


            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(OrderVM orderVM)
        {
            OrderHeader orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder(OrderVM orderVM)
        {
            OrderHeader orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;

            if(orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(OrderVM orderVM)
        {
            OrderHeader orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            if(orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                // Initiate refund
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.PaymentStatusRefunded);
            }
            else
            {
                // No refund
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);

            }

            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult PayNow(OrderVM orderVM)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            IEnumerable<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(includeProperties: "Product");

            orderVM.OrderHeader = orderHeader;
            orderVM.OrderDetails = orderDetails;


            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {   
                SuccessUrl = domain+$"admin/order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domain+$"admin/details?orderId={orderVM.OrderHeader.Id}",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)item.Price * 100,
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
            _unitOfWork.OrderHeader.UpdateStripePaymentID(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }


        [HttpGet]
        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);


                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.Save();
                }
            }

          
            return View(orderHeaderId);

        }

        #region API_CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;


            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the Id of logged in user
                orderHeaders = _unitOfWork.OrderHeader.GetAll( u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }


            switch (status)
            {
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new { data = orderHeaders });
        }
    }
    #endregion


}
