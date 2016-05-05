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
             *  FIXES
             *      1. WAC vs Net Coupon (meanings are reversed, names are bad)
             *      2. CashFlows needs to be replaced with Expected CashFlows to get the correct price
             *  NEW IMPLEMENTATION     
             *      1. Add delay
             *      2. Add SecType enum PT, PO, IO
             */

            Date referenceDate = new Date(16, 11, 2015);
            Settings.setEvaluationDate(referenceDate);
            int settlementDays = 0;
            Calendar calendar = new TARGET();
            int origTerm = 360;
            Frequency sinkingFrequency = Frequency.Monthly;
            DayCounter accrualDayCounter = new Thirty360();
            BusinessDayConvention paymentConvention = BusinessDayConvention.Unadjusted;


            double wac = 0.03875;
            int wam = 357;
            int wala = origTerm - wam;
            Date factorDate = new Date(1, 12, 2015);
            Date issueDate = calendar.advance(factorDate, -wala, TimeUnit.Months, BusinessDayConvention.Unadjusted);
            double factor = 1.0;
            double currentFace = 1000000;
            double originalFace = currentFace / factor;
            int statedDelay = 30; //54;
            double netCoupon = 0.030;
            string secType = "PT";
            Date settleDate = referenceDate;

            double yield_be = 0.0270;
            //double price;
            
            
            double speed = 0.08;

            IPrepayModel prepaymodel = new ConstantCPR(speed);
            //IPrepayModel prepaymodel = new PSACurve(factorDate, speed);

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
                prepaymodel,
                paymentConvention,
                issueDate);

            YieldTermStructure discountCurve = new FlatForward(referenceDate, yield_be, new Thirty360(), Compounding.Compounded, Frequency.Semiannual);

            DiscountingBondEngine discountingBondEngine = new DiscountingBondEngine(new Handle<YieldTermStructure>(discountCurve));

            mbs.setPricingEngine(discountingBondEngine);

            // display results
            Console.WriteLine("WAC         : {0:F5}", wac);
            Console.WriteLine("WALA        : {0}", wala);
            Console.WriteLine("WAM         : {0}", wam);
            Console.WriteLine("Factor Date : {0}", factorDate.ToShortDateString());
            Console.WriteLine("Factor      : {0:F10}", factor);
            Console.WriteLine("Orig Face   : {0:N}", originalFace);
            Console.WriteLine("Curr Face   : {0:N}", currentFace);
            Console.WriteLine("Stated Delay: {0}", statedDelay);
            Console.WriteLine("Net Coupon  : {0:F3}", netCoupon);
            Console.WriteLine("Sec Type    : {0}", secType);
            Console.WriteLine("Settle Date : {0}", settleDate.ToShortDateString());
            Console.WriteLine("Model Type  : {0}", prepaymodel.GetType().ToString());
            Console.WriteLine("Model Speed : {0:F3}", speed);
            Console.WriteLine("Yield       : {0:F5}", yield_be);

            Console.WriteLine("Clean Price : {0:F6}", mbs.cleanPrice());
            Console.WriteLine("Dirty Price : {0:F6}", mbs.dirtyPrice());
            Console.WriteLine("Accrued     : {0:F6}", mbs.accruedAmount());

            // month, factor, pay date, ending prin, interest, reg principal, prepaid principal, total principal, net flow, cpr, smm, wala, wam, p&i payment, i payment, beg balance, days, discount, pv
            double ebal = currentFace;

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("output.csv"))
            {
                DayCounter dc = discountCurve.dayCounter();
                Date refdate = discountCurve.referenceDate();

                 

                sw.WriteLine("month,factor date,factor,pay date,ending principal,interest,regular principal,prepaid principal,total principal,net flow,cpr,smm,wala,wam,p&i payment,interest payment,beginning balance,days,discount,pv");
                for (int i = 0; i < wam; i++)
                {
                    double upmt = 0;
                    double ppmt = 0;
                    double ipmt = 0;
                    double bbal = ebal;

                    Date paydate = null;

                    for (int j = 0; j <= 2; j++)
                    {
                        int k = i * 3 + j;
                        CashFlow cf = mbs.expectedCashflows()[k];
                        if (cf.GetType() == typeof(VoluntaryPrepay))
                        {
                            upmt = cf.amount();
                            paydate = cf.date();
                        }
                        if (cf.GetType() == typeof(AmortizingPayment))
                            ppmt = cf.amount();
                        if (cf.GetType() == typeof(FixedRateCoupon))
                            ipmt = cf.amount();
                    }
                    int days = dc.dayCount(refdate, paydate);
                    double df = discountCurve.discount(paydate);
                    ebal = bbal - upmt - ppmt;
                    double smm = upmt / (bbal - ppmt);

                    sw.Write("{0},", i + 1); //month
                    sw.Write("{0},", calendar.advance(factorDate, i,TimeUnit.Months,BusinessDayConvention.Unadjusted)); //factor date
                    sw.Write("{0:F10},", factor * bbal / currentFace); //factor
                    sw.Write("{0},", paydate.ToShortDateString()); //pay date
                    sw.Write("{0:F2},", ebal); //ending principal
                    sw.Write("{0:F2},", ipmt); //interest
                    sw.Write("{0:F2},", ppmt); //regular principal
                    sw.Write("{0:F2},", upmt); //prepaid principal
                    sw.Write("{0:F2},", ppmt + upmt); //total principal
                    sw.Write("{0:F2},", ipmt + ppmt + upmt); //net flow
                    sw.Write("{0:F4},", 1 - Math.Pow(1 - smm, 12)); //cpr
                    sw.Write("{0:F6},", smm); //smm
                    sw.Write("{0},", wala + i); //wala
                    sw.Write("{0},", wam - i); //wam
                    sw.Write("{0},", i); //p&i payment
                    sw.Write("{0},", i); //interest payment
                    sw.Write("{0:F2},", bbal); //beginning balance
                    sw.Write("{0},", days); //days
                    sw.Write("{0:F8},", df); //discount
                    sw.WriteLine("{0}", df * (ipmt + ppmt + upmt)); //pv
                }
            } 
        }
    }
}
