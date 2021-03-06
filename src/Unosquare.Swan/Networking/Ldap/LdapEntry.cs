﻿namespace Unosquare.Swan.Networking.Ldap
{
    using System.Linq;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a single entry in a directory, consisting of
    /// a distinguished name (DN) and zero or more attributes.
    /// An instance of
    /// LdapEntry is created in order to add an entry to a directory, and
    /// instances of LdapEntry are returned on a search by enumerating an
    /// LdapSearchResults.
    /// </summary>
    /// <seealso cref="LdapAttribute"></seealso>
    /// <seealso cref="LdapAttributeSet"></seealso>
    public class LdapEntry
    {
        private readonly LdapAttributeSet _attrs;

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapEntry" /> class.
        /// Constructs a new entry with the specified distinguished name and set
        /// of attributes.
        /// </summary>
        /// <param name="dn">The distinguished name of the new entry. The
        /// value is not validated. An invalid distinguished
        /// name will cause operations using this entry to fail.</param>
        /// <param name="attrs">The initial set of attributes assigned to the
        /// entry.</param>
        public LdapEntry(string dn = null, LdapAttributeSet attrs = null)
        {
            DN = dn ?? string.Empty;
            _attrs = attrs ?? new LdapAttributeSet();
        }

        /// <summary>
        /// Returns the distinguished name of the entry.
        /// </summary>
        /// <value>
        /// The dn.
        /// </value>
        public string DN { get; }

        /// <summary>
        /// Returns the attributes matching the specified attrName.
        /// </summary>
        /// <param name="attrName">The name of the attribute or attributes to return.</param>
        /// <returns>
        /// The attribute matching the name.
        /// </returns>
        public LdapAttribute GetAttribute(string attrName) => _attrs[attrName];

        /// <summary>
        /// Returns the attribute set of the entry.
        /// All base and subtype variants of all attributes are
        /// returned. The LdapAttributeSet returned may be
        /// empty if there are no attributes in the entry.
        /// </summary>
        /// <returns>
        /// The attribute set of the entry.
        /// </returns>
        public LdapAttributeSet GetAttributeSet() => _attrs;

        /// <summary>
        /// Returns an attribute set from the entry, consisting of only those
        /// attributes matching the specified subtypes.
        /// The getAttributeSet method can be used to extract only
        /// a particular language variant subtype of each attribute,
        /// if it exists. The "subtype" may be, for example, "lang-ja", "binary",
        /// or "lang-ja;phonetic". If more than one subtype is specified, separated
        /// with a semicolon, only those attributes with all of the named
        /// subtypes will be returned. The LdapAttributeSet returned may be
        /// empty if there are no matching attributes in the entry.
        /// </summary>
        /// <param name="subtype">One or more subtype specification(s), separated
        /// with semicolons. The "lang-ja" and
        /// "lang-en;phonetic" are valid subtype
        /// specifications.</param>
        /// <returns>
        /// An attribute set from the entry with the attributes that
        /// match the specified subtypes or an empty set if no attributes
        /// match.
        /// </returns>
        public LdapAttributeSet GetAttributeSet(string subtype) => _attrs.GetSubset(subtype);
    }

    /// <summary>
    /// The name and values of one attribute of a directory entry.
    /// LdapAttribute objects are used when searching for, adding,
    /// modifying, and deleting attributes from the directory.
    /// LdapAttributes are often used in conjunction with an
    /// LdapAttributeSet when retrieving or adding multiple
    /// attributes to an entry.
    /// </summary>
    public class LdapAttribute
    {
        private readonly string _baseName; // cn of cn;lang-ja;phonetic
        private readonly string[] _subTypes; // lang-ja of cn;lang-ja
        private object[] _values; // Array of byte[] attribute values

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapAttribute"/> class.
        /// Constructs an attribute with no values.
        /// </summary>
        /// <param name="attrName">Name of the attribute.</param>
        /// <exception cref="ArgumentException">Attribute name cannot be null.</exception>
        public LdapAttribute(string attrName)
        {
            Name = attrName ?? throw new ArgumentNullException(nameof(attrName));
            _baseName = GetBaseName(attrName);
            _subTypes = GetSubtypes(attrName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapAttribute" /> class.
        /// Constructs an attribute with a single <see cref="System.String" /> value.
        /// </summary>
        /// <param name="attrName">Name of the attribute.</param>
        /// <param name="attrString">Value of the attribute as a string.</param>
        /// <exception cref="ArgumentException">Attribute value cannot be null.</exception>
        public LdapAttribute(string attrName, string attrString)
            : this(attrName)
        {
            Add(Encoding.UTF8.GetSBytes(attrString));
        }

        /// <summary>
        /// Returns the values of the attribute as an array of bytes.
        /// </summary>
        /// <value>
        /// The byte value array.
        /// </value>
        public sbyte[][] ByteValueArray
        {
            get
            {
                if (_values == null)
                    return Array.Empty<sbyte[]>();

                var size = _values.Length;
                var bva = new sbyte[size][];

                // Deep copy so application cannot change values
                for (int i = 0, u = size; i < u; i++)
                {
                    bva[i] = new sbyte[((sbyte[])_values[i]).Length];
                    Array.Copy((Array)_values[i], 0, bva[i], 0, bva[i].Length);
                }

                return bva;
            }
        }

        /// <summary>
        /// Returns the values of the attribute as an array of strings.
        /// </summary>
        /// <value>
        /// The string value array.
        /// </value>
        public string[] StringValueArray
        {
            get
            {
                if (_values == null)
                    return Array.Empty<string>();

                var size = _values.Length;
                var sva = new string[size];

                for (var j = 0; j < size; j++)
                {
                    sva[j] = Encoding.UTF8.GetString((sbyte[])_values[j]);
                }

                return sva;
            }
        }

        /// <summary>
        /// Returns the the first value of the attribute as an UTF-8 string.
        /// </summary>
        /// <value>
        /// The string value.
        /// </value>
        public string StringValue => _values == null ? null : Encoding.UTF8.GetString((sbyte[])_values[0]);

        /// <summary>
        /// Returns the first value of the attribute as a byte array or null.
        /// </summary>
        /// <value>
        /// The byte value.
        /// </value>
        public sbyte[] ByteValue
        {
            get
            {
                if (_values == null) return null;

                // Deep copy so app can't change the value
                var bva = new sbyte[((sbyte[])_values[0]).Length];
                Array.Copy((Array)_values[0], 0, bva, 0, bva.Length);

                return bva;
            }
        }

        /// <summary>
        /// Returns the language subtype of the attribute, if any.
        /// For example, if the attribute name is cn;lang-ja;phonetic,
        /// this method returns the string, lang-ja.
        /// </summary>
        /// <value>
        /// The language subtype.
        /// </value>
        public string LangSubtype => _subTypes?.FirstOrDefault(t => t.StartsWith("lang-"));

        /// <summary>
        /// Returns the name of the attribute.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        internal string Value
        {
            set
            {
                _values = null;

                Add(Encoding.UTF8.GetSBytes(value));
            }
        }

        /// <summary>
        /// Extracts the subtypes from the specified attribute name.
        /// For example, if the attribute name is cn;lang-ja;phonetic,
        /// this method returns an array containing lang-ja and phonetic.
        /// </summary>
        /// <param name="attrName">Name of the attribute from which to extract
        /// the subtypes.</param>
        /// <returns>
        /// An array subtypes or null if the attribute has none.
        /// </returns>
        /// <exception cref="ArgumentException">Attribute name cannot be null.</exception>
        public static string[] GetSubtypes(string attrName)
        {
            if (attrName == null)
            {
                throw new ArgumentException("Attribute name cannot be null");
            }

            var st = new Tokenizer(attrName, ";");
            string[] subTypes = null;
            var cnt = st.Count;

            if (cnt > 0)
            {
                st.NextToken(); // skip over basename
                subTypes = new string[cnt - 1];
                var i = 0;
                while (st.HasMoreTokens())
                {
                    subTypes[i++] = st.NextToken();
                }
            }

            return subTypes;
        }

        /// <summary>
        /// Returns the base name of the specified attribute name.
        /// For example, if the attribute name is cn;lang-ja;phonetic,
        /// this method returns cn.
        /// </summary>
        /// <param name="attrName">Name of the attribute from which to extract the
        /// base name.</param>
        /// <returns> The base name of the attribute. </returns>
        /// <exception cref="ArgumentException">Attribute name cannot be null.</exception>
        public static string GetBaseName(string attrName)
        {
            if (attrName == null)
            {
                throw new ArgumentException("Attribute name cannot be null");
            }

            var idx = attrName.IndexOf(';');
            return idx == -1 ? attrName : attrName.Substring(0, idx - 0);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public LdapAttribute Clone()
        {
            var newObj = MemberwiseClone();
            if (_values != null)
            {
                Array.Copy(_values, 0, ((LdapAttribute)newObj)._values, 0, _values.Length);
            }

            return (LdapAttribute)newObj;
        }

        /// <summary>
        /// Adds a <see cref="System.String" /> value to the attribute.
        /// </summary>
        /// <param name="attrString">Value of the attribute as a String.</param>
        /// <exception cref="ArgumentException">Attribute value cannot be null.</exception>
        public void AddValue(string attrString)
        {
            if (attrString == null)
            {
                throw new ArgumentException("Attribute value cannot be null");
            }

            Add(Encoding.UTF8.GetSBytes(attrString));
        }

        /// <summary>
        /// Adds a byte-formatted value to the attribute.
        /// </summary>
        /// <param name="attrBytes">Value of the attribute as raw bytes.
        /// Note: If attrBytes represents a string it should be UTF-8 encoded.</param>
        /// <exception cref="ArgumentException">Attribute value cannot be null.</exception>
        public void AddValue(sbyte[] attrBytes)
        {
            if (attrBytes == null)
            {
                throw new ArgumentException("Attribute value cannot be null");
            }

            Add(attrBytes);
        }

        /// <summary>
        /// Adds a base64 encoded value to the attribute.
        /// The value will be decoded and stored as bytes.  String
        /// data encoded as a base64 value must be UTF-8 characters.
        /// </summary>
        /// <param name="attrString">The base64 value of the attribute as a String.</param>
        /// <exception cref="ArgumentException">Attribute value cannot be null.</exception>
        public void AddBase64Value(string attrString)
        {
            if (attrString == null)
            {
                throw new ArgumentException("Attribute value cannot be null");
            }

            Add(Convert.FromBase64String(attrString).ToSByteArray());
        }

        /// <summary>
        /// Adds a base64 encoded value to the attribute.
        /// The value will be decoded and stored as bytes.  Character
        /// data encoded as a base64 value must be UTF-8 characters.
        /// </summary>
        /// <param name="attrString">The base64 value of the attribute as a StringBuffer.</param>
        /// <param name="start">The start index of base64 encoded part, inclusive.</param>
        /// <param name="end">The end index of base encoded part, exclusive.</param>
        /// <exception cref="ArgumentNullException">attrString.</exception>
        public void AddBase64Value(StringBuilder attrString, int start, int end)
        {
            if (attrString == null)
            {
                throw new ArgumentNullException(nameof(attrString));
            }

            Add(Convert.FromBase64String(attrString.ToString(start, end)).ToSByteArray());
        }

        /// <summary>
        /// Adds a base64 encoded value to the attribute.
        /// The value will be decoded and stored as bytes.  Character
        /// data encoded as a base64 value must be UTF-8 characters.
        /// </summary>
        /// <param name="attrChars">The base64 value of the attribute as an array of
        /// characters.</param>
        /// <exception cref="ArgumentNullException">attrChars.</exception>
        public void AddBase64Value(char[] attrChars)
        {
            if (attrChars == null)
            {
                throw new ArgumentNullException(nameof(attrChars));
            }

            Add(Convert.FromBase64CharArray(attrChars, 0, attrChars.Length).ToSByteArray());
        }

        /// <summary>
        /// Returns the base name of the attribute.
        /// For example, if the attribute name is cn;lang-ja;phonetic,
        /// this method returns cn.
        /// </summary>
        /// <returns>
        /// The base name of the attribute.
        /// </returns>
        public string GetBaseName() => _baseName;

        /// <summary>
        /// Extracts the subtypes from the attribute name.
        /// For example, if the attribute name is cn;lang-ja;phonetic,
        /// this method returns an array containing lang-ja and phonetic.
        /// </summary>
        /// <returns>
        /// An array subtypes or null if the attribute has none.
        /// </returns>
        public string[] GetSubtypes() => _subTypes;

        /// <summary>
        /// Reports if the attribute name contains the specified subtype.
        /// For example, if you check for the subtype lang-en and the
        /// attribute name is cn;lang-en, this method returns true.
        /// </summary>
        /// <param name="subtype">The single subtype to check for.</param>
        /// <returns>
        /// True, if the attribute has the specified subtype;
        /// false, if it doesn't.
        /// </returns>
        /// <exception cref="ArgumentNullException">subtype.</exception>
        public bool HasSubtype(string subtype)
        {
            if (subtype == null)
            {
                throw new ArgumentNullException(nameof(subtype));
            }

            return _subTypes != null && _subTypes.Any(t => string.Equals(t, subtype, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reports if the attribute name contains all the specified subtypes.
        /// For example, if you check for the subtypes lang-en and phonetic
        /// and if the attribute name is cn;lang-en;phonetic, this method
        /// returns true. If the attribute name is cn;phonetic or cn;lang-en,
        /// this method returns false.
        /// </summary>
        /// <param name="subtypes">An array of subtypes to check for.</param>
        /// <returns>
        /// True, if the attribute has all the specified subtypes;
        /// false, if it doesn't have all the subtypes.
        /// </returns>
        /// <exception cref="ArgumentNullException">subtypes.</exception>
        /// <exception cref="ArgumentException">subtype at array index {i}.</exception>
        public bool HasSubtypes(string[] subtypes)
        {
            if (subtypes == null)
            {
                throw new ArgumentNullException(nameof(subtypes));
            }

            for (var i = 0; i < subtypes.Length; i++)
            {
                foreach (var sub in _subTypes)
                {
                    if (sub == null)
                    {
                        throw new ArgumentException($"subtype at array index {i} cannot be null");
                    }

                    if (string.Equals(sub, subtypes[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a string value from the attribute.
        /// </summary>
        /// <param name="attrString">Value of the attribute as a string.
        /// Note: Removing a value which is not present in the attribute has
        /// no effect.</param>
        public void RemoveValue(string attrString) => RemoveValue(Encoding.UTF8.GetSBytes(attrString));

        /// <summary>
        /// Removes a byte-formatted value from the attribute.
        /// </summary>
        /// <param name="attrBytes">Value of the attribute as raw bytes.
        /// Note: If attrBytes represents a string it should be UTF-8 encoded.
        /// Note: Removing a value which is not present in the attribute has
        /// no effect.
        /// </param>
        /// <exception cref="ArgumentNullException">attrBytes.</exception>
        public void RemoveValue(sbyte[] attrBytes)
        {
            if (attrBytes == null)
            {
                throw new ArgumentNullException(nameof(attrBytes));
            }

            for (var i = 0; i < _values.Length; i++)
            {
                if (!Equals(attrBytes, (sbyte[])_values[i])) continue;

                if (i == 0 && _values.Length == 1)
                {
                    // Optimize if first element of a single valued attr
                    _values = null;
                    return;
                }

                if (_values.Length == 1)
                {
                    _values = null;
                }
                else
                {
                    var moved = _values.Length - i - 1;
                    var tmp = new object[_values.Length - 1];
                    if (i != 0)
                    {
                        Array.Copy(_values, 0, tmp, 0, i);
                    }

                    if (moved != 0)
                    {
                        Array.Copy(_values, i + 1, tmp, i, moved);
                    }

                    _values = tmp;
                }

                break;
            }
        }

        /// <summary>
        /// Returns the number of values in the attribute.
        /// </summary>
        /// <returns>
        /// The number of values in the attribute.
        /// </returns>
        public int Size() => _values?.Length ?? 0;

        /// <summary>
        /// Compares this object with the specified object for order.
        /// Ordering is determined by comparing attribute names using the method Compare() of the String class.
        /// </summary>
        /// <param name="attribute">The LdapAttribute to be compared to this object.</param>
        /// <returns>
        /// Returns a negative integer, zero, or a positive
        /// integer as this object is less than, equal to, or greater than the
        /// specified object.
        /// </returns>
        public int CompareTo(object attribute)
            => string.Compare(Name, ((LdapAttribute)attribute).Name, StringComparison.Ordinal);

        /// <summary>
        /// Returns a string representation of this LdapAttribute.
        /// </summary>
        /// <returns>
        /// a string representation of this LdapAttribute.
        /// </returns>
        /// <exception cref="Exception">NullReferenceException.</exception>
        public override string ToString()
        {
            var result = new StringBuilder("LdapAttribute: ");

            result.Append("{type='" + Name + "'");

            if (_values != null)
            {
                result
                    .Append(", ")
                    .Append(_values.Length == 1 ? "value='" : "values='");

                for (var i = 0; i < _values.Length; i++)
                {
                    if (i != 0)
                    {
                        result.Append("','");
                    }

                    if (((sbyte[])_values[i]).Length == 0)
                    {
                        continue;
                    }

                    var sval = Encoding.UTF8.GetString((sbyte[])_values[i]);
                    if (sval.Length == 0)
                    {
                        // didn't decode well, must be binary
                        result.Append($"<binary value, length:{sval.Length}");
                        continue;
                    }

                    result.Append(sval);
                }

                result.Append("'");
            }

            result.Append("}");

            return result.ToString();
        }
        
        private static bool Equals(IReadOnlyList<sbyte> e1, IReadOnlyList<sbyte> e2)
        {
            // If same object, they compare true
            if (object.Equals(e1, e2))
                return true;

            // If either but not both are null, they compare false
            if (e1 == null || e2 == null)
                return false;

            // If arrays have different length, they compare false
            var length = e1.Count;
            if (e2.Count != length)
                return false;

            // If any of the bytes are different, they compare false
            for (var i = 0; i < length; i++)
            {
                if (e1[i] != e2[i])
                    return false;
            }

            return true;
        }

        private void Add(IReadOnlyList<sbyte> bytes)
        {
            if (_values == null)
            {
                _values = new object[] { bytes };
            }
            else
            {
                // Duplicate attribute values not allowed
                if (_values.Any(t => Equals(bytes, (sbyte[])t)))
                {
                    return; // Duplicate, don't add
                }

                var tmp = new object[_values.Length + 1];
                Array.Copy(_values, 0, tmp, 0, _values.Length);
                tmp[_values.Length] = bytes;
                _values = tmp;
            }
        }
    }

    /// <summary>
    /// A set of LdapAttribute objects.
    /// An LdapAttributeSet is a collection of LdapAttribute
    /// classes as returned from an LdapEntry on a search or read
    /// operation. LdapAttributeSet may be also used to construct an entry
    /// to be added to a directory.  
    /// </summary>
    /// <seealso cref="LdapAttribute"></seealso>
    /// <seealso cref="LdapEntry"></seealso>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class LdapAttributeSet : Dictionary<string, LdapAttribute>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LdapAttributeSet"/> class.
        /// </summary>
        public LdapAttributeSet()
        : base(StringComparer.OrdinalIgnoreCase)
        {
            // placeholder
        }

        /// <summary>
        /// Creates a new attribute set containing only the attributes that have
        /// the specified subtypes.
        /// For example, suppose an attribute set contains the following
        /// attributes:
        /// <ul><li>    cn</li><li>    cn;lang-ja</li><li>    sn;phonetic;lang-ja</li><li>    sn;lang-us</li></ul>
        /// Calling the <c>getSubset</c> method and passing lang-ja as the
        /// argument, the method returns an attribute set containing the following
        /// attributes:.
        /// <ul><li>cn;lang-ja</li><li>sn;phonetic;lang-ja</li></ul>
        /// </summary>
        /// <param name="subtype">Semi-colon delimited list of subtypes to include. For
        /// example:
        /// <ul><li> "lang-ja" specifies only Japanese language subtypes</li><li> "binary" specifies only binary subtypes</li><li>
        /// "binary;lang-ja" specifies only Japanese language subtypes
        /// which also are binary
        /// </li></ul>
        /// Note: Novell eDirectory does not currently support language subtypes.
        /// It does support the "binary" subtype.</param>
        /// <returns>
        /// An attribute set containing the attributes that match the
        /// specified subtype.
        /// </returns>
        public LdapAttributeSet GetSubset(string subtype)
        {
            // Create a new tempAttributeSet
            var tempAttributeSet = new LdapAttributeSet();

            foreach (var kvp in this)
            {
                if (kvp.Value.HasSubtype(subtype))
                    tempAttributeSet.Add(kvp.Value.Clone());
            }

            return tempAttributeSet;
        }

        /// <summary>
        /// Returns <c>true</c> if this set contains an attribute of the same name
        /// as the specified attribute.
        /// </summary>
        /// <param name="attr">Object of type <c>LdapAttribute</c>.</param>
        /// <returns>
        /// true if this set contains the specified attribute.
        /// </returns>
        public bool Contains(object attr) => ContainsKey(((LdapAttribute)attr).Name);

        /// <summary>
        /// Adds the specified attribute to this set if it is not already present.
        /// If an attribute with the same name already exists in the set then the
        /// specified attribute will not be added.
        /// </summary>
        /// <param name="attr">Object of type <c>LdapAttribute</c>.</param>
        /// <returns>
        /// <c>true</c> if the attribute was added.
        /// </returns>
        public bool Add(LdapAttribute attr)
        {
            var name = attr.Name;

            if (ContainsKey(name))
                return false;

            this[name] = attr;
            return true;
        }

        /// <summary>
        /// Removes the specified object from this set if it is present.
        /// If the specified object is of type <c>LdapAttribute</c>, the
        /// specified attribute will be removed.  If the specified object is of type
        /// string, the attribute with a name that matches the string will
        /// be removed.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>
        /// true if the object was removed.
        /// </returns>
        public bool Remove(LdapAttribute entry) => Remove(entry.Name);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var retValue = new StringBuilder("LdapAttributeSet: ");
            var first = true;

            foreach (var attr in this)
            {
                if (!first)
                {
                    retValue.Append(" ");
                }

                first = false;
                retValue.Append(attr);
            }

            return retValue.ToString();
        }
    }
}