﻿using NUnit.Framework;
using Moq;
using Rubberduck.Refactorings.EncapsulateField;
using RubberduckTests.Mocks;
using Rubberduck.SmartIndenter;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using System.Linq;
using System.Collections.Generic;
using Rubberduck.VBEditor;
using Rubberduck.Refactorings;
using Rubberduck.Parsing.Rewriter;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor.Utility;
using Rubberduck.Parsing.Symbols;
using System;
using Rubberduck.VBEditor.SafeComWrappers;

namespace RubberduckTests.Refactoring.EncapsulateField
{
    [TestFixture]
    public class EncapsulateFieldValidatorTests : InteractiveRefactoringTestBase<IEncapsulateFieldPresenter, EncapsulateFieldModel>
    {
        private EncapsulateFieldTestSupport Support { get; } = new EncapsulateFieldTestSupport();

        [TestCase("fizz", "_Fizz", false)]
        [TestCase("fizz", "FizzProp", true)]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void VBAIdentifier_Property(string originalFieldName, string newPropertyName, bool expectedResult)
        {
            string inputCode =
$@"Public {originalFieldName} As String";

            var encapsulatedField = Support.RetrieveEncapsulatedField(inputCode, originalFieldName);

            encapsulatedField.PropertyName = newPropertyName;
            encapsulatedField.EncapsulateFlag = true;
            //var field = encapsulatedField as IEncapsulateFieldCandidateValidations;
            Assert.AreEqual(expectedResult, encapsulatedField.TryValidateEncapsulationAttributes(out _));
        }

//        [TestCase("fizz", "_Fizz", false)]
//        [TestCase("fizz", "FizzProp", true)]
//        [Category("Refactorings")]
//        [Category("Encapsulate Field")]
//        public void VBAIdentifier_FieldName(string originalFieldName, string newFieldName, bool expectedResult)
//        {
//            string inputCode =
//$@"Public {originalFieldName} As String";

//            var encapsulatedField = Support.RetrieveEncapsulatedField(inputCode, originalFieldName);

//            encapsulatedField.FieldIdentifier = newFieldName;
//            var field = encapsulatedField as IEncapsulateFieldCandidateValidations;
//            Assert.AreEqual(expectedResult, field.HasVBACompliantFieldIdentifier);
//        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void EncapsulatePrivateField_ReadOnlyRequiresSet()
        {
            const string inputCode =
                @"|Private fizz As Collection";

            const string expectedCode =
                @"Private fizz As Collection

Public Property Get Name() As Collection
    Set Name = fizz
End Property
";
            var presenterAction = Support.SetParametersForSingleTarget("fizz", "Name", isReadonly: true);
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            Assert.AreEqual(expectedCode.Trim(), actualCode);
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void PropertyNameNotDuplicated()
        {
            const string inputCode =
                @"Public var|iable As Integer, variable1 As Long, variable2 As Integer";

            var userInput = new UserInputDataObject()
                .AddAttributeSet("variable")
                .AddAttributeSet("variable1")
                .AddAttributeSet("variable2");

            var presenterAction = Support.SetParameters(userInput);
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            StringAssert.Contains("Public Property Get Variable() As Integer", actualCode);
            StringAssert.Contains("Variable = variable_1", actualCode);
            StringAssert.Contains("Public Property Get Variable1() As Long", actualCode);
            StringAssert.Contains("Variable1 = variable1_1", actualCode);
            StringAssert.Contains("Public Property Get Variable2() As Integer", actualCode);
            StringAssert.Contains("Variable2 = variable2_1", actualCode);
            StringAssert.DoesNotContain("Public Property Get Variable3() As Integer", actualCode);
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void UDTMemberPropertyConflictsWithExistingFunction()
        {
            string inputCode =
$@"
Private Type TBar
    First As String
    Second As Long
End Type

Public myBar As TBar

Private Function First() As String
    First = myBar.First
End Function";

            var encapsulatedField = Support.RetrieveEncapsulatedField(inputCode, "First", DeclarationType.UserDefinedTypeMember);
            var validation = encapsulatedField as IEncapsulateFieldCandidateValidations;
            var result = validation.HasConflictingPropertyIdentifier;
            Assert.AreEqual(true, validation.HasConflictingPropertyIdentifier);
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void FieldNameDefaultsToNonConflictName()
        {
            string inputCode =
$@"Public fizz As String

            Private fizzle As String

            'fizz1 is the initial default name for encapsulating 'fizz'            
            Public Property Get Fizz_1() As String
                Fizz_1 = fizzle
            End Property

            Public Property Let Fizz_1(ByVal value As String)
                fizzle = value
            End Property
            ";
            var encapsulatedField = Support.RetrieveEncapsulatedField(inputCode, "fizz");
            Assert.IsTrue(encapsulatedField.TryValidateEncapsulationAttributes(out _));
        }

        [TestCase("Name")]
        [TestCase("mName")]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void UserEntersConflictingName(string userModifiedPropertyName)
        {
            string inputCode =
$@"Public fizz As String

            Private mName As String

            Public Property Get Name() As String
                Name = mName
            End Property

            Public Property Let Name(ByVal value As String)
                mName = value
            End Property
            ";

            var fieldUT = "fizz";
            var userInput = new UserInputDataObject("fizz", userModifiedPropertyName, true);

            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out _).Object;
            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.IsFalse(model["fizz"].TryValidateEncapsulationAttributes(out _));
        }

        [TestCase("Number", "Bazzle", true, true)]
        [TestCase("Number", "Number", false, false)]
        [TestCase("Test", "Number", false, true)]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void UserModificationIsExistingPropertyNameConflicts(string fizz_modifiedPropertyName, string bazz_modifiedPropertyName, bool fizz_expectedResult, bool bazz_expectedResult)
        {
            string inputCode =
$@"Public fizz As Integer
Public bazz As Integer
Public buzz As Integer

Private mTest As Integer

Public Property Get Test() As Integer
    Test = mTest
End Property";

            var fieldUT = "fizz";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT, fizz_modifiedPropertyName, true)
                .AddAttributeSet("bazz", bazz_modifiedPropertyName, true);


            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out _).Object;
            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.AreEqual(fizz_expectedResult, model["fizz"].TryValidateEncapsulationAttributes(out _), "fizz failed");
            Assert.AreEqual(bazz_expectedResult, model["bazz"].TryValidateEncapsulationAttributes(out _), "bazz failed");
        }

        [TestCase("Private", "Private")]
        [TestCase("Public", "Private")]
        [TestCase("Private", "Public")]
        [TestCase("Public", "Public")]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void EncapsulateMultipleUDTFields_DefaultsAreNotInConflict(string udtAccessibility, string fieldAccessibility)
        {
            string inputCode =
$@"
{udtAccessibility} Type TBar
    First As Long
    Second As String
End Type

{fieldAccessibility} this As TBar

{fieldAccessibility} that As TBar
";
            var fieldUT = "this";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT)
                .AddAttributeSet("that");

            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out _).Object;
            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.AreEqual(true, model[fieldUT].TryValidateEncapsulationAttributes(out var message), message);
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void PropertyNameConflictsWithModuleVariable()
        {
            string inputCode =
$@"
Public longValue As Long

Public wholeNumber As String
";
            var fieldUT = "longValue";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT, "WholeNumber", true);

            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out _).Object;
            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.AreEqual(false, model[fieldUT].TryValidateEncapsulationAttributes(out _));
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void EncapsulatePrivateField_EnumMemberConflict()
        {
            //5.2.3.4: An enum member name may not be the same as any variable name, or constant name that is defined within the same module
            const string inputCode =
                @"

Public Enum NumberTypes 
     Whole = -1 
     Integral = 0 
     Rational_1 = 1 
End Enum

Private rati|onal As NumberTypes
";

            var presenterAction = Support.UserAcceptsDefaults();
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            StringAssert.Contains("Public Property Get Rational() As NumberTypes", actualCode);
            StringAssert.Contains("Rational = rational_2", actualCode);
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void EncapsulatePrivateField_UDTMemberConflict()
        {
            const string inputCode =
                @"

Private Type TVehicle
    Wheels As Integer
    MPG As Double
End Type

Private whe|els As Integer
";

            var presenterAction = Support.UserAcceptsDefaults();
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            StringAssert.Contains("Public Property Get Wheels()", actualCode);
            StringAssert.Contains("Wheels = wheels_1", actualCode);
        }

        [TestCase("Dim test As String", "arg")] //Local variable
        [TestCase(@"Const test As String = ""Foo""", "arg")] //Local constant
        [TestCase(@"Const localTest As String = ""Foo""", "test")] //parameter
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void TargetNameUsedForLimitedScopeDeclarations(string localDeclaration, string parameter)
        {
            string inputCode =
$@"
Private te|st As Long

Private Function Foo({parameter} As String) As String
    {localDeclaration}
    test = test & ""Foo""
    Foo = test
End Function
";
            var presenterAction = Support.UserAcceptsDefaults();
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            StringAssert.Contains("Test", actualCode);
            StringAssert.Contains("test_1", actualCode);
            StringAssert.DoesNotContain("Test_1", actualCode);
        }

        [TestCase("Dim test As String", "arg")] //Local variable
        [TestCase(@"Const test As String = ""Foo""", "arg")] //Local constant
        [TestCase(@"Const localTest As String = ""Foo""", "test")] //parameter
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void TargetReferenceScopeUsesPropertyName(string localDeclaration, string parameter)
        {
            string inputCode =
$@"
Private aName As String

Private Function Foo({parameter} As String) As String
    {localDeclaration}
    test = aName & test
    Foo = test
End Function
";
            var fieldUT = "aName";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT, "Test", true);

            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out _).Object;
            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.AreEqual(false, model[fieldUT].TryValidateEncapsulationAttributes(out _));
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void TargetDefaultFieldIDConflict()
        {
            string inputCode =
$@"
Private tes|t As String
Private test_1 As String

Public Sub Foo(arg As String)
    test = arg & test_1
End Sub
";
            var presenterAction = Support.UserAcceptsDefaults();
            var actualCode = Support.RefactoredCode(inputCode.ToCodeString(), presenterAction);
            StringAssert.Contains("Test", actualCode);
            StringAssert.Contains("Private test_2 As String", actualCode);
            StringAssert.DoesNotContain("test_1 = arg & test_1", actualCode);
        }

        [TestCase(MockVbeBuilder.TestModuleName)]
        [TestCase("TestModule")]
        [TestCase("TestClass")]
        [TestCase(MockVbeBuilder.TestProjectName)]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void ModuleAndProjectNamesAreValid(string userEnteredName)
        {
            var fieldUT = "foo";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT, userEnteredName, true);

            var presenterAction = Support.SetParameters(userInput);

            var vbe = MockVbeBuilder.BuildFromModules(
                (MockVbeBuilder.TestModuleName, "Private foo As String", ComponentType.StandardModule),
                ("TestModule", "Private foo1 As String", ComponentType.StandardModule),
                ("TestClass", "Private foo2 As String", ComponentType.ClassModule)).Object;

            var model = Support.RetrieveUserModifiedModelPriorToRefactoring(vbe, fieldUT, DeclarationType.Variable, presenterAction);

            Assert.AreEqual(true, model[fieldUT].TryValidateEncapsulationAttributes(out _));
        }

        [Test]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void MultipleUserDefinedTypesOfSameNameOtherModule()
        {
            var moduleOneName = "ModuleOne";
            string inputCode =
$@"
Option Explicit

Public mF|oo As Long
";

            string module2Content =
$@"
Public Type TModuleOne
    FirstVal As Long
    SecondVal As String
End Type
";

            var fieldUT = "mFoo";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT);

            userInput.EncapsulateAsUDT = true;

            var presenterAction = Support.SetParameters(userInput);

            var codeString = inputCode.ToCodeString();
            var actualModuleCode = RefactoredCode(
                moduleOneName,
                codeString.CaretPosition.ToOneBased(),
                presenterAction,
                null,
                false,
                ("Module2", module2Content, ComponentType.StandardModule),
                (moduleOneName, codeString.Code, ComponentType.StandardModule));

            var actualCode = actualModuleCode[moduleOneName];

            StringAssert.Contains($"Private Type TModuleOne", actualCode);
        }

        [TestCase("Public")]
        [TestCase("Private")]
        [Category("Refactorings")]
        [Category("Encapsulate Field")]
        public void MultipleUserDefinedTypesOfSameNameSameModule(string accessibility)
        {
            var moduleOneName = "ModuleOne";
            string inputCode =
$@"
Option Explicit

{accessibility} Type TModuleOne
    FirstVal As Long
    SecondVal As String
End Type

Public mF|oo As Long
";


            var fieldUT = "mFoo";
            var userInput = new UserInputDataObject()
                .AddAttributeSet(fieldUT);

            userInput.EncapsulateAsUDT = true;

            var presenterAction = Support.SetParameters(userInput);

            var codeString = inputCode.ToCodeString();
            var actualModuleCode = RefactoredCode(
                moduleOneName,
                codeString.CaretPosition.ToOneBased(),
                presenterAction,
                null,
                false,
                (moduleOneName, codeString.Code, ComponentType.StandardModule));

            var actualCode = actualModuleCode[moduleOneName];

            StringAssert.Contains($"Private Type TModuleOne_1", actualCode);
        }


        protected override IRefactoring TestRefactoring(IRewritingManager rewritingManager, RubberduckParserState state, IRefactoringPresenterFactory factory, ISelectionService selectionService)
        {
            return Support.SupportTestRefactoring(rewritingManager, state, factory, selectionService);
        }
    }
}
