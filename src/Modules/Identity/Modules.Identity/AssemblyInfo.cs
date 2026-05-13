using System.Runtime.CompilerServices;
using AMIS.Framework.Web.Modules;

[assembly: AmisModule(typeof(AMIS.Modules.Identity.IdentityModule), 100)]
[assembly: InternalsVisibleTo("Identity.Tests")]


