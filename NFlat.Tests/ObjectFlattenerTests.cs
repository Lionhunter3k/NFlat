using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NFlat.Tests
{
    public class User
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }

        public int PhoneNumber { get; set; }

        public Country Country { get; set; }
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
                { "Email", "something@something.com" },
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

    }
}
