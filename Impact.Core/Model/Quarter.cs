﻿using System;

namespace Impact.Core.Model
{
    public class Quarter
    {
        public int Number { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string GetDisplayTitle()
        {
            switch (Number)
            {
                case 1:
                    return "1. Kvartal";
                case 2:
                    return "2. Kvartal";
                case 3:
                    return "3. Kvartal";
                case 4:
                    return "4. Kvartal";
                default:
                    throw new IndexOutOfRangeException("Quarter was now 1, 2, 3, or 4. Real value: " + Number);
            }
        }

        public string GetDisplayMonths()
        {
            switch (Number)
            {
                case 1:
                    return "Januar til Marts";
                case 2:
                    return "April til Juni";
                case 3:
                    return "Juli til September";
                case 4:
                    return "Oktober til December";
                default:
                    throw new IndexOutOfRangeException("Quarter was now 1, 2, 3, or 4. Real value: " + Number);
            }
        }
        
        public string GetDisplayOvertimePayoutMonth()
        {
            // The payout month got changed at this specific date. See "PdfRegistreringer/Fortroligt Information vedr udbetalingstidspunkt for overarbejde.pdf"
            if (From < new DateTime(2017, 8, 15))
            {
                switch (Number)
                {
                    case 1:
                        return $"April {From.Year}";
                    case 2:
                        return $"Juli {From.Year}";
                    case 3:
                        return $"Oktober {From.Year}";
                    case 4:
                        return $"Februar {From.Year + 1}";
                    default:
                        throw new IndexOutOfRangeException("Quarter was now 1, 2, 3, or 4. Real value: " + Number);
                }
            }
            switch (Number)
            {
                case 1:
                    return $"Maj {From.Year}";
                case 2:
                    return $"August {From.Year}";
                case 3:
                    return $"November {From.Year}";
                case 4:
                    return $"Februar eller Marts {From.Year + 1}";
                default:
                    throw new IndexOutOfRangeException("Quarter was now 1, 2, 3, or 4. Real value: " + Number);
            }
        }

        private bool Equals(Quarter other)
        {
            return Number == other.Number && From.Equals(other.From) && To.Equals(other.To);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Quarter) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Number;
                hashCode = (hashCode * 397) ^ From.GetHashCode();
                hashCode = (hashCode * 397) ^ To.GetHashCode();
                return hashCode;
            }
        }
    }
}
