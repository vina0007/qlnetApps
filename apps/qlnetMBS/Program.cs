using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace qlnetMBS
{
    using QLNet;

    class Program
    {
        static void Main(string[] args)
        {
            int settlementDays = 2;
            Calendar calendar = new TARGET();
            int origTerm = 360;
            Frequency sinkingFrequency = Frequency.Monthly;
            DayCounter accrualDayCounter = new Thirty360();

            double wac = 4.125;
            int wala;
            int wam = 324;
            Date factorDate = new Date(2016, 05, 01);
            double factor;
            //double originalFace;
            double currentFace = 1000000;
            int statedDelay;
            double netCoupon = 3.5;
            string secType;
            Date settleDate;

            double yield_be;
            //double price;
            
            double cpr = 0.08;

            IPrepayModel iPrepayModel = new ConstantCPR(cpr);

            MBSFixedRateBond mbs = new MBSFixedRateBond(settlementDays, calendar, currentFace, factorDate,
                new Period(wam, TimeUnit.Months), new Period(origTerm, TimeUnit.Months), sinkingFrequency, wac, netCoupon, accrualDayCounter, iPrepayModel);

        }
    }
}
