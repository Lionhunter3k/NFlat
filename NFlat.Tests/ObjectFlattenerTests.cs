using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NFlat.Tests
{
    public class User
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public Address Address { get; set; }

        public List<Country> Countries { get; set; }
    }

    public struct Address
    {
        public string Street { get; set; }

        public int PhoneNumber { get; set; }

        public Country Country { get; set; }

        public List<int> Ids { get; set; }
    }

    public struct Country
    {
        public string Symbol { get; set; }

        public string Name { get; set; }

        public List<Tax> Taxes { get; set; }
    }

    public class Tax
    {
        public decimal Rate { get; set; }
    }

    [TestFixture]
    public class ObjectFlattenerTests
    {
        [Test]
        public void Simple_Objects_Are_Same()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Username", "John" },
                { "Email", "something@something.com" }
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u,v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
        }

        [Test]
        public void First_Level_Nested_Objects_Should_Work()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Address_Street", "Victory Street" },
                { "Address_PhoneNumber", "321321423" },
                { "Username", "John" },
                { "Email", "something@something.com" },
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User, Address>(() => new Address(), (u, v) => { u.Address = v; return u; }))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => { u.Street = v; return u; }))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => { u.PhoneNumber = v; return u; }))
                .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
            var address = unflattenedObject.Address;
            Assert.IsNotNull(address);
            Assert.AreEqual("Victory Street", address.Street);
            Assert.AreEqual(321321423, address.PhoneNumber);
        }

        [Test]
        public void First_Level_Nested_Objects_With_Unmapped_Properties_Should_Work()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Address_Street", "Victory Street" },
                { "Address_PhoneNumber", "321321423" },
                { "Address_Something_DoesntExist", "lol" },
                { "Username", "John" },
                { "Email", "something@something.com" },
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User, Address>(() => new Address(), (u, v) => { u.Address = v; return u; }))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => { u.Street = v; return u; }))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => { u.PhoneNumber = v; return u; }))
                .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
            var address = unflattenedObject.Address;
            Assert.IsNotNull(address);
            Assert.AreEqual("Victory Street", address.Street);
            Assert.AreEqual(321321423, address.PhoneNumber);
        }

        [Test]
        public void Second_Level_Nested_Objects_Should_Work()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Address_Country_Symbol", "ro" },
                { "Address_Country_Name", "Romania" },
                { "Address_Street", "Victory Street" },
                { "Address_PhoneNumber", "321321423" },
                { "Username", "John" },
                { "Email", "something@something.com" }
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User, Address>(() => new Address(), (u, v) => { u.Address = v; return u; }))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => { u.Street = v; return u; }))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => { u.PhoneNumber = v; return u; }))
               .MapNested("Address_Country", new GenericConstructorMap<Address, Country>(() => new Country(), (u, v) => { u.Country = v; return u; }))
               .MapProperty("Address_Country_Symbol", new StringPropertyMap<Country>((u, v) => { u.Symbol = v; return u; }))
               .MapProperty("Address_Country_Name", new StringPropertyMap<Country>((u, v) => { u.Name = v; return u; }))
               .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
            var address = unflattenedObject.Address;
            Assert.IsNotNull(address);
            Assert.AreEqual("Victory Street", address.Street);
            Assert.AreEqual(321321423, address.PhoneNumber);
            var country = address.Country;
            Assert.IsNotNull(country);
            Assert.AreEqual("ro", country.Symbol);
            Assert.AreEqual("Romania", country.Name);
        }

        [Test]
        public void Nested_Lists_Should_Work()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Address_Country_Symbol", "ro" },
                { "Address_Country_Name", "Romania" },
                { "Address_Street", "Victory Street" },
                { "Address_PhoneNumber", "321321423" },
                { "Address_Ids_0", "1" },
                { "Address_Ids_1", "2" },
                { "Username", "John" },
                { "Email", "something@something.com" },
                { "Countries_0_Symbol", "en" },
                { "Countries_0_Name", "UK" },
                { "Countries_1_Symbol", "us" },
                { "Countries_1_Name", "USA" },
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
   .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User, Address>(() => new Address(), (u, v) => { u.Address = v; return u; }))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => { u.Street = v; return u; }))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => { u.PhoneNumber = v; return u; }))
               .MapNested("Address_Country", new GenericConstructorMap<Address, Country>(() => new Country(), (u, v) => { u.Country = v; return u; }))
               .MapProperty("Address_Country_Symbol", new StringPropertyMap<Country>((u, v) => { u.Symbol = v; return u; }))
               .MapProperty("Address_Country_Name", new StringPropertyMap<Country>((u, v) => { u.Name = v; return u; }))
               .MapNested("Address_Ids", new GenericConstructorMap<Address, List<int>>(() => new List<int>(), (u,v) => { u.Ids = v; return u; }))
               .MapProperty("Address_Ids_*", new Int32PropertyMap<List<int>>((u, v) => { u.Add(v); return u; }))
               .MapNested("Countries", new GenericConstructorMap<User, List<Country>> (() => new List<Country>(), (u, v) => { u.Countries = v; return u; }))
               .MapNested("Countries_*", new GenericConstructorMap<List<Country>, Country>(() => new Country(), (u, v) => { u.Add(v); return u; }))
               .MapProperty("Countries_*_Symbol", new StringPropertyMap<Country>((u, v) => { u.Symbol = v; return u; }))
               .MapProperty("Countries_*_Name", new StringPropertyMap<Country>((u, v) => { u.Name = v; return u; }))
               .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
            var address = unflattenedObject.Address;
            Assert.IsNotNull(address);
            Assert.AreEqual("Victory Street", address.Street);
            Assert.AreEqual(321321423, address.PhoneNumber);
            var country = address.Country;
            Assert.IsNotNull(country);
            Assert.AreEqual("ro", country.Symbol);
            Assert.AreEqual("Romania", country.Name);
            var ids = address.Ids;
            Assert.IsNotNull(ids);
            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(1, ids[0]);
            Assert.AreEqual(2, ids[1]);
            var countries = unflattenedObject.Countries;
            Assert.IsNotNull(countries);
            Assert.AreEqual(2, countries.Count);
            Assert.AreEqual("UK", countries[0].Name);
            Assert.AreEqual("en", countries[0].Symbol);
            Assert.AreEqual("USA", countries[1].Name);
            Assert.AreEqual("us", countries[1].Symbol);
        }

        [Test]
        public void Nested_Lists_Within_Nested_Lists_Should_Work()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Address_Country_Symbol", "ro" },
                { "Address_Country_Name", "Romania" },
                { "Address_Country_Taxes_0_Rate", "9" },
                { "Address_Street", "Victory Street" },
                { "Address_PhoneNumber", "321321423" },
                { "Address_Ids_0", "1" },
                { "Address_Ids_1", "2" },
                { "Username", "John" },
                { "Email", "something@something.com" },
                { "Countries_0_Symbol", "en" },
                { "Countries_0_Name", "UK" },
                { "Countries_0_Taxes_0_Rate", "1,2" },
                { "Countries_0_Taxes_1_Rate", "2,2" },
                { "Countries_1_Symbol", "us" },
                { "Countries_1_Name", "USA" },
                { "Countries_1_Taxes_0_Rate", "3,19" },
                { "Countries_1_Taxes_1_Rate", "4,19" },
            };
            var unflattenedObject = new ObjectFlattener<User, string>()
   .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => { u.Username = v; return u; }))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => { u.Email = v; return u; }))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User, Address>(() => new Address(), (u, v) => { u.Address = v; return u; }))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => { u.Street = v; return u; }))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => { u.PhoneNumber = v; return u; }))
               .MapNested("Address_Country", new GenericConstructorMap<Address, Country>(() => new Country(), (u, v) => { u.Country = v; return u; }))
               .MapProperty("Address_Country_Symbol", new StringPropertyMap<Country>((u, v) => { u.Symbol = v; return u; }))
               .MapProperty("Address_Country_Name", new StringPropertyMap<Country>((u, v) => { u.Name = v; return u; }))
               .MapNested("Address_Ids", new GenericConstructorMap<Address, List<int>>(() => new List<int>(), (u, v) => { u.Ids = v; return u; }))
               .MapProperty("Address_Ids_*", new Int32PropertyMap<List<int>>((u, v) => { u.Add(v); return u; }))
               .MapNested("Countries", new GenericConstructorMap<User, List<Country>>(() => new List<Country>(), (u, v) => { u.Countries = v; return u; }))
               .MapNested("Countries_*", new GenericConstructorMap<List<Country>, Country>(() => new Country(), (u, v) => { u.Add(v); return u; }))
               .MapProperty("Countries_*_Symbol", new StringPropertyMap<Country>((u, v) => { u.Symbol = v; return u; }))
               .MapProperty("Countries_*_Name", new StringPropertyMap<Country>((u, v) => { u.Name = v; return u; }))
               .MapNested("Countries_*_Taxes", new GenericConstructorMap<Country, List<Tax>>(() => new List<Tax>(), (u, v) => { u.Taxes = v; return u; }))
               .MapNested("Countries_*_Taxes_*", new GenericConstructorMap<List<Tax>, Tax>(() => new Tax(), (u, v) => { u.Add(v); return u; }))
               .MapProperty("Countries_*_Taxes_*_Rate", new DecimalPropertyMap<Tax>((u, v) => { u.Rate = v; return u; }))
               .MapNested("Address_Country_Taxes", new GenericConstructorMap<Country, List<Tax>>(() => new List<Tax>(), (u, v) => { u.Taxes = v; return u; }))
               .MapNested("Address_Country_Taxes_*", new GenericConstructorMap<List<Tax>, Tax>(() => new Tax(), (u, v) => { u.Add(v); return u; }))
               .MapProperty("Address_Country_Taxes_*_Rate", new DecimalPropertyMap<Tax>((u, v) => { u.Rate = v; return u; }))
               .Unflatten(flatObject);
            Assert.AreEqual("John", unflattenedObject.Username);
            Assert.AreEqual("something@something.com", unflattenedObject.Email);
            var address = unflattenedObject.Address;
            Assert.IsNotNull(address);
            Assert.AreEqual("Victory Street", address.Street);
            Assert.AreEqual(321321423, address.PhoneNumber);
            var country = address.Country;
            Assert.IsNotNull(country);
            Assert.AreEqual("ro", country.Symbol);
            Assert.AreEqual("Romania", country.Name);
            var listOfTaxes = country.Taxes;
            Assert.IsNotNull(listOfTaxes);
            Assert.AreEqual(1, listOfTaxes.Count);
            Assert.AreEqual(9, listOfTaxes[0].Rate);
            var ids = address.Ids;
            Assert.IsNotNull(ids);
            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(1, ids[0]);
            Assert.AreEqual(2, ids[1]);
            var countries = unflattenedObject.Countries;
            Assert.IsNotNull(countries);
            Assert.AreEqual(2, countries.Count);
            Assert.AreEqual("UK", countries[0].Name);
            Assert.AreEqual("en", countries[0].Symbol);
            var firstNestedTaxes = countries[0].Taxes;
            Assert.IsNotNull(firstNestedTaxes);
            Assert.AreEqual(2, firstNestedTaxes.Count);
            Assert.AreEqual(1.2m, firstNestedTaxes[0].Rate);
            Assert.AreEqual(2.2m, firstNestedTaxes[1].Rate);
            Assert.AreEqual("USA", countries[1].Name);
            Assert.AreEqual("us", countries[1].Symbol);
            var secondNestedTaxes = countries[1].Taxes;
            Assert.IsNotNull(secondNestedTaxes);
            Assert.AreEqual(2, secondNestedTaxes.Count);
            Assert.AreEqual(3.19m, secondNestedTaxes[0].Rate);
            Assert.AreEqual(4.19m, secondNestedTaxes[1].Rate);
        }

    }
}
