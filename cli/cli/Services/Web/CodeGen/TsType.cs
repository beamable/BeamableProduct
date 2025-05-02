namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript type expression.
/// All concrete variants live as private nested classes;
/// Use the static factory helpers and primitives below to build them.
/// </summary>
public abstract class TsType : TsNode
{
	/// <summary>
	/// The type of the value <c>any</c>.
	/// </summary>
	/// <returns>A type expression for the given any type.</returns>
	/// <example><c>TsType.Any</c> is equivalent to <c>TsType.Of("any")</c> and equals <c>any</c>.</example>
	public static TsType Any => Of("any");

	/// <summary>
	/// The type of the value <c>undefined</c>.
	/// </summary>
	/// <returns>A type expression for the given unknown type.</returns>
	/// <example><c>TsType.Unknown</c> is equivalent to <c>TsType.Of("unknown")</c> and equals <c>unknown</c>.</example>
	public static TsType Unknown => Of("unknown");

	/// <summary>
	/// The type of the value <c>never</c>.
	/// </summary>
	/// <returns>A type expression for the given never type.</returns>
	/// <example><c>TsType.Never</c> is equivalent to <c>TsType.Of("never")</c> and equals <c>never</c>.</example>
	public static TsType Never => Of("never");

	/// <summary>
	/// The type of the value <c>void</c>.
	/// </summary>
	/// <returns>A type expression for the given void type.</returns>
	/// <example><c>TsType.Void</c> is equivalent to <c>TsType.Of("void")</c> and equals <c>void</c>.</example>
	public static TsType Void => Of("void");

	/// <summary>
	/// The type of the value <c>undefined</c>.
	/// </summary>
	/// <returns>A type expression for the given undefined type.</returns>
	/// <example>
	/// <c>TsType.Undefined</c> is equivalent to <c>TsType.Of("undefined")</c> and equals <c>undefined</c>.
	/// </example>
	public static TsType Undefined => Of("undefined");

	/// <summary>
	/// The type of the value <c>null</c>.
	/// </summary>
	/// <returns>A type expression for the given null type.</returns>
	/// <example><c>TsType.Null</c> is equivalent to <c>TsType.Of("null")</c> and equals <c>null</c>.</example>
	public static TsType Null => Of("null");

	/// <summary>
	/// The type of the value <c>boolean</c>.
	/// </summary>
	/// <returns>A type expression for the given boolean type.</returns>
	/// <example><c>TsType.Boolean</c> is equivalent to <c>TsType.Of("boolean")</c> and equals <c>boolean</c>.</example>
	public static TsType Boolean => Of("boolean");

	/// <summary>
	/// The type of the value <c>number</c>.
	/// </summary>
	/// <returns>A type expression for the given number type.</returns>
	/// <example><c>TsType.Number</c> is equivalent to <c>TsType.Of("number")</c> and equals <c>number</c>.</example>
	public static TsType Number => Of("number");

	/// <summary>
	/// The type of the value <c>string</c>.
	/// </summary>
	/// <returns>A type expression for the given string type.</returns>
	/// <example><c>TsType.String</c> is equivalent to <c>TsType.Of("string")</c> and equals <c>string</c>.</example>
	public static TsType String => Of("string");

	/// <summary>
	/// The type of the value <c>symbol</c>.
	/// </summary>
	/// <returns>A type expression for the given symbol type.</returns>
	/// <example><c>TsType.Symbol</c> is equivalent to <c>TsType.Of("symbol")</c> and equals <c>symbol</c>.</example>
	public static TsType Symbol => Of("symbol");

	/// <summary>
	/// The type of the value <c>bigint</c>.
	/// </summary>
	/// <returns>A type expression for the given bigint type.</returns>
	/// <example><c>TsType.BigInt</c> is equivalent to <c>TsType.Of("bigint")</c> and equals <c>bigint</c>.</example>
	public static TsType BigInt => Of("bigint");

	/// <summary>
	/// Builds a type expression for the given named type.
	/// </summary>
	/// <param name="nameOrTypeParam">The name of the type or a type expression.</param>
	/// <returns>A type expression for the given named type.</returns>
	/// <example><c>TsType.Of("Date")</c> equals <c>Date</c>.</example>
	public static TsType Of(string nameOrTypeParam) => new NamedType(nameOrTypeParam);

	/// <summary>
	/// Builds a type expression for the given array type.
	/// </summary>
	/// <param name="elem">The type of the array elements.</param>
	/// <returns>A type expression for the given array type.</returns>
	/// <example><c>TsType.ArrayOf(TsType.Number)</c> equals <c>number[]</c>.</example>
	public static TsType ArrayOf(TsType elem) => new ArrayType(elem);

	/// <summary>
	/// Builds a type expression for the given tuple type.
	/// </summary>
	/// <param name="elems">The types of the tuple elements.</param>
	/// <returns>A type expression for the given tuple type.</returns>
	/// <example><c>TsType.Tuple(TsType.Number, TsType.String)</c> equals <c>[number, string]</c>.</example>
	public static TsType Tuple(params TsType[] elems) => new TupleType(elems);

	/// <summary>
	/// Builds a type expression for the given union type.
	/// </summary>
	/// <param name="elems">The types of the union elements.</param>
	/// <returns>A type expression for the given union type.</returns>
	/// <example><c>TsType.Union(TsType.Number, TsType.String)</c> equals <c>number | string</c>.</example>
	public static TsType Union(params TsType[] elems) => new UnionType(elems);

	/// <summary>
	/// Builds a type expression for the given intersection type.
	/// </summary>
	/// <param name="elems">The types of the intersection elements.</param>
	/// <returns>A type expression for the given intersection type.</returns>
	/// <example><c>TsType.Intersection(TsType.Number, TsType.String)</c> equals <c>number &amp; string</c>.</example>
	public static TsType Intersection(params TsType[] elems) => new IntersectionType(elems);

	/// <summary>
	/// Builds a type expression for the given optional type.
	/// </summary>
	/// <param name="inner">The type of the optional element.</param>
	/// <returns>A type expression for the given optional type.</returns>
	/// <example><c>TsType.Optional(TsType.Number)</c> equals <c>number | undefined</c>.</example>
	public static TsType Optional(TsType inner) => new OptionalType(inner);

	/// <summary>
	/// Builds a type expression for the given generic type.
	/// </summary>
	/// <param name="name">The name of the generic type.</param>
	/// <param name="args">The type arguments of the generic type.</param>
	/// <returns>A type expression for the given generic type.</returns>
	/// <example><c>TsType.Generic("Promise", TsType.String)</c> equals <c>Promise&lt;string&gt;</c>.</example>
	public static TsType Generic(string name, params TsType[] args) => new GenericType(name, args);

	/// <summary>
	/// Builds a type expression for the given object type.
	/// </summary>
	/// <param name="props">The properties of the object type.</param>
	/// <returns>A type expression for the given object type.</returns>
	/// <example>
	/// <c>TsType.Object("foo", TsType.String, "bar", TsType.Number)</c> equals <c>{ foo: string; bar: number; }</c>.
	/// </example>
	public static TsType Object(params (string name, TsType type)[] props) => new ObjectType(props);

	/// <summary>
	/// Builds a type expression for the given mapped type.
	/// </summary>
	/// <param name="keyParam">The type of the mapped keys.</param>
	/// <param name="valueType">The type of the mapped values.</param>
	/// <returns>A type expression for the given mapped type.</returns>
	/// <example>
	/// <c>TsType.Mapped(TsType.Of("Foo"), TsType.String)</c> equals <c>{ [K in Foo]: string; }</c>.
	/// </example>
	public static TsType Mapped(string keyParam, TsType valueType) => new MappedType(keyParam, valueType);

	/// <summary>
	/// Convenience for <c>{ [P in keyof Foo]: V }</c>
	/// <example>
	/// <c>TsType.MappedKeyof("Foo", TsType.String)</c> equals <c>{ [P in keyof Foo]: string; }</c>.
	/// </example>
	/// </summary>
	public static TsType MappedKeyof(string targetName, TsType value) => new MappedType($"keyof {targetName}", value);

	private class NamedType : TsType
	{
		private readonly string _name;

		public NamedType(string name) => _name = name;

		public override void Write(TsCodeWriter w) => w.Write(_name);
	}

	private class ArrayType : TsType
	{
		private readonly TsType _elem;

		public ArrayType(TsType e) => _elem = e;

		public override void Write(TsCodeWriter w)
		{
			_elem.Write(w);
			w.Write("[]");
		}
	}

	private class TupleType : TsType
	{
		private readonly TsType[] _elems;

		public TupleType(TsType[] e) => _elems = e;

		public override void Write(TsCodeWriter w)
		{
			w.Write("[");
			for (int i = 0; i < _elems.Length; i++)
			{
				_elems[i].Write(w);
				if (i < _elems.Length - 1)
					w.Write(", ");
			}

			w.Write("]");
		}
	}

	private class UnionType : TsType
	{
		private readonly TsType[] _types;

		public UnionType(TsType[] t) => _types = t;

		public override void Write(TsCodeWriter w)
		{
			for (int i = 0; i < _types.Length; i++)
			{
				_types[i].Write(w);
				if (i < _types.Length - 1)
					w.Write(" | ");
			}
		}
	}

	private class IntersectionType : TsType
	{
		private readonly TsType[] _types;

		public IntersectionType(TsType[] t) => _types = t;

		public override void Write(TsCodeWriter w)
		{
			for (int i = 0; i < _types.Length; i++)
			{
				_types[i].Write(w);
				if (i < _types.Length - 1)
					w.Write(" & ");
			}
		}
	}

	private class OptionalType : TsType
	{
		private readonly TsType _inner;

		public OptionalType(TsType inner) => _inner = inner;

		public override void Write(TsCodeWriter w)
		{
			_inner.Write(w);
			w.Write(" | undefined");
		}
	}

	private class GenericType : TsType
	{
		private readonly string _name;
		private readonly TsType[] _args;

		public GenericType(string n, TsType[] a)
		{
			_name = n;
			_args = a;
		}

		public override void Write(TsCodeWriter w)
		{
			w.Write(_name);
			w.Write("<");
			for (int i = 0; i < _args.Length; i++)
			{
				_args[i].Write(w);
				if (i < _args.Length - 1)
					w.Write(", ");
			}

			w.Write(">");
		}
	}

	private class ObjectType : TsType
	{
		private readonly (string name, TsType type)[] _props;

		public ObjectType((string, TsType)[] p) => _props = p;

		public override void Write(TsCodeWriter w)
		{
			w.Write("{ ");
			for (int i = 0; i < _props.Length; i++)
			{
				w.Write(_props[i].name);
				w.Write(": ");
				_props[i].type.Write(w);
				if (i < _props.Length - 1)
					w.Write("; ");
			}

			w.Write(" }");
		}
	}

	private class MappedType : TsType
	{
		private readonly string _key;
		private readonly TsType _val;

		public MappedType(string keyParam, TsType val)
		{
			_key = keyParam;
			_val = val;
		}

		public override void Write(TsCodeWriter w)
		{
			w.Write("{ [K in ");
			w.Write(_key);
			w.Write("]: ");
			_val.Write(w);
			w.Write(" }");
		}
	}
}
