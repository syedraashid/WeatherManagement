namespace WeatherManagement.Domain.Contracts;

public interface ISyncNotifier
{
    Task NotifyAsync(string triggeredBy, int locationCount);
}
