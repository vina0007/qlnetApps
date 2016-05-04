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
             *  4. replace cash flows with expected cash flows to get the correct price 
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
            double netCoupon = 0.035;
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

            double cp = mbs.cleanPrice();

            Console.WriteLine("clean price: {0:F6}", mbs.cleanPrice());
            Console.WriteLine("dirty price: {0:F6}", mbs.dirtyPrice());
            Console.WriteLine("accrued int: {0:F6}", mbs.accruedAmount());

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
