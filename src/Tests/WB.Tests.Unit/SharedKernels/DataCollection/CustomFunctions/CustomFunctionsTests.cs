﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.DataCollection.V2.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.V5.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage.CustomFunctions;
using Extensions = WB.Core.SharedKernels.DataCollection.V2.CustomFunctions.Extensions;

namespace WB.Tests.Unit.SharedKernels.DataCollection.CustomFunctions
{
    [TestFixture]
    internal class CustomFunctionsTests
    {
        // For all methods using params[], the indication is that 16383 is the limit:
        // http://stackoverflow.com/questions/12658883/what-is-the-maximum-number-of-parameters-that-a-c-sharp-method-can-be-defined-as

        private int[] _mc123;
        private int[] _mc3;

        [SetUp]
        public void Init()
        {
            _mc123 = new int[] { 1, 2, 3 };
            _mc3 = new int[] { 3 };
        }

        #region Tests

        [Test]
        public void Test_InRange()
        {
            Assert.IsTrue(1.InRange(0, 10));
            Assert.IsFalse(1.InRange(3, 10));
            Assert.IsFalse(13.InRange(3, 10));

            Assert.IsTrue( ((double?)1d).InRange(0.0, 10.0));
            Assert.IsFalse(((double?)1d).InRange(3.0, 10.0));
            Assert.IsFalse(((double?)13d).InRange(3.0, 10.0));

            decimal? ten = 10;
            decimal? three = 3;
            decimal? one = 1;
            decimal? zero = 0;
            decimal? thirteen = 13;

            Assert.IsTrue(one.InRange(zero, ten));
            Assert.IsFalse(one.InRange(three, ten));
            Assert.IsFalse(thirteen.InRange(three, ten));

            Assert.IsTrue(one.InRange(zero, ten));
            Assert.IsFalse(one.InRange(three, ten));
            Assert.IsFalse(thirteen.InRange(three, ten));

            Assert.IsFalse(((int?)null).InRange(0, 10));
            Assert.IsFalse(((int?)1).InRange(null, 10));
            Assert.IsFalse(((int?)1).InRange(2, (int?)null));
            
            Assert.IsFalse(((long?)null).InRange(0, 10));
            Assert.IsFalse(((long?)1).InRange(null, 10));
            Assert.IsFalse(((long?)1).InRange(2, (int?)null));

            Assert.IsFalse(((double?)null).InRange(0, 10));
            Assert.IsFalse(((double?)1).InRange(null, 10));
            Assert.IsFalse(((double?)1).InRange(2, (int?)null));
            
            Assert.IsFalse(((decimal?)null).InRange(0, 10));
            Assert.IsFalse(((decimal?)1).InRange(null, 10));
            Assert.IsFalse(((decimal?)1).InRange(2, (int?)null));
            
            
            Assert.IsFalse(((long?)11).InRange(0, 10));
            Assert.IsFalse(((double?)11).InRange(0, 10));
        }
        
        [Test]
        public void Test_InRangeDate()
        {
            DateTime? date1 = new DateTime(2001, 1, 1);
            DateTime? date2 = new DateTime(2002, 2, 2);
            DateTime? date3 = new DateTime(2003, 3, 3);

            Assert.IsTrue(date2.InRange(date1, date3));
            Assert.IsFalse(date1.InRange(date2, date3));
            Assert.IsFalse(date3.InRange(date1, date2));

            DateTime? date0 = null;
            Assert.IsFalse(date0.InRange(date1, date3));
            Assert.IsFalse(date2.InRange(null, date3));
            Assert.IsFalse(date2.InRange(date1, null));
        }


        [Test]
        public void Test_InList()
        {
            long? d = 2;
            Assert.IsFalse(d.InList());
            Assert.IsTrue(Extensions.InList(1, 1, 2, 3, 4));
            Assert.IsFalse(Extensions.InList(0, 1, 2, 3, 4));
            Assert.IsFalse(Extensions.InList(null, 1, 2, 3, 4));
            Assert.IsTrue(Extensions.InList(null, 1, 2, 3, 4, null));
        }

        [Test]
        public void Test_InListStr()
        {
            string name = "Washington";
            Assert.IsTrue(name.InList("Jackson", "Washington", "Bush"));
            Assert.IsFalse(name.InList("Jackson", "Clinton", "Bush"));
            Assert.IsFalse(String.Empty.InList("Jackson", "Clinton", "Bush"));
            Assert.IsFalse(name.InList());
        }

        [Test]
        public void Test_InListDouble()
        {
            double? d = 2.0;
            Assert.IsFalse(d.InList());
            Assert.IsTrue(Extensions.InList(1.0, 1.0, 2.0, 3.0, 4.0));
            Assert.IsFalse(Extensions.InList(0.0, 1.0, 2.0, 3.0, 4.0));
            Assert.IsFalse(Extensions.InList(null, 1.0, 2.0, 3.0, 4.0));
            Assert.IsTrue(Extensions.InList(null, 1.0, 2.0, 3.0, 4.0, null));
        }

        [Test]
        public void Test_InListDecimal()
        {
            decimal? d = 2;
            decimal? zero = 0;
            decimal? one = 1;
            decimal? two = 2;
            decimal? three = 3;
            decimal? four = 4;
            Assert.IsFalse(d.InList());
            Assert.IsTrue(Extensions.InList(one, one, two, three, four));
            Assert.IsFalse(Extensions.InList(zero, one, two, three, four));
            Assert.IsFalse(Extensions.InList(null, one, two, three, four));
            Assert.IsTrue(Extensions.InList(null, one, two, three, four, null));
        }

        [Test]
        public void Test_ContainsAny()
        {
            Assert.IsTrue(_mc123.ContainsAny(2, 0));
            Assert.IsFalse(_mc123.ContainsAny(5, 10));

            Assert.IsTrue(_mc123.ContainsAny(2));

            Assert.IsTrue(_mc123.ContainsAny(null));
            Assert.IsTrue(_mc123.ContainsAny(new int[0]));

            decimal[] empty = null;
            Assert.IsFalse(empty.ContainsAny(2));
            Assert.IsFalse(empty.ContainsAny(null));

            empty = new decimal[0];
            Assert.IsFalse(empty.ContainsAny(2));
        }

        [Test]
        public void Test_ContainsOnly()
        {
            Assert.IsFalse(_mc123.ContainsOnly(1));   // False because also contains 2 and 3
            Assert.IsFalse(_mc123.ContainsOnly(2));   // False because also contains 1 and 3
            Assert.IsFalse(_mc123.ContainsOnly(3));   // False because also contains 1 and 2
            Assert.IsFalse(_mc123.ContainsOnly(1, 2));   // False because also contains 3
            Assert.IsTrue(_mc123.ContainsOnly(1, 2, 3)); // True because contains each of the items and no other items.

            Assert.IsFalse(_mc3.ContainsOnly(1)); // False because contains a different item
            Assert.IsTrue(_mc3.ContainsOnly(3)); // True because contains exactly this item

            decimal[] empty = null;
            Assert.IsFalse(empty.ContainsOnly(2));     // nothing does not contain 2
            //Assert.IsFalse(empty.ContainsOnly(null));       // nothing consists of nothing
        }

        [Test]
        public void Test_ContainsAll()
        {
            Assert.IsTrue(_mc123.ContainsAll(1));
            Assert.IsTrue(_mc123.ContainsAll(2));
            Assert.IsTrue(_mc123.ContainsAll(3));

            Assert.IsTrue(_mc123.ContainsAll(1, 2));
            Assert.IsTrue(_mc123.ContainsAll(1, 3));
            Assert.IsTrue(_mc123.ContainsAll(2, 3));

            Assert.IsTrue(_mc123.ContainsAll(1, 2, 3));

            // Order does not matter!
            Assert.IsTrue(_mc123.ContainsAll(2, 1));
            Assert.IsTrue(_mc123.ContainsAll(3, 1));
            Assert.IsTrue(_mc123.ContainsAll(3, 2));

            Assert.IsTrue(_mc123.ContainsAll(3, 2, 1));

            Assert.IsFalse(_mc123.ContainsAll(3, 9));

            Assert.IsTrue(_mc123.ContainsAll((decimal[])null));
            Assert.IsTrue(_mc123.ContainsAll((int[])null));
            
            Assert.IsTrue(_mc123.ContainsAll(new decimal[0]));


            decimal[] empty = null;
            Assert.IsFalse(empty.ContainsAll(1));

            decimal[] empty2 = new decimal[0];
            Assert.IsFalse(empty2.ContainsAll(1));
        }


        [Test]
        public void Test_IsNoneOf()
        {
            decimal? educ = 4;
            Assert.IsTrue(educ.IsNoneOf(2, 3, 11)); // bacause value of educ is not blacklisted
            Assert.IsFalse(educ.IsNoneOf(2, 3, 4)); // because 4 is blacklisted but is the current value of educ
            Assert.IsTrue(educ.IsNoneOf());         // because the blacklist is empty
            Assert.IsTrue(educ.IsNoneOf(null));

            decimal? none = null;
            Assert.IsTrue(none.IsNoneOf(2, 3, 4));
        }

        [Test]
        public void Test_CountValue()
        {
            decimal? q1 = 2;
            decimal? q2 = 3;
            decimal? q3 = 1;
            decimal? q4 = 2;
            decimal? q5 = 2;
            decimal? q6 = 3;
            decimal? q7 = 2;
            decimal? q8 = null;


            Assert.AreEqual(1, new AbstractConditionalLevelInstanceFunctions().CountValue(1, q1, q2, q3, q4, q5, q6, q7));
            Assert.AreEqual(4, new AbstractConditionalLevelInstanceFunctions().CountValue(2, q1, q2, q3, q4, q5, q6, q7));
            Assert.AreEqual(2, new AbstractConditionalLevelInstanceFunctions().CountValue(3, q1, q2, q3, q4, q5, q6, q7));
            Assert.AreEqual(0, new AbstractConditionalLevelInstanceFunctions().CountValue(4, q1, q2, q3, q4, q5, q6, q7));
            Assert.AreEqual(4, new AbstractConditionalLevelInstanceFunctions().CountValue(2, q1, q2, q3, q4, q5, q6, q7, q8));
        }

        [Test]
        public void Test_CountValues()
        {
            var _mc123_1 = new decimal[] { 1, 2, 3 };

            Assert.AreEqual(1, _mc123_1.CountValues(1));
            Assert.AreEqual(1, _mc123_1.CountValues(2));
            Assert.AreEqual(1, _mc123_1.CountValues(3));
            Assert.AreEqual(2, _mc123_1.CountValues(1, 2));
            Assert.AreEqual(2, _mc123_1.CountValues(1, 3));
            Assert.AreEqual(3, _mc123_1.CountValues(1, 2, 3));
            Assert.AreEqual(3, _mc123_1.CountValues(1, 2, 3, 4));
            Assert.AreEqual(0, _mc123_1.CountValues());
            Assert.AreEqual(0, _mc123_1.CountValues(null));

            decimal[] empty = null;
            Assert.AreEqual(0, empty.CountValues(1, 2, 3));

            decimal[] empty2 = new decimal[0];
            Assert.AreEqual(0, empty2.CountValues(2, 3, 4));

        }

        [Test]
        public void Test_CenturyMonthCode()
        {
            Assert.AreEqual(11, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(11, 1900));
            Assert.AreEqual(1383, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(3, 2015));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(11, null));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, 2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, null));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(13, 2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(0, 2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(6, 1812));
        }

        [Test]
        public void Test_CenturyMonthCodeDouble()
        {
            Assert.AreEqual(11, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(11.0, 1900.0));
            Assert.AreEqual(1383, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(3.0, 2015.0));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(11.0, null));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, 2015.0));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, null));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(13.0, 2015.0));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(0.0, 2015.0));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(6.0, 1812.0));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(2.2, 2015.0));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(6.0, 1812.3));
        }

        [Test]
        public void Test_CenturyMonthCodeDecimal()
        {
            decimal yr2015 = 2015;
            decimal yr1900 = 1900;
            decimal yr1812 = 1812;

            decimal m11 = 11;

            Assert.AreEqual(11, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(m11, yr1900));
            Assert.AreEqual(1383, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(3, yr2015));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(m11, null));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, yr2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(null, null));

            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(13, yr2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(0, yr2015));
            Assert.AreEqual(-1, new AbstractConditionalLevelInstanceFunctions().CenturyMonthCode(6, yr1812));
        }

        [Test]
        public void Test_IsDate()
        {
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, 12, 31));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, 2, 31));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(null, (decimal)12, 31));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, null, 31));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, 12, null));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, 12, null));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010.2, 12, 10));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, (decimal)12.2, 10));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate((decimal)2010, 12, (decimal)10.2));
        }

        [Test]
        public void Test_IsDateDouble()
        {
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.0, 12, 31));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.0, 2, 31));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(null, 12, 31.0));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.0, null, 31));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.0, 12, null));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.0, 12, null));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010.2, 12, 10));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010, 12.2, 10));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsDate(2010, 12, 10.2));
        }

        [Test]
        public void Test_IsMilitaryTime()
        {
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("0600"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("2323"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("0600"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("2323"));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime(""));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("0090"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("2500"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("2525"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("545")); // leading zeroes are required
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("5:45")); // delimiters are not allowed
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTime("15.3")); // only digits are allowed
        }

        [Test]
        public void Test_IsMilitaryTimeZ()
        {
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("0600Z"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("2323J"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("0600Z"));
            Assert.IsTrue(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("2323J"));

            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ(""));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("0090A"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("2500Z"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("2525B"));
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("0630q")); // small letters not allowed, capital letters are required
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("0630")); // time zone is required
            Assert.IsFalse(new AbstractConditionalLevelInstanceFunctions().IsMilitaryTimeZ("545A")); // leading zeroes are required
        }

        [Test]
        [TestCase("2001-09-12", "2015-03-20", ExpectedResult = 13)]
        [TestCase("2014-02-20", "2015-03-20", ExpectedResult = 1)]
        [TestCase("2015-03-20", "2015-12-31", ExpectedResult = 0)]
        [TestCase("2015-12-31", "2015-03-20", ExpectedResult = -9998)]
        [TestCase("2015-12-31", null, ExpectedResult = -9999)]
        [TestCase(null, "2015-12-31", ExpectedResult = -9999)]
        [TestCase(null, null, ExpectedResult = -9999)]

        [TestCase("2000-02-29", "2001-02-28", ExpectedResult = 0)]
        [TestCase("2000-02-29", "2001-03-01", ExpectedResult = 1)]
        [TestCase("2000-02-29", "2004-02-28", ExpectedResult = 3)]
        [TestCase("2000-02-29", "2004-02-29", ExpectedResult = 4)]
        [TestCase("2000-02-29", "2004-03-01", ExpectedResult = 4)]
        public int Test_FullYearsBetween(DateTime? first, DateTime? second)
        {
            return new AbstractConditionalLevelInstanceFunctions().FullYearsBetween(first, second);

            
            /*Assert.AreEqual(level.FullYearsBetween(d11, d12), 0);
            Assert.AreEqual(level.FullYearsBetween(d11, d13), 1);
            Assert.AreEqual(level.FullYearsBetween(d11, d14), 3);
            Assert.AreEqual(level.FullYearsBetween(d11, d15), 4);
            Assert.AreEqual(level.FullYearsBetween(d11, d16), 4);
            */



            /*
            Assert.AreEqual(13, level.FullYearsBetween(d1, d2));
            Assert.AreEqual(1, level.FullYearsBetween(d3, d2));
            Assert.AreEqual(0, level.FullYearsBetween(d2, d4));

            Assert.AreEqual(-9998, level.FullYearsBetween(d4, d2));
            Assert.AreEqual(-9999, level.FullYearsBetween(d4, null));
            Assert.AreEqual(-9999, level.FullYearsBetween(null, d4));
            Assert.AreEqual(-9999, level.FullYearsBetween(null, null));
            */

        }

        [Test]
        public void Test_FullYearsSince()
        {
            DateTime? d1 = new DateTime(2000, 1, 1);
            DateTime? d2 = new DateTime(1990, 1, 1);

            Assert.AreEqual(10, d1.FullYearsSince(d2));
        }

        [Test]
        public void Test_SelectKish1949()
        {
            Assert.AreEqual(5, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(8, 5));
            Assert.AreEqual(1, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(8, 1));
            Assert.AreEqual(2, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(3, 20));

            Assert.AreEqual(-9999, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(8, null)); // undefined household size
            Assert.AreEqual(-9998, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(10, 3)); // invalid table number
            Assert.AreEqual(-9997, new AbstractConditionalLevelInstanceFunctions().SelectKish1949(8, 0));
        }

        [Test]
        public void Test_SelectKishIlo()
        {
            Assert.AreEqual(1, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(121, 8));
            Assert.AreEqual(1, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(1210, 8));
            Assert.AreEqual(5, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(555, 5));
            Assert.AreEqual(1, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(121, 12));
            Assert.AreEqual(-9997, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(121, 0));
            Assert.AreEqual(-9998, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(0, 1));
            Assert.AreEqual(-9999, new AbstractConditionalLevelInstanceFunctions().SelectKishIlo(121, null));
        }

        [Test]
        public void Test_Concat()
        {
            Assert.AreEqual("George Washington", new AbstractConditionalLevelInstanceFunctions().Concat("George", " ", "Washington"));
            Assert.AreEqual("Washington", new AbstractConditionalLevelInstanceFunctions().Concat("Washington"));
            Assert.AreEqual("", new AbstractConditionalLevelInstanceFunctions().Concat(null));
            Assert.AreEqual("George", new AbstractConditionalLevelInstanceFunctions().Concat("", "George", "", null));
        }

        [Test]
        public void Test_IsLike()
        {
            Assert.IsTrue("abcdefgh".IsLike("abcdefgh"));
            Assert.IsTrue("abcdefgh".IsLike("ab?defgh"));
            Assert.IsTrue("abcdefgh".IsLike("a*h"));
            Assert.IsTrue("abcdefgh".IsLike("a*"));
            Assert.IsTrue("abcdefgh".IsLike("*h"));
            Assert.IsTrue("abcdefgh".IsLike("ab?d*"));
            Assert.IsTrue("abcdefgh".IsLike("ab????gh"));
            Assert.IsTrue("abcdefgh".IsLike("*abcdefgh"));
            Assert.IsTrue("abcdefgh".IsLike("abcdefgh*"));

            Assert.IsFalse("abcdefgh".IsLike("bacdefgh"));
            Assert.IsFalse("abcdefgh".IsLike("?abcdefgh"));
            Assert.IsFalse("abcdefgh".IsLike("abcdefgh?"));
            Assert.IsFalse("abcdefgh".IsLike("?c?"));
            Assert.IsTrue("".IsLike(""));
            Assert.IsFalse("".IsLike("*"));

            Assert.IsFalse("abcdefgh".IsLike("abcde*efgh"));
        }

        [Test]
        public void Test_IsLike2()
        {
            Assert.Throws<ArgumentException>(() => "abcdefgh".IsLike("*cdef*"));
        }

        [Test]
        public void Test_IsLike3()
        {
            Assert.IsTrue("abc".IsLike("abc"));
            Assert.IsFalse("abc".IsLike("Abc"));
            Assert.IsTrue("abc".IsLike("a?c"));
            Assert.IsTrue("abc".IsLike("a*c"));
            Assert.IsTrue("abc".IsLike("a*"));
            Assert.IsTrue("abc".IsLike("*bc"));
            Assert.IsFalse("abc".IsLike("?abc"));
        }

        [Test]
        public void Test_Left()
        {
            Assert.AreEqual(null, "abcdefgh".Left(null));
            Assert.AreEqual("", "abcdefgh".Left(0));
            Assert.AreEqual("ab", "abcdefgh".Left(2));
            Assert.AreEqual("abcdefgh", "abcdefgh".Left(222));
            Assert.AreEqual("", "".Left(2));
            Assert.AreEqual("", "abcdefg".Left(-2));
        }

        [Test]
        public void Test_LeftDouble()
        {
            double? n = null;
            Assert.AreEqual(null, "abcdefgh".Left(n));
            Assert.AreEqual("", "abcdefgh".Left(0.0));
            Assert.AreEqual("ab", "abcdefgh".Left(2.0));
            Assert.AreEqual("abcdefgh", "abcdefgh".Left(222.0));
            Assert.AreEqual("", "".Left(2.0));
            Assert.AreEqual("", "abcdefg".Left(-2.0));
        }

        [Test]
        public void Test_LeftDecimal()
        {
            decimal? n = null;

            Assert.AreEqual(null, "abcdefgh".Left(n));
            Assert.AreEqual("", "abcdefgh".Left((decimal)0));
            Assert.AreEqual("ab", "abcdefgh".Left((decimal)2));
            Assert.AreEqual("abcdefgh", "abcdefgh".Left((decimal)222));
            Assert.AreEqual("", "".Left((decimal)2));
            Assert.AreEqual("", "abcdefg".Left((decimal)-2));
        }


        [Test]
        public void Test_Right()
        {
            Assert.AreEqual(null, "abcdefgh".Right(null));
            Assert.AreEqual("", "abcdefgh".Right(0));
            Assert.AreEqual("gh", "abcdefgh".Right(2));
            Assert.AreEqual("abcdefgh", "abcdefgh".Right(222));
            Assert.AreEqual("", "".Right(2));
            Assert.AreEqual("", "abcde".Right(-2));
        }

        [Test]
        public void Test_RightDouble()
        {
            double? n = null;
            Assert.AreEqual(null, "abcdefgh".Right(n));
            Assert.AreEqual("", "abcdefgh".Right(0.0));
            Assert.AreEqual("gh", "abcdefgh".Right(2.0));
            Assert.AreEqual("abcdefgh", "abcdefgh".Right(222.0));
            Assert.AreEqual("", "".Right(2.0));
        }

        [Test]
        public void Test_RightDecimal()
        {
            decimal? n = null;

            Assert.AreEqual(null, "abcdefgh".Right(n));
            Assert.AreEqual("", "abcdefgh".Right((decimal)0));
            Assert.AreEqual("gh", "abcdefgh".Right((decimal)2));
            Assert.AreEqual("abcdefgh", "abcdefgh".Right((decimal)222));
            Assert.AreEqual("", "".Right((decimal)2));
        }


        [Test]
        public void Test_IsIntegerNumber()
        {
            Assert.AreEqual(true, "12".IsIntegerNumber());
            Assert.AreEqual(true, "-120".IsIntegerNumber());
            Assert.AreEqual(false, "12.5".IsIntegerNumber());
            Assert.AreEqual(false, "abc".IsIntegerNumber());
            Assert.AreEqual(false, "".IsIntegerNumber());
        }

        [Test]
        [SetCulture("en-US")]
        public void Test_IsNumber()
        {
            Assert.AreEqual(true, "3.1415".IsNumber());
            Assert.AreEqual(true, "-3.1415".IsNumber());
            Assert.AreEqual(false, "3.14.15".IsNumber());
            Assert.AreEqual(false, "3FA2".IsNumber());
            Assert.AreEqual(false, "".IsNumber());
        }

        [Test]
        public void Test_IsAlphaLatin()
        {
            Assert.IsTrue("".IsAlphaLatin());
            Assert.IsTrue("ABC".IsAlphaLatin());
            Assert.IsTrue("xyz".IsAlphaLatin());
            Assert.IsTrue("ABCxyz".IsAlphaLatin());
            Assert.IsFalse("abc.".IsAlphaLatin());
        }

        [Test]
        public void Test_IsAlphaLatinOrDelimiter()
        {
            Assert.IsTrue("".IsAlphaLatinOrDelimiter());
            Assert.IsTrue("ABC".IsAlphaLatinOrDelimiter());
            Assert.IsTrue("xyz".IsAlphaLatinOrDelimiter());
            Assert.IsTrue("ABCxyz".IsAlphaLatinOrDelimiter());
            Assert.IsTrue("abc.".IsAlphaLatinOrDelimiter());
            Assert.IsFalse("abc(def)gh".IsAlphaLatinOrDelimiter());
        }

        [Test]
        public void Test_ConsistsOf()
        {
            Assert.IsTrue("abcdefgabcdefg".ConsistsOf("gfedcba"));
            Assert.IsFalse("987".ConsistsOf("01"));
            Assert.IsTrue("George Washington".ConsistsOf("Georg Washint"));
            Assert.IsFalse("George Washington".ConsistsOf("Geor washint"));

            string tst = null;
            Assert.IsTrue(tst.ConsistsOf("ABCDEF"));
        }

        [Test]
        public void Test_GpsDistance()
        {
            var p1 = new GeoLocation(38.9047, -77.0164, 15, 15);
            var p2 = new GeoLocation(39.9500, -75.1667, 15, 15);

            var d = Extensions.GpsDistance(p1, p2);
            Assert.IsTrue(Math.Abs(196800 - d) < 100);   // meters

            p1.Latitude = 36.12;
            p1.Longitude = -86.67;
            p2.Latitude = 33.94;
            p2.Longitude = -118.4;

            Assert.IsTrue(Math.Abs(2887259.95060711 - p1.GpsDistance(p2)) < 0.001);
        }

        [Test]
        public void Test_GpsDistanceCoord()
        {
            var p1 = new GeoLocation(38.9047, -77.0164, 15, 15);
            var d = Extensions.GpsDistance(p1, 39.9500, -75.1667);
            Assert.IsTrue(Math.Abs(196800 - d) < 100); // meters
        }


        [Test]
        public void Test_GpsDistanceKm()
        {
            var p1 = new GeoLocation(38.9047, -77.0164, 15, 15);
            var p2 = new GeoLocation(39.9500, -75.1667, 15, 15);

            var d = Extensions.GpsDistanceKm(p1, p2);
            Assert.IsTrue(Math.Abs(196.8 - d) < 0.1);   // kilometers

            p1.Latitude = 36.12;
            p1.Longitude = -86.67;
            p2.Latitude = 33.94;
            p2.Longitude = -118.4;

            Assert.IsTrue(Math.Abs(2887.25995060711 - p1.GpsDistanceKm(p2)) < 1);
        }

        [Test]
        public void Test_GpsDistanceCoordKm()
        {
            var p1 = new GeoLocation(38.9047, -77.0164, 15, 15);
            var d = Extensions.GpsDistanceKm(p1, 39.9500, -75.1667);
            Assert.IsTrue(Math.Abs(196.8 - d) < 0.1); // kilometers
        }

        [Test]
        public void Test_InRectangle()
        {
            var point = new GeoLocation(20, 20, 0, 0);
            Assert.IsTrue(point.InRectangle(30, 0, 10, 40));   // Ok
            Assert.IsFalse(point.InRectangle(15, 0, 10, 40));  // point too far North
            Assert.IsFalse(point.InRectangle(30, 25, 10, 40));  // point too far West
            Assert.IsFalse(point.InRectangle(30, 0, 25, 40));  // point too far South
            Assert.IsFalse(point.InRectangle(30, 0, 10, 15));  // point too far East
        }


        [Test]
        public void Test_Bmi()
        {
            Assert.AreEqual(24.98, (double)new AbstractConditionalLevelInstanceFunctions().Bmi(68, 1.65), 0.05);
        }

        #endregion

        #region ZSCORES TESTS

        [Test]
        public void Test_Zscores()
        {
            var delta = 0.1;

            Assert.AreEqual(2, ZScore.Bmifa(20, false, 18.7), delta);
            Assert.AreEqual(2, ZScore.Hcfa(20, false, 49.4), delta);
            Assert.AreEqual(2, ZScore.Lhfa(20, false, 88.7), delta);
            Assert.AreEqual(2, ZScore.Wfa(20, false, 13.7), delta);

            Assert.AreEqual(2, ZScore.Wfl(99.5, false, 18.0), delta);
            Assert.AreEqual(2, ZScore.Ssfa(20, true, 9.0), delta);
            Assert.AreEqual(2, ZScore.Acfa(20, true, 17.4), delta);
            Assert.AreEqual(2, ZScore.Tsfa(50, true, 12.9), delta);
            Assert.AreEqual(2, ZScore.Wfh(85, true, 13.8), delta);

        }

        [Test]
        public void Test_Bmia2()
        {
            var ht = 1.00;
            var wt = 12.8;
            Assert.AreEqual(-2, ZScore.Bmifa(50, false, wt, ht), 0.02);

            wt = 17.7;
            ht = 1.00;
            Assert.AreEqual(1, ZScore.Bmifa(16, true, wt, ht), 0.02);
        }



        [Test]
        public void TestForNullsInSex()
        {
            Assert.Throws<ArgumentNullException>(() => ZScore.Ssfa(12, null, 0));
        }

        [Test]
        public void TestForNullsInLen()
        {
            Assert.Throws<ArgumentNullException>(() => ZScore.Wfl(null, true, 0));
        }


        #region WFH

        [Test]
        public void Test_ZscoresWfh_When_height_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ZScore.Wfh(null, true, 10));
        }

        [Test]
        public void Test_ZscoresWfh()
        {
            Assert.Throws<ArgumentException>(() => ZScore.Wfh(0, true, -10));
        }

        [Test]
        public void Test_ZscoresWfh2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Wfh(49, true, 14));
        }

        #endregion

        [Test]
        public void Test_ZscoresTsfa2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Tsfa(-3, true, 14));
        }

        #region WFL
        [Test]
        public void Test_ZscoresWfl2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Wfl(-3, true, 14));
        }

        [Test]
        public void Test_ZscoresWfl3()
        {
            Assert.Throws<ArgumentException>(() => ZScore.Wfl(3, true, -14));
        }

        #endregion

        #region SSFA
        [Test]
        public void TestSsfaForNullsInAge()
        {
            Assert.Throws<ArgumentNullException>(() => ZScore.Ssfa(null, true, 0));
        }

        [Test]
        public void TestSsfaForNullsInSex()
        {
            Assert.Throws<ArgumentNullException>(() => ZScore.Ssfa(20, null, 0));
        }

        [Test]
        public void TestSsfaForRangeMonths()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Ssfa(-3, true, 14));
        }

        [Test]
        public void Test_ZscoresSsfa4()
        {
            Assert.Throws<ArgumentException>(() => ZScore.Ssfa(5, true, -10));
        }
        #endregion

        #region ACFA
        [Test]
        public void Test_ZscoresAcfa2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Acfa(-3, true, 14));
        }

        [Test]
        public void Test_ZscoresAcfa3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Acfa(1, true, 14)); // still out of range
        }

        [Test]
        public void Test_ZscoresAcfa4()
        {
            Assert.Throws<ArgumentException>(() => ZScore.Acfa(5, true, -10));
        }

        #endregion

        #region HCFA

        [Test]
        public void Test_ZscoresHcfa2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZScore.Hcfa(-3, false, 48));
        }

        [Test]
        public void Test_ZscoresHcfa3()
        {
            Assert.Throws<ArgumentException>(() => ZScore.Hcfa(3, true, -48));
        }

        #endregion

        #endregion

        #region EMAIL TESTS 
        // Examples https://en.wikipedia.org/wiki/Email_address#Examples

        [Test]
        public void Test_EmailValid()
        {
            var validEmails = new List<String>();
            validEmails.Add("prettyandsimple@example.com");
            validEmails.Add("very.common@example.com");
            validEmails.Add("disposable.style.email.with+symbol@example.com");
            validEmails.Add("other.email-with-dash@example.com");
            validEmails.Add("\"much.more unusual\"@example.com");
            validEmails.Add("\"very.unusual.@.unusual.com\"@example.com");
            validEmails.Add("\"very.(),:;<>[]\\\".VERY.\\\"very@\\\\ \\\"very\\\".unusual\"@strange.example.com");
            //validEmails.Add("admin@mailserver1");  // ??
            validEmails.Add("#!$%&'*+-/=?^_`{}|~@example.org");
            validEmails.Add("\"()<>[]:,;@\\\\\\\"!#$%&'*+-/=?^_`{}| ~.a\"@example.org");
            validEmails.Add("\" \"@example.org");
            validEmails.Add("üñîçøðé@example.com");
            validEmails.Add("üñîçøðé@üñîçøðé.com");
            validEmails.Add("чебурашка@ящик-с-апельсинами.рф");
            validEmails.Add("甲斐@黒川.日本");
            validEmails.Add("我買@屋企.香港");
            validEmails.Add("δοκιμή@παράδειγμα.δοκιμή");
            validEmails.Add("Pelé@example.com");
            validEmails.Add("\"john..doe\"@example.com");  // Quoted content should be permitted
            //validEmails.Add("postmaster@.");             // Microsoft disallows this. Only one reference on StackOverflow that this may be a valid email. 

            validEmails.Add("support@mysurvey.solutions");

            foreach (var v in validEmails)
            {
                Assert.IsTrue(v.IsValidEmail());
            }
        }
        [Test]
        public void Test_EmailInvalid()
        {
            var invalidEmails = new List<String>();
            invalidEmails.Add("Abc.example.com");
            invalidEmails.Add("A@b@c@example.com");
            invalidEmails.Add("a\"b(c)d,e:f;g<h>i[j\\k]l@example.com");
            invalidEmails.Add("just\"not\"right@example.com");
            invalidEmails.Add("this is\"not\\allowed@example.com");
            invalidEmails.Add("this\\ still\\\"not\\\\allowed@example.com");
            invalidEmails.Add("john..doe@example.com");
            invalidEmails.Add("john.doe@example..com");
            invalidEmails.Add(" prettyandsimple@example.com");
            invalidEmails.Add("prettyandsimple@example.com ");
            invalidEmails.Add("abc");

            foreach (var v in invalidEmails)
            {
                Assert.IsFalse(v.IsValidEmail());
            }
        }

        #endregion

        #region March 2016 functions


        [Test]
        public void Test_IsDateLong()
        {
            Assert.IsTrue(new LevelFunctions().IsDate((long)2010, 12, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 2, 31));

            Assert.IsFalse(new LevelFunctions().IsDate(null, (long)12, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, null, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 12, null));

            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 12, null));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 13, 10));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 12, 32));

            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, 12, -1));
            Assert.IsFalse(new LevelFunctions().IsDate((long)2010, -12, 1));
            Assert.IsFalse(new LevelFunctions().IsDate((long)-2010, 12, 1));
        }

        [Test]
        public void Test_IsDateInt()
        {
            Assert.IsTrue(new LevelFunctions().IsDate((int)2010, 12, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 2, 31));

            Assert.IsFalse(new LevelFunctions().IsDate(null, (int)12, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, null, 31));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 12, null));

            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 12, null));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 13, 10));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 12, 32));

            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, 12, -1));
            Assert.IsFalse(new LevelFunctions().IsDate((int)2010, -12, 1));
            Assert.IsFalse(new LevelFunctions().IsDate((int)-2010, 12, 1));
        }

        [Test]
        public void Test_DaysBetweenDates()
        {
            DateTime? d1 = new DateTime(2016, 03, 01);
            DateTime? d2 = new DateTime(2016, 03, 02);
            DateTime? d3 = new DateTime(2016, 02, 27);
            DateTime? d4 = new DateTime(2000, 01, 01);

            Assert.AreEqual(1, new LevelFunctions().DaysBetweenDates(d1, d2));
            Assert.AreEqual(3, new LevelFunctions().DaysBetweenDates(d3, d1));
            Assert.AreEqual(4, new LevelFunctions().DaysBetweenDates(d3, d2));

            Assert.AreEqual(5904, new LevelFunctions().DaysBetweenDates(d4, d1)); //confirmed with Gnumeric

            Assert.AreEqual(-9998, new LevelFunctions().DaysBetweenDates(d2, d1));

            Assert.AreEqual(-9999, new LevelFunctions().DaysBetweenDates(null, d2));
            Assert.AreEqual(-9999, new LevelFunctions().DaysBetweenDates(d1, null));
            Assert.AreEqual(-9999, new LevelFunctions().DaysBetweenDates(null, null));
        }

        [Test]
        public void Test_YearOfCmc()
        {
            var cmc1 = new LevelFunctions().CenturyMonthCode(1, 1900); // jan1900
            var cmc2 = new LevelFunctions().CenturyMonthCode(12, 1900); // dec1900
            var cmc3 = new LevelFunctions().CenturyMonthCode(1, 1901); // jan1901
            var cmc4 = new LevelFunctions().CenturyMonthCode(3, 2016); // mar2016
            var cmc5 = new LevelFunctions().CenturyMonthCode(1, 1946); // jan1946

            Assert.AreEqual(1900, new LevelFunctions().YearOfCmc(cmc1));
            Assert.AreEqual(1900, new LevelFunctions().YearOfCmc(cmc2));
            Assert.AreEqual(1901, new LevelFunctions().YearOfCmc(cmc3));
            Assert.AreEqual(2016, new LevelFunctions().YearOfCmc(cmc4));
            Assert.AreEqual(1946, new LevelFunctions().YearOfCmc(cmc5));

            Assert.AreEqual(-9999, new LevelFunctions().YearOfCmc(-1));
        }

        [Test]
        public void Test_DaysInMonth1()
        {
            var cmc1 = new LevelFunctions().CenturyMonthCode(1, 1900); // jan1900
            var cmc2 = new LevelFunctions().CenturyMonthCode(12, 1900); // dec1900
            var cmc3 = new LevelFunctions().CenturyMonthCode(1, 1901); // jan1901
            var cmc4 = new LevelFunctions().CenturyMonthCode(3, 2016); // mar2016
            var cmc5 = new LevelFunctions().CenturyMonthCode(1, 1946); // jan1946

            var cmc6 = new LevelFunctions().CenturyMonthCode(2, 1900); // feb1900
            var cmc7 = new LevelFunctions().CenturyMonthCode(2, 2000); // feb2000
            var cmc8 = new LevelFunctions().CenturyMonthCode(11, 2016); // nov2016

            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(cmc1));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(cmc2));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(cmc3));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(cmc4));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(cmc5));
            Assert.AreEqual(28, new LevelFunctions().DaysInMonth(cmc6));
            Assert.AreEqual(29, new LevelFunctions().DaysInMonth(cmc7));
            Assert.AreEqual(30, new LevelFunctions().DaysInMonth(cmc8));

            Assert.AreEqual(-9999, new LevelFunctions().DaysInMonth(-1));
        }

        [Test]
        public void Test_DaysInMonth3()
        {
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(new DateTime(1900, 1, 9)));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(new DateTime(1900, 12, 9)));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(new DateTime(1901, 1, 9)));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(new DateTime(2016, 3, 9)));
            Assert.AreEqual(31, new LevelFunctions().DaysInMonth(new DateTime(1946, 1, 9)));
            Assert.AreEqual(28, new LevelFunctions().DaysInMonth(new DateTime(1900, 2, 9)));
            Assert.AreEqual(29, new LevelFunctions().DaysInMonth(new DateTime(2000, 2, 9)));
            Assert.AreEqual(30, new LevelFunctions().DaysInMonth(new DateTime(2016, 11, 9)));

            Assert.AreEqual(-9999, new LevelFunctions().DaysInMonth(null));
        }

        [Test]
        public void Test_MonthOfCmc()
        {
            var cmc1 = new LevelFunctions().CenturyMonthCode(1, 1900); // jan1900
            var cmc2 = new LevelFunctions().CenturyMonthCode(12, 1900); // dec1900
            var cmc3 = new LevelFunctions().CenturyMonthCode(1, 1901); // jan1901
            var cmc4 = new LevelFunctions().CenturyMonthCode(3, 2016); // mar2016
            var cmc5 = new LevelFunctions().CenturyMonthCode(1, 1946); // jan1946

            Assert.AreEqual(1, new LevelFunctions().MonthOfCmc(cmc1));
            Assert.AreEqual(12, new LevelFunctions().MonthOfCmc(cmc2));
            Assert.AreEqual(1, new LevelFunctions().MonthOfCmc(cmc3));
            Assert.AreEqual(3, new LevelFunctions().MonthOfCmc(cmc4));
            Assert.AreEqual(1, new LevelFunctions().MonthOfCmc(cmc5));

            Assert.AreEqual(-9999, new LevelFunctions().MonthOfCmc(-1));
        }

        [Test]
        public void Test_ContainsAnyOtherThan()
        {
            Assert.IsTrue(_mc123.ContainsAnyOtherThan(2)); // Contains 1, 3
            Assert.IsTrue(_mc123.ContainsAnyOtherThan(2, 0)); // Contains 1, 3; 0 is irrelevant
            Assert.IsTrue(_mc123.ContainsAnyOtherThan(5, 10)); // Contains 1,2,3
            Assert.IsTrue(_mc123.ContainsAnyOtherThan(null)); // Contains 1,2,3
            Assert.IsTrue(_mc123.ContainsAnyOtherThan(new int[0])); // Contains 1,2,3
            Assert.IsFalse(_mc123.ContainsAnyOtherThan(1, 2, 3)); // No, does not contain anything else
            Assert.IsFalse(_mc123.ContainsAnyOtherThan(1, 2, 3, 4, 5)); // No, does not contain anything else, couple of irrelevant options

            int[] empty = null;
            Assert.IsFalse(empty.ContainsAnyOtherThan(2)); // No, empty does not contain any other
            Assert.IsFalse(empty.ContainsAnyOtherThan(null));

            empty = new int[0];
            Assert.IsFalse(empty.ContainsAnyOtherThan(2)); // No, empty does nto contain any other

            var trivial = new int[] { 0 };
            Assert.IsTrue(trivial.ContainsAnyOtherThan(2)); // Yes, contains 0
        }

        [Test]
        public void Test_BracketIndexLeftDouble()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft(-1.2, 1, 2, 3));
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft(1.0, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft(1.2, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft(2.0, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft(2.2, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft(3.0, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexLeft(3.3, 1, 2, 3));
        }

        [Test]
        public void Test_BracketIndexRightDouble()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexRight(-1.2, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight(1.0, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight(1.2, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight(2.0, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight(2.2, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight(3.0, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight(3.3, 1, 2, 3));
        }

        [Test]
        public void Test_BracketIndexLeftDecimal()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft(-1.2m, 1, 2, 3));
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft(1.0m, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft(1.2m, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft(2.0m, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft(2.2m, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft(3.0m, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexLeft(3.3m, 1, 2, 3));
        }

        [Test]
        public void Test_BracketIndexRightDecimal()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexRight(-1.2m, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight(1.0m, 1, 2, 3));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight(1.2m, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight(2.0m, 1, 2, 3));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight(2.2m, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight(3.0m, 1, 2, 3));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight(3.3m, 1, 2, 3));
        }

        [Test]
        public void Test_BracketIndexLeftLong()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft((long)-12, 10, 20, 30));
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft((long)10, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft((long)12, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft((long)20, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft((long)22, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft((long)30, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexLeft((long)33, 10, 20, 30));
        }

        [Test]
        public void Test_BracketIndexRightLong()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexRight((long)-12, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight((long)10, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight((long)12, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight((long)20, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight((long)22, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight((long)30, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight((long)33, 10, 20, 30));
        }

        [Test]
        public void Test_BracketIndexLeftInt()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft((int)-12, 10, 20, 30));
            Assert.AreEqual(0, new LevelFunctions().BracketIndexLeft((int)10, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft((int)12, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexLeft((int)20, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft((int)22, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexLeft((int)30, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexLeft((int)33, 10, 20, 30));
        }

        [Test]
        public void Test_BracketIndexRightInt()
        {
            Assert.AreEqual(0, new LevelFunctions().BracketIndexRight((int)-12, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight((int)10, 10, 20, 30));
            Assert.AreEqual(1, new LevelFunctions().BracketIndexRight((int)12, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight((int)20, 10, 20, 30));
            Assert.AreEqual(2, new LevelFunctions().BracketIndexRight((int)22, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight((int)30, 10, 20, 30));
            Assert.AreEqual(3, new LevelFunctions().BracketIndexRight((int)33, 10, 20, 30));
        }

        #endregion

    }
}
