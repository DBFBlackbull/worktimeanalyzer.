﻿using System;
using System.Collections.Generic;
using System.Linq;
using Impact.Core.Model;
using TimeLog.TransactionalApi.SDK.ProjectManagementService;

namespace Impact.DataAccess.Strategies
{
    public class AddMonthStrategy : IAddRegistrationStrategy<Month>
    {
        private Dictionary<DateTime, Month> Months { get; }

        public AddMonthStrategy()
        {
            Months = new Dictionary<DateTime, Month>();
        }

        public void AddRegistration(WorkUnitFlat registration)
        {
            var dateTime = new DateTime(registration.Date.Year, registration.Date.Month, 1);
            if (!Months.TryGetValue(dateTime, out var month))
                Months[dateTime] = month = new Month(dateTime);

            switch (registration.TaskName)
            {
                case "Fed torsdag":
                case "PL Fed Torsdag":
                    month.AwesomeThursdayRawHours += registration.Hours;
                    break;
                case "R&D":
                    month.RAndDRawHours = registration.Hours;
                    break;
                default:
                    return;
            }
        }

        public IEnumerable<Month> GetList()
        {
            var months = Months.Values.ToList();
            months.ForEach(m => m.ConvertToDecimal());
            return months.OrderBy(m => m.Date);
        }
    }
}