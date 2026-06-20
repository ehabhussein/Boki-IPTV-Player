namespace BokiIPTV.Core.Services;
public interface IConfigService { AppConfig Load(); void Save(AppConfig config); }
