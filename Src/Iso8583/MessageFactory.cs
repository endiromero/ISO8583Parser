using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Fintec.Iso8583
{

	/// <summary>
	/// This class can create ISO8583 messages from a template defined in a configuration
	/// file, or from a byte buffer that was read from a stream. For messages created
	/// from templates, it can set the current DateTime in field 4 and also use
	/// an ITraceGenerator to assign the new message a new trace number.
	/// </summary>
	public class MessageFactory
	{

		private readonly Dictionary<int, IsoMessage> _typeTemplates = new Dictionary<int, IsoMessage>();
		private readonly Dictionary<int, Dictionary<int, FieldParseInfo>> _parseMap = new Dictionary<int, Dictionary<int, FieldParseInfo>>();
		private readonly Dictionary<int, List<int>> _parseOrder = new Dictionary<int, List<int>>();

		private ITraceGenerator _traceGen;

		private readonly Dictionary<int, string> _isoHeaders = new Dictionary<int, string>();
		private bool _setDate;
		private int _etx = -1;

		/// <summary>
		/// The ETX character to assign to new messages. The default is -1 which
		/// means not to use an ETX character.
		/// </summary>
		public int Etx
		{
			set { _etx = value; }
			get { return _etx; }
		}

		/// <summary>
		/// Tells the MessageFactory whether it should assign the date
		/// to newly created messages or not. Default is false. The
		/// date is field 7.
		/// </summary>
		public bool AssignDate
		{
			set { _setDate = value; }
			get { return _setDate; }
		}

		/// <summary>
		/// If a TraceGenerator is set then it is used to assign
		/// a new message trace to every message created by the factory.
		/// Default is null. The trace is a 6-digit number in field 11.
		/// </summary>
		public ITraceGenerator TraceGenerator
		{
			set { _traceGen = value; }
			get { return _traceGen; }
		}

		/// <summary>
		/// Sets the ISO message header to use when creating messages of the given
		/// type.
		/// </summary>
		/// <param name="type">The ISO message type (0x200, 0x400, etc)</param>
		/// <param name="value">The ISO header to use, or null to not use a header for the message type.</param>
		public void SetIsoHeader(int type, string value)
		{
			if (value == null)
			{
				_isoHeaders.Remove(type);
			}
			else {
				_isoHeaders[type] = value;
			}
		}
		/// <summary>
		/// Returns the ISO message header for the given message type.
		/// </summary>
		/// <param name="type">The ISO message type (0x200, 0x400, etc)</param>
		public string GetIsoHeader(int type)
		{
			return _isoHeaders[type];
		}

		/// <summary>
		/// Stores the given message as a template to create new messages of the
		/// same type as it. It overwrites any previously stored templates for
		/// the same message type.
		/// </summary>
		/// <param name="templ">A message to be used as a template.</param>
		public void AddMessageTemplate(IsoMessage templ)
		{
			if (templ != null)
			{
				_typeTemplates[templ.Type] = templ;
			}
		}
		/// <summary>
		/// Removes the template for the given message type.
		/// </summary>
		/// <param name="type">The ISO message type (0x200, 0x400, etc)</param>
		public void RemoveMessageTemplate(int type)
		{
			_typeTemplates.Remove(type);
		}

		/// <summary>
		/// Sets a dictionary with the necessary information to parse a message
		/// of the given type. The dictionary contains FieldParseInfo objects
		/// under the field index they correspond to.
		/// </summary>
		/// <param name="type">The ISO message type (0x200, 0x400, etc)</param>
		/// <param name="dict">The FieldParseInfo objects for parsing individual fields under the field index they correspond to.</param>
		public void SetParseDictionary(int type, Dictionary<int, FieldParseInfo> dict)
		{
			_parseMap[type] = dict;
			List<int> index = new List<int>();

			//// Add all the keys of the dict to index
			//// sort index by numbers
			for (int i = 2; i < 129; i++)
			{
				if (dict.ContainsKey(i))
				{
					index.Add(i);
				}
			}

			_parseOrder[type] = index;
		}

		/// <summary>
		/// Creates a new message of the given type. If there is a template
		/// for the message type, then it is used to set all the values in the
		/// new message (the values will be copied from the original messages,
		/// not referred to directly, to avoid affecting the templates if a value
		/// in a message created this way is modified). If the factory has an
		/// ITraceGenerator set, it uses it to assign a new trace number as a
		/// NUMERIC value of length 6 in field 11; if AssignDate is true,
		/// then the current DateTime is stored in field 7 as a DATE10 type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IsoMessage NewMessage(int type)
		{
			IsoMessage m = new IsoMessage(_isoHeaders[type])
			{
				Type = type,
				Etx = Etx
			};

			IsoMessage templ = _typeTemplates[type];
			if (templ != null)
			{
				for (int i = 2; i < 128; i++)
				{
					if (templ.HasField(i))
					{
						m.SetField(i, (IsoValue)templ.GetField(i).Clone());
					}
				}
			}

			if (TraceGenerator != null)
			{
				m.SetValue(11, TraceGenerator.NextTrace(), IsoType.NUMERIC, 6);
			}
			if (AssignDate)
			{
				m.SetValue(7, DateTime.Now, IsoType.DATE10, 10);
			}
			return m;
		}

		/// <summary>
		/// Creates a response for the specified request, by creating a new
		/// message with a message type of the original request type plus 16.
		/// If there is a template for the resulting type, its values are copied
		/// onto the new message; after that, all the values from the original
		/// request that are not already in the response are copied to it.
		/// </summary>
		/// <param name="request">An ISO8583 request.</param>
		/// <returns>A new ISO8583 message with the corresponding response
		/// type for the request and with values already copied from its
		/// template (if any) and the request.</returns>
		public IsoMessage CreateResponse(IsoMessage request)
		{
			IsoMessage resp = new IsoMessage(_isoHeaders[request.Type + 16])
			{
				Type = request.Type + 16,
				Etx = _etx
			};

			IsoMessage templ = _typeTemplates[resp.Type];
			if (templ != null)
			{
				for (int i = 2; i < 128; i++)
				{
					if (templ.HasField(i))
					{
						resp.SetField(i, (IsoValue)templ.GetField(i).Clone());
					}
				}
			}
			for (int i = 2; i < 128; i++)
			{
				if (request.HasField(i))
				{
					resp.SetField(i, (IsoValue)request.GetField(i).Clone());
				}
			}
			return resp;
		}

		/// <summary>
		/// Parses a buffer containing a message, considering the specified
		/// length as a prefix for the ISO header, using UTF8 encoding.
		/// </summary>
		/// <param name="buf">The buffer containing the message.</param>
		/// <param name="isoHeaderLength">The length of the ISO header.</param>
		/// <returns>The parsed message.</returns>
		public IsoMessage ParseMessage(byte[] buf, int isoHeaderLength)
		{
			return ParseMessage(buf, isoHeaderLength, Encoding.UTF8);
		}

		/// <summary>
		/// Parses a byte buffer containing an ISO8583 message. The buffer must
		/// not include the length header. If it includes the ISO message header,
		/// then its length must be specified so the message type can be found.
		/// </summary>
		/// <param name="buf">The byte buffer containing the message, starting
		/// at the ISO header or the message type.</param>
		/// <param name="isoHeaderLength">Specifies the position at which the message
		/// type is located, which is algo the length of the ISO header.</param>
		/// <param name="encoder">The encoder to use for reading string values.</param>
		/// <returns>The parsed message.</returns>
		public IsoMessage ParseMessage(byte[] buf, int isoHeaderLength, Encoding encoder)
		{
			IsoMessage m = new IsoMessage(isoHeaderLength > 0 ? encoder.GetString(buf, 0, isoHeaderLength) : null);
			int type = ((buf[isoHeaderLength] - 48) << 12)
			| ((buf[isoHeaderLength + 1] - 48) << 8)
			| ((buf[isoHeaderLength + 2] - 48) << 4)
			| (buf[isoHeaderLength + 3] - 48);
			m.Type = type;

			//Parse the bitmap
			bool extended = (HexByteValue(buf[isoHeaderLength + 4]) & 8) > 0;
			BitArray bs = new BitArray(extended ? 128 : 64);
			int pos = 0;
			for (int i = isoHeaderLength + 4; i < isoHeaderLength + 20; i++)
			{
				int hex = HexByteValue(buf[i]);
				bs.Set(pos++, (hex & 8) > 0);
				bs.Set(pos++, (hex & 4) > 0);
				bs.Set(pos++, (hex & 2) > 0);
				bs.Set(pos++, (hex & 1) > 0);
			}
			//Extended bitmap?
			if (bs.Get(0))
			{
				for (int i = isoHeaderLength + 20; i < isoHeaderLength + 36; i++)
				{
					int hex = HexByteValue(buf[i]);
					bs.Set(pos++, (hex & 8) > 0);
					bs.Set(pos++, (hex & 4) > 0);
					bs.Set(pos++, (hex & 2) > 0);
					bs.Set(pos++, (hex & 1) > 0);
				}
				pos = 36 + isoHeaderLength;
			}
			else {
				pos = 20 + isoHeaderLength;
			}

			//Parse each field
			Dictionary<int, FieldParseInfo> guide = _parseMap[type];
			List<int> index = _parseOrder[type];

			foreach (int i in index)
			{
				FieldParseInfo fpi = guide[i];
				if (i <= bs.Count)
				{ // TM extended parsing

					if (bs.Get(i - 1))
					{
						IsoValue val = fpi.Parse(buf, pos, encoder);
						m.SetField(i, val);
						pos += val.Length;
						if (val.Type == IsoType.LLVAR || val.Type == IsoType.LLVARnp)
						{
							pos += 2;
							//@@@ TAM temporary hardcode, must be a better way to parse Padding F
							if (val.Type == IsoType.LLVAR && (val.Length % 2 != 0))
							{
								pos += 1;
							}

						}
						else if (val.Type == IsoType.LLLVAR)
						{
							//@@@ TAM packe 3 change to  4 for Paymark
							//pos += 3;
							pos += 4;
						}
						else if (val.Type == IsoType.LLLVARnp) // for NAB the LLLVAR Non packed(ascci codec) will have HEX as length so 6 digits(303139 = 019)
						{
							pos += 6;
						}
					}
				}
			}
			return m;
		}

		/// <summary>
		/// Parses a byte containing a Hex representation (a digit from 0 to 9
		/// or a letter from A to F upper or lower case), and returns the value
		/// which can be from 0 to 15 inclusive.
		/// </summary>
		/// <param name="val">The byte containing the Hex representation of a nibble.</param>
		/// <returns>The real value of the nibble, between 0 and 15 inclusive.</returns>
		/// <exception cref="ArgumentException">When the value is not a valid Hex rep of a nibble.</exception>
		public static int HexByteValue(byte val)
		{
			short what = Convert.ToInt16(val);
			if (val > 47 && val < 58)
			{
				return val - 48;
				//TAM Fixed Bug 70 102 to 71 103
			}
			else if ((val > 64 && val < 71) || (val > 96 && val < 103))
			{
				return val - 55;
			}
			throw new ArgumentException("The byte is not a valid hex nibble rep", nameof(val));
		}

	}

}
