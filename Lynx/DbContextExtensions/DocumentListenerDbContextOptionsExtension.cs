using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.DbContextExtensions;

/// <summary>
/// An extension for adding a document session listener to the database context.
/// </summary>
internal class DocumentListenerDbContextOptionsExtension : IDbContextOptionsExtension
{
    private readonly IDocumentSessionListener _listener;

    public DocumentListenerDbContextOptionsExtension(IDocumentSessionListener listener)
    {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
    }
    
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(_listener);
    }

    public void Validate(IDbContextOptions options)
    {
        
    }

    public DbContextOptionsExtensionInfo Info => new DocumentListenerDbContextOptionsExtensionInfo(this);
    
    private class DocumentListenerDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        private readonly DocumentListenerDbContextOptionsExtension _extension;

        public DocumentListenerDbContextOptionsExtensionInfo(DocumentListenerDbContextOptionsExtension extension)
            : base(extension)
        {
            _extension = extension;
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "DocumentListener";

        public override int GetServiceProviderHashCode() => _extension._listener.GetHashCode();
        
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is DocumentListenerDbContextOptionsExtensionInfo otherInfo
            && otherInfo._extension._listener == _extension._listener;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}