using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace qlnetInterestRateSwap
{
    using QLNet;
    class Program
    {
        static void Main(string[] args)
        {
            double nominal = 575000000;

            Date _marketDate;
            Date _settlementDate;
            Dictionary<string, double> _depositRates;
            Dictionary<string, double> _swapRates;
            List<RateHelper> _rateHelpers;
            Calendar _calendar = new TARGET();
            int _fixingDays = 2;

            _marketDate = new Date(new DateTime(2015, 12, 17));
            Settings.setEvaluationDate(_marketDate);

            _depositRates = new Dictionary<string, double>();
            _depositRates.Add("1M", 0.0045);
            _depositRates.Add("3M", 0.0070);
            _depositRates.Add("6M", 0.0090);

            _swapRates = new Dictionary<string, double>();
            _swapRates.Add("1Y", 0.0080);
            _swapRates.Add("2Y", 0.0109);
            _swapRates.Add("3Y", 0.0134);
            _swapRates.Add("4Y", 0.0153);
            _swapRates.Add("5Y", 0.0169);
            _swapRates.Add("7Y", 0.0193);
            _swapRates.Add("10Y", 0.0218);
            _swapRates.Add("30Y", 0.0262);

            _rateHelpers = new List<RateHelper>();
            foreach (var v in _depositRates)
            {
                SimpleQuote sq = new SimpleQuote(v.Value);
                _rateHelpers.Add(new DepositRateHelper(new Handle<Quote>(sq), new Period(v.Key),
                    _fixingDays, _calendar, BusinessDayConvention.ModifiedFollowing, true, new Actual360()));
            }
            foreach (var v in _swapRates)
            {
                SimpleQuote sq = new SimpleQuote(v.Value);
                _rateHelpers.Add(new SwapRateHelper(new Handle<Quote>(sq), new Period(v.Key),
                    _calendar, Frequency.Semiannual, BusinessDayConvention.Unadjusted, 
                    new Thirty360(Thirty360.Thirty360Convention.USA), new Euribor3M()));
            }

            _marketDate = _calendar.adjust(_marketDate);
            _settlementDate = _calendar.advance(_marketDate, _fixingDays, TimeUnit.Days);

            YieldTermStructure yieldTermStructure = new PiecewiseYieldCurve<Discount, LogLinear>(
                _settlementDate, _rateHelpers, new ActualActual(ActualActual.Convention.ISDA));

            RelinkableHandle<YieldTermStructure> yieldTermStructureHandle = new RelinkableHandle<YieldTermStructure>();

            
            Frequency fixedLegFrequency = Frequency.Semiannual;
            BusinessDayConvention fixedLegConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter fixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.USA);
            double fixedRate = 0.0144;

            Frequency floatLegFrequency = Frequency.Quarterly;
            BusinessDayConvention floatLegConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter floatLegDayCounter = new Actual360();
            IborIndex iborIndex = new Euribor3M(yieldTermStructureHandle);
            iborIndex.addFixing(new Date(18, Month.Aug, 2015), 0.0033285);
            iborIndex.addFixing(new Date(18, Month.Nov, 2015), 0.0036960);
            double floatSpread = 0.0;

            VanillaSwap.Type swapType = VanillaSwap.Type.Receiver;

            Date maturity = new Date(20, Month.Nov, 2018);
            Date effective = new Date(20, Month.Nov, 2013);
            Schedule fixedSchedule = new Schedule(effective, maturity, new Period(fixedLegFrequency), _calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, false);
            Schedule floatSchedule = new Schedule(effective, maturity, new Period(floatLegFrequency), _calendar, floatLegConvention, floatLegConvention, DateGeneration.Rule.Forward, false);
            
            VanillaSwap vanillaSwap = new VanillaSwap(swapType, nominal, fixedSchedule, fixedRate, fixedLegDayCounter, floatSchedule, iborIndex, floatSpread, floatLegDayCounter);

            InterestRate interestRate = new InterestRate(fixedRate, fixedLegDayCounter, Compounding.Simple, fixedLegFrequency);
            List<InterestRate> coupons = new List<InterestRate>();
            for (int i = 0; i < fixedSchedule.Count; i++)
                coupons.Add(interestRate);
            FixedRateBond fixedBond = new FixedRateBond(_fixingDays, nominal, fixedSchedule, coupons, BusinessDayConvention.ModifiedFollowing);
            FloatingRateBond floatBond = new FloatingRateBond(_fixingDays, nominal, floatSchedule, iborIndex, floatLegDayCounter);

            IPricingEngine bondPricingEngine = new DiscountingBondEngine(yieldTermStructureHandle);
            fixedBond.setPricingEngine(bondPricingEngine);
            floatBond.setPricingEngine(bondPricingEngine);

            IPricingEngine swapPricingEngine = new DiscountingSwapEngine(yieldTermStructureHandle);
            vanillaSwap.setPricingEngine(swapPricingEngine);

            yieldTermStructureHandle.linkTo(yieldTermStructure);

            double swapNPV = vanillaSwap.NPV();
            double swapFixedNPV = vanillaSwap.fixedLegNPV();
            double swapFloatNPV = vanillaSwap.floatingLegNPV();

            double bondFixedNPV = fixedBond.NPV();
            double bondFloatNPV = floatBond.NPV();

            int w = (swapType == VanillaSwap.Type.Receiver ? 1 : -1);
            double asBondsMarketValue = w * (bondFixedNPV - bondFloatNPV);
            double asBondsMarketValueNoAcc = w * (fixedBond.cleanPrice() - floatBond.cleanPrice()) / 100.0 * nominal;
            double asBondsAccruedInterest = asBondsMarketValue - asBondsMarketValueNoAcc;

            Console.WriteLine("Vanilla Swap Maket Value      : {0:N}", swapNPV);
            Console.WriteLine("As Bonds Market Value         : {0:N}", asBondsMarketValue);
            Console.WriteLine("As Bonds Market Value (no acc): {0:N}", asBondsMarketValueNoAcc);
            Console.WriteLine("As Bonds Accrued Interest     : {0:N}", asBondsAccruedInterest);

            Date rollDate = new Date(1, Month.Nov, 2015);
            double bondFixedCash = 0;
            foreach (CashFlow cf in fixedBond.cashflows())
            {
                if (cf.date() > rollDate & cf.date() <= _marketDate)
                    bondFixedCash += cf.amount();
            }
            double bondFloatCash = 0;
            foreach (CashFlow cf in floatBond.cashflows())
            {
                if (cf.date() > rollDate & cf.date() <= _marketDate)
                    bondFloatCash += cf.amount();
            }
            double asBondsCash = w * (bondFixedCash - bondFloatCash);
            Console.WriteLine("As Bonds Settled Cash         : {0:N}", asBondsCash);
        }
    }
}
