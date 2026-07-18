using BasicUtilities;
using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests;

[TestClass]
public sealed class PrototypeDefinitionHelpers_Tests
{
	[TestInitialize]
	public void InitializeOntology()
	{
		Initializer.Initialize();
	}

	// Purpose: Preserve direct inheritance and use the interpreter's simple field name for qualified field prototypes.
	[TestMethod]
	public void PrototypeToPrototypeDefinition_QualifiedFieldName_ParsesCompilesAndAssigns()
	{
		string parentName = UniqueName("ConverterParent");
		string sourceName = UniqueName("ConverterSource");
		Prototype parent = TemporaryPrototypes.GetOrCreateTemporaryPrototype(parentName);
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(sourceName, parent);
		Prototype property = TemporaryPrototypes.GetOrCreateTemporaryPrototype(parentName + ".Field.ExactName");
		source.Properties[property.PrototypeID] = new StringWrapper("value");

		string generated = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);
		AssertSinglePartialDefinition(generated, sourceName);
		StringAssert.Contains(generated, "ExactName = \"value\";");
		Assert.IsFalse(generated.Contains(property.PrototypeName + " =", StringComparison.Ordinal));

		string completeParent = "prototype " + parentName + "\n{\n\tString ExactName;\n}\n";
		ProtoScript.File file = Files.ParseFileContents(completeParent + generated);
		var compiler = new Compiler();
		compiler.Initialize();
		ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
		Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.ToString())));
		var interpretter = new NativeInterpretter(compiler);
		interpretter.Evaluate(compiled);

		Prototype interpreted = Prototypes.GetPrototypeByPrototypeName(sourceName);
		Prototype interpretedProperty = Prototypes.GetPrototypeByPrototypeName(parentName + ".Field.ExactName");
		Assert.IsNotNull(interpretedProperty);
		Assert.AreEqual("value", StringWrapper.ToString(interpreted.Properties[interpretedProperty.PrototypeID]));
	}

	// Purpose: Emit escaped ordinary strings and verbatim multiline strings as one parseable partial definition.
	[TestMethod]
	public void MaterializePrototypeToString_EmitsEscapedAndMultilineStrings()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("StringSource"));
		SetProperty(source, "EscapedText", new StringWrapper("quoted \"value\"\\path"));
		SetProperty(source, "MultilineText", new StringWrapper("first line\nsecond line"));

		string text = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);

		AssertSinglePartialDefinition(text, source.PrototypeName);
		StringAssert.Contains(text, "quoted \\\"value\\\"\\\\path");
		StringAssert.Contains(text, "@\"first line\nsecond line\"");
	}

	// Purpose: Materialize integer, boolean, and invariant-culture double simulation wrappers.
	[TestMethod]
	public void MaterializePrototypeToString_EmitsSimulationWrapperValues()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("WrapperSource"));
		SetProperty(source, "IntegerValue", new IntWrapper(42));
		SetProperty(source, "BooleanValue", new BoolWrapper(true));
		SetProperty(source, "DoubleValue", new DoubleWrapper(12.75));

		string text = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);

		AssertSinglePartialDefinition(text, source.PrototypeName);
		StringAssert.Contains(text, "= 42;");
		StringAssert.Contains(text, "= true;");
		StringAssert.Contains(text, "= 12.75;");
	}

	// Purpose: Materialize the NativeValuePrototype representation supplied by the current runtime.
	[TestMethod]
	public void MaterializePrototypeToString_EmitsNativeValuePrototypeValues()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("NativeSource"));
		SetProperty(source, "NativeString", NativeValuePrototype.GetOrCreateNativeValuePrototype("native"));
		SetProperty(source, "NativeInteger", NativeValuePrototype.GetOrCreateNativeValuePrototype(7));
		SetProperty(source, "NativeBoolean", NativeValuePrototype.GetOrCreateNativeValuePrototype(false));
		SetProperty(source, "NativeDouble", NativeValuePrototype.GetOrCreateNativeValuePrototype(2.5));

		string text = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);

		AssertSinglePartialDefinition(text, source.PrototypeName);
		StringAssert.Contains(text, "= \"native\";");
		StringAssert.Contains(text, "= 7;");
		StringAssert.Contains(text, "= false;");
		StringAssert.Contains(text, "= 2.5;");
	}

	// Purpose: Preserve ordinary prototype values as identifiers.
	[TestMethod]
	public void MaterializePrototypeToString_EmitsPrototypeReferenceIdentifier()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("ReferenceSource"));
		Prototype target = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("ReferencedPrototype"));
		Prototype property = SetProperty(source, "Related", target);

		string text = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);

		AssertSinglePartialDefinition(text, source.PrototypeName);
		StringAssert.Contains(text, StringUtil.RightOfLast(property.PrototypeName, ".") + " = " + target.PrototypeName + ";");
	}

	// Purpose: Materialize collection-valued properties and direct children as arrays.
	[TestMethod]
	public void MaterializePrototypeToString_EmitsCollectionsAndChildren()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("CollectionSource"));
		Prototype first = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("FirstItem"));
		Prototype second = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("SecondItem"));
		var collection = new Collection();
		collection.Children.Add(first);
		collection.Children.Add(second);
		Prototype itemsProperty = SetProperty(source, "Items", collection);
		source.Children.Add(second);

		string text = PrototypeDefinitionHelpers.MaterializePrototypeToString(source);

		AssertSinglePartialDefinition(text, source.PrototypeName);
		StringAssert.Contains(text, StringUtil.RightOfLast(itemsProperty.PrototypeName, ".") + " = [" + first.PrototypeName + ", " + second.PrototypeName + "];");
		StringAssert.Contains(text, "Children = [" + second.PrototypeName + "];");
	}

	// Purpose: Preserve supported runtime associations as the predecessor's bidirectional annotation.
	[TestMethod]
	public void PrototypeToPrototypeDefinition_EmitsAssociationAnnotation()
	{
		Prototype source = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("AssociationSource"));
		Prototype associated = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName("Associated"));
		source.AssociateWithWeight(associated, 1.0);

		PrototypeDefinition definition = PrototypeDefinitionHelpers.PrototypeToPrototypeDefinition(source);
		string text = SimpleGenerator.Generate(definition);

		Assert.AreEqual(1, definition.Annotations.Count);
		StringAssert.Contains(text, "[BidirectionalAssociation(" + associated.PrototypeName + ")]");
		AssertSinglePartialDefinition(text, source.PrototypeName);
	}

	// Purpose: Fail immediately for a missing source prototype.
	[TestMethod]
	public void Converter_NullPrototype_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => PrototypeDefinitionHelpers.PrototypeToPrototypeDefinition(null!));
		Assert.ThrowsException<ArgumentNullException>(() => PrototypeDefinitionHelpers.MaterializePrototypeToString(null!));
	}

	private static void AssertSinglePartialDefinition(string text, string expectedName)
	{
		var tokenizer = new ProtoScript.Parsers.Tokenizer(text);
		PrototypeDefinition parsed = PrototypeDefinitions.Parse(tokenizer);
		tokenizer.movePastWhitespace();
		Assert.AreEqual(string.Empty, tokenizer.peekNextToken() ?? string.Empty);
		Assert.IsTrue(parsed.IsPartial);
		Assert.AreEqual(expectedName, parsed.PrototypeName.TypeName);
	}

	private static Prototype SetProperty(Prototype source, string propertyName, Prototype value)
	{
		Prototype property = TemporaryPrototypes.GetOrCreateTemporaryPrototype(UniqueName(propertyName));
		source.Properties[property.PrototypeID] = value;
		return property;
	}

	private static string UniqueName(string prefix)
	{
		return prefix + "_" + Guid.NewGuid().ToString("N");
	}
}