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
            Date referenceDate = new Date(29, 4, 2016);
            int settlementDays = 2;
            Calendar calendar = new TARGET();
            int origTerm = 360;
            Frequency sinkingFrequency = Frequency.Monthly;
            DayCounter accrualDayCounter = new Thirty360();

            double wac = 0.04;
            int wala;
            int wam = 324;
            Date factorDate = new Date(1, 5, 2016);
            double factor;
            //double originalFace;
            double currentFace = 1000000;
            int statedDelay;
            double netCoupon = 0.035;
            string secType;
            Date settleDate;

            double yield_be = 0.027;
            //double price;
            
            double cpr = 0.08;

            IPrepayModel iPrepayModel = new ConstantCPR(cpr);

            MBSFixedRateBond mbs = new MBSFixedRateBond(settlementDays, calendar, currentFace, factorDate,
                new Period(wam, TimeUnit.Months), new Period(origTerm, TimeUnit.Months), sinkingFrequency, wac, netCoupon, accrualDayCounter, iPrepayModel);

            YieldTermStructure discountCurve = new FlatForward(referenceDate, yield_be, new Thirty360(), Compounding.Compounded, Frequency.Semiannual);

            DiscountingBondEngine discountingBondEngine = new DiscountingBondEngine(new Handle<YieldTermStructure>(discountCurve));

            mbs.setPricingEngine(discountingBondEngine);

            Console.WriteLine(mbs.cleanPrice());

        }
    }
}
