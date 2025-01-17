using System.Linq.Expressions;
using FluentAssertions.Events;
using Lynx.DocumentStore.Query;
using Lynx.EfCore;
using Lynx.EfCore.OptionalForeign;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Lynx.Tests;

public class OptionalReference(ITestOutputHelper output)
{
}