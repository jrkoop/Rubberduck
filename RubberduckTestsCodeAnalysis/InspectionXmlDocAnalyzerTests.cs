﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

namespace RubberduckCodeAnalysis.Test
{
    [TestFixture]
    public class InspectionXmlDocAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new InspectionXmlDocAnalyzer();

        private Diagnostic[] GetDiagnostics(string code)
        {
            const string iinspection = @"
public interface IInspection { }
public class RequiredLibraryAttribute : System.Attribute { }
";
            return GetSortedDiagnostics(new[] { iinspection + code }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer());
        }

        [Test][Category("InspectionXmlDoc")]
        public void NegativeIfNotInNamespaceRubberduckCodeAnalysisInspectionConcrete()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.NotConcrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionSummaryElement));
        }

        [Test][Category("InspectionXmlDoc")]
        public void NegativeIfNotInInheritingIInspection()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Abstract
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : NotIInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionSummaryElement));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingInspectionSummary()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionSummaryElement));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingInspectionSummary_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionSummaryElement));
        }


        [Test][Category("InspectionXmlDoc")]
        public void MissingInspectionWhyElement()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionWhyElement));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingInspectionWhyElement_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingInspectionWhyElement));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingReferenceElement()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""Excel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingReferenceElement));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingReferenceElement_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference name=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""Excel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingReferenceElement));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingReferenceAttribute_Missing()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference name=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingRequiredLibraryAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        [Ignore("This does not work because the Attribute does not exist in this solution. It only exists in the main solution.")]
        public void MissingReferenceAttribute_WrongLibrary()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference name=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""NotExcel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingRequiredLibraryAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingReferenceAttribute_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference name=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""Excel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingRequiredLibraryAttribute));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingNameAttribute_ReferenceElement()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference bad=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""Excel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingNameAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingNameAttribute_ReferenceElement_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <reference name=""Excel"" />
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    [RequiredLibrary(""Excel"")]
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingNameAttribute));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingHasResultAttribute_Misspelled()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasResults=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingHasResultAttribute));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingHasResultAttribute_InconsistentCasingIsNotMissing()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example HasResult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingHasResultAttribute));
        }
        [Test][Category("InspectionXmlDoc")]
        public void MissingHasResultAttribute_Missing()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example>
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingHasResultAttribute));
        }

        [Test][Category("InspectionXmlDoc")]
        public void MissingHasResultAttribute_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <why>
    /// blablabla
    /// </why>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingHasResultAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingModuleElement()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingModuleElement));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingModuleElement_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module name=""MyModule"" type=""Standard Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingModuleElement));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingNameAttribute_ModuleElement()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module noName=""MyModule"" type=""Standard Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingNameAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingNameAttribute_ModuleElement_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module name=""MyModule"" type=""Standard Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingNameAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingTypeAttribute()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module name=""MyModule"" noType=""Standard Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingTypeAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void MissingTypeAttribute_Negative()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module name=""MyModule"" type=""Standard Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.MissingTypeAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        public void InvalidTypeAttribute()
        {
            var test = @"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{
    /// <summary>
    /// blablabla
    /// </summary>
    /// <example hasresult=""true"">
    /// <module name=""MyModule"" type=""Procedural Module"">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     ' ...
    /// End Sub
    /// ]]>
    /// </module>
    /// </example>
    public sealed class SomeInspection : IInspection { }
}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsTrue(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.InvalidTypeAttribute));
        }

        [Test]
        [Category("InspectionXmlDoc")]
        [TestCase("Standard Module")]
        [TestCase("Class Module")]
        [TestCase("Document")]
        [TestCase("User Form")]
        public void InvalidTypeAttribute_Negative(string typeName)
        {
            var test = $@"
namespace Rubberduck.CodeAnalysis.Inspections.Concrete
{{
        /// <summary>
        /// blablabla
        /// </summary>
        /// <example hasresult=""true"">
        /// <module name=""MyModule"" type=""{typeName}"">
        /// <![CDATA[
        /// Public Sub DoSomething()
        ///     ' ...
        /// End Sub
        /// ]]>
        /// </module>
        /// </example>
        public sealed class SomeInspection : IInspection {{ }}
}}
";

            var diagnostics = GetDiagnostics(test);
            Assert.IsFalse(diagnostics.Any(d => d.Descriptor.Id == InspectionXmlDocAnalyzer.InvalidTypeAttribute));
        }
    }
}