﻿using System;
using System.Collections.Generic;
using Impact.Core.Model;
using TimeLog.TransactionalApi.SDK.ProjectManagementService;

namespace Impact.Business.Time
{
	public interface ITimeService
	{
	    Quarter GetQuarter(DateTime dateTime);
	    IEnumerable<Week> CategorizeWeeks(Quarter quarter, List<Week> rawWeeks, decimal normalWorkDay, SecurityToken token);
	    IEnumerable<Week> GetNormalizedWeeks(List<Week> weeksList, decimal normalWorkWeek);
	    IEnumerable<Month> GetNormalizedMonths(List<Month> monthsList);
	    VacationYear GetVacationYear(DateTime date);
	}
}
