﻿namespace Unosquare.Swan.Test.JsonTests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Formatters;
    using Reflection;
    using Mocks;

    public abstract class JsonTest : TestFixtureBase
    {
        protected const string ArrayStruct = "[{\"Value\": 1,\"Name\": \"A\"},{\"Value\": 2,\"Name\": \"B\"}]";

        protected static readonly AdvJson AdvObj = new AdvJson
        {
            StringData = "string,\r\ndata\\",
            IntData = 1,
            NegativeInt = -1,
            DecimalData = 10.33M,
            BoolData = true,
            InnerChild = BasicJson.GetDefault(),
        };

        protected static string BasicStr => "{" + BasicJson.GetControlValue() + "}";

        protected string AdvStr =>
            "{\"InnerChild\": " + BasicStr + "," + BasicJson.GetControlValue() + "}";

        protected string BasicAStr => "[\"A\",\"B\",\"C\"]";

        protected int[] NumericArray => new[] {1, 2, 3};

        protected string NumericAStr => "[1,2,3]";

        protected BasicArrayJson BasicAObj => new BasicArrayJson
        {
            Id = 1,
            Properties = new[] {"One", "Two", "Babu"},
        };

        protected AdvArrayJson AdvAObj => new AdvArrayJson
        {
            Id = 1,
            Properties = new[] {BasicJson.GetDefault(), BasicJson.GetDefault()},
        };

        protected string BasicAObjStr => "{\"Id\": 1,\"Properties\": [\"One\",\"Two\",\"Babu\"]}";

        protected string AdvAStr => "{\"Id\": 1,\"Properties\": [" + BasicStr + "," + BasicStr + "]}";
    }

    [TestFixture]
    public class ToJson : JsonTest
    {
        [Test]
        public void CheckJsonFormat_ValidatesIfObjectsAreEqual()
        {
            Assert.AreEqual(BasicStr, BasicJson.GetDefault().ToJson(false));
        }

        [Test]
        public void CheckJsonFormat_ValidatesIfObjectsAreNotEqual()
        {
            Assert.AreNotEqual(BasicStr, BasicJson.GetDefault().ToJson());
        }

        [Test]
        public void NullObjectAndEmptyString_ValidatesIfTheyAreEquals()
        {
            Assert.AreEqual(string.Empty, NullObj.ToJson());
        }
    }

    [TestFixture]
    public class Serialize : JsonTest
    {
        [Test]
        public void WithStringArray_ReturnsArraySerialized()
        {
            Assert.AreEqual(BasicAStr, Json.Serialize(DefaultStringList));
        }

        [Test]
        public void WithStringsArrayAndWeakReference_ReturnsArraySerialized()
        {
            var instance = BasicJson.GetDefault();
            var reference = new List<WeakReference> {new WeakReference(instance)};

            var data = Json.Serialize(instance, false, null, false, null, null, reference, JsonSerializerCase.None);

            Assert.IsTrue(data.StartsWith("{ \"$circref\":"));
        }

        [Test]
        public void WithNumericArray_ReturnsArraySerialized()
        {
            Assert.AreEqual(NumericAStr, Json.Serialize(NumericArray));
        }

        [Test]
        public void WithBasicObjectWithArray_ReturnsObjectWithArraySerialized()
        {
            Assert.AreEqual(BasicAObjStr, Json.Serialize(BasicAObj));
        }

        [Test]
        public void WithArrayOfObjects_ReturnsArrayOfObjectsSerialized()
        {
            var data = Json.Serialize(new List<BasicJson>
            {
                BasicJson.GetDefault(),
                BasicJson.GetDefault(),
            });

            Assert.IsNotNull(data);
            Assert.AreEqual($"[{BasicStr},{BasicStr}]", data);
        }

        [Test]
        public void AdvObject_ReturnsAdvObjectSerialized()
        {
            Assert.AreEqual(AdvStr, Json.Serialize(AdvObj));
        }

        [Test]
        public void AdvObjectArray_ReturnsAdvObjectArraySerialized()
        {
            Assert.AreEqual(AdvAStr, Json.Serialize(AdvAObj));
        }

        [TestCase("1", 1)]
        [TestCase("1", 1F)]
        [TestCase("string", "string")]
        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("null", null)]
        [TestCase("null", default)]
        public void WithPrimitive_ReturnsStringValue(string expected, object actual)
        {
            Assert.AreEqual(expected, Json.Serialize(actual));
        }

        [Test]
        public void WithDateTest_ReturnsDateTestSerialized()
        {
            var obj = new DateTimeJson {Date = new DateTime(2010, 1, 1)};
            var data = Json.Serialize(obj);

            Assert.IsNotNull(data);
            Assert.AreEqual(
                $"{{\"Date\": \"{obj.Date.Value:s}\"}}", 
                data,
                "Date must be formatted as ISO");

            var dict = Json.Deserialize<Dictionary<string, DateTime>>(data);

            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey("Date"));
            Assert.AreEqual(obj.Date, dict["Date"]);

            var objDeserialized = Json.Deserialize<DateTimeJson>(data);

            Assert.IsNotNull(objDeserialized);
            Assert.AreEqual(obj.Date, objDeserialized.Date);
        }

        [Test]
        public void WithStructureArray_ReturnsStructureArraySerialized()
        {
            var result = new[] {new SampleStruct {Value = 1, Name = "A"}, new SampleStruct {Value = 2, Name = "B"}};

            Assert.AreEqual(ArrayStruct, Json.Serialize(result));
        }

        [Test]
        public void WithEmptyClass_ReturnsEmptyClassSerialized()
        {
            Assert.AreEqual("{ }", Json.Serialize(new EmptyJson()));
        }

        [Test]
        public void WithStructure_ReturnsStructureSerialized()
        {
            Assert.AreEqual("{\"Value\": 1,\"Name\": \"DefaultStruct\"}", Json.Serialize(DefaultStruct));
        }
    }

    [TestFixture]
    public class Deserialize : JsonTest
    {
        [Test]
        public void WithIncludeNonPublic_ReturnsObjectDeserialized()
        {
            var obj = Json.Deserialize<BasicJson>(BasicStr, false);

            Assert.IsNotNull(obj);
            Assert.AreEqual(obj.StringData, BasicJson.GetDefault().StringData);
            Assert.AreEqual(obj.IntData, BasicJson.GetDefault().IntData);
            Assert.AreEqual(obj.NegativeInt, BasicJson.GetDefault().NegativeInt);
            Assert.AreEqual(obj.BoolData, BasicJson.GetDefault().BoolData);
            Assert.AreEqual(obj.DecimalData, BasicJson.GetDefault().DecimalData);
            Assert.AreEqual(obj.StringNull, BasicJson.GetDefault().StringNull);
        }

        [Test]
        public void WithBasicObject_ReturnsObjectDeserialized()
        {
            var obj = Json.Deserialize<BasicJson>(BasicStr);

            Assert.IsNotNull(obj);
            Assert.AreEqual(obj.StringData, BasicJson.GetDefault().StringData);
            Assert.AreEqual(obj.IntData, BasicJson.GetDefault().IntData);
            Assert.AreEqual(obj.NegativeInt, BasicJson.GetDefault().NegativeInt);
            Assert.AreEqual(obj.BoolData, BasicJson.GetDefault().BoolData);
            Assert.AreEqual(obj.DecimalData, BasicJson.GetDefault().DecimalData);
            Assert.AreEqual(obj.StringNull, BasicJson.GetDefault().StringNull);
        }

        [Test]
        public void WithBasicArray_ReturnsArrayDeserialized()
        {
            var arr = Json.Deserialize<List<string>>(BasicAStr);

            Assert.IsNotNull(arr);
            Assert.AreEqual(string.Join(",", DefaultStringList), string.Join(",", arr));
        }

        [Test]
        public void WithBasicObjectWithArray_ReturnsBasicObjectWithArrayDeserialized()
        {
            var data = Json.Deserialize<BasicArrayJson>(BasicAObjStr);

            Assert.IsNotNull(data);
            Assert.AreEqual(BasicAObj.Id, data.Id);
            Assert.IsNotNull(data.Properties);
            Assert.AreEqual(string.Join(",", BasicAObj.Properties), string.Join(",", data.Properties));
        }

        [Test]
        public void WithArrayOfObjects_ReturnsArrayOfObjectsDeserialized()
        {
            var data = Json.Deserialize<List<ExtendedPropertyInfo>>(BasicAObjStr);

            Assert.IsNotNull(data);
        }

        [Test]
        public void WithAdvObject_ReturnsAdvObjectDeserialized()
        {
            var data = Json.Deserialize<AdvJson>(AdvStr);

            Assert.IsNotNull(data);
            Assert.IsNotNull(data.InnerChild);

            foreach (var obj in new[] {data, data.InnerChild})
            {
                Assert.AreEqual(obj.StringData, AdvObj.StringData);
                Assert.AreEqual(obj.IntData, AdvObj.IntData);
                Assert.AreEqual(obj.NegativeInt, AdvObj.NegativeInt);
                Assert.AreEqual(obj.BoolData, AdvObj.BoolData);
                Assert.AreEqual(obj.DecimalData, AdvObj.DecimalData);
                Assert.AreEqual(obj.StringNull, AdvObj.StringNull);
            }
        }

        [Test]
        public void WithAdvObjectArray_ReturnsAdvObjectArray()
        {
            var data = Json.Deserialize<AdvArrayJson>(AdvAStr);

            Assert.IsNotNull(data);
            Assert.AreEqual(BasicAObj.Id, data.Id);
            Assert.IsNotNull(data.Properties);

            foreach (var obj in data.Properties)
            {
                Assert.AreEqual(obj.StringData, AdvObj.StringData);
                Assert.AreEqual(obj.IntData, AdvObj.IntData);
                Assert.AreEqual(obj.NegativeInt, AdvObj.NegativeInt);
                Assert.AreEqual(obj.BoolData, AdvObj.BoolData);
                Assert.AreEqual(obj.DecimalData, AdvObj.DecimalData);
                Assert.AreEqual(obj.StringNull, AdvObj.StringNull);
            }
        }

        [Test]
        public void WithEmptyString_ReturnsNull()
        {
            Assert.IsNull(Json.Deserialize(string.Empty));
        }

        [Test]
        public void WithEmptyStringWithTypeParam_ReturnsNull()
        {
            Assert.IsNull(Json.Deserialize<BasicJson>(string.Empty));
        }

        [Test]
        public void WithEmptyPropertyTest_ReturnsNotNullPropertyDeserialized()
        {
            Assert.IsNotNull(Json.Deserialize<BasicJson>("{ \"\": \"value\" }"));
        }

        [Test]
        public void ObjectWithArrayWithData_ReturnsObjectWithArrayWithDataDeserialized()
        {
            var data = Json.Deserialize<ArrayJsonWithInitialData>("{\"Id\": 2,\"Properties\": [\"THREE\"]}");

            Assert.AreEqual(2, data.Id);
            Assert.AreEqual(1, data.Properties.Length);
        }

        [Test]
        public void WithJsonProperty_ReturnsPropertiesDeserialized()
        {
            var obj = new JsonPropertySample {Data = "OK", IgnoredData = "OK"};
            var data = Json.Serialize(obj);

            Assert.AreEqual("{\"data\": \"OK\"}", data);

            var objDeserialized = Json.Deserialize<JsonPropertySample>(data);

            Assert.AreEqual(obj.Data, objDeserialized.Data);
        }

        [Test]
        public void WithStructureArray_ReturnsStructureArrayDeserialized()
        {
            var data = Json.Deserialize<SampleStruct>("{\"Value\": 1,\"Name\": \"A\"}");

            Assert.AreEqual(data.Value, 1);
            Assert.AreEqual(data.Name, "A");
        }

        [Test]
        public void WithStructure_ReturnsStructureDeserialized()
        {
            var data = Json.Deserialize<SampleStruct[]>(ArrayStruct);

            Assert.IsTrue(data.Any());
            Assert.AreEqual(data.First().Value, 1);
            Assert.AreEqual(data.First().Name, "A");
        }

        [Test]
        public void WithEmptyClass_ReturnsEmptyClassDeserialized()
        {
            Assert.IsNotNull(Json.Deserialize<EmptyJson>("{ }"));
        }

        [Test]
        public void WithEmptyType_ResolveType()
        {
            Assert.IsNotNull(Json.Deserialize(BasicStr, null));
        }

        [Test]
        public void WithClassWithoutPublicCtor_ReturnDefault()
        {
            // Default value is null
            Assert.IsNull(Json.Deserialize<BasicJsonWithoutCtor>(BasicStr));
        }

        [Test]
        public void WithInvalidProperty_ReturnDefaultValueProperty()
        {
            var obj = Json.Deserialize<BasicJson>(BasicStr.Replace("\"NegativeInt\": -1", "\"NegativeInt\": \"OK\""));

            Assert.IsNotNull(obj);
            Assert.AreEqual(default(int), obj.NegativeInt);
        }

        [Test]
        public void WithEnumProperty_ReturnValidObject()
        {
            var obj = Json.Deserialize<ObjectEnum>("{ \"Id\": 1, \"MyEnum\": \"Three\" }");

            Assert.IsNotNull(obj);
            Assert.AreEqual(MyEnum.Three, obj.MyEnum);
        }

        [Test]
        public void WithByteArrayProperty_ReturnValidObject()
        {
            var obj = Json.Deserialize<JsonFile>("{ \"Data\": \"DATA1\", \"Filename\": \"Three\" }");

            Assert.IsNotNull(obj);
            Assert.AreEqual("DATA1", obj.Data.ToText());
        }
    }

    [TestFixture]
    public class SerializeOnly : JsonTest
    {
        [Test]
        public void WithObject_ReturnsObjectSerialized()
        {
            var includeNames = new[]
            {
                nameof(BasicJson.StringData),
                nameof(BasicJson.IntData),
                nameof(BasicJson.NegativeInt),
            };

            var dataSerialized = Json.SerializeOnly(BasicJson.GetDefault(), false, includeNames);

            Assert.AreEqual(
                "{\"StringData\": \"string,\\r\\ndata\\\\\",\"IntData\": 1,\"NegativeInt\": -1}",
                dataSerialized);
        }

        [Test]
        public void WithString_ReturnsString()
        {
            Assert.AreEqual("\"\\b\\t\\f\\u0000\"", Json.SerializeOnly("\b\t\f\0", true, null));
        }

        [Test]
        public void WithEmptyString_ReturnsEmptyString()
        {
            var dataSerialized = Json.SerializeOnly(string.Empty, true, null);

            Assert.AreEqual("\"\"", dataSerialized);
        }

        [Test]
        public void WithType_ReturnsString()
        {
            var dataSerialized = Json.SerializeOnly(typeof(string), true, null);

            Assert.AreEqual("\"System.String\"", dataSerialized);
        }

        [Test]
        public void WithEmptyEnumerable_ReturnsEmptyArrayLiteral()
        {
            var emptyEnumerable = Enumerable.Empty<int>();

            var dataSerialized = Json.SerializeOnly(emptyEnumerable, true, null);

            Assert.AreEqual("[ ]", dataSerialized);
        }

        [Test]
        public void WithEmptyDictionary_ReturnsEmptyObjectLiteral()
        {
            var dataSerialized = Json.SerializeOnly(new Dictionary<string, string>(), true, null);

            Assert.AreEqual("{ }", dataSerialized);
        }

        [Test]
        public void WithDictionaryOfDictionaries_ReturnsString()
        {
            var persons = new Dictionary<string, Dictionary<int, string>>
            {
                {"A", new Dictionary<int, string>()},
                {"B", DefaultDictionary },
            };

            var dataSerialized = Json.SerializeOnly(persons, false, null);

            Assert.AreEqual("{\"A\": { },\"B\": {\"1\": \"A\",\"2\": \"B\",\"3\": \"C\",\"4\": \"D\",\"5\": \"E\"}}",
                dataSerialized);
        }

        [Test]
        public void WithDictionaryOfArrays_ReturnsString()
        {
            var wordDictionary =
                new Dictionary<string, string[][]>
                {
                    {"A", new[] {new string[] { }, DefaultStringList.ToArray() }},
                };

            var dataSerialized = Json.SerializeOnly(wordDictionary, false, null);

            Assert.AreEqual("{\"A\": [[ ],[\"A\",\"B\",\"C\"]]}", dataSerialized);
        }
    }

    [TestFixture]
    public class SerializeExcluding : JsonTest
    {
        [Test]
        public void WithObject_ReturnsObjectSerializedExcludingProps()
        {
            var excludeNames = new[]
            {
                nameof(BasicJson.StringData),
                nameof(BasicJson.IntData),
                nameof(BasicJson.NegativeInt),
            };

            var dataSerialized = Json.SerializeExcluding(BasicJson.GetDefault(), false, excludeNames);

            Assert.AreEqual(
                "{\"DecimalData\": 10.33,\"BoolData\": true,\"StringNull\": null}",
                dataSerialized);
        }
    }
}