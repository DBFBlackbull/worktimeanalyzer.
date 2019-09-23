﻿using System;
using System.Collections.Generic;
using System.Linq;
using Impact.Business.Holiday;
using Impact.Core.Constants;
using Impact.Core.Interfaces;
using Impact.Core.Model;
using Impact.DataAccess.Timelog;
using TimeLog.TransactionalApi.SDK.ProjectManagementService;

namespace Impact.Business.Time
{
    public class TimeLogService : ITimeService
    {
        private readonly ITimeRepository _timeRepository;
        private readonly IHolidayService _holidayService;

        public TimeLogService(ITimeRepository timeRepository, IHolidayService holidayService)
        {
            _timeRepository = timeRepository;
            _holidayService = holidayService;
        }

        public Quarter GetQuarter(DateTime dateTime)
        {
            var quarter = new Quarter();

            var year = dateTime.Year;
            var month = dateTime.Month;

            DateTime fromDate;
            DateTime toDate;

            if (month < 4)
            {
                fromDate = new DateTime(year, 1, 1);
                toDate = new DateTime(year, 3, 31);
                quarter.Number = 1;
            }
            else if (month < 7)
            {
                fromDate = new DateTime(year, 4, 1);
                toDate = new DateTime(year, 6, 30);
                quarter.Number = 2;
            }
            else if (month < 10)
            {
                fromDate = new DateTime(year, 7, 1);
                toDate = new DateTime(year, 9, 30);
                quarter.Number = 3;
            }
            else
            {
                fromDate = new DateTime(year, 10, 1);
                toDate = new DateTime(year, 12, 31);
                quarter.Number = 4;
            }

            quarter.From = fromDate;
            quarter.To = toDate;

            return quarter;
        }

        public IEnumerable<Week> CategorizeWeeks(Quarter quarter, List<Week> rawWeeks, decimal normalWorkDay, SecurityToken token)
        {
            HandleWeek53(quarter, rawWeeks, token);
            _holidayService.AddHolidayHours(quarter, rawWeeks, normalWorkDay);
            rawWeeks.FirstOrDefault()?.AddQuarterEdgeHours(quarter, normalWorkDay);

            if (rawWeeks.Count > 1)
                rawWeeks.LastOrDefault()?.AddQuarterEdgeHours(quarter, normalWorkDay);

            rawWeeks.ForEach(week => week.CategorizeHours(normalWorkDay * 5));
            return rawWeeks;
        }

        /// <summary>
        /// Fix for week 53. It is ignored at the end of the year, and added to beginning of the next year.
        /// </summary>
        /// <param name="quarter"></param>
        /// <param name="rawWeeks"></param>
        /// <param name="token"></param>
        private void HandleWeek53(Quarter quarter, List<Week> rawWeeks, SecurityToken token)
        {
            rawWeeks.RemoveAll(w => w.Number == 53);

            var firstWeek = rawWeeks.FirstOrDefault();
            if (quarter.Number != 1 || firstWeek == null)
                return;
            
            var firstDate = firstWeek.Dates.FirstOrDefault();
            if (ApplicationConstants.GetWeekNumber(firstDate) != 53)
                return;
            
            var week53Quarter = new Quarter
            {
                From = firstDate,
                To = new DateTime(firstDate.Year, 12, 31)
            };
            var week53 = _timeRepository.GetRawWeeksInQuarter(week53Quarter, token).FirstOrDefault();
            if (week53 != null)
                firstWeek.TotalHours += week53.TotalHours;
        }

        public IEnumerable<Week> GetNormalizedWeeks(List<Week> weeksList, decimal normalWorkWeek)
        {
            var weeks = weeksList.ConvertAll(w => w.Clone());

            var lowWeeks = weeks.Where(w => w.WorkHours + w.HolidayHours + w.QuarterEdgeHours < normalWorkWeek);
            var movableWeeks = weeks.Where(w => w.MovableOvertimeHours > 0).ToList();
            MoveHours(lowWeeks, movableWeeks, "MovableOvertimeHours");
            
            lowWeeks = weeks.Where(w => w.WorkHours + w.HolidayHours + w.QuarterEdgeHours < normalWorkWeek);
            var interestWeeks = weeks.Where(w => w.InterestHours > 0).ToList();
            MoveHours(lowWeeks, interestWeeks, "InterestHours");
            
            return weeks;
        }

        public IEnumerable<Month> GetNormalizedMonths(List<Month> monthsList)
        {
            var months = monthsList.ConvertAll(m => m.Clone());

            List<Month> lowAwesomeMonths = months
                .Where(m => m.Date < ApplicationConstants.RAndDStartDate)
                .Where(m => m.AwesomeThursdayHours + m.RAndDHours < ApplicationConstants.AwesomeThursdayApproximation)
                .ToList();
            List<Month> highAwesomeMonths = months
                .Where(m => m.Date < ApplicationConstants.RAndDStartDate)
                .Where(m => m.AwesomeThursdayHours + m.RAndDHours > ApplicationConstants.AwesomeThursdayApproximation)
                .ToList();
            MoveHours(lowAwesomeMonths, highAwesomeMonths, null);
            
            List<Month> lowRAndDMonths = months
                .Where(m => m.AwesomeThursdayHours + m.RAndDHours < ApplicationConstants.AwesomeThursdayApproximation)
                .ToList();
            List<Month> highRAndDMonths = months
                .Where(m => m.AwesomeThursdayHours + m.RAndDHours > ApplicationConstants.AwesomeThursdayApproximation)
                .ToList();
            MoveHours(lowRAndDMonths, highRAndDMonths, null);
            
            return months;
        }

        public VacationYear GetVacationYear(DateTime date)
        {
            DateTime start;
            DateTime end;

            var year = date.Year;

            
            if (ApplicationConstants.MiniVacationStart <= date && date <= ApplicationConstants.MiniVacationEnd)
            {
                return new VacationYear(ApplicationConstants.MiniVacationStart, ApplicationConstants.MiniVacationEnd)
                {
                    TotalVacationDays = 16.64m,
                    TotalExtraVacationDays = 3.33m
                };
            }
            if (date < new DateTime(2020, 9, 1)) //Old vacation year
            {
                if (date.Month < 5) // before May, use last years vacation calendar
                    year -= 1;
            
                start = new DateTime(year, 5, 1);
                end = new DateTime(year + 1, 4, 30);
            } 
            else //New vacation year. 16 months period to spend, up from 12 months
            {
                if (date.Month < 9)
                    year -= 1;
            
                start = new DateTime(year, 9, 1);
                end = new DateTime(year + 1, 12, 31);
            }

            return new VacationYear(start, end);
        }

        private static void MoveHours<T>(IEnumerable<IAbsorbable<T>> lowHoursElements, List<T> movableHoursElements, string propertyName)
        {
            foreach (var lowHoursElement in lowHoursElements)
            {
                var elementsAbsorbed = 0;
                foreach (var movableHoursElement in movableHoursElements)
                {
                    var doneAbsorbing = lowHoursElement.AbsorbHours(movableHoursElement, propertyName);
                    if (doneAbsorbing)
                        break;
                    elementsAbsorbed++;
                }
                movableHoursElements.RemoveRange(0, elementsAbsorbed);
            }
        }
    }
}
