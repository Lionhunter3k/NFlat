using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NFlat.Tests
{
    [TestFixture]
    public class DictionaryFlattenerTests
    {
        [Test]
        public void Simple_Objects_Are_Same()
        {
            var flatObject = new Dictionary<string, string>()
            {
                { "Username", "John" },
                { "Email", "something@something.com" }
            };
            var unflattenedObject = new DictionaryFlattener().Unflatten(flatObject);
            Assert.IsTrue(unflattenedObject.ContainsKey("Username"));
            Assert.AreEqual("John", unflattenedObject["Username"]);
            Assert.IsTrue(unflattenedObject.ContainsKey("Email"));
            Assert.AreEqual("something@something.com", unflattenedObject["Email"]);
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
            var unflattenedObject = new DictionaryFlattener().Unflatten(flatObject);
            Assert.IsTrue(unflattenedObject.ContainsKey("Username"));
            Assert.AreEqual("John", unflattenedObject["Username"]);
            Assert.IsTrue(unflattenedObject.ContainsKey("Email"));
            Assert.AreEqual("something@something.com", unflattenedObject["Email"]);
            Assert.IsTrue(unflattenedObject.ContainsKey("Address"));
            var address = unflattenedObject["Address"] as Dictionary<string, object>;
            Assert.IsNotNull(address);
            Assert.IsTrue(address.ContainsKey("Street"));
            Assert.AreEqual("Victory Street", address["Street"]);
            Assert.IsTrue(address.ContainsKey("PhoneNumber"));
            Assert.AreEqual("321321423", address["PhoneNumber"]);
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
            var unflattenedObject = new DictionaryFlattener().Unflatten(flatObject);
            Assert.IsTrue(unflattenedObject.ContainsKey("Username"));
            Assert.AreEqual("John", unflattenedObject["Username"]);
            Assert.IsTrue(unflattenedObject.ContainsKey("Email"));
            Assert.AreEqual("something@something.com", unflattenedObject["Email"]);
            Assert.IsTrue(unflattenedObject.ContainsKey("Address"));
            var address = unflattenedObject["Address"] as Dictionary<string, object>;
            Assert.IsNotNull(address);
            Assert.IsTrue(address.ContainsKey("Street"));
            Assert.AreEqual("Victory Street", address["Street"]);
            Assert.IsTrue(address.ContainsKey("PhoneNumber"));
            Assert.AreEqual("321321423", address["PhoneNumber"]);
            Assert.IsTrue(address.ContainsKey("Country"));
            var country = address["Country"] as Dictionary<string, object>;
            Assert.IsNotNull(country);
            Assert.IsTrue(country.ContainsKey("Symbol"));
            Assert.AreEqual("ro", country["Symbol"]);
            Assert.IsTrue(country.ContainsKey("Name"));
            Assert.AreEqual("Romania", country["Name"]);
        }

    }
}
