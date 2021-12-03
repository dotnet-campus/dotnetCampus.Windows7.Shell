using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Markup;

[assembly: ComVisible(false)]
[assembly: Guid("573618e1-4f3f-4395-a3bf-ffebfb342917")]
[assembly: CLSCompliant(true)]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation/shell", "Microsoft.Windows.Shell")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Justification = "Internal-only namespace", Scope = "namespace", Target = "Standard")]
[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Assembly has strong name when published")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyDefaultAlias("Microsoft.Windows.Shell.dll")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
