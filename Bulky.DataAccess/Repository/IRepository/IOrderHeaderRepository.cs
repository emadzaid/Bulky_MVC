﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.Models;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        public void Update(OrderHeader orderHeader);
        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null);
        public void UpdateStripePaymentID(int id, string sessionId, string paymentIntentId);
    }
}
