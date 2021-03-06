﻿using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public partial struct Variant : IEquatable<Variant>, IFormattable {

        /// <summary>
        /// Maps from type to variant type.
        /// </summary>
        public static IReadOnlyDictionary<Type, VariantType> VariantTypeMap { get; } = new System.Collections.ObjectModel.ReadOnlyDictionary<Type, VariantType>(new Dictionary<Type, VariantType>() {
            [typeof(bool)] = VariantType.Boolean,
            [typeof(byte)] = VariantType.Byte,
            [typeof(DateTime)] = VariantType.DateTime,
            [typeof(double)] = VariantType.Double,
            [typeof(EncodedObject)] = VariantType.ExtensionObject,
            [typeof(float)] = VariantType.Float,
            [typeof(short)] = VariantType.Int16,
            [typeof(int)] = VariantType.Int32,
            [typeof(long)] = VariantType.Int64,
            [typeof(sbyte)] = VariantType.SByte,
            [typeof(string)] = VariantType.String,
            [typeof(TimeSpan)] = VariantType.TimeSpan,
            [typeof(ushort)] = VariantType.UInt16,
            [typeof(uint)] = VariantType.UInt32,
            [typeof(ulong)] = VariantType.UInt64,
            [typeof(Uri)] = VariantType.Url
        });

        /// <summary>
        /// Default string format to use for date-time variant values (ISO 8601-1:2019 extended profile).
        /// </summary>
        public const string DefaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        /// <summary>
        /// Default string format to use for double-precision variant values.
        /// </summary>
        public const string DefaultDoubleFormat = "G17";

        /// <summary>
        /// Default string format to use for single-precision variant values.
        /// </summary>
        public const string DefaultFloatFormat = "G9";

        /// <summary>
        /// Default string format to use for integral variant values.
        /// </summary>
        public const string DefaultIntegralFormat = "G";

        /// <summary>
        /// Null variant.
        /// </summary>
        public static Variant Null { get; } = new Variant(null, VariantType.Null, null);

        /// <summary>
        /// The value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The variant type.
        /// </summary>
        public VariantType Type { get; }

        /// <summary>
        /// If <see cref="Value"/> is an array, <see cref="ArrayDimensions"/> defines the 
        /// dimensions of the array. For all other values, this property will be 
        /// <see langword="null"/>.
        /// </summary>
        public int[]? ArrayDimensions { get; }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object. It is preferable to call <see cref="FromValue"/> 
        /// instead of calling the constructor directly, as this allows the <see cref="Type"/> of 
        /// the variant to be inferred from the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="type">
        ///   The variant type.
        /// </param>
        /// <param name="arrayDimensions">
        ///   The array dimensions of the value, if it is an array.
        /// </param>
        internal Variant(object? value, VariantType type, int[]? arrayDimensions) {
            Value = value;
            Type = type;
            ArrayDimensions = arrayDimensions;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object from the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is not a supported type. See <see cref="VariantTypeMap"/> 
        ///   for supported types.
        /// </exception>
        /// <remarks>
        ///   
        /// <para>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </para>
        /// 
        /// <para>
        ///   If <paramref name="value"/> is a <see cref="Variant"/>, the new <see cref="Variant"/> 
        ///   will be a copy of the existing <paramref name="value"/>.
        /// </para>
        /// 
        /// <para>
        ///   For any other type, the type of <paramref name="value"/> must have an entry in the 
        ///   <see cref="VariantTypeMap"/> mapping. If <paramref name="value"/> is an <see cref="Array"/> 
        ///   type, the element type of the array must have an entry in the <see cref="VariantTypeMap"/> 
        ///   mapping.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="VariantTypeMap"/>
        public Variant(object? value) {
            if (value == null) {
                Value = Null.Value;
                Type = Null.Type;
                ArrayDimensions = Null.ArrayDimensions;
                return;
            }

            if (value is Variant v) {
                Value = v.Value;
                Type = v.Type;
                ArrayDimensions = v.ArrayDimensions;
                return;
            }

            VariantType variantType;
            int[]? arrayDimensions = null;

            if (value is Array a) {
                GetArraySettings(a, out variantType, out arrayDimensions);
            }
            else {
                var valueType = value.GetType();
                if (!TryGetVariantType(valueType, out variantType)) {
                    throw new ArgumentOutOfRangeException(nameof(value), valueType, SharedResources.Error_TypeIsUnsupported);
                }
            }

            Value = value;
            Type = variantType;
            ArrayDimensions = arrayDimensions;
        }




        /// <summary>
        /// Creates a new <see cref="Variant"/> object from the specified array.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="value"/> is not a supported type. See 
        ///   <see cref="VariantTypeMap"/> for supported types.
        /// </exception>
        /// <remarks>
        ///   
        /// <para>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </para>
        /// 
        /// <para>
        ///   For any other type, the type of <paramref name="value"/> must have an entry in the 
        ///   <see cref="VariantTypeMap"/> mapping. If <paramref name="value"/> is an <see cref="Array"/> 
        ///   type, the element type of the array must have an entry in the <see cref="VariantTypeMap"/> 
        ///   mapping.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="VariantTypeMap"/>
        public Variant(Array? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            GetArraySettings(value!, out var variantType, out var arrayDimensions);

            Value = value;
            Type = variantType;
            ArrayDimensions = arrayDimensions;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(bool value) {
            Value = value;
            Type = VariantType.Boolean;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(bool[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Boolean;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(byte value) {
            Value = value;
            Type = VariantType.Byte;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(byte[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Byte;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(DateTime value) {
            Value = value;
            Type = VariantType.DateTime;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(DateTime[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.DateTime;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(double value) {
            Value = value;
            Type = VariantType.Double;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(double[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Double;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(float value) {
            Value = value;
            Type = VariantType.Float;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(float[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Float;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(int value) {
            Value = value;
            Type = VariantType.Int32;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(int[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Int32;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(long value) {
            Value = value;
            Type = VariantType.Int64;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(long[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Int64;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(sbyte value) {
            Value = value;
            Type = VariantType.SByte;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(sbyte[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.SByte;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(short value) {
            Value = value;
            Type = VariantType.Int16;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(short[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Int16;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(string? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.String;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(string[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.String;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(TimeSpan value) {
            Value = value;
            Type = VariantType.TimeSpan;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(TimeSpan[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.TimeSpan;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(uint value) {
            Value = value;
            Type = VariantType.UInt32;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(uint[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.UInt32;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(ulong value) {
            Value = value;
            Type = VariantType.UInt64;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(ulong[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.UInt64;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(ushort value) {
            Value = value;
            Type = VariantType.UInt16;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(ushort[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.UInt16;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(Uri? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Url;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(Uri[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.Url;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        public Variant(EncodedObject? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.ExtensionObject;
            ArrayDimensions = null;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> instance with the specified array value.
        /// </summary>
        /// <param name="value">
        ///   The array value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is <see langword="null"/>, the <see cref="Variant"/> 
        ///   will be equal to <see cref="Null"/>.
        /// </remarks>
        public Variant(EncodedObject[]? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            Value = value;
            Type = VariantType.ExtensionObject;
            ArrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Gets the <see cref="VariantType"/> and array dimensions for the specified array.
        /// </summary>
        /// <param name="value">
        ///   The array.
        /// </param>
        /// <param name="variantType">
        ///   The <see cref="VariantType"/> for the array's element type.
        /// </param>
        /// <param name="arrayDimensions">
        ///   The dimensions of the array.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="value"/> is not a supported type. See 
        ///   <see cref="VariantTypeMap"/> for supported types.
        /// </exception>
        private static void GetArraySettings(Array value, out VariantType variantType, out int[] arrayDimensions) {
            Type? elemType = value.GetType().GetElementType();
            if (elemType == null || !TryGetVariantType(elemType, out variantType)) {
                throw new ArgumentOutOfRangeException(nameof(value), elemType, SharedResources.Error_ArrayElementTypeIsUnsupported);
            }

            arrayDimensions = GetArrayDimensions(value);
        }


        /// <summary>
        /// Gets the dimensions of the specified array.
        /// </summary>
        /// <param name="value">
        ///   The array.
        /// </param>
        /// <returns>
        ///   The array dimensions.
        /// </returns>
        private static int[] GetArrayDimensions(Array value) {
            var arrayDimensions = new int[value.Rank];
            for (var i = 0; i < value.Rank; i++) {
                arrayDimensions[i] = value.GetLength(i);
            }

            return arrayDimensions;
        } 


        /// <summary>
        /// Tries to get the <see cref="VariantType"/> for the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="variantType">
        ///   The <see cref="VariantType"/> that the <paramref name="type"/> maps to.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="type"/> has an equivalent <see cref="VariantType"/>, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetVariantType(Type type, out VariantType variantType) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsArray) {
                var elementType = type.GetElementType();
                if (elementType == null) {
                    variantType = VariantType.Unknown;
                    return false;
                }
                return VariantTypeMap.TryGetValue(elementType, out variantType);
            }

            return VariantTypeMap.TryGetValue(type, out variantType);
        }


        /// <summary>
        /// Tests if an instance of the specified <see cref="Type"/> can be specified as the value 
        /// of a <see cref="Variant"/> instance.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if an instance of the <paramref name="type"/> can be specified 
        ///   as the value of a <see cref="Variant"/> instance, or <see langword="false"/> 
        ///   otherwise.         
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsSupportedValueType(Type type) {
            return TryGetVariantType(type, out var _);
        }


        /// <summary>
        /// Tests if the specified variant has a type of <see cref="VariantType.Null"/> or a 
        /// <see langword="null"/> value.
        /// </summary>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the variant type is <see cref="VariantType.Null"/> or the 
        ///   variant value is <see langword="null"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsNull(Variant variant) {
            return variant.Type == VariantType.Null || variant.Value == null;
        }



        /// <summary>
        /// Creates a new <see cref="Variant"/> object that infers the <see cref="VariantType"/> 
        /// from the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is a <see cref="Variant"/>, it will be returned 
        ///   unmodified.
        /// </remarks>
        public static Variant FromValue(object? value) {
            if (value == null) {
                return Null;
            }

            if (value is Variant v) {
                return v;
            }

            if (value is Array a) {
                return new Variant(a);
            }

            return new Variant(value);
        }


        /// <summary>
        /// Gets the default string format to use for the specified <see cref="VariantType"/>.
        /// </summary>
        /// <param name="type">
        ///   The <see cref="VariantType"/>.
        /// </param>
        /// <returns>
        ///   The default format to use when <see cref="ToString()"/> is called.
        /// </returns>
        public static string GetDefaultFormat(VariantType type) {
            string format = null!;

            // Special handling for some types to ensure correct round-tripping. 
            // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings

            switch (type) {
                case VariantType.Double:
                    format = DefaultDoubleFormat;
                    break;
                case VariantType.Float:
                    format = DefaultFloatFormat;
                    break;
                case VariantType.Byte:
                case VariantType.Int16:
                case VariantType.Int32:
                case VariantType.Int64:
                case VariantType.SByte:
                case VariantType.UInt16:
                case VariantType.UInt32:
                case VariantType.UInt64:
                    format = DefaultIntegralFormat;
                    break;
                case VariantType.DateTime:
                    format = DefaultDateTimeFormat;
                    break;
            }

            return format;
        }


        /// <summary>
        ///   Formats the value of the current instance.
        /// </summary>
        /// <returns>
        ///   The formatted value.
        /// </returns>
        public override string ToString() {
            return ToString(GetDefaultFormat(Type), null!);
        }


        /// <summary>
        /// Formats the value of the current instance using the specified format.
        /// </summary>
        /// <param name="format">
        ///   The format to use.
        /// </param>
        /// <returns>
        ///   The formatted value.
        /// </returns>
        public string ToString(string format) {
            return ToString(format, null!);
        }


        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider) {
            if (Value == null) {
                return null!;
            }
            if (Value is string s) {
                return s;
            }

            if (Value is Array a) {
                return a.ToString();
            }

            try {
                return (format != null && Value is IFormattable formattable)
                    ? formattable.ToString(format, formatProvider)
                    : Value?.ToString()!;
            }
            catch {
                return Value?.ToString()!;
            }
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
#if NETSTANDARD2_0 || NET46
            return HashGenerator.Combine(Type, Value);
#else
            return HashCode.Combine(Type, Value);
#endif
        }


        /// <inheritdoc/>
        public override bool Equals(object? obj) {
            if (obj is not Variant v) {
                return false;
            }

            return Equals(v);
        }


        /// <inheritdoc/>
        public bool Equals(Variant other) {
            return other.Type == Type && Equals(other.Value, Value);
        }

    }

}
