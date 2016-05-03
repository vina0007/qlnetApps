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
            /*
             * TODO:
             *  1. Add delay
             *  2. Add SecType enum PT, PO, IO
             *  3. WAC vs Net Coupon (meanings are reversed)
             * 
             */

            Date referenceDate = new Date(12, 11, 2015);
            Settings.setEvaluationDate(referenceDate);
            int settlementDays = 0;
            Calendar calendar = new TARGET();
            int origTerm = 360;
            Frequency sinkingFrequency = Frequency.Monthly;
            DayCounter accrualDayCounter = new Thirty360();
            BusinessDayConvention paymentConvention = BusinessDayConvention.Unadjusted;


            double wac = 0.035;
            int wam = 354;
            int wala = origTerm - wam;
            Date factorDate = new Date(1, 11, 2015);
            Date issueDate = calendar.advance(factorDate, -wala, TimeUnit.Months, BusinessDayConvention.Unadjusted);
            double factor = 1.0;
            double currentFace = 1000000;
            double originalFace = currentFace / factor;
            int statedDelay = 0; //54;
            double netCoupon = 0.04;
            string secType;
            Date settleDate;

            double yield_be = 0.035;
            //double price;
            
            double cpr = 0.07;
            double psa = 1;

            IPrepayModel constantcpr = new ConstantCPR(cpr);
            IPrepayModel psacurve = new PSACurve(factorDate, psa);

            MBSFixedRateBond mbs = new MBSFixedRateBond(
                settlementDays, 
                calendar, 
                currentFace, 
                factorDate,
                new Period(wam, TimeUnit.Months), 
                new Period(origTerm, TimeUnit.Months), 
                sinkingFrequency,
                wac,
                netCoupon,
                accrualDayCounter,
                psacurve,
                paymentConvention,
                issueDate);

            YieldTermStructure discountCurve = new FlatForward(referenceDate, yield_be, new Thirty360(), Compounding.Compounded, Frequency.Semiannual);

            DiscountingBondEngine discountingBondEngine = new DiscountingBondEngine(new Handle<YieldTermStructure>(discountCurve));

            mbs.setPricingEngine(discountingBondEngine);

            Console.WriteLine("clean price: {0:F6}", mbs.cleanPrice());
            Console.WriteLine("dirty price: {0:F6}", mbs.dirtyPrice());
            Console.WriteLine("accrued int: {0:F6}", mbs.accruedAmount());

            Console.WriteLine(discountCurve.discount(new Date(1, 12, 2015)));
            Console.WriteLine(discountCurve.discount(new Date(1, 1, 2016)));
            Console.WriteLine(discountCurve.discount(new Date(1, 2, 2016)));
            Console.WriteLine(discountCurve.discount(new Date(1, 3, 2016)));

            foreach (CashFlow cf in mbs.expectedCashflows())
            {
                if (cf.GetType() == typeof(VoluntaryPrepay))
                    Console.WriteLine("upmt: {0:F2} --> {1:mm/dd/yyy}", cf.amount(), cf.date());
                if (cf.GetType() == typeof(AmortizingPayment))
                    Console.WriteLine("ppmt: {0:F2} --> {1:mm/dd/yyy}", cf.amount(), cf.date());
                if (cf.GetType() == typeof(FixedRateCoupon))
                    Console.WriteLine("ipmt: {0:F2} --> {1:mm/dd/yyy}", cf.amount(), cf.date());
            }

        }
    }
}
