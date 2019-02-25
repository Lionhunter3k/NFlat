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

    public class Address
    {
        public string Street { get; set; }

        public int PhoneNumber { get; set; }

        public Country Country { get; set; }

        public List<int> Ids { get; set; }
    }

    public class Country
    {
        public string Symbol { get; set; }

        public string Name { get; set; }
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
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u,v) => u.Username = v))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => u.Email = v))
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
                .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => u.Username = v))
                .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => u.Email = v))
                .MapNested(nameof(User.Address), new GenericConstructorMap<User>(u => u.Address = new Address(), u => u.Address))
                .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => u.Street = v))
                .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => u.PhoneNumber = v))
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
               .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => u.Username = v))
               .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => u.Email = v))
               .MapNested(nameof(User.Address), new GenericConstructorMap<User>(u => u.Address = new Address(), u => u.Address))
               .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => u.Street = v))
               .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => u.PhoneNumber = v))
               .MapNested("Address_Country", new GenericConstructorMap<Address>(u => u.Country = new Country(), u => u.Country))
               .MapProperty("Address_Country_Symbol", new StringPropertyMap<Country>((u, v) => u.Symbol = v))
               .MapProperty("Address_Country_Name", new StringPropertyMap<Country>((u, v) => u.Name = v))
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
               .MapProperty(nameof(User.Username), new StringPropertyMap<User>((u, v) => u.Username = v))
               .MapProperty(nameof(User.Email), new StringPropertyMap<User>((u, v) => u.Email = v))
               .MapNested(nameof(User.Address), new GenericConstructorMap<User>(u => u.Address = new Address(), u => u.Address))
               .MapProperty("Address_Street", new StringPropertyMap<Address>((u, v) => u.Street = v))
               .MapProperty("Address_PhoneNumber", new Int32PropertyMap<Address>((u, v) => u.PhoneNumber = v))
               .MapNested("Address_Country", new GenericConstructorMap<Address>(u => u.Country = new Country(), u => u.Country))
               .MapProperty("Address_Country_Symbol", new StringPropertyMap<Country>((u, v) => u.Symbol = v))
               .MapProperty("Address_Country_Name", new StringPropertyMap<Country>((u, v) => u.Name = v))
               .MapNested("Address_Ids", new GenericConstructorMap<Address>(u => u.Ids = new List<int>(), u => u.Ids))
               .MapProperty("Address_Ids_*", new Int32PropertyMap<List<int>>((u, v) => u.Add(v)))
               .MapNested("Countries", new GenericConstructorMap<User>(u => u.Countries = new List<Country>(), u => u.Countries))
               .MapNested("Countries_*", new GenericConstructorMap<List<Country>>((u) => u.Add(new Country()), u => u.Last()))
               .MapProperty("Countries_*_Symbol", new StringPropertyMap<Country>((u, v) => u.Symbol = v))
               .MapProperty("Countries_*_Name", new StringPropertyMap<Country>((u, v) => u.Name = v))
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

    }
}
