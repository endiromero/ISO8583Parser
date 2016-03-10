using System;
using System.IO;
using System.Text;

namespace Fintec.Iso8583 {

	/// <summary>
	/// Stores a value contained in a field inside a Message.
	/// </summary>
	public class IsoValue : ICloneable {
		private readonly IsoType _type;
		private int _length;
		private readonly object _fval;

		/// <summary> Creates a new instance to store a value of a fixed-length type. Fixed-length types are DATE4, DATE_EXP, DATE10, TIME, AMOUNT. </summary>
		/// <param name="t">The <see cref="IsoType"/> (8583) type of the value that is going to be stored.</param>
		/// <param name="value">The value to store.s</param>
		public IsoValue(IsoType t, object value) {
			if (value == null) {
				throw new ArgumentException("Value cannot be null");
			}
			if (IsoTypeHelper.NeedsLength(t)) {
				throw new ArgumentException("Use IsoValue constructor for Fixed-value types");
			}
			_type = t;
			_fval = value;
            if (t == IsoType.LLVAR || _type == IsoType.LLLVAR || t == IsoType.LLVARnp || _type == IsoType.LLLVARnp)
            {
				_length = value.ToString().Length;
			} else {
				_length = IsoTypeHelper.GetLength(t);
			}
		}

		/// <summary>
		/// Creates a new instance to store a value of a given type. This constructor
		/// is used for variable-length types (LLVAR, LLLVAR, ALPHA, NUMERIC) -
		/// variable in the sense that that length of the value does not depend
		/// solely on the ISO type.
		/// </summary>
		/// <param name="t">the <see cref="IsoType"/> ISO8583 type of the value to be stored.</param>
		/// <param name="val">The value to be stored.</param>
		/// <param name="len">The length of the field.</param>
		public IsoValue(IsoType t, object val, int len) {
			if (val == null) {
				throw new ArgumentException("Value cannot be null");
			}
			_type = t;
			_fval = val;
			_length = len;
			if (_length < 0 && IsoTypeHelper.NeedsLength(t)) {
				throw new ArgumentException("Length must be greater than zero");
			} else if (t == IsoType.LLVAR || t == IsoType.LLLVAR) {
				_length = val.ToString().Length;
                //@@@ TAM
			} else if (t == IsoType.LLVARnp || t == IsoType.LLLVARnp) {
				_length = val.ToString().Length * 2;
			}
		}

		/// <summary>
		/// The ISO8583 type of the value stored in the receiver.
		/// </summary>
		public IsoType Type {
			get { return _type; }
		}

		/// <summary>
		/// The length of the field.
		/// </summary>
		public int Length {
			get { return _length; }
		}

		/// <summary>
		/// The value stored in the receiver.
		/// </summary>
		public object Value {
			get { return _fval; }
		}

		public override bool Equals(object other) {
			if (other == null || !(other is IsoValue)) {
				return false;
			}
			IsoValue comp = (IsoValue)other;
			return (comp.Type == _type && comp.Value.Equals(_fval) && comp.Length == _length);
		}

		public override int GetHashCode() {
			return _fval.GetHashCode();
		}

		/// <summary>
		/// Returns the string representation of the stored value, which
		/// varies a little depending on its type: NUMERIC and ALPHA values
		/// are returned padded to the specified length of the field either
		/// with zeroes or spaces; AMOUNT values are formatted to 12 digits
		/// as cents; date/time values are returned with the specified format
		/// for their type. LLVAR and LLLVAR values are returned as they are.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {

			if (_type == IsoType.NUMERIC || _type == IsoType.ALPHA) {
				return IsoTypeHelper.Format(_fval.ToString(), _type, _length);
			} else if (_type == IsoType.AMOUNT) {
				if (_fval is decimal) {
					return IsoTypeHelper.Format((decimal)_fval, _type, 12);
				} else {
					return IsoTypeHelper.Format(Convert.ToDecimal(_fval), _type, 12);
				}
			} else if (_fval is DateTime) {
				return IsoTypeHelper.Format((DateTime)_fval, _type);
                //TAM ADDED to PAD F an the end of Packed VAR 
            } else if (_type == IsoType.LLVAR || _type == IsoType.LLLVAR) {
                return IsoTypeHelper.Format(_fval.ToString(), _type, _length);
            }
			return _fval.ToString();
		}

		/// <summary>
		/// Writes the stored value to a stream, preceded by the length header
		/// in case of LLVAR or LLLVAR values.
		/// </summary>
		/// <param name="outs"></param>
        /// //@@@@TAM DO the non packed (ascii) LLVARnp 
		public void Write(Stream outs) {
			//TODO binary encoding is pending
			string v = ToString();
			if (_type == IsoType.LLVAR || _type == IsoType.LLLVAR) {
                //@@@@ TAM
                _length = v.Length;
                try
                {
                    if ( v.Substring(v.Length - 1).ToUpper().Equals("F") ) {
                        _length = v.Length - 1;
                    }
                }
                catch (Exception e) { }

				
				if (_length > 100) {
					outs.WriteByte((byte)((_length / 100) + 48));
				} else if (_type == IsoType.LLLVAR) {
					outs.WriteByte(48);
				}
				if (_length >= 10) {
					outs.WriteByte((byte)(((_length % 100) / 10) + 48));
				} else {
					outs.WriteByte(48);
				}
				outs.WriteByte((byte)((_length % 10) + 48));
			}
			byte[] buf = Encoding.ASCII.GetBytes(v);
			outs.Write(buf, 0, buf.Length);
		}

		public object Clone() {
			return MemberwiseClone();
		}

	}

}
