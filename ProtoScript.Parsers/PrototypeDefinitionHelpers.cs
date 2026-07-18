using BasicUtilities;
using Ontology;
using Ontology.Simulation;
using System.Globalization;

namespace ProtoScript.Parsers;

// Converts runtime ontology prototypes into partial ProtoScript definitions.
public static class PrototypeDefinitionHelpers
{
	// Convert and materialize one ontology prototype using the standard simple generator.
	public static string MaterializePrototypeToString(Prototype prototype)
	{
		ArgumentNullException.ThrowIfNull(prototype);
		return SimpleGenerator.Generate(PrototypeToPrototypeDefinition(prototype));
	}

	// Convert one runtime prototype while preserving its direct graph facts in a partial definition.
	public static PrototypeDefinition PrototypeToPrototypeDefinition(Prototype prototype)
	{
		ArgumentNullException.ThrowIfNull(prototype);
		var definition = new PrototypeDefinition
		{
			PrototypeName = new ProtoScript.Type { TypeName = prototype.PrototypeName },
			IsPartial = true
		};

		foreach (int typeOfID in prototype.GetTypeOfs())
		{
			Prototype typeOf = Prototypes.GetPrototype(typeOfID);
			definition.Inherits.Add(new ProtoScript.Type { TypeName = typeOf.PrototypeName });
		}

		var initializer = new PrototypeInitializer();
		foreach (KeyValuePair<int, Prototype> property in prototype.NormalProperties)
		{
			Prototype propertyPrototype = Prototypes.GetPrototype(property.Key);
			initializer.Statements.Add(BuildAssignment(StringUtil.RightOfLast(propertyPrototype.PrototypeName, "."), PrototypeToExpression(property.Value)));
		}

		if (prototype.Children.Count > 0)
			initializer.Statements.Add(BuildAssignment("Children", PrototypesToArrayLiteral(prototype.Children)));

		if (initializer.Statements.Count > 0)
			definition.Initializers.Add(initializer);

		foreach (KeyValuePair<Prototype, double> association in prototype.Associations)
			definition.Annotations.Add(AnnotationExpressions.Parse("[BidirectionalAssociation(" + association.Key.PrototypeName + ")]"));

		return definition;
	}

	private static ExpressionStatement BuildAssignment(string propertyName, Expression value)
	{
		return new ExpressionStatement
		{
			Expression = new BinaryOperator
			{
				Value = "=",
				Left = new Identifier(propertyName),
				Right = value
			}
		};
	}

	private static ArrayLiteral PrototypesToArrayLiteral(IEnumerable<Prototype> prototypes)
	{
		var literal = new ArrayLiteral();
		foreach (Prototype prototype in prototypes)
			literal.Values.Add(PrototypeToExpression(prototype));
		return literal;
	}

	private static Expression PrototypeToExpression(Prototype prototype)
	{
		if (prototype is StringWrapper)
			return StringToLiteral(StringWrapper.ToString(prototype));
		if (prototype is IntWrapper)
			return new IntegerLiteral(IntWrapper.ToInteger(prototype).ToString(CultureInfo.InvariantCulture));
		if (prototype is BoolWrapper)
			return new BooleanLiteral(BoolWrapper.ToBoolean(prototype));
		if (prototype is DoubleWrapper)
			return new DoubleLiteral(DoubleWrapper.ToDouble(prototype).ToString("R", CultureInfo.InvariantCulture));
		if (prototype is NativeValuePrototype nativeValue)
			return NativeValueToExpression(nativeValue.NativeValue);
		if (Prototypes.TypeOf(prototype, Collection.Prototype))
			return PrototypesToArrayLiteral(prototype.Children);
		return new Identifier(prototype.PrototypeName);
	}

	private static Expression NativeValueToExpression(object value)
	{
		return value switch
		{
			string stringValue => StringToLiteral(stringValue),
			int intValue => new IntegerLiteral(intValue.ToString(CultureInfo.InvariantCulture)),
			bool boolValue => new BooleanLiteral(boolValue),
			double doubleValue => new DoubleLiteral(doubleValue.ToString("R", CultureInfo.InvariantCulture)),
			_ => throw new NotSupportedException("Unsupported native prototype value type: " + value.GetType().FullName + ".")
		};
	}

	private static StringLiteral StringToLiteral(string value)
	{
		if (value.Contains('\n') && !value.Contains('"'))
			return new StringLiteral("@\"" + value + "\"");
		return new StringLiteral(StringHelper.EscapeStringForCSharpLiteral(value));
	}
}
